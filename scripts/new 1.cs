// Written by Laihela

static class Base {
	public static readonly DateTime Version = new DateTime(2021, 05, 16, 17, 44, 00);
	
	public static MyGridProgram Program = null;
	public static List<IMyTerminalBlock> ConstructBlocks = new List<IMyTerminalBlock>();
	public static List<IMyTerminalBlock> GroupBlocks = new List<IMyTerminalBlock>();
	public static List<IMyTerminalBlock> GridBlocks = new List<IMyTerminalBlock>();
	
	public static double DeltaTime { get {
		return deltaTime;
	}}
	public static double ProcessorLoad { get {
		return (double)Program.Runtime.CurrentInstructionCount / Program.Runtime.MaxInstructionCount;
	}}
	public static double ExecutionTime { get {
		return (double)(Program.Runtime.LastRunTimeMs) / deltaTime;
	}}
	
	static double deltaTime = 0.0;
	static DateTime lastUpdate = DateTime.Now;
	
	// Remember to call this first
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
	}
	
	public static void Update() {
		deltaTime = (DateTime.Now - lastUpdate).TotalSeconds;
		lastUpdate = DateTime.Now;
	}
	
	// Writes text onto the programmable block display.
	public static void Print(string text = "") {
		Program.Me.GetSurface(0).WriteText(text + "\n", true);
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
	
	// Set rotor velocity in radians per second
	public static void SetRotorVelocity(IMyMotorStator rotor, double velocity) {
		double rpm = velocity * 30.0 / Math.PI;
		if (double.IsNaN(rpm)) rpm = 0.0;
		rotor.SetValue<float>("Velocity", (float)rpm);
	}
}

static class DisplayControl {
	public static readonly DateTime Version = new DateTime(2021, 05, 15, 23, 09, 00);
	
	public static Color	ContentColor =		new Color(255, 255, 255);
	public static Color	BackgroundColor =	new Color(0, 0, 0);
	public static double	TextScale =			1.0;
	public static double	TextPadding =		10.0;
	
	// Remember to call this first
	public static void Init() {
		Base.Program.Echo("Initializing DisplayControl");
		Base.Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		foreach (var block in Base.ConstructBlocks.OfType<IMyTextSurfaceProvider>()) {
			for (int i = 0; i < block.SurfaceCount; i++) {
				IMyTextSurface display = block.GetSurface(i);
				display.FontColor = ContentColor;
				display.BackgroundColor = BackgroundColor;
				display.ScriptForegroundColor = ContentColor;
				display.ScriptBackgroundColor = BackgroundColor;
				
				display.ContentType = ContentType.TEXT_AND_IMAGE;
				display.Alignment = TextAlignment.LEFT;
				display.PreserveAspectRatio = true;
				display.Font = "Monospace";
				display.TextPadding = (float)TextPadding;
				display.FontSize = (float)TextScale;
			}
		}
	}
	
	public static void Print(IMyTextSurfaceProvider block, int displayIndex, string text) {
		if (block == null) return;
		if (displayIndex >= block.SurfaceCount) return;
		IMyTextSurface display = block.GetSurface(displayIndex);
		display.WriteText(text + "\n", true);
	}
	
	public static void Clear(IMyTextSurfaceProvider block, int displayIndex = -1) {
		if (block == null) return;
		if (displayIndex == -1) {
			for (int s = 0; s < block.SurfaceCount; s++) {
				block.GetSurface(s).WriteText("");
			}
		}
		else if (displayIndex < block.SurfaceCount) {
			block.GetSurface(displayIndex).WriteText("");
		}
	}
	
	public static void SetTextSize(IMyTextSurfaceProvider block, int displayIndex = -1, double scale = 1.0, double padding = 5.0) {
		if (block == null) return;
		if (displayIndex == -1) {
			for (int s = 0; s < block.SurfaceCount; s++) {
				IMyTextSurface display = block.GetSurface(s);
				display.TextPadding = (float)padding;
				display.FontSize = (float)scale;
			}
		}
		else if (displayIndex < block.SurfaceCount) {
			IMyTextSurface display = block.GetSurface(displayIndex);
			display.TextPadding = (float)padding;
			display.FontSize = (float)scale;
		}
	}
	
