using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public abstract class Untermensch
    {
        public MyGridProgram Ubermensch { get; private set; } // This is the main script object.
        public UpdateFrequency Frequency { get; set; }

        public Dictionary<UpdateFrequency, float> FrequencyFloats = new Dictionary<UpdateFrequency, float>()
        {
            { UpdateFrequency.Update1, 1.0f },
            { UpdateFrequency.Update10, 10.0f },
            { UpdateFrequency.Update100, 100.0f },
            { UpdateFrequency.Once, -1.0f },
            { UpdateFrequency.None, -1.0f }
        };

        public float TickFactor => (FrequencyFloats[Frequency] / FrequencyFloats[Ubermensch.Runtime.UpdateFrequency]);
        public float LastTick { get; set; } = 0.0f;

        protected Untermensch(MyGridProgram ubermensch, UpdateFrequency frequency = UpdateFrequency.Update100)
        {
            Ubermensch = ubermensch;
            Frequency = frequency;
        }

        public virtual bool OnMain(string argument, UpdateType updateSource)
        {
            #region Count everytime main script runs at its frequency and check if this slave script should run with regard to its own frequency.

            if (LastTick++ >= TickFactor)
            {
                LastTick = 0.0f;
                return true;
            }

            return false;

            #endregion
        }

        public virtual void OnSave()
        {
            // Override derived classes.
        }

        public virtual bool TryEcho(ref string _txt)
        {
            _txt = null;
            return false;
            // Do not call base.TryEcho from override methods.
        }
    }
}