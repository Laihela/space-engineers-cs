// DefenseSystem.cs
// Written by Laihela

Program () {
	Echo(""); // Clear output
	Base.Init(this);
	Util.Init(this);
	Comms.Init(this);
	Sensors.Init(this);
	RotorTurret.Init(this);
	TargetTracking.Init(this);
	SafeZoneTracking.Init(this);
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	
	Me.GetSurface(0).FontSize = 1.2F;
	Comms.AddReceiver("Target");
	Comms.AddReceiver("SafeZone");
	//Comms.AntennaPulseRange = 1000.0; // Automatically set antenna range to 1 meter when not transmitting to avoid detection.
	Sensors.PreventDetectorTurretFiring = true; // Preseves ammo for vanilla turret blocks, empty turrets don't detect targets.
	//RotorTurret.RippleFireRate = 4.0; // Rounds per second, useful for rocket turrets. Leave to zero for max fire rate.
	RotorTurret.MaxDeviation = 4.0; // How many meters the aim can be off-target before not firing. Lower values conserve ammo.
	RotorTurret.MaxRange = 800.0; // Turret won't fire at targets beyond this distance.
	TargetTracking.MissingTargetExpireTime = 2.0F; // Forgets the target if it stays undetected for longer than this. In seconds.
	//TargetTracking.PingEnabled = true; // Enables ping functionality. Consumes more power.
}

