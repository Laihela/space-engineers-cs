// KillVehicle.cs
// Written by Laihela



//// SETTINGS ////
static Vector3D HOVER_POSITION = new Vector3D(-3, 2, 0);



//// INITIALIZATION ////
Program () {

	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	Echo(""); // Clear the output window in terminal
	
	// Initialize classes
	Base.Init(this); // Always init Base first!
	Sensors.Init(this, Base.ConstructBlocks);
	ShipControl.Init(this, Base.ConstructBlocks);
	
	// Change settings
	Base.Title = "KV Test";
	Sensors.PreventDetectorTurretFiring = true;
	ShipControl.MaxApproachSpeed = 2.0;
}



//// EXECUTION ////
void Main(string argument) {

	if (argument == "") {
		Base.Update(); // Always update Base first!
		
		//Control();
		MyDetectedEntityInfo target = Sensors.Detect();
		ShipControl.FlyTo(target.Position + Vector3D.Transform(HOVER_POSITION, target.Orientation), target.Velocity, target.Orientation);
		
		Sensors.LateUpdate();
		
		// Call Base.ProcessorLoad last, anything after wont be taken into account.
		Base.Print("CPU: " + Base.ProcessorLoad.ToString("000.000%"));
	}

	else if (argument.Contains("Offset")) {
		string[] props = argument.Split(':')[1].Split(',');
		HOVER_POSITION = new Vector3D(double.Parse(props[0]), double.Parse(props[1]), double.Parse(props[2]));
	}
}



//// CLASSES ////
static class Base {
	public static readonly DateTime Version = new DateTime(2022, 06, 27, 05, 00, 00);



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
		Program.Me.GetSurface(0).WriteText($"{Title} {GetSymbol()}\n");
		deltaTime = (DateTime.Now - lastUpdate).TotalSeconds;
		lastUpdate = DateTime.Now;
	}
	// Writes text onto the programmable block display.
	public static void Print(object text = null) {
		Program.Me.GetSurface(0).WriteText(text?.ToString() + "\n", true);
	}
	// Write an error to the programmable block display and stop the program.
	public static void Throw(object message) {
		Print("ERR: " + message.ToString());
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
	
	// Set rotor velocity in radians per second.
	public static void SetRotorVelocity(IMyMotorStator rotor, double velocity) {
		rotor.TargetVelocityRad = (float)(double.IsNaN(velocity) ? 0.0 : velocity);
	}
	
}
static class Sensors {
	public static readonly DateTime Version = new DateTime(2022, 06, 25, 05, 49, 00);



	//// PUBLIC SETTINGS ////
	public static bool PreventDetectorTurretFiring = false;



	//// PUBLIC DATA ////
	public static List<IMySensorBlock> SensorBlocks;
	public static List<IMyLargeTurretBase> TurretBlocks;



	//// PRIVATE DATA ////
	static MyGridProgram Program;



	//// PUBLIC METHODS ////
	// Remember to call this first
	public static void Init(MyGridProgram program, List<IMyTerminalBlock> blocks) {
		Program = program;
		Program.Echo("Initializing Sensors");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		SensorBlocks = blocks.OfType<IMySensorBlock>().ToList();
		Program.Echo("    SensorBlocks: " + SensorBlocks.Count());
		TurretBlocks = blocks.OfType<IMyLargeTurretBase>().ToList();
		Program.Echo("    TurretBlocks: " + TurretBlocks.Count());
	}
	// Call this every frame after everything else
	public static void LateUpdate() {
		foreach (IMyLargeTurretBase turret in TurretBlocks) {
			turret.Enabled = turret.GetTargetedEntity().Position == Vector3D.Zero || !PreventDetectorTurretFiring;
		}
	}
	// Get detected entity from sensors and turrets.
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
static class ShipControl {
	public static readonly DateTime Version = new DateTime(2022, 06, 27, 15, 33, 00);



	//// PUBLIC SETTINGS ////
	public static double MaxShipSpeed = 1000.0;
	public static double GyroMaxDelta = 0.15;
	public static double MouseSensitivity = 0.03;
	// Turn off thrusters below this value to conserve fuel/power (BROKEN)
	//public static double MinThrust = 0.0;
	
