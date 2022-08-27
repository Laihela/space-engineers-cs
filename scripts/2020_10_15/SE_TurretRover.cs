// AI Rover Script
// Version: 20.06.01.01.04
// Written by: Laihela



// CONFIGURATION VARIABLES

// How hard the rover steers towards it's target.
const double SteerMultiplier = 2.0;

// How far away the rover stops from it's target in meters.
const double StopDistance = 30.0;



// PROGRAM VARIABLES

List<IMyMotorSuspension> Wheels = new List<IMyMotorSuspension>{};
List<IMyLargeTurretBase> Turrets = new List<IMyLargeTurretBase>{};
List<IMyLightingBlock> Lights = new List<IMyLightingBlock>{};
List<IMySensorBlock> Sensors = new List<IMySensorBlock>{};
IMyRadioAntenna Antenna = null;
Vector3D LastPosition = Vector3D.Zero;
Vector3D Velocity = Vector3D.Zero;
DateTime LastTime = new DateTime();



// MAIN PROGRAM

public Program() {
	Echo(""); // Clear output.
	SetupProgrammableBlockLCD();
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	foreach (IMyTerminalBlock block in GetBlocksInSameGroupAs(Me)) {
		if (block is IMyMotorSuspension) Wheels.Add(block as IMyMotorSuspension);
		if (block is IMyLargeTurretBase) Turrets.Add(block as IMyLargeTurretBase);
		if (block is IMyLightingBlock) Lights.Add(block as IMyLightingBlock);
		if (block is IMySensorBlock) Sensors.Add(block as IMySensorBlock);
		if (block is IMyRadioAntenna) Antenna = block as IMyRadioAntenna;
	}
	if (Sensors.Count == 0 && Turrets.Count == 0) throw new Exception("No turrets or sensors grouped with programmable block!");
	if (Wheels.Count == 0) throw new Exception("No wheel suspension blocks grouped with programmable block!");
}

public void Main(string argument) {
	Print($"AI Rover Script {RunSymbol()}");
	Print("", false, Antenna);
	MyDetectedEntityInfo target = GetTarget();
	if (target.TimeStamp != 0) {
		Print($"\nTRACKING {target.Name}", true, Antenna);
		SetLights(true);
		double distanceToTarget = Vector3D.Distance(Me.GetPosition(), target.Position);
		foreach (IMyMotorSuspension wheel in Wheels) {
			Vector3D wheelDirection = VectorToWorldSpace(Vector3D.Up, wheel) - wheel.GetPosition();
			Vector3D forwardDirection = VectorToWorldSpace(Vector3D.Up, Me) - Me.GetPosition();
			Vector3D rightDirection = VectorToWorldSpace(Vector3D.Left, Me) - Me.GetPosition();
			Vector3D targetDirection = Vector3D.Normalize(target.Position - Me.GetPosition());
			Vector3D wheelPosition = wheel.GetPosition() - Me.GetPosition();
			double forwardVelocity = Vector3D.Dot(forwardDirection, Velocity);
			// 1 or -1, depending on which side of the vehicle the wheel is attached to
			int sideScale = Vector3D.Dot(rightDirection, wheelDirection) > 0 ? 1 : -1;
			// 1 or -1, depending on if the wheel is attached to the front or back
			int frontScale = Vector3D.Dot(forwardDirection, wheelPosition) > 0 ? 1 : -1;
			double steering = Vector3D.Dot(rightDirection, targetDirection) * SteerMultiplier;
			double throttle = Clamp(distanceToTarget - StopDistance, 0, 1);
			if (throttle > 0.01) wheel.SetValue<float>("Propulsion override", (float)throttle * sideScale);
			else wheel.SetValue<float>("Propulsion override", -(float)forwardVelocity * sideScale);
			wheel.SetValue<float>("Steer override", -(float)steering * frontScale);
		}
	}
	else {
		Print("\nIDLE", true, Antenna);
		SetLights(false);
		foreach (IMyMotorSuspension wheel in Wheels) {
			wheel.SetValue<float>("Propulsion override", 0.0F);
			wheel.SetValue<float>("Steer override", 0.0F);
		}
	}
	DateTime now = DateTime.Now;
	Velocity = (Me.GetPosition() - LastPosition) / ((now - LastTime).Ticks / 1e7);
	LastPosition = Me.GetPosition();
	LastTime = now;
}



// HELPER FUNCTIONS

// Set all lights on or off
void SetLights(bool state) {
	foreach (IMyLightingBlock light in Lights) {
		light.Enabled = state;
	}
}

// Checks if sensors or turrets are detecting a target and returns it if found.
MyDetectedEntityInfo GetTarget() {
	foreach (IMyLargeTurretBase turret in Turrets) {
		MyDetectedEntityInfo target = turret.GetTargetedEntity();
		if (target.TimeStamp != 0) return target;
	}
	foreach (IMySensorBlock sensor in Sensors) {
		MyDetectedEntityInfo target = sensor.LastDetectedEntity;
		if (target.TimeStamp != 0) return target;
	}
	return new MyDetectedEntityInfo();
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