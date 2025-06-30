using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class AutodoorAndAirlockManager : Program
    {
        public double
            actionLimiterMultiplier = 0.0075, runTimeLimiter = 1.5,
            occupyDelay = 3, oxygenFillLevel = 0.99,
            oxygenPercentage = 0, intakeCutOffLevel = 0.9, 
            largeBlockDistance = 1, smallBlockPlayerLead = 1,
            largeBlockPlayerLead = 1, smallBlockDistance = 1,
            hydrogenPercentage = 0;

        public int
            sizeLimit = 250, outputLimit = 10,
            thoughtStatus = 0, thoughtIndex = 0,
            progressBarLength = 40, oxygenTankCount = 0,
            updateFrequency = 1, generatorCount,
            hydrogenTankCount = 0;

        public string
            nLine = Environment.NewLine,
            autodoorKeyword = "Autodoor",
            outputPanelName = "Airlock Panel",
            innerAirlockPanel = "Airlock Status",
            soundBlockKeyword = "Airlock",
            thoughtLine = "---------------------------------------",
            slidingDoorDef = "MyObjectBuilder_AirtightSlideDoor/LargeBlockSlideDoor";

        public List<IMyTerminalBlock>
            lockDoorList = new List<IMyTerminalBlock>(),
            allDoorList = new List<IMyTerminalBlock>(),
            ventList = new List<IMyTerminalBlock>(),
            lcdList = new List<IMyTerminalBlock>(),
            soundBlockList = new List<IMyTerminalBlock>(),
            hydrogenTankList = new List<IMyTerminalBlock>(),
            oxygenTankList = new List<IMyTerminalBlock>();

        public List<string> outputList = new List<string>();

        public IMyCubeGrid Grid;

        public DateTime
            tickStartTime = DateTime.Now,
            controlGeneratorTime = DateTime.Now,
            renewTankTime = DateTime.Now;

        public List<Airlock> airlockList = new List<Airlock>();

        public List<Autodoor> autoDoorList = new List<Autodoor>();

        public Random rnd = new Random();

        public bool
            initScanned = false, initScanA = false, initScanB = false,
            initScanC = false, initClose = false, airlocksChecked = false,
            rescanAutoDoors = false, autoUseLCDs = true,
            autoUseSoundBlocks = true, controlIntake = true,
            useRangeOnly = false, inAtmosphere = false,
            controlGenerators = true;

        public AutodoorAndAirlockManager()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            LoadData();
            Grid = Me.CubeGrid;
        }

        public void Save() {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            tickStartTime = DateTime.Now;
            if (argument != "") try { Commands(argument); } catch { Output("Error caught running: " + argument); }
            try { ControlIntake(); } catch { Output("Error caught controlling intakes"); }
            try { ControlGenerators(); } catch { Output("Error caught controlling generators"); }
            if (!initScanned || rescanAutoDoors) try { InitScan(); } catch { Output("Error caught initializing scan"); }
            else {
                if (lockDoorList.Count > 0 || allDoorList.Count > 0) try { AssignDoors(); } catch { Output("Error caught assigning doors to airlocks"); }
                else if (!airlocksChecked) try { CheckAirlocks(); } catch { Output("Error caught checking airlocks"); }
                else if (scanIndex < airlockList.Count) try { ScanSpace(); } catch { Output("Error caught scanning airlock area"); }
                else try { SensorScan(); } catch { Output("Error caught checking sensors"); }
            }
            PaintOutput();
            Save();
        }

        public void Commands(string argument)
        {
            Output("Running Command: " + argument);
            string arg = argument.ToLower().Replace(" ", "");
            if (arg == "load") LoadData();
            else if (arg == "save") SaveData();
            else if (arg == "scan") {
                ResetScan();
                airlockList.Clear();
                autoDoorList.Clear();
                lockDoorList.Clear();
                allDoorList.Clear();
                autoAirlocks = false;
                autoAirlockCount = 0;
                autoAirlockIndex = 0;
                ventList.Clear();
                lcdList.Clear();
                soundBlockList.Clear();
                potentialSpots.Clear();
                initScanned = false;
                initScanA = false;
                initScanB = false;
                initScanC = false;
                initClose = false;
                firstScan = true;
                scanIndex = 0;
                ventIndex = 0;
            } else if (arg == "autodoor" || (arg.Contains("scan") && arg.Contains("door")))
                rescanAutoDoors = true;
        }

        private void ResetRuntimes()
        {
            Runtime.UpdateFrequency = UpdateFrequency.None;
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update1;
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update10;
            Runtime.UpdateFrequency &= ~UpdateFrequency.Update100;
            if (updateFrequency == 100)
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            else if (updateFrequency == 1)
                Runtime.UpdateFrequency = UpdateFrequency.Update1;
            else
                Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        public void ControlIntake()
        {
            if (DateTime.Now >= findOxygenTime) {
                findOxygenTime = DateTime.Now.AddSeconds(rnd.Next(3, 5));
                FindOxygenLevels();
                if (controlIntake) {
                    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                    GridTerminalSystem.GetBlocksOfType<IMyAirVent>(blocks, (p => p.CustomName.ToLower().Contains("intake")));
                    if (blocks.Count > 0) {
                        if (((IMyAirVent)blocks[0]).GetOxygenLevel() > 0.0) inAtmosphere = true;
                        else inAtmosphere = false;
                    } else inAtmosphere = false;
                    if (oxygenPercentage >= intakeCutOffLevel)
                        ApplyActions<IMyAirVent>("intake", "OnOff_Off");
                    else ApplyActions<IMyAirVent>("intake", "OnOff_On");
                }
            }
        }

        public void ControlGenerators()
        {
            if (DateTime.Now >= controlGeneratorTime && controlGenerators) {
                controlGeneratorTime = DateTime.Now.AddSeconds(rnd.Next(3, 5));
                string tempA = "OnOff_On";
                if (oxygenPercentage >= intakeCutOffLevel && hydrogenPercentage >= intakeCutOffLevel) tempA = "OnOff_Off";
                ApplyActions<IMyGasGenerator>("", tempA);
            }
        }

        private void LoadData()
        {
            Output("Loading data");
            if (Me.CustomData != "" && Storage != Me.CustomData) Storage = Me.CustomData;
            if (Storage != "") {
                try {
                    if (Me.CustomData == "") Me.CustomData = Storage;
                    string setting = Storage;
                    setting = setting.Replace("\r\n", String.Empty);
                    setting = setting.Replace("\n", String.Empty);
                    setting = setting.Replace("\r", String.Empty);
                    setting = setting.Replace("\t", String.Empty);
                    string[] settingArray = setting.Split(';');
                    int settingsCount = 0;
                    for (int i = 0; i < settingArray.Length; i++) {
                        if (!string.IsNullOrEmpty(settingArray[i]) && settingArray[i] != "" && settingArray[i].Contains("|")) {
                            settingsCount++;
                            ProcessSetting(settingArray[i]);
                        }
                    }
                    if (settingsCount < 21) SaveData();
                    else Output("Settings: " + settingsCount.ToString());
                }
                catch { Output("Error loading data"); } } else SaveData();
        }

        private void SaveData()
        {
            try {
                Output("Saving data");
                if (Me.CustomData == Storage || Me.CustomData == "") {
                    string saveData = "outputPanelName|" + outputPanelName + ";";
                    saveData += nLine + "innerAirlockPanel|" + innerAirlockPanel + ";";
                    saveData += nLine + "soundBlockKeyword|" + soundBlockKeyword + ";";
                    saveData += nLine + "autodoorKeyword|" + autodoorKeyword + ";";
                    saveData += nLine + "thoughtLine|" + thoughtLine + ";";
                    saveData += nLine + "sizeLimit|" + sizeLimit.ToString() + ";";
                    saveData += nLine + "outputLimit|" + outputLimit.ToString() + ";";
                    saveData += nLine + "progressBarLength|" + progressBarLength.ToString() + ";";
                    saveData += nLine + "oxygenFillLevel|" + oxygenFillLevel.ToString("N4") + ";";
                    saveData += nLine + "actionLimiterMultiplier|" + actionLimiterMultiplier.ToString("N4") + ";";
                    saveData += nLine + "runTimeLimiter|" + runTimeLimiter.ToString("N4") + ";";
                    saveData += nLine + "occupyDelay|" + occupyDelay.ToString("N4") + ";";
                    saveData += nLine + "smallBlockDistance|" + smallBlockDistance.ToString("N2") + ";";
                    saveData += nLine + "largeBlockDistance|" + largeBlockDistance.ToString("N2") + ";";
                    saveData += nLine + "smallBlockPlayerLead|" + smallBlockPlayerLead.ToString("N2") + ";";
                    saveData += nLine + "largeBlockPlayerLead|" + largeBlockPlayerLead.ToString("N2") + ";";
                    saveData += nLine + "autoUseLCDs|" + autoUseLCDs.ToString() + ";";
                    saveData += nLine + "useRangeOnly|" + useRangeOnly.ToString() + ";";
                    saveData += nLine + "controlIntake|" + controlIntake.ToString() + ";";
                    saveData += nLine + "controlGenerators|" + controlGenerators.ToString() + ";";
                    saveData += nLine + "autoUseSoundBlocks|" + autoUseSoundBlocks.ToString() + ";";
                    Me.CustomData = saveData;
                    Storage = saveData;
                    Output("Saved data");
                } else {
                    Storage = Me.CustomData;
                    Output("User Settings Moved From Custom Data To Storage" + nLine +
                           "Use Load Or Recompile To Load Settings Into Script");
                } } catch { Output("Error Saving Data"); }
        }

        private void ProcessSetting(string settingText)
        {
            try {
                int setIndex = settingText.IndexOf("|"), lH = settingText.Length;
                setIndex++;
                bool settingBool = settingText.ToLower().Contains("true");
                double settingDouble = -123.321;
                string settingString = settingText.Substring((settingText.IndexOf("|") + 1), settingText.Length - (settingText.IndexOf("|") + 1));
                try {
                    settingDouble = double.Parse(settingText.Substring(setIndex, lH - setIndex));
                } catch { }
                try {
                    if (settingText.StartsWith("outputPanelName|")) outputPanelName = settingString;
                    else if (settingText.StartsWith("autodoorKeyword|")) autodoorKeyword = settingString;
                    else if (settingText.StartsWith("innerAirlockPanel|")) innerAirlockPanel = settingString;
                    else if (settingText.StartsWith("soundBlockKeyword|")) soundBlockKeyword = settingString;
                    else if (settingText.StartsWith("thoughtLine|")) thoughtLine = settingString;
                    else if (settingText.StartsWith("actionLimiterMultiplier|")) actionLimiterMultiplier = settingDouble;
                    else if (settingText.StartsWith("runTimeLimiter|")) runTimeLimiter = settingDouble;
                    else if (settingText.StartsWith("occupyDelay|")) occupyDelay = settingDouble;
                    else if (settingText.StartsWith("oxygenFillLevel|")) oxygenFillLevel = settingDouble;
                    else if (settingText.StartsWith("autoUseLCDs|")) autoUseLCDs = settingBool;
                    else if (settingText.StartsWith("controlGenerators|")) controlGenerators = settingBool;
                    else if (settingText.StartsWith("useRangeOnly|")) useRangeOnly = settingBool;
                    else if (settingText.StartsWith("controlIntake|")) controlIntake = settingBool;
                    else if (settingText.StartsWith("autoUseSoundBlocks|")) autoUseSoundBlocks = settingBool;
                    else if (settingText.StartsWith("progressBarLength|")) progressBarLength = (int)settingDouble;
                    else if (settingText.StartsWith("smallBlockDistance|")) smallBlockDistance = settingDouble;
                    else if (settingText.StartsWith("largeBlockDistance|")) largeBlockDistance = settingDouble;
                    else if (settingText.StartsWith("smallBlockPlayerLead|")) smallBlockPlayerLead = settingDouble;
                    else if (settingText.StartsWith("largeBlockPlayerLead|")) largeBlockPlayerLead = settingDouble;
                    else if (settingText.StartsWith("sizeLimit|")) sizeLimit = (int)settingDouble;
                    else if (settingText.StartsWith("outputLimit|")) outputLimit = (int)settingDouble;
                    Output("Processed Setting: " + settingText);
                }
                catch { Output("Error Processing Setting: " + settingText); } }
            catch{ Output("Error Processing Setting: " + settingText); }
        }

        private int airlockCheckIndex = 0;
        public void CheckAirlocks()
        {
            for (int i = airlockCheckIndex; i < airlockList.Count; i += 0)
            {
                if (airlockList[i].innerDoors.Count == 0 || airlockList[i].outerDoors.Count == 0) {
                    if (airlockList[i].innerDoors.Count == 0)
                        Output("Airlock: " + airlockList[i].airlockId + " has no inner doors");
                    else Output("Airlock: " + airlockList[i].airlockId + " has no outer doors");
                    airlockList.RemoveAt(i);
                }
                else {
                    i++;
                    airlockCheckIndex++;
                }
                if (!AvailableActions()) break;
            }
            if (airlockCheckIndex >= airlockList.Count) {
                airlockCheckIndex = 0;
                airlocksChecked = true;
            }
        }

        public DateTime findOxygenTime = DateTime.Now;
        public void FindOxygenLevels()
        {
            if (DateTime.Now >= renewTankTime) {
                renewTankTime = DateTime.Now.AddSeconds(15);
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                hydrogenTankList.Clear();
                oxygenTankList.Clear();
                GridTerminalSystem.GetBlocksOfType<IMyGasTank>(blocks);
                for (int i = 0; i < blocks.Count; i++) {
                    if (blocks[i].BlockDefinition.ToString().ToLower().Contains("hydrogen"))
                        hydrogenTankList.Add(blocks[i]);
                    else oxygenTankList.Add(blocks[i]);
                }
                oxygenTankCount = oxygenTankList.Count;
                hydrogenTankCount = hydrogenTankList.Count;
            }
            double oxygenLevel = 0.0;
            for (int i = 0; i < oxygenTankList.Count; i++)
                oxygenLevel += ((IMyGasTank)oxygenTankList[i]).FilledRatio;
            oxygenPercentage = oxygenLevel / (double)oxygenTankList.Count;
            oxygenLevel = 0.0;
            for (int i = 0; i < hydrogenTankList.Count; i++)
                oxygenLevel += ((IMyGasTank)hydrogenTankList[i]).FilledRatio;
            hydrogenPercentage = oxygenLevel / (double)hydrogenTankList.Count;
        }

        public void InitScan()
        {
            if (updateFrequency != 1) {
                updateFrequency = 1;
                ResetRuntimes();
            }
            if (!initScanA || rescanAutoDoors) {
                rescanAutoDoors = false;
                initScanA = true;
                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMyDoor>(blocks, (p => p.CustomName.ToLower().Contains(autodoorKeyword.ToLower())));
                for (int i = 0; i < blocks.Count; i++) {
                    Autodoor door = new Autodoor();
                    if (!blocks[i].CustomName.Contains("-")) blocks[i].CustomName = blocks[i].CustomName + " -6.5";
                    door.doorBlock = blocks[i];
                    autoDoorList.Add(door);
                }
            } else if (!initScanB) {
                initScanB = true;
                GridTerminalSystem.GetBlocksOfType<IMyDoor>
                (lockDoorList, (p => p.CustomName.Contains("-") && p.CustomName.Contains(":") &&
                                     p.CustomName.Contains("[") && p.CustomName.Contains("]")));
                GridTerminalSystem.GetBlocksOfType<IMyDoor>(allDoorList);
            } else if (!initScanC) {
                initScanC = true;
                GridTerminalSystem.GetBlocksOfType<IMyAirVent>(ventList);
                GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(lcdList, (p => !p.CustomName.ToLower().Contains(outputPanelName.ToLower())));
                GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(soundBlockList);
            } else if (!initClose) {
                initClose = true;
                initScanned = true;
                for (int i = 0; i < allDoorList.Count; i++)
                    allDoorList[i].ApplyAction("Open_Off");
            }
        }

        public bool AddAuto(int index)
        {
            IMyTerminalBlock block = allDoorList[index];
            Matrix matrix = new Matrix();
            block.Orientation.GetMatrix(out matrix);
            Vector3I match = AddVectors(block.Position, (Vector3I)matrix.Backward);
            if (Grid.CubeExists(match)) {
                IMySlimBlock matchSlimBlock = Grid.GetCubeBlock(match);
                if (matchSlimBlock != null) {
                    IMyTerminalBlock matchBlock = (IMyTerminalBlock)matchSlimBlock.FatBlock;
                    if (matchBlock.BlockDefinition.ToString() == slidingDoorDef) {
                        Matrix bMatrix = new Matrix();
                        matchBlock.Orientation.GetMatrix(out bMatrix);
                        if (AddVectors(matchBlock.Position, (Vector3I)bMatrix.Backward) == block.Position) {
                            if (!block.CustomName.Contains("-")) block.CustomName = block.CustomName + " -6.5";
                            if (!matchBlock.CustomName.Contains("-")) matchBlock.CustomName = matchBlock.CustomName + " -6.5";
                            autoAirlockCount++;
                            Airlock airlock = new Airlock();
                            airlock.parentProgram = this;
                            airlock.Me = Me;
                            airlock.airlockId = "Auto Airlock " + autoAirlockCount.ToString();
                            airlock.innerDoors.Add(block);
                            airlock.outerDoors.Add(matchBlock);
                            airlock.autoMade = true;
                            airlockList.Add(airlock);
                            allDoorList.RemoveAt(index);
                            for (int i = 0; i < allDoorList.Count; i++)
                            {
                                if (allDoorList[i].Position == matchBlock.Position) {
                                    allDoorList.RemoveAt(i);
                                    break;
                                }
                            }
                            for (int i = lockDoorList.Count - 1; i >= 0; i--)
                            {
                                if (lockDoorList[i].Position == matchBlock.Position || lockDoorList[i].Position == block.Position)
                                    lockDoorList.RemoveAt(i);
                            }
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public Vector3D AddVectors(Vector3D A, Vector3D B)
        {
            return new Vector3D(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
        }

        public bool autoAirlocks = false;
        public int autoAirlockCount = 0, autoAirlockIndex = 0;
        public void AssignDoors()
        {
            if (!autoAirlocks) {
                while (autoAirlockIndex < allDoorList.Count) {
                    if (AddAuto(autoAirlockIndex)) autoAirlockIndex++;
                    if (!AvailableActions()) break;
                }
                if (autoAirlockIndex >= allDoorList.Count) {
                    allDoorList.Clear();
                    autoAirlockIndex = 0;
                    autoAirlocks = true;
                }
            } else {
                while (lockDoorList.Count > 0) {
                    AddDoor(lockDoorList[0]);
                    lockDoorList.RemoveAt(0);
                    if (!AvailableActions()) break;
                }
            }
        }

        public void AddDoor(IMyTerminalBlock block)
        {
            bool placed = false, inner = InnerDoor(block);
            string airlockId = GetDoorID(block);
            for (int i = 0; i < airlockList.Count; i++) {
                try {
                    if (airlockList[i].airlockId == airlockId) {
                        placed = true;
                        if (inner) airlockList[i].innerDoors.Add(block);
                        else airlockList[i].outerDoors.Add(block);
                        break;
                    } } catch { Output("Error comparing door to airlock"); }
            }
            if (!placed) {
                Airlock airlock = new Airlock();
                airlock.parentProgram = this;
                airlock.Me = Me;
                airlock.airlockId = airlockId;
                if (inner) airlock.innerDoors.Add(block);
                else airlock.outerDoors.Add(block);
                airlockList.Add(airlock);
            }
        }

        public void VentScan()
        {
            for (int i = ventIndex; i < airlockList[scanIndex].airlockArea.Count; i++) {
                ventIndex++;
                CheckForNearbyBlock(airlockList[scanIndex].airlockArea[i]);
                if (!AvailableActions()) break;
            }
        }

        public void ApplyActions<T>(string Name, string Action) where T : class
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<T>(blocks, (p => p.CustomName.ToLower().Contains(Name.ToLower())));
            for (int i = 0; i < blocks.Count; i++)
                blocks[i].ApplyAction(Action);
        }

        public bool firstScan = true;
        public int scanIndex = 0, ventIndex = 0;
        public List<Vector3I> scanNearby = new List<Vector3I>();
        public List<Vector3I> scannedNearby = new List<Vector3I>();
        public List<Vector3I> scanned = new List<Vector3I>();
        public void ScanSpace()
        {
            if (scanIndex < airlockList.Count) {
                if (!airlockList[scanIndex].autoMade) {
                    if (firstScan) {
                        firstScan = false;
                        for (int i = 0; i < airlockList[scanIndex].outerDoors.Count; i++)
                            InitialEmptySpot(airlockList[scanIndex].outerDoors[i].Position);
                        if (potentialSpots.Count > 0) TryNextArea();
                        Output("Scanning space for: " + airlockList[scanIndex].airlockId);
                    }
                    while (scanNearby.Count > 0 && airlockList[scanIndex].airlockArea.Count < sizeLimit) {
                        try {
                            ScanNearby(scanNearby[0]);
                            scanNearby.RemoveAt(0);
                        } catch { Output("Error caught checking point for airlock area"); }
                        if (!AvailableActions()) break;
                    }
                    if (airlockList[scanIndex].airlockArea.Count >= sizeLimit) {
                        ResetScan();
                        airlockList[scanIndex].airlockArea.Clear();
                        ventIndex = 0;
                        if (potentialSpots.Count > 0) TryNextArea();
                        else {
                            firstScan = true;
                            scanIndex++;
                            Output("No space found");
                        }
                    }
                    else if (scanNearby.Count == 0) {
                        VentScan();
                        if (ventIndex >= airlockList[scanIndex].airlockArea.Count) {
                            ventIndex = 0;
                            firstScan = true;
                            ResetScan();
                            Output(airlockList[scanIndex].airlockId + ": Vents: " + airlockList[scanIndex].vents.Count.ToString());
                            Output(airlockList[scanIndex].airlockId + ": Spots: " + airlockList[scanIndex].airlockArea.Count.ToString());
                            Output(airlockList[scanIndex].airlockId + ": LCDs: " + airlockList[scanIndex].lcds.Count.ToString());
                            Output(airlockList[scanIndex].airlockId + ": Sound Blocks: " + airlockList[scanIndex].soundBlocks.Count.ToString());
                            scanIndex++;
                            potentialSpots.Clear();
                        }
                    }
                } else scanIndex++;
            }
        }

        public void CheckAutoDoorStatus(SensorEntity info)
        {
            double dist = 0.0;
            for (int i = 0; i < autoDoorList.Count; i++) {
                dist = GetDoorDistance(autoDoorList[i].doorBlock);
                if (VDistance(info.Position, autoDoorList[i].doorBlock.Position) <= dist) {
                    autoDoorList[i].activeCycle = true;
                }
            }
        }

        public double GetDoorDistance(IMyTerminalBlock block)
        {
            try {
                string name = block.CustomName;
                int tempA = name.IndexOf("-") + 1;
                name = name.Substring(tempA, (name.Length - tempA));
                if (name.Contains(" "))
                    tempA = name.IndexOf(" ");
                else if (name.Contains("["))
                    tempA = name.IndexOf("[");
                else tempA = name.Length;
                return double.Parse(name.Substring(0, tempA));
            } catch { Output("Error parsing door distance"); }
            return 0.0;
        }

        public int sensorIndex = 0, entityIndex = 0, airlockIndex = 0;
        public List<SensorEntity> entityList = new List<SensorEntity>();
        public bool detectedAnything = false;
        public void SensorScan()
        {
            if (entityList.Count == 0) {
                if (updateFrequency != 10) {
                    updateFrequency = 10;
                    ResetRuntimes();
                }
                soundBlockList.Clear();
                lockDoorList.Clear();
                ventList.Clear();
                lcdList.Clear();
                soundBlockList.Clear();
                if (!detectedAnything) {
                    ApplyActions<IMyDoor>(autodoorKeyword, "Open_Off");
                    ApplyActions<IMyDoor>(":inner", "Open_Off");
                    ApplyActions<IMyDoor>(":outer", "Open_Off");
                }
                detectedAnything = false;
                List<IMyTerminalBlock> sensorList = new List<IMyTerminalBlock>();
                GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensorList);
                List<MyDetectedEntityInfo> list = new List<MyDetectedEntityInfo>();
                while (sensorList.Count > 0) {
                    ((IMySensorBlock)sensorList[0]).DetectedEntities(list);
                    sensorList.RemoveAt(0);
                    while (list.Count > 0) {
                        if (list[0].Type != MyDetectedEntityType.None &&
                            (list[0].Relationship == MyRelationsBetweenPlayerAndBlock.Owner ||
                             list[0].Relationship == MyRelationsBetweenPlayerAndBlock.FactionShare)) {
                            SensorEntity entity = new SensorEntity();
                            entity.Position = Grid.WorldToGridInteger(list[0].Position);
                            entity.EntityId = list[0].EntityId;
                            entity.Orientation = list[0].Orientation;
                            entityList.Add(entity);
                        }
                        list.RemoveAt(0);
                    }
                }
            }
            if (!detectedAnything && entityList.Count > 0) detectedAnything = true;
            while (entityList.Count > 0) {
                for (int i = airlockIndex; i < airlockList.Count; i++) {
                    airlockList[i].CheckStatus(entityList[0]);
                    airlockIndex++;
                    if (!AvailableActions()) break;
                }
                if (airlockIndex >= airlockList.Count) {
                    airlockIndex = 0;
                    CheckAutoDoorStatus(entityList[0]);
                    entityList.RemoveAt(0);
                }
                if (!AvailableActions()) break;
            }
            if (entityList.Count == 0) {
                FinishDoorCycle();
                FinishAirlockCycle();
            }
        }

        public void FinishDoorCycle()
        {
            for (int i = 0; i < autoDoorList.Count; i++) {
                if (autoDoorList[i].activeCycle) {
                    if (((IMyDoor)autoDoorList[i].doorBlock).Status != DoorStatus.Opening && ((IMyDoor)autoDoorList[i].doorBlock).Status != DoorStatus.Open)
                        ((IMyDoor)autoDoorList[i].doorBlock).OpenDoor();
                } else {
                    if (((IMyDoor)autoDoorList[i].doorBlock).Status != DoorStatus.Closing && ((IMyDoor)autoDoorList[i].doorBlock).Status != DoorStatus.Closed)
                        ((IMyDoor)autoDoorList[i].doorBlock).CloseDoor();
                }
                autoDoorList[i].activeCycle = false;
            }
        }

        public void FinishAirlockCycle()
        {
            for (int i = 0; i < airlockList.Count; i++) {
                airlockList[i].currentlyActive = airlockList[i].activeCycle;
                airlockList[i].activeCycle = false;
                airlockList[i].facingInner = airlockList[i].innerCycle;
                airlockList[i].innerCycle = false;
                if (!airlockList[i].currentlyOccupied && airlockList[i].occupiedCycle)
                    airlockList[i].occupyTime = DateTime.Now.AddSeconds(occupyDelay);
                airlockList[i].currentlyOccupied = airlockList[i].occupiedCycle;
                airlockList[i].occupiedCycle = false;
                airlockList[i].Control();
            }
        }

        public string GetDoorID(IMyTerminalBlock block)
        {
            string name = block.CustomName;
            int tempA = name.IndexOf("[") + 1;
            name = name.Substring(tempA, (name.Length - tempA));
            tempA = name.IndexOf(":");
            return name.Substring(0, tempA);
        }

        public bool InnerDoor(IMyTerminalBlock block)
        {
            string name = block.CustomName.ToLower();
            int tempA = name.IndexOf(":") + 1;
            name = name.Substring(tempA, (name.Length - tempA));
            tempA = name.IndexOf("]");
            name = name.Substring(0, tempA);
            if (name == "inner") return true;
            return false;
        }

        public string Status()
        {
            string tempA = "Autodoor Count: " + autoDoorList.Count.ToString(), tempB = thoughtLine, tempC = "";

            if (thoughtStatus == 0) tempC = @"\";
            else if (thoughtStatus == 1) tempC = "|";
            else if (thoughtStatus == 2) tempC = "/";
            thoughtStatus++;
            if (thoughtStatus > 2) {
                thoughtStatus = 0;
                thoughtIndex++;
                if ((thoughtIndex + 1) > thoughtLine.Length) thoughtIndex = 0; }
            if (thoughtIndex == 0) tempB = tempC + tempB;
            else if (thoughtIndex == tempB.Length) tempB += tempC;
            else tempB = tempB.Substring(0, thoughtIndex) + tempC + tempB.Substring(thoughtIndex + 1, (tempB.Length - thoughtIndex - 1));

            if (scanIndex < airlockList.Count) tempA += "; Initializing";
            else tempA += "; Active";
            tempA += nLine + "Airlock Count: " + airlockList.Count.ToString();
            tempA += nLine + "Airlocks " + tempB;
            for (int i = 0; i < airlockList.Count; i++) {
                if (tempA != "") tempA += nLine;
                tempA += "-" + airlockList[i].airlockId;
                if (!useRangeOnly) {
                    if (airlockList[i].currentlyOccupied)
                        tempA += "; Occupied";
                    else tempA += "; Unoccupied";
                } else {
                    if (airlockList[i].currentlyActive)
                        tempA += "; Active";
                    else tempA += "; Inactive";
                }
                tempA += nLine + "-- Inner: " + airlockList[i].innerDoors.Count.ToString();
                if (airlockList[i].innerSealed)
                    tempA += "; Sealed";
                else tempA += "; Opened";
                tempA += "; Outer: " + airlockList[i].outerDoors.Count.ToString();
                if (airlockList[i].outerSealed)
                    tempA += "; Sealed";
                else tempA += "; Opened";
                if (airlockList[i].vents.Count > 0) {
                    tempA += nLine + "-- Vents: " + airlockList[i].vents.Count.ToString();
                    tempA += "; O2 Level: " + (airlockList[i].oxygenLevel * 100.0).ToString("N1") + "%";
                }
            }
            return tempA;
        }

        public bool AvailableActions()
        {
            return
                (Runtime.CurrentInstructionCount < (Runtime.MaxInstructionCount * actionLimiterMultiplier) &&
                 Runtime.CurrentCallChainDepth < (Runtime.MaxCallChainDepth * actionLimiterMultiplier) &&
                 (DateTime.Now - tickStartTime).TotalMilliseconds < runTimeLimiter);
        }

        public void ResetScan()
        {
            scannedNearby.Clear();
            scanned.Clear();
            scanNearby.Clear();
        }

        public void CheckForNearbyBlock(Vector3I position)
        {
            AddBlockAt(position);
            Vector3I tempA = AddVectors(Vector3I.Up, position);
            AddBlockAt(tempA);
            tempA = AddVectors(Vector3I.Down, position);
            AddBlockAt(tempA);
            tempA = AddVectors(Vector3I.Left, position);
            AddBlockAt(tempA);
            tempA = AddVectors(Vector3I.Right, position);
            AddBlockAt(tempA);
            tempA = AddVectors(Vector3I.Forward, position);
            AddBlockAt(tempA);
            tempA = AddVectors(Vector3I.Backward, position);
            AddBlockAt(tempA);
        }

        public List<Vector3I> potentialSpots = new List<Vector3I>();
        public void InitialEmptySpot(Vector3I position)
        {
            if (CheckPosition(AddVectors(position, Vector3I.Up)))
                potentialSpots.Add(AddVectors(position, Vector3I.Up));
            if (CheckPosition(AddVectors(position,Vector3I.Down)))
                potentialSpots.Add(AddVectors(position, Vector3I.Down));
            if (CheckPosition(AddVectors(position, Vector3I.Left)))
                potentialSpots.Add(AddVectors(position, Vector3I.Left));
            if (CheckPosition(AddVectors(position, Vector3I.Right)))
                potentialSpots.Add(AddVectors(position, Vector3I.Right));
            if (CheckPosition(AddVectors(position, Vector3I.Forward)))
                potentialSpots.Add(AddVectors(position, Vector3I.Forward));
            if (CheckPosition(AddVectors(position, Vector3I.Backward)))
                potentialSpots.Add(AddVectors(position, Vector3I.Backward));
        }

        public void TryNextArea()
        {
            if (potentialSpots.Count > 0) {
                ScanNearby(potentialSpots[0]);
                potentialSpots.RemoveAt(0);
            }
        }

        public void AddBlockAt(Vector3I position)
        {
            bool tempA = IsVent(position);
            if (tempA) {
                for (int i = 0; i < ventList.Count; i++) {
                    if (ventList[i].Position == position) {
                        airlockList[scanIndex].vents.Add(ventList[i]);
                        ventList.RemoveAt(i);
                        break;
                    }
                }
            }
            if (!tempA) {
                tempA = IsLCD(position);
                if (tempA) {
                    for (int i = 0; i < lcdList.Count; i++) {
                        if (lcdList[i].Position == position) {
                            airlockList[scanIndex].lcds.Add(lcdList[i]);
                            lcdList.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
            if (!tempA) {
                tempA = IsSoundBlock(position);
                if (tempA) {
                    for (int i = 0; i < soundBlockList.Count; i++) {
                        if (soundBlockList[i].Position == position) {
                            airlockList[scanIndex].soundBlocks.Add(soundBlockList[i]);
                            soundBlockList.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        public bool IsVent(Vector3I position)
        {
            if (!Grid.CubeExists(position)) return false;
            else {
                IMySlimBlock block = Grid.GetCubeBlock(position);
                if (block == null) return false;
                else {
                    if (block.BlockDefinition.ToString().ToLower().Contains("airvent"))
                        return true;
                }
            }
            return false;
        }

        public bool IsLCD(Vector3I position)
        {
            if (!Grid.CubeExists(position)) return false;
            else {
                IMySlimBlock block = Grid.GetCubeBlock(position);
                if (block == null) return false;
                else {
                    if (block.BlockDefinition.ToString().ToLower().Contains("textpanel"))
                        return true;
                }
            }
            return false;
        }

        public bool IsDoor(Vector3I position)
        {
            if (!Grid.CubeExists(position)) return false;
            else {
                IMySlimBlock block = Grid.GetCubeBlock(position);
                if (block == null) return false;
                else {
                    if (block.BlockDefinition.ToString().ToLower().Contains("door"))
                        return true;
                }
            }
            return false;
        }

        public bool IsSoundBlock(Vector3I position)
        {
            if (!Grid.CubeExists(position)) return false;
            else {
                IMySlimBlock block = Grid.GetCubeBlock(position);
                if (block == null) return false;
                else {
                    if (block.BlockDefinition.ToString().ToLower().Contains("soundblock"))
                        return true;
                }
            }
            return false;
        }

        public void ScanNearby(Vector3I position)
        {
            CheckAddPosition(position);
            if (!scannedNearby.Contains(position)) {
                scannedNearby.Add(position);
                for (int i = -1; i <= 1; i++)
                    if (i != 0) {
                        Vector3I tempA = AddVectors(position, new Vector3I(i, 0, 0));
                        CheckAddPosition(tempA);
                    }
                for (int i = -1; i <= 1; i++)
                    if (i != 0) {
                        Vector3I tempA = AddVectors(position, new Vector3I(0, i, 0));
                        CheckAddPosition(tempA);
                    }
                for (int i = -1; i <= 1; i++)
                    if (i != 0) {
                        Vector3I tempA = AddVectors(position, new Vector3I(0, 0, i));
                        CheckAddPosition(tempA);
                    }
            }
        }

        public bool CheckPosition(Vector3I position)
        {
            bool tempA = false;
            try {
                if (!Grid.CubeExists(position))
                    tempA = true;
                else {
                    IMySlimBlock block = Grid.GetCubeBlock(position);
                    if (block != null) {
                        string blockDef = block.BlockDefinition.ToString().ToLower();
                        if (blockDef.Contains("textpanel") ||
                            blockDef.Contains("interiorlight") ||
                            blockDef.Contains("sensorblock") ||
                            blockDef.Contains("soundblock"))
                            tempA = true;
                    }
                }
            } catch { Output("Error caught checking specific point for airlock"); }
            return tempA;
        }

        public void CheckAddPosition(Vector3I position)
        {
            if (!scanned.Contains(position)) {
                scanned.Add(position);
                if (CheckPosition(position)) {
                    if (!scannedNearby.Contains(position))
                        scanNearby.Add(position);
                    airlockList[scanIndex].airlockArea.Add(position);
                }
            }
        }

        public Vector3I AddVectors(Vector3I tempA, Vector3I tempB)
        {
            return new Vector3I(tempA.X + tempB.X, tempA.Y + tempB.Y, tempA.Z + tempB.Z);
        }

        public double VDistance(Vector3I a, Vector3I b)
        {
            return Vector3D.Distance(Trans(a), Trans(b));
        }

        public Vector3D Trans(Vector3I position)
        {
            return Grid.GridIntegerToWorld(position);
        }

        public Vector3I Trans(Vector3D position)
        {
            return Grid.WorldToGridInteger(position);
        }

        public void PaintOutput()
        {
            string outputString = Status();
            List<IMyTerminalBlock> textBlocks = new List<IMyTerminalBlock>();
            for (int i = 0; i < outputList.Count; i++) {
                if (outputString != "") outputString += nLine;
                outputString += outputList[i]; }
            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(textBlocks, (p => p.CustomName.ToLower().Contains(outputPanelName.ToLower()) && p.CubeGrid.Equals(Me.CubeGrid)));
            for (int i = 0; i < textBlocks.Count; i++) {
                ((IMyTextSurface)textBlocks[i]).ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                ((IMyTextSurface)textBlocks[i]).WriteText(outputString);
            }
        }

        public void Output(string text)
        {
            if (text != "" && (outputList.Count == 0 || outputList[0] != text)) {
                outputList.Insert(0, text);
                for (int i = 0; i < 75; i++) {
                    if (outputList.Count > outputLimit) outputList.RemoveAt(outputLimit);
                    else break;
                } }
        }

        public class SensorEntity
        {
            public Vector3I Position = new Vector3I(0, 0, 0);
            public long EntityId = -1;
            public MatrixD Orientation;
        }

        public class Autodoor
        {
            public IMyTerminalBlock doorBlock;
            public bool activeCycle = false;
        }

        public class Airlock
        {
            public List<IMyTerminalBlock> innerDoors = new List<IMyTerminalBlock>();
            public List<IMyTerminalBlock> outerDoors = new List<IMyTerminalBlock>();
            public List<IMyTerminalBlock> vents = new List<IMyTerminalBlock>();
            public List<IMyTerminalBlock> lcds = new List<IMyTerminalBlock>();
            public List<IMyTerminalBlock> soundBlocks = new List<IMyTerminalBlock>();
            public List<Vector3I> airlockArea = new List<Vector3I>();
            public IMyTerminalBlock Me;
            public DateTime
                occupyTime = DateTime.Now, soundTime = DateTime.Now,
                checkOccupancyTime = DateTime.Now, openTime = DateTime.Now,
                innerSealTime = DateTime.Now, outerSealTime = DateTime.Now;
            public bool
                currentlyOccupied = false, innerSealed = true, outerSealed = true,
                sealingInner = true, sealingOuter = true, pressurizing = true,
                pressurized = false, innerOpened = false, outerOpened = false,
                currentlyActive = false, facingInner = true, innerCycle = false,
                activeCycle = false, occupiedCycle = false, autoMade = false;
            public double oxygenLevel = 0.0;
            public MyGridProgram parentProgram;
            public string airlockId = "";
            public List<long> activeEntities = new List<long>();

            public double VDistance(Vector3I a, Vector3I b)
            {
                return Vector3D.Distance(Trans(a), Trans(b));
            }

            public double VDistance(Vector3D a, Vector3I b)
            {
                return Vector3D.Distance(a, Trans(b));
            }

            private bool Occupied(Vector3I position)
            {
                try {
                    if (DateTime.Now >= checkOccupancyTime) {
                        checkOccupancyTime = DateTime.Now.AddSeconds(1.5);
                        double dist = parentProgram.largeBlockDistance;
                        if (parentProgram.GridTerminalSystem.Grid.GridSizeEnum == MyCubeSize.Small)
                            dist = parentProgram.smallBlockDistance;
                        for (int i = 0; i < airlockArea.Count; i++) {
                            if (VDistance(position, airlockArea[i]) <= dist)
                                return true;
                        }
                        for (int i = 0; i < innerDoors.Count; i++) {
                            if (VDistance(position, innerDoors[i].Position) <= dist)
                                return true;
                        }
                        for (int i = 0; i < outerDoors.Count; i++) {
                            if (VDistance(position, outerDoors[i].Position) <= dist)
                                return true;
                        }
                        return false;
                    }
                } catch { parentProgram.Output("Error caught checking occupancy of: " + airlockId); }
                return currentlyOccupied;
            }

            public void Control()
            {
                try {
                    ControlVents();
                    ControlDoors();
                } catch { parentProgram.Output("Error caught controlling doors and vents"); }
            }

            public void CheckStatus(SensorEntity info)
            {
                try {
                    if (!parentProgram.useRangeOnly) {
                        if (!occupiedCycle && Occupied(info.Position)) {
                            occupiedCycle = true;
                            if (!activeEntities.Contains(info.EntityId)) activeEntities.Add(info.EntityId);
                        } else if (activeEntities.Contains(info.EntityId)) activeEntities.Remove(info.EntityId);
                    } else currentlyOccupied = false;

                    try {
                        if (!activeCycle) Active(info);
                        OxygenLevel();
                        InnerSealed();
                        OuterSealed();
                    } catch { parentProgram.Output("Error caught getting info"); }

                    if (lcds.Count > 0) { try {
                        string text = airlockId + ": ";
                        if (!parentProgram.useRangeOnly) {
                            if (currentlyOccupied) text += "Occupied";
                            else text += "Empty";
                        } else {
                            if (currentlyActive) text += "Active";
                            else text += "Inactive";
                        }
                        if (sealingInner && !innerSealed) text += ": Sealing Inner";
                        else if (sealingOuter && !outerSealed) text += ": Sealing Outer";
                        else if (!sealingInner || !sealingOuter) text += ": Open";
                        if (vents.Count > 0) {
                            if (pressurizing && oxygenLevel < parentProgram.oxygenFillLevel && ((IMyAirVent)vents[0]).CanPressurize)
                                text += ": Pressurizing";
                            else if (!pressurizing && oxygenLevel > 0.0 && ((IMyAirVent)vents[0]).CanPressurize)
                                text += ": Depressurizing";
                            else if (innerSealed && outerSealed && !((IMyAirVent)vents[0]).CanPressurize)
                                text += ": Not Pressurized";
                            text += Environment.NewLine + ProgressBarGraph(oxygenLevel * 100.0, parentProgram.progressBarLength);
                        }
                        Color fontColor = new Color(255f - (255f * (float)oxygenLevel), 255f * (float)oxygenLevel, 0f);
                        if (vents.Count == 0) fontColor = new Color(0f, 255f, 0f);
                        for (int i = 0; i < lcds.Count; i++) {
                            if (parentProgram.autoUseLCDs || lcds[i].CustomName.ToLower().Contains(parentProgram.innerAirlockPanel.ToLower())) {
                                ((IMyTextSurface)lcds[i]).ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                                ((IMyTextSurface)lcds[i]).WriteText(text);
                                ((IMyTextSurface)lcds[i]).FontColor = fontColor;
                            }
                        }
                    } catch { parentProgram.Output("Error caught creating LCD output for " + airlockId); } }
                } catch { parentProgram.Output("Error caught checking sensor info"); }
            }

            public void Active(SensorEntity info)
            {
                double dist = parentProgram.largeBlockPlayerLead, tempA = 0.0;
                if (parentProgram.Grid.GridSizeEnum == MyCubeSize.Small)
                    dist = parentProgram.smallBlockPlayerLead;
                Vector3D position = AddVectors(Trans(info.Position), MultiplyVectors(info.Orientation.Forward, dist));
                bool inner = true, active = false;
                dist = -1.0;
                for (int x = 0; x < innerDoors.Count; x++)
                {
                    tempA = VDistance(position, innerDoors[x].Position);
                    if (dist == -1.0 || tempA < dist) {
                        inner = true;
                        dist = 0.0 + tempA;
                    }
                    if (tempA <= GetDoorDistance(innerDoors[x]))
                        active = true;
                }
                for (int x = 0; x < outerDoors.Count; x++)
                {
                    tempA = VDistance(position, outerDoors[x].Position);
                    if (dist == -1.0 || tempA < dist) {
                        inner = false;
                        dist = 0.0 + tempA;
                    }
                    if (tempA <= GetDoorDistance(outerDoors[x]))
                        active = true;
                }
                if (active) {
                    parentProgram.Output(airlockId + " Active");
                    activeCycle = true;
                    if (!activeEntities.Contains(info.EntityId)) activeEntities.Add(info.EntityId);
                } else if (!currentlyOccupied && activeEntities.Contains(info.EntityId)) activeEntities.Remove(info.EntityId);
                if (activeEntities.Contains(info.EntityId))
                    if (inner) innerCycle = true;
            }

            public string ProgressBarGraph(double perc, int totalLength) 
            {
                double percentage = perc;
                if (percentage > 100.0) percentage = 100.0;
                string text = "", percString = perc.ToString("N0") + "%";
                if (perc >= 1000.0) percString = "999%";
                int length = totalLength - 2;
                if (percentage >= 0) {
                    double fullBars = ((double)length) * percentage / 100.0;
                    int fBars = (int)fullBars;
                    int emptyBars = length - fBars;
                    int halfBars = 0;

                    if (fullBars >= (((double)fBars) + 0.5) && emptyBars > 0) { emptyBars -= 1; halfBars = 1; }

                    for (int i = 0; i < (int)fullBars; i++)
                        text += "|";
                    for (int i = 0; i < halfBars; i++)
                        text += "¦";
                    for (int i = 0; i < emptyBars; i++)
                        text += "'"; }
                else
                    for (int i = 0; i < length; i++) text += "'";

                return "[" + text + "] " + percString.PadLeft(4, ' ');
            }

            public void OxygenLevel()
            {
                oxygenLevel = 0.0;
                for (int i = 0; i < vents.Count; i++)
                    oxygenLevel += ((IMyAirVent)vents[i]).GetOxygenLevel();
                oxygenLevel = oxygenLevel / (double)vents.Count;
            }

            public void SetSound(bool positive)
            {
                if (DateTime.Now >= soundTime) {
                    soundTime = DateTime.Now.AddSeconds(2);
                    for (int i = 0; i < soundBlocks.Count; i++)
                    {
                        if (parentProgram.autoUseSoundBlocks ||
                            soundBlocks[i].CustomName.ToLower().Contains(parentProgram.soundBlockKeyword.ToLower())) {
                            if (positive) ((IMySoundBlock)soundBlocks[i]).SelectedSound = "Objective complete";
                            else ((IMySoundBlock)soundBlocks[i]).SelectedSound = "Alert 2";
                            ((IMySoundBlock)soundBlocks[i]).Play();
                        }
                    }
                }
            }

            public bool AllowExit()
            {
                if (DateTime.Now < innerSealTime) return false;
                if (!innerSealed) return false;
                else {
                    if (vents.Count == 0) return true;
                    else if (!((IMyAirVent)vents[0]).CanPressurize) return true;
                    else if (oxygenLevel == 0.0 ||
                             parentProgram.inAtmosphere ||
                             parentProgram.oxygenPercentage == 1.0 ||
                             parentProgram.oxygenTankCount == 0)
                        return true;
                }
                return false;
            }

            public bool AllowEntry()
            {
                if (DateTime.Now < outerSealTime) return false;
                if (!outerSealed) return false;
                else {
                    if (vents.Count == 0) return true;
                    else if ((oxygenLevel >= parentProgram.oxygenFillLevel ||
                              parentProgram.oxygenPercentage == 0.0) ||
                             (parentProgram.oxygenTankCount == 0 && parentProgram.generatorCount == 0))
                        return true;
                }
                return false;
            }

            public void ControlDoors()
            {
                if (currentlyActive || currentlyOccupied) {
                    if (facingInner) {
                        if (AllowEntry())
                            SetDoors(true, true);
                        else SetDoors(false, false);
                    } else {
                        if (AllowExit())
                            SetDoors(false, true);
                        else SetDoors(true, false);
                    }
                }
            }

            public void ControlVents()
            {
                if (currentlyActive || currentlyOccupied) {
                    if (facingInner)
                        SetVents(true);
                    else {
                        if (!outerSealed && oxygenLevel > 0.0)
                            SetVents(true);
                        else if (outerSealed)
                            SetVents(false);
                    }
                }
            }

            public void SetVents(bool pressurize)
            {
                if (vents.Count > 0 && ((IMyAirVent)vents[0]).CanPressurize)
                    if ((!pressurize && oxygenLevel > 0.0) || (pressurize && oxygenLevel < parentProgram.oxygenFillLevel))
                        SetSound(false);
                for (int i = 0; i < vents.Count; i++)
                    if (pressurize) {
                        ((IMyAirVent)vents[i]).ApplyAction("Depressurize_Off");
                        if (((IMyAirVent)vents[i]).CanPressurize)
                            pressurizing = true;
                    }
                    else if (!pressurize && innerSealed) {
                        ((IMyAirVent)vents[i]).ApplyAction("Depressurize_On");
                        pressurizing = false;
                    }
            }

            public bool InnerSealed()
            {
                bool tempA = innerSealed;
                innerSealed = true;
                for (int i = 0; i < innerDoors.Count; i++) {
                    if (((IMyDoor)innerDoors[i]).OpenRatio != 0.0F && ((IMyDoor)innerDoors[i]).Status != DoorStatus.Closed) {
                        innerSealed = false;
                        break;
                    }
                }
                if (!tempA && innerSealed) innerSealTime = DateTime.Now.AddSeconds(2);
                return innerSealed;
            }

            public bool OuterSealed()
            {
                bool tempA = outerSealed;
                outerSealed = true;
                for (int i = 0; i < outerDoors.Count; i++) {
                    if (((IMyDoor)outerDoors[i]).OpenRatio != 0.0F && ((IMyDoor)outerDoors[i]).Status != DoorStatus.Closed) {
                        outerSealed = false;
                        break;
                    }
                }
                if (!tempA && outerSealed) outerSealTime = DateTime.Now.AddSeconds(2);
                return outerSealed;
            }

            public void SetDoors(bool inner, bool open)
            {
                if (outerSealed && inner && open) sealingInner = false;
                if (!innerSealed && inner && !open) sealingInner = true;
                if (innerSealed && !inner && open) sealingOuter = false;
                if (!outerSealed && !inner && !open) sealingOuter = true;
                if (((inner && open && outerSealed && !innerOpened) || (!inner && open && innerSealed && !outerOpened)) && DateTime.Now >= soundTime) {
                    SetSound(true);
                    if (inner) innerOpened = true;
                    else outerOpened = true;
                    if (innerSealed) innerOpened = false;
                    if (outerSealed) outerOpened = false;
                }
                if (inner)
                    for (int i = 0; i < innerDoors.Count; i++) {
                        if (open && outerSealed) {
                            if (((IMyDoor)innerDoors[i]).Status != DoorStatus.Open && ((IMyDoor)innerDoors[i]).Status != DoorStatus.Opening)
                                ((IMyDoor)innerDoors[i]).OpenDoor();
                        } else if (((IMyDoor)innerDoors[i]).Status != DoorStatus.Closed && ((IMyDoor)innerDoors[i]).Status != DoorStatus.Closing) {
                            ((IMyDoor)innerDoors[i]).CloseDoor();
                        }
                    }
                else
                    for (int i = 0; i < outerDoors.Count; i++) {
                        if (open && innerSealed) {
                            if (((IMyDoor)outerDoors[i]).Status != DoorStatus.Open && ((IMyDoor)outerDoors[i]).Status != DoorStatus.Opening)
                                ((IMyDoor)outerDoors[i]).OpenDoor();
                        } else if (((IMyDoor)outerDoors[i]).Status != DoorStatus.Closed && ((IMyDoor)outerDoors[i]).Status != DoorStatus.Closing) {
                            ((IMyDoor)outerDoors[i]).CloseDoor();
                        }
                    }
            }

            public double GetDoorDistance(IMyTerminalBlock block)
            {
                try {
                    string name = block.CustomName;
                    int tempA = name.IndexOf("-") + 1;
                    name = name.Substring(tempA, (name.Length - tempA));
                    if (name.Contains(" "))
                        tempA = name.IndexOf(" ");
                    else if (name.Contains("["))
                        tempA = name.IndexOf("[");
                    else tempA = name.Length;
                    return double.Parse(name.Substring(0, tempA));
                } catch { parentProgram.Output("Error parsing door distance"); }
                return 0.0;
            }

            public Vector3D Trans(Vector3I position)
            {
                return parentProgram.Grid.GridIntegerToWorld(position);
            }

            public Vector3I Trans(Vector3D position)
            {
                return parentProgram.Grid.WorldToGridInteger(position);
            }

            public Vector3D AddVectors(Vector3D A, Vector3D B)
            {
                return new Vector3D(A.X + B.X, A.Y + B.Y, A.Z + B.Z);
            }

            public Vector3D SubtractVectors(Vector3D A, Vector3D B)
            {
                return new Vector3D(B.X - A.X, B.Y - A.Y, B.Z - A.Z);
            }

            public Vector3D MultiplyVectors(Vector3D A, double B)
            {
                return new Vector3D(A.X * B, A.Y * B, A.Z * B);
            }
        }
    }
}