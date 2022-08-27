// _CLASS_LIBRARY.cs
// Written by Laihela



static class TEMPLATE {
	public static readonly DateTime Version = new DateTime(2022, 07, 17, 00, 00, 00);



	//// PUBLIC DATA ////
	public static MyGridProgram Program;
	public static List<IMyMotorStator> Rotors = new List<IMyMotorStator>();



	//// PUBLIC METHODS ////
	// Remember to call this first
	public static void Init(MyGridProgram program, List<IMyTerminalBlock> blocks) {
		Program = program;
		Program.Echo("Initializing TEMPLATE");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		Rotors = blocks.OfType<IMyMotorStator>().ToList();
		Program.Echo("    Rotors: " + Rotors.Count);
	}
	// Speeen rotorrr
	public static void Spin() {
		Base.Print("Speeen!");
		foreach (var rotor in Rotors) {
			Base.SetRotorVelocity(rotor, Math.PI);
		}
	}
}

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
static class Comms {
	public static readonly DateTime Version = new DateTime(2022, 06, 24, 22, 57, 00);



	//// PUBLIC SETTINGS ////
	public static double AntennaPulseRange = 0.0;



	//// PUBLIC DATA ////
	public static MyGridProgram Program;
	public static Dictionary<string, IMyBroadcastListener> Receivers = new Dictionary<string, IMyBroadcastListener>();



	//// PUBLIC METHODS ///
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing Comms");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
	}
	// Start receiving messages sent to this channel.
	public static void AddReceiver(string channel) {
		if (Receivers.ContainsKey(channel)) Base.Throw($"Receiver for channel \"{channel}\" already exists");
		Receivers.Add(channel, Program.IGC.RegisterBroadcastListener(channel));
	}
	// Transmit a message on this channel.
	public static void Transmit(string channel, string data) {
		Program.IGC.SendBroadcastMessage(channel, data, TransmissionDistance.TransmissionDistanceMax);
	}
	// Get one message received on this channel and remove it from the receiver.
	public static string ReadOne(string channel) {
		IMyBroadcastListener receiver = null;
		bool exists = Receivers.TryGetValue(channel, out receiver);
		if (!exists) Base.Throw($"Read failed: channel \"{channel}\" does not have a receiver");
		if (!receiver.HasPendingMessage) return "";
		return receiver.AcceptMessage().Data.ToString();
	}
	// Get all messages received on this channel and remove them from the receiver.
	public static List<string> ReadAll(string channel) {
		IMyBroadcastListener receiver = null;
		bool exists = Receivers.TryGetValue(channel, out receiver);
		if (!exists) Base.Throw($"Read failed: channel \"{channel}\" does not have a receiver");
		List<string> messages = new List<string>();
		while (receiver.HasPendingMessage) messages.Add(receiver.AcceptMessage().Data.ToString());
		return messages;
	}
}
static class Sensors {
	public static readonly DateTime Version = new DateTime(2022, 06, 25, 05, 49, 00);



	//// PUBLIC SETTINGS ////
	public static bool PreventDetectorTurretFiring = false;



	//// PUBLIC DATA ////
	public static List<IMySensorBlock> SensorBlocks;
	public static List<IMyLargeTurretBase> TurretBlocks;



	//// PRIVATE DATA ////
	static MyGridProgram Program;



