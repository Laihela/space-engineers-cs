// Rotor Control Script
// Version: 20.01.17.12.10
// Written by: Laihela


// VARIABLES

List<IMyTerminalBlock> GroupBlocks = new List<IMyTerminalBlock>();
IMyShipController Controller = null;
const float sensitivity = 30;


// MAIN FUNCTIONS

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	IMyBlockGroup blockGroup = FindGroupWhichIncludes(Me);
	if (blockGroup == null) {
		throw new Exception("Programmable block is not in a block group");
	}
	blockGroup.GetBlocks(GroupBlocks);
	foreach (IMyTerminalBlock block in GroupBlocks) {
		if (block.CustomName.Contains("(Controller)"))
			Controller = block as IMyShipController;
	}
}

public void Main(string argument) {
	if (Controller == null) {
		throw new Exception("Rotor controller not found");
	}
	Vector3 movement = Controller.MoveIndicator;
	Vector2 rotation = Controller.RotationIndicator;
	float roll = Controller.RollIndicator;
	foreach (IMyTerminalBlock block in Blocks) {
		IMyMotorStator rotor = block as IMyMotorStator;
		if (rotor == null) continue;
		if (rotor.CustomName.Contains("(X Axis)"))
			rotor.SetValue<float>("Velocity", movement.X * sensitivity);
		else if (rotor.CustomName.Contains("(Y Axis)"))
			rotor.SetValue<float>("Velocity", movement.Y * sensitivity);
		else if (rotor.CustomName.Contains("(Z Axis)"))
			rotor.SetValue<float>("Velocity", movement.Z * sensitivity);
		else if (rotor.CustomName.Contains("(Pitch)"))
			rotor.SetValue<float>("Velocity", rotation.X);
		else if (rotor.CustomName.Contains("(Yaw)"))
			rotor.SetValue<float>("Velocity", rotation.Y);
		else if (rotor.CustomName.Contains("(Roll)"))
			rotor.SetValue<float>("Velocity", roll * sensitivity);
	}
}


// HELPER FUNCTIONS

IMyBlockGroup FindGroupWhichIncludes(IMyTerminalBlock block) {
	List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
	GridTerminalSystem.GetBlockGroups(blockGroups);
	foreach(IMyBlockGroup blockGroup in blockGroups) {
		List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
		blockGroup.GetBlocks(blocks);
		if (blocks.Contains(block)) return blockGroup;
	}
	return null;
}