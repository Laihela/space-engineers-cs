// Raydar Script
// Version: 20.05.28.23.14
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
// Blind tracking assumes that the target continues in a straight line with 
// last measured velocity from it's last detected position.
const double MaxBlindTrackTime = 1.0;

// How much the lock "jitters" around the center of the target in meters.
// Jittering the lock position allows tracking objects with a hole in the center.
// Decreases tracking accuracy of small targets if set too high.
const double LockJitterDistance = 2.0;

// How fast the raydar rotates while scanning for targets in revolutions per minute.
// The name of the rotor must include the text "(Yaw)", brackets included.
// Has no effect if the raydar is copying a turret.
const float ScanRotationSpeed = 10F;

// Pitch angle of the raydar dish while scanning for targets in degrees.
// Has no effect if the raydar is copying a turret.
// May not work if radar dish is attached with more than one rotor.
const float ScanPitchAngle = 0F;

 // How hard the rotors correct for aim deviation.
 // Lower values make aiming smoother, higher values make aiming more accurate. 
 // Can cause oscillation if set too high.
const int AimCorrectionRate = 32;

// Angle of the camera raycasting cone in degrees.
// Higher values increase possible detection angle at the cost of range effectiveness.
// Maximum angle is 45°.
const float ScanAngle = 7.0F;

// Number of scans performed per program loop.
// Higher values increase scanning effectiveness at the cost of range.
const int ScansPerTick = 3;


// PROGRAM VARIABLES

MyDetectedEntityInfo RemoteTarget = new MyDetectedEntityInfo();
MyDetectedEntityInfo Target = new MyDetectedEntityInfo();
List<IMyCameraBlock> Cameras = new List<IMyCameraBlock>{};
List<IMyMotorStator> Rotors = new List<IMyMotorStator>{};
IMyBroadcastListener TargetReceiver;
IMyRadioAntenna Antenna;
IMyLargeTurretBase Turret;

DateTime LastTrack = DateTime.Now;
Random Rand = new Random();
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
	Print($"\nRange: {String.Format("{0:0.00}", Cameras[CameraCounter].AvailableScanRange / 1000F)} km");
	if (TargetReceiver.HasPendingMessage) {
		RemoteTarget = Unpack(TargetReceiver.AcceptMessage().Data.ToString());
		if (Target.TimeStamp == 0) LastTrack = DateTime.Now;
	}
	if (Target.TimeStamp != 0) {
		Print($"\nTracking {Target.Name}", true);
		string data = Pack(Target);
		IGC.SendBroadcastMessage("Target", data, TransmissionDistance.TransmissionDistanceMax);
		AimRadarDish(Target.Position);
		TrackTarget(ref Target);
	}
	else if (RemoteTarget.TimeStamp != 0) {
		Print($"\nSeeking {Target.Name}", true);
		AimRadarDish(RemoteTarget.Position);
		long lastStamp = RemoteTarget.TimeStamp;
		TrackTarget(ref RemoteTarget);
		bool targetLocked = RemoteTarget.TimeStamp != lastStamp;
		if (targetLocked) Target = RemoteTarget;
	}
	else {
		Print("\nScanning", true);
		if (Turret != null) AimRadarWithTurret();
		else RotateRadarDish(ScanRotationSpeed, ScanPitchAngle);
		ScanForTargets(ScansPerTick);
	}
}


// MAIN HELPER FUNCTIONS

bool IsValidTarget(MyDetectedEntityInfo target) {
	return !(target.TimeStamp == 0 || IgnoreEntities.Contains(target.Type) || target.Relationship.IsFriendly()) || target.Name == "Shoot me pls";
}

string Pack(MyDetectedEntityInfo target) {
	Vector3D pos = target.Position;
	Vector3D vel = target.Velocity;
	return $"{pos.X}:{pos.Y}:{pos.Z}:{vel.X}:{vel.Y}:{vel.Z}";
}

MyDetectedEntityInfo Unpack(string data) {
	string[] values = data.Split(':');
	Vector3D position = new Vector3D(double.Parse(values[0]), double.Parse(values[1]), double.Parse(values[2]));
	Vector3D velocity = new Vector3D(double.Parse(values[3]), double.Parse(values[4]), double.Parse(values[5]));
	MyDetectedEntityInfo target = new MyDetectedEntityInfo(
		0, "REMOTE TARGET", MyDetectedEntityType.Unknown, null, MatrixD.Identity, velocity,
		MyRelationsBetweenPlayerAndBlock.Enemies,
		new BoundingBoxD(position - Vector3D.One, position + Vector3D.One), 1
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

void TrackTarget(ref MyDetectedEntityInfo target) {
	IMyCameraBlock camera = Cameras[CameraCounter];
	double timeFromLastTrack = (DateTime.Now - LastTrack).Ticks / 1e7;
	if (timeFromLastTrack > MaxBlindTrackTime) target = new MyDetectedEntityInfo();
	Vector3D newTargetPosition = VectorToBlockSpace(target.Position + target.Velocity * (float)timeFromLastTrack, camera);
	double syncDelay = newTargetPosition.Length() / 2000.0 / Cameras.Count;
	if (camera.AvailableScanRange < newTargetPosition.Length() || (Target.TimeStamp != 0 && timeFromLastTrack < syncDelay)) return;
	CameraCounter = (CameraCounter + 1) % Cameras.Count;
	Vector3D jitter = new Vector3D(Rand.NextDouble() - 0.5, Rand.NextDouble() - 0.5, Rand.NextDouble() - 0.5) * 2.0 * LockJitterDistance ;
	MyDetectedEntityInfo newTarget = camera.Raycast(camera.AvailableScanRange, newTargetPosition + jitter);
	if (IsValidTarget(newTarget)) {
		LastTrack = DateTime.Now;
		target = newTarget;
	}
}

void ScanForTargets(int scans) {
	for (int s = 0; s < scans; s++) {
		IMyCameraBlock camera = Cameras[CameraCounter];
		CameraCounter = (CameraCounter + 1) % Cameras.Count;
		float pitch = ((float)Rand.NextDouble() * 2 - 1) * ScanAngle;
		float yaw = ((float)Rand.NextDouble() * 2 - 1) * ScanAngle;
		Target = camera.Raycast(camera.AvailableScanRange, pitch, yaw);
		if (IsValidTarget(Target)) break;
	}
	if (IsValidTarget(Target) == false) Target = new MyDetectedEntityInfo();
	else LastTrack = DateTime.Now;
}


// GENERIC HELPER FUNCTIONS

void Print(string text, bool append = false) {
	Me.GetSurface(0).WriteText(text, append);
	if (Antenna != null) {
		if (!append) Antenna.HudText = $"{text}";
		else Antenna.HudText = Antenna.HudText + $"{text}";
	}
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
