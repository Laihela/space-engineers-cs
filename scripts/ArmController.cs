// Written by Laihela



//// INITIALIZATION ////
Program () {

	// Initialize classes
	Base.Init(this); // Always init Base first!
	RotorControl.Init(this, Base.GroupBlocks);
	
	// Change settings
	Base.Title = "Arm Control";
	Base.DisplayOutput(Base.GroupBlocks, "(Output)", 0);
	Base.DisplayOutput(Base.GroupBlocks.OfType<IMyCockpit>(), surfaceId:2);
	Echo("Output displays: " + Base.OutputDisplays.Count);
	Base.SetLCDTheme(Base.GroupBlocks, new Color(255, 64, 8), Color.Black);
	
	Runtime.UpdateFrequency = UpdateFrequency.Update1;

}



//// EXECUTION ////
void Main(string argument) {

	if (argument == "") {
		Base.Update(); // Always update Base first!
		RotorControl.Update();
		
		// Call Base.ProcessorLoad last, anything after wont be taken into account.
		Base.Print("CPU: " + Base.ProcessorLoad.ToString("000.000%"));
	}

}



//// CLASSES ////
static class Base {
	public static readonly DateTime Version = new DateTime(2022, 07, 19, 18, 47, 00);



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
		DisplayKeyboard(Program.Me.GetSurface(1));
		SetLCDTheme(Program.Me.GetSurface(1), new Color(128, 255, 0), new Color(0, 0, 0));
	}
	// Call this first, every frame.
	public static void Update() {
		foreach (var display in OutputDisplays) display.WriteText($"{Title} {symbol}\n", false);
		realDeltaTime = (DateTime.Now - lastUpdate).TotalSeconds;
		lastUpdate = DateTime.Now;
		UpdateSymbol();
	}
	// Write text to all output displays.
	public static void Print(object text = null) {
		foreach (var display in OutputDisplays) display.WriteText($"{text?.ToString()}\n", true);
	}
	// Write an error message to all output displays and stop the program.
	public static void Throw(object message) {
		Print("ERR: " + message.ToString());
		SetLCDTheme(Program.Me, new Color(0, 0, 0), new Color(255, 0, 0));
		SetLCDTheme(OutputDisplays, new Color(0, 0, 0), new Color(255, 0, 0));
		throw new System.Exception(message.ToString());
	}


	//// LCD CONTROL ////
	// Add target(s) to which the Print() and Throw() methods will write text to.
	// Target can be a text surface, a block, block list, etc... (see cases)
	// Tag: if provided, only adds blocks which have the tag string in their name (for collections only).
	// SurfaceId: If provided, only adds one display from the block(s) with the specified surfaceId (if it exists).
	// !!! Changes the theme of the display(s) !!!
	public static void DisplayOutput(object target, string tag = "", int surfaceId = -1) {
		
		// Cases should be ordered from least recursive to most recursive, and then from most used to least used for best performance.
		
		// Case: target is single text surface.
		var display = target as IMyTextSurface;
		if (display != null) {
			display.ContentType = ContentType.TEXT_AND_IMAGE;
			SetLCDTheme(display, new Color(128, 255, 0), new Color(0, 0, 0), 1f, 2f);
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
			
			double sensitivity = GetBlockProperty(controller, "mouse sensitivity", 1.0);
			double pitch = controller.RotationIndicator.X * sensitivity;
			double yaw = controller.RotationIndicator.Y * sensitivity;
			double roll = controller.RollIndicator;
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
		// Limit a vector between a minimum and a maximum length.
	public static Vector3D Clamp(Vector3D vec, double min, double max) {
		double len = vec.Length();
		if (len == 0.0) return vec;
		return len < min ? vec / len * min : (len > max ? vec / len * max : vec);
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
				string[] prop = line.Split(':');
				if (prop.Length != 2) continue;
				BlockProperties[block].Add(prop[0].Trim(' ').ToLower(), prop[1].Trim(' '));
			}
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

