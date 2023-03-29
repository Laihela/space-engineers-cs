// Written by Laihela



//// INITIALIZATION ////
Program () {

	// Initialize classes
	Base.Init(this); // Always init Base first!
	ShipControl.Init(this, Base.GridBlocks);
	//RoverControl.Init(this, Base.GridBlocks);
	RotorControl.Init(this, Base.ConstructBlocks);
	SolarControl.Init(this, Base.ConstructBlocks);
	TargetTracking.Init(this, Base.GridBlocks);
	Ballistics.Init(this, Base.GridBlocks);
	MissileLauncher.Init(this, Base.GridBlocks);
	
	// Change settings
	Base.Title = "Ship Control";
	Base.DisplayOutput(Base.GroupBlocks, "(Output)", 0);
	Base.DisplayOutput(Base.GroupBlocks.OfType<IMyCockpit>(), surfaceId:0);
	Echo("Output displays: " + Base.OutputDisplays.Count);
	Base.SetLCDTheme(Base.GroupBlocks, Color.Yellow, Color.Black);
	
	ShipControl.GyroMaxDelta = 0.1;
	ShipControl.MaxShipSpeed = 100.0;
	TargetTracking.PingEnabled = true;
	//Ballistics.ProjectileVelocity = 500.0;
	//Ballistics.ProjectileHasGravity = true;
	
	Runtime.UpdateFrequency = UpdateFrequency.Update1;

}



//// EXECUTION ////
void Main(string argument) {
	try {

		if (argument == "launchGPS") {
			MissileLauncher.LaunchGPS();
		}

		else if (argument == "faster") {
			ShipControl.MaxShipSpeed *= 2.0;
			if (ShipControl.MaxShipSpeed > 100.0) ShipControl.MaxShipSpeed = 100.0;
		}

		else if (argument == "slower") {
			ShipControl.MaxShipSpeed *= 0.5;
			if (ShipControl.MaxShipSpeed < 0.78125) ShipControl.MaxShipSpeed = 0.78125; // = 100.0 / 128
		}

		else if (argument == "target") {
			if (TargetTracking.CurrentTarget == null) TargetTracking.Ping();
			else TargetTracking.CurrentTarget = null;
		}

		else {
			Base.Update(); // Always update Base first!
			TargetTracking.Update();
			ShipControl.Update();
			SolarControl.Update();
			RotorControl.Update();
			
			ShipControl.NormalFlight();
			//ShipControl.ExperimentalFlight();
			//RoverControl.Drive();
			
			if (TargetTracking.CurrentTarget != null) {
				Base.Print("Tracking target!");
				var controller = ShipControl.Controllers.FirstOrDefault();
				if (controller != null) {
					var vectors = TargetTracking.GetVectors();
					Ballistics.Evaluate(ref targetLead, vectors[0], vectors[1], vectors[2]);
					var relativePos = vectors[0] - controller.GetPosition();
					var relativeVel = vectors[1] - controller.GetShipVelocities().LinearVelocity;
					var gravity = controller.GetNaturalGravity();
					ShipControl.SetAngle(relativePos + targetLead, -gravity, response: 0.2);
					var angularCorrectionVel = Base.GetRotation(relativePos, relativePos + Vector3D.Reject(relativeVel, controller.WorldMatrix.Forward));
					//var angularCorrectionVel = Vector3D.Cross(controller.WorldMatrix.Forward, Vector3D.Reject(controller.WorldMatrix.Forward, relativeVel));
					//angularCorrectionVel.Normalize();
					//angularCorrectionVel *= (relativeVel.Length() / relativePos.Length());
					ShipControl.AddRotation(angularCorrectionVel * 0.5, response: 1);
				}
			}
			else Base.Print("Range: " + TargetTracking.GetRange());
			
			// Call Base.ProcessorLoad last for an accurate readout.
			Base.Print("CPU: " + Base.ProcessorLoad.ToString("000.000%"));
		}

	}
	catch (System.Exception exception) { Base.Throw(exception); }
}



//// STATE ////
Vector3D targetLead = Vector3D.Zero;



//// CLASSES ////
static class Base {
	public static readonly DateTime Version = new DateTime(2023, 03, 29, 08, 04, 00);



	//// PUBLIC SETTINGS ////
	public static string Title = "Title";



	//// PUBLIC DATA ////
	public static Dictionary<IMyTerminalBlock, Dictionary<string, string>> BlockProperties = new Dictionary<IMyTerminalBlock, Dictionary<string, string>>();
	public static Dictionary<string, HashSet<IMyTerminalBlock>> MyBlockGroups = new Dictionary<string, HashSet<IMyTerminalBlock>>();
	public static HashSet<IMyTerminalBlock> ConstructBlocks = new HashSet<IMyTerminalBlock>();
	public static HashSet<IMyTerminalBlock> GroupBlocks = new HashSet<IMyTerminalBlock>();
	public static HashSet<IMyTerminalBlock> GridBlocks = new HashSet<IMyTerminalBlock>();
	public static HashSet<IMyTextSurface> OutputDisplays = new HashSet<IMyTextSurface>();
	// DeltaTime remains consistent with simulation speed changes.
	public static double DeltaTime { get {
		return _program.Runtime.TimeSinceLastRun.TotalSeconds;
	}}
	// Real-time seconds between updates, will increase if simulation speed drops.
	public static double RealDeltaTime { get {
		return _realDeltaTime;
	}}
	// Ratio of currently used program instructions to max allowed instructions. (0 to 1)
	public static double ProcessorLoad { get {
		return (double)_program.Runtime.CurrentInstructionCount / _program.Runtime.MaxInstructionCount;
	}}



	//// PRIVATE DATA ////
	static MyGridProgram _program;
	static bool _isInitialized = false;
	static DateTime _lastUpdate = DateTime.Now;
	static double _realDeltaTime = 0.0;
	static string _symbol = "";
	static StringBuilder _printBuffer = new StringBuilder();
	static StringBuilder _warnBuffer = new StringBuilder();
	static Color _contentColor = new Color(179, 237, 255);
	static Color _backgroundColor = new Color(0, 88, 151);



