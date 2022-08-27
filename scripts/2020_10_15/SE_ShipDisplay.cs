// Ship Display Script
// Version: 20.02.02.13.25
// Written by: Laihela


// PROGRAM VARIABLES

List<IMyBatteryBlock> Batteries = new List<IMyBatteryBlock>();
List<IMyInventory> Inventories = new List<IMyInventory>();
IMyTextSurface ScreenCenter;
IMyTextSurface ScreenRight;
IMyTextSurface ScreenLeft;
IMyCockpit Cockpit;


// MAIN PROGRAM

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	List<IMyTerminalBlock> groupBlocks = GetBlocksInSameGroupAs(Me);
	if (groupBlocks == null) throw new Exception("Programmable block is not in a block group");
	foreach (IMyTerminalBlock block in groupBlocks) {
		if (block.GetInventory() != null) Inventories.Add(block.GetInventory());
		if (block is IMyBatteryBlock) Batteries.Add(block as IMyBatteryBlock);
		if (block is IMyCockpit) Cockpit = block as IMyCockpit;
	}
	ScreenCenter = Cockpit.GetSurface(0);
	ScreenRight = Cockpit.GetSurface(2);
	ScreenLeft = Cockpit.GetSurface(1);
}

public void Main(string argument) {
	double currentVolume = 0.0;
	double maxVolume = 0.0;
	double icePercent = 0.0;
	double fillPercent = 0.0;
	double velocity = 0.0;
	double charge = 0.0;
	foreach (IMyBatteryBlock battery in Batteries) {
		charge += battery.CurrentStoredPower;
	}
	foreach (IMyInventory inventory in Inventories) {
		if (inventory.Owner is IMyGasGenerator) continue;
		currentVolume += (double)inventory.CurrentVolume;
		maxVolume += (double)inventory.MaxVolume;
	}
	fillPercent = currentVolume / maxVolume * 100;
	currentVolume = 0.0;
	maxVolume = 0.0;
	foreach (IMyInventory inventory in Inventories) {
		if (inventory.Owner is IMyGasGenerator == false) continue;
		currentVolume += (double)inventory.CurrentVolume;
		maxVolume += (double)inventory.MaxVolume;
	}
	icePercent = currentVolume / maxVolume * 100;
	velocity = Cockpit.GetShipSpeed();
	ScreenCenter.WriteText($"{velocity.ToString("F2")} m/s\n{charge.ToString("F2")} MWh\n{fillPercent.ToString("F1")}% ORE\n{icePercent.ToString("F1")}% ICE");
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