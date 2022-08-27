// Template Script
// Version: 20.10.10.15.13
// Written by: Laihela



// CONFIGURATION VARIABLES

/// const int Value = 1;



// PROGRAM VARIABLES

IMyTextSurface DebugDisplay;
/// List<BlockType> BlockList = new List<BlockType>();
/// BlockType block;



// MAIN PROGRAM

Program() {
	Echo(""); // Clear output.
	DebugDisplay = Me.GetSurface(0);
	SetupProgrammableBlockLCD();
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	foreach (IMyTerminalBlock block in GetBlocksInSameGroupAs(Me)) {
		/// if (block is BlockType) BlockList.Add(block as BlockType);
	}
	/// if (block == null) throw new Exception("No block grouped with programmable block");
}

public void Main(string argument) {
	Clear();
	Print($"TEMPLATE SCRIPT {RunSymbol()}");
	/// foreach (BlockType block in BlockList)
}



// HELPER FUNCTIONS

// Transfroms a vector from local block coordinates to world coordinates.
Vector3D VectorToWorldSpace(Vector3D vector, IMyCubeBlock block) {
	return Vector3D.Transform(vector, block.WorldMatrix);
}

// Transfroms a vector from world coordinates to local block coordinates.
Vector3D VectorToBlockSpace(Vector3D vector, IMyCubeBlock block) {
	return Vector3D.TransformNormal(vector - block.GetPosition(), MatrixD.Transpose(block.WorldMatrix));
}

// Ensure that a number stays between a minimum and a maximum value.
double Clamp(double value, double min, double max) {
	return value < min ? min : (value > max ? max : value);
}

// Returns a value interpolated from a to b by time.
double Lerp(double a, double b, double time) {
	return a * (1 - time) + b * time;
}

// Returns the modulus of a number.
// Result is always positive, works consistently with negative input values.
double Mod(double v, double m) {
	v = v % m;
	return v < 0 ? v + m: v;
}

// Writes text onto the DebugDisplay.
void Print(string text = "") {
	DebugDisplay.WriteText(text + "\n", true);
}

// Clears the DebugDisplay.
void Clear() {
	DebugDisplay.WriteText("");
}

// Returns current time in seconds.
static double Now() {
	return DateTime.Now.Ticks / 1e7;
}

// Returns an animated symbol.
string RunSymbol() {
	double cycle = Now() % (2.0 / 3.0) * 6.0;
	if (cycle < 1) return "\\";
	if (cycle < 2) return "|";
	if (cycle < 3) return "/";
	return "-";
}

// Rotaters all rotors in the list so that they point at the target location.
// If no target position is given, the rotors will return to center instead.
void AimRotors(List<IMyMotorStator> rotors, Vector3D? target = null, float rate = 32F) {
	foreach (IMyMotorStator rotor in rotors) {
		float targetAngle = 0F;
		if (target != null) {
			Vector3D localTarget = VectorToBlockSpace((Vector3D)target, rotor);
			targetAngle = (float)Math.Atan2(-localTarget.X, localTarget.Z);
		}
		float deviation = targetAngle - rotor.Angle;
		if (deviation < -(float)Math.PI) deviation += (float)Math.PI * 2f;
		rotor.SetValue<float>("Velocity", deviation * rate);
	}
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