using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class GasTankLevelEventArgs : System.EventArgs
    {
        public IMyGasTank GasTank { get; set; }
        public GasTankLevelEvents GasTankEventType { get; set; }
    }
}