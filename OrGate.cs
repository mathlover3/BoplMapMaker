using BoplFixedMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker
{
    public class OrGate : LogicGate
    {
        public void Register()
        {
            SignalSystem.RegisterLogicGate(this);
        }
        public override void Logic(Fix SimDeltaTime)
        {
            var output = OutputSignals[0];
            //UnityEngine.Debug.Log($"InputSignals has length {InputSignals.Count}");
            //if there are 0 signals then Logic will never be called.
            if (InputSignals.Count == 1)
            {
                output.WasOnLastTick = output.IsOn;
                output.IsOn = InputSignals[0].IsOn;

                return;
            }
            var CurrentValue = false;
            //and all of the inputs together.
            for (int i = 0; i < InputSignals.Count; i++)
            {
                CurrentValue = InputSignals[i].IsOn || CurrentValue;
            }
            output.WasOnLastTick = output.IsOn;
            output.IsOn = CurrentValue;

        }
    }
}
