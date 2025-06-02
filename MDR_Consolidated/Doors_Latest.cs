using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    internal class Doors_Latest : Untermensch
    {
        private readonly Dictionary<IMyDoor, int> DoorsCurrentlyManaged = new Dictionary<IMyDoor, int>();

// You can add an additional delay (in seconds) between closing the first airlock door and opening the second one (Default: 0).
        private readonly double airlockDelaySeconds = 0;

// The script will detect airlocks within a 2 block radius of a just opened door (like back to back sliding doors).
// Change this value, if your airlocks are wider:
        private readonly int airlockRadius = 2;

// Should hangar doors also be closed and after which time?
        private readonly bool autoCloseHangarDoors = true;
        private readonly double autoCloseHangarDoorsSeconds = 10;


// Isy's Simple Doors
// ===============
// Version: 1.0.8
// Date: 2023-05-24

// =======================================================================================
//                                                                            --- Configuration ---
// =======================================================================================

// --- Door Auto Close ---
// =======================================================================================

// The script will automatically close a door 1 seconds after it's being opened. Change this value here if needed:
        private readonly double autoCloseSeconds = 1;

        private readonly string[] b =
        {
            "/", "-", "\\", "|"
        };

        private DateTime c;
        private int TickTimer;


// --- Simple Airlock ---
// =======================================================================================

// By default, the script will try to find airlocks (two doors close to each other) and manage them. It will close
// the just opened door first, then open the other one and close it again (all depending on autoCloseSeconds).
// If you don't want this functionality, set this main trigger to false:
        private readonly bool manageAirlocks = true;

// If you don't want to auto close specific doors, add the manual door keyword to their names
// Note: blockname changes are only noticed every ~17 seconds, so it takes some time until your door is really excluded!
        private readonly string manualDoorKeyword = "!manual";

// If two nearby doors are accidentally treated as an airlock but are in fact just regular doors, you can add this keyword
// to one or both door's names to disable airlock functionality (autoclose still works).
// Note: blockname changes are only noticed every ~17 seconds, so it takes some time until your door is really excluded!
        private readonly string noAirlockKeyword = "!noAirlock";

// To protect the airlock from being opened too early, the script deactivates the second door until the first one is closed
// To change this behavior, set the following value to false:
        private readonly bool protectAirlock = true;


// =======================================================================================
//                                                                      --- End of Configuration ---
//                                                        Don't change anything beyond this point!
// =======================================================================================


        private readonly List<IMyDoor> AllDoors = new List<IMyDoor>();
        private List<IMyDoor> NonHangarDoors = new List<IMyDoor>();
        private readonly List<IMyDoor> DamagedDoors = new List<IMyDoor>();
        private int LastAllDoorsCount;
        private readonly Dictionary<IMyDoor, DateTime> X = new Dictionary<IMyDoor, DateTime>();
        private readonly Dictionary<IMyDoor, DateTime> Y = new Dictionary<IMyDoor, DateTime>();
        private readonly Dictionary<IMyDoor, IMyDoor> AirlockDoorPairs = new Dictionary<IMyDoor, IMyDoor>();

        public Doors_Latest(MyGridProgram ubermensch, UpdateFrequency frequency = UpdateFrequency.Update100) : base(
            ubermensch, frequency)
        {
        }

        private void Main()
        {
            if (TickTimer == 0)
            {
                CheckIfDoorCountChangedAndInit();
            }

            if (manageAirlocks)
            {
                if (TickTimer == 0)
                {
                    D();
                }

                K();
            }

            Q();
            R();
            c += Ubermensch.Runtime.TimeSinceLastRun;
            if (TickTimer >= 99)
            {
                TickTimer = 0;
            }
            else
            {
                TickTimer++;
            }
        }

        private void CheckIfDoorCountChangedAndInit()
        {
            DamagedDoors.Clear();
            Ubermensch.GridTerminalSystem.GetBlocksOfType(AllDoors, IsDoorValid);
            if (AllDoors.Count != LastAllDoorsCount)
            {
                LastAllDoorsCount = AllDoors.Count;
                X.Clear();
                AirlockDoorPairs.Clear();
                DoorsCurrentlyManaged.Clear();
            }
        }

        private bool IsDoorValid(IMyDoor C)
        {
            if (!C.IsSameConstructAs(Ubermensch.Me))
            {
                return false;
            }

            if (!C.CubeGrid.GetCubeBlock(C.Position).IsFullIntegrity)
            {
                DamagedDoors.Add(C);
                return false;
            }

            if (C.CustomName.Contains(manualDoorKeyword))
            {
                return false;
            }

            if (!autoCloseHangarDoors && C is IMyAirtightHangarDoor)
            {
                return false;
            }

            return true;
        }

        private void D()
        {
            var position = new Vector3();
            float distance = 0;
            var minDistance = float.MaxValue;
            var indexOfClosestDoor = -1;
            NonHangarDoors.Clear();
            AirlockDoorPairs.Clear();
            NonHangarDoors = AllDoors.FindAll(I => !(I is IMyAirtightHangarDoor));
            foreach (var C in NonHangarDoors)
            {
                if (C.CustomName.Contains(noAirlockKeyword))
                {
                    continue;
                }

                position = C.Position;
                minDistance = float.MaxValue;
                indexOfClosestDoor = -1;
                for (var J = 0; J < NonHangarDoors.Count; J++)
                {
                    if (NonHangarDoors[J] == C)
                    {
                        continue;
                    }

                    if (NonHangarDoors[J].CustomName.Contains(noAirlockKeyword))
                    {
                        continue;
                    }

                    distance = Vector3.Distance(position, NonHangarDoors[J].Position);
                    if (distance <= airlockRadius && distance < minDistance)
                    {
                        minDistance = distance;
                        indexOfClosestDoor = J;
                        if (distance == 1)
                        {
                            break;
                        }
                    }
                }

                if (indexOfClosestDoor >= 0)
                {
                    AirlockDoorPairs[C] = NonHangarDoors[indexOfClosestDoor];
                }
            }
        }

        private void K()
        {
            foreach (var L in AirlockDoorPairs)
            {
                var M = L.Key;
                var N = L.Value;
                var O = Y.ContainsKey(M)
                    ? (c - Y[M]).TotalMilliseconds >= airlockDelaySeconds *
                    1000
                    : true;
                var P = DoorsCurrentlyManaged.ContainsKey(M) ? DoorsCurrentlyManaged[M] : 0;
                if (protectAirlock)
                {
                    if ((M.Enabled == false && N.Enabled == false) ||
                        (M.Status != DoorStatus.Closed && N.Status != DoorStatus.Closed))
                    {
                        M.Enabled = true;
                        N.Enabled = true;
                    }
                    else if (M.Status != DoorStatus.Closed || !O || P == 1)
                    {
                        N.Enabled =
                            false;
                    }
                    else
                    {
                        N.Enabled = true;
                    }
                }

                if (DoorsCurrentlyManaged.ContainsKey(N))
                {
                    continue;
                }

                if (M.Status == DoorStatus.Open)
                {
                    DoorsCurrentlyManaged[M] = 1;
                }

                if (DoorsCurrentlyManaged.ContainsKey(M))
                {
                    if (DoorsCurrentlyManaged[M]
                        == 1 && M.Status == DoorStatus.Closed)
                    {
                        Y[M] = c;
                        DoorsCurrentlyManaged[M] = 2;
                        continue;
                    }

                    if (DoorsCurrentlyManaged[M] == 2 && O)
                    {
                        Y.Remove(M);
                        DoorsCurrentlyManaged[M] = 3;
                        N.OpenDoor();
                    }

                    if (DoorsCurrentlyManaged[M] == 3 && N.Status == DoorStatus.Closed)
                    {
                        DoorsCurrentlyManaged.Remove(M);
                    }
                }
            }
        }

        private void Q()
        {
            foreach (var C in AllDoors)
                if (C.Status == DoorStatus.Open)
                {
                    if (!X.ContainsKey(C))
                    {
                        X[
                            C] = C is IMyAdvancedDoor ? c + TimeSpan.FromSeconds(1) : c;
                        continue;
                    }

                    if (C is IMyAirtightHangarDoor)
                    {
                        if ((c - X[C]).TotalMilliseconds >= autoCloseHangarDoorsSeconds * 1000)
                        {
                            C.CloseDoor();
                            X.Remove(C);
                        }
                    }
                    else
                    {
                        if ((c - X[C]).TotalMilliseconds >= autoCloseSeconds *
                            1000)
                        {
                            C.CloseDoor();
                            X.Remove(C);
                        }
                    }
                }
        }

        private void R()
        {
            var A = new StringBuilder("Isy's Simple Doors " + b[TickTimer % 4] +
                                      "\n================\n\n");
            A.Append("Refreshing cached doors in: " + Math.Ceiling((double)(99 - TickTimer) / 6) + "s\n\n");
            A.Append("Managed doors: " + AllDoors.Count +
                     "\n");
            A.Append("Door close seconds: " + autoCloseSeconds + "\n");
            if (autoCloseHangarDoors)
            {
                A.Append("Hangar door close seconds: "
                         + autoCloseHangarDoorsSeconds + "\n");
            }

            if (manageAirlocks)
            {
                A.Append("\n");
                A.Append("Airlocks: " + AirlockDoorPairs.Count / 2 + "\n");
                A.Append(
                    "Airlock delay seconds: " + airlockDelaySeconds + "\n");
                A.Append("Airlock protection: " + (protectAirlock ? "true" : "false"));
                A.Append("\n");
            }

            if (DamagedDoors.Count > 0
               )
            {
                A.Append("\n");
                A.Append("Damaged doors: " + DamagedDoors.Count + "\n");
                foreach (var C in DamagedDoors) A.Append("- " + C.CustomName + "\n");
            }

            Ubermensch.Echo(A.ToString());
        }
    }
}