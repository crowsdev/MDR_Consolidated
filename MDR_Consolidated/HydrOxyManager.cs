using System;
using System.Collections.Generic;
using System.Linq;
using IngameScript.Enums;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Collections;

namespace IngameScript
{
    class HydrOxyManager : Untermensch
    {
        #region User Settings.
        
        // When airvent O2 level is LT this value, this airvent requests GasGenerators until full.
        private float O2Low = 0.85f;
        
        // When airvent O2 level is GTE this value, this airvent is considered 'full' and stops requesting GasGenerators.
        private float O2High = 0.90f;
        
        // When GasTank filled-ratio is LT this value, this GasTank's 'OnOff_On' function is invoked.
        // When using 'OneByOne' mode this setting is ignored.
        private double H2Low = 0.65;
        
        // When GasTank filled-ratio is GTE this value, this GasTank's 'OnOff_Off' function is invoked.
        // When using 'OneByOne' mode a single H2Tank is turned on until GTE this setting. This loops through all H2Tanks.
        private double H2High = 0.90;
        
        // Set to 'OneByOne' if tank filling causes too much lag. Otherwise 'AnyLow' mode will fill any tanks that are H2Low concurrently.
        // Use the '//' characters to disable 1 of the following 2 lines to select desired TankFillMode. 'AnyLow' mode is selected by default and 'OneByOne' is disabled. 
        
        private TankFillMode H2FillMode = TankFillMode.AnyLow;
        // private TankFillMode H2FillMode = TankFillMode.OneByOne;
        
        // END OF TankFillMode options.

        #endregion
        
        public HydrOxyManager(MyGridProgram ubermensch, UpdateFrequency frequency = UpdateFrequency.Update100) : base(ubermensch, frequency)
        {
            // On startup, turn all off if OneByOne mode.
            if (H2FillMode == TankFillMode.OneByOne)
            {
                H2Tanks.ForEach(x =>
                {
                    if (x.Enabled) x.ApplyAction(Off);
                });
            }
        }

        #region Blocks.

        List<IMyAirVent> AirVents
        {
            get
            {
                return GetBlocks<IMyAirVent>(x => Ubermensch.Me.CubeGrid == x.CubeGrid);
            }
        }
        
        List<IMyGasTank> GasTanks
        {
            get
            {
                return GetBlocks<IMyGasTank>(x =>
                {
                    return Ubermensch.Me.CubeGrid == x.CubeGrid;
                });
            }
        }

        List<IMyGasTank> O2Tanks
        {
            get
            {
                return GasTanks.Where(x => x.DetailedInfo.Contains("Oxygen")).ToList();
            }
        }
        
        List<IMyGasTank> H2Tanks
        {
            get
            {
                return GasTanks.Where(x => x.DetailedInfo.Contains("Hydrogen")).ToList();
            }
        }

        List<IMyGasGenerator> GasGenerators
        {
            get
            {
                return GetBlocks<IMyGasGenerator>(x => Ubermensch.Me.CubeGrid == x.CubeGrid);
            }
        }

        int H2TankToFillIndex = -1;

        #endregion

        #region Status.

        private bool IsO2High => AirVents.TrueForAll(x => x.GetOxygenLevel() >= O2High) && O2Tanks.TrueForAll(x => x.FilledRatio >= O2High);
        private bool IsH2High => H2Tanks.TrueForAll(x => x.FilledRatio >= H2High);
        bool IsO2Low => (AirVents.Any(x => x.GetOxygenLevel() < O2Low) || O2Tanks.Any(x => x.FilledRatio < O2Low)) || (GeneratorsRequested && !IsO2High);
        bool IsH2Low => H2Tanks.Any(x => x.FilledRatio < H2Low) || (GeneratorsRequested && !IsH2High);

        private bool GeneratorsRequested => IsO2Low || IsH2Low;
        private bool LastGeneratorsRequested = false;

        #endregion

        private string Off = "OnOff_Off";
        private string On = "OnOff_On";

        #region PB Events

        public override bool OnMain(string argument, UpdateType updateSource)
        {
            #region Check if this should run according to its own frequency.

            if (!base.OnMain(argument, updateSource))
            {
                return false;
            }

            #endregion
            
            // This handles turning generators on and off as required.
            if (GeneratorsRequested != LastGeneratorsRequested)
            {
                LastGeneratorsRequested = GeneratorsRequested;
                GasGenerators.ForEach(x => x.Enabled = GeneratorsRequested);
            }

            switch (H2FillMode)
            {
                case TankFillMode.OneByOne:
                {
                    HandleH2OneByOne();
                    break;
                }
            }

            return true;
        }

        public override void OnSave()
        {
            base.OnSave();
        }

        public override bool TryEcho(ref string _txt)
        {
            _txt =  $"================================\n" +
                    $"GasTank count :: {GasTanks.Count}.\n" +
                    $"OxygenTank count :: {O2Tanks.Count}.\n" +
                    $"HydrogenTank count :: {H2Tanks.Count}.\n" +
                    $"GasGenerator count :: {GasGenerators.Count}.\n" +
                    $"================================\n" +
                    $"LowOxygenDetected :: {IsO2Low}.\n" +
                    $"LowHydrogenDetected :: {IsH2Low}.\n" +
                    $"================================\n" +
                    $"GeneratorsRequested :: {GeneratorsRequested}.\n" +
                    $"================================\n" +
                    $"OxygenFull :: {IsO2High}.\n" +
                    $"HydrogenFull :: {IsH2High}.\n" +
                    $"================================\n";
            return true;
        }

        #endregion

        public void HandleH2OneByOne()
        {
            if (H2TankToFillIndex == -1 || H2TankToFillIndex == H2Tanks.Count)
            {
                H2TankToFillIndex = 0;
                H2Tanks[H2TankToFillIndex].ApplyAction(On);
            }

            if (H2Tanks[H2TankToFillIndex].FilledRatio < H2High) return;
            
            H2Tanks[H2TankToFillIndex].ApplyAction(Off);
            H2TankToFillIndex++;
            if (H2TankToFillIndex == H2Tanks.Count) H2TankToFillIndex = 0;
            H2Tanks[H2TankToFillIndex].ApplyAction(On);
        }
        
        private List<T> GetBlocks<T>(Func<T,bool> _predicate = null) where T : class
        {
            List<T> result = new List<T>();
            Ubermensch.GridTerminalSystem.GetBlocksOfType<T>(result, _predicate);
            return result;
        }
    }
}