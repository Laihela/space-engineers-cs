// Remote Turret Script
// Version 20.10.01.20.18
// Written by: Laihela



// CONFIGURATION VARIABLES

// Projectile velocity of the weapons in meters per second.
// Used for leading trajectory calculations.
// Rocket launcher = 200, Gatling gun = 400.
const float ProjectileVelocity = 400F;

// Maximum aim error. If error is greater then hold fire.
// May prevent weapons from firing if set too low.
const double MaxDeviation = 0.001D;

 // How hard the rotors correct for aim deviation.
 // Low values give smoother aiming, while higher values are more accurate. 
 // Can cause oscillation if set too high.
const int AimCorrectionRate = 64;

// Maximum weapons range.
// Projectiles seem to despawn at around 800 meters.
const int MaxRange = 800;

// Higher values increase accuracy at the cost of performance, but have diminishing returns.
const int TrajectoryCalculationsPerTick = 16;



// PROGRAM VARIABLES

class FriendlyFireZone {
	public long OwnerId;
	public double Radius;
	public Vector3D Position;
	public Vector3D Velocity;
}

List<IMyUserControllableGun> Weapons = new List<IMyUserControllableGun>{};
List<IMyCameraBlock> Cameras = new List<IMyCameraBlock>{};
List<IMyMotorStator> Rotors = new List<IMyMotorStator>{};
IMyBroadcastListener TargetReceiver;
IMyTextSurface DebugDisplay;

List<FriendlyFireZone> FriendlyFireZones = new List<FriendlyFireZone>();
Vector3D PreviousTargetVelocity = new Vector3D();
int Counter = 0;



// MAIN PROGRAM

public Program() {
	Echo(""); // Clear output.
	DebugDisplay = Me.GetSurface(0);
	SetupProgrammableBlockLCD();
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	List<IMyTerminalBlock> groupBlocks = GetBlocksInSameGroupAs(Me);
	if (groupBlocks == null) throw new Exception("Programmable block is not in a block group");
	foreach (IMyTerminalBlock block in groupBlocks) {
		if (block is IMyUserControllableGun) Weapons.Add(block as IMyUserControllableGun);
		if (block is IMyCameraBlock) Cameras.Add(block as IMyCameraBlock);
		if (block is IMyMotorStator) Rotors.Add(block as IMyMotorStator);
	}
	foreach (IMyCameraBlock camera in Cameras) camera.EnableRaycast = true;
	TargetReceiver = IGC.RegisterBroadcastListener("Target");
}

public void Main(string argument) {
	Clear();
	Print("Remote Turret " + RunSymbol());
	Vector3D[] targetVectors = {};
	if (TargetReceiver.HasPendingMessage) {
		 targetVectors = Unpack(TargetReceiver.AcceptMessage().Data.ToString());
	}
	AimTurret(targetVectors);
	FireWeapons(targetVectors);
	if (targetVectors.Length != 0) {
		Print("TRACKING");
		PreviousTargetVelocity = targetVectors[1];
	}
	else Print("NO TARGET");
	if (Counter < 2) SetRotors(Counter == 1); // A workaround for a bug with rotors
	Counter = (Counter + 1) % 30;
}



// HELPER FUNCTIONS

// Sets all rotors on or off
void SetRotors(bool state) {
	foreach (IMyMotorStator rotor in Rotors) {
		if (state == true) rotor.ApplyAction("OnOff_On");
		else rotor.ApplyAction("OnOff_Off");
	}
}

// Expected data format = PosX : PosY : PosZ : VelX : VelY : VelZ
Vector3D[] Unpack(string data) {
	string[] values = data.Split(':');
	Vector3D[] unpackedData = {
		new Vector3D(double.Parse(values[0]), double.Parse(values[1]), double.Parse(values[2])),
		new Vector3D(double.Parse(values[3]), double.Parse(values[4]), double.Parse(values[5]))
	};
	return unpackedData;
}

