// Written by Laihela



//// INITIALIZATION ////
Program () {

	// Initialize classes
	Base.Init(this); // Always init Base first!
	ShipControl.Init(this, Base.GridBlocks);
	SimpleMissile.Init(this, Base.GridBlocks);
	
	// Change settings
	Base.Title = "Simple Missile";
	Base.SetLCDTheme(Base.GridBlocks, Color.Orange, Color.Black);
	ShipControl.GyroMaxDelta = 0.1;
	ShipControl.MaxShipSpeed = 100.0;
	ShipControl.APPROACH_SLOW_DOWN = 30.0;
	SimpleMissile.DropTime = 0.5f;
	SimpleMissile.DeployAntennaRange = 20000;
	
	Runtime.UpdateFrequency = UpdateFrequency.Update1;

}



//// EXECUTION ////
void Main(string argument, UpdateType updateType) {
	try {

		Base.Update(); // Always update Base first!
		ShipControl.Update();
		SimpleMissile.Update();

		if (argument.ToLower().Contains("gps")) {
			string[] bits = argument.Split(':');
			if (bits.Length > 4) {
				guidance = "gps";
				target = new Vector3D(double.Parse(bits[2]), double.Parse(bits[3]), double.Parse(bits[4]));
				SimpleMissile.Deploy();
			}
			else Base.Warn("Incorrect GPS format: " + argument);
		}

		if (SimpleMissile.IsDeployed && SimpleMissile.DropTime < 0.001) {
			if (guidance == "gps") {
				Vector3D targetDirection = target - SimpleMissile.Warhead.GetPosition();
				ShipControl.SetVelocity(ShipControl.Controllers[0].WorldMatrix.Forward);
				ShipControl.SetAngle(Vector3D.Normalize(targetDirection));
				if (targetDirection.Length() < 5.0) SimpleMissile.Warhead.Detonate();
			}
			else ShipControl.CentripetalFlight(0.02);
		}

		// Call Base.ProcessorLoad last, anything after wont be taken into account.
		Base.Print("CPU: " + Base.ProcessorLoad.ToString("000.000%"));

	}
	catch (System.Exception exception) { Base.Throw(exception); }
}



//// STATE ////
string guidance = "manual";
Vector3D target = Vector3D.Zero;