void Main(string command) {
	Base.Title("Defence Network");
	Comms.Update();
	RotorTurret.Update();
	TargetTracking.Update();
	SafeZoneTracking.Update();
	
	Base.Print("Rang " + TargetTracking.GetRange().ToString("000000.00") + " m");
	if (command == "Ping") TargetTracking.Ping(); // TEST THIS PLZ
	
	foreach (string zoneData in Comms.ReadAll("SafeZone")) SafeZoneTracking.AddZone(zoneData);
	Comms.Transmit("SafeZone", SafeZoneTracking.MyZone.Serialize());
	
	Base.Print("Comrades: " + SafeZoneTracking.SafeZones.Count());
	
	TargetTracking.ConsiderTarget(Sensors.Detect());
	TargetTracking.ConsiderTarget(Comms.Read("Target"));
	if (TargetTracking.CurrentTarget != null) {
		Base.Print("Tracking target");
		Base.Print("Size " + (TargetTracking.CurrentTarget.Size).ToString("0000.00") + " m");
		Base.Print("Dist " + (TargetTracking.CurrentTarget.Distance).ToString("0000.00") + " m");
		Base.Print("Vel  " + (TargetTracking.CurrentTarget.Velocity.Length()).ToString("0000.00") + " m/s");
		Base.Print("Scan " + (TargetTracking.CurrentTarget.BlindTime * 1e3).ToString("0000.00") + " ms");
		List<Vector3D> vectors = TargetTracking.GetVectors();
		RotorTurret.Target(vectors);
		if (TargetTracking.CurrentTarget.BlindTime < 0.5) {
			Comms.Transmit("Target", TargetTracking.CurrentTarget.Serialize());
			foreach (var zone in SafeZoneTracking.SafeZones) {
				if (RotorTurret.SphereIsInLineOfFire(zone.Position, zone.Size)) return;
			}
			RotorTurret.FireWeapons(vectors);
		}
	}
	else {
		Base.Print("No target");
		RotorTurret.Target(null);
	}
	
	Sensors.LateUpdate();
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

static class Comms {
	public static readonly DateTime Version = new DateTime(2020, 10, 20, 23, 47, 00);
	public static MyGridProgram Program;
	public static List<IMyRadioAntenna> AntennaBlocks;
	public static Dictionary<string, IMyBroadcastListener> Receivers = new Dictionary<string, IMyBroadcastListener>();
	public static double AntennaPulseRange = 0.0;
	
	static DateTime lastTransmission = DateTime.Now;
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing Comms");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		AntennaBlocks = Base.GroupBlocks.OfType<IMyRadioAntenna>().ToList();
	}
	
	public static void Update() {
		if (AntennaPulseRange > 0.0) SetAntennaRange(1.0);
	}
	
	public static void AddReceiver(string name) {
		foreach (string key in Receivers.Keys) if (key == name) {
			Program.Echo($"Comms.AddReceiver: Receiver with name {name} already exists!");
			return;
		}
		Receivers.Add(name, Program.IGC.RegisterBroadcastListener(name));
	}
	
	static void SetAntennaRange(double range) {
		foreach (var antenna in AntennaBlocks) antenna.SetValue<Single>("Radius", (Single)range);
	}
	
	public static void Transmit(string channel, string data) {
		if (AntennaPulseRange > 0.0) {
			SetAntennaRange(AntennaPulseRange);
			lastTransmission = DateTime.Now;
		}
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
	
	public static List<string> ReadAll(string channel) {
		List<string> messages = new List<string>();
		foreach (string key in Receivers.Keys) {
			if (key == channel) {
				IMyBroadcastListener listener = Receivers[channel];
				if (!listener.HasPendingMessage) continue;
				while (listener.HasPendingMessage) messages.Add(listener.AcceptMessage().Data.ToString());
			}
		}
		return messages;
	}
}

static class Sensors {
	public static readonly DateTime Version = new DateTime(2020, 10, 20, 15, 01, 00);
	public static MyGridProgram Program;
	public static List<IMySensorBlock> SensorBlocks;
	public static List<IMyLargeTurretBase> TurretBlocks;
	public static bool PreventDetectorTurretFiring = false;
	
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
	
	public static void LateUpdate() {
		foreach (IMyLargeTurretBase turret in TurretBlocks) {
			turret.Enabled = turret.GetTargetedEntity().Position == Vector3D.Zero || !PreventDetectorTurretFiring;
		}
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
	public static readonly DateTime Version = new DateTime(2021, 02, 24, 14, 28, 00);
	public static MyGridProgram Program;
	public static IMyShipController Controller;
	public static List<IMyMotorStator> RotorBlocks;
	public static List<IMyUserControllableGun> WeaponBlocks;
	public static int TrajectoryCalculations = 64;
	public static double MaxRange = 800.0;
	public static double MaxDeviation = 2.0;
	public static double RippleFireRate = 0.0;
	public static double AimCorrectionRate = 16.0;
	public static double ProjectileVelocity = 400.0; // Rocket = 198, Gatling = 400
	
	static DateTime lastUpdate = DateTime.Now;
	static DateTime RippleLastFire = DateTime.Now;
	static Vector3D shipAngularVelocity = Vector3D.Zero;
	static Vector3D shipLinearVelocity = Vector3D.Zero;
	static Vector3D shipAcceleration = Vector3D.Zero;
	static int RippleWeaponIndex = 0;
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing RotorTurret");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		foreach (var block in Base.GroupBlocks.OfType<IMyShipController>()) Controller = block;
		Program.Echo("    ShipController: " + (Controller == null ? "no" : "yes"));
		RotorBlocks = Base.GroupBlocks.OfType<IMyMotorStator>().ToList();
		WeaponBlocks = Base.GroupBlocks.OfType<IMyUserControllableGun>().ToList();
		foreach(var turret in WeaponBlocks.OfType<IMyLargeTurretBase>().ToList()) WeaponBlocks.Remove(turret);
		Program.Echo("    RotorBlocks: " + RotorBlocks.Count);
		Program.Echo("    WeaponBlocks: " + WeaponBlocks.Count);
		if (WeaponBlocks.Count > 0 && WeaponBlocks[0] is IMySmallMissileLauncher) ProjectileVelocity = 198.0;
	}
	
	public static void Update() {
		double deltaTime = (DateTime.Now - lastUpdate).TotalSeconds;
		
		if (Controller != null) {
			shipAngularVelocity = Controller.GetShipVelocities().AngularVelocity;
			Vector3D currentVelocity = Controller.GetShipVelocities().LinearVelocity;
			shipAcceleration = (currentVelocity - shipLinearVelocity) * deltaTime;
			shipLinearVelocity = currentVelocity;
		}
		
		lastUpdate = DateTime.Now;
	}
	
	public static void Target(List<Vector3D> vectors) {
		
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
		Vector3D velocity = vectors[1] - shipLinearVelocity;
		Vector3D acceleration = vectors[2] - shipAcceleration;
		
		foreach (IMyMotorStator rotor in RotorBlocks) {
			List<string> data = rotor.CustomData.Split('\n').ToList();
			while (data.Count() < 3) data.Add("");
			double manualOffsetAngle = 0.0;
			double.TryParse(data[0], out manualOffsetAngle);
			manualOffsetAngle *= Math.PI / 180.0;
			double manualOffsetPosition = 0.0; // TODO: Take rotation into account!!
			double.TryParse(data[1], out manualOffsetPosition);
			bool overrideVeocityCorrection = false;
			bool.TryParse(data[2], out overrideVeocityCorrection);
			
			Vector3D offset = position - rotor.GetPosition();
			Vector3D lead = CalculateLead(offset, velocity, acceleration);
			Vector3D aim = Util.DirectionToBlockSpace(offset + lead, rotor) + MatrixD.CreateRotationY(manualOffsetAngle - rotor.Angle).Right * manualOffsetPosition;
			double deviation = Math.Atan2(-aim.X, aim.Z) - rotor.Angle + manualOffsetAngle;
			if (deviation < -Math.PI) deviation += Math.PI * 2;
			double distance = offset.Length();
			double angularVelocity = 0.0;
			if (overrideVeocityCorrection == false) {
				angularVelocity = Util.DirectionToBlockSpace(Vector3D.Cross((velocity + acceleration) / distance, offset / distance) + shipAngularVelocity, rotor).Y;
			}
			Util.SetRotorVelocity(rotor, deviation * AimCorrectionRate + angularVelocity);
		}
	}
	
	public static void FireWeapons(List<Vector3D> vectors) {
		
		while (vectors.Count < 3) vectors.Add(Vector3D.Zero);
		
		Vector3D position = vectors[0];
		Vector3D velocity = vectors[1] - shipLinearVelocity;
		Vector3D acceleration = vectors[2] - shipAcceleration;
		
		if (WeaponBlocks.Count() == 0) return;
		if (RippleFireRate > 0.0) {
			if ((DateTime.Now - RippleLastFire).TotalSeconds < 1.0 / RippleFireRate) return;
			IMyUserControllableGun weapon = WeaponBlocks[RippleWeaponIndex];
			Vector3D offset = position - weapon.GetPosition();
			Vector3D lead = CalculateLead(offset, velocity, acceleration);
			Vector3D aim = Util.DirectionToBlockSpace(offset + lead, weapon);
			double deviation = new Vector2D(aim.X, aim.Y).Length();
			if (deviation < MaxDeviation && aim.Length() < MaxRange) {
				weapon.ApplyAction("ShootOnce");
				RippleWeaponIndex = ++RippleWeaponIndex % WeaponBlocks.Count();
				RippleLastFire = DateTime.Now;
			}
		}
		else {
			foreach (IMyUserControllableGun weapon in WeaponBlocks) {
				Vector3D offset = position - weapon.GetPosition();
				Vector3D lead = CalculateLead(offset, velocity, acceleration);
				Vector3D aim = Util.DirectionToBlockSpace(offset + lead, weapon);
				double deviation = new Vector2D(aim.X, aim.Y).Length();
				if (deviation < MaxDeviation && aim.Length() < MaxRange) weapon.ApplyAction("ShootOnce");
			}
		}
	}
	
	public static bool SphereIsInLineOfFire(Vector3D position, double size) {
		foreach(var weapon in WeaponBlocks) {
			Vector3D localPosition = Util.VectorToBlockSpace(position, weapon);
			double zoneDeviation = new Vector2D(localPosition.X, localPosition.Y).Length();
			if (zoneDeviation < size + MaxDeviation) return true;
		}
		return false;
	}
	
	// Returns correct lead vector based on target vectors and ProjectileVelocity
	// Higher TrajectoryCalculations increases accuracy at the cost of performance, but with diminishing returns
	static Vector3D CalculateLead(Vector3D position, Vector3D velocity, Vector3D acceleration) {
		double time = 0;
		Vector3D lead = Vector3D.Zero;
		for (int i = 0; i < TrajectoryCalculations; i++) {
			time = (position + lead).Length() / ProjectileVelocity;
			lead = velocity * time + acceleration * Math.Pow(time, 2) * 0.5;
		}
		return lead;
	}
}

static class TargetTracking {
	public static readonly DateTime Version = new DateTime(2021, 02, 24, 17, 26, 00);
	public static MyGridProgram Program;
	public static List<IMyCameraBlock> CameraBlocks;
	public static double MissingTargetExpireTime = 3.0;
	public static double NewTargetPriorityBias = 0.95;
	public static bool PingEnabled = false;
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
		if (CurrentTarget == null) {
			foreach (var camera in CameraBlocks) camera.EnableRaycast = PingEnabled;
			return;
		}
		foreach (var camera in CameraBlocks) camera.EnableRaycast = true;
		track(CurrentTarget);
		if (CurrentTarget.BlindTime > MissingTargetExpireTime) CurrentTarget = null;
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
	
	static void track(Target target) {
		if (CameraBlocks.Count() == 0) return;
		
		Vector3D jitter = new Vector3D(Rand.NextDouble() - 0.5, Rand.NextDouble() - 0.5, Rand.NextDouble() - 0.5) * target.Size;
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

static class SafeZoneTracking {
	public static readonly DateTime Version = new DateTime(2020, 10, 20, 22, 05, 00);
	public static MyGridProgram Program;
	public static List<SafeZone> SafeZones = new List<SafeZone>();
	public static double MissingZoneExpireTime = 3.0;
	
	public static SafeZone MyZone {
		get {
			SafeZone myZone = new SafeZone();
			IMyCubeGrid myGrid = Program.Me.CubeGrid;
			Vector3D min = myGrid.GridIntegerToWorld(myGrid.Min);
			Vector3D max = myGrid.GridIntegerToWorld(myGrid.Max);
			myZone.Id = myGrid.EntityId;
			myZone.Size = (max - min).AbsMin();
			myZone.Position = (min + max) / 2.0;
			return myZone;
		}
	}
	
	public class SafeZone {
		public long Id = 0;
		public double Size = 0.0;
		public Vector3D Position = Vector3D.Zero;
		public DateTime LastDetected = DateTime.Now;
		
		public double BlindTime {
			get { return (DateTime.Now - LastDetected).TotalSeconds; }
		}
		public double Distance {
			get { return (Position - Program.Me.GetPosition()).Length(); }
		}
		public bool IsValid {
			get { return Size != 0; }
		}
		
		public SafeZone() {}
		
		public SafeZone(MyDetectedEntityInfo entityInfo) {
			Id = entityInfo.EntityId;
			Size = (entityInfo.BoundingBox.Max - entityInfo.BoundingBox.Min).AbsMin();
			Position = entityInfo.BoundingBox.Center;
		}
		
		public string Serialize() {
			return string.Join(":",
				Id,
				Size,
				Position.X,
				Position.Y,
				Position.Z,
				LastDetected.Ticks
			);
		}
		
		public static SafeZone Deserialize(string data) {
			SafeZone zone = new SafeZone();
			string[] values = data.Split(new char[]{':'}, StringSplitOptions.RemoveEmptyEntries);
			if (values.Count() != 6) {
				Program.Echo($"SafeZoneTracking.SafeZone.Deserialize: incorrect data format! ({values.Count()} values)");
				return zone;
			}
			zone.Id = long.Parse(values[0]);
			zone.Size = double.Parse(values[1]);
			zone.Position.X = double.Parse(values[2]);
			zone.Position.Y = double.Parse(values[3]);
			zone.Position.Z = double.Parse(values[4]);
			zone.LastDetected = new DateTime(long.Parse(values[5]));
			return zone;
		}
	}
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing SafeZoneTracking");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		//Blocks = Base.GroupBlocks.OfType<BlockType>().ToList();
		//Program.Echo("    Blocks: " + Blocks.Count);
	}
	
	public static void Update() {
		foreach (var zone in new List<SafeZone>(SafeZones)) {
			if (zone.BlindTime > MissingZoneExpireTime) SafeZones.Remove(zone);
		}
	}
	
	public static void AddZone(object zone) {
		SafeZone newZone;
		if (zone is SafeZone) newZone = (SafeZone)zone;
		else if (zone is MyDetectedEntityInfo) newZone = new SafeZone((MyDetectedEntityInfo)zone);
		else if (zone is string) {
			string data = (string)zone;
			if (data == "") newZone = new SafeZone();
			else newZone = SafeZone.Deserialize(data);
		}
		else {
			Program.Echo("SafeZoneTracking.AddZone: invalid zone type: " + zone.GetType());
			return;
		}
		if (newZone.IsValid == false) return;
		if (newZone.Id == MyZone.Id) return;
		foreach (SafeZone existingZone in SafeZones) {
			if (existingZone.Id == newZone.Id) {
				existingZone.Size = newZone.Size;
				existingZone.Position = newZone.Position;
				existingZone.LastDetected = newZone.LastDetected;
				return;
			}
		}
		SafeZones.Add(newZone);
	}
}

