List<IMyTerminalBlock> TerminalBlocks = new List<IMyTerminalBlock>();

public void Main(string argument) {
	GridTerminalSystem.GetBlocks(TerminalBlocks);
	for (int i=0; i<TerminalBlocks.Count; i++) {
		
		string name = "";
		
		if (TerminalBlocks[i] is IMyThrust) {name = "Thruster";}
		if (TerminalBlocks[i] is IMySmallMissileLauncher) {name = "Rocket Launcher";}
		
		if (name != "") {
			TerminalBlocks[i].CustomName = name;
		}
		
		// TerminalBlocks[i].CustomName = TerminalBlocks[i].GetType().Name;
	}
}