// Fires all weapons that are pointing in the correct direction
void FireWeapons(Vector3D[] targetVectors) {
	if (targetVectors.Length != 2) return;
	Vector3D targetPosition = targetVectors[0];
	Vector3D targetVelocity = targetVectors[1];
	Vector3D targetAcceleration = (targetVelocity - PreviousTargetVelocity) * 60;
	foreach (IMyUserControllableGun weapon in Weapons) {
		Vector3D lead = CalculateLead(weapon.GetPosition(), Vector3D.Zero, targetPosition, targetVelocity, Vector3D.Zero);
		Vector3D aimPosition = targetPosition + lead - weapon.GetPosition();
		double deviation = Vector3D.Dot(weapon.WorldMatrix.Forward, Vector3D.Normalize(aimPosition));
		if (1D - deviation < MaxDeviation && aimPosition.Length() < MaxRange) {
			foreach (FriendlyFireZone zone in FriendlyFireZones) {
				Vector3D zoneLead = CalculateLead(weapon.GetPosition(), Vector3D.Zero, zone.Position, zone.Velocity, Vector3D.Zero);
				Print("FIX ME PLZ");
			}
			weapon.ApplyAction("ShootOnce");
		}
	}
}

Vector3D CalculateLead(Vector3D position, Vector3D velocity, Vector3D targetPosition, Vector3D targetVelocity, Vector3D targetAcceleration) {
	double time = 0;
	Vector3D lead = Vector3D.Zero;
	for (int i = 0; i < TrajectoryCalculationsPerTick; i++) {
		time = (targetPosition + lead - position).Length() / ProjectileVelocity;
		lead = targetVelocity * time + 0.5 * targetAcceleration * Math.Pow(time, 2);
	}
	return lead;
}

// Points all rotors in the target direction, leads target based on projectile velocity
void AimTurret(Vector3D[] targetVectors) {
	if (targetVectors.Length != 2) {
		foreach (IMyMotorStator rotor in Rotors) {
			float deviation = -rotor.Angle;
			if (deviation < -(float)Math.PI) deviation += (float)Math.PI * 2f;
			rotor.SetValue<float>("Velocity", deviation * AimCorrectionRate);
		}
		return;
	}
	Vector3D targetPosition = targetVectors[0];
	Vector3D targetVelocity = targetVectors[1];
	Vector3D targetAcceleration = (targetVelocity - PreviousTargetVelocity) * 60;
	foreach (IMyMotorStator rotor in Rotors) {
		double time = (targetPosition - rotor.GetPosition()).Length() / ProjectileVelocity;
		Vector3D lead = targetVelocity * time + 0.5 * targetAcceleration * Math.Pow(time, 2);
		for (int i = 1; i < TrajectoryCalculationsPerTick; i++) {
			time = (targetPosition + lead - rotor.GetPosition()).Length() / ProjectileVelocity;
			lead = targetVelocity * time + 0.5 * targetAcceleration * Math.Pow(time, 2);
		}
		Vector3D localPosition = VectorToBlockSpace(targetPosition + lead, rotor);
		Vector3D localVelocity = VectorToBlockSpace(targetVelocity + rotor.GetPosition(), rotor);
		Vector3D localAcceleration = VectorToBlockSpace(targetAcceleration + rotor.GetPosition(), rotor);
		double distance = localPosition.Length();
		float targetAngle = (float)Math.Atan2(-localPosition.X, localPosition.Z);
		float targetAngularVelocity = (float)Vector3D.Cross((localVelocity + localAcceleration) / distance, localPosition / distance).Y * (float)(4.0 * Math.PI);
		float deviation = targetAngle - rotor.Angle;
		if (rotor.CustomData != "") deviation += float.Parse(rotor.CustomData) / 180F * (float)Math.PI;
		if (rotor.GetValue<float>("UpperLimit") > 360F && rotor.GetValue<float>("LowerLimit") < -360F) {
			if (deviation < -(float)Math.PI) deviation += (float)Math.PI * 2f;
		}
		rotor.SetValue<float>("Velocity", deviation * AimCorrectionRate + targetAngularVelocity);
	}
}

// Transfroms a vector from world coordinates to local block coordinates.
Vector3D VectorToBlockSpace(Vector3D vector, IMyCubeBlock block) {
	return Vector3D.TransformNormal(vector - block.GetPosition(), MatrixD.Transpose(block.WorldMatrix));
}

// Writes text onto the DebugDisplay.
void Print(string text = "") {
	DebugDisplay.WriteText(text + "\n", true);
}

// Clears the DebugDisplay.
void Clear() {
	DebugDisplay.WriteText("");
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