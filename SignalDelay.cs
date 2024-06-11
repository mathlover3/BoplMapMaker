using BoplFixedMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker
{
    public class SignalDelay : LogicGate
    {
        public Fix delay = Fix.One;
        private List<bool> SignalBuffer = new List<bool>();
        private List<Fix> SignalTimeBuffer = new List<Fix>();

        public void Register()
        {
            SignalSystem.RegisterLogicGate(this);
        }
        public bool IsOn()
        {
            return InputSignals[0].IsOn;
        }
        public override void Logic(Fix SimDeltaTime)
        {
            SignalBuffer.Add(IsOn());
            SignalTimeBuffer.Add(Updater.SimTimeSinceLevelLoaded);
            if (SignalTimeBuffer[0] + delay < Updater.SimTimeSinceLevelLoaded)
            {
                //UnityEngine.Debug.Log("hi");
                var signal = SignalBuffer[0];
                OutputSignals[0].IsOn = signal;
                SignalBuffer.RemoveAt(0);
                SignalTimeBuffer.RemoveAt(0);
            }
        }
    }
}
