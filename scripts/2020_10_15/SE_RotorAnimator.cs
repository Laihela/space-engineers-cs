// Written by: Laihela


#region PRIVATE_VARIABLES

const float V = 30;
const float R = 16;
const float PI = (float)(Math.PI);
List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
int[,] AnimationMatrix = {
	{ 0, 0, 0, 0, 0 },
	{ 0, -10, 0, 0, 0 },
	{ 0, -5, 0, 0, 0 }
};

#endregion


#region PUBLIC_FUNCTIONS

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	IMyBlockGroup blockGroup = FindGroupWhichIncludes(Me);
	if (blockGroup == null) {
		throw new Exception("Programmable block is not in a block group");
	}
	blockGroup.GetBlocks(Blocks);
}

public void Main(string argument) {
	foreach (IMyTerminalBlock block in Blocks) {
		IMyMotorStator rotor = block as IMyMotorStator;
		if (rotor == null) {
			continue;
		}
		if (argument == "") {
			float targetAngle = float.Parse(rotor.CustomData) / 180F * PI;
			float deviation = targetAngle - rotor.Angle;
			if (deviation < -PI) {
				deviation += PI * 2f;
			}
			float targetVelocity = Clamp(deviation * R, V);
			rotor.SetValue<float>("Velocity", targetVelocity);
			continue;
		}
		int frame = int.Parse(argument) - 1;
		if (rotor.CustomName.Contains("(1)")) {
			rotor.CustomData = AnimationMatrix[0, frame].ToString();
		}
		else if (rotor.CustomName.Contains("(2)")) {
			rotor.CustomData = AnimationMatrix[1, frame].ToString();
		}
		else if (rotor.CustomName.Contains("(3)")) {
			rotor.CustomData = AnimationMatrix[2, frame].ToString();
		}
		else if (rotor.CustomName.Contains("(4)")) {
			rotor.CustomData = AnimationMatrix[3, frame].ToString();
		}
	}
}

#endregion


#region PRIVATE_FUNCTIONS

float Clamp(float value, float minmax) {
	return value < -minmax ? -minmax : (value > minmax ? minmax : value);
}
float Clamp(float value, float min, float max) {
	return value < min ? min : (value > max ? max : value);
}

IMyTerminalBlock Find(string name) {
	return GridTerminalSystem.GetBlockWithName(name);
}

List<IMyTerminalBlock> FindAll(string name) {
	List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
	GridTerminalSystem.SearchBlocksOfName(name, blocks);
	return blocks;
}

IMyBlockGroup FindGroupWhichIncludes(IMyTerminalBlock block) {
	List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
	GridTerminalSystem.GetBlockGroups(blockGroups);
	foreach(IMyBlockGroup blockGroup in blockGroups) {
		List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
		blockGroup.GetBlocks(blocks);
		if (blocks.Contains(block)) {
			return blockGroup;
		}
	}
	return null;
}

#endregion