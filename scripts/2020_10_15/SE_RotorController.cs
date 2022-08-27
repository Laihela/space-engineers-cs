// Written by: Laihela


private List<IMyTerminalBlock> Rotors = new List<IMyTerminalBlock>();


public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	
	Rotors = FindAll("Rotor (aim)");
}


public void Main(string argument) {
	string[] data = Me.CustomData.Split(',');
	Vector3D target = new Vector3D(
		double.Parse(data[0]),
		double.Parse(data[1]),
		double.Parse(data[2])
	);
	
	Echo(Rotors.Count().ToString());
	foreach (IMyTerminalBlock block in Rotors) {
		if ((block is IMyMotorStator) == false) {
			continue;
		}
		IMyMotorStator rotor = block as IMyMotorStator;
		Vector3D targetDirection = Vector3D.TransformNormal(target - block.GetPosition(), MatrixD.Transpose(block.WorldMatrix));
		float targetAngle = (float)Math.Atan2(-targetDirection.X, targetDirection.Z);
		if (target == Vector3D.Zero) {
			targetAngle = 0f;
		}
		float deviation = targetAngle - rotor.Angle;
		if (deviation < -(float)Math.PI) {
			deviation += (float)Math.PI * 2f;
		}
		
		rotor.SetValue<float>("Velocity", deviation * 32);
	}
}


private IMyTerminalBlock Find(string name) {
	return GridTerminalSystem.GetBlockWithName(name);
}

private List<IMyTerminalBlock> FindAll(string name) {
	List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
	GridTerminalSystem.SearchBlocksOfName(name, blocks);
	return blocks;
}