	//// PUBLIC METHODS ////
	// Remember to call this first, before all other classes.
	public static void Init(MyGridProgram program) {
		_program = program;
		_program.Echo("Initializing Base");
		_program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		GetGroups();
		GetBlocks();
		ReadProperties();
		
		_program.Echo("    ConstructTerminalBlocks: " + ConstructBlocks.Count);
		_program.Echo("    GroupTerminalBlocks: " + GroupBlocks.Count);
		_program.Echo("    GridTerminalBlocks: " + GridBlocks.Count);
		_program.Echo("    MyBlockGroups: " + MyBlockGroups.Count);
		
		//_program.Me.GetSurface(0).ContentType = ContentType.TEXT_AND_IMAGE;
		//SetLCDTheme(_program.Me, new Color(128, 255, 0), new Color(0, 0, 0), 1f, 2f);
		DisplayOutput(_program.Me.GetSurface(0));
		SetLCDTheme(_program.Me, new Color(128, 255, 0), new Color(0, 0, 0), 1f, 2f);
		DisplayKeyboard(_program.Me.GetSurface(1));
		_isInitialized = true;
	}
	// Call this first, every frame.
	public static void Update() {
		AssertInit();
		FlushOutput();
		Print($"{Title} {_symbol}");
		_realDeltaTime = (DateTime.Now - _lastUpdate).TotalSeconds;
		_lastUpdate = DateTime.Now;
		UpdateSymbol();
	}
	// Write text to all output displays.
	public static void Print(string message) {
		AssertInit();
		//foreach (var display in OutputDisplays) display.WriteText(message.ToString() + "\n", true);
		_printBuffer.Append(message).Append('\n');
	}
	public static void Print(object message) {
		Print(message.ToString());
	}
	public static void Warn (object message) {
		_warnBuffer.Append(message).Append('\n');
	}
	// Write an error message to all output displays and stop the program.
	public static void Throw(object message) {
		Print("ERR: " + message.ToString());
		FlushOutput();
		SetLCDTheme(_program.Me, new Color(0, 0, 0), new Color(255, 0, 0));
		SetLCDTheme(OutputDisplays, new Color(0, 0, 0), new Color(255, 0, 0));
		throw new System.Exception(message.ToString());
	}
	// Make sure that Init() has been called, if not, throw an error.
	public static void AssertInit() {
		if (_isInitialized == false) throw new System.Exception($"Class has not been initialized!");
	}


