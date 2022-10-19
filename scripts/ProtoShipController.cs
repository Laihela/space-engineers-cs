// Written by Laihela



//// INITIALIZATION ////
public static Program Current = null;
Program () {
	Base.Initialize("Prototype", this);
	
	Test test;
	
	Runtime.UpdateFrequency = UpdateFrequency.Update1;

}



//// EXECUTION ////
void Main(string argument) {
	Current = this;
	try {
		Base.InstanceUpdate();
	}
	catch (System.Exception exception) {
		Base.Throw(exception);
	}
	Current = null;
}



//// INTERFACES ////
interface IBase {
	
}



//// CLASSES ////
abstract class Base {
	public static readonly DateTime Version = new DateTime(2022, 10, 04, 00, 05, 00);
	
	static List<Base> _instances = new List<Base>();
	static MyGridProgram _program;
	static string _programName = "";
	static bool _isInitialized = false;
	static DateTime _lastUpdate = DateTime.Now;
	static double _realDeltaTime = 0.0;
	static StringBuilder _printBuffer = new StringBuilder();
	static StringBuilder _warnBuffer = new StringBuilder();
	
	
	
	protected MyGridProgram Program { get { return _program; } }
	
	
	
	static string _symbol = "";
	static string Symbol { get {
		if       (_symbol == "|")  _symbol = "/";
		else if  (_symbol == "/")  _symbol = "-";
		else if  (_symbol == "-")  _symbol = "\\";
		else                      _symbol = "|";
		return _symbol;
	}}
	
	
	
	protected Base(DateTime version) {
		_instances.Add(this);
		
		Program.Echo($"Initializing {this.GetType()}");
		Program.Echo("  Version: " + version.ToString("yy.MM.dd.HH.mm"));
	}
	
	abstract protected void Update();
	
	
	
	public static void Initialize(string title, MyGridProgram program) {
		if (_isInitialized) return;
		_programName = title;
		_program = program;
		_isInitialized = true;
	}
	
	public static void InstanceUpdate() {
		Print($"{_programName} {Symbol}");
		_realDeltaTime = (DateTime.Now - _lastUpdate).TotalSeconds;
		_lastUpdate = DateTime.Now;
		foreach (var i in _instances) i.Update();
	}
	
		// Write text to all output displays.
	public static void Print(string message) {
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
}
class Debug : Base {
	public static readonly DateTime Version = new DateTime(2022, 10, 04, 00, 05, 00);
	
	
	public Debug() : base(Version) {
		
	}
	
	
	
	override protected void Update() {
		
	}
}
class Test : Base {
	public static readonly DateTime Version = new DateTime(2022, 10, 04, 00, 05, 00);
	
	
	
	Test() : base(Version) {
		
	}
	
	
	
	override protected void Update() {
		
	}
}