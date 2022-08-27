// FighterController.cs
// Written by Laihela

Program () {
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	Echo("");// Clear the output window in terminal
	
	// Initialize classes
	Base.Init(this); // Always init Base first!
	ShipControl.Init(this);
	BombDropper.Init(this);
	RocketSalvo.Init(this);
	
	// Change settings
	Base.Title = "Fighter Control";
	foreach (var block in Base.GridBlocks) Base.SetLCDTheme(block, new Color(255, 255, 64), new Color(0, 0, 0));
	Base.DisplayKeyboard((ShipControl.Controller as IMyCockpit)?.GetSurface(3), 0.8f, 30f);
	Base.DisplayKeyboard((ShipControl.Controller as IMyCockpit)?.GetSurface(4), 0.8f, 30f);
	RocketSalvo.FireRate *= 2.0;
	ShipControl.SpeedLimit = 100.0;
}

void Main(string command) { switch (command) {
	
	case "Attach":
		BombDropper.Attach();
	break;
	
	case "DropOne":
		BombDropper.Drop(1);
	break;
	
	case "DropAll":
		BombDropper.Drop(1000000);
	break;
	
	case "":
		Base.Update();
		ShipControl.Update();
		RocketSalvo.Update();
		
		ShipControl.FlyOrbit(30.0);
		
		MyDetectedEntityInfo info = new MyDetectedEntityInfo();
		foreach (var turret in Base.GridBlocks.OfType<IMyTurretControlBlock>().ToList()) if (turret.GetTargetedEntity().EntityId != 0L) info = turret.GetTargetedEntity();
		if (info.EntityId != 0L) (ShipControl.Controller as IMyTextSurfaceProvider).GetSurface(1).WriteText(info.Name);
		else (ShipControl.Controller as IMyTextSurfaceProvider).GetSurface(1).WriteText("No target");
		
		Base.Print("CPU: " + Base.ProcessorLoad.ToString("000.000%"));
	break;

}}



static class Base {
	public static readonly DateTime Version = new DateTime(2022, 06, 25, 07, 10, 00);



	//// PUBLIC SETTINGS ////
	public static string Title = "Title";



	//// PUBLIC DATA ////
	public static List<IMyTerminalBlock> ConstructBlocks = new List<IMyTerminalBlock>();
	public static List<IMyTerminalBlock> GroupBlocks = new List<IMyTerminalBlock>();
	public static List<IMyTerminalBlock> GridBlocks = new List<IMyTerminalBlock>();
	public static double DeltaTime { get {
		return deltaTime;
	}}
	public static double ProcessorLoad { get {
		return (double)Program.Runtime.CurrentInstructionCount / Program.Runtime.MaxInstructionCount;
	}}



	//// PRIVATE DATA ////
	static MyGridProgram Program;
	static DateTime lastUpdate = DateTime.Now;
	static double deltaTime = 0.0;
	static string symbol = "";



	//// PUBLIC METHODS ////
	// Remember to call this first, before all other classes.
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing Base");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
		Program.GridTerminalSystem.GetBlockGroups(blockGroups);
		foreach(IMyBlockGroup blockGroup in blockGroups) {
			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
			blockGroup.GetBlocks(blocks);
			if (blocks.Contains(Program.Me)) GroupBlocks = blocks;
		}
		Program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(ConstructBlocks, b => b.IsSameConstructAs(Program.Me));
		Program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(GridBlocks, b => b.CubeGrid == Program.Me.CubeGrid);
		Program.Echo("    ConstructBlocks: " + ConstructBlocks.Count);
		Program.Echo("    GroupBlocks: " + GroupBlocks.Count);
		Program.Echo("    GridBlocks: " + GridBlocks.Count);
		