	//// LCD CONTROL ////
	// Add target(s) to which the Print() and Throw() methods will write text to.
	// Target can be a text surface, a block, block list, etc... (see cases)
	// Tag: if provided, only adds blocks which have the tag string in their name (for collections only).
	// SurfaceId: If provided, only adds one display from the block(s) with the specified surfaceId (if it exists).
	public static void DisplayOutput(object target, string tag = "", int surfaceId = -1) {
		
		// Cases should be ordered from least recursive to most recursive, and then from most used to least used for best performance.
		
		// Case: target is single text surface.
		var display = target as IMyTextSurface;
		if (display != null) {
			display.ContentType = ContentType.TEXT_AND_IMAGE;
			//SetLCDTheme(display, new Color(128, 255, 0), new Color(0, 0, 0), 1f, 2f);
			OutputDisplays.Add(display);
			return;
		}
		
		// Case: target is a single block (and has displays), call recursively for each display. (1 layer recursion)
		var block = target as IMyTextSurfaceProvider;
		if (block != null) {
			if (surfaceId == -1) for (int i = 0; i < block.SurfaceCount; i++) DisplayOutput(block.GetSurface(i));
			else if (surfaceId < block.SurfaceCount) DisplayOutput(block.GetSurface(surfaceId));
			return;
		}
		
		// Case: target is a collection, call recursively for each item. (1 layer recursion)
		var items = target as IEnumerable;
		if (items != null) {
			foreach (var item in items) {
				var b = item as IMyTerminalBlock;
				if (tag == "" || b == null || b.CustomName.Contains(tag)) DisplayOutput(item, tag, surfaceId);
			}
			return;
		}
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
	// Change colors and set font to Monospace on a display. Also works for list if displays, a block, or a list of blocks.
	public static void SetLCDTheme(object target, Color? _contentColor = null, Color? _backgroundColor = null, float fontSize = -1f, float textPadding = -1f) {
		
		// Cases should be ordered from most specific to least specific, then from least
		// recursive to most recursive, and then from most used to least used for best performance.
		
		// Case: target is single display.
		var display = target as IMyTextSurface;
		if (display != null) {
			Color content = _contentColor.GetValueOrDefault(new Color(179, 237, 255));
			Color background = _backgroundColor.GetValueOrDefault(new Color(0, 88, 151));
			display.ScriptBackgroundColor = background;
			display.ScriptForegroundColor = content;
			display.BackgroundColor = background;
			display.FontColor = content;
			display.Font = "Monospace";
			if (display.ContentType == ContentType.NONE) display.ContentType = ContentType.SCRIPT;
			if (textPadding > 0f) display.TextPadding = textPadding;
			if (fontSize > 0f) display.FontSize = fontSize;
			return;
		}
		
		// Case: target is a single block (and has displays), call recursively for each display. (1 layer recursion)
		var block = target as IMyTextSurfaceProvider;
		if (block != null) {
			for(int x = 0; x < block.SurfaceCount; x++) SetLCDTheme(block.GetSurface(x), _contentColor, _backgroundColor, fontSize, textPadding);
			return;
		}
		
		// Case: target is a collection, call recursively for each item. (1 or more layers of recursion depending on collection type)
		var items = target as IEnumerable;
		if (items != null) {
			foreach (var x in items) SetLCDTheme(x, _contentColor, _backgroundColor, fontSize, textPadding);
			return;
		}
	}


	//// MISC ////
	// Returns current time in seconds.
	public static double Now() {
		return DateTime.Now.Ticks / 1e7;
	}
	// Set rotor velocity in radians per second, NaN safety included.
	public static void SetRotorVelocity(IMyMotorStator rotor, double velocity, double response = 1.0) {
		if (double.IsNaN(velocity)) velocity = 0.0;
		rotor.TargetVelocityRad = (float)Lerp(rotor.TargetVelocityRad, velocity, response);
	}
	public static void SetGyroVelocity(IMyGyro gyro, Vector3D velocity) {
		// Keen coded the override for gyros backwards because Yes.
		Vector3 v = Base.DirectionToBlockSpace(velocity, gyro);
		gyro.Pitch = -v.X;
		gyro.Yaw = -v.Y;
		gyro.Roll = -v.Z;
	}
	public static void AddGyroVelocity(IMyGyro gyro, Vector3D velocity) {
		// Keen coded the override for gyros backwards because Yes.
		Vector3 v = Base.DirectionToBlockSpace(velocity, gyro);
		gyro.Pitch -= v.X;
		gyro.Yaw -= v.Y;
		gyro.Roll -= v.Z;
	}
	// Get property value by name, as defined in block CustomData.
	public static T GetBlockProperty<T>(IMyTerminalBlock block, string propertyName, T defaultValue) {
		try {
			return (T)Convert.ChangeType((BlockProperties[block][propertyName]), typeof(T));
		}
		catch (Exception e) {
			return defaultValue;
		}
	}
	// Get pilot input values from a controller or set of blocks. Inputs from multiple controllers are added together.
	public static List<Vector3D> GetInput(object target, bool worldSpace = false, bool mainControllerOnly = false) {
		List<Vector3D> vectors = new List<Vector3D>{ Vector3D.Zero, Vector3D.Zero };
		
		// Cases should be ordered from least recursive to most recursive, and then from most used to least used for best performance.
		
		// Case: target is single controller.
		IMyShipController controller = target as IMyShipController;
		if (controller != null) {
			if (mainControllerOnly && (controller.IsMainCockpit == false)) return vectors;
			
			double sensitivity = GetBlockProperty(controller, "mouse sensitivity", 1.0) * 0.1;
			// Keen coded the rotation indicators are backwards because Yes.
			double pitch = -controller.RotationIndicator.X * sensitivity;
			double yaw = -controller.RotationIndicator.Y * sensitivity;
			double roll = -controller.RollIndicator;
			vectors[0] = controller.MoveIndicator;
			vectors[1] = new Vector3D(pitch, yaw, roll);
			if (worldSpace) {
				vectors[0] = Base.DirectionToWorldSpace(vectors[0], controller);
				vectors[1] = Base.DirectionToWorldSpace(vectors[1], controller);
			}
			return vectors;
		}
		
		// Case: target is a collection, call recursively for each item and add values. (1 layer recursion)
		IEnumerable items = target as IEnumerable;
		if (items != null) {
			foreach (var item in items) {
				var input = GetInput(item, worldSpace, mainControllerOnly);
				vectors[0] += input[0];
				vectors[1] += input[1];
			}
			return vectors;
		}
		
		// Case: target is null or an unimplemented type.
		return vectors;
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
		return Vector3D.TransformNormal(direction, block.WorldMatrix);
	}
	// Transfroms a vector from world space to block space, ignoring block position.
	public static Vector3D DirectionToBlockSpace(Vector3D direction, IMyCubeBlock block) {
		return Vector3D.TransformNormal(direction, MatrixD.Transpose(block.WorldMatrix));
	}

	// Get an euler rotation from direction to direction in radians.
	public static Vector3D GetRotation(Vector3D from, Vector3D to) {
		from = Vector3D.Normalize(from);
		to = Vector3D.Normalize(to);
		double angle = Math.Acos(Vector3D.Dot(from, to));
		Vector3D axis = Vector3D.Normalize(Vector3D.Cross(from, to));
		Vector3D result = axis * angle;
		if (double.IsNaN(result.X)) return Vector3D.Zero;
		else return result;
	}
		// Limit a vector between a minimum and a maximum length.
	public static Vector3D Clamp(Vector3D vec, double min, double max) {
		double len = vec.Length();
		if (len == 0.0) return vec;
		return len < min ? vec / len * min : (len > max ? vec / len * max : vec);
	}
	public static Vector3D Clamp(Vector3D vec, double max) {
		double len = vec.Length();
		return len > max ? vec / len * max : vec;
	}


	//// NUMBER MATH ////
	// Limit a number between a minimum and a maximum value.
	public static double Clamp(double val, double min, double max) {
		return val < min ? min : (val > max ? max : val);
	}
	// Return a number interpolated from a to b by time.
	public static double Lerp(double a, double b, double time) {
		return a * (1.0 - time) + b * time;
	}
	// Return the modulus of a number.
	// Result is always positive, works consistently with negative input values.
	public static double Mod(double v, double m) {
		v = v % m;
		return v < 0 ? v + m: v;
	}



	//// PRIVATE METHODS ////
	static void UpdateSymbol() {
		if       (_symbol == "|")  _symbol = "/";
		else if  (_symbol == "/")  _symbol = "-";
		else if  (_symbol == "-")  _symbol = "\\";
		else                      _symbol = "|";
	}
	static void GetGroups() {
		List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
		_program.GridTerminalSystem.GetBlockGroups(blockGroups);
		foreach(var blockGroup in blockGroups) {
			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
			blockGroup.GetBlocks(blocks);
			if (blocks.Contains(_program.Me) == false) continue;
			foreach (var block in blocks) GroupBlocks.Add(block);
			MyBlockGroups.Add(blockGroup.Name, new HashSet<IMyTerminalBlock>(blocks));
		}
	}
	static void GetBlocks() {
		List<IMyTerminalBlock> terminalBlocks = new List<IMyTerminalBlock>();
		_program.GridTerminalSystem.GetBlocks(terminalBlocks);
		foreach (var block in terminalBlocks) {
			if (block.IsSameConstructAs(_program.Me)) ConstructBlocks.Add(block);
			if (block.CubeGrid == _program.Me.CubeGrid) GridBlocks.Add(block);
		}
		
	}
	// Parse properties from blocks' CustomData into the BlockProperties variable.
	// Property format is as follows:
	// [property name, case insensitive] : [property value, case sensitive]
	// One property per line, whitespace from start/end of name and value is removed.
	static void ReadProperties() {
		foreach (var block in Base.ConstructBlocks) {
			string data = block.CustomData;
			if (data == "") continue;
			BlockProperties.Add(block, new Dictionary<string, string>());
			foreach (string line in data.Split('\n')) {
				int split = line.IndexOf(':');
				if (split == -1) continue;
				string propName = line.Substring(0, split).Trim(' ').ToLower();
				string propValue = line.Substring(split + 1).Trim(' ');
				BlockProperties[block].Add(propName, propValue);
			}
		}
	}
	static void FlushOutput() {
		foreach (var display in OutputDisplays) display.WriteText(_warnBuffer.ToString() + _printBuffer.ToString());
		_printBuffer.Clear();
	}
}
static class ShipControl {
	public static readonly DateTime Version = new DateTime(2023, 03, 29, 07, 03, 00);



