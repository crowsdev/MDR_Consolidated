using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class AutoDoors : Program
    {
        /*
         * Author: Wanderer
         * Contacts: strderer@gmail.com
         *
         * Current version: 1.0
         *
         * Script timer set up:
         *  - Action 1: Put script in PB, compile and save.
         *  - Action 2: ???
         *  - Action 3: PROFIT!
         *
         * Variable you can alter and their description search in code, it's all commented.
         *
         * Script support custom settings for every door. Put one of this keywords in door's Custom Data (not case sensitive):
         * - DCS_EXCLUDE - script will ignore this door, and will not close this door, and will not form an airlock with this door.
         * - DCS_NOCLOSE - script will not close this door but will form an airlock with this door, if it can.
         * - DCS_CLOSETIME <value> - will set custom time before auto close in seconds.
         *                            Example: DCS_CLOSETIME 10.5 - this door will be auto closed after ten and half seconds.
         *
         */
        DoorsControlSystem DCS;
        StringBuilder status;
        int TickCounter;

        public AutoDoors()
        {
            status = new StringBuilder();
            DCS = new DoorsControlSystem(this);
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            status.Append("\n_DCS initialized...");
            PrintErrWarDebgs(DCS);
            Echo(status.ToString());
        }

        public void Save()
        {
        }

        public void Main(string args, UpdateType updateSource)
        {
            if ((updateSource & (UpdateType.Update10 | UpdateType.Update100)) != 0)
            {
                TickCounter++;
                status.Clear();
                status.Append("DCS running ");
                switch (TickCounter % 4)
                {
                    case 0:
                        status.Append("--");
                        break;
                    case 1:
                        status.Append("\\");
                        break;
                    case 2:
                        status.Append(" | ");
                        break;
                    case 3:
                        status.Append("/");
                        break;
                }

                status.Append("\n_");

                if (DCS.IsBusy)
                    DCS.Update(updateSource);
                else if (TickCounter % 3 == 0)
                    DCS.Update(updateSource);

                PrintErrWarDebgs(DCS);
                status.AppendFormat("\nInstruction used: {0:G}/{1:G}\nLast run time: {2:F3} ms",
                    Runtime.CurrentInstructionCount, Runtime.MaxInstructionCount, Runtime.LastRunTimeMs);
                Echo(status.ToString());
            }
        }

        void PrintErrWarDebgs(DoorsControlSystem myClass)
        {
            if (myClass.Debug.Count > 0)
                status.AppendFormat("\nDebug entries: {0:D}", myClass.Debug.Count.ToString());
            for (int i = 0; i < myClass.Debug.Count; i++)
                status.AppendFormat("\n{0:D}#: {1}", i.ToString(), myClass.Debug[i]);
        }

        //--------------------------------------------------------------------------------------------------- 
        // Core class 
        //--------------------------------------------------------------------------------------------------- 
        public class DoorsControlSystem
        {
            public bool AllowIdle = true; // Allow update100 frequency. Will make calculations less precise. 

            //----------------------------------------------------------------------------------------------- 
            // Public section. You can access this variables from PB's Main, or change initial values here. 
            //----------------------------------------------------------------------------------------------- 
            public double DoorCloseDelaySec = 3; // Dealy befor door auto closed in seconds 
            private List<DoorWithTimer> Doors;

            private Program Parent;
            //----------------------------------------------------------------------------------------------- 
            // End of private section 
            //----------------------------------------------------------------------------------------------- 

            public DoorsControlSystem(Program parentProgram)
            {
                Parent = parentProgram;
                Debug = new List<string>();

                Doors = new List<DoorWithTimer>();
                DetectFunctionalBlocks();
            }

            public bool IsBusy { get; private set; }

            public List<string> Debug { get; } // Used for debug messages. Read only.  

            public void Update(UpdateType updateSource)
            {
                Debug.Clear();
                double dT = 0;
                if ((updateSource & UpdateType.Update1) != 0)
                    dT = 1.0 / 60.0;
                else if ((updateSource & UpdateType.Update10) != 0)
                    dT = 1.0 / 6.0;
                else if ((updateSource & UpdateType.Update100) != 0)
                    dT = 1.0 / 0.6;

                IMyDoor doorA;
                IMyDoor doorB;
                IsBusy = !AllowIdle;
                for (int i = 0; i < Doors.Count; i++)
                {
                    if (CheckForExlude(Doors[i].Door))
                        continue;
                    if (!Doors[i].Processed)
                    {
                        doorA = Doors[i].Door;
                        for (int j = i + 1; j < Doors.Count; j++)
                        {
                            if (CheckForExlude(Doors[j].Door))
                                continue;
                            if (doorA.CustomName.Equals(Doors[j].Door.CustomName))
                            {
                                doorB = Doors[j].Door;
                                if (doorA.Status == doorB.Status)
                                    doorA.Enabled = doorB.Enabled = true;
                                else
                                {
                                    if (doorA.Status == DoorStatus.Opening || doorA.Status == DoorStatus.Open)
                                    {
                                        if (doorB.Status != DoorStatus.Closing)
                                        {
                                            IsBusy = true;
                                            doorB.Enabled = false;
                                        }
                                    }

                                    if (doorB.Status == DoorStatus.Opening || doorB.Status == DoorStatus.Open)
                                    {
                                        if (doorA.Status != DoorStatus.Closing)
                                        {
                                            IsBusy = true;
                                            doorA.Enabled = false;
                                        }
                                    }
                                }

                                Doors[j].Processed = true;
                            }
                        }
                    }
                    else
                    {
                        Doors[i].Processed = false;
                    }

                    if (Doors[i].Door.Status == DoorStatus.Open && CheckForNoClose(Doors[i].Door) == false)
                    {
                        Doors[i].Timer += dT;
                        double timeToClose = DoorCloseDelaySec;
                        TryGetCustomParameter(Doors[i].Door.CustomData, "DCS_CLOSETIME ", ref timeToClose);
                        if (Doors[i].Timer >= timeToClose)
                        {
                            Doors[i].Door.CloseDoor();
                            Doors[i].Timer = 0;
                        }

                        IsBusy = true;
                        ;
                    }
                    else
                        Doors[i].Timer = 0;
                }
            }

            public void DetectFunctionalBlocks()
            {
                Doors.Clear();

                List<IMyDoor> allDoors = new List<IMyDoor>();
                Parent.GridTerminalSystem.GetBlocksOfType(allDoors,
                    block => block.BlockDefinition.TypeIdString != "MyObjectBuilder_AirtightHangarDoor");
                foreach (IMyDoor door in allDoors)
                    Doors.Add(new DoorWithTimer(door));
            }

            //----------------------------------------------------------------------------------------------- 
            //----------------------------------------------------------------------------------------------- 
            //----------------------------------------------------------------------------------------------- 

            private bool CheckForExlude(IMyTerminalBlock block)
            {
                return block.CustomData.IndexOf("DCS_EXCLUDE", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private bool CheckForNoClose(IMyTerminalBlock block)
            {
                return block.CustomData.IndexOf("DCS_NOCLOSE", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            private bool TryGetCustomParameter(string customData, string keyword, ref double value)
            {
                string mask = "0123456789.,";
                bool result = false;
                int indx;
                int length;
                indx = customData.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                if (indx >= 0)
                {
                    length = 0;
                    for (int i = indx + keyword.Length; i < customData.Length; i++, length++)
                        if (mask.IndexOf(customData[i]) < 0)
                            break;
                    result = double.TryParse(customData.Substring(indx + keyword.Length, length), out value);
                }

                return result;
            }
            //----------------------------------------------------------------------------------------------- 
            // End of public section. 
            //----------------------------------------------------------------------------------------------- 

            //----------------------------------------------------------------------------------------------- 
            // Private variables for inner use, DO NOT modify anything here 
            //----------------------------------------------------------------------------------------------- 
            public class DoorWithTimer
            {
                public bool Processed;
                public double Timer;

                public DoorWithTimer(IMyDoor door)
                {
                    Door = door;
                    Timer = 0;
                }

                public IMyDoor Door { get; }
            }
        }
    }
}