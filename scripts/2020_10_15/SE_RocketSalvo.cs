// Rocket Salvo Script
// Version: 20.10.08.21.59
// Written By: Laihela


// VARIABLES

List<IMyUserControllableGun> RocketLaunchers = new List<IMyUserControllableGun>();
int LauncherIndex = 0;
int IdleFrames = 0;


// MAIN FUNCTIONS

public Program() {
	Echo(""); // Clear output.
	SetupProgrammableBlockLCD();
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	List<IMyTerminalBlock> groupBlocks = GetBlocksInSameGroupAs(Me);
	if (groupBlocks == null) throw new Exception("Programmable block is not in a block group");
	foreach (IMyTerminalBlock block in groupBlocks) {
		if (block is IMySmallMissileLauncher) {
			RocketLaunchers.Add(block as IMyUserControllableGun);
			block.ApplyAction("OnOff_Off");
		}
	}
	if (RocketLaunchers.Count == 0) throw new Exception("No rocket launchers in block group");
}

public void Main() {
	Print($"Rocket Salvo {RunSymbol()}");
	if (IdleFrames-- > 0) return;
	if (LauncherIndex + 1 > RocketLaunchers.Count) LauncherIndex = 0;
	IMyUserControllableGun launcher = RocketLaunchers[LauncherIndex];
	if (launcher.IsFunctional == false) LauncherIndex++;
	if (launcher.Enabled == false) launcher.ApplyAction("OnOff_On");
	if (launcher.IsShooting) {
		launcher.ApplyAction("OnOff_Off");
		IdleFrames = (int)Math.Ceiling(60F / RocketLaunchers.Count());
		LauncherIndex++;
	}
}


// HELPER FUNCTIONS

// Writes text onto the programmable block's LCD.
// Optionally also writes into the hud text of an antenna if passed as a parameter.
void Print(string text = "", bool append = false, IMyRadioAntenna antenna = null) {
	Me.GetSurface(0).WriteText(text, append);
	if (antenna != null && !append) antenna.HudText = $"{text}";
	else if (antenna != null) antenna.HudText = antenna.HudText + $"{text}";
}

// Returns an animated symbol.
string RunSymbol() {
	double cycle = DateTime.Now.Ticks / 1e7 % 1 * 6.0;
	if (cycle < 1) return "\\";
	if (cycle < 2) return "|";
	if (cycle < 3) return "/";
	return "-";
}

// Returns a list of blocks that are in the same block group as the passed block.
// If multiple groups contain the block, then one will be chosen at random.
List<IMyTerminalBlock> GetBlocksInSameGroupAs(IMyTerminalBlock block) {
	List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
	GridTerminalSystem.GetBlockGroups(blockGroups);
	foreach(IMyBlockGroup blockGroup in blockGroups) {
		List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
		blockGroup.GetBlocks(blocks);
		if (blocks.Contains(block)) return blocks;
	}
	return new List<IMyTerminalBlock>{};
}

// Sets the visual style of the programmable block's LCDs.
// The keyboard LCD will show an ASCII-keyboard.
void SetupProgrammableBlockLCD() {
	Me.GetSurface(0).ContentType = ContentType.TEXT_AND_IMAGE;
	Me.GetSurface(0).Alignment = TextAlignment.LEFT;
	Me.GetSurface(0).FontColor = new Color(128, 255, 0);
	Me.GetSurface(0).Font = "Monospace";
	Me.GetSurface(0).TextPadding = 5.0F;
	Me.GetSurface(0).FontSize = 1.4F;
	Me.GetSurface(1).ContentType = ContentType.TEXT_AND_IMAGE;
	Me.GetSurface(1).Alignment = TextAlignment.CENTER;
	Me.GetSurface(1).FontColor = new Color(128, 255, 0);
	Me.GetSurface(1).Font = "Monospace";
	Me.GetSurface(1).TextPadding = 10.0F;
	Me.GetSurface(1).FontSize = 1.6F;
	Me.GetSurface(1).WriteText(string.Join("\n",
		"[!][?][$][%][*]   [ENTR] [RTRN]",
		"",
		"[A][B][C][D][E][F][G] [1][2][3]",
		"[H][I][J][K][L][M][N] [4][5][6]",
		"[O][P][Q][R][S][T][U] [7][8][9]",
		"[V][W][X][Y][Z][,][.] [ ][+][-]"
	));
}