	//// PUBLIC SETTINGS ////
	public static double MaxShipSpeed = 1000.0;
	public static double GyroMaxDelta = 0.15;
	// Turn off thrusters below this value to conserve fuel/power (BROKEN)
	//public static double MinThrust = 0.0;
	
	//// AUTO PILOT SETTINGS ////
	public static double GyroResponse = 0.2; // 0 to 1
	public static double VelocityResponse = 0.3;
	public static double HomingResponse = 0.5; // 0 to 1
	public static double MaxApproachSpeed = 200.0;
	// Approximate mass to thrust ratio (kg / kN) (TODO: Calculate automatically)
	public static double APPROACH_SLOW_DOWN = 37.7;



	//// PUBLIC DATA ////
	public static MyGridProgram Program;
	public static List<IMyShipController> Controllers = new List<IMyShipController>();
	public static List<IMyThrust> Thrusters = new List<IMyThrust>();
	public static List<IMyGyro> Gyroscopes = new List<IMyGyro>();
	public static List<ThrusterGroup> ThrusterGroups = new List<ThrusterGroup>();



	//// PUBLIC CLASSES ////
	public class ThrusterGroup {
		public Vector3D Direction = Vector3D.Zero;
		public List<IMyThrust> Thrusters = new List<IMyThrust>();
		
		double currentThrust = 0.0;
		
		public ThrusterGroup(List<IMyThrust> thrusters, Base6Directions.Direction direction) {
			Direction = Base6Directions.GetVector(direction);
			foreach (var thruster in thrusters) {
				if (thruster.Orientation.Forward == direction) Thrusters.Add(thruster);
			}
		}
		
		public void SetThrust(double thrust) {
			currentThrust = thrust;
			foreach (var thruster in Thrusters) {
				// Do not set override for disabled thrusters, it causes the grid to awake from sleep and make noises o_0
				if (thruster.Enabled == false) continue;
				// Keep thrust overrie just above zero to prevent auto dampeners from slowing the ship.
				thruster.ThrustOverride = Math.Max((float)(currentThrust * thruster.MaxThrust), 0.000000000000000000000000000000001f);
			}
		}
		public void AddThrust(double thrust) {
			currentThrust += thrust;
			foreach (var thruster in Thrusters) {
				// Do not set override for disabled thrusters, it causes the grid to awake from sleep and make noises o_0
				if (thruster.Enabled == false) continue;
				// Keep thrust overrie just above zero to prevent auto dampeners from slowing the ship.
				thruster.ThrustOverride = Math.Max((float)(currentThrust * thruster.MaxThrust), 0.000000000000000000000000000000001f);
			}
		}
		public void SetAcceleration(double acceleration) {
			if (Controllers.Count == 0) Base.Throw("SetAcceleration failed: Cannot calculate acceleration on ship without controller");
			double mass = Controllers[0].CalculateShipMass().PhysicalMass;
			SetThrust((acceleration * mass) / GetMaxForce());
		}
		public void AddAcceleration(double acceleration) {
			if (Controllers.Count == 0) Base.Throw("AddAcceleration failed: Cannot calculate acceleration on ship without controller");
			double mass = Controllers[0].CalculateShipMass().PhysicalMass;
			AddThrust((acceleration * mass) / GetMaxForce());
		}
		
		public double GetMaxForce() {
			double maxForce = 0.0;
			foreach (var thruster in Thrusters) {
				//bool wasEnabled = thruster.Enabled;
				//thruster.Enabled = true;
				if (thruster.IsWorking == false) continue;
				maxForce += thruster.MaxEffectiveThrust;
				//thruster.Enabled = wasEnabled;
			}
			return maxForce;
		}
		public Vector3D GetWorldDirection() {
			if (Thrusters.Count == 0) Base.Throw("GetWorldDirection failed: Thruster group has no thrusters");
			return Thrusters[0].WorldMatrix.Backward;
		}
	}