		Program.Me.GetSurface(0).ContentType = ContentType.TEXT_AND_IMAGE;
		SetLCDTheme(Program.Me, new Color(128, 255, 0), new Color(0, 0, 0), 1f, 2f);
		DisplayKeyboard(Program.Me.GetSurface(1));
	}
	// Call this first, every frame.
	public static void Update() {
		deltaTime = (DateTime.Now - lastUpdate).TotalSeconds;
		Program.Me.GetSurface(0).WriteText($"{Title} {GetSymbol()}\n");
		lastUpdate = DateTime.Now;
	}
	// Writes text onto the programmable block display.
	public static void Print(object text = null) {
		Program.Me.GetSurface(0).WriteText(text?.ToString() + "\n", true);
	}
	// Write an error to the programmable block display and stop the program.
	public static void Throw(object message) {
		Program.Me.GetSurface(0).WriteText("ERR: " + message.ToString());
		SetLCDTheme(Program.Me, new Color(0, 0, 0), new Color(255, 0, 0));
		throw new System.Exception(message.ToString());
	}

	// Write an ASCII keyboard to a text surface.
	public static void DisplayKeyboard(IMyTextSurface surface, float fontSize = 1.6f, float textPadding = 10f) {
		surface.ContentType = ContentType.TEXT_AND_IMAGE;
		surface.Alignment = TextAlignment.CENTER;
		surface.Font = "Monospace";
		surface.TextPadding = textPadding;
		surface.FontSize = fontSize;
		surface.WriteText(string.Join("\n",
			"[!][?][$][%][*]  [ENTR] [BCKSP]",
			"",
			"[Q][W][E][R][T][Y][U] [1][2][3]",
			"[A][S][D][F][G][H][J] [4][5][6]",
			"[Z][X][C][V][B][N][M] [7][8][9]",
			"[I][O][P][K][L][,][.] [ ][+][-]"
		));
	}
	// Change colors and font of all text surfaces on a block (if it has any).
	public static void SetLCDTheme(object block, Color? contentColor = null, Color? backgroundColor = null, float fontSize = -1f, float textPadding = -1f) {
		IMyTextSurfaceProvider provider = block as IMyTextSurfaceProvider;
		if (provider == null) return;
		Color content = contentColor.GetValueOrDefault(new Color(179, 237, 255));
		Color background = backgroundColor.GetValueOrDefault(new Color(0, 88, 151));
		for (int s = 0; s < provider.SurfaceCount; s++) {
			IMyTextSurface surface = provider.GetSurface(s);
			surface.ScriptBackgroundColor = background;
			surface.ScriptForegroundColor = content;
			surface.BackgroundColor = background;
			surface.FontColor = content;
			surface.Font = "Monospace";
			if (textPadding > 0f) surface.TextPadding = textPadding;
			if (fontSize > 0f) surface.FontSize = fontSize;
			if (surface.ContentType == ContentType.NONE) surface.ContentType = ContentType.SCRIPT;
		}
	}

	// Returns current time in seconds.
	public static double Now() {
		return DateTime.Now.Ticks / 1e7;
	}
	// Returns an animated symbol.
	public static string GetSymbol() {
		if       (symbol == "|")  symbol = "/";
		else if  (symbol == "/")  symbol = "-";
		else if  (symbol == "-")  symbol = "\\";
		else                      symbol = "|";
		return symbol;
	}


	//// VECTOR MATH ////
	// Transfrom a vector from block space to world space.
	public static Vector3D VectorToWorldSpace(Vector3D vector, IMyCubeBlock block) {
		return Vector3D.Transform(vector, block.WorldMatrix);
	}
	// Transfrom a vector from world space to block space.
	public static Vector3D VectorToBlockSpace(Vector3D vector, IMyCubeBlock block) {
		return Vector3D.TransformNormal(vector - block.GetPosition(), MatrixD.Transpose(block.WorldMatrix));
	}
	// Transfrom a vector from block space to world space, ignoring block position.
	public static Vector3D DirectionToWorldSpace(Vector3D direction, IMyCubeBlock block) {
		return Vector3D.Transform(direction, block.WorldMatrix) - block.GetPosition();
	}
	// Transfroms a vector from world space to block space, ignoring block position.
	public static Vector3D DirectionToBlockSpace(Vector3D direction, IMyCubeBlock block) {
		return Vector3D.TransformNormal(direction, MatrixD.Transpose(block.WorldMatrix));
	}

	// Get an euler rotation from direction to direction in radians.
	public static Vector3D GetRotation(Vector3D from, Vector3D to) {
		from = Vector3D.Normalize(from);
		to = Vector3D.Normalize(to);
		if (from == to) return Vector3D.Zero; // Avoid division by zero with Math.Acos and Vector3D.Normalize
		double angle = Math.Acos(Vector3D.Dot(from, to));
		Vector3D axis = Vector3D.Normalize(Vector3D.Cross(from, to));
		return axis * angle;
	}
		// Limit the length of a vector between a minimum and a maximum value.
	public static Vector3D Clamp(Vector3D vec, double min, double max) {
		double len = vec.Length();
		if (len == 0) return vec;
		return len < min ? vec / len * min : (len > max ? vec / len * max : vec);
	}


	//// NUMBER MATH ////
	// Limit a number between a minimum and a maximum value.
	public static double Clamp(double val, double min, double max) {
		return val < min ? min : (val > max ? max : val);
	}
	// Return a number interpolated from a to b by time.
	public static double Lerp(double a, double b, double time) {
		return a * (1 - time) + b * time;
	}
	// Return the modulus of a number.
	// Result is always positive, works consistently with negative input values.
	public static double Mod(double v, double m) {
		v = v % m;
		return v < 0 ? v + m: v;
	}
	
	// Set rotor velocity in radians per second.
	public static void SetRotorVelocity(IMyMotorStator rotor, double velocity) {
		rotor.TargetVelocityRad = (float)(double.IsNaN(velocity) ? 0.0 : velocity);
	}
	
}
static class ShipControl {
	public static readonly DateTime Version = new DateTime(2022, 06, 25, 06, 18, 00);
	public static MyGridProgram Program;
	
