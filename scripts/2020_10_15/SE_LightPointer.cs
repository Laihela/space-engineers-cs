// Light Pointer Script
// Version: 20.05.28.20.24
// Written by: Laihela



// CONFIGURATION VARIABLES

// How fast the light turns to face the player.
const float Agressiveness = 64.0F;

// How far the light tracks the player in meters.
const double MaxDistance = 100.0;



// PROGRAM VARIABLES

List<IMyMotorStator> Rotors = new List<IMyMotorStator>{};
IMyRemoteControl Controller = null;
IMyReflectorLight Light = null;



// MAIN PROGRAM

public Program() {
	Echo(""); // Clear output.
	SetupProgrammableBlockLCD();
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	foreach (IMyTerminalBlock block in GetBlocksInSameGroupAs(Me)) {
		if (block is IMyRemoteControl) Controller = block as IMyRemoteControl;
		if (block is IMyMotorStator) Rotors.Add(block as IMyMotorStator);
		if (block is IMyReflectorLight) Light = block as IMyReflectorLight;
	}
	if (Controller == null) throw new Exception("No remote controller grouped with programmable block");
}

public void Main(string argument) {
	Print($"Light Pointer {RunSymbol()}");
	Vector3D targetPosition;
	double distance = 0.0;
	bool found = Controller.GetNearestPlayer(out targetPosition);
	if (found) distance = (Controller.GetPosition() - targetPosition).Length();
	if (found && distance < MaxDistance) {
		AimRotors(Rotors, targetPosition, Agressiveness);
		Light.ApplyAction("OnOff_On");
	}
	else {
		AimRotors(Rotors, null, Agressiveness);
		Light.ApplyAction("OnOff_Off");
	}
}



// HELPER FUNCTIONS

// Transfroms a vector from world coordinates to local block coordinates.
Vector3D VectorToBlockSpace(Vector3D vector, IMyCubeBlock block) {
	return Vector3D.TransformNormal(vector - block.GetPosition(), MatrixD.Transpose(block.WorldMatrix));
}

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

// Rotaters all rotors in the list so that they point at the target location.
// If no target vector is passed, the rotors will return to center instead.
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
	return null;
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