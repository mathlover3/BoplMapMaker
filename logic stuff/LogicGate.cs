using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoplFixedMath;
using UnityEngine;
using static UnityEngine.UIElements.UIRAtlasManager;

namespace MapMaker
{
    public abstract class LogicGate : MonoBehaviour
    {
        public readonly List<LogicInput> InputSignals = new();
        public readonly List<LogicOutput> OutputSignals = new();
        //called when all of the inputs have been updated
        public abstract void Logic(Fix SimDeltaTime);
    }
}
