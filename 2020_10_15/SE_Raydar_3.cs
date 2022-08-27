// Raydar Script
// Version: 20.05.27.09.36
// Written by: Laihela


// CONFIGURATION VARIABLES

// The raydar will ignore all target types in this list.
List<MyDetectedEntityType> IgnoreEntities = new List<MyDetectedEntityType>{
	MyDetectedEntityType.Planet,
	MyDetectedEntityType.Asteroid,
	//MyDetectedEntityType.LargeGrid,
	MyDetectedEntityType.FloatingObject,
};

// How long the radar will attempt to track a lost target before giving up.
const double MaxBlindTrackTime = 1.0;

// How fast the raydar rotates while scanning for targets in revolutions per minute.
// Has no effect if the raydar is copying a turret.
const float ScanRotationSpeed = 10F;

// Pitch angle of the raydar dish while scanning for targets in degrees.
// Has no effect if the raydar is copying a turret.
const float ScanPitchAngle = 20F;

 // How hard the rotors correct for aim deviation.
 // Lower values make aiming smoother, higher values make aiming more accurate. 
 // Can cause oscillation if set too high.
const int AimCorrectionRate = 32;

// Angle of the camera raycasting cone in degrees.
// Higher values increase possible detection angle at the cost of range effectiveness.
// Maximum angle is 45°.
const float ScanAngle = 12.0F;

// Number of scans performed per program loop.
// Higher values increase scanning effectiveness at the cost of range.
const int ScansPerTick = 3;


// PROGRAM VARIABLES

// Blocks
List<IMyCameraBlock> Cameras = new List<IMyCameraBlock>{};
List<IMyMotorStator> Rotors = new List<IMyMotorStator>{};
IMyBroadcastListener TargetReceiver;
IMyRadioAntenna Antenna;
IMyLargeTurretBase Turret;
// Other
DateTime LastTrack = DateTime.Now;
Random Rand = new Random();
MyDetectedEntityInfo Target;
string State = "scanning";
int CameraCounter = 0;


// MAIN PROGRAM

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	List<IMyTerminalBlock> blocks = GetBlocksInSameGroupAs(Me);
	if (blocks == null) throw new Exception("Programmable block is not in a block group");
	foreach (IMyTerminalBlock block in blocks) {
		if (block is IMyLargeTurretBase) Turret = block as IMyLargeTurretBase;
		else if (block is IMyRadioAntenna) Antenna = block as IMyRadioAntenna;
		else if (block is IMyCameraBlock) Cameras.Add(block as IMyCameraBlock);
		else if (block is IMyMotorStator) Rotors.Add(block as IMyMotorStator);
	}
	if (Cameras.Count == 0) throw new Exception("No cameras grouped with programmable block");
	foreach (IMyCameraBlock camera in Cameras) camera.EnableRaycast = true;
	TargetReceiver = IGC.RegisterBroadcastListener("Target");
}

public void Main(string argument) {
	Print($"Range: {String.Format("{0:0.00}", Cameras[CameraCounter].AvailableScanRange / 1000F)} km");
	if (TargetReceiver.HasPendingMessage) {
		Target = Unpack(TargetReceiver.AcceptMessage().Data.ToString());
		LastTrack = DateTime.Now;
		State = "tracking";
	}
	if (State == "scanning") {
		Print("\nState: Scanning", true);
		if (Antenna != null) Antenna.HudText = "(Scanning)";
		bool targetDetected = ScanForTargets(ScansPerTick);
		if (targetDetected) State = "tracking";
		else {
			if (Turret != null) AimRadarWithTurret();
			else RotateRadarDish(ScanRotationSpeed, ScanPitchAngle);
		}
	}
	else if (State == "tracking") {
		Print($"\nState: Tracking {Target.Name}", true);
		double timeFromLastTrack = (DateTime.Now - LastTrack).Ticks / 10e6D;
		bool targetLock = TrackTarget();
		if (targetLock) {
			if (Antenna != null) Antenna.HudText = $"(Tracking {Target.Name})";
			AimRadarDish(Target.Position);
			string data = Pack(Target);
			IGC.SendBroadcastMessage("Target", data, TransmissionDistance.TransmissionDistanceMax);
		}
		else if (timeFromLastTrack < MaxBlindTrackTime) {
			if (Antenna != null) Antenna.HudText = $"(Seeking {Target.Name})";
			AimRadarDish(Target.Position);
		}
		else State = "scanning";
	}
	else State = "scanning";
}


// MAIN HELPER FUNCTIONS

string Pack(MyDetectedEntityInfo target) {
	Vector3D pos = target.Position;
	Vector3D vel = target.Velocity;
	return $"{pos.X}:{pos.Y}:{pos.Z}:{vel.X}:{vel.Y}:{vel.Z}";
}