	public static double SpeedLimit = 1000.0;
	public static double AimSensitivity = 0.03;
	
	static List<IMyGyro> gyroBlocks = new List<IMyGyro>();
	static List<IMyThrust> thrusterBlocks = new List<IMyThrust>();
	static List<ThrusterGroup> thrusterGroups = new List<ThrusterGroup>();
	static IMyShipController shipController = null;
	
	public static IMyShipController Controller {get{ return shipController; }}
	
	
	
	// Remember to call this first
	public static void Init(MyGridProgram program, List<IMyTerminalBlock> blocks) {
		Program = program;
		Program.Echo("Initializing ShipControl");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		gyroBlocks = blocks.OfType<IMyGyro>().ToList();
		thrusterBlocks = blocks.OfType<IMyThrust>().ToList();
		shipController = blocks.OfType<IMyShipController>().Where(s => s.IsMainCockpit).FirstOrDefault();
		if (shipController == null) shipController = blocks.OfType<IMyShipController>().FirstOrDefault();
		Program.Echo("    shipController: " + (shipController != null));
		Program.Echo("    ThrusterBlocks: " + thrusterBlocks.Count);
		Program.Echo("    GyroBlocks: " + gyroBlocks.Count);
		
		foreach (var gyro in gyroBlocks) gyro.GyroOverride = true;
		thrusterGroups.Add(ThrusterGroup.CreateGroup(Base6Directions.Direction.Forward, thrusterBlocks));
		thrusterGroups.Add(ThrusterGroup.CreateGroup(Base6Directions.Direction.Backward, thrusterBlocks));
		thrusterGroups.Add(ThrusterGroup.CreateGroup(Base6Directions.Direction.Left, thrusterBlocks));
		thrusterGroups.Add(ThrusterGroup.CreateGroup(Base6Directions.Direction.Right, thrusterBlocks));
		thrusterGroups.Add(ThrusterGroup.CreateGroup(Base6Directions.Direction.Up, thrusterBlocks));
		thrusterGroups.Add(ThrusterGroup.CreateGroup(Base6Directions.Direction.Down, thrusterBlocks));
		thrusterGroups.RemoveAll(g => g == null);
	}
	public static void Update() {
		SetThrust(Vector3.Zero);
	}
	public static void Fly(double smoothing = 30.0) {
		Vector3D linearVelocity = shipController.GetShipVelocities().LinearVelocity;
		Vector3D angularVelocity = shipController.GetShipVelocities().AngularVelocity;
		Vector3D centripetalAcceleration = Vector3D.Cross(angularVelocity, linearVelocity);
		
		List<Vector3D> controlVectors = GetVectors();
		
		SetRotation(controlVectors[1] * 2 * Math.PI, 8.0);
		if (shipController.DampenersOverride == false) SetThrust(controlVectors[0]);
		else SetVelocity(controlVectors[0] * SpeedLimit, smoothing);
	}
	public static void FlyOrbit(double smoothing = 30.0) {
		Vector3D linearVelocity = shipController.GetShipVelocities().LinearVelocity;
		Vector3D angularVelocity = shipController.GetShipVelocities().AngularVelocity;
		Vector3D centripetalAcceleration = Vector3D.Cross(angularVelocity, linearVelocity);
		
		List<Vector3D> controlVectors = GetVectors();
		SetRotation(controlVectors[1] * 2 * Math.PI, 8.0);
		if (shipController.DampenersOverride == false) SetThrust(controlVectors[0]);
		else SetVelocity(controlVectors[0] * SpeedLimit, smoothing);
		AddAcceleration(centripetalAcceleration);
	}
	/* public static void InterceptTarget(List<Vector3D> targetVectors) {
		Vector3D position = shipController.GetPosition();
		Vector3D velocity = shipController.GetShipVelocities().LinearVelocity;
		while (targetVectors.Count < 2) targetVectors.Add(Vector3D.Zero);
		Vector3D targetPosition = targetVectors[0];
		Vector3D targetVelocity = targetVectors[1];
		Vector3D target = Vector3D.Normalize(targetPosition - position) * 100.0 + targetVelocity;
		SetLinearVelocity(target, 0.0);
		SetAngle(velocity, targetVelocity - velocity);
	}
	*/
	public static void SetAngle(Vector3D targetForward, Vector3D? targetUp = null, double smoothing = 10.0) {
		if (shipController == null) Base.Throw("SetAngle failed: Ship has no controller");
		if (smoothing < 0.0) smoothing = 0.0;
		Vector3D forward = shipController.WorldMatrix.Forward;
		Vector3D up = shipController.WorldMatrix.Up;
		Vector3D pitchYawDelta = Base.GetRotation(forward, targetForward);
		Vector3D rollDelta = Vector3D.Zero;
		if (targetUp != null) rollDelta = Base.GetRotation(up, targetUp.Value);
		Vector3D targetVelocity = (pitchYawDelta + rollDelta) * 0.5 / Base.DeltaTime;
		Vector3D velocity = shipController.GetShipVelocities().AngularVelocity;
		SetRotation(targetVelocity - velocity * velocity.Length() /** velocity.Length()*/ * smoothing);
	}
	public static void SetRotation(Vector3D targetVelocity, double smoothing = 0.0) {
		if (smoothing < 0.0) smoothing = 0.0;
		if (shipController != null) {
			Vector3D velocity = shipController.GetShipVelocities().AngularVelocity;
			if (smoothing < 0.0) smoothing = 0.0;
			targetVelocity = Vector3D.ClampToSphere((targetVelocity - velocity) / (smoothing + 1.0), 0.2) + velocity;
		}
		foreach (var gyro in gyroBlocks) {
			Vector3 v = -Base.DirectionToBlockSpace(targetVelocity, gyro);
			gyro.Pitch = v.X;
			gyro.Yaw = v.Y;
			gyro.Roll = v.Z;
		}
	}
	public static void AddRotation(Vector3D targetVelocity, double smoothing = 0.0) {
		if (smoothing < 0.0) smoothing = 0.0;
		if (shipController != null) {
			Vector3D velocity = shipController.GetShipVelocities().AngularVelocity;
			if (smoothing < 0.0) smoothing = 0.0;
			targetVelocity = Vector3D.ClampToSphere((targetVelocity - velocity) / (smoothing + 1.0), 0.2) + velocity;
		}
		foreach (var gyro in gyroBlocks) {
			Vector3 v = -Base.DirectionToBlockSpace(targetVelocity, gyro);
			gyro.Pitch += v.X;
			gyro.Yaw += v.Y;
			gyro.Roll += v.Z;
		}
	}
	public static void SetThrust(Vector3D inputVector) {
		foreach (var thrusterGroup in thrusterGroups) {
			Vector3D d = thrusterGroup.thrusterBlocks[0].WorldMatrix.Forward;
			double t = Vector3D.Dot(d, -inputVector);
			thrusterGroup.SetThrust(t);
		}
	}
	public static void SetAcceleration(Vector3D acceleration) {
		if (shipController == null) Base.Throw("Ship has no controller");
		foreach (var thrusterGroup in thrusterGroups) {
			Vector3D d = thrusterGroup.thrusterBlocks[0].WorldMatrix.Forward;
			double a = Vector3D.Dot(d, -acceleration);
			thrusterGroup.SetAcceleration(a);
		}
	}
	public static void AddAcceleration(Vector3D acceleration) {
		if (shipController == null) Base.Throw("Ship has no controller");
		foreach (var thrusterGroup in thrusterGroups) {
			Vector3D d = thrusterGroup.thrusterBlocks[0].WorldMatrix.Forward;
			double a = Vector3D.Dot(d, -acceleration);
			thrusterGroup.AddAcceleration(a);
		}
	}
	public static void SetVelocity(Vector3D targetVelocity, double smoothing) {
		if (shipController == null) return;
		if (smoothing < 0.0) smoothing = 0.0;
		Vector3D gravity = shipController.GetNaturalGravity();
		Vector3D velocity = shipController.GetShipVelocities().LinearVelocity;
		foreach (var thrusterGroup in thrusterGroups) {
			Vector3D d = thrusterGroup.thrusterBlocks[0].WorldMatrix.Forward;
			double g = Vector3D.Dot(d, gravity);
			double v = Vector3D.Dot(d, velocity - targetVelocity) / Base.DeltaTime / (smoothing + 1);
			double a = (v / 2 + g);
			thrusterGroup.SetAcceleration(a);
		}
	}
	public static List<Vector3D> GetVectors() {
		List<Vector3D> vectors = new List<Vector3D>{ Vector3D.Zero, Vector3D.Zero };
		if (shipController == null) return vectors;
		double pitch = Base.Clamp(-shipController.RotationIndicator.X * AimSensitivity, -1.0, 1.0);
		double roll = Base.Clamp(-shipController.RotationIndicator.Y * AimSensitivity, -1.0, 1.0);
		double yaw = -shipController.RollIndicator;
		vectors[0] = Base.DirectionToWorldSpace(shipController.MoveIndicator, shipController);
		vectors[1] = Base.DirectionToWorldSpace(new Vector3D(pitch, roll, yaw), shipController);
		return vectors;
	}

