﻿using BoplFixedMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker
{
    public class NotGate : LogicGate
    {
        public void Register()
        {
            UUID = Plugin.NextUUID;
            Plugin.NextUUID++;
            SignalSystem.RegisterLogicGate(this);
        }
        public override void Logic(Fix SimDeltaTime)
        {
            OutputSignals[0].WasOnLastTick = OutputSignals[0].IsOn;
            OutputSignals[0].IsOn = !InputSignals[0].IsOn;
        }
    }
}
