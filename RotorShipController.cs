// RotorShipController.cs
// Written by Laihela

Program () {
	Echo(""); // Clear output
	Base.Init(this);
	Util.Init(this);
	RotorcraftControl.Init(this);
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	
	// Change settings
	Me.GetSurface(0).FontSize = 1.2F;
}

void Main() {
	Base.Title("Rotorcraft Control");
	RotorcraftControl.Update();
}



static class Base {
	public static readonly DateTime Version = new DateTime(2020, 10, 14, 19, 50, 00);
	public static MyGridProgram Program;
	public static List<IMyTerminalBlock> GroupBlocks;
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing Base");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		SetupProgrammableBlockLCD();
		
		List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
		Program.GridTerminalSystem.GetBlockGroups(blockGroups);
		foreach(IMyBlockGroup blockGroup in blockGroups) {
			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
			blockGroup.GetBlocks(blocks);
			if (blocks.Contains(Program.Me)) GroupBlocks = blocks;
		}
		
		if (GroupBlocks.Count == 0) throw new Exception("NO BLOCKS GROUPED WITH PROGRAMMABLE BLOCK");
	}
	
	// Clears the programmable block display and shows a title.
	public static void Title(string title) {
		Program.Me.GetSurface(0).WriteText($"{title} {RunSymbol()}\n");
	}
	
	// Writes text onto the programmable block display.
	public static void Print(string text = "") {
		Program.Me.GetSurface(0).WriteText(text + "\n", true);
	}
	
	// Returns current time in seconds.
	public static double Now() {
		return DateTime.Now.Ticks / 1e7;
	}
	
	// Returns an animated symbol.
	public static string RunSymbol() {
		double cycle = Now() % (2.0 / 3.0) * 6.0;
		if (cycle < 1) return "\\";
		if (cycle < 2) return "|";
		if (cycle < 3) return "/";
		return "-";
	}
	
	// Sets the visual style of the programmable block's LCDs.
	// The keyboard LCD will show an ASCII-keyboard.
	public static void SetupProgrammableBlockLCD() {
		IMyTextSurface display = Program.Me.GetSurface(0);
		IMyTextSurface keyboard = Program.Me.GetSurface(1);
		display.ContentType = ContentType.TEXT_AND_IMAGE;
		display.Alignment = TextAlignment.LEFT;
		display.FontColor = new Color(128, 255, 0);
		display.Font = "Monospace";
		display.TextPadding = 5.0F;
		display.FontSize = 1.4F;
		keyboard.ContentType = ContentType.TEXT_AND_IMAGE;
		keyboard.Alignment = TextAlignment.CENTER;
		keyboard.FontColor = new Color(128, 255, 0);
		keyboard.Font = "Monospace";
		keyboard.TextPadding = 10.0F;
		keyboard.FontSize = 1.6F;
		keyboard.WriteText(string.Join("\n",
			"[!][?][$][%][*]   [ENTR] [RTRN]",
			"",
			"[A][B][C][D][E][F][G] [1][2][3]",
			"[H][I][J][K][L][M][N] [4][5][6]",
			"[O][P][Q][R][S][T][U] [7][8][9]",
			"[V][W][X][Y][Z][,][.] [ ][+][-]"
		));
	}
}

static class Util {
	public static readonly DateTime Version = new DateTime(2020, 10, 20, 16, 40, 00);
	public static MyGridProgram Program;
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing Util");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
	}
	
	// Transfroms a vector from local block coordinates to world coordinates.
	public static Vector3D VectorToWorldSpace(Vector3D vector, IMyCubeBlock block) {
		return Vector3D.Transform(vector, block.WorldMatrix);
	}

	// Transfroms a vector from world coordinates to local block coordinates.
	public static Vector3D VectorToBlockSpace(Vector3D vector, IMyCubeBlock block) {
		return Vector3D.TransformNormal(vector - block.GetPosition(), MatrixD.Transpose(block.WorldMatrix));
	}
	
	// Transfroms a vector from local block coordinates to world coordinates.
	// Does not take block position into account.
	public static Vector3D DirectionToWorldSpace(Vector3D direction, IMyCubeBlock block) {
		return Vector3D.Transform(direction, block.WorldMatrix) - block.GetPosition();
	}

	// Transfroms a vector from world coordinates to local block coordinates.
	// Does not take block position into account.
	public static Vector3D DirectionToBlockSpace(Vector3D direction, IMyCubeBlock block) {
		return Vector3D.TransformNormal(direction, MatrixD.Transpose(block.WorldMatrix));
	}

	// Ensure that a number stays between a minimum and a maximum value.
	public static double Clamp(double val, double min, double max) {
		return val < min ? min : (val > max ? max : val);
	}

	// Ensure that the length of a vector stays between a minimum and a maximum value.
	public static Vector3D Clamp(Vector3D vec, double min, double max) {
		double len = vec.Length();
		if (len == 0) return vec;
		return len < min ? vec / len * min : (len > max ? vec / len * max : vec);
	}

	// Returns a value interpolated from a to b by time.
	public static double Lerp(double a, double b, double time) {
		return a * (1 - time) + b * time;
	}

	// Returns the modulus of a number.
	// Result is always positive, works consistently with negative input values.
	public static double Mod(double v, double m) {
		v = v % m;
		return v < 0 ? v + m: v;
	}
	
	public static bool IsNormal(double val) {
		return val != double.NaN && val != double.PositiveInfinity && val != double.NegativeInfinity;
	}
	
	public static void SetRotorVelocity(IMyMotorStator rotor, double velocity) {
		double rpm = velocity * 30.0 / Math.PI;
		if (double.IsNaN(rpm)) rpm = 0.0;
		rotor.SetValue<float>("Velocity", (float)rpm);
	}
}

