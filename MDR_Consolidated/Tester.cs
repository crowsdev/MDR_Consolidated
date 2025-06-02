using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class Tester : MyGridProgram
    {
public List<IMyDoor> Doors;  

public Tester()
{
    this.Runtime.UpdateFrequency = UpdateFrequency.Update1;
    this.Doors = new List<IMyDoor>();
}
public void Main(string _arg, UpdateType _updateType)
{
    this.GridTerminalSystem.GetBlocksOfType(this.Doors, null);
    List<string> logLines = new List<string>();
    logLines.Add("DoorID        ::      State       ::      Enabled");
    foreach (IMyDoor myDoor in Doors)
    {
        logLines.Add($"{myDoor.GetId()}     ::      {myDoor.Status}     ::      {myDoor.Enabled}");
    }
    
    Echo.Invoke(string.Join("\n", logLines));
}

        public void Save()
        {
            
        }
    }
}