	//// AUTO PILOT SETTINGS ////
	public static double GyroResponse = 0.2; // 0 to 1
	public static double VelocityResponse = 0.3; // 0 to 1
	public static double HomingResponse = 0.5; // 0 to 1
	public static double MaxApproachSpeed = 200.0;
	// Approximate mass to thrust ratio (kg / kN) (TODO: Calculate automatically)
	public static double APPROACH_SLOW_DOWN = 37.7;



	//// PUBLIC DATA ////
	public static MyGridProgram Program;
	public static IMyShipController Controller = null;
	public static List<IMyGyro> Gyroscopes = new List<IMyGyro>();
	public static List<IMyThrust> Thrusters = new List<IMyThrust>();
	public static List<ThrusterGroup> ThrusterGroups = new List<ThrusterGroup>();



	//// PUBLIC METHODS ////
	// Remember to call this first
	public static void Init(MyGridProgram program, List<IMyTerminalBlock> blocks) {
		Program = program;
		Program.Echo("Initializing ShipControl");
		Program.Echo("  Version: " + Version.ToString("yy.MM.dd.HH.mm"));
		
		Gyroscopes = blocks.OfType<IMyGyro>().ToList();
		Thrusters = blocks.OfType<IMyThrust>().ToList();
		Controller = blocks.OfType<IMyShipController>().Where(s => s.IsMainCockpit).FirstOrDefault();
		if (Controller == null) Controller = blocks.OfType<IMyShipController>().FirstOrDefault();
		Program.Echo("    Controller: " + (Controller == null ? "none" : Controller.CustomName));
		if (Controller == null) Program.Echo("      WARNING: No cockpit or remote control, functionality limited!");
		Program.Echo("    Gyroscopes: " + Gyroscopes.Count);
		Program.Echo("    Thrusters: " + Thrusters.Count);
		
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
		if (ShipControl.Controller == null) Base.Throw("Ship has no controller");
		
		// Reduce unnecessary division operations
		double perSecond = 1.0 / Base.DeltaTime;
		// Pos(ition), Rot(ation), Vel(ocity), Acc(eleration)
		Vector3D myPos = ShipControl.Controller.CenterOfMass;
		Vector3D myVel = ShipControl.Controller.GetShipVelocities().LinearVelocity;
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
		ShipControl.AddRotation(targetRotVel, 1.0); /// broken for some reason ///
		Base.Print(targetRotVel);
		
		lastTargetVel = targetVel;
		lastTargetAcc = targetAcc;
		lastTargetFwd = targetFwd;
		lastTargetUp = targetUp;
	}
	public static void Fly(double response = 0.5, double gyroResponse = 0.1) {
		response = Base.Clamp(response, 0.0, 1.0);
		gyroResponse = Base.Clamp(gyroResponse, 0.0, 1.0);
		
		Vector3D linearVelocity = Controller.GetShipVelocities().LinearVelocity;
		Vector3D angularVelocity = Controller.GetShipVelocities().AngularVelocity;
		Vector3D centripetalAcceleration = Vector3D.Cross(angularVelocity, linearVelocity);
		List<Vector3D> controlVectors = GetPilotInput();
		
		SetRotation(controlVectors[1] * 2 * Math.PI, gyroResponse);
		if (Controller.DampenersOverride == false) SetThrust(controlVectors[0]);
		else SetVelocity(controlVectors[0], response);
	}
	public static void FlyOrbit(double response = 0.5, double gyroResponse = 0.1) {
		response = Base.Clamp(response, 0.0, 1.0);
		gyroResponse = Base.Clamp(gyroResponse, 0.0, 1.0);
		
		Vector3D linearVelocity = Controller.GetShipVelocities().LinearVelocity;
		Vector3D angularVelocity = Controller.GetShipVelocities().AngularVelocity;
		Vector3D centripetalAcceleration = Vector3D.Cross(angularVelocity, linearVelocity);
		List<Vector3D> controlVectors = GetPilotInput();
		
		SetRotation(controlVectors[1] * 2 * Math.PI, gyroResponse);
		if (Controller.DampenersOverride == false) SetThrust(controlVectors[0]);
		else SetVelocity(controlVectors[0] * MaxShipSpeed, response);
		AddAcceleration(centripetalAcceleration);
	}
	/* public static void InterceptTarget(List<Vector3D> targetVectors) {
		Vector3D position = Controller.GetPosition();
		Vector3D velocity = Controller.GetShipVelocities().LinearVelocity;
		while (targetVectors.Count < 2) targetVectors.Add(Vector3D.Zero);
		Vector3D targetPosition = targetVectors[0];
		Vector3D targetVelocity = targetVectors[1];
		Vector3D target = Vector3D.Normalize(targetPosition - position) * 100.0 + targetVelocity;
		SetLinearVelocity(target, 0.0);
		SetAngle(velocity, targetVelocity - velocity);
	}
	*/
	
