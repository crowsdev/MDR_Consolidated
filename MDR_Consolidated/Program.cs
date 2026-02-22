using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Scripting.MemorySafeTypes;
using VRageMath;

namespace IngameScript
{
    public class Program : MyGridProgram
    {
        public MemorySafeDictionary<UntermenschType,Untermensch> Judenlager;
        public UpdateFrequency Frequency = UpdateFrequency.Update1;
        
        public Program()
        {
            Judenlager = new MemorySafeDictionary<UntermenschType,Untermensch>()
            {
                { UntermenschType.Thrust, new Thrust(this, UpdateFrequency.Update1) },
                { UntermenschType.Doors, new SussDoors(this, UpdateFrequency.Update1) },
                { UntermenschType.FuelManager, new FuelManager(this, UpdateFrequency.Update1) }
            };

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            
            // #region Set update-frequency to fastest required by slaves.
// 
            // if (Judenlager.Any(x => x.Value.Frequency == UpdateFrequency.Update1))
            // {
            //     Frequency = UpdateFrequency.Update1;
            // }
            // else if (Judenlager.Any(x => x.Value.Frequency == UpdateFrequency.Update10))
            // {
            //     Frequency = UpdateFrequency.Update10;
            // }
            // else // if (Judenlager.Any(x => x.Value.Frequency == UpdateFrequency.Update100))
            // {
            //     Frequency = UpdateFrequency.Update100;
            // }
// 
            // #endregion
        }

        public void Save()
        {
            foreach (var kvp0 in Judenlager)
            {
                kvp0.Value.OnSave();
            }
        }

        public void Main(string argument, UpdateType updateSource)
        {
            foreach (var x in Judenlager)
            {
                x.Value.OnMain(argument, updateSource);
                string echoTxt = "";
                if (x.Value.TryEcho(ref echoTxt))
                {
                    this.Echo(echoTxt);
                }
            }
        }
    }
}