	//// PUBLIC METHODS ////
	// Remember to call this first
	public static void Init(MyGridProgram program, List<IMyTerminalBlock> blocks) {
		Program = program;
		Program.Echo("Initializing Sensors");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		SensorBlocks = blocks.OfType<IMySensorBlock>().ToList();
		Program.Echo("    SensorBlocks: " + SensorBlocks.Count());
		TurretBlocks = blocks.OfType<IMyLargeTurretBase>().ToList();
		Program.Echo("    TurretBlocks: " + TurretBlocks.Count());
	}
	// Call this every frame after everything else
	public static void LateUpdate() {
		foreach (IMyLargeTurretBase turret in TurretBlocks) {
			turret.Enabled = turret.GetTargetedEntity().Position == Vector3D.Zero || !PreventDetectorTurretFiring;
		}
	}
	// Get detected entity from sensors and turrets.
	public static MyDetectedEntityInfo Detect() {
		foreach (IMySensorBlock sensor in SensorBlocks) {
			MyDetectedEntityInfo entityInfo = new MyDetectedEntityInfo();
			try { entityInfo = sensor.LastDetectedEntity; } catch {}
			if (entityInfo.Position != Vector3D.Zero) return entityInfo;
		}
		foreach (IMyLargeTurretBase turret in TurretBlocks) {
			MyDetectedEntityInfo entityInfo = turret.GetTargetedEntity();
			if (entityInfo.Position != Vector3D.Zero) return entityInfo;
		}
		return new MyDetectedEntityInfo();
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
	public static readonly DateTime Version = new DateTime(2021, 05, 08, 01, 05, 00);
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
		launchers = Base.GridBlocks.OfType<IMySmallMissileLauncher>().ToList();
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
static class TargetTracking {
	public static readonly DateTime Version = new DateTime(2022, 06, 25, 12, 39, 00);



	//// PUBLIC SETTINGS ////
	public static double MissingTargetExpireTime = 3.0;
	public static double NewTargetPriorityBias = 0.95;
	public static double TrackingJitterScale = 1.0;
	public static bool PingEnabled = false;



	//// PUBLIC DATA ////
	public static MyGridProgram Program;
	public static List<IMyCameraBlock> CameraBlocks;
	public static Target CurrentTarget = null;



	//// PRIVATE DATA ////
	static int currentCamera = 0;
	static DateTime lastScan = DateTime.Now;
	static Random Rand = new Random();
	
	public class Target {
		public long Id = 0;
		public double Size = 0.0;
		public Vector3D Position = Vector3D.Zero;
		public Vector3D Velocity = Vector3D.Zero;
		public Vector3D Acceleration = Vector3D.Zero;
		public DateTime LastDetected = DateTime.Now;
		
		public double BlindTime {
			get { return (DateTime.Now - LastDetected).TotalSeconds; }
		}
		public double Distance {
			get { return (Position - Program.Me.GetPosition()).Length(); }
		}
		public double Priority {
			get { return Size / Distance / (BlindTime + 1); }
		}
		public bool IsValid {
			get { return Size != 0; }
		}
		public Vector3D PositionEstimate {
			get {
				double time = BlindTime;
				return Position + Velocity * time + Acceleration * time * time * 0.5;
			}
		}
		public Vector3D VelocityEstimate {
			get { return Velocity + Acceleration * BlindTime; }
		}
		
		public Target() {}
		
		public Target(MyDetectedEntityInfo entityInfo) {
			Id = entityInfo.EntityId;
			Size = (entityInfo.BoundingBox.Max - entityInfo.BoundingBox.Min).AbsMin();
			Position = entityInfo.BoundingBox.Center;
			Velocity = entityInfo.Velocity;
		}
		
		public string Serialize() {
			return String.Join(":",
				Id,
				Size,
				Position.X,
				Position.Y,
				Position.Z,
				Velocity.X,
				Velocity.Y,
				Velocity.Z,
				Acceleration.X,
				Acceleration.Y,
				Acceleration.Z,
				LastDetected.Ticks
			);
		}
		
		public static Target Deserialize(string data) {
			Target target = new Target();
			string[] values = data.Split(new char[]{':'}, StringSplitOptions.RemoveEmptyEntries);
			if (values.Count() != 12) {
				Program.Echo($"TargetTracking.Target.Deserialize: incorrect data format! ({values.Count()} values)");
				return target;
			}
			target.Id = long.Parse(values[0]);
			target.Size = double.Parse(values[1]);
			target.Position.X = double.Parse(values[2]);
			target.Position.Y = double.Parse(values[3]);
			target.Position.Z = double.Parse(values[4]);
			target.Velocity.X = double.Parse(values[5]);
			target.Velocity.Y = double.Parse(values[6]);
			target.Velocity.Z = double.Parse(values[7]);
			target.Acceleration.X = double.Parse(values[8]);
			target.Acceleration.Y = double.Parse(values[9]);
			target.Acceleration.Z = double.Parse(values[10]);
			target.LastDetected = new DateTime(long.Parse(values[11]));
			return target;
		}
	}



	//// PUBLIC METHODS ////
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing TargetTracking");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		//CameraBlocks = Base.GroupBlocks.OfType<IMyCameraBlock>().ToList();
		CameraBlocks = Base.GroupBlocks.OfType<IMyCameraBlock>().ToList();
		Program.Echo("    CameraBlocks: " + CameraBlocks.Count);
		foreach(IMyCameraBlock camera in CameraBlocks) camera.EnableRaycast = true;
	}
	// Call this every frame
	public static void Update() {
		if (CurrentTarget == null) {
			foreach (var camera in CameraBlocks) camera.EnableRaycast = PingEnabled;
			return;
		}
		foreach (var camera in CameraBlocks) camera.EnableRaycast = true;
		Track(CurrentTarget);
		if (CurrentTarget.BlindTime > MissingTargetExpireTime) CurrentTarget = null;
	}
	public static void ConsiderTarget(object target) {
		Target newTarget;
		if (target is Target) newTarget = (Target)target;
		else if (target is MyDetectedEntityInfo) newTarget = new Target((MyDetectedEntityInfo)target);
		else if (target is string) {
			string data = (string)target;
			if (data == "") newTarget = new Target();
			else newTarget = Target.Deserialize(data);
		}
		else {
			Program.Echo("TargetTracking.ConsiderTarget: invalid target type: " + target.GetType());
			return;
		}
		if (newTarget.IsValid == false) return;
		if (CurrentTarget == null) CurrentTarget = newTarget;
		if (newTarget.LastDetected < CurrentTarget.LastDetected) return;
		if (newTarget.Priority * NewTargetPriorityBias > CurrentTarget.Priority) CurrentTarget = newTarget;
	}
	public static void Ping() {
		if (CameraBlocks.Count() == 0) return;
		IMyCameraBlock camera = CameraBlocks[currentCamera];
		ConsiderTarget(camera.Raycast(camera.AvailableScanRange));
		currentCamera = (currentCamera + 1) % CameraBlocks.Count;
	}
	public static double GetRange() {
		if (CameraBlocks.Count() == 0) return 0.0;
		return CameraBlocks[currentCamera].AvailableScanRange;
	}
	public static List<Vector3D> GetVectors() {
		List<Vector3D> vectors = new List<Vector3D> {Vector3D.Zero, Vector3D.Zero, Vector3D.Zero};
		if (CurrentTarget != null) {
			vectors[0] = CurrentTarget.PositionEstimate;
			vectors[1] = CurrentTarget.VelocityEstimate;
			vectors[2] = CurrentTarget.Acceleration;
		}
		return vectors;
	}



	//// PRIVATE METHODS ////
	static void Track(Target target) {
		if (CameraBlocks.Count() == 0) return;
		
		Vector3D jitter = new Vector3D(Rand.NextDouble() - 0.5, Rand.NextDouble() - 0.5, Rand.NextDouble() - 0.5) * target.Size;
		Vector3D newPosition = target.PositionEstimate + jitter * TrackingJitterScale;
		
		IMyCameraBlock camera = CameraBlocks[currentCamera];
		double syncDelay = target.Distance / 2000.0 / CameraBlocks.Count;
		if ((DateTime.Now - lastScan).TotalSeconds < syncDelay) return;
		
		Target newTarget = new Target(camera.Raycast(camera.AvailableScanRange, Base.VectorToBlockSpace(newPosition, camera)));
		currentCamera = (currentCamera + 1) % CameraBlocks.Count;
		lastScan = DateTime.Now;
		if (newTarget.Id == target.Id) {
			target.Size = newTarget.Size;
			target.Position = newTarget.Position;
			target.Acceleration = (newTarget.Velocity - target.Velocity) / (newTarget.LastDetected - target.LastDetected).TotalSeconds;
			target.Velocity = newTarget.Velocity;
			target.LastDetected = DateTime.Now;
		}
	}
}
// TODO: use delegate instead of static method
static class DisplayControl {
	public static readonly DateTime Version = new DateTime(2021, 05, 11, 17, 38, 00);
	