	//// PUBLIC METHODS ////
	// Remember to call this first
	public static void Init(MyGridProgram program, HashSet<IMyTerminalBlock> blocks, bool controlGyroscopes = true) {
		Program = program;
		Program.Echo("Initializing ShipControl");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		Gyroscopes = blocks.OfType<IMyGyro>().ToList();
		Thrusters = blocks.OfType<IMyThrust>().ToList();
		//Controllers = blocks.OfType<IMyShipController>().Where(c => c.CubeGrid == Program.Me.CubeGrid).ToList();
		Controllers = blocks.OfType<IMyShipController>().ToList();
		Program.Echo("    Controllers: " + Controllers.Count);
		Program.Echo("    Gyroscopes: " + Gyroscopes.Count);
		Program.Echo("    Thrusters: " + Thrusters.Count);
		if (Controllers.Count == 0) Program.Echo("    WARNING: No cockpit or remote control, functionality limited!");
		
		if (controlGyroscopes == false) Gyroscopes.Clear();
		foreach (var gyro in Gyroscopes) gyro.GyroOverride = true;
		
		ThrusterGroups.Add(new ThrusterGroup(Thrusters, Base6Directions.Direction.Forward));
		ThrusterGroups.Add(new ThrusterGroup(Thrusters, Base6Directions.Direction.Backward));
		ThrusterGroups.Add(new ThrusterGroup(Thrusters, Base6Directions.Direction.Left));
		ThrusterGroups.Add(new ThrusterGroup(Thrusters, Base6Directions.Direction.Right));
		ThrusterGroups.Add(new ThrusterGroup(Thrusters, Base6Directions.Direction.Up));
		ThrusterGroups.Add(new ThrusterGroup(Thrusters, Base6Directions.Direction.Down));
		ThrusterGroups.RemoveAll(g => g.Thrusters.Count == 0);
	}
	public static void Update() {
		SetThrust(Vector3.Zero);
		SetRotation(Vector3D.Zero);
	}
	
	//// AUTO PILOTING ////
	public static void FlyTo(Vector3D targetPos, Vector3D targetVel, MatrixD targetOrientation) {
		if (Controllers.Count == 0) Base.Throw("Ship has no controller");
		
		// Reduce unnecessary division operations
		double perSecond = 1.0 / Base.DeltaTime;
		// Pos(ition), Rot(ation), Vel(ocity), Acc(eleration)
		Vector3D myPos = Controllers[0].CenterOfMass;
		Vector3D myVel = Controllers[0].GetShipVelocities().LinearVelocity;
		Vector3D targetAcc = (targetVel - lastTargetVel) * perSecond;
		Vector3D targetFwd = targetOrientation.Forward;
		Vector3D targetUp = targetOrientation.Up;
		Vector3D targetRotVel = (Base.GetRotation(lastTargetFwd, targetFwd) + Base.GetRotation(lastTargetUp, targetUp)) * perSecond;
		
		Vector3D relativePos = targetPos - myPos;
		Vector3D relativeVel = targetVel - myVel;
		Vector3D approachBrake = relativePos * Vector3.Dot(relativePos / relativePos.Length(), relativeVel) * Base.DeltaTime * APPROACH_SLOW_DOWN;
		Vector3D approachVel = (relativePos * perSecond + relativeVel) * VelocityResponse * HomingResponse;
		Vector3D predictPos = targetVel * Base.DeltaTime;
		
		ShipControl.SetVelocity(targetVel + predictPos + Base.Clamp(approachVel + approachBrake, 0.0, MaxApproachSpeed), VelocityResponse);
		ShipControl.AddAcceleration(targetAcc * 2.0 - lastTargetAcc);
		ShipControl.SetAngle(targetOrientation, GyroResponse);
		//ShipControl.AddRotation(targetRotVel, 1.0); /// broken for some reason ///
		
		lastTargetVel = targetVel;
		lastTargetAcc = targetAcc;
		lastTargetFwd = targetFwd;
		lastTargetUp = targetUp;
	}
	public static void NormalFlight(double response = 0.02, double gyroResponse = 0.1, bool cancelGravity = true) {
		if (Controllers.Count == 0) Base.Throw("Ship has no controller");
		gyroResponse = Base.Clamp(gyroResponse, 0.0, 1.0);
		response = Base.Clamp(response, 0.0, 1.0);
		
		List<Vector3D> input = Base.GetInput(Controllers, true);
		
		SetRotation(input[1] * 2.0 * Math.PI, gyroResponse);
		if (Controllers[0].DampenersOverride == false) SetThrust(input[0], cancelGravity);
		else SetVelocity(input[0] * MaxShipSpeed, response, cancelGravity);
	}
	public static void CentripetalFlight(double response = 0.02, double gyroResponse = 0.1, bool cancelGravity = true) {
		if (Controllers.Count == 0) Base.Throw("Ship has no controller");
		gyroResponse = Base.Clamp(gyroResponse, 0.0, 1.0);
		response = Base.Clamp(response, 0.0, 1.0);
		
		Vector3D linearVelocity = Controllers[0].GetShipVelocities().LinearVelocity;
		Vector3D angularVelocity = Controllers[0].GetShipVelocities().AngularVelocity;
		Vector3D centripetalAcceleration = Vector3D.Cross(angularVelocity, linearVelocity);
		List<Vector3D> input = Base.GetInput(Controllers, true);
		
		SetRotation(input[1] * 2 * Math.PI, gyroResponse);
		if (Controllers[0].DampenersOverride == false) SetThrust(input[0], cancelGravity);
		else SetVelocity(input[0] * MaxShipSpeed, response, cancelGravity);
		AddAcceleration(centripetalAcceleration);
	}
	public static void ExperimentalFlight(double response = 0.02, double gyroResponse = 0.1, bool cancelGravity = true) {
		if (Controllers.Count == 0) Base.Throw("Ship has no controller");
		
		List<Vector3D> input = Base.GetInput(Controllers, true);
		
		SetRotation(input[1] * 2 * Math.PI, gyroResponse);
		
		const int maxSteps = 500;
		double mass = Controllers[0].CalculateShipMass().PhysicalMass;
		Vector3D gravity = cancelGravity ? Controllers[0].GetNaturalGravity() : Vector3D.Zero;
		Vector3D currentThrust = Vector3D.Zero;
		Vector3D velocity = Controllers[0].DampenersOverride ? Controllers[0].GetShipVelocities().LinearVelocity : Vector3D.Zero;
		Vector3D targetThrust = ((velocity - input[0] * MaxShipSpeed) / Base.DeltaTime * response + gravity) * mass;
		int step = 0;
		while (++step < maxSteps) {
			if (SolveThrust(ref currentThrust, targetThrust, mass)) break;
		}
		Base.Print("Solver steps: " + step);
		Base.Print("ErrorLen: " + (currentThrust - targetThrust).Length().ToString("0.000"));
		Base.Print("ErrorRot: " + Base.GetRotation(currentThrust, targetThrust).Length().ToString("0.000"));
	}
	static bool SolveThrust(ref Vector3D currentThrust, Vector3D targetThrust, double mass) {
		Vector3D thrustError = targetThrust - currentThrust;
		if (thrustError.Length() < 0.0001) return true;
		
		IMyThrust optimalThruster = null;
		Vector3D thrustDirection = Vector3D.Zero;
		double accuracy = 0.000001;
		foreach (var thruster in Thrusters) {
			if (thruster.ThrustOverride > thruster.MaxEffectiveThrust * 0.999f) continue;
			Vector3D thisThrustDirection = thruster.WorldMatrix.Forward;
			double thisAccuracy = Vector3D.Dot(thisThrustDirection, thrustError);
			if (thisAccuracy <= accuracy) continue;
			accuracy = thisAccuracy;
			optimalThruster = thruster;
			thrustDirection = thisThrustDirection;
		}
		if (optimalThruster == null) return true;
		
		// Keep thrust overrie just above zero to prevent auto dampeners from slowing the ship.
		double thrust = Base.Clamp(Vector3D.Dot(thrustError, thrustDirection), 0.000000000000000000000000000000001, optimalThruster.MaxEffectiveThrust);
		optimalThruster.ThrustOverride = (float)thrust * (optimalThruster.MaxThrust / optimalThruster.MaxEffectiveThrust);
		currentThrust += thrustDirection * thrust;
		return false;
	}
	
