using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class CATRedAlert : Program
    {
        /*
         *   Cats makes the best programmers!
         *   Really basic red alert light script
         *   Now with sound and closing doors, Light colour alert and better filtering!
         */ 
 
// change these to alter behaviour 
        const string SHIP_PREFIX = "[CAT]"; // the script will only try to effect blocks with this perfix (so docked ships dont get messed up) 
        const string LIGHT_NAME = "Light [ALERT]"; //  The name (excluding prefix) of the light being used to track alert status 
        const string SOUND_NAME = "Sound [ALERT]"; // the name (excluding prefix) of the sound block that activates on alert 
        const string INT_LIGHT_KEYWORD = "Interior Light"; // Set to null to disable: This is a unique part of he name of all lights to change the colour of when alert is tripped 
        Color COLOUR_NORMAL = new Color( 255, 210, 81 ); // white (ish) : Colour when no enemy detected. Color( R, G, B ) values 0 to 255 
        Color COLOUR_ALERT = new Color( 255, 0, 0 ); // red : Colour when enemy detected 
 
// used by the script. don't play with unless editing the way the script works. 
        bool SET = false; // Check to set if setup worked 
        bool ALERT = false; // Current alert status 
        IMyInteriorLight LIGHT = null; // The alert light 
        IMySoundBlock SOUND = null; // The alert light 
 
 
 
 
// used once to ready the script 
        public void Setup() { 
            var sound = (IMySoundBlock)GridTerminalSystem.GetBlockWithName( SHIP_PREFIX +" "+ SOUND_NAME ); 
            if( sound != null ) { 
                SOUND = sound; 
            } 
            var light = (IMyInteriorLight)GridTerminalSystem.GetBlockWithName( SHIP_PREFIX +" "+ LIGHT_NAME ); 
            if( light != null ) { 
                LIGHT = light; 
                SET = true; 
                Update(); 
            } 
     
        } 
 
/*
 * called by Main, it prepares the needed variables and calls the appropriate set methods to update the required blocks for current alert level
 */ 
        public void Update() { 
            Color col = COLOUR_NORMAL; 
            string actName = "StopSound"; 
            if( ALERT ) { 
                col = COLOUR_ALERT; 
                actName = "PlaySound"; 
                CloseAllDoors(); 
            } 
     
            SetLightColor( col, INT_LIGHT_KEYWORD ); 
 
            if( SOUND != null ) { 
                var action = SOUND.GetActionWithName( actName ); 
                if( action != null ) { 
                    action.Apply( SOUND ); 
                } 
            } 
        } 
 
/*
 * Updates the colour of desired interior lights.
 * col is the colour the lights are set to
 * filter is the keyword to the lights to update (or null not to use a filter)
 */ 
        public void SetLightColor( Color col, string filter ) { 
            var blocks = new List<IMyTerminalBlock>(); 
            GridTerminalSystem.GetBlocksOfType<IMyInteriorLight>( blocks ); 
            if( filter == null ) { 
                for( int e=0; e < blocks.Count; e++ ) { 
                    if( blocks[e].CustomName.StartsWith( SHIP_PREFIX ) ) { 
                        blocks[e].SetValue( "Color", col ); 
                    } 
                } 
            } else { 
                for( int e=0; e < blocks.Count; e++ ) { 
                    if( blocks[e].CustomName.StartsWith( SHIP_PREFIX ) && blocks[e].CustomName.Contains( filter ) ) { 
                        blocks[e].SetValue( "Color", col ); 
                    } 
                } 
            } 
        } 
 
/*
 * Close all ship doors
 */ 
        public void CloseAllDoors() { 
            var blocks = new List<IMyTerminalBlock>(); 
            GridTerminalSystem.GetBlocksOfType<IMyDoor>( blocks ); 
            if( blocks.Count > 0 ) { 
                var action = blocks[0].GetActionWithName( "Open_Off" ); 
                if( action != null ) { 
                    for( int e=0; e < blocks.Count; e++ ) { 
                        if( blocks[e].CustomName.StartsWith( SHIP_PREFIX ) ) { 
                            action.Apply( blocks[e] ); 
                        } 
                    } 
                } 
            } 
        } 
 
 
/*
 * return true if state of indicator light has changed
 */ 
        public bool ChangedAlert() { 
            if( LIGHT.Enabled != ALERT ) { 
                ALERT = LIGHT.Enabled; 
                return true; 
            } else { 
                return false; 
            } 
        } 
 
/*
 * LEGACY
 */ 
        public void Print( string txt ) { 
            Display( txt ); 
        } 
 
/*
 *   Used for debugging and information display
 */ 
        public void Display( string txt ) { 
            var blocks = new List<IMyTerminalBlock>(); 
            GridTerminalSystem.GetBlocksOfType<IMyRadioAntenna>( blocks ); 
            if( blocks.Count >= 1 ) { 
                for( int e = 0; e < blocks.Count; e++ ) { 
                    if( SHIP_PREFIX != null ) { 
                        if( blocks[e].CustomName.StartsWith( SHIP_PREFIX ) ) { 
                            blocks[e].SetCustomName( SHIP_PREFIX +'\n'+ txt ); 
                        } 
                    } else { 
                        blocks[e].SetCustomName( ">>"+'\n'+ txt ); 
                    } 
                } 
            } 
        } 
 
/* MAIN */ 
        void Main() { 
            if( SET == false ) { 
                Setup(); 
            } else if( ChangedAlert() ) { 
                Update(); 
            } 
        }
    }
}