	public static Color	ContentColor =				new Color(255, 64, 0);
	public static Color	BackgroundColor =			new Color(32, 8, 0);
	public static Color	SelectedContentColor =		new Color(255, 64, 0);
	public static Color	SelectedBackgroundColor =	new Color(64, 16, 0);
	public static double	TextScale =					2.0;
	public static double	TextPadding =				15.0;
	
	// Define which screen to jump to with directional input.
	// Defined for each direction of each screen of each cockpit.
	// Order: upIndex downIndex leftIndex rightIndex
	static readonly Dictionary<string, List<int[]>> ScreenJumpIndexes = new Dictionary<string, List<int[]>> {
		{"Cockpit", new List<int[]> {
			new int[] { 0, 3, 1, 2 },
			new int[] { 1, 3, 1, 0 },
			new int[] { 2, 3, 0, 2 },
			new int[] { 0, 3, 1, 2 }
		}},
		{"Fighter Cockpit", new List<int[]> {
			new int[] { 0, 1, 0, 0 },
			new int[] { 0, 3, 1, 2 },
			new int[] { 0, 3, 1, 2 },
			new int[] { 1, 4, 3, 3 },
			new int[] { 3, 4, 4, 5 },
			new int[] { 5, 5, 4, 5 }
		}}
	};
	
