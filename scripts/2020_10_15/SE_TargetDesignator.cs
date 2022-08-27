// Target Designator Script
// Version: 20.05.30.20.32
// Written by: Laihela


// CONFIGURATION VARIABLES

// The designator will ignore all target types in this list.
List<MyDetectedEntityType> IgnoreEntities = new List<MyDetectedEntityType>{
	MyDetectedEntityType.Planet,
	MyDetectedEntityType.Asteroid,
	MyDetectedEntityType.FloatingObject,
};

// If not zero, automatically scan for targets from this range
const double AutoScanRange = 5000.0;

// How long the designator will attempt to track a lost target before giving up.
// Blind tracking assumes that the target continues in a straight line with 
// last measured velocity from it's last detected position.
const double MaxBlindTrackTime = 1.0;


// PROGRAM VARIABLES

List<IMyCameraBlock> Cameras = new List<IMyCameraBlock>{};
List<IMyMotorStator> Rotors = new List<IMyMotorStator>{};
IMyTextSurface Display = null;
MyDetectedEntityInfo Target = new MyDetectedEntityInfo();
DateTime LastScan = DateTime.Now;
int CameraCounter = 0;
int BlinkTimer = 0;


// MAIN PROGRAM

public Program() {
	Echo(""); // Clear output.
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	List<IMyTerminalBlock> blocks = GetBlocksInSameGroupAs(Me);
	if (blocks == null) throw new Exception("Programmable block is not in a block group");
	foreach (IMyTerminalBlock block in blocks) {
		if (block is IMyCameraBlock) Cameras.Add(block as IMyCameraBlock);
		if (block is IMyMotorStator) Rotors.Add(block as IMyMotorStator);
		if (block is IMyCockpit) Display = (block as IMyCockpit).GetSurface(0);
	}
	if (Cameras.Count == 0) throw new Exception("No cameras grouped with programmable block");
	foreach (IMyCameraBlock camera in Cameras) camera.EnableRaycast = true;
}

public void Main(string argument) {
	double timeFromLastScan = (DateTime.Now - LastScan).Ticks / 1e7;
	if (argument == "DesignateTarget") DesignateTarget();
	else if (argument == "ClearTarget") Target = new MyDetectedEntityInfo();
	else if (Target.TimeStamp != 0 && timeFromLastScan < MaxBlindTrackTime) {
		double distanceToTarget = (Target.Position - Cameras[CameraCounter].GetPosition()).Length();
		string range = string.Format("{0:0.00}", distanceToTarget / 1e3);
		if (BlinkTimer > 9) Print($"LOCK\n{range} km\n{Target.Name}");
		else Print($"\n{range} km\n{Target.Name}");
		TrackTarget();
		AimRotors(Target.Position, Rotors, 64.0);
		BroadcastTargetInfo();
	}
	else {
		AimRotors(null, Rotors, 64.0);
		if (AutoScanRange != 0.0) {
			double delay = timeFromLastScan * Cameras.Count;
			string frequency = string.Format("{0:0.00}", 2e3 * Cameras.Count / AutoScanRange);
			string range = string.Format("{0:0.00}", AutoScanRange / 1e3);
			Print($"RDAR\n{range} km\n{frequency} Hz");
			if (delay * 2e3 > AutoScanRange) DesignateTarget();
		}
		else Print($"RDAR\nMANUAL\n{GetRangeText()} km");
	}
	int brokenCameras = 0;
	foreach (IMyCameraBlock camera in Cameras) {
		if (camera.IsWorking == false) brokenCameras++;
	}
	if (brokenCameras != 0 && BlinkTimer > 9) Print($"\nDMG {brokenCameras * 100 / Cameras.Count}%", true);
	BlinkTimer = (BlinkTimer + 1) % 20;
}


// MAIN HELPER FUNCTIONS

bool IsValidTarget(MyDetectedEntityInfo target) {
	return !(target.TimeStamp == 0 || IgnoreEntities.Contains(target.Type) || target.Relationship.IsFriendly()) || target.Name == "Shoot me pls";
}

void BroadcastTargetInfo() {
	Vector3D pos = Target.Position;
	Vector3D vel = Target.Velocity;
	string data = $"{pos.X}:{pos.Y}:{pos.Z}:{vel.X}:{vel.Y}:{vel.Z}";
	IGC.SendBroadcastMessage("Designator", data, TransmissionDistance.TransmissionDistanceMax);
}

string GetRangeText() {
	IMyCameraBlock camera = null;
	foreach (IMyCameraBlock c in Cameras) {
		if (camera == null || c.AvailableScanRange > camera.AvailableScanRange) camera = c;
	}
	return string.Format("{0:000.0}",  Clamp(camera.AvailableScanRange / 1000D, 0, 999.9));
}

void AimRotors(Vector3D? target, List<IMyMotorStator> rotors, double aggressiveness) {
	foreach (IMyMotorStator rotor in rotors) {
		float targetAngle = 0F;
		if (target != null) {
			Vector3D localTarget = VectorToBlockSpace((Vector3D)target, rotor);
			targetAngle = (float)Math.Atan2(-localTarget.X, localTarget.Z);
		}
		float deviation = targetAngle - rotor.Angle;
		if (deviation < -(float)Math.PI) deviation += (float)Math.PI * 2f;
		rotor.SetValue<float>("Velocity", deviation * (float)aggressiveness);
	}
}

void DesignateTarget() {
	IMyCameraBlock camera = null;
	foreach (IMyCameraBlock c in Cameras) {
		if (camera == null || c.AvailableScanRange > camera.AvailableScanRange) camera = c;
	}
	if (camera.IsWorking == false) return;
	MyDetectedEntityInfo newTarget = camera.Raycast(camera.AvailableScanRange, 0, 0);
	if (IsValidTarget(newTarget)) Target = newTarget;
	else Target = new MyDetectedEntityInfo();
	LastScan = DateTime.Now;
}

void TrackTarget() {
	double timeFromScan = (DateTime.Now - LastScan).Ticks / 10e6F;
	IMyCameraBlock camera = Cameras[CameraCounter];
	if (camera.IsWorking == false) return;
	Vector3D newTargetPosition = VectorToBlockSpace(Target.Position + Target.Velocity * (float)timeFromScan, camera);
	double syncDelay = newTargetPosition.Length() / 2e3 / Cameras.Count;
	if (camera.AvailableScanRange > newTargetPosition.Length() && timeFromScan > syncDelay) {
		CameraCounter = (CameraCounter + 1) % Cameras.Count;
		MyDetectedEntityInfo newTarget = camera.Raycast(camera.AvailableScanRange, newTargetPosition);
		if (IsValidTarget(newTarget)) {
			Target = newTarget;
			LastScan = DateTime.Now;
		}
		else Target = new MyDetectedEntityInfo();
	}
}


// GENERIC HELPER FUNCTIONS

double Clamp(double value, double min, double max) {
	return value < min ? min : (value > max ? max : value);
}

Vector3D VectorToBlockSpace(Vector3D vector, IMyFunctionalBlock block) {
	return Vector3D.TransformNormal(vector - block.GetPosition(), MatrixD.Transpose(block.WorldMatrix));
}

void Print(string text, bool append = false) {
	if (Display != null) Display.WriteText(text, append);
	else Me.GetSurface(0).WriteText(text, append);
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