	//// MANUAL PILOTING ////
	public static void SetAngle(Vector3D targetForward, Vector3D? targetUp = null, double response = 0.1) {
		if (Controller == null) Base.Throw("SetAngle failed: Ship has no controller");
		response = Base.Clamp(response, 0.0, 1.0);
		Vector3D forward = Controller.WorldMatrix.Forward;
		Vector3D up = Controller.WorldMatrix.Up;
		Vector3D pitchYawDelta = Base.GetRotation(forward, targetForward);
		Vector3D rollDelta = Vector3D.Zero;
		if (targetUp != null) rollDelta = Base.GetRotation(up, targetUp.Value);
		Vector3D targetVelocity = (pitchYawDelta + rollDelta) * 0.5 / Base.DeltaTime;
		Vector3D velocity = Controller.GetShipVelocities().AngularVelocity;
		SetRotation(targetVelocity * response - velocity * velocity.Length());
	}
	public static void SetAngle(MatrixD orientation, double response = 0.1) {
		SetAngle(orientation.Forward, orientation.Up, response);
	}
	public static void SetRotation(Vector3D velocity, double response = 1.0) {
		response = Base.Clamp(response, 0.0, 1.0);
		if (Controller != null) {
			Vector3D myVelocity = Controller.GetShipVelocities().AngularVelocity;
			velocity = Base.Clamp((velocity - myVelocity) * response, 0.0, GyroMaxDelta) + myVelocity;
		}
		foreach (var gyro in Gyroscopes) {
			Vector3 v = -Base.DirectionToBlockSpace(velocity, gyro);
			gyro.Pitch = v.X;
			gyro.Yaw = v.Y;
			gyro.Roll = v.Z;
		}
	}
	public static void SetRotation(MatrixD velocity, double response = 1.0) {
		Vector3D axis = Vector3D.Zero;
		double angle = 0.0;
		QuaternionD.CreateFromRotationMatrix(velocity).GetAxisAngle(out axis, out angle);
		SetRotation(axis * angle, response);
	}
	public static void AddRotation(Vector3D velocity, double response = 1.0) {
		response = Base.Clamp(response, 0.0, 1.0);
		/*if (Controller != null) {
			Vector3D myVelocity = Controller.GetShipVelocities().AngularVelocity;
			velocity = Base.Clamp((velocity - myVelocity) * response, 0.0, GyroMaxDelta) + myVelocity;
		}*/
		foreach (var gyro in Gyroscopes) {
			Vector3 v = -Base.DirectionToBlockSpace(velocity, gyro);
			gyro.Pitch += v.X;
			gyro.Yaw += v.Y;
			gyro.Roll += v.Z;
		}
	}
	/*public static void AddRotation(MatrixD velocity, double response = 1.0) { /// BROKEN ///
		Vector3D euler = Vector3D.Zero;
		MatrixD.GetEulerAnglesXYZ(ref velocity, out euler);
		AddRotation(euler, response);
	}*/
	public static void SetThrust(Vector3D input) {
		foreach (var thrusterGroup in ThrusterGroups) {
			Vector3D d = thrusterGroup.GetWorldDirection();
			double t = Vector3D.Dot(-d, input);
			thrusterGroup.SetThrust(t);
		}
	}
	public static void SetAcceleration(Vector3D acceleration) {
		foreach (var thrusterGroup in ThrusterGroups) {
			Vector3D d = thrusterGroup.GetWorldDirection();
			double a = Vector3D.Dot(-d, acceleration);
			thrusterGroup.SetAcceleration(a);
		}
	}
	public static void AddAcceleration(Vector3D acceleration) {
		foreach (var thrusterGroup in ThrusterGroups) {
			Vector3D d = thrusterGroup.GetWorldDirection();
			double a = Vector3D.Dot(-d, acceleration);
			thrusterGroup.AddAcceleration(a);
		}
	}
	public static void SetVelocity(Vector3D targetVelocity, double response = 1.0) {
		if (Controller == null) Base.Throw("SetVelocity failed: Cannot calculate velocity on ship without controller");
		response = Base.Clamp(response, 0.0, 1.0);
		Vector3D gravity = Controller.GetNaturalGravity();
		Vector3D velocity = Controller.GetShipVelocities().LinearVelocity;
		foreach (var thrusterGroup in ThrusterGroups) {
			Vector3D d = thrusterGroup.Thrusters[0].WorldMatrix.Backward;
			Vector3D v = (targetVelocity - velocity) / Base.DeltaTime * response;
			double a = Vector3D.Dot(d, v * 0.5 - gravity);
			thrusterGroup.SetAcceleration(a);
		}
	}
	