	class ThrusterGroup {
		public Base6Directions.Direction direction;
		public Vector3D vector;
		public List<IMyThrust> thrusterBlocks = new List<IMyThrust>();
		
		public static ThrusterGroup CreateGroup(Base6Directions.Direction dir, List<IMyThrust> thrusters) {
			ThrusterGroup g = new ThrusterGroup();
			g.direction = dir;
			g.vector = Base6Directions.GetVector(dir);
			foreach (var thruster in thrusters) {
				if (thruster.Orientation.Forward != dir) continue;
				g.thrusterBlocks.Add(thruster);
			}
			if (g.thrusterBlocks.Count == 0) return null;
			return g;
		}
		public double GetMaxForce() {
			double maxForce = 0.0;
			foreach (var thruster in thrusterBlocks) {
				if (thruster.IsWorking == false) continue;
				maxForce += thruster.MaxEffectiveThrust;
			}
			return maxForce;
		}
		public void SetAcceleration(double acceleration) {
			if (shipController == null) Base.Throw("Ship has no cockpit");
			double mass = shipController.CalculateShipMass().PhysicalMass;
			SetThrust((acceleration * mass) / GetMaxForce());
		}
		public void SetThrust(double thrust) {
			foreach (var thruster in thrusterBlocks) {
				float force = (float)thrust * thruster.MaxThrust;
				thruster.ThrustOverride = Math.Max(force, 0.0000000000000000000000000000000000116f);
				// Broken, disabling thrusters messes with GetMaxForce()
				//thruster.Enabled = thrust > 0.001;
			}
		}
		public void AddAcceleration(double acceleration) {
			if (shipController == null) Base.Throw("Ship has no cockpit");
			double mass = shipController.CalculateShipMass().PhysicalMass;
			AddThrust((acceleration * mass) / GetMaxForce());
		}
		public void AddThrust(double thrust) {
			foreach (var thruster in thrusterBlocks) {
				float force = (float)thrust * thruster.MaxThrust;
				thruster.ThrustOverride += force;
			}
		}
	}
}
static class RocketSalvo {
	public static readonly DateTime Version = new DateTime(2021, 05, 10, 16, 34, 00);
	public static MyGridProgram Program;
	public static double FireRate = 1.0;
	