	class CockpitDisplayInputController {
		public bool Enabled = false;
		public IMyCockpit Cockpit = null;
		public List<List<string>> ScreenContent = new List<List<string>>();
		
		bool selectingContent = false;
		bool blockInput = false;
		int selectedScreen = 0;
		int selectedLine = 0;
		
		public CockpitDisplayInputController(IMyCockpit c) {
			Cockpit = c;
			for (int s = 0; s < Cockpit.SurfaceCount; s++) {
				ScreenContent.Add(new List<string>());
			}
		}
		
		public void Update() {
			
			// Draw screen content.
			for (int s = 0; s < Cockpit.SurfaceCount; s++) {
				IMyTextSurface display = Cockpit.GetSurface(s);
				if (Enabled && s == selectedScreen) {
					display.FontColor = SelectedContentColor;
					display.BackgroundColor = SelectedBackgroundColor;
					display.ScriptForegroundColor = SelectedContentColor;
					display.ScriptBackgroundColor = SelectedBackgroundColor;
				}
				else {
					display.FontColor = ContentColor;
					display.BackgroundColor = BackgroundColor;
					display.ScriptForegroundColor = ContentColor;
					display.ScriptBackgroundColor = BackgroundColor;
				}
				for (int l = 0; l < ScreenContent[s].Count; l++) {
					string content = ScreenContent[s][l];
					bool highlight = Enabled && selectingContent && s == selectedScreen && l == selectedLine;
					if (highlight) display.WriteText($"> {content}\n", true);
					else display.WriteText($"  {content}\n", true);
				}
			}
			
			if (Enabled == false) return;
			
			// Process input.
			Vector3D inputVector = Cockpit.MoveIndicator;
			bool pressedSelect =	inputVector.Y > 0.0;
			bool pressedReturn =	inputVector.Y < 0.0;
			bool pressedUp =		inputVector.Z < 0.0;
			bool pressedDown =		inputVector.Z > 0.0;
			bool pressedLeft =		inputVector.X < 0.0;
			bool pressedRight =		inputVector.X > 0.0;
			bool anyKeyDown = pressedSelect || pressedReturn || pressedUp || pressedDown || pressedLeft || pressedRight;
			
			if (blockInput) {
				if (anyKeyDown == false) blockInput = false;
			}
			else if (anyKeyDown) {
				blockInput = true;
				if (soundBlock != null) soundBlock.Play();
				
				if (selectingContent) {
					if (pressedReturn) {
						selectingContent = false;
						selectedLine = 0;
					}
					else if (pressedSelect) {
						string content = ScreenContent[selectedScreen][selectedLine];
						//Base.Program.Me.TryRun(content);
						OnCockpitDisplaySelected(content, Cockpit);
					}
					else {
						if (pressedUp) selectedLine--;
						if (pressedDown) selectedLine++;
						selectedLine = (int)Base.Mod((double)selectedLine, (double)ScreenContent[selectedScreen].Count);
					}
				}
				else {
					if (pressedReturn) Enabled = false;
					else if (pressedSelect) {
						if (ScreenContent[selectedScreen].Count != 0) selectingContent = true;
					}
					else {
						int jumpDirection = -1;
						if (pressedUp)		jumpDirection = 0;
						if (pressedDown)	jumpDirection = 1;
						if (pressedLeft)	jumpDirection = 2;
						if (pressedRight)	jumpDirection = 3;
						if (jumpDirection == -1) return;
						string cockpitType = Cockpit.DisplayNameText;
						selectedScreen = ScreenJumpIndexes[cockpitType][selectedScreen][jumpDirection];
					}
				}
			}
		}
	}
	
