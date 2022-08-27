// Written by: Laihela


// GLOBAL VARIABLES

List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
float AnimationSpeed = 0.0005F;
float AnimationCycle = 0;
int[,] Animation = {
	// Left leg
	{ 0, -45, -60, -90 },
	{ -60, 0, 15, -120 },
	{ 0, -30, -45, 0 },
	// Right leg
	{ 0, 45, 60, 90 },
	{ 60, 0, -15, 120 },
	{ 0, 30, 45, 0 },
	// Left arm
	{ 0, 0, 0, 0 },
	{ 0, 0, 0, 0 },
	// Right arm
	{ 0, 0, 0, 0 },
	{ 0, 0, 0, 0 }
};


// MAIN FUNCTIONS

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	IMyBlockGroup blockGroup = FindGroupWhichIncludes(Me);
	if (blockGroup == null) {
		GridTerminalSystem.GetBlocks(Blocks);
	}
	else {
		blockGroup.GetBlocks(Blocks);
	}
}

public void Main(string argument) {
	foreach (IMyTerminalBlock block in Blocks) {
		IMyMotorStator rotor = block as IMyMotorStator;
		if (rotor == null) {
			continue;
		}
		for (int i = 0; i < Animation.GetLength(0); i++) {
			int frames = Animation.GetLength(1);
			if (frames == 0) {
				continue;
			}
			if (rotor.CustomName.Contains("(" + (i + 1) + ")")) {
				float offset = 0;
				if (rotor.CustomData != "") {
					offset = float.Parse(rotor.CustomData);
				}
				float target = 0;
				if (frames == 1) {
					target = Animation[i, 0];
				}
				else {
					int last = Animation[i, (int)Math.Floor((AnimationCycle + offset) % 1 * frames)];
					int next = Animation[i, (int)Math.Floor((AnimationCycle + offset) % 1 * frames + 1) % frames];
					target = last * (1 - (AnimationCycle + offset) % 1 * frames % 1) + next * ((AnimationCycle % 1 + offset) * frames % 1);
					Echo(last.ToString() + " " + next.ToString() + " " + target.ToString());
				}
				float deviation = target - rotor.Angle / (float)Math.PI * 180;
				if (deviation < -180) {
					deviation += 360;
				}
				rotor.SetValue<float>("Velocity", deviation);
			}
		}
		AnimationCycle = (AnimationCycle + AnimationSpeed) % 1;
	}
}


// HELPER FUNCTIONS

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
