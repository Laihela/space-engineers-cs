Program () {
	Echo("");
	Base.Init(this);
	Util.Init(this);
	Comms.Init(this);
	Sensors.Init(this);
	RotorTurret.Init(this);
	TargetTracking.Init(this);
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	
	Comms.AddReceiver("Target");
}

void Main() {
	Base.Title("Raydar");
	TargetTracking.Update();
	
	TargetTracking.ConsiderTarget(Sensors.Detect());
	TargetTracking.ConsiderTarget(Comms.Read("Target"));
	if (TargetTracking.CurrentTarget == null) {
		Base.Print("No target");
		RotorTurret.Target(null);
	}
	else {
		double timeUndetected = (DateTime.Now - TargetTracking.CurrentTarget.LastDetected).TotalSeconds;
		Base.Print("Tracking");
		Base.Print("Last seen " + timeUndetected.ToString("0.00") + " s");
		Base.Print("Range " + (TargetTracking.CurrentTarget.Distance / 1e3).ToString("0.00") + " km");
		Base.Print("Speed " + TargetTracking.CurrentTarget.Velocity.Length().ToString("0.00") + " m/s");
		Base.Print("Size " + TargetTracking.CurrentTarget.Size.ToString("0.00") + " m");
		Base.Print("X " + TargetTracking.CurrentTarget.Position.X.ToString("0.0"));
		Base.Print("Y " + TargetTracking.CurrentTarget.Position.Y.ToString("0.0"));
		Base.Print("Z " + TargetTracking.CurrentTarget.Position.Z.ToString("0.0"));
		RotorTurret.Target(new List<Vector3D>{TargetTracking.CurrentTarget.PositionEstimate});
		if (timeUndetected < 0.5) Comms.Transmit("Target", TargetTracking.CurrentTarget.Serialize());
	}
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
	public static readonly DateTime Version = new DateTime(2020, 10, 14, 22, 31, 00);
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
	public static double Clamp(double value, double min, double max) {
		return value < min ? min : (value > max ? max : value);
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
	
	public static void SetRotorVelocity(IMyMotorStator rotor, double velocity) {
		double rpm = velocity * 30.0 / Math.PI;
		rotor.SetValue<float>("Velocity", (float)Clamp(rpm, -60.0, Math.PI * 60.0));
	}
}

static class Comms {
	public static readonly DateTime Version = new DateTime(2020, 10, 14, 19, 50, 00);
	public static MyGridProgram Program;
	public static Dictionary<string, IMyBroadcastListener> Receivers = new Dictionary<string, IMyBroadcastListener>();
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing Comms");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
	}
	
	public static void AddReceiver(string name) {
		foreach (string key in Receivers.Keys) if (key == name) {
			Program.Echo($"Comms.AddReceiver: Receiver with name {name} already exists!");
			return;
		}
		Receivers.Add(name, Program.IGC.RegisterBroadcastListener(name));
	}
	
	public static void Transmit(string channel, string data) {
		Program.IGC.SendBroadcastMessage(channel, data, TransmissionDistance.TransmissionDistanceMax);
	}
	
	public static string Read(string channel) {
		string message = "";
		foreach (string key in Receivers.Keys) {
			if (key == channel) {
				IMyBroadcastListener listener = Receivers[channel];
				if (!listener.HasPendingMessage) continue;
				while (listener.HasPendingMessage) message = listener.AcceptMessage().Data.ToString();
			}
		}
		return message;
	}
}

static class Sensors {
	public static readonly DateTime Version = new DateTime(2020, 10, 14, 21, 44, 00);
	public static MyGridProgram Program;
	public static List<IMySensorBlock> SensorBlocks;
	public static List<IMyLargeTurretBase> TurretBlocks;
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing Sensors");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		SensorBlocks = Base.GroupBlocks.OfType<IMySensorBlock>().ToList();
		Program.Echo("    SensorBlocks: " + SensorBlocks.Count());
		TurretBlocks = Base.GroupBlocks.OfType<IMyLargeTurretBase>().ToList();
		Program.Echo("    TurretBlocks: " + TurretBlocks.Count());
	}
	
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

