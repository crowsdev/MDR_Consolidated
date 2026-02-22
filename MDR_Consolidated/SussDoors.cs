using System;
using Sandbox.ModAPI.Ingame;
using VRage.Scripting.MemorySafeTypes;

namespace IngameScript
{
    /*
     * Need to handle 'hangar doors'. Read the existing ISYS script for methodology.
     * Need to spawn in and see how hangar doors are implemented, Apple will show me.
     */
    class SussDoors : Untermensch
    {
        #region Settings and variables.

        // Exclude doors with this TAG:
        public string ExclusionTag = "[xxx]";
        
        // Milliseconds to wait after opening door until closing it.
        public double CloseDelay = 3000;
        
        public MemorySafeList<IMyDoor> AllDoors = new MemorySafeList<IMyDoor>();
        private bool bDoorOpen;
        private IMyDoor ActiveDoor;
        private DateTime OpenWhen = DateTime.Now;
        private bool bLockdownRequested;

        #endregion
        
        public SussDoors(MyGridProgram ubermensch, UpdateFrequency frequency = UpdateFrequency.Update100) : base(ubermensch, frequency)
        {
            this.Frequency = frequency;
            this.Ubermensch =  ubermensch;
        }

        public override bool OnMain(string argument, UpdateType updateSource)
        {
            // if (!base.OnMain(argument, updateSource))
            // {
            //     return false;
            // }
            
            this.Ubermensch.GridTerminalSystem.GetBlocksOfType<IMyDoor>(AllDoors, x => !x.CustomName.Contains(ExclusionTag));
            
            if (bDoorOpen)
            {
                if (ActiveDoor.Status == DoorStatus.Open)
                {
                    if ((DateTime.Now - OpenWhen).TotalMilliseconds >= CloseDelay)
                    {
                        ActiveDoor.CloseDoor();
                    }
                }

                if (ActiveDoor.Status == DoorStatus.Closed)
                {
                    bDoorOpen = false;
                    AllDoors.ForEach(x =>
                    {
                        x.Enabled = true;
                    });
                }
                
                return true;
            }

            foreach (IMyDoor myDoor in AllDoors)
            {
                if (myDoor.Status == DoorStatus.Open || myDoor.Status == DoorStatus.Opening)
                {
                    OpenWhen = DateTime.Now;
                    bLockdownRequested = true;
                    bDoorOpen = true;
                    ActiveDoor = myDoor;
                    break;
                }
            }

            if (bDoorOpen && bLockdownRequested)
            {
                AllDoors.ForEach(x =>
                {
                    if (x != ActiveDoor)
                    {
                        x.CloseDoor();
                        x.Enabled = false;
                    }
                });
                bLockdownRequested = false;
            }

            return true;
        }

        public override bool TryEcho(ref string _txt)
        {
            return base.TryEcho(ref _txt);
        }
    }
}