	public static void ShowDebug(IMyTextSurfaceProvider block, int displayIndex = -1) {
		if (block == null) return;
		if (displayIndex == -1) {
			for (int s = 0; s < block.SurfaceCount; s++) {
				IMyTextSurface display = block.GetSurface(s);
				display.WriteText(string.Join("\n",
					$"SCREEN {s}",
					$"{display.SurfaceSize.X.ToString("0")}x{display.SurfaceSize.Y.ToString("0")}"
				));
			}
		}
		else if (displayIndex < block.SurfaceCount) {
			IMyTextSurface display = block.GetSurface(displayIndex);
			block.GetSurface(displayIndex).WriteText(string.Join("\n",
				$"SCREEN {displayIndex}",
				$"{display.SurfaceSize.X.ToString("0")}x{display.SurfaceSize.Y.ToString("0")}"
			));
		}
	}
}



Program () {
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	Echo(""); // Clear output
	Base.Init(this); // Always init Base first!
	
	DisplayControl.BackgroundColor = new Color(0, 80, 128);
	DisplayControl.ContentColor = new Color(128, 224, 255);
	DisplayControl.Init();
	
	DisplayControl.SetTextSize(Me, 0, 1.0, 5.0);
	DisplayControl.SetTextSize(Me, 1, 3.5, 5.0);
	
	//Me.CustomData = "Kits: " + Base.GridBlocks.OfType<IMySurvivalKit>().Count;
}



void Main(string command) { switch (command) {
	
	default:
		Base.Update();
		
		DisplayControl.Clear(Me);
		DisplayControl.Print(Me, 0, "STATION\n");
		
		double currentCargo = 0.0;
		double currentPower = 0.0;
		double currentGen = 0.0;
		double currentIce = 0.0;
		double maxCargo = 0.0;
		double maxPower = 0.0;
		double maxGen = 0.0;
		List<MyInventoryItem> items = new List<MyInventoryItem>();
		foreach (var block in Base.GridBlocks) {
			IMyPowerProducer producer = block as IMyPowerProducer;
			IMyBatteryBlock battery = block as IMyBatteryBlock;
			
			if (block.InventoryCount > 0) {
				for (int i = 0; i < block.InventoryCount; i++) {
					IMyInventory inventory = block.GetInventory(i);
					currentCargo += (double)inventory.CurrentVolume;
					maxCargo += (double)inventory.MaxVolume;
					inventory.GetItems(items);
					foreach (var item in items) {
						if (item.Type.SubtypeId == "Ice") currentIce += (double)item.Amount;
					}
				}
			}
			if (block.IsWorking == false) continue;
			if (producer != null) {
				currentGen += (double)producer.CurrentOutput;
				maxGen += (double)producer.MaxOutput;
			}
			if (battery != null) {
				currentPower += (double)battery.CurrentStoredPower;
				maxPower += (double)battery.MaxStoredPower;
			}
		}
		int battLifeD = (int)(currentPower / currentGen / 24.0);
		int battLifeH = (int)(currentPower / currentGen % 24.0);
		int battLifeM = (int)(currentPower / currentGen * 60.0 % 60.0);
		int battLifeS = (int)(currentPower / currentGen * 3600.0 % 60.0);
		
		DisplayControl.Print(Me, 0, string.Format("LIFE {0}d {1}h {2}m {3}s", battLifeD, battLifeH, battLifeM, battLifeS));
		DisplayControl.Print(Me, 0, "CHRG " + (currentPower / maxPower).ToString("000% ") + (currentPower * 1000.0).ToString("0.000kWh"));
		DisplayControl.Print(Me, 0, "LOAD " + (currentGen / maxGen).ToString("000% ") + (currentGen * 1000.0).ToString("0.000kW"));
		DisplayControl.Print(Me, 0, "CRGO " + (currentCargo / maxCargo).ToString("000% ") + (currentCargo * 1000.0).ToString("0L"));
		DisplayControl.Print(Me, 0, "ICE  " + (currentIce).ToString("0k"));
		
		DisplayControl.Print(Me, 1, "CPU " + Base.ProcessorLoad.ToString("0.000%"));
		DisplayControl.Print(Me, 1, "RUN " + Base.ExecutionTime.ToString("0.000ms/s"));
	break;

}}