MyDetectedEntityInfo Unpack(string data) {
	string[] values = data.Split(':');
	Vector3D[] vectors = {
		new Vector3D(double.Parse(values[0]), double.Parse(values[1]), double.Parse(values[2])),
		new Vector3D(double.Parse(values[3]), double.Parse(values[4]), double.Parse(values[5]))
	};
	MyDetectedEntityInfo target = new MyDetectedEntityInfo(
		0, "REMOTE TARGET", MyDetectedEntityType.Unknown, null, MatrixD.Identity, vectors[1],
		MyRelationsBetweenPlayerAndBlock.Enemies,
		new BoundingBoxD(vectors[0] - Vector3D.One, vectors[0] + Vector3D.One), 1
	);
	return target;
}

void AimRadarDish(Vector3D target) {
	foreach (IMyMotorStator rotor in Rotors) {
		Vector3D localTarget = VectorToBlockSpace(target, rotor);
		float targetAngle = (float)Math.Atan2(-localTarget.X, localTarget.Z);
		float deviation = targetAngle - rotor.Angle;
		if (deviation < -(float)Math.PI) deviation += (float)Math.PI * 2f;
		rotor.SetValue<float>("Velocity", deviation * AimCorrectionRate);
	}
}

void AimRadarWithTurret () {
	Vector3D targetDirection = MatrixD.CreateFromYawPitchRoll(Turret.Azimuth, Turret.Elevation, 0D).Forward;
	targetDirection = Vector3D.Transform(targetDirection, Turret.WorldMatrix) - Turret.GetPosition();
	foreach (IMyMotorStator rotor in Rotors) {
		Vector3D localTarget = VectorToBlockSpace(targetDirection + rotor.GetPosition(), rotor);
		float targetAngle = (float)Math.Atan2(-localTarget.X, localTarget.Z);
		float deviation = targetAngle - rotor.Angle;
		if (deviation < -(float)Math.PI) deviation += (float)Math.PI * 2f;
		rotor.SetValue<float>("Velocity", deviation * AimCorrectionRate);
	}
}

void RotateRadarDish(float yawRPM, float pitch) {
	foreach (IMyMotorStator rotor in Rotors) {
		if (rotor.CustomName.Contains("(Yaw)")) {
			rotor.SetValue<float>("Velocity", yawRPM);
		}
		else {
			float deviation = pitch / 180 * (float)Math.PI - rotor.Angle;
			if (deviation < -(float)Math.PI) deviation += (float)Math.PI * 2f;
			rotor.SetValue<float>("Velocity", deviation * AimCorrectionRate);
		}
	}
}

bool TrackTarget() {
	IMyCameraBlock camera = Cameras[CameraCounter];
	CameraCounter = (CameraCounter + 1) % Cameras.Count;
	double timeFromLastTrack = (DateTime.Now - LastTrack).Ticks / 10e6D;
	MyDetectedEntityInfo newTarget = camera.Raycast(camera.AvailableScanRange, VectorToBlockSpace(Target.Position + Target.Velocity * (float)timeFromLastTrack, camera));
	bool success = !(newTarget.IsEmpty() || IgnoreEntities.Contains(newTarget.Type) || newTarget.Relationship.IsFriendly()) || newTarget.Name == "Shoot me pls";
	if (success) {
		LastTrack = DateTime.Now;
		Target = newTarget;
	}
	return success;
}

bool ScanForTargets(int scans) {
	bool success = false;
	for (int s = 0; s < scans; s++) {
		IMyCameraBlock camera = Cameras[CameraCounter];
		CameraCounter = (CameraCounter + 1) % Cameras.Count;
		float pitch = ((float)Rand.NextDouble() * 2 - 1) * ScanAngle;
		float yaw = ((float)Rand.NextDouble() * 2 - 1) * ScanAngle;
		Target = camera.Raycast(camera.AvailableScanRange, pitch, yaw);
		success = !(Target.IsEmpty() || IgnoreEntities.Contains(Target.Type) || Target.Relationship.IsFriendly()) || Target.Name == "Shoot me pls";
		if (success) break;
	}
	return success;
}


// GENERIC HELPER FUNCTIONS

void Print(string text, bool append = false) {
	Me.GetSurface(0).WriteText(text, append);
}

Vector3D VectorToBlockSpace(Vector3D vector, IMyFunctionalBlock block) {
	return Vector3D.TransformNormal(vector - block.GetPosition(), MatrixD.Transpose(block.WorldMatrix));
}

List<IMyTerminalBlock> GetBlocksInSameGroupAs(IMyTerminalBlock block) {
	List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
	GridTerminalSystem.GetBlockGroups(groups);
	foreach(IMyBlockGroup group in groups) {
		List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
		group.GetBlocks(blocks);
		if (blocks.Contains(block)) return blocks;
	}
	return null;
}