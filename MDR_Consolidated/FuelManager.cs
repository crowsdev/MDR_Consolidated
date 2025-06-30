using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;

namespace IngameScript
{
    /*
     * Need to modify code so that h2 tanks are filled one at a time, filling many at same time causes horrible lag.
     */
    public class FuelManager : Untermensch
    {
        #region Settings.

        float LowOxygenAirVent = 0.85f;
        float FullOxygenAirVent = 0.9f;
        double LowGasTanks = (double)0.65;
        double FullGasTanks = (double)0.9;

        #endregion

        #region Events

        public delegate void GasTankLevelEvent(GasTankLevelEventArgs args);

        public static event GasTankLevelEvent GasTankLevelAlarm;

        #endregion
        
        
        List<IMyAirVent> _airVentList => GetFuelSystemObjects<IMyAirVent>();
        List<IMyGasTank> _gasTankList => GetFuelSystemObjects<IMyGasTank>();
        List<IMyGasTank> _gasTankO2List => GetFuelSystemObjects<IMyGasTank>(x => x.DetailedInfo.Contains("Oxygen"));
        List<IMyGasTank> _gasTankH2List => GetFuelSystemObjects<IMyGasTank>(x => x.DetailedInfo.Contains("Hydrogen"));
        List<IMyGasGenerator> _gasGeneratorList => GetFuelSystemObjects<IMyGasGenerator>();

        
        bool LowHydrogen => CheckAnyGasTankLow(_gasTankH2List);
        bool LowOxygen => CheckAnyGasTankLow(_gasTankO2List) || CheckAnyAirVentLow(_airVentList);
        bool LowGas => LowHydrogen || LowOxygen;
        bool HydrogenFull => CheckAllGasTanksFull(_gasTankH2List);
        bool OxygenFull => CheckAllGasTanksFull(_gasTankO2List) && CheckAllAirVentsFull(_airVentList);
        bool GasFull => HydrogenFull && OxygenFull;

        private bool _generatorsRequested = true; // Fill tanks when first started.
        public bool GeneratorsRequested
        {
            get
            {
                if (LowGas)
                {
                    _generatorsRequested = true;
                }

                if (GasFull)
                {
                    _generatorsRequested = false;
                }

                return _generatorsRequested;
            }
        }

        public bool LastGenRequestState = false;

        public FuelManager(MyGridProgram _parent, UpdateFrequency _freq) : base(_parent, _freq)
        {
            // Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public override bool OnMain(string argument, UpdateType updateSource)
        {
            #region Check if this should run according to its own frequency.

            if (!base.OnMain(argument, updateSource))
            {
                return false;
            }

            #endregion
            
            if (LastGenRequestState != GeneratorsRequested)
            {
                LastGenRequestState = GeneratorsRequested;
                _gasGeneratorList.ForEach(x => x.Enabled = GeneratorsRequested);
            }

            return true;
        }

        public override bool TryEcho(ref string _txt)
        {
            _txt =  $"================================\n" +
                    $"GasTank count :: {_gasTankList.Count}.\n" +
                    $"OxygenTank count :: {_gasTankO2List.Count}.\n" +
                    $"HydrogenTank count :: {_gasTankH2List.Count}.\n" +
                    $"GasGenerator count :: {_gasGeneratorList.Count}.\n" +
                    $"================================\n" +
                    $"LowOxygenDetected :: {LowOxygen}.\n" +
                    $"LowHydrogenDetected :: {LowHydrogen}.\n" +
                    $"================================\n" +
                    $"GeneratorsRequested :: {GeneratorsRequested}.\n" +
                    $"================================\n" +
                    $"OxygenFull :: {OxygenFull}.\n" +
                    $"HydrogenFull :: {HydrogenFull}.\n" +
                    $"================================\n";
            return true;
        }

        bool CheckAnyAirVentLow(List<IMyAirVent> _myAirVents)
        {
            bool LowVentDetected = false;

            foreach (var av in _myAirVents)
            {
                if (av.GetOxygenLevel() <= LowOxygenAirVent)
                {
                    string toLog = av.GetOxygenLevel().ToString();
                    Ubermensch.Echo("NEW-OBJECT-DETECTED-------------------------");
                    Ubermensch.Echo("Detected Low AirVent OxygenLevel::" + toLog);
                    toLog = "CustomName::" + av.CustomName;
                    Ubermensch.Echo(toLog);
                    toLog = "EntityId::" + av.CubeGrid.EntityId.ToString();
                    Ubermensch.Echo(toLog);
                    toLog = av.DetailedInfo;
                    Ubermensch.Echo("DetailedInfo::" + toLog);
                    Ubermensch.Echo("--------------------------------------------");
                    LowVentDetected = true;
                }
            }

            return LowVentDetected;
        }

        bool  CheckAnyGasTankLow(List<IMyGasTank> _myGasTanks)
        {
            bool LowTanksDetected = false;

            foreach (var gt in _myGasTanks)
            {
                if (gt.FilledRatio <= LowGasTanks)
                {
                    string toLog = gt.FilledRatio.ToString();
                    Ubermensch.Echo("NEW-OBJECT-DETECTED-------------------------");
                    Ubermensch.Echo("Detected Low GasTank FilledRatio::" + toLog);
                    toLog = "CustomName::" + gt.CustomName;
                    Ubermensch.Echo(toLog);
                    toLog = "EntityId::" + gt.CubeGrid.EntityId.ToString();
                    Ubermensch.Echo(toLog);
                    toLog = gt.DetailedInfo;
                    Ubermensch.Echo("DetailedInfo::" + toLog);
                    Ubermensch.Echo("--------------------------------------------");
                    LowTanksDetected = true;
                }
            }

            return LowTanksDetected;
        }

        bool CheckAllAirVentsFull(List<IMyAirVent> _myAirVents)
        {
            bool bAllVentsFull = true;

            foreach (var av in _myAirVents)
            {
                if (av.GetOxygenLevel() < FullOxygenAirVent)
                {
                    return false;
                }
            }

            return bAllVentsFull;
        }

        bool CheckAllGasTanksFull(List<IMyGasTank> _myGasTanks)
        {
            foreach (var gt in _myGasTanks)
            {
                if (gt.FilledRatio < FullGasTanks)
                {
                    return false;
                }
            }

            return true;
        }

        private List<T> GetFuelSystemObjects<T>(Func<T,bool> _predicate = null) where T : class
        {
            List<T> result = new List<T>();
            Ubermensch.GridTerminalSystem.GetBlocksOfType<T>(result, _predicate);
            return result;
        }

        #region EventHandlers.

        private void HandleH2TankLevelEvents()
        {
            List<IMyTerminalBlock> v0 = new List<IMyTerminalBlock>();
            Ubermensch.GridTerminalSystem.GetBlocksOfType<IMyGasTank>(v0, IsValidH2);
        }

        private bool IsValidH2(IMyTerminalBlock _gasTank)
        {
            IMyTerminalBlock asTermBlk = (IMyTerminalBlock)_gasTank; 
            return (asTermBlk.CubeGrid == this.Ubermensch.Me.CubeGrid && asTermBlk.BlockDefinition.SubtypeId.Contains("Hydrogen"));
        }

        #endregion
    }
}