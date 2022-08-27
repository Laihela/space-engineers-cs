// Raydar Script
// Version: 20.01.15.20.01
// Written by: Laihela


// GLOBAL VARIABLES

List<IMyCameraBlock> Cameras = new List<IMyCameraBlock>();
Random Rand = new Random();
IMyTextPanel Output = null;
float ScanAngle = 30.0F;
int Counter = 0;


// MAIN FUNCTIONS

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update10;
	List<IMyTerminalBlock> blocks = GetBlocksInSameGroupAs(Me);
	if (blocks == null) {
		throw new Exception("Programmable block is not in a block group");
	}
	foreach (IMyTerminalBlock block in blocks) {
		if (block is IMyCameraBlock) {
			IMyCameraBlock camera = block as IMyCameraBlock;
			camera.EnableRaycast = true;
			Cameras.Add(camera);
		}
		else if (block is IMyTextPanel) {
			Output = block as IMyTextPanel;
		}
	}
	if (Cameras.Count == 0) {
		throw new Exception("No cameras grouped with programmable block");
	}
	if (Output == null) {
		throw new Exception("No text panel grouped with programmable block");
	}
}

public void Main(string argument) {
	IMyCameraBlock camera = Cameras[Counter];
	float pitch = ((float)Rand.NextDouble() * 2 - 1) * ScanAngle;
	float yaw = ((float)Rand.NextDouble() * 2 - 1) * ScanAngle;
	Echo(
		$"Range: {String.Format("{0:0.00}", camera.AvailableScanRange / 1000F)} km\n" +
		$"Camera: {Counter}"
	);
	MyDetectedEntityInfo result = camera.Raycast(camera.AvailableScanRange, pitch, yaw);
	if (result.IsEmpty() == false) {
		if (result.Type != MyDetectedEntityType.Planet) {
			double x = Math.Round(result.Position.X);
			double y = Math.Round(result.Position.Y);
			double z = Math.Round(result.Position.Z);
			Output.WriteText($"\n{DateTime.Now.ToString("dd/MM  H:mm")}  [{x}, {y}, {z}]  {result.Name}", true);
		}
	}
	Counter = (Counter + 1) % Cameras.Count;
}


// HELPER FUNCTIONS

List<IMyTerminalBlock> GetBlocksInSameGroupAs(IMyTerminalBlock block) {
	List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
	GridTerminalSystem.GetBlockGroups(groups);
	foreach(IMyBlockGroup group in groups) {
		List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
		group.GetBlocks(blocks);
		if (blocks.Contains(block)) {
			return blocks;
		}
	}
	return null;
}