	static Dictionary<IMyCockpit, int> screenSelectionStates = new Dictionary<IMyCockpit, int>();
	static List<CockpitDisplayInputController> cockpitDisplayControllers = new List<CockpitDisplayInputController>();
	static IMySoundBlock soundBlock = null;
	
	// Remember to call this first
	public static void Init() {
		Base.Program.Echo("Initializing DisplayControl");
		Base.Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		soundBlock = Base.GridBlocks.OfType<IMySoundBlock>().Where(b => b.CustomData.Contains("DisplayInputSound")).FirstOrDefault();
		if (soundBlock != null) soundBlock.LoopPeriod = 0.08f;
		Base.Program.Echo("    SoundBlock: " + (soundBlock != null));
		SetupDisplays();
		
		foreach (var cockpit in Base.GridBlocks.OfType<IMyCockpit>()) {
			cockpitDisplayControllers.Add(new CockpitDisplayInputController(cockpit));
		}
	}
	
	public static void EnableControls(IMyCockpit cockpit) {
		var controller = cockpitDisplayControllers.Where(c => c.Cockpit == cockpit).FirstOrDefault();
		if (controller != null) controller.Enabled = true;
	}
	
	public static void SetScreenOptions(IMyCockpit cockpit, int displayIndex, List<string> options) {
		if (displayIndex >= cockpit.SurfaceCount) return;
		var controller = cockpitDisplayControllers.Where(c => c.Cockpit == cockpit).FirstOrDefault();
		if (controller == null) return;
		controller.ScreenContent[displayIndex] = options;
	}
	
	public static void DrawCockpitInterface() {
		foreach (var controller in cockpitDisplayControllers) {
			controller.Update();
		}
	}
	
	public static void Print(IMyTextSurfaceProvider block, int screenIndex, string text) {
		if (block == null) return;
		if (screenIndex >= block.SurfaceCount) return;
		IMyTextSurface display = block.GetSurface(screenIndex);
		display.WriteText(text + "\n", true);
	}
	
	public static void ClearDisplays() {
		foreach (var block in Base.ConstructBlocks.OfType<IMyTextSurfaceProvider>()) {
			for (int i = 0; i < block.SurfaceCount; i++) {
				block.GetSurface(i).WriteText("");
			}
		}
	}
	
