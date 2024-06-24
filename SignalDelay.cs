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
        private Fix lastSimTime;

        public void Register()
        {
            //we also need to register the inputs. sure the output will be registerd multiple times but thats fine.
            SignalSystem.RegisterInput(InputSignals[0]);
            //this may change output even if the input doesnt change.
            SignalSystem.RegisterTrigger(OutputSignals[0]);
        }
        public bool IsOn()
        {
            return InputSignals[0].IsOn;
        }
        public override void Logic(Fix SimDeltaTime)
        {
            if (Updater.SimTimeSinceLevelLoaded != lastSimTime)
            {
                lastSimTime = Updater.SimTimeSinceLevelLoaded;
                SignalBuffer.Add(IsOn());
                SignalTimeBuffer.Add(Updater.SimTimeSinceLevelLoaded);
                if (SignalTimeBuffer[0] + delay < Updater.SimTimeSinceLevelLoaded)
                {
                    //UnityEngine.Debug.Log("hi");
                    var signal = SignalBuffer[0];
                    OutputSignals[0].WasOnLastTick = OutputSignals[0].IsOn;
                    OutputSignals[0].IsOn = signal;
                    SignalBuffer.RemoveAt(0);
                    SignalTimeBuffer.RemoveAt(0);
                }
            }
            //if its already been called this frame then we dont want to add more but we want to replace the one for this frame with the new input in case it changed.
            else
            {
                SignalBuffer[SignalBuffer.Count - 1] = IsOn();
            }
        }
    }
}
