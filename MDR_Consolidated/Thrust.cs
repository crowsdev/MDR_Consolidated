using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class Thrust : Untermensch
    {
        internal UpdateFrequency Frequency = UpdateFrequency.Update100;

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
            Ubermensch.Me.CustomData = "Pending....";
            // Ubermensch.Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        public override bool OnMain(string argument, UpdateType updateSource)
        {
            #region Check if this should run according to its own frequency.

            if (!base.OnMain(argument, updateSource)) return false;

            #endregion
            
            // Collect the desired thruster blocks.
            Ubermensch.GridTerminalSystem.GetBlockGroupWithName(UpThrustGroupName).GetBlocksOfType<IMyThrust>(UpThrustGroupFunctional, delegate(IMyThrust thrust) { return thrust.IsFunctional && thrust.Enabled && thrust.IsWorking; });
            List<IMyThrust> allBlocksInGroup = new List<IMyThrust>();
            Ubermensch.GridTerminalSystem.GetBlockGroupWithName(UpThrustGroupName).GetBlocksOfType<IMyThrust>(allBlocksInGroup);
            UpThrustUnitCountTotal = allBlocksInGroup.Count;
            UpThrustUnitCountFunctional = UpThrustGroupFunctional.Count; // Get the amount of thruster blocks collected.
            float totalSum = 0f; // Init

            foreach (var thruster in UpThrustGroupFunctional)
            {
                totalSum += thruster.CurrentThrustPercentage;
            }

            UpThrustPercentage = totalSum / UpThrustUnitCountFunctional;

            string toDisplay = $"   Current Thrust % = {UpThrustPercentage}.\n" +
                               $"   Total Thrusters in Group = {UpThrustUnitCountFunctional}/{UpThrustUnitCountTotal}.\n";

            Ubermensch.Me.CustomData = toDisplay;

            return true;
        }
    }
}