using BoplFixedMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MapMaker
{
    public class ShakePlatform : LogicGate
    {
        //if true it only activates when the signal turns on. instead of contunesy activating as long as the signal is on.
        public bool OnlyActivateOnRise = false;
        public ShakablePlatform shakablePlatform;
        public Fix duration = Fix.One;
        public Fix shakeAmount = Fix.One;
        private bool WasOnLastFrame = false;
        public void Register()
        {
            SignalSystem.RegisterLogicGate(this);
            //should update its connectson line so it always points to the platform
            SignalSystem.RegisterInputThatUpdatesConnectson(InputSignals[0]);
            //must always run so that it can do its logic
            SignalSystem.RegisterGateThatAlwaysRuns(this);
        }
        public bool IsOn()
        {

            if (InputSignals.Count > 0)
            {
                return InputSignals[0].IsOn;
            }
            else
            {
                UnityEngine.Debug.Log("error: ShakePlatform has no inputs");
                return false;
            }

        }
        public override void Logic(Fix SimDeltaTime)
        {
            //if it should be on
            if ((OnlyActivateOnRise && !WasOnLastFrame && IsOn()) || (!OnlyActivateOnRise && IsOn()))
            {
                shakablePlatform.AddShake(duration, shakeAmount, 11, null, null);
                
            }
            WasOnLastFrame = IsOn();
        }
    }
}
