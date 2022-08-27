// MissileController.cs
// Written by Laihela

Program () {
	Echo(""); // Clear output
	Base.Init(this);
	TargetTracking.Init(this);
	MissileControl.Init(this);
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	
	// Change settings
	Me.GetSurface(0).FontSize = 1.2F;
	TargetTracking.PingEnabled = true;
	TargetTracking.TrackingJitterScale = 0.3;
	MissileControl.MaxAngularVelocity = 20.0;
	MissileControl.ControlResponsiveness = 4.0;
	MissileControl.DetonationProximity = 5.0;
}

void Main(string command) {
	Base.Title("Missile");
	
	if (command == "Ping") TargetTracking.Ping();
	MissileControl.TargetVectors = TargetTracking.GetVectors();
	
	TargetTracking.Update();
	MissileControl.Update();
}



static class Base {
	public static readonly DateTime Version = new DateTime(2021, 05, 05, 17, 18, 00);
	public static MyGridProgram Program;
	public static List<IMyTerminalBlock> GroupBlocks = new List<IMyTerminalBlock>();
	public static List<IMyTerminalBlock> GridBlocks = new List<IMyTerminalBlock>();
	
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
		Program.GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(GridBlocks, b => b.CubeGrid == Program.Me.CubeGrid);
		Program.Echo("    GroupBlocks: " + GroupBlocks.Count);
		Program.Echo("    GridBlocks: " + GridBlocks.Count);
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

static class TargetTracking {
	public static readonly DateTime Version = new DateTime(2021, 05, 05, 21, 04, 00);
	public static MyGridProgram Program;
	public static List<IMyCameraBlock> CameraBlocks;
	public static double MissingTargetExpireTime = 3.0;
	public static double NewTargetPriorityBias = 0.95;
	public static double TrackingJitterScale = 1.0;
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
		//CameraBlocks = Base.GroupBlocks.OfType<IMyCameraBlock>().ToList();
		CameraBlocks = Base.GridBlocks.OfType<IMyCameraBlock>().ToList();
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

static class MissileControl {
	public static readonly DateTime Version = new DateTime(2021, 05, 05, 20, 40, 00);
	public static MyGridProgram Program;
	public static List<IMyGyro> GyroBlocks;
	public static List<IMyWarhead> WarheadBlocks;
	public static List<IMyThrust> ThrusterBlocks;
	public static List<IMyCameraBlock> CameraBlocks;
	public static double MaxAngularVelocity = 15.0;
	public static double ControlResponsiveness = 3.0;
	public static double WarheadArmDistance = 100.0;
	public static double DetonationProximity = 10.0;
	
	public static List<Vector3D> TargetVectors = new List<Vector3D>();
	
	static bool deployed = false;
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing MissileControl");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		GyroBlocks = Base.GridBlocks.OfType<IMyGyro>().ToList();
		CameraBlocks = Base.GridBlocks.OfType<IMyCameraBlock>().ToList();
		WarheadBlocks = Base.GridBlocks.OfType<IMyWarhead>().ToList();
		ThrusterBlocks = Base.GridBlocks.OfType<IMyThrust>().ToList();
		Program.Echo("    GyroBlocks: " + GyroBlocks.Count);
		Program.Echo("    CameraBlocks: " + CameraBlocks.Count);
		Program.Echo("    WarheadBlocks: " + WarheadBlocks.Count);
		Program.Echo("    ThrusterBlocks: " + ThrusterBlocks.Count);
	}
	
	public static void Update() {
		
		while (TargetVectors.Count < 3) TargetVectors.Add(Vector3D.Zero);
		Vector3D position = TargetVectors[0];
		//Vector3D velocity = TargetVectors[1];
		//Vector3D acceleration = TargetVectors[2];
		
		if (position == Vector3D.Zero) {
			Rotate(Vector3D.Zero);
			return;
		}
		
		if (deployed == false) Launch();
		
		double distance = (position - CameraBlocks[0].GetPosition()).Length();
		if (distance < DetonationProximity) {
			foreach (var warhead in WarheadBlocks) warhead.Detonate();
		}
		else if (distance < WarheadArmDistance) {
			foreach (var warhead in WarheadBlocks) warhead.IsArmed = true;
		}
		
		Vector3D forward = CameraBlocks[0].WorldMatrix.Forward;
		Vector3D targetDirection = Vector3D.Normalize(position - CameraBlocks[0].GetPosition());
		double angle = Math.Acos(Vector3D.Dot(forward, targetDirection));
		Vector3D angularDeviation = Vector3D.Normalize(Vector3D.Cross(forward, targetDirection)) * angle;
		Vector3D.ClampToSphere(ref angularDeviation, MaxAngularVelocity / 180.0 * Math.PI);
		Rotate(-angularDeviation * ControlResponsiveness);
	}
	
	public static void Launch() {
		deployed = true;
		foreach (var block in Base.GridBlocks.OfType<IMyFunctionalBlock>()) {
			var rotor = block as IMyMotorStator;
			if (rotor != null) rotor.Detach();
			else block.Enabled = true;
		}
	}
	
	public static void Rotate(Vector3D angularVelocity) {
		foreach (var gyroscope in GyroBlocks) {
			Vector3D velocity = Base.DirectionToBlockSpace(angularVelocity, gyroscope);
			gyroscope.Pitch = (float)velocity.X;
			gyroscope.Yaw = (float)velocity.Y;
			gyroscope.Roll = (float)velocity.Z;
		}
	}
}

