using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class AlertManager : Program
    {
        /*
         * R e a d m e
         * -----------
         *
         * Run this with various arguments to trigger various alarms:
         * - "Red"
         * - "Yellow"
         * - "Blue"
         * - "BioHazard"
         * - "Off" / No argument -> Deactivates alerts
         * CAREFUL: Upper/Lowercase matters! Omit the quotation marks.
         */

// Set the name of your alert light group.
// BEWARE: SOME SETTINGS WILL BE MODIFIED BY THIS SCRIPT! BE CAREFUL TO CHOOSE THE CORRECT GROUP!
// Modify settings like blink timing using the default terminal.
        private const string ALERT_GROUP_NAME = "Alert Lights";

        private const string ALERT_SOUND_GROUP = "Alert Sounds";

// Normal light dim settings - configure lighting intensity of your chosen dim group for each alert type
        private const string DIM_LIGHT_GROUP = "Bridge Lights";
        private const float RED_ALERT_INTENSITY = 0.5f;
        private const float YELLOW_ALERT_INTENSITY = 2.0f;
        private const float BLUE_ALERT_INTENSITY = 0.5f;
        private const float BIOHAZARD_ALERT_INTENSITY = 0.3f;
        private const float NO_ALERT_INTENSITY = 2.0f; // Set this to the default intensity of your chosen group

//////////////////////// DO NOT TOUCH BELOW THIS ////////////////////////

        public AlertManager()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Once;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            List<IMyTerminalBlock> lights = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlockGroupWithName(ALERT_GROUP_NAME).GetBlocks(lights);
            AlertType type = ParseAlertType(argument);

            foreach(var light in lights)
            {
                Color color;
                switch(type)
                {
                    case AlertType.Red:
                        color = Color.Red;
                        light.ApplyAction("OnOff_On");
                        SetDimGroupLightIntensity(RED_ALERT_INTENSITY);
                        break;
                    case AlertType.Yellow:
                        color = new Color(255, 206, 0);
                        light.ApplyAction("OnOff_On");
                        SetDimGroupLightIntensity(YELLOW_ALERT_INTENSITY);
                        break;
                    case AlertType.Blue:
                        color = Color.Blue;
                        light.ApplyAction("OnOff_On");
                        SetDimGroupLightIntensity(BLUE_ALERT_INTENSITY);
                        break;
                    case AlertType.BioHazard:
                        color = new Color(103, 0, 255);
                        light.ApplyAction("OnOff_On");
                        SetDimGroupLightIntensity(BIOHAZARD_ALERT_INTENSITY);
                        break;
                    default: // Equals off
                        color = Color.Black;
                        light.ApplyAction("OnOff_Off");
                        SetDimGroupLightIntensity(NO_ALERT_INTENSITY);
                        break;
                }
                light.SetValue("Color", color);
            }
        }


        private void SetDimGroupLightIntensity(float intensity)
        {
            List<IMyTerminalBlock> lights = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlockGroupWithName(DIM_LIGHT_GROUP)?.GetBlocks(lights);
            foreach (IMyLightingBlock light in lights)
            {
                light.Intensity = intensity;
            }
        }


        private AlertType ParseAlertType(string alert)
        {
            try
            {
                return (AlertType)Enum.Parse(typeof(AlertType), alert, true);
            }
            catch
            {
                return AlertType.Off;
            }
        }


        private void ToggleSoundBlock(bool play)
        {
            var soundBlocks = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlockGroupWithName(ALERT_SOUND_GROUP)?.GetBlocks(soundBlocks);

            if(play)
            {
                foreach (IMySoundBlock soundBlock in soundBlocks)
                {
                    soundBlock.Play();
                }
            }
            else
            {
                foreach (IMySoundBlock soundBlock in soundBlocks)
                {
                    soundBlock.Stop();
                }
            }
        }


        private enum AlertType
        {
            Red,
            Yellow,
            Blue,
            BioHazard,
            Off
        }
    }
}