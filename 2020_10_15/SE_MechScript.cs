// Written by: Laihela


#region PRIVATE_VARIABLES

int Frame = 0;
int Frequency = 20;
const float V = 20;
const float R = 24;
const float PI = (float)(Math.PI);
List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
int[,] AnimationMatrix = {
	{ 0, 15, -120, -60 },
	{ -30, 0, -40,  0 },
	{ 10, 30, -15, 0 },
	{ 120, 60, 0, -15 },
	{ 40, 0, 30, 0 },
	{ 15, 10, -10, -30 }
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
		float targetAngle = float.Parse(rotor.CustomData) / 180F * PI;
		float deviation = targetAngle - rotor.Angle;
		if (deviation < -PI) {
			deviation += PI * 2f;
		}
		float targetVelocity = Clamp(deviation * R, V);
		rotor.SetValue<float>("Velocity", targetVelocity);

		if (Frame % Frequency == 0) {
			int frames = AnimationMatrix.GetLength(1);
			if (rotor.CustomName.Contains("(1)")) {
				rotor.CustomData = AnimationMatrix[0, (Frame / Frequency) % frames].ToString();
			}
			else if (rotor.CustomName.Contains("(2)")) {
				rotor.CustomData = AnimationMatrix[1, (Frame / Frequency) % frames].ToString();
			}
			else if (rotor.CustomName.Contains("(3)")) {
				rotor.CustomData = AnimationMatrix[2, (Frame / Frequency) % frames].ToString();
			}
			else if (rotor.CustomName.Contains("(4)")) {
				rotor.CustomData = AnimationMatrix[3, (Frame / Frequency) % frames].ToString();
			}
			else if (rotor.CustomName.Contains("(5)")) {
				rotor.CustomData = AnimationMatrix[4, (Frame / Frequency) % frames].ToString();
			}
			else if (rotor.CustomName.Contains("(6)")) {
				rotor.CustomData = AnimationMatrix[5, (Frame / Frequency) % frames].ToString();
			}
		}
	}
	Frame++;
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
