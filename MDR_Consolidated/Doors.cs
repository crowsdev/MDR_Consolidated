using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRage.Scripting.MemorySafeTypes;
using VRageMath;

namespace IngameScript
{
    public class Doors : Untermensch
    {
        /*
         * 0 = no status.
         * 1 = door has been opened.
         * 2 = door was 1 but has now been closed.
         * 3 = timer finished, door 2 opened.
         */
        private MemorySafeDictionary<IMyDoor, int> DoorsInManagedSequence = new MemorySafeDictionary<IMyDoor, int>();

        // You can add an additional delay (in seconds) between closing the first airlock door and opening the second one (Default: 0).
        private double airlockDelaySeconds = 0;

        // The script will detect airlocks within a 2 block radius of a just opened door (like back to back sliding doors).
        // Change this value, if your airlocks are wider:
        private int airlockRadius = 2;

        // Should hangar doors also be closed and after which time?
        private bool autoCloseHangarDoors = true;

        private double autoCloseHangarDoorsSeconds = 10;

        // The script will automatically close a door 2 seconds after it's being opened. Change this value here if needed:
        private double autoCloseSeconds = 2;

        private string[] b =
        {
            "/", "-", "\\", "|"
        };

        private DateTime timeNow = new DateTime();
        private int TickCounter = 0;


        // --- Simple Airlock ---
        // =======================================================================================

        // By default, the script will try to find airlocks (two doors close to each other) and manage them. It will close
        // the just opened door first, then open the other one and close it again (all depending on autoCloseSeconds).
        // If you don't want this functionality, set this main trigger to false:
        private bool manageAirlocks = true;

        // If you don't want to auto close specific doors, add the manual door keyword to their names
        // Note: blockname changes are only noticed every ~17 seconds, so it takes some time until your door is really excluded!
        private string manualDoorKeyword = "!manual";

        // If two nearby doors are accidentally treated as an airlock but are in fact just regular doors, you can add this keyword
        // to one or both door's names to disable airlock functionality (autoclose still works).
        // Note: blockname changes are only noticed every ~17 seconds, so it takes some time until your door is really excluded!
        private string noAirlockKeyword = "!noAirlock";

        // To protect the airlock from being opened too early, the script deactivates the second door until the first one is closed
        // To change this behavior, set the following value to false:
        private bool protectAirlock = true;

        // =======================================================================================
        //                         --- End of Configuration ---
        //                  Don't change anything beyond this point!
        // =======================================================================================

        private MemorySafeList<IMyDoor> ManagedDoors = new MemorySafeList<IMyDoor>();
        private MemorySafeList<IMyDoor> AllNonHangarDoors = new MemorySafeList<IMyDoor>();
        private MemorySafeList<IMyDoor> DmgDoors = new MemorySafeList<IMyDoor>();
        private int LastManagedDoorsCount = 0;
        private MemorySafeDictionary<IMyDoor, DateTime> ManagedHangarDoors = new MemorySafeDictionary<IMyDoor, DateTime>();
        private MemorySafeDictionary<IMyDoor, DateTime> DoorsOnTimerDelay = new MemorySafeDictionary<IMyDoor, DateTime>();
        private MemorySafeDictionary<IMyDoor, IMyDoor> AirlockPairs = new MemorySafeDictionary<IMyDoor, IMyDoor>();

