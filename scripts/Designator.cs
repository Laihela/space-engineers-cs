Program() {
	Echo("");
	Base.Init(this);
	Util.Init(this);
	Comms.Init(this);
	TargetTracking.Init(this);
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	TargetTracking.MissingTargetExpireTime = 0.0;
}

void Main(string command) {
	Base.Title("Designator");
	if (command == "Ping") TargetTracking.Ping();
	if (TargetTracking.CurrentTarget != null) {
		Comms.Transmit("Target", TargetTracking.CurrentTarget.Serialize());
		TargetTracking.CurrentTarget = null;
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
	public static readonly DateTime Version = new DateTime(2020, 10, 14, 19, 50, 00);
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

static class TargetTracking {
	public static readonly DateTime Version = new DateTime(2020, 10, 14, 21, 24, 00);
	public static MyGridProgram Program;
	public static List<IMyCameraBlock> CameraBlocks;
	public static double MissingTargetExpireTime = 3.0;
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
			Size = (entityInfo.BoundingBox.Max - entityInfo.BoundingBox.Min).Length();
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
		if (CurrentTarget == null) CurrentTarget = newTarget;
		if (newTarget.Id == CurrentTarget.Id) return;
		if (newTarget.Priority > CurrentTarget.Priority) CurrentTarget = newTarget;
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