	//// MANUAL PILOTING ////
	public static void SetAngle(Vector3D targetForward, Vector3D? targetUp = null, double response = 0.1) {
		if (Controllers.Count == 0) Base.Throw("SetAngle failed: Ship has no controller");
		response = Base.Clamp(response, 0.0, 1.0);
		Vector3D forward = Controllers[0].WorldMatrix.Forward;
		Vector3D up = Controllers[0].WorldMatrix.Up;
		Vector3D pitchYawDelta = Base.GetRotation(forward, targetForward);
		Vector3D rollDelta = Vector3D.Zero;
		if (targetUp != null) rollDelta = forward * Vector3D.Dot(forward, Base.GetRotation(up, targetUp.Value));
		Vector3D targetVelocity = (pitchYawDelta + rollDelta) * 0.5 / Base.DeltaTime;
		Vector3D velocity = Controllers[0].GetShipVelocities().AngularVelocity;
		SetRotation(targetVelocity * response - velocity * velocity.Length());
	}
	public static void SetAngle(MatrixD orientation, double response = 0.1) {
		SetAngle(orientation.Forward, orientation.Up, response);
	}
	public static void SetRotation(Vector3D velocity, double response = 1.0) {
		response = Base.Clamp(response, 0.0, 1.0);
		if (Controllers.Count != 0) {
			Vector3D myVelocity = Controllers[0].GetShipVelocities().AngularVelocity;
			velocity = Base.Clamp((velocity - myVelocity) * response, GyroMaxDelta) + myVelocity;
		}
		foreach (var gyro in Gyroscopes) Base.SetGyroVelocity(gyro, velocity);
	}
	public static void SetRotation(MatrixD velocity, double response = 1.0) {
		Vector3D axis = Vector3D.Zero;
		double angle = 0.0;
		QuaternionD.CreateFromRotationMatrix(velocity).GetAxisAngle(out axis, out angle);
		SetRotation(axis * angle, response);
	}
	public static void AddRotation(Vector3D velocity, double response = 1.0) {
		/*response = Base.Clamp(response, 0.0, 1.0);
		if (Controllers.Count != 0) {
			Vector3D myVelocity = Controllers[0].GetShipVelocities().AngularVelocity;
			velocity = Base.Clamp((velocity - myVelocity) * response, GyroMaxDelta) + myVelocity;
		}*/
		foreach (var gyro in Gyroscopes) Base.AddGyroVelocity(gyro, velocity);
	}

	public static void SetThrust(Vector3D input, bool cancelGravity = false) {
		Vector3D gravity = Controllers.Count != 0 ? Controllers[0].GetNaturalGravity() : Vector3D.Zero;
		foreach (var thrusterGroup in ThrusterGroups) {
			Vector3D d = thrusterGroup.GetWorldDirection();
			double t = Vector3D.Dot(d, input);
			thrusterGroup.SetThrust(t);
			if (cancelGravity) {
				double a = Vector3D.Dot(d, -gravity);
				thrusterGroup.AddAcceleration(a);
			}
		}
	}
	public static void SetAcceleration(Vector3D acceleration) {
		foreach (var thrusterGroup in ThrusterGroups) {
			Vector3D d = thrusterGroup.GetWorldDirection();
			double a = Vector3D.Dot(d, acceleration);
			thrusterGroup.SetAcceleration(a);
		}
	}
	public static void AddAcceleration(Vector3D acceleration) {
		foreach (var thrusterGroup in ThrusterGroups) {
			Vector3D d = thrusterGroup.GetWorldDirection();
			double a = Vector3D.Dot(d, acceleration);
			thrusterGroup.AddAcceleration(a);
		}
	}
	public static void SetVelocity(Vector3D targetVelocity, double response = 1.0, bool cancelGravity = true) {
		if (Controllers.Count == 0) Base.Throw("SetVelocity failed: Cannot calculate velocity on ship without controller");
		response = Base.Clamp(response, 0.0, 1.0);
		Vector3D gravity = cancelGravity ? Controllers[0].GetNaturalGravity() : Vector3D.Zero;
		Vector3D velocity = Controllers[0].GetShipVelocities().LinearVelocity;
		Vector3D stopper = Base.Clamp(-velocity * 0.5, 0.001);
		foreach (var thrusterGroup in ThrusterGroups) {
			Vector3D d = thrusterGroup.GetWorldDirection();
			Vector3D v = ((targetVelocity - velocity) * response + stopper) / Base.DeltaTime;
			double a = Vector3D.Dot(d, v - gravity);
			thrusterGroup.SetAcceleration(a);
		}
	}