	static int launcherIndex = 0;
	static DateTime lastLaunch = DateTime.Now;
	static IMySmallMissileLauncher launcher = null;
	static List<IMySmallMissileLauncher> launchers = new List<IMySmallMissileLauncher>();
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing RocketSalvo");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		launchers = Base.GroupBlocks.OfType<IMySmallMissileLauncher>().ToList();
		if (launchers.Count == 0) launchers = Base.GridBlocks.OfType<IMySmallMissileLauncher>().ToList();
		Program.Echo("    Launchers: " + launchers.Count);
		
		if (launchers.Count == 0) return;
		foreach (var launcher in launchers) launcher.Enabled = false;
		launcher = launchers[launcherIndex];
		FireRate = launchers.Count;
	}
	
	public static void Update() {
		if (launchers.Count == 0) return;
		if (launcher == null || launcher.IsFunctional == false) {
			nextLauncher();
			return;
		}
		if ((DateTime.Now - lastLaunch).TotalSeconds < 1.0 / FireRate) return;
		launcher.Enabled = true;
		if (launcher.IsShooting) {
			launcher.Enabled = false;
			nextLauncher();
			lastLaunch = DateTime.Now;
		}
	}
	
	static void nextLauncher() {
		launcherIndex = (launcherIndex + 1) % launchers.Count;
		launcher = launchers[launcherIndex];
	}
}
static class BombDropper {
	public static readonly DateTime Version = new DateTime(2021, 05, 10, 02, 52, 00);
	public static MyGridProgram Program;
	
	static List<IMyMotorStator> dropperMotors = new List<IMyMotorStator>();
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing BombDropper");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		dropperMotors = Base.GridBlocks.OfType<IMyMotorStator>().Where(m => m.CustomData.Contains("BombDropper")).ToList();
		
		Program.Echo("    DropperMotors: " + dropperMotors.Count);
	}
	
	public static void Attach() {
		foreach (var motor in dropperMotors) {
			motor.Detach();
			motor.Attach();
		}
	}
	
	public static void Drop(int count) {
		foreach (var motor in dropperMotors) {
			if (motor.IsAttached == false) continue;
			if (--count < 0) break;
			var top = motor.Top;
			foreach (var computer in Base.ConstructBlocks.OfType<IMyProgrammableBlock>()) {
				if (computer.CubeGrid != top.CubeGrid) continue;
				if (computer.CustomData.Contains("BombComputer")) computer.TryRun("Fire");
			}
			motor.Detach();
		}
	}
}

