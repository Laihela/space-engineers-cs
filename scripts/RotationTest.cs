List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
List<IMyTerminalBlock> GridBlocks = new List<IMyTerminalBlock>();
IMyShipController Controller = null;
List<IMyGyro> Gyroscopes = new List<IMyGyro>();



Program() {
	Me.GetSurface(0).ContentType = ContentType.TEXT_AND_IMAGE;
	Me.GetSurface(0).FontSize = 2.0f;
	
	GridTerminalSystem.GetBlocks(Blocks);
	GridBlocks = Blocks.Where(b => b.CubeGrid == Me.CubeGrid).ToList();
	Controller = GridBlocks.OfType<IMyShipController>().FirstOrDefault();
	Gyroscopes = GridBlocks.OfType<IMyGyro>().ToList();
	foreach (var gyro in Gyroscopes) gyro.GyroOverride = true;
	
	if (Controller == null) Throw("Ship has no controller");
	
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
}



void Main() {
	Print("Rotation test", false);
	
	// Scale mouse input with 0.01 to reduce sensitivity
	double pitch = Controller.RotationIndicator.X * 0.01;
	double yaw = Controller.RotationIndicator.Y * 0.01;
	double roll = Controller.RollIndicator;
	Vector3D input = DirectionToWorldSpace(new Vector3D(pitch, yaw, roll), Controller);
	Vector3D rotation = Controller.GetShipVelocities().AngularVelocity;
	
	// Set gyroscope override to (input + angular velocity) so that
	// the rotation is accumulative and doesn't slow down.
	foreach (var gyro in Gyroscopes) {
		// Note how angular velocity is negated here.
		Vector3 localRotation = DirectionToBlockSpace(input - rotation, gyro);
		gyro.Pitch = localRotation.X;
		gyro.Yaw = localRotation.Y;
		gyro.Roll = localRotation.Z;
	}
	
	// Here we print the raw rotation vector in local space so that
	// a given rotation outputs the same value regardless of the ship's current angle.
	//Vector3D local = DirectionToBlockSpace(rotation, Controller);
	Print("X: " + rotation.X.ToString("0.00"));
	Print("Y: " + rotation.Y.ToString("0.00"));
	Print("Z: " + rotation.Z.ToString("0.00"));
	
	// To confirm that it's the gyroscope override wich is backwards and not GetShipVelocities().AngularVelocity,
	// we can rotate a vector with a positive angle around an axis and see which way it rotates.
	var quat = QuaternionD.CreateFromAxisAngle(Vector3D.Up, Math.PI * 0.5);
	Print(quat * Vector3D.Forward);
	// Rotating the forward vector (0, 0, -1) around the up vector (0, 1, 0) gives us the left vector (-1, 0, 0),
	// This means that turning the ship left should give a negative rotation on the up axis, and indeed this is the case.
	// This means that the output value of GetShipVelocities().AngularVelocity is correct.
}



void Print(object message, bool append = true) {
	Me.GetSurface(0).WriteText(message.ToString() + "\n", append);
}
void Throw(object message) {
	Print("ERR: " + message.ToString());
	throw new System.Exception(message.ToString());
}
Vector3D DirectionToBlockSpace(Vector3D direction, IMyCubeBlock block) {
	return Vector3D.TransformNormal(direction, MatrixD.Transpose(block.WorldMatrix));
}
Vector3D DirectionToWorldSpace(Vector3D direction, IMyCubeBlock block) {
	return Vector3D.Transform(direction, block.WorldMatrix) - block.GetPosition();
}
