// Raydar Script
// Written by: Laihela

// Detects objects using the Raycast() function of multiple cameras, and outputs their GPS locations into an LCD Panel.
// You need these to make it work: 1 Programmable block, 1 Timer Block, at least 1 Camera, 1 LCD Panel.
// Optional: 1 Interior Light.

int ScanRange = 1000; // Maximum scan range in meters. Affects scan frequency. Shorter range means faster scanning.
float ScanAngle = 45f; // Maximum scan angle in degrees. Higher values work better for shorter ranges. Cannot be higher than 45.

List<IMyTerminalBlock> RaydarBlocks = new List<IMyTerminalBlock>();
Dictionary<Int64, bool> EntityIdList = new Dictionary<Int64, bool>();
IMyTextPanel Panel;
IMyInteriorLight Light;

Random Rand = new Random();
public Program()
{
  GridTerminalSystem.SearchBlocksOfName("(Raydar)", RaydarBlocks);
  for (int i=0; i<RaydarBlocks.Count; i++)
  {
    if (RaydarBlocks[i] is IMyTextPanel)
    {
      Panel = RaydarBlocks[i] as IMyTextPanel;
      Panel.WritePublicText("LOG_START");
    }
    else if (RaydarBlocks[i] is IMyInteriorLight) 
    { 
      Light = RaydarBlocks[i] as IMyInteriorLight; 
    }
  }
}

public void Main()
{ 
  Light.BlinkLength = 0;
  for (int i=0; i<RaydarBlocks.Count; i++)
  {
    IMyCameraBlock Camera = RaydarBlocks[i] as IMyCameraBlock;
    if (Camera is IMyCameraBlock && !Camera.EnableRaycast)
    {
      Camera.EnableRaycast = true;
    }
    if (Camera is IMyCameraBlock)
    {
      if (Camera.AvailableScanRange > ScanRange +Rand.Next(ScanRange))
      {
        float Pitch = (Rand.Next(100)-50)/100f *ScanAngle;
        float Yaw = (Rand.Next(100)-50)/100f *ScanAngle;
        MyDetectedEntityInfo DetectedObject = Camera.Raycast(Camera.AvailableScanRange, Pitch, Yaw);

        String Relationship = DetectedObject.Relationship.ToString(); 
        Int64 EntityId = DetectedObject.EntityId;
        String Type = DetectedObject.Type.ToString();

        if (Type != "None")
        { 
          Light.BlinkLength = 100;
          if (!EntityIdList.ContainsKey(EntityId))
          {
            EntityIdList[EntityId] = true;

            Vector3 Position = DetectedObject.Position; 
            Double X = Math.Round(Position.X); 
            Double Y = Math.Round(Position.Y); 
            Double Z = Math.Round(Position.Z);

            String GPS = String.Format("GPS:{0}:{1}:{2}:{3}:", Type+" ("+Relationship+")", X, Y, Z);
            Panel.WritePublicText("\n"+GPS, true);
          }
        }
      }
    }
  }
}