// Rotor Turret Script
// Version: 20.01.18.08.42
// Written by: Laihela


// CONFIGURATION VARIABLES

const int AimSpeed = 256;
const double MaxDeviation = 0.01D;


// PROGRAM VARIABLES

List<IMyTerminalBlock> GroupBlocks;
List<IMyUserControllableGun> Weapons = new List<IMyUserControllableGun>{};
List<IMyMotorStator> Rotors = new List<IMyMotorStator>{};
IMyCameraBlock Camera;
IMySensorBlock Sensor;
IMyInteriorLight Light;


// MAIN FUNCTIONS

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	GroupBlocks = GetBlocksInSameGroupAs(Me);
	if (GroupBlocks == null) throw new Exception("Programmable block is not in a block group");
	foreach (IMyTerminalBlock block in GroupBlocks) {
		if (block is IMyUserControllableGun) Weapons.Add(block as IMyUserControllableGun);
		else if (block is IMyMotorStator) Rotors.Add(block as IMyMotorStator);
		else if (block is IMyCameraBlock) Camera = block as IMyCameraBlock;
		else if (block is IMySensorBlock) Sensor = block as IMySensorBlock;
		else if (block is IMyInteriorLight) Light = block as IMyInteriorLight;
	}
	if (Camera != null) Camera.EnableRaycast = true;
}

public void Main(string argument) {
	bool obstructed = false;
	if (Light != null) Light.ApplyAction("OnOff_Off");
	MyDetectedEntityInfo target = new MyDetectedEntityInfo();
	if (Sensor.Enabled) target = Sensor.LastDetectedEntity;
	if (Camera != null) {
		Echo($"Range: {Camera.AvailableScanRange}");
		Vector3D cameraTargetDirection = Vector3D.TransformNormal(target.Position - Camera.GetPosition(), MatrixD.Transpose(Camera.WorldMatrix));
		MyDetectedEntityInfo cameraTarget = Camera.Raycast(Camera.AvailableScanRange, cameraTargetDirection);
		if (cameraTarget.IsEmpty() || cameraTarget.EntityId != target.EntityId) obstructed = true;
		Echo($"Obstructed: {obstructed}");
	}
	foreach (IMyMotorStator rotor in Rotors) {
		float targetAngle = 0f;
		if ((target.IsEmpty() || target.Type == MyDetectedEntityType.Planet) == false) {
			Vector3D targetDirection = Vector3D.TransformNormal(target.Position - rotor.GetPosition(), MatrixD.Transpose(rotor.WorldMatrix));
			targetAngle = (float)Math.Atan2(-targetDirection.X, targetDirection.Z);
		}
		float deviation = targetAngle - rotor.Angle;
		if (deviation < -(float)Math.PI) deviation += (float)Math.PI * 2f;
		rotor.SetValue<float>("Velocity", deviation * AimSpeed);
	}
	foreach (IMyUserControllableGun weapon in Weapons) {
		if ((target.IsEmpty() || target.Type == MyDetectedEntityType.Planet) == false) {
			double deviation = Vector3D.Dot(weapon.WorldMatrix.Forward, Vector3D.Normalize(target.Position - weapon.GetPosition()));
			if (1d - deviation < MaxDeviation) {
				if (obstructed == false) weapon.SetValue<bool>("Shoot", true);
				else weapon.SetValue<bool>("Shoot", false);
				if (Light != null) Light.ApplyAction("OnOff_On");
			}
			else weapon.SetValue<bool>("Shoot", false);
		}
		else weapon.SetValue<bool>("Shoot", false);
	}
}


// HELPER FUNCTIONS

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