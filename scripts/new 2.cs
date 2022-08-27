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

static class RangeFinder {
	public static readonly DateTime Version = new DateTime(2021, 05, 28, 16, 15, 00);
	
	public static IMyCameraBlock CameraBlock = null;
	public static double LastRange = 0.0;
	
	public static bool Enabled {
	set {
		if (CameraBlock != null) CameraBlock.EnableRaycast = value;
	}
	get {
		if (CameraBlock == null) return false;
		return CameraBlock.EnableRaycast;
	}}
	
	// Remember to call this first
	public static void Init() {
		Base.Program.Echo("Initializing RangeFinder");
		Base.Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		CameraBlock = Base.GridBlocks.OfType<IMyCameraBlock>().FirstOrDefault();
		Base.Program.Echo("    Camera: " + (CameraBlock != null));
	}
	
	public static void Scan() {
		if (CameraBlock == null) return;
		var detectedEntity = CameraBlock.Raycast(CameraBlock.AvailableScanRange, 0.0f, 0.0f);
		if (detectedEntity.Position == Vector3D.Zero || detectedEntity.HitPosition == null) LastRange = -1.0;
		else LastRange = (CameraBlock.GetPosition() - (Vector3D)detectedEntity.HitPosition).Length() / 1000.0;
	}
}



Program () {
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	Echo(""); // Clear output
	Base.Init(this); // Always init Base first!
	
	RangeFinder.Init();
	RangeFinder.Enabled = false;
	
	DisplayControl.BackgroundColor = new Color(16, 4, 4);
	DisplayControl.ContentColor = new Color(255, 64, 64);
	DisplayControl.Init();
		
	var cockpit = Base.GridBlocks.OfType<IMyCockpit>().FirstOrDefault();
	if (cockpit == null) return;
	DisplayControl.SetTextSize(cockpit, 0, 1.2, 5.0);
	DisplayControl.SetTextSize(cockpit, 1, 1.6, 5.0);
	DisplayControl.SetTextSize(cockpit, 2, 1.8, 5.0);
}



void Main(string command) { switch (command) {
		
	case "EnableRangeFinder":
		RangeFinder.Enabled = true;
	break;
	
	case "DisableRangeFinder":
		RangeFinder.Enabled = false;
	break;
	
	case "FindRange":
		RangeFinder.Scan();
	break;
	
	default:
		Base.Update();
		
		var cockpit = Base.GridBlocks.OfType<IMyCockpit>().FirstOrDefault();
		if (cockpit == null) return;
		
		DisplayControl.Clear(cockpit);
		DisplayControl.Print(cockpit, 0, "DRILL SHIP");
		
		double currentCharge = 0.0;
		double currentCargo = 0.0;
		double currentGen = 0.0;
		double currentIce = 0.0;
		double maxCharge = 0.0;
		double maxCargo = 0.0;
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
			if (block.IsFunctional == false) continue;
			if (producer != null) {
				currentGen += (double)producer.CurrentOutput;
				maxGen += (double)producer.MaxOutput;
			}
			if (battery != null) {
				currentCharge += battery.CurrentStoredPower;
				maxCharge += battery.MaxStoredPower;
			}
		}
		
		Vector3D velocity = Base.DirectionToBlockSpace(cockpit.GetShipVelocities().LinearVelocity, cockpit);
		
		DisplayControl.Print(cockpit, 0, "MASS " + (cockpit.CalculateShipMass().TotalMass / 1000.0).ToString("0.000t"));
		DisplayControl.Print(cockpit, 0, "");
		DisplayControl.Print(cockpit, 0, "CRGO " + (currentCargo / maxCargo).ToString("000% ") + (currentCargo * 1000.0).ToString("0L"));
		//DisplayControl.Print(cockpit, 0, "ICE  " + (currentIce).ToString("0k"));
		DisplayControl.Print(cockpit, 0, "CHRG " + (currentCharge / maxCharge).ToString("000% ") + (currentCharge * 1000.0).ToString("0.000kWh"));
		DisplayControl.Print(cockpit, 0, "LOAD " + (currentGen / maxGen).ToString("000% ") + (currentGen * 1000.0).ToString("0.000kW"));
		
		DisplayControl.Print(cockpit, 1, "VELz " + (-velocity.Z).ToString("000.00m/s"));
		DisplayControl.Print(cockpit, 1, "VELy " + velocity.Y.ToString("000.00m/s"));
		DisplayControl.Print(cockpit, 1, "VELx " + velocity.X.ToString("000.00m/s\n"));
		DisplayControl.Print(cockpit, 1, "");
		DisplayControl.Print(cockpit, 1, "RUN " + Base.ExecutionTime.ToString("0.00ms/s"));
		DisplayControl.Print(cockpit, 1, "CPU " + Base.ProcessorLoad.ToString("0.00%"));
		
		DisplayControl.Print(cockpit, 2, "RANGE FINDER");
		DisplayControl.Print(cockpit, 2, "SCAN " + (RangeFinder.Enabled ? "Enabled" : "Disabled"));
		DisplayControl.Print(cockpit, 2, "RNGE " + RangeFinder.LastRange.ToString("0.0km"));
		
	break;

}}