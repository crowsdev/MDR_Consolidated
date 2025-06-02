using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class Thrust : Untermensch
    {
        internal string UpThrustGroupName;
        internal List<IMyThrust> UpThrustGroupFunctional;
        internal float UpThrustPercentage;
        internal int UpThrustUnitCountTotal;
        internal int UpThrustUnitCountFunctional;
        
        // internal string DisplayTag = "[MDR-LCD]";


        public Thrust(MyGridProgram _parent, UpdateFrequency _freq) : base(_parent, _freq)
        {
            UpThrustGroupName = "DD1 - LIFT THRUST";
            UpThrustGroupFunctional = new List<IMyThrust>();
            UpThrustPercentage = 0f;
            UpThrustUnitCountFunctional = 0;
            // Ubermensch.Me.CustomData = "Pending....";
        }

        public override bool OnMain(string argument, UpdateType updateSource)
        {
            #region Check if this should run according to its own frequency.

            if (!base.OnMain(null, updateSource))
            {
                return false;
            }

            #endregion
            
            // Collect the desired thruster blocks.
            Ubermensch.GridTerminalSystem.GetBlocksOfType(UpThrustGroupFunctional);
            UpThrustUnitCountTotal = UpThrustGroupFunctional.Count;
            UpThrustUnitCountFunctional = UpThrustGroupFunctional.Count(x => x.IsFunctional); // Get the amount of thruster blocks collected.
            UpThrustPercentage = UpThrustGroupFunctional.Select(x => x.CurrentThrustPercentage).Average();
            string toDisplay = $"   Current Thrust % = {UpThrustPercentage}.\n" +
                               $"   Total Thrusters in Group = {UpThrustUnitCountFunctional}/{UpThrustUnitCountTotal}.\n";

            Ubermensch.Me.CustomData = toDisplay;

            return true;
        }
    }
}