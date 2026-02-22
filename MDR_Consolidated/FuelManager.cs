using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Scripting.MemorySafeTypes;

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
        
        
        MemorySafeList<IMyAirVent> _airVentList => GetFuelSystemObjects<IMyAirVent>();
        MemorySafeList<IMyGasTank> _gasTankList => GetFuelSystemObjects<IMyGasTank>();
        MemorySafeList<IMyGasTank> _gasTankO2List => GetFuelSystemObjects<IMyGasTank>(x => x.DetailedInfo.Contains("Oxygen"));
        MemorySafeList<IMyGasTank> _gasTankH2List => GetFuelSystemObjects<IMyGasTank>(x => x.DetailedInfo.Contains("Hydrogen"));
        MemorySafeList<IMyGasGenerator> _gasGeneratorList => GetFuelSystemObjects<IMyGasGenerator>();

        
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
            this.Frequency = _freq;
            this.Ubermensch = _parent;
        }

        public override bool OnMain(string argument, UpdateType updateSource)
        {
            // if (!base.OnMain(argument, updateSource))
            // {
            //     return false;
            // }
            
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

        bool  CheckAnyGasTankLow(MemorySafeList<IMyGasTank> _myGasTanks)
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

        bool CheckAllAirVentsFull(MemorySafeList<IMyAirVent> _myAirVents)
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

        bool CheckAllGasTanksFull(MemorySafeList<IMyGasTank> _myGasTanks)
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

        private MemorySafeList<T> GetFuelSystemObjects<T>(Func<T,bool> _predicate = null) where T : class
        {
            MemorySafeList<T> result = new MemorySafeList<T>();
            Ubermensch.GridTerminalSystem.GetBlocksOfType<T>(result, _predicate);
            return result;
        }
    }
}