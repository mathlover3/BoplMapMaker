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

                //TODO: Fix it not working correctly when it is called multiple times in one frame (this causes odd behavor like having the buffer be 4 big when the delay is 0 when it should only be 1 big (at the end of the frame anyways))
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
        }
    }
}
