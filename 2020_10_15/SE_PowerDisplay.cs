// Power Display Script
// Version: 20.02.03.03.22
// Written by: Laihela


// CONFIGURATION VARIABLES

/// const int Value = 1;


// PROGRAM VARIABLES

String[] Symbol = {"(|)", "(/)", "(-)", "(\\)"};
List<IMyPowerProducer> Generators = new List<IMyPowerProducer>();
List<IMyBatteryBlock> Batteries = new List<IMyBatteryBlock>();
IMyTextSurface Display;
int Counter = 0;


// MAIN PROGRAM

public Program() {
	Echo("");
	Runtime.UpdateFrequency = UpdateFrequency.Update100;
	List<IMyTerminalBlock> groupBlocks = GetBlocksInSameGroupAs(Me);
	if (groupBlocks == null) throw new Exception("Programmable block is not in a block group");
	foreach (IMyTerminalBlock block in groupBlocks) {
		if (block is IMyPowerProducer) Generators.Add(block as IMyPowerProducer);
		if (block is IMyBatteryBlock) Batteries.Add(block as IMyBatteryBlock);
		if (block is IMyTextSurface) Display = block as IMyTextSurface;
	}
}

public void Main(string argument) {
	double currentStorage = 0.0;
	double currentOutput = 0.0;
	double maxStorage = 0.0;
	double maxOutput = 0.0;
	foreach (IMyPowerProducer generator in Generators) {
		currentOutput += generator.CurrentOutput;
		maxOutput += generator.MaxOutput;
	}
	foreach (IMyBatteryBlock battery in Batteries) {
		currentStorage += battery.CurrentStoredPower;
		maxStorage += battery.MaxStoredPower;
	}
	double percentage = currentOutput / maxOutput * 100;
	Me.GetSurface(0).WriteText(
		"POWER MANAGEMENT " + Symbol[Counter++%4] + "\n\n" +
		"Current  " + String.Format("{0:000.00}", currentOutput) + " MWh" + "\n" +
		"Max      " + String.Format("{0:000.00}", maxOutput) + " MWh" + "\n" +
		"Load     " + String.Format("{0:000.00}", percentage) + " %" + "\n\n" + 
		"Stored   " + String.Format("{0:000.00}", currentStorage) + " MWh" + "\n" +
		"Capacity " + String.Format("{0:000.00}", maxStorage) + " MWh"
	);
}


// HELPER FUNCTIONS

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