// Guided Missile Script
// Version: 20.05.30.22.28
// Written by: Laihela



// CONFIGURATION VARIABLES

// How agressively the gyroscopes turn towards the target.
const double AimCorrectionRate = 3.0;

// How agressively the gyroscopes tries to cancel angular velocity.
const double VelocityDamping = 3;

// How long the missile will track a lost target before giving up.
// Blind tracking assumes that the target continues in a straight line with 
// last measured velocity from it's last detected position.
const double MaxBlindTrackTime = 3.0;

// How close the missile has to get before detonating it's warhead.
// Further proximity increases chances of hitting with the cost of potential damage.
const double DetonationProximity = 10.0;

const double VelocityCorrection = 0.15;

const double TargetVelocityCorrection = 0.1;



// PROGRAM VARIABLES

List<IMyGyro> Gyroscopes = new List<IMyGyro>{};
IMyBroadcastListener TargetReceiver = null;
IMyRadioAntenna Antenna = null;
IMyWarhead Warhead = null;

MyDetectedEntityInfo Target = new MyDetectedEntityInfo();
Vector3D PreviousDirection = Vector3D.Zero;
Vector3D PreviousPosition = Vector3D.Zero;
Vector3D AngularVelocity = Vector3D.Zero;
Vector3D LinearVelocity = Vector3D.Zero;
DateTime LastTrack = new DateTime();




// MAIN PROGRAM

public Program() {
	Echo("Reade to lunch");
}

public void Main(string argument) {
	if (argument == "launch") {
		GetBlocks();
		SetupProgrammableBlockLCD();
		Runtime.UpdateFrequency = UpdateFrequency.Update1;
		TargetReceiver = IGC.RegisterBroadcastListener("Designator");
	}
	else {
		GetBlocks();
		Print($"GUIDED MISSILE {RunSymbol()}", false, Antenna);
		if (TargetReceiver.HasPendingMessage) {
			Target = Unpack(TargetReceiver.AcceptMessage().Data.ToString());
			if (Target.TimeStamp != 0 && Target.Position.Length() != 0) LastTrack = DateTime.Now;
		}
		Vector3D forwardDirection = VectorToWorldSpace(new Vector3D(0, 0, -1), Antenna) - Antenna.GetPosition();
		AngularVelocity = Vector3D.Cross(forwardDirection, PreviousDirection);
		LinearVelocity = (Antenna.GetPosition() - PreviousPosition) * 60.0;
		PreviousPosition = Antenna.GetPosition();
		PreviousDirection = forwardDirection;
		double timeFromLastTrack = (LastTrack - DateTime.Now).Ticks / 1e7;
		if (timeFromLastTrack < MaxBlindTrackTime) {
			Print($"\nTracking {Target.Name}\n{Target.Position}", true, Antenna);
			Vector3D targetPositionEstimate = Target.Position + Target.Velocity * (float)timeFromLastTrack;
			double distance = (targetPositionEstimate - Antenna.GetPosition()).Length();
			Vector3D lateralVelocity = Vector3D.Reject(LinearVelocity * VelocityCorrection - Target.Velocity * (float)TargetVelocityCorrection, Vector3D.Normalize(targetPositionEstimate - Antenna.GetPosition()));
			Vector3D pointToDirection = targetPositionEstimate - Antenna.GetPosition() - lateralVelocity * distance;
			Warhead.IsArmed = true;
			if (distance < DetonationProximity) Warhead.Detonate();
			PointGyros(Gyroscopes, forwardDirection, pointToDirection, AimCorrectionRate, VelocityDamping);
		}
		else {
			Print($"\nTARGET LOST", true, Antenna);
			Warhead.IsArmed = false;
			PointGyros(Gyroscopes, Vector3D.Right, Vector3D.Right, AimCorrectionRate, VelocityDamping);
		}
	}
}



// MAIN HELPER FUNCTIONS

void GetBlocks() {
	List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>{};
	GridTerminalSystem.GetBlocks(blocks);
	foreach (IMyTerminalBlock block in blocks) {
		if (block is IMyGyro) Gyroscopes.Add(block as IMyGyro);
		if (block is IMyRadioAntenna) Antenna = block as IMyRadioAntenna;
		if (block is IMyWarhead) Warhead = block as IMyWarhead;
	}
	if (Gyroscopes.Count == 0) throw new Exception("No gyroscopes grouped with programmable block");
	if (Warhead == null) throw new Exception("No warhead grouped with programmable block");
	if (Antenna == null) throw new Exception("No antenna grouped with programmable block");
}

MyDetectedEntityInfo Unpack(string data) {
	string[] values = data.Split(':');
	Vector3D position = new Vector3D(double.Parse(values[0]), double.Parse(values[1]), double.Parse(values[2]));
	Vector3D velocity = new Vector3D(double.Parse(values[3]), double.Parse(values[4]), double.Parse(values[5]));
	MyDetectedEntityInfo target = new MyDetectedEntityInfo(
		0, "REMOTE TARGET", MyDetectedEntityType.Unknown, null, MatrixD.Identity, velocity,
		MyRelationsBetweenPlayerAndBlock.Enemies,
		new BoundingBoxD(position - Vector3D.One, position + Vector3D.One), 1
	);
	return target;
}

void PointGyros(List<IMyGyro> gyroscopes, Vector3D forwardDirection, Vector3D targetDirection, double rate, double damping) {
	Vector3D targetRotation = Vector3D.Cross(Vector3D.Normalize(targetDirection), Vector3D.Normalize(forwardDirection)) * rate;
	foreach (IMyGyro gyroscope in gyroscopes) {
		Vector3D torque = VectorToBlockSpace(targetRotation - AngularVelocity * damping + gyroscope.GetPosition(), gyroscope);
		gyroscope.Pitch = (float)torque.X;
		gyroscope.Yaw = (float)torque.Y;
		gyroscope.Roll = (float)torque.Z;
	}
}



// GENERIC HELPER FUNCTIONS

// Transfroms a vector from local block coordinates to world coordinates.
Vector3D VectorToWorldSpace(Vector3D vector, IMyCubeBlock block) {
	return Vector3D.Transform(vector, block.WorldMatrix);
}

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