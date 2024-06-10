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
    public abstract class LogicGate: MonoBehaviour
    {
        public readonly List<LogicInput> InputSignals = new();
        public readonly List<LogicOutput> OutputSignals = new();
        //called when all of the inputs have been updated
        public abstract void Logic(Fix SimDeltaTime);
    }
    public abstract class LogicGateTrigger : LogicGate, IUpdatable
    {

        public int hierarchyNumber;

        public bool IsDestroyed { get; set; }

        public int HierarchyNumber
        {
            get
            {
                return hierarchyNumber;
            }
            set
            {
                hierarchyNumber = value;
            }
        }

        public abstract void Init();

        public abstract void UpdateSim(Fix SimDeltaTime);

        public virtual bool IsEnabled()
        {
            return base.isActiveAndEnabled;
        }

        public virtual void LateUpdateSim(Fix SimDeltaTime)
        {
        }

        public virtual void OnDestroyUpdatable()
        {
        }
    }
}