	//// PRIVATE DATA ////
	static Vector3D lastTargetVel = Vector3D.Zero;
	static Vector3D lastTargetAcc = Vector3D.Zero;
	static Vector3D lastTargetFwd = Vector3D.Zero;
	static Vector3D lastTargetUp = Vector3D.Zero;
}
static class RoverControl {
	public static readonly DateTime Version = new DateTime(2022, 07, 20, 00, 00, 00);



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
		
		var input = Base.GetInput(Controller);
		Vector3D forwardDirection = Controller.WorldMatrix.Forward;
		Vector3D rightDirection = Controller.WorldMatrix.Right;
		foreach (IMyMotorSuspension wheel in Wheels) {
			Vector3D wheelDirection = wheel.WorldMatrix.Up;
			Vector3D wheelPosition = wheel.GetPosition() - Controller.GetPosition();
			bool isRightWheel = Vector3D.Dot(rightDirection, wheelDirection) > 0.0;
			bool isFrontWheel = Vector3D.Dot(forwardDirection, wheelPosition) > 0.0;
			double steering = Controller.MoveIndicator.X;
			double throttle = Controller.MoveIndicator.Z;
			// Only assign if value changed to avoid network/physics issues
			float steerOverride = (float)(isFrontWheel ? -steering : steering);
			float propulsionOverride = (float)(isRightWheel ? throttle : -throttle);
			if (wheel.SteeringOverride != steerOverride) wheel.SteeringOverride = steerOverride;
			if (wheel.PropulsionOverride != propulsionOverride) wheel.PropulsionOverride = propulsionOverride;
		}
		foreach (var remote in Remotes) {
			remote.HandBrake = Controller.HandBrake;
		}
	}
}
static class RotorControl {
	public static readonly DateTime Version = new DateTime(2022, 07, 22);



	//// PUBLIC SETTINGS ////
	public static double RotorResponse = 0.1;



	//// PUBLIC DATA ////
	public static MyGridProgram Program;
	public static HashSet<IMyShipController> Controllers = new HashSet<IMyShipController>();
	public static HashSet<IMyMotorStator> Rotors = new HashSet<IMyMotorStator>();



	//// PUBLIC METHODS ////
	// Remember to call this first.
	public static void Init(MyGridProgram program, HashSet<IMyTerminalBlock> blocks) {
		Program = program;
		Program.Echo("Initializing RotorControl");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		Controllers = blocks.OfType<IMyShipController>().ToHashSet();
		Program.Echo("    Controllers: " + Controllers.Count);
		Rotors = blocks.OfType<IMyMotorStator>().ToHashSet();
		Program.Echo("    Rotors: " + Rotors.Count);
		
	}
	// Call this every frame.
	public static void Update() {
		var input = Base.GetInput(Controllers);
		foreach (var rotor in Rotors) {
			double response = Base.Clamp(RotorResponse * Base.GetBlockProperty(rotor, "response", 1.0), 0.0, 1.0);
			double speed = 0.0;
			speed += Base.GetBlockProperty(rotor, "x", 0.0) * input[0].X;
			speed += Base.GetBlockProperty(rotor, "y", 0.0) * input[0].Y;
			speed += Base.GetBlockProperty(rotor, "z", 0.0) * input[0].Z;
			speed += Base.GetBlockProperty(rotor, "pitch", 0.0) * input[1].X * 0.1;
			speed += Base.GetBlockProperty(rotor, "yaw", 0.0) * input[1].Y * 0.1;
			speed += Base.GetBlockProperty(rotor, "roll", 0.0) * input[1].Z;
			Base.SetRotorVelocity(rotor, speed * Math.PI, response);
		}
	}
}
static class MissileLauncher {
	public static readonly DateTime Version = new DateTime(2022, 09, 17, 00, 55, 0);



	//// PUBLIC DATA ////
	public static MyGridProgram Program;
	public static HashSet<IMyMotorBase> Connectors = new HashSet<IMyMotorBase>();



	//// PUBLIC METHODS ////
	// Remember to call this first
	public static void Init(MyGridProgram program, HashSet<IMyTerminalBlock> blocks) {
		Program = program;
		Program.Echo("Initializing SimpleMissile");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		Connectors = blocks.OfType<IMyMotorBase>().ToHashSet();
		Program.Echo("    Connectors: " + Connectors.Count);
	}
	// Tries to launch one missile
	public static void LaunchGPS() {
		List<IMyProgrammableBlock> computers = new List<IMyProgrammableBlock>();
		Program.GridTerminalSystem.GetBlocksOfType(computers);
		foreach (var computer in computers) {
			if (!computer.CustomData.Contains("SimpleMissile")) continue;
			//string[] data = Program.Me.CustomData.Split('\n');
			//string gps = data.Where(s => s.Contains("gps")).FirstOrDefault();
			//if (gps == "") continue;
			//var args = new List<TerminalActionParameter>{ TerminalActionParameter.Get("launchGPS : " + gps) };
			//computer.ApplyAction("Run");
			computer.TryRun(Program.Me.CustomData);
			//computer.TryRun("");
			break;
		}
	}
}
static class SolarControl {
	public static readonly DateTime Version = new DateTime(2023, 03, 29, 02, 38, 00);



	//// PUBLIC SETTINGS ////
	public static double SatisfactoryAlignment = 0.15999; // MW per solar panel
	public static double RotorAlignSpeed = 0.7;
	public static int RotorAlignReversals = 2;
	public static double RotorReversalCooldown = 0.5; // seconds
	public static string RotorAlignTag = "align";