static class RotorcraftControl {
	public static readonly DateTime Version = new DateTime(2021, 02, 24, 14, 55, 00);
	public static MyGridProgram Program;
	public static List<IMyGyro> GyroBlocks;
	public static List<IMyThrust> ThrusterBlocks;
	public static List<IMyMotorStator> RotorBlocks;
	public static List<IMyShipController> ShipControllers;
	public static double ControlAcceleration = 6.0;
	public static double RotorCorrectionRate = 2.0;
	public static double RollInertiaCorrection = 8.0;
	
	static public DateTime lastUpdate = DateTime.Now; // Fix bruh moment
	static Vector3D previousAngularVelocity = new Vector3D(0.0, 0.0, 0.0);
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing RotorCraftControl");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		GyroBlocks = Base.GroupBlocks.OfType<IMyGyro>().ToList();
		ThrusterBlocks = Base.GroupBlocks.OfType<IMyThrust>().ToList();
		RotorBlocks = Base.GroupBlocks.OfType<IMyMotorStator>().ToList();
		ShipControllers = Base.GroupBlocks.OfType<IMyShipController>().ToList();
		Program.Echo("    GyroBlocks: " + GyroBlocks.Count);
		Program.Echo("    RotorBlocks: " + RotorBlocks.Count);
		Program.Echo("    ThrusterBlocks: " + ThrusterBlocks.Count);
		Program.Echo("    ShipControllers: " + ShipControllers.Count);
	}
	
	public static void Update() {
		if (ShipControllers.Count == 0) return;
		IMyShipController controller = ShipControllers[0];
		
		double deltaTime = (DateTime.Now - lastUpdate).TotalSeconds;
		
		Vector3D gravity = controller.GetNaturalGravity();
		double mass = controller.CalculateShipMass().TotalMass;
		double maxThrust = 0.0;
		foreach (var thruster in ThrusterBlocks) maxThrust += thruster.MaxThrust;
		double maxAccel = maxThrust / mass;
		
		Vector3D moveDirection = Util.DirectionToWorldSpace(controller.MoveIndicator, controller);
		Vector3D targetAcceleration = gravity - moveDirection * ControlAcceleration;
		
		Base.Print("Move: " + moveDirection.Length().ToString("00.0"));
		
		foreach (var rotor in RotorBlocks) {
			Vector3D aim = Util.DirectionToBlockSpace(targetAcceleration, rotor);
			double deviation = Math.Atan2(-aim.X, aim.Z) - rotor.Angle;
			if (deviation < -Math.PI) deviation += Math.PI * 2;
			Util.SetRotorVelocity(rotor, deviation * RotorCorrectionRate);
		}
		
		foreach (var thruster in ThrusterBlocks) {
			thruster.ThrustOverride = (float)(targetAcceleration.Length() * mass / ThrusterBlocks.Count);
		}
		
		Vector3D angularVelocity = controller.GetShipVelocities().AngularVelocity;
		Vector3D angularAcceleration = (angularVelocity - previousAngularVelocity) * deltaTime;
		Vector3D forward = controller.WorldMatrix.Forward;
		Vector3D right = controller.WorldMatrix.Right;
		// TODOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOOO
		
		foreach (var gyroscope in GyroBlocks) {
			//thruster.ThrustOverride = (float)(targetAcceleration.Length() * mass / ThrusterBlocks.Count);
		}
		
		Base.Print("Grav: " + gravity.Length().ToString());
		Base.Print("Mass: " + mass.ToString());
		Base.Print("Thrs: " + maxThrust.ToString());
		Base.Print("Accl: " + maxAccel.ToString());
		
		lastUpdate = DateTime.Now;
		previousAngularVelocity = angularVelocity;
	}
}
