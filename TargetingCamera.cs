// TargetingCamera.cs
// Written by Laihela



//// INITIALIZATION ////
Program () {

	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	Echo(""); // Clear the output window in terminal
	
	// Initialize classes
	Base.Init(this); // Always init Base first!
	TargetCamera.Init(this);
	TargetTracking.Init(this);
	
	// Change settings
	Base.Title = "Targeting Camera";
	TargetTracking.PingEnabled = true;

}



//// EXECUTION ////
void Main(string argument) {

	if (argument == "") {
		Base.Update(); // Always update Base first!
		TargetCamera.Update();
		TargetTracking.Update();
		
		Base.Print(TargetTracking.GetRange());
		Base.Print(TargetTracking.CurrentTarget?.Position);
		
		// Call Base.ProcessorLoad last, anything after wont be taken into account.
		Base.Print("CPU: " + Base.ProcessorLoad.ToString("000.000%"));
	}
	
	else if (argument == "Ping") TargetTracking.Ping();

}



//// CLASSES ////
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
static class TargetCamera {
	public static readonly DateTime Version = new DateTime(2022, 06, 24, 08, 00, 00);
	public static MyGridProgram Program;
	
	public static double AimSensitivity = 0.02;
	
	static List<IMyMotorStator> rotors = new List<IMyMotorStator>();
	static IMyShipController controller = null;
	static IMyCameraBlock camera = null;
	
	// Remember to call this first
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing TargetCamera");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		rotors = Base.GroupBlocks.OfType<IMyMotorStator>().ToList();
		controller = Base.GroupBlocks.OfType<IMyShipController>().FirstOrDefault();
		camera = Base.GroupBlocks.OfType<IMyCameraBlock>().FirstOrDefault();
		Program.Echo("    GroupRotors: " + rotors.Count);
		Program.Echo("    GroupController: " + (controller != null));
		
		if (controller == null) Base.Throw("No controller grouped with programmable block");
		if (camera == null) Base.Throw("No camera grouped with programmable block");
	}
	// Call this every frame
	public static void Update() {
		Vector3D[] inputVectors = GetInput();
		foreach (var rotor in rotors) {
			Vector3D vec = Base.DirectionToBlockSpace(rotor.WorldMatrix.Up, camera).Cross(Vector3D.Forward);
			double vel = vec.Dot(inputVectors[1]);
			Base.SetRotorVelocity(rotor, vel * Math.PI);
		}
	}
	public static Vector3D[] GetInput() {
		double pitch = Base.Clamp(-controller.RotationIndicator.Y * AimSensitivity, -1.0, 1.0);
		double yaw = Base.Clamp(controller.RotationIndicator.X * AimSensitivity, -1.0, 1.0);
		double roll = -controller.RollIndicator;
		Vector3D moveInput = controller.MoveIndicator;
		Vector3D lookInput = new Vector3D(pitch, yaw, roll);
		return new Vector3D[]{ moveInput, lookInput };
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

