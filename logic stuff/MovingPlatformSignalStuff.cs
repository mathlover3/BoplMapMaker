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
            UUID = Plugin.NextUUID;
            Plugin.NextUUID++;
            SignalSystem.RegisterLogicGate(this);
            //should update its connectson line so it always points to the platform
            SignalSystem.RegisterInputThatUpdatesConnectson(InputSignals[0]);
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
            //UnityEngine.Debug.Log("MovingPlatforms Logic");
        }
    }
}
