// Security Alarm Script
// Version: 20.02.06.19.45
// Written by: Laihela


// PROGRAM VARIABLES

string[] UpdateSymbol = {
	":.......",
	".:......",
	"..:.....",
	"...:....",
	"....:...",
	".....:..",
	"......:.",
	".......:",
	"......:.",
	".....:..",
	"....:...",
	"...:....",
	"..:.....",
	".:......",
}; //{"|", "/", "-", "\\"};

List<IMyLargeTurretBase> Turrets = new List<IMyLargeTurretBase>();
List<IMyTerminalBlock> AllBlocks = new List<IMyTerminalBlock>();
List<IMySoundBlock> Speakers = new List<IMySoundBlock>();
List<IMySensorBlock> Sensors = new List<IMySensorBlock>();
List<IMyInteriorLight> Lights = new List<IMyInteriorLight>();
IMyTextSurface Screen = null;

int UpdateCounter = 0;
bool Alarm = false;


// MAIN PROGRAM

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	List<IMyTerminalBlock> groupBlocks = GetBlocksInSameGroupAs(Me);
	if (groupBlocks == null) throw new Exception("Programmable block is not in a block group");
	foreach (IMyTerminalBlock block in groupBlocks) {
		if (block is IMyLargeTurretBase) Turrets.Add(block as IMyLargeTurretBase);
		if (block is IMySoundBlock) Speakers.Add(block as IMySoundBlock);
		if (block is IMySensorBlock) Sensors.Add(block as IMySensorBlock);
		if (block is IMyInteriorLight) Lights.Add(block as IMyInteriorLight);
	}
	GridTerminalSystem.GetBlocks(AllBlocks);
	Screen = Me.GetSurface(0);
	Echo(""); // Clears the output
}

public void Main(string argument) {
	UpdateCounter++;
	foreach (IMyTerminalBlock block in AllBlocks) {
		if (block.IsBeingHacked) {
			Alarm = true;
			Screen.WriteText("\nSECURITY SYSTEM\nHacking detected!");
			Echo("\nSECURITY SYSTEM\nHacking detected!");
		}
	}
	foreach (IMySensorBlock sensor in Sensors) {
		if (sensor.LastDetectedEntity.IsEmpty() == false) {
			Alarm = true;
			Screen.WriteText("\nSECURITY SYSTEM\nTrespassing detected!");
			Echo("\nSECURITY SYSTEM\nTrespassing detected!");
		}
	}
	if (Alarm) {
		Runtime.UpdateFrequency = UpdateFrequency.None;
		foreach (IMyLargeTurretBase turret in Turrets) {
			turret.Enabled = true;
			turret.ApplyAction("TargetCharacters_On");
			turret.ApplyAction("TargetNeutrals_On");
		}
		foreach (IMySoundBlock speaker in Speakers) {
			speaker.Enabled = true;
			speaker.Play();
		}
		foreach (IMyInteriorLight light in Lights) {
			light.Enabled = true;
			light.Color = new Color(255, 0, 0);
		}
		foreach (IMyTerminalBlock block in AllBlocks) {
			if (block.HasAction("AnyoneCanUse") && block.GetActionWithName("AnyoneCanUse").IsEnabled(block)) 
				block.ApplyAction("AnyoneCanUse");
			if (block.HasAction("Open_Off"))
				block.ApplyAction("Open_Off");
		}
	}
	else {
		string symbol = UpdateSymbol[UpdateCounter % UpdateSymbol.Length];
		Screen.WriteText("\nSECURITY SYSTEM\nMonitoring\n\n" + symbol);
		Echo("\nSECURITY SYSTEM\nMonitoring\n\n" + symbol);
	}
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