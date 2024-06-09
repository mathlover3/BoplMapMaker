using BoplFixedMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MapMaker
{
    public class MovingPlatformSignalStuff : LogicGate
    {
        //yes this is all. just for storing some signal related stuff
        public bool SignalIsInverted = false;
        public void Register()
        {
            SignalSystem.RegisterLogicGate(this);
        }
        public bool IsOn()
        {

            if (InputSignals.Count > 0)
            {
                return InputSignals[0].IsOn;
            }
            else
            {
                UnityEngine.Debug.Log("error: MovingPlatformSignalStuff has no inputs");
                return false;
            }

        }
        public override void Logic(Fix SimDeltaTime)
        {
            UnityEngine.Debug.Log("MovingPlatforms Logic");
        }
    }
}