//// CLASSES ////
static class Base {
	public static readonly DateTime Version = new DateTime(2022, 09, 17, 01, 38, 00);



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
		return Program.Runtime.TimeSinceLastRun.TotalSeconds;
	}}
	// Real-time seconds between updates, will increase if simulation speed drops.
	public static double RealDeltaTime { get {
		return realDeltaTime;
	}}
	// Ratio of currently used program instructions to max allowed instructions. (0 to 1)
	public static double ProcessorLoad { get {
		return (double)Program.Runtime.CurrentInstructionCount / Program.Runtime.MaxInstructionCount;
	}}



	//// PRIVATE DATA ////
	static MyGridProgram Program;
	static DateTime lastUpdate = DateTime.Now;
	static double realDeltaTime = 0.0;
	static string symbol = "";
	static StringBuilder printBuilder = new StringBuilder();
	static StringBuilder warnBuilder = new StringBuilder();
	static Color contentColor = new Color(179, 237, 255);
	static Color backgroundColor = new Color(0, 88, 151);



	//// PUBLIC METHODS ////
	// Remember to call this first, before all other classes.
	public static void Init(MyGridProgram program) {
		Program = program;
		Program.Echo("Initializing Base");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		GetGroups();
		GetBlocks();
		ReadProperties();
		
		Program.Echo("    ConstructTerminalBlocks: " + ConstructBlocks.Count);
		Program.Echo("    GroupTerminalBlocks: " + GroupBlocks.Count);
		Program.Echo("    GridTerminalBlocks: " + GridBlocks.Count);
		Program.Echo("    MyBlockGroups: " + MyBlockGroups.Count);
		
		//Program.Me.GetSurface(0).ContentType = ContentType.TEXT_AND_IMAGE;
		//SetLCDTheme(Program.Me, new Color(128, 255, 0), new Color(0, 0, 0), 1f, 2f);
		DisplayOutput(Program.Me.GetSurface(0));
		SetLCDTheme(Program.Me, new Color(128, 255, 0), new Color(0, 0, 0), 1f, 2f);
		DisplayKeyboard(Program.Me.GetSurface(1));
	}
	// Call this first, every frame.
	public static void Update() {
		FlushOutput();
		Print($"{Title} {symbol}");
		realDeltaTime = (DateTime.Now - lastUpdate).TotalSeconds;
		lastUpdate = DateTime.Now;
		UpdateSymbol();
	}
	// Write text to all output displays.
	public static void Print(object message) {
		//foreach (var display in OutputDisplays) display.WriteText(message.ToString() + "\n", true);
		printBuilder.Append(message).Append('\n');
	}
	public static void Warn (object message) {
		warnBuilder.Append(message).Append('\n');
	}
	// Write an error message to all output displays and stop the program.
	public static void Throw(object message) {
		Print("ERR: " + message.ToString());
		FlushOutput();
		SetLCDTheme(Program.Me, new Color(0, 0, 0), new Color(255, 0, 0));
		SetLCDTheme(OutputDisplays, new Color(0, 0, 0), new Color(255, 0, 0));
		throw new System.Exception(message.ToString());
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
	public static void SetLCDTheme(object target, Color? contentColor = null, Color? backgroundColor = null, float fontSize = -1f, float textPadding = -1f) {
		
		// Cases should be ordered from most specific to least specific, then from least
		// recursive to most recursive, and then from most used to least used for best performance.
		
		// Case: target is single display.
		var display = target as IMyTextSurface;
		if (display != null) {
			Color content = contentColor.GetValueOrDefault(new Color(179, 237, 255));
			Color background = backgroundColor.GetValueOrDefault(new Color(0, 88, 151));
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
			for(int x = 0; x < block.SurfaceCount; x++) SetLCDTheme(block.GetSurface(x), contentColor, backgroundColor, fontSize, textPadding);
			return;
		}
		
		// Case: target is a collection, call recursively for each item. (1 or more layers of recursion depending on collection type)
		var items = target as IEnumerable;
		if (items != null) {
			foreach (var x in items) SetLCDTheme(x, contentColor, backgroundColor, fontSize, textPadding);
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
		Vector3 v = -Base.DirectionToBlockSpace(velocity, gyro);
		gyro.Pitch = v.X;
		gyro.Yaw = v.Y;
		gyro.Roll = v.Z;
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
		Vector3D axis = Vector3D.Normalize(Vector3D.Cross(to, from));
		Vector3D result = axis * angle;
		if (double.IsNaN(result.X)) return Vector3D.Zero;
		else return axis * angle;
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
		if       (symbol == "|")  symbol = "/";
		else if  (symbol == "/")  symbol = "-";
		else if  (symbol == "-")  symbol = "\\";
		else                      symbol = "|";
	}
	static void GetGroups() {
		List<IMyBlockGroup> blockGroups = new List<IMyBlockGroup>();
		Program.GridTerminalSystem.GetBlockGroups(blockGroups);
		foreach(var blockGroup in blockGroups) {
			List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
			blockGroup.GetBlocks(blocks);
			if (blocks.Contains(Program.Me) == false) continue;
			foreach (var block in blocks) GroupBlocks.Add(block);
			MyBlockGroups.Add(blockGroup.Name, new HashSet<IMyTerminalBlock>(blocks));
		}
	}
	static void GetBlocks() {
		List<IMyTerminalBlock> terminalBlocks = new List<IMyTerminalBlock>();
		Program.GridTerminalSystem.GetBlocks(terminalBlocks);
		foreach (var block in terminalBlocks) {
			if (block.IsSameConstructAs(Program.Me)) ConstructBlocks.Add(block);
			if (block.CubeGrid == Program.Me.CubeGrid) GridBlocks.Add(block);
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
		foreach (var display in OutputDisplays) display.WriteText(warnBuilder.ToString() + printBuilder.ToString());
		printBuilder.Clear();
	}
}
static class ShipControl {
	public static readonly DateTime Version = new DateTime(2022, 09, 16);



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
	public static void NormalFlight(double response = 0.5, double gyroResponse = 0.1, bool cancelGravity = true) {
		if (Controllers.Count == 0) Base.Throw("Ship has no controller");
		gyroResponse = Base.Clamp(gyroResponse, 0.0, 1.0);
		response = Base.Clamp(response, 0.0, 1.0);
		
		List<Vector3D> input = Base.GetInput(Controllers, true);
		
		SetRotation(input[1] * 2 * Math.PI, gyroResponse);
		if (Controllers[0].DampenersOverride == false) SetThrust(input[0], cancelGravity);
		else SetVelocity(input[0] * MaxShipSpeed, response, cancelGravity);
	}
	public static void CentripetalFlight(double response = 0.5, double gyroResponse = 0.1, bool cancelGravity = true) {
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
	public static void ExperimentalFlight(double response = 0.1, double gyroResponse = 0.1, bool cancelGravity = true) {
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
		if (targetUp != null) rollDelta = Base.GetRotation(up, targetUp.Value);
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
			velocity = Base.Clamp((velocity - myVelocity) * response, 0.0, GyroMaxDelta) + myVelocity;
		}
		foreach (var gyro in Gyroscopes) Base.SetGyroVelocity(gyro, velocity);
	}
	public static void SetRotation(MatrixD velocity, double response = 1.0) {
		Vector3D axis = Vector3D.Zero;
		double angle = 0.0;
		QuaternionD.CreateFromRotationMatrix(velocity).GetAxisAngle(out axis, out angle);
		SetRotation(axis * angle, response);
	}
	/*public static void AddRotation(MatrixD velocity, double response = 1.0) { /// BROKEN ///
		Vector3D euler = Vector3D.Zero;
		MatrixD.GetEulerAnglesXYZ(ref velocity, out euler);
		AddRotation(euler, response);
	}*/
	
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
static class SimpleMissile {
	public static readonly DateTime Version = new DateTime(2023, 03, 20, 02, 26, 0);



	//// PUBLIC DATA ////
	public static MyGridProgram Program;
	public static IMyRemoteControl Controller = null;
	public static IMyMotorBase Connector = null;
	public static IMyWarhead Warhead = null; // TODO: multiple warheads
	public static bool IsDeployed { get { return isDeployed; } }
	public static bool LaunchWithRotorDisplacement = false;
	public static double MaxRotorDisplacement = -0.11;
	public static double DropTime = 0.0;
	public static double DeployAntennaRange = 10000.0;



	//// PUBLIC METHODS ////
	// Remember to call this first
	public static void Init(MyGridProgram program, HashSet<IMyTerminalBlock> blocks) {
		Program = program;
		Program.Echo("Initializing SimpleMissile");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		Controller = blocks.OfType<IMyRemoteControl>().FirstOrDefault();
		Program.Echo("    Controller: " + (Controller != null));
		Connector = GetConnector();
		Program.Echo("    Connector: " + (Connector != null));
		Warhead = blocks.OfType<IMyWarhead>().FirstOrDefault();
		Program.Echo("    Warhead: " + (Warhead != null));
		
		if (Program.Me.CustomData.Contains("SimpleMissile") == false) Program.Me.CustomData = "SimpleMissile\n" + Program.Me.CustomData;
		Standby();
	}
	// Call this every frame
	public static void Update() {
		if (Connector == null) Connector = GetConnector();
		if (isDeployed) {
			if (Warhead != null) Warhead.IsArmed = true;
			if (DropTime > 0.0) DropTime -= Base.RealDeltaTime;
		}
		else if (Controller.IsUnderControl) Deploy();
	}
	public static void Deploy() {
		if (isDeployed) return;
		if (Connector == null) Connector = GetConnector();
		foreach (var block in Base.GridBlocks.OfType<IMyFunctionalBlock>()) {
			block.Enabled = true;
			var battery = block as IMyBatteryBlock;
			var tank = block as IMyGasTank;
			var antenna = block as IMyRadioAntenna;
			if (battery != null) battery.ChargeMode = ChargeMode.Discharge;
			if (tank != null) tank.Stockpile = false;
			if (antenna != null) antenna.Radius = (float)DeployAntennaRange;
		}
		if (LaunchWithRotorDisplacement && isConnectorExtended == false) {
			ExtendConnectorRotor();
			return;
		}
		Connector?.Detach();
		isDeployed = true;
	}



	//// PRIVATE DATA ////
	static bool isDeployed = false;
	static bool isConnectorExtended = false;



	//// PRIVATE METHODS ////
	static void Standby() {
		foreach (var block in Base.GridBlocks.OfType<IMyFunctionalBlock>()) {
			var battery = block as IMyBatteryBlock;
			var tank = block as IMyGasTank;
			if (battery != null) battery.ChargeMode = ChargeMode.Recharge;
			if (tank != null) tank.Stockpile = true;
			if (block == Connector || block == tank || block == battery || block == Program.Me) {
				block.Enabled = true;
			}
			else block.Enabled = false;
		}
	}
	static IMyMotorBase GetConnector() {
		var motors = new List<IMyMotorBase>();
		Program.GridTerminalSystem.GetBlocksOfType(motors);
		foreach (var motor in motors) {
			var head = motor.Top;
			if (head == null) continue;
			if (head.CubeGrid == Program.Me.CubeGrid) return motor;
		}
		return null;
	}
	static void ExtendConnectorRotor() {
		var rotor = Connector as IMyMotorStator;
		if (rotor == null) return;
		rotor.Displacement = (float)MaxRotorDisplacement;
		isConnectorExtended = true;
	}
}