	static void SetupDisplays() {
		foreach (var block in Base.ConstructBlocks.OfType<IMyTextSurfaceProvider>()) {
			if (block == Base.Program.Me) continue;
			bool isCockpit = block is IMyCockpit;
			for (int i = 0; i < block.SurfaceCount; i++) {
				IMyTextSurface display = block.GetSurface(i);
				display.FontColor = ContentColor;
				display.BackgroundColor = BackgroundColor;
				display.ScriptForegroundColor = ContentColor;
				display.ScriptBackgroundColor = BackgroundColor;
				
				if (isCockpit && i == 0) continue; // keep artificial horizon for cockpits
				
				display.ContentType = ContentType.TEXT_AND_IMAGE;
				display.Alignment = TextAlignment.LEFT;
				display.PreserveAspectRatio = true;
				display.Font = "Monospace";
				display.TextPadding = (float)TextPadding;
				display.FontSize = (float)TextScale;
				display.WriteText(string.Join("\n",
					$"SCREEN {i}",
					$"{display.SurfaceSize.X.ToString("0")}x{display.SurfaceSize.Y.ToString("0")}"
				));
			}
		}
	}
}
static class RoverControl {
	public static readonly DateTime Version = new DateTime(2022, 07, 17, 00, 00, 00);



	//// PUBLIC SETTINGS ////
	public static double MouseSensitivity = 0.03;



	//// PUBLIC DATA ////
	public static MyGridProgram Program;
	public static IMyShipController Controller = null;
	public static List<IMyMotorSuspension> Wheels = new List<IMyMotorSuspension>();
	public static List<IMyRemoteControl> Remotes = new List<IMyRemoteControl>();



	//// PUBLIC METHODS ////
	// Remember to call this first
	public static void Init(MyGridProgram program, HashSet<IMyTerminalBlock> blocks) {
		Program = program;
		Program.Echo("Initializing ShipControl");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		Controller = blocks.OfType<IMyShipController>().Where(s => s.IsMainCockpit).FirstOrDefault();
		if (Controller == null) Controller = blocks.OfType<IMyShipController>().FirstOrDefault();
		Program.Echo("    Controller: " + (Controller == null ? "none" : Controller.CustomName));
		if (Controller == null) Program.Echo("      WARNING: No cockpit or remote control!");
		
		Remotes = blocks.OfType<IMyRemoteControl>().ToList();
		Program.Echo("    Remotes: " + Remotes.Count);
		Wheels = blocks.OfType<IMyMotorSuspension>().ToList();
		Program.Echo("    Wheels: " + Wheels.Count);
	}
	public static void Drive() {
		if (Controller == null) Base.Throw("Ship has no controller");
		
		var input = GetPilotInput();
		Vector3D forwardDirection = Controller.WorldMatrix.Forward;
		Vector3D rightDirection = Controller.WorldMatrix.Right;
		foreach (IMyMotorSuspension wheel in Wheels) {
			Vector3D wheelDirection = wheel.WorldMatrix.Up;
			Vector3D wheelPosition = wheel.GetPosition() - Controller.GetPosition();
			bool isRightWheel = Vector3D.Dot(rightDirection, wheelDirection) > 0.0;
			bool isFrontWheel = Vector3D.Dot(forwardDirection, wheelPosition) > 0.0;
			double steering = Controller.MoveIndicator.X;
			double throttle = Controller.MoveIndicator.Z;
			wheel.SteeringOverride = (float)(isFrontWheel ? steering : -steering);
			wheel.PropulsionOverride = (float)(isRightWheel ? throttle : -throttle);
		}
		foreach (var remote in Remotes) {
			remote.HandBrake = Controller.HandBrake;
		}
	}
	
	public static List<Vector3D> GetPilotInput() {
		List<Vector3D> vectors = new List<Vector3D>{ Vector3D.Zero, Vector3D.Zero };
		if (Controller == null) return vectors;
		double pitch = Base.Clamp(-Controller.RotationIndicator.X * MouseSensitivity, -1.0, 1.0);
		double roll = Base.Clamp(-Controller.RotationIndicator.Y * MouseSensitivity, -1.0, 1.0);
		double yaw = -Controller.RollIndicator;
		vectors[0] = Base.DirectionToWorldSpace(Controller.MoveIndicator, Controller);
		vectors[1] = Base.DirectionToWorldSpace(new Vector3D(pitch, roll, yaw), Controller);
		return vectors;
	}
}

