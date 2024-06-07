using BoplFixedMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MapMaker
{
    internal class Trigger : MonoUpdatable, ICollisionCallback, IUpdatable
    {
        //TRIGGERS PHYSICS BOXES ARE ON LAYER 3!
        public static DPhysicsBox DPhysicsBoxPrefab;
        public uint Signal = 0;
        public DPhysicsBox dPhysicsBox = null;
        public IPhysicsCollider hitbox = null;
        public FixTransform fixTrans = null;
        public void Awake()
        {
            UnityEngine.Debug.Log("Trigger Awake");

            //i have no clue why but there are 2 if i dont have this check causing a LOT of errors.
            if (GetComponent<LineRenderer>() == null) 
            {
                LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));
                lineRenderer.endColor = (lineRenderer.startColor = Color.black);
                lineRenderer.endWidth = (lineRenderer.startWidth = 0.2f);
            }
            dPhysicsBox = FixTransform.InstantiateFixed<DPhysicsBox>(DPhysicsBoxPrefab, new Vec2(Fix.Zero, Fix.Zero), Fix.Zero);
            dPhysicsBox.gameObject.GetComponent<Hitbox>().IsDestroyed = true;
            Destroy(dPhysicsBox.gameObject.GetComponent<Hitbox>());
            dPhysicsBox.tag = "TriggerBox";
            dPhysicsBox.gameObject.layer = 3;
            dPhysicsBox.ManualInit();
            fixTrans = GetComponent<FixTransform>();
            dPhysicsBox.RegisterCollisionCallback(this);
            SignalSystem.RegisterTrigger(this);
            Updater.RegisterUpdatable(this);
            hitbox = GetComponent<IPhysicsCollider>();
            //hitbox.RegisterCollisionCallback(this);

        }
        public void SetPos(Vec2 pos)
        {
            dPhysicsBox.position = pos;
            fixTrans.position = pos;
        }
        public void SetExtents(Vec2 extents)
        {
            dPhysicsBox.SetExtents(extents);
            UnityEngine.Debug.Log("Exstents are " + dPhysicsBox.CalcExtents());
        }
        public override void Init()
        {
            UnityEngine.Debug.Log("Init");
        }

        public void OnCollide(CollisionInformation collision)
        {
            UnityEngine.Debug.Log("OnCollide! " + collision);
        }

        public override void UpdateSim(Fix SimDeltaTime)
        {
            
            try
            {
                //debug stuff
                LineRenderer lineRenderer = gameObject.GetComponent<LineRenderer>();
                FixTransform component = base.GetComponent<FixTransform>();
                Vector2 vector = (Vector2)dPhysicsBox.CalcExtents();
                Vector3 b = base.transform.right * vector.x;
                Vector3 b2 = base.transform.up * vector.y;
                Vector3 a = (component == null) ? base.transform.position : (base.transform.position + base.transform.up * (float)component.offset.y + base.transform.right * (float)component.offset.x);
                Vector3 vector2 = a + b + b2;
                Vector3 vector3 = a + b - b2;
                Vector3 vector4 = a - b + b2;
                Vector3 vector5 = a - b - b2;
                lineRenderer.positionCount = 5;
                lineRenderer.SetPosition(0, vector2);
                lineRenderer.SetPosition(1, vector3);
                lineRenderer.SetPosition(2, vector5);
                lineRenderer.SetPosition(3, vector4);
                lineRenderer.SetPosition(4, vector2);
                Gizmos.DrawLine(vector2, vector3);
                Gizmos.DrawLine(vector3, vector5);
                Gizmos.DrawLine(vector5, vector4);
                Gizmos.DrawLine(vector4, vector2);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Error Drawing Debug Lines: {ex}");
            }
            
        }

    }
}