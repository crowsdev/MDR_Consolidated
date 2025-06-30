using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class AnimatedAlertPanels : Program
    {
        //What to look for in the name of LCD Panels and Lights.
        string idText = "Alert";

//The Image Change Interval to set on the LCDs.
        float imageChangeInterval = 0.3f;

//The colors for the Lights.
        Color greenColor = new Color(0,255,0);
        Color yellowColor = new Color(255, 255, 0);
        Color redColor = new Color(255, 0, 0);
        Color blueColor = new Color(0, 0, 255);

//The blink intervals for the lights, 0 means no blinking.
        float greenBlinkInterval = 0f;
        float yellowBlinkInterval = 0f;
        float redBlinkInterval = 1.5f;
        float blueBlinkInterval = 0f;

//The texts for the LCD Text Panels.
        string greenText = "Condition Green";
        string yellowText = "Yellow Alert";
        string redText = "Red Alert";
        string blueText = "Blue Alert\nLanding";
        string blueTakeoffText = "Blue Alert\nTakeoff";
        string blueDockingText = "Blue Alert\nDocking";

//The textures to apply to the LCD panels.
        string[] green = {"Green Alert 0", "Green Alert 1", "Green Alert 2", "Green Alert 3", "Green Alert 4"};
        string[] yellow = {"Yellow Alert 0", "Yellow Alert 1", "Yellow Alert 2", "Yellow Alert 3", "Yellow Alert 4"};
        string[] red = {"Red Alert 0", "Red Alert 1", "Red Alert 2", "Red Alert 3", "Red Alert 4"};
        string[] blue = {"Blue Alert 0", "Blue Alert 1", "Blue Alert 2", "Blue Alert 3", "Blue Alert 4"};
        string[] blueTakeoff = {"Blue Takeoff Alert 0", "Blue Takeoff Alert 1", "Blue Takeoff Alert 2", "Blue Takeoff Alert 3", 
            "Blue Takeoff Alert 4"}; 
        string[] blueDocking = {"Blue Docking Alert 0", "Blue Docking Alert 1", "Blue Docking Alert 2", 
            "Blue Docking Alert 3", "Blue Docking Alert 4"};

        void Main(string argument)
        {
            List<string> target = null;
            Color targetColor;
            float targetBlinkInterval;
            string alert;
            string text;
 
            argument = argument.Trim();

            if( "green".Equals(argument, StringComparison.InvariantCultureIgnoreCase) ){
                target = new List<string>(green);
                targetColor = greenColor;
                targetBlinkInterval = greenBlinkInterval;
                alert = "green";
                text = greenText;
                Echo("Condition Green");
            }
            else if( "yellow".Equals(argument, StringComparison.InvariantCultureIgnoreCase) ){
                target = new List<string>(yellow); 
                targetColor = yellowColor; 
                targetBlinkInterval = yellowBlinkInterval;
                alert = "yellow";
                text = yellowText;
                Echo("Yellow Alert");
            }
            else if( "red".Equals(argument, StringComparison.InvariantCultureIgnoreCase) ){
                target = new List<string>(red); 
                targetColor = redColor; 
                targetBlinkInterval = redBlinkInterval;
                alert = "red";
                text = redText;
                Echo("Red Alert");
            }
            else if( "blue".Equals(argument, StringComparison.InvariantCultureIgnoreCase) ||
                     "blue landing".Equals(argument, StringComparison.InvariantCultureIgnoreCase)){
                target = new List<string>(blue); 
                targetColor = blueColor; 
                targetBlinkInterval = blueBlinkInterval;
                alert =  "blue";
                text = blueText;
                Echo("Blue Landing Alert");
            }
            else if( "blue takeoff".Equals(argument, StringComparison.InvariantCultureIgnoreCase) ){ 
                target = new List<string>(blueTakeoff);  
                targetColor = blueColor;  
                targetBlinkInterval = blueBlinkInterval;
                alert = "blue";
                text = blueTakeoffText;
                Echo("Blue Takeoff Alert"); 
            }
            else if( "blue docking".Equals(argument, StringComparison.InvariantCultureIgnoreCase) ){ 
                target = new List<string>(blueDocking);  
                targetColor = blueColor;  
                targetBlinkInterval = blueBlinkInterval; 
                alert = "blue";
                text = blueDockingText;
                Echo("Blue Docking Alert"); 
            }
            else{
                Echo("Unknown Condition:");
                Echo(argument);
                Echo("Aborting");
                return;
            }
    
            List<IMyTerminalBlock> list = new List<IMyTerminalBlock>();

            GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(list, checkBlock);

            for(int i = 0; i < list.Count; i++){
                IMyTextPanel current = list[i] as IMyTextPanel;

                if (!extractTag(current).Contains("Text")) {
                    current.WriteText("");
                    current.ChangeInterval = imageChangeInterval;
                    current.ClearImagesFromSelection();
                    current.AddImagesToSelection(target, true);
                } else {
                    current.WriteText(text);
                    current.ClearImagesFromSelection();
                }
            }

            list.Clear();
 
            getAllSurfaceProviders(list);

            for(int i = 0; i < list.Count; i++){
                IMyTextSurfaceProvider current = list[i] as IMyTextSurfaceProvider;
                string tag = extractTag(list[i]);
                List<int> surfaces = getSurfaces(tag);
       
                for(int j = 0; j < surfaces.Count; ++j) {
                    if (surfaces[j] >= current.SurfaceCount) {
                        Echo(list[i].CustomName + " does not have a display "+surfaces[j]);
                        continue;
                    }
                    IMyTextSurface surface = current.GetSurface(surfaces[j]);
                    if (!tag.Contains("Text")) {
                        surface.WriteText("");
                        surface.ChangeInterval = imageChangeInterval;
                        surface.ClearImagesFromSelection();
                        surface.AddImagesToSelection(target, true);
                    } else {   
                        surface.WriteText(text);
                        surface.ClearImagesFromSelection();
                    }
                }
            }

            list.Clear();

            GridTerminalSystem.GetBlocksOfType<IMyLightingBlock>(list, checkBlock);

            for(int i = 0; i < list.Count; i++){
                IMyLightingBlock current = list[i] as IMyLightingBlock;

                current.SetValue<Color>("Color",targetColor);
                current.SetValueFloat("Blink Interval", targetBlinkInterval);
            }

            list.Clear();
            GridTerminalSystem.GetBlocksOfType<IMySoundBlock>(list, checkBlock);

            for(int i = 0; i < list.Count; i++){
                IMySoundBlock current = list[i] as IMySoundBlock;
        
                if(extractTag(current).Contains(alert)){
                    current.ApplyAction("PlaySound");
                } else {
                    current.ApplyAction("StopSound");
                }        
            }
        }

        void getAllSurfaceProviders(List<IMyTerminalBlock> list) {
            GridTerminalSystem.GetBlocks(list);
            list.RemoveAll(predicateForGetAllSurfaceProviders);
        }

        bool predicateForGetAllSurfaceProviders(IMyTerminalBlock block) {
            return !checkBlock(block)  || !(block is IMyTextSurfaceProvider);
        }

        List<int> getSurfaces(string tag) {
            int displayLength = "display=".Length;
            int start = tag.IndexOf("display=");
            if (start < 0) {
                return new List<int>();
            }

            int end = tag.IndexOf(" ", start + displayLength);
            if (end < 0) {
                end = tag.Length;
            }
            string display = tag.Substring(start + displayLength, end - start - displayLength);

            string[] strings = display.Split(',');
            List<int> result = new List<int>();
            for(int i = 0; i < strings.Length; ++i) {
                result.Add(Int32.Parse(strings[i]));
            }
            return result;
        }

        string extractTag(IMyTerminalBlock block){
            string name = block.CustomName; 
            int start = name.IndexOf("["); 
            int end = name.IndexOf("]"); 
 
            if( start < 0 || end < 0 || end < start){ 
                return ""; 
            }
            return name.Substring(start + 1, end - start - 1);
        }

        bool checkBlock(IMyTerminalBlock block){
            string tag = extractTag(block);
            return tag.Contains(idText);
        }
    }
}