static class RotorTurret {
	public static readonly DateTime Version = new DateTime(2020, 10, 14, 22, 31, 00);
	public static MyGridProgram Program;
	public static List<IMyMotorStator> RotorBlocks;
	public static List<IMyUserControllableGun> WeaponBlocks;
	public static int TrajectoryCalculations = 16;
	public static double MaxRange = 800.0;
	public static double MaxDeviation = 1.0;
	public static double AimCorrectionRate = 16.0;
	public static double ProjectileVelocity = 400.0; // Rocket = 200, Gatling = 400
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing RotorTurret");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		RotorBlocks = Base.GroupBlocks.OfType<IMyMotorStator>().ToList();
		Program.Echo("    RotorBlocks: " + RotorBlocks.Count);
		WeaponBlocks = Base.GroupBlocks.OfType<IMyUserControllableGun>().ToList();
		Program.Echo("    WeaponBlocks: " + WeaponBlocks.Count);
		if (WeaponBlocks.Count > 0 && WeaponBlocks[0] is IMySmallMissileLauncher) ProjectileVelocity = 200;
	}
	
	// Returns correct lead vector based on target vectors and ProjectileVelocity
	// Higher TrajectoryCalculations increases accuracy at the cost of performance, but with diminishing returns
	public static Vector3D CalculateLead(Vector3D position, Vector3D velocity, Vector3D acceleration) {
		double time = 0;
		Vector3D lead = Vector3D.Zero;
		for (int i = 0; i < TrajectoryCalculations; i++) {
			time = (position + lead).Length() / ProjectileVelocity;
			lead = velocity * time + acceleration * Math.Pow(time, 2) * 0.5;
		}
		return lead;
	}
	
	public static void Target (List<Vector3D> vectors, bool fireWeapons = false) {
		if (vectors == null) {
			foreach (IMyMotorStator rotor in RotorBlocks) {
				double deviation = -rotor.Angle;
				if (deviation < -Math.PI) deviation += Math.PI * 2;
				Util.SetRotorVelocity(rotor, deviation * AimCorrectionRate);
			}
			return;
		}
		
		while (vectors.Count < 3) vectors.Add(Vector3D.Zero);
		
		Vector3D position = vectors[0];
		Vector3D velocity = vectors[1];
		Vector3D acceleration = vectors[2];
		
		foreach (IMyMotorStator rotor in RotorBlocks) {
			Vector3D offset = position - rotor.GetPosition();
			Vector3D lead = CalculateLead(offset, velocity, acceleration);
			Vector3D aim = Util.DirectionToBlockSpace(offset + lead, rotor);
			double deviation = Math.Atan2(-aim.X, aim.Z) - rotor.Angle;
			if (rotor.CustomData != "") deviation += double.Parse(rotor.CustomData) / 180.0 * Math.PI;
			if (deviation < -Math.PI) deviation += Math.PI * 2;
			double distance = offset.Length();
			double angularVelocity = Vector3D.Cross((velocity + acceleration) / distance, offset / distance).Y;
			Util.SetRotorVelocity(rotor, deviation * AimCorrectionRate + angularVelocity);
		}
		
		if (fireWeapons == false) return;
		
		foreach (IMyUserControllableGun weapon in WeaponBlocks) {
			Vector3D offset = position - weapon.GetPosition();
			Vector3D lead = CalculateLead(offset, velocity, acceleration);
			Vector3D aim = Util.DirectionToBlockSpace(offset + lead, weapon);
			double deviation = new Vector2D(aim.X, aim.Y).Length();
			if (deviation < MaxDeviation && aim.Length() < MaxRange) weapon.ApplyAction("ShootOnce");
		}
	}
}

static class TargetTracking {
	public static readonly DateTime Version = new DateTime(2020, 10, 14, 22, 53, 00);
	public static MyGridProgram Program;
	public static List<IMyCameraBlock> CameraBlocks;
	public static double MissingTargetExpireTime = 3.0;
	public static double NewTargetPriorityBias = -10.0;
	public static Target CurrentTarget = null;
	
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
		
		public double Distance {
			get { return (Position - Program.Me.GetPosition()).Length(); }
		}
		public double Priority {
			get { return Distance / Size; }
		}
		public bool IsValid {
			get { return Size != 0; }
		}
		public Vector3D PositionEstimate {
			get {
				double time = (DateTime.Now - LastDetected).TotalSeconds;
				return Position + Velocity * time + Acceleration * time * time * 0.5;
			}
		}
		public Vector3D VelocityEstimate {
			get {
				double time = (DateTime.Now - LastDetected).TotalSeconds;
				return Velocity + Acceleration * time;
			}
		}
		
		public Target(){}
		
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
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing TargetTracking");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		CameraBlocks = Base.GroupBlocks.OfType<IMyCameraBlock>().ToList();
		Program.Echo("    CameraBlocks: " + CameraBlocks.Count);
		foreach(IMyCameraBlock camera in CameraBlocks) camera.EnableRaycast = true;
	}
	
	public static void Update() {
		if (CurrentTarget == null) return;
		track(CurrentTarget);
		double timeUndetected = (DateTime.Now - CurrentTarget.LastDetected).TotalSeconds;
		if (timeUndetected > MissingTargetExpireTime) CurrentTarget = null;
	}
	
	public static List<Vector3D> GetVectors() {
		List<Vector3D> vectors = new List<Vector3D> {Vector3D.Zero, Vector3D.Zero, Vector3D.Zero};
		if (CurrentTarget != null) {
			vectors[0] = CurrentTarget.Position;
			vectors[1] = CurrentTarget.Velocity;
			vectors[2] = CurrentTarget.Acceleration;
		}
		return vectors;
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
			Program.Echo("ConsiderTarget: invalid target type: " + target.GetType());
			return;
		}
		if (newTarget.IsValid == false) return;
		if (CurrentTarget == null || CurrentTarget.Id == newTarget.Id) CurrentTarget = newTarget;
		if (newTarget.Priority + NewTargetPriorityBias > CurrentTarget.Priority) CurrentTarget = newTarget;
	}
	
	public static void Ping() {
		if (CameraBlocks.Count() == 0) return;
		IMyCameraBlock camera = CameraBlocks[currentCamera];
		ConsiderTarget(camera.Raycast(camera.AvailableScanRange));
		currentCamera = (currentCamera + 1) % CameraBlocks.Count;
	}
	
	static void track(Target target) {
		if (CameraBlocks.Count() == 0) return;
		
		Vector3D jitter = new Vector3D(Rand.NextDouble() - 0.5, Rand.NextDouble() - 0.5, Rand.NextDouble() - 0.5) * target.Size;
		Base.Print(jitter.Length().ToString("0.0"));
		Vector3D newPosition = target.PositionEstimate + jitter;
		
		IMyCameraBlock camera = CameraBlocks[currentCamera];
		double syncDelay = target.Distance / 2000.0 / CameraBlocks.Count;
		if ((DateTime.Now - lastScan).TotalSeconds < syncDelay) return;
		
		Target newTarget = new Target(camera.Raycast(camera.AvailableScanRange, Util.VectorToBlockSpace(newPosition, camera)));
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

