using BoplFixedMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker
{
    internal class Trigger : MonoUpdatable, ICollisionCallback
    {
        public uint Signal = 0;
        public DPhysicsBox dPhysicsBox = null;
        public IPhysicsCollider hitbox = null;
        public void Awake()
        {
            UnityEngine.Debug.Log("Trigger Awake");
            dPhysicsBox = gameObject.AddComponent<DPhysicsBox>();
            SignalSystem.RegisterTrigger(this);
            //hitbox = GetComponent<IPhysicsCollider>();
            //hitbox.RegisterCollisionCallback(this);
        }
        public void SetPos(Vec2 pos)
        {
            dPhysicsBox.position = pos;
        }
        public void SetExtents(Vec2 extents)
        {
            dPhysicsBox.SetExtents(extents);
        }
        public override void Init()
        {
            UnityEngine.Debug.Log("Init");
        }

        public void OnCollide(CollisionInformation collision)
        {
            UnityEngine.Debug.Log("OnCollide");
        }

        public override void UpdateSim(Fix SimDeltaTime)
        {

        }
    }
}
