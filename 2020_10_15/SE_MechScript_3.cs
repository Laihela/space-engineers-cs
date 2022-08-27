// Rotor Animator Script
// Version: 20.10.03.17.50
// Written by: Laihela


// CONFIGURATION VARIABLES

const double FullAnimationSpeedVelocity = 21;
const double MaxAnimationSpeed = 0.011;
const double ThrottleVelocityBoost = 10.0;
const int MinRotorCorrectionRate = 10;
const int MaxRotorCorrectionRate = 70;
int[,] WalkAnimMatrix = {
	// Hips
	{ +000, -045, +045, +000 },
	// Knees
	{ -045, +060, +060, -045 },
	// Feet
	{ +000, +015, +030, -030 }
};
int[,] CrouchAnimMatrix = {
	// Hips
	{ -045, +000 },
	// Knees
	{ +045, +045 },
	// Feet
	{ +000, +045 }
};


// PROGRAM VARIABLES

List<IMyMotorStator> Rotors = new List<IMyMotorStator>();
IMyShipController Controller;
double AnimationCycle = 0;


// MAIN PROGRAM

public Program() {
	Echo(""); // Clear output.
	SetupProgrammableBlockLCD();
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	List<IMyTerminalBlock> groupBlocks = GetBlocksInSameGroupAs(Me);
	if (groupBlocks == null) throw new Exception("Programmable block is not in a block group");
	foreach (IMyTerminalBlock block in groupBlocks) {
		if (block is IMyMotorStator) Rotors.Add(block as IMyMotorStator);
		else if (block is IMyShipController) Controller = block as IMyShipController;
	}
	if (Controller == null) throw new Exception("No ship controller grouped with programmable block");
}

public void Main(string argument) {
	Print($"MECH ANIMATOR {RunSymbol()}");
	double throttle = -Controller.MoveIndicator.Z;
	double crouch = -Controller.MoveIndicator.Y;
	// TODO: Use dot product instead of vector transformation
	// double velocity = (VectorToBlockSpace(Controller.GetShipVelocities().LinearVelocity + Controller.GetPosition(), Controller)).Z;
	MatrixD matrix = Controller.WorldMatrix;
	double velocity = -Vector3D.Dot(Controller.GetShipVelocities().LinearVelocity, matrix.Forward);
	double speedScale = Math.Abs(velocity) / FullAnimationSpeedVelocity;
	double correctionRate = Lerp(MinRotorCorrectionRate, MaxRotorCorrectionRate, speedScale);
	velocity -= throttle * ThrottleVelocityBoost;
	double AnimationSpeed =  Clamp(-velocity / FullAnimationSpeedVelocity, -1, 1) *  MaxAnimationSpeed;
	AnimationCycle = Mod(AnimationCycle + AnimationSpeed, 1);
	foreach (IMyMotorStator rotor in Rotors) {
		double? targetAngle = null;
		if (crouch > 0)
			targetAngle = AnimationLookup(CrouchAnimMatrix, 0, rotor);
		else {
			targetAngle = AnimationLookup(WalkAnimMatrix, AnimationCycle, rotor);
			if (targetAngle != null)
				targetAngle *= Clamp(Math.Abs(velocity) - 2, 0, 1);
		}
		if (targetAngle == null) continue;
		AimRotor(rotor, correctionRate, targetAngle);
	}
}


// HELPER FUNCTIONS

double? AnimationLookup(int[,] animMatrix, double time, IMyMotorStator rotor) {
	int animationIndex = int.Parse(rotor.CustomName.Split('(')[1].Split(')')[0]) - 1;
	if (animationIndex > animMatrix.GetLength(0) - 1) return null;
	string[] data = rotor.CustomData.Split(',');
	double scale = double.Parse(data[0]);
	double offset = double.Parse(data[1]);
	double keyframe = Mod(time + offset, 1) * animMatrix.GetLength(1);
	int fromAngle = animMatrix[animationIndex, (int)Math.Floor(keyframe)];
	int toAngle = animMatrix[animationIndex, (int)Math.Floor(keyframe + 1) % animMatrix.GetLength(1)];
	double targetAngle = (fromAngle * (1 - keyframe % 1) + toAngle * (keyframe % 1)) * scale;
	targetAngle *= Math.PI / 180; // Convert from degrees to radians
	return targetAngle;
}

// Aims a rotor towards a position or an angle, depending on which parameter is provided, prefers position over angle.
// If neither an angle or a position is given, the rotor returns to center.
void AimRotor(IMyMotorStator rotor, double correctionRate, double? targetAngle = null, Vector3D? targetPosition = null) {
	double error = -rotor.Angle;
	if (targetPosition != null) {
		Vector3D localTarget = VectorToBlockSpace((Vector3D)targetPosition, rotor);
		error += Math.Atan2(-localTarget.X, localTarget.Z);
	}
	else if (targetAngle != null)
		error += (double)targetAngle;
	if (error < -Math.PI)
		error += Math.PI * 2;
	rotor.SetValue<float>("Velocity",  (float)(error * correctionRate));
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