	//// PUBLIC METHODS ////
	// Remember to call this first
	public static void Init(MyGridProgram program, HashSet<IMyTerminalBlock> blocks) {
		program.Echo("Initializing SolarControl");
		program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		solarPanels = blocks.OfType<IMySolarPanel>().ToHashSet();
		program.Echo("    Solar panels: " + solarPanels.Count);
		rotors = blocks.OfType<IMyMotorStator>().Where(rotor => Base.GetBlockProperty(rotor, RotorAlignTag, false)).ToHashSet();
		program.Echo("    Rotors: " + rotors.Count);
		
		currentRotor = rotors.GetEnumerator();
		currentRotor.MoveNext();
	}
	public static void Update() {
		if (rotors.Count == 0) {
			Base.Print("No rotors to align solar panels!");
			return;
		}
		
		// Get current solar panel aligment
		double alignmentScore = 0.0;
		foreach (var panel in solarPanels) alignmentScore = Math.Max(alignmentScore, panel.MaxOutput);
		double deviation = Math.Sqrt(1.0 - alignmentScore / SatisfactoryAlignment);
		
		// Adjust rotors to try to align solar panels
		currentRotor.Current.Enabled = true;
		Base.SetRotorVelocity(currentRotor.Current, RotorAlignSpeed * deviation * currentRotorDirection);
		if (directionSwitchCooldown > 0.0) directionSwitchCooldown -= Base.DeltaTime;
		else if (alignmentScore >= SatisfactoryAlignment) {
			FirstRotor();
			foreach (var rotor in rotors) rotor.Enabled = false;
		}
		else if (currentRotorReversals >= RotorAlignReversals) NextRotor();
		else if (currentRotorDirection == 0) currentRotorDirection = 1;
		else if (alignmentScore < lastAlignmentScore || directionExpireTimer < 0.0) {
			currentRotorDirection *= -1;
			currentRotorReversals++;
			directionSwitchCooldown = RotorReversalCooldown;
			directionExpireTimer = 10.0;
		}
		directionExpireTimer -= Base.DeltaTime;
		
		lastAlignmentScore = alignmentScore;
	}



	//// PRIVATE DATA ////
	static int currentRotorReversals = 0;
	static HashSet<IMyMotorStator>.Enumerator currentRotor;
	static int currentRotorDirection = 0;
	static double lastAlignmentScore = 0.0;
	static double directionSwitchCooldown = 0.0;
	static double directionExpireTimer = 0.0;
	static HashSet<IMySolarPanel> solarPanels = new HashSet<IMySolarPanel>();
	static HashSet<IMyMotorStator> rotors = new HashSet<IMyMotorStator>();



	//// PRIVATE METHODS ////
	static void FirstRotor() {
		Base.SetRotorVelocity(currentRotor.Current, 0.0);
		currentRotor = rotors.GetEnumerator();
		currentRotor.MoveNext();
		currentRotorDirection = 0;
		currentRotorReversals = 0;
	}
	static void NextRotor() {
		Base.SetRotorVelocity(currentRotor.Current, 0.0);
		currentRotor.Current.Enabled = false;
		if (!currentRotor.MoveNext()) {
			currentRotor = rotors.GetEnumerator();
			currentRotor.MoveNext();
		}
		currentRotorDirection = 0;
		currentRotorReversals = 0;
	}
}
static class TargetTracking {
	public static readonly DateTime Version = new DateTime(2023, 03, 28, 04, 27, 00);



	//// PUBLIC SETTINGS ////
	public static double MissingTargetExpireTime = 3.0;
	public static double NewTargetPriorityBias = 0.95;
	public static double TrackingJitterScale = 1.0;
	public static bool PingEnabled = false;
	public static List<MyDetectedEntityType> IgnoreTargetTypes = new List<MyDetectedEntityType>() { MyDetectedEntityType.Asteroid, MyDetectedEntityType.Planet };



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
	public static void Init(MyGridProgram program, HashSet<IMyTerminalBlock> blocks) {
		Program = program;
		Program.Echo("Initializing TargetTracking");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		CameraBlocks = blocks.OfType<IMyCameraBlock>().ToList();
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
		var hit = camera.Raycast(camera.AvailableScanRange);
		if (!IgnoreTargetTypes.Contains(hit.Type)) ConsiderTarget(hit);
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
static class Ballistics {
	public static readonly DateTime Version = new DateTime(2023, 03, 29, 07, 48, 00);



	//// PUBLIC SETTINGS ////
	public static double ProjectileVelocity = 400.0;
	public static bool ProjectileHasGravity = true;
	public static int LaunchDelay = 2; //frames



	//// PUBLIC METHODS ////
	// Remember to call this first
	public static void Init(MyGridProgram program, HashSet<IMyTerminalBlock> blocks) {
		program.Echo("Initializing SolarControl");
		program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		programmableBlock = program.Me;
		controller = blocks.OfType<IMyShipController>().FirstOrDefault();
		program.Echo("    Controller: " + (controller == null ? "No, self-velocity compensation disabled" : "Yes"));
		program.Echo("    Controller: " + (true ? "No, self-velocity compensation disabled" : "Yes"));
	}
	public static void Update() {}
	public static void Evaluate(ref Vector3D targetLead, Vector3D targetPos, Vector3D targetVel, Vector3D targetAcc) {
		if (controller != null) {
			targetPos -= controller.GetPosition();
			targetVel -= controller.GetShipVelocities().LinearVelocity;
			if (ProjectileHasGravity) targetAcc -= controller.GetNaturalGravity();
		}
		else targetPos -= programmableBlock.GetPosition();
		var travelTime = (targetPos + targetLead).Length() / ProjectileVelocity + LaunchDelay * Base.DeltaTime;
		targetLead = targetVel * travelTime + targetAcc * Math.Pow(travelTime, 2) * 0.5;
	}



	//// PRIVATE DATA ////
	static IMyProgrammableBlock programmableBlock = null;
	static IMyShipController controller = null;
}