	public static List<Vector3D> GetPilotInput() {
		List<Vector3D> vectors = new List<Vector3D>{ Vector3D.Zero, Vector3D.Zero };
		if (Controller == null) return vectors;
		double pitch = Base.Clamp(-Controller.RotationIndicator.X * MouseSensitivity, -1.0, 1.0);
		double roll = Base.Clamp(-Controller.RotationIndicator.Y * MouseSensitivity, -1.0, 1.0);
		double yaw = -Controller.RollIndicator;
		vectors[0] = Base.DirectionToWorldSpace(Controller.MoveIndicator, Controller);
		vectors[1] = Base.DirectionToWorldSpace(new Vector3D(pitch, roll, yaw), Controller);
		return vectors;
	}



	//// PRIVATE DATA ////
	static Vector3D lastTargetVel = Vector3D.Zero;
	static Vector3D lastTargetAcc = Vector3D.Zero;
	static Vector3D lastTargetFwd = Vector3D.Zero;
	static Vector3D lastTargetUp= Vector3D.Zero;



	//// PRIVATE CLASSES ////
	public class ThrusterGroup {
		public Vector3D Direction = Vector3D.Zero;
		public List<IMyThrust> Thrusters = new List<IMyThrust>();
		
		public ThrusterGroup(List<IMyThrust> thrusters, Base6Directions.Direction direction) {
			Direction = Base6Directions.GetVector(direction);
			foreach (var thruster in thrusters) {
				if (thruster.Orientation.Forward == direction) Thrusters.Add(thruster);
			}
		}
		
		public void SetThrust(double thrust) {
			foreach (var thruster in Thrusters) {
				float force = (float)thrust * thruster.MaxThrust;
				// Keep thrust overrie just above zero to prevent auto dampeners from slowing the ship.
				thruster.ThrustOverride = Math.Max(force, 0.0000000000000000000000000000000000116f);
				//thruster.Enabled = thruster.ThrustOverride >= MinThrust;
			}
		}
		public void AddThrust(double thrust) {
			foreach (var thruster in Thrusters) {
				float force = (float)thrust * thruster.MaxThrust;
				thruster.ThrustOverride += force;
				//thruster.Enabled = thruster.ThrustOverride >= MinThrust;
			}
		}
		public void SetAcceleration(double acceleration) {
			if (Controller == null) Base.Throw("SetAcceleration failed: Cannot calculate acceleration on ship without controller");
			double mass = Controller.CalculateShipMass().PhysicalMass;
			SetThrust((acceleration * mass) / GetMaxForce());
		}
		public void AddAcceleration(double acceleration) {
			if (Controller == null) Base.Throw("AddAcceleration failed: Cannot calculate acceleration on ship without controller");
			double mass = Controller.CalculateShipMass().PhysicalMass;
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
			return Thrusters[0].WorldMatrix.Forward;
		}
	}
}

