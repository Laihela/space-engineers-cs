// Written by Laihela



//// INITIALIZATION ////
Program () {

	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	Echo(""); // Clear the output window in terminal
	
	// Initialize classes
	Base.Init(this); // Always init Base first!
	Base.DisplayOutput(Base.GridBlocks, "ScriptOutput");
	Base.SetLCDTheme(Base.ConstructBlocks, new Color(128, 255, 32), Color.Black);
	
	// Change settings
	Base.Title = "Piston Control";
	
}



//// EXECUTION ////
void Main(string argument) {

	if (argument == "") {
		Base.Update(); // Always update Base first!
		
		
		
		Base.Print("Current speed: " + Velocity.ToString("0.000 m/s"));
		
		
		
		// Call Base.ProcessorLoad last, anything after wont be taken into account.
		Base.Print("CPU: " + Base.ProcessorLoad.ToString("000.000%"));
	}

	else if (argument.Contains("Add")) {
		Velocity += float.Parse(argument.Split(':')[1]);
		foreach (var block in Base.GroupBlocks) {
			var piston = block as IMyPistonBase;
			if (piston == null) continue;
			//piston.Enabled = Math.Abs(Velocity) > 0.000001f;
			piston.Velocity = Velocity;
		}
	}

	else if (argument.Contains("Set")) {
		Velocity = float.Parse(argument.Split(':')[1]);
		foreach (var block in Base.ConstructBlocks) {
			var piston = block as IMyPistonBase;
			if (piston == null) continue;
			//piston.Enabled = Math.Abs(Velocity) > 0.000001f;
			piston.Velocity = Velocity;
		}
	}

}



//// STATE ////
float Velocity = 0.0f;


//// CLASSES ////
static class Base {
	public static readonly DateTime Version = new DateTime(2022, 06, 30, 16, 26, 00);



	//// PUBLIC SETTINGS ////
	public static string Title = "Title";



	//// PUBLIC DATA ////
	public static List<IMyTerminalBlock> ConstructBlocks = new List<IMyTerminalBlock>();
	public static List<IMyTerminalBlock> GroupBlocks = new List<IMyTerminalBlock>();
	public static List<IMyTerminalBlock> GridBlocks = new List<IMyTerminalBlock>();
	public static List<IMyTextSurface> OutputDisplays = new List<IMyTextSurface>();
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
		updateSymbol();
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

	// Add target(s) to which the Print() and Throw() methods will write text to.
	// Target can be a text surface, a block, block list, etc... (see cases)
	// Tag: if provided, only adds blocks which have the tag string in their name.
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
		// Case: Target is a list of displays, call recursively for each display. (1 layer recursion)
		var displayList = target as List<IMyTextSurface>;
		if (displayList != null) {
			// no need to pass tag or surfaceId for a text surface.
			foreach (var d in displayList) DisplayOutput(d);
			return;
		}
		
		// Case: Target is a list of terminal blocks, call recursively for each block. (2 layer recursion)
		var blockList = target as List<IMyTerminalBlock>;
		if (blockList != null) {
			foreach (var b in blockList) {
				if (tag != "" && b.CustomName.Contains(tag) == false) continue;
				DisplayOutput(b, surfaceId:surfaceId);
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
		
		// Cases should be ordered from least recursive to most recursive, and then from most used to least used for best performance.
		
		// Case: target is single display.
		IMyTextSurface display = target as IMyTextSurface;
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
		IMyTextSurfaceProvider block = target as IMyTextSurfaceProvider;
		if (block != null) {
			for(int x = 0; x < block.SurfaceCount; x++) SetLCDTheme(block.GetSurface(x), contentColor, backgroundColor, fontSize, textPadding);
			return;
		}
		// Case: Target is a list of displays, call recursively for each display. (1 layer recursion)
		List<IMyTextSurface> displayList = target as List<IMyTextSurface>;
		if (displayList != null) {
			foreach (var x in displayList) SetLCDTheme(x, contentColor, backgroundColor, fontSize, textPadding);
			return;
		}
		
		// Case: Target is a list of terminal blocks, call recursively for each block. (2 layer recursion)
		List<IMyTerminalBlock> blockList = target as List<IMyTerminalBlock>;
		if (blockList != null) {
			foreach (var x in blockList) SetLCDTheme(x, contentColor, backgroundColor, fontSize, textPadding);
			return;
		}
	}
	
	// Returns current time in seconds.
	public static double Now() {
		return DateTime.Now.Ticks / 1e7;
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
		return a * (1 - time) + b * time;
	}
	// Return the modulus of a number.
	// Result is always positive, works consistently with negative input values.
	public static double Mod(double v, double m) {
		v = v % m;
		return v < 0 ? v + m: v;
	}
	
	// Set rotor velocity in radians per second, NaN safety included.
	public static void SetRotorVelocity(IMyMotorStator rotor, double velocity) {
		rotor.TargetVelocityRad = (double.IsNaN(velocity) ? 0.0f : (float)velocity);
	}



	//// PRIVATE METHODS ////
	static void updateSymbol() {
		if       (symbol == "|")  symbol = "/";
		else if  (symbol == "/")  symbol = "-";
		else if  (symbol == "-")  symbol = "\\";
		else                      symbol = "|";
	}
}

