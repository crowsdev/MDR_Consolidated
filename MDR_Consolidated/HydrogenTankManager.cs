using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class HydrogenTankManager : Program
    {
        // To setup, Turn all hydrogen tanks offline, then select 1 of the tanks to be set as online. The script will then start refueling the first one and jump
        // to the next one after the first one is full. 
        // Removed oxygen feature as its not really needed(oxygen refills are normally instant by itself)
        // Nothing more to be added really. Its a simple setup for 1 purpose(Less Lag)

        public HydrogenTankManager()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
        }

        void Main(string argument)
        {
            // block declarations
            List<IMyTerminalBlock> v0 = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(v0, filterThisHyd);//Manages Hydrogen Tanks

            // logic
            for(int i = 0; i < v0.Count; i++) 
            {
                if(((IMyGasTank)v0[i]).Enabled == true && ((IMyGasTank)v0[i]).FilledRatio == 1 && i != v0.Count-1) // if tank is enabled & FULL & not last tank in list...
                { //Checks if tank is the enabled tank+If its filled 100% + if its not the last tank to be filled, the -1 to v0.count is EXTREMLY important, as it counts from 1 not 0.
                    v0[i].ApplyAction("OnOff_Off"); // turn current tank OFF.
                    v0[i+1].ApplyAction("OnOff_On"); // turn next tank in list ON
                    break; //Break is to give the next tank the chance to start filling, without this tanks will all be disabled
                }
                else if(((IMyGasTank)v0[i]).Enabled == true && ((IMyGasTank)v0[i]).FilledRatio == 0 && i != 0) // if tank is enabled & EMPTY & not the first item in List<T>
                { //Checks if tank is the enabled tank+If its filled 100% + if its not the last tank to be emptied
                    v0[i].ApplyAction("OnOff_Off");
                    v0[i-1].ApplyAction("OnOff_On");
                }

            } 
            Echo("Refueling Hydro");
            Echo("");
            Echo("Refueling 1 Tank at a time");
            Echo("");
            Echo("Reminder to turn off all hydro tanks and turn 1 hydro tank on if script fails");

        }
        
        bool filterThisHyd(IMyTerminalBlock block) { //For Hydrogen
            return (block.CubeGrid == Me.CubeGrid && block.BlockDefinition.SubtypeId.Contains("Hydrogen")); //For oxygen, put ! before block.BlockDefinition -> !block.BlockDefinition
        }
    }
}