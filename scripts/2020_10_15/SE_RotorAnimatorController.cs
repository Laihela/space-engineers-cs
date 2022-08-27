// Written by: Laihela


#region PRIVATE_VARIABLES

const float PI = (float)(Math.PI);
List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
int FrameCounter = 0;

#endregion


#region PUBLIC_FUNCTIONS

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update100;
	IMyBlockGroup blockGroup = FindGroupWhichIncludes(Me);
	if (blockGroup == null) {
		throw new Exception("Programmable block is not in a block group");
	}
	blockGroup.GetBlocks(Blocks);
}

public void Main(string argument) {
	foreach (IMyTerminalBlock block in Blocks) {
		IMyProgrammableBlock animator = block as IMyProgrammableBlock;
		if (animator.CustomName.Contains("(RAU)") == false) {
			continue;
		}
		animator.ApplyAction("Run", new List<TerminalActionParameter>{ FrameCounter % 10 + 1 });
	}
	FrameCounter++;
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