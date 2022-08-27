// Written by: Laihela


#region PRIVATE_VARIABLES

const float PI = (float)(Math.PI);
const float V = 30;
const float R = 16;
List<IMyTerminalBlock> Rotors = new List<IMyTerminalBlock>();

#endregion


#region PUBLIC_FUNCTIONS

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	Rotors = FindAll("Rotor (Arm)");
}

public void Main(string argument) {
	foreach (IMyTerminalBlock block in Rotors) {
		IMyMotorStator rotor = block as IMyMotorStator;
		if (rotor == null) {
			continue;
		}
		float targetAngle = float.Parse(rotor.CustomData) / 180F * PI;
		float deviation = targetAngle - rotor.Angle;
		if (deviation < -PI) {
			deviation += PI * 2f;
		}
		float targetVelocity = Clamp(deviation * R, V);
		rotor.SetValue<float>("Velocity", targetVelocity);
	}
}

#endregion


#region PRIVATE_FUNCTIONS

IMyTerminalBlock Find(string name) {
	return GridTerminalSystem.GetBlockWithName(name);
}

List<IMyTerminalBlock> FindAll(string name) {
	List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
	GridTerminalSystem.SearchBlocksOfName(name, blocks);
	return blocks;
}

float Clamp(float value, float minmax) {
	return value < -minmax ? -minmax : (value > minmax ? minmax : value);
}
float Clamp(float value, float min, float max) {
	return value < min ? min : (value > max ? max : value);
}

#endregion