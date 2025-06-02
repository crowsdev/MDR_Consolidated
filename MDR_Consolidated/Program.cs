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
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        public Dictionary<UntermenschType,Untermensch> Judenlager;
        public UpdateFrequency Frequency = UpdateFrequency.Update100;
        
        public Program()
        {
            Judenlager = new Dictionary<UntermenschType,Untermensch>()
            {
                { UntermenschType.Thrust, new Thrust(this, UpdateFrequency.Update10) },
                { UntermenschType.Doors, new Doors(this, UpdateFrequency.Update10) },
                { UntermenschType.FuelManager, new FuelManager(this, UpdateFrequency.Update100) }
            };

            #region Set update-frequency to fastest required by slaves.

            if (Judenlager.Any(x => x.Value.Frequency == UpdateFrequency.Update1))
            {
                Frequency = UpdateFrequency.Update1;
            }
            else if (Judenlager.Any(x => x.Value.Frequency == UpdateFrequency.Update10))
            {
                Frequency = UpdateFrequency.Update10;
            }
            else if (Judenlager.Any(x => x.Value.Frequency == UpdateFrequency.Update100))
            {
                Frequency = UpdateFrequency.Update100;
            }

            #endregion
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