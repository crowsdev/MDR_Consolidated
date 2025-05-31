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
        public List<(UntermenschType untermenschType, Untermensch untermensch)> Judenlager;
        public UpdateFrequency Frequency = UpdateFrequency.Update100;
        
        public Program()
        {
            Judenlager = new List<(UntermenschType untermenschType, Untermensch untermensch)>()
            {
                { (UntermenschType.Thrust, new Thrust(this, UpdateFrequency.Update10)) },
                { (UntermenschType.Doors, new Doors(this, UpdateFrequency.Update10)) },
                { (UntermenschType.FuelManager, new FuelManager(this, UpdateFrequency.Update100)) }
            };

            #region Set update-frequency to fastest required by slaves.

            if (Judenlager.Any(x => x.untermensch.Frequency == UpdateFrequency.Update1))
            {
                Frequency = UpdateFrequency.Update1;
            }
            else if (Judenlager.Any(x => x.untermensch.Frequency == UpdateFrequency.Update10))
            {
                Frequency = UpdateFrequency.Update10;
            }
            else if (Judenlager.Any(x => x.untermensch.Frequency == UpdateFrequency.Update100))
            {
                Frequency = UpdateFrequency.Update100;
            }

            #endregion
        }

        public void Save()
        {
            Judenlager.ForEach(x => x.untermensch.OnSave());
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Judenlager.ForEach(x =>
            {
                x.untermensch.OnMain(argument, updateSource);
                
                if (x.untermensch.TryEcho(out string echoTxt))
                {
                    this.Echo(echoTxt);
                }
            });
        }
    }
}