        public Doors(MyGridProgram _parent, UpdateFrequency _freq) : base(_parent, _freq)
        {
            // ParentScript = _parent;
            // ParentScript.Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public override bool OnMain(string argument, UpdateType updateSource)
        {
            #region Check if this should run according to its own frequency.

            if (!base.OnMain(argument, updateSource))
            {
                return false;
            }

            #endregion

            if (TickCounter == 0)
            { // Seems to count to 100 and then resets to 0.
                CheckForChangedDoorCount();
            }

            if (manageAirlocks)
            {
                if (TickCounter == 0)
                {
                    FindAirlockDoorPairs();
                }

                HandleAirlocks();
            }

            
            HandleHangarDoors();
            timeNow += Ubermensch.Runtime.TimeSinceLastRun;
            if (TickCounter >= 99)
            {
                TickCounter = 0;
            }
            else
            {
                TickCounter++;
            }

            return true;
        }

        private void CheckForChangedDoorCount()
        {
            DmgDoors.Clear();
            Ubermensch.GridTerminalSystem.GetBlocksOfType(ManagedDoors, CheckDoorIsValid);
            if (ManagedDoors.Count != LastManagedDoorsCount)
            {
                LastManagedDoorsCount = ManagedDoors.Count;
                ManagedHangarDoors.Clear();
                AirlockPairs.Clear();
                DoorsInManagedSequence.Clear();
            }
        }

        private bool CheckDoorIsValid(IMyDoor _door)
        {
            // if door from different grid, discard.
            if (!_door.IsSameConstructAs(Ubermensch.Me))
            {
                return false;
            }

            if (!_door.CubeGrid.GetCubeBlock(_door.Position).IsFullIntegrity)
            {
                DmgDoors.Add(_door);
                return false;
            }

            if (_door.CustomName.Contains(manualDoorKeyword))
            {
                return false;
            }

            if (!autoCloseHangarDoors && _door is IMyAirtightHangarDoor)
            {
                return false;
            }

            return true;
        }

        private void FindAirlockDoorPairs()
        { // Called once every 100 ticks.
            var myDoorPosition = new Vector3();
            float distance = 0;
            var minDistance = float.MaxValue;
            var closestDoorIndex = -1;
            AllNonHangarDoors.Clear();
            AirlockPairs.Clear();
            AllNonHangarDoors = ManagedDoors.FindAll(I => !(I is IMyAirtightHangarDoor));
            foreach (var myDoor in AllNonHangarDoors)
            {
                if (myDoor.CustomName.Contains(noAirlockKeyword))
                {
                    continue;
                }

                myDoorPosition = myDoor.Position;
                minDistance = float.MaxValue;
                closestDoorIndex = -1;
                for (var J = 0; J < AllNonHangarDoors.Count; J++)
                {
                    // Finding door pairs to manage as airlocks. 
                    if (AllNonHangarDoors[J] == myDoor)
                    {
                        continue;
                    }

                    if (AllNonHangarDoors[J].CustomName.Contains(noAirlockKeyword))
                    {
                        continue;
                    }

                    distance = Vector3.Distance(myDoorPosition, AllNonHangarDoors[J].Position);
                    if (distance <= airlockRadius && distance < minDistance)
                    { // Check that distance between door pair is within airlockRadius setting and if its closest door checked so far.
                        minDistance = distance;
                        closestDoorIndex = J;
                        if (distance == 1)
                        {
                            break;
                        }
                    }
                }

                // Check if a valid airlock door pair was found at all.
                if (closestDoorIndex >= 0)
                {
                    AirlockPairs[myDoor] = AllNonHangarDoors[closestDoorIndex];
                }
            }
        }

        private void HandleAirlocks()
        {
            foreach (var L in AirlockPairs)
            {
                var alDoor0 = L.Key;
                var alDoor1 = L.Value;
                // Check if enough time has passed since last time this pair was managed, if not then 
                var delayTimerElapsed = DoorsOnTimerDelay.ContainsKey(alDoor0)
                    ? (timeNow - DoorsOnTimerDelay[alDoor0]).TotalMilliseconds >= airlockDelaySeconds *
                    1000
                    : true;
                var managerSequenceState = DoorsInManagedSequence.ContainsKey(alDoor0) ? DoorsInManagedSequence[alDoor0] : 0;
                if (protectAirlock)
                {
                    if ((alDoor0.Enabled == false && alDoor1.Enabled == false) || (alDoor0.Status != DoorStatus.Closed && alDoor1.Status != DoorStatus.Closed))
                    {
                        alDoor0.Enabled = true;
                        alDoor1.Enabled = true;
                    }
                    else if (alDoor0.Status != DoorStatus.Closed || !delayTimerElapsed || managerSequenceState == 1)
                    {
                        /*
                         * if first airlock door is not FULLY closed, or if delay timer still active, or if door has been
                         * opened:
                         *      Disable the last door of airlock pair.
                         */
                        alDoor1.Enabled = false;
                    }
                    else
                    {
                        alDoor1.Enabled = true;
                    }
                }

                if (DoorsInManagedSequence.ContainsKey(alDoor1))
                {
                    continue;
                }

                if (alDoor0.Status == DoorStatus.Open)
                {
                    DoorsInManagedSequence[alDoor0] = 1;
                }

                if (DoorsInManagedSequence.ContainsKey(alDoor0))
                {
                    if (DoorsInManagedSequence[alDoor0] == 1 && alDoor0.Status == DoorStatus.Closed)
                    {
                        DoorsOnTimerDelay[alDoor0] = timeNow;
                        DoorsInManagedSequence[alDoor0] = 2;
                        continue;
                    }

                    if (DoorsInManagedSequence[alDoor0] == 2 && delayTimerElapsed)
                    {
                        // First door is now closed, delay timer finished, open final airlock door.
                        DoorsOnTimerDelay.Remove(alDoor0);
                        DoorsInManagedSequence[alDoor0] = 3;
                        alDoor1.OpenDoor();
                    }

                    if (DoorsInManagedSequence[alDoor0] == 3 && alDoor1.Status == DoorStatus.Closed)
                    {
                        DoorsInManagedSequence.Remove(alDoor0);
                    }
                }
            }
        }

        private void HandleHangarDoors()
        {
            foreach (var myDoor in ManagedDoors)
            {
                if (myDoor.Status == DoorStatus.Open)
                {
                    if (!ManagedHangarDoors.ContainsKey(myDoor))
                    {
                        ManagedHangarDoors[myDoor] = myDoor is IMyAdvancedDoor ? timeNow + TimeSpan.FromSeconds(1) : timeNow;
                        continue;
                    }

                    if (myDoor is IMyAirtightHangarDoor)
                    {
                        if ((timeNow - ManagedHangarDoors[myDoor]).TotalMilliseconds >= autoCloseHangarDoorsSeconds * 1000)
                        {
                            myDoor.CloseDoor();
                            ManagedHangarDoors.Remove(myDoor);
                        }
                    }
                    else
                    {
                        if ((timeNow - ManagedHangarDoors[myDoor]).TotalMilliseconds >= autoCloseSeconds *
                            1000)
                        {
                            myDoor.CloseDoor();
                            ManagedHangarDoors.Remove(myDoor);
                        }
                    }
                }
            }
        }

        public override bool TryEcho(ref string _txt)
        {
            var echoLines = new StringBuilder("Isy's Simple Doors " + b[TickCounter % 4] +
                                      "\n================\n\n");
            echoLines.Append("Refreshing cached doors in: " + Math.Ceiling((double)(99 - TickCounter) / 6) + "s\n\n");
            echoLines.Append("Managed doors: " + ManagedDoors.Count +
                     "\n");
            echoLines.Append("Door close seconds: " + autoCloseSeconds + "\n");
            if (autoCloseHangarDoors)
            {
                echoLines.Append("Hangar door close seconds: "
                         + autoCloseHangarDoorsSeconds + "\n");
            }

            if (manageAirlocks)
            {
                echoLines.Append("\n");
                echoLines.Append("Airlocks: " + AirlockPairs.Count / 2 + "\n");
                echoLines.Append(
                    "Airlock delay seconds: " + airlockDelaySeconds + "\n");
                echoLines.Append("Airlock protection: " + (protectAirlock ? "true" : "false"));
                echoLines.Append("\n");
            }

            if (DmgDoors.Count > 0)
            {
                echoLines.Append("\n");
                echoLines.Append("Damaged doors: " + DmgDoors.Count + "\n");
                foreach (var C in DmgDoors)
                {
                    echoLines.Append("- " + C.CustomName + "\n");
                }
            }

            _txt = echoLines.ToString();
            return true;
        }
    }
}