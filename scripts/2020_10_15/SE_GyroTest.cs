// Gyro Test Script
// Version: 20.05.21.23.29
// Written by: Laihela


// CONFIGURATION VARIABLES

// How agressively the gyroscopes turn towards the target.
const int AimCorrectionRate = 2;

// How agressively the gyroscopes try to cancel angular velocity.
const int VelocityDamping = 128;


// PROGRAM VARIABLES

List<IMyGyro> Gyroscopes = new List<IMyGyro>();
IMySensorBlock Sensor = null;
Vector3D PreviousDirection = new Vector3D(0, 0, -1);


// MAIN PROGRAM

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	List<IMyTerminalBlock> groupBlocks = GetBlocksInSameGroupAs(Me);
	if (groupBlocks == null) throw new Exception("Programmable block is not in a block group");
	foreach (IMyTerminalBlock block in groupBlocks) {
		if (block is IMySensorBlock) Sensor = block as IMySensorBlock;
		if (block is IMyGyro) Gyroscopes.Add(block as IMyGyro);
	}
}

public void Main(string argument) {
	MyDetectedEntityInfo target = Sensor.LastDetectedEntity;
	Vector3D forwardDirection = VectorToWorldSpace(new Vector3D(0, 0, -1), Sensor) - Sensor.GetPosition();
	Vector3D targetDirection = target.Position - Sensor.GetPosition();
	Vector3D targetRotation = Vector3D.Cross(forwardDirection, targetDirection) * AimCorrectionRate;
	Vector3D angularVelocity = Vector3D.Cross(forwardDirection, PreviousDirection) * VelocityDamping;
	foreach (IMyGyro gyroscope in Gyroscopes) {
		Vector3D localTargetRotation = VectorToBlockSpace(targetRotation + angularVelocity + gyroscope.GetPosition(), gyroscope);
		gyroscope.Pitch = -(float)localTargetRotation.X;
		gyroscope.Yaw = -(float)localTargetRotation.Y;
		gyroscope.Roll = -(float)localTargetRotation.Z;
	}
	PreviousDirection = forwardDirection;
}


// HELPER FUNCTIONS

Vector3D VectorToWorldSpace(Vector3D vector, IMyCubeBlock block) {
	return Vector3D.Transform(vector, block.WorldMatrix);
}

Vector3D VectorToBlockSpace(Vector3D vector, IMyCubeBlock block) {
	return Vector3D.TransformNormal(vector - block.GetPosition(), MatrixD.Transpose(block.WorldMatrix));
}

List<IMyTerminalBlock> GetBlocksInSameGroupAs(IMyTerminalBlock block) {
	List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
	GridTerminalSystem.GetBlockGroups(blockGroups);
	foreach(IMyBlockGroup blockGroup in blockGroups) {
		List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
		blockGroup.GetBlocks(blocks);
		if (blocks.Contains(block)) return blocks;
	}
	return null;
}