IMyDoor Door1 = null;
IMyDoor Door2 = null;

public Program() {
	Runtime.UpdateFrequency = UpdateFrequency.Update1;
	Door1 = GridTerminalSystem.GetBlockWithName("Door 1") as IMyDoor;
	Door2 = GridTerminalSystem.GetBlockWithName("Door 2") as IMyDoor;
}

public void Main(string argument) {
	// Only these two lines run every tick
	if (Door1.Status == DoorStatus.Opening) Door2.CloseDoor();
	else if (Door2.Status == DoorStatus.Opening) Door1.CloseDoor();
}