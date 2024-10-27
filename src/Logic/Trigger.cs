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
    public class Trigger : MonoUpdatable, ICollisionCallback
    {
        //TRIGGERS PHYSICS BOXES ARE ON LAYER 3!
        public static DPhysicsBox DPhysicsBoxPrefab;
        public LogicOutput LogicOutput = new LogicOutput();
        public DPhysicsBox dPhysicsBox = null;
        public FixTransform fixTrans = null;
        public int UUID;
        public bool Visable = true;
        public Color color = Color.blue;
        public bool DettectPlayers = true;
        public bool DettectGrenades = true;
        public bool DettectArrows = true;
        public bool DettectPlatforms = true;
        public bool DettectBoulders = true;
        public bool DettectEngine = true;
        public bool DettectMissle = true;
        public bool DettectSpike = true;
        public bool DettectSmoke = true;
        public bool DettectSmokeGrenade = true;
        public bool DettectBlackHole = true;
        public bool DettectMine = true;
        public bool DettectTesla = true;
        public bool DettectAbilityOrbs = true;
        private bool Colliding = false;
        public void Awake()
        {
            UnityEngine.Debug.Log("Trigger Awake");
            if (gameObject.name != "TriggerObject")
            {
                LogicOutput.Owner = gameObject;
                //i have no clue why but there are 2 if i dont have this check causing a LOT of errors.
                if (GetComponent<LineRenderer>() == null)
                {
                    LineRenderer lineRenderer = gameObject.AddComponent<LineRenderer>();
                    lineRenderer.material = new Material(Shader.Find("Hidden/Internal-Colored"));
                    lineRenderer.endColor = (lineRenderer.startColor = color);
                    lineRenderer.endWidth = (lineRenderer.startWidth = 0.2f);
                }
                dPhysicsBox = FixTransform.InstantiateFixed<DPhysicsBox>(DPhysicsBoxPrefab, new Vec2(Fix.Zero, Fix.Zero), Fix.Zero);
                //remove hitbox comp
                dPhysicsBox.gameObject.GetComponent<Hitbox>().IsDestroyed = true;
                Destroy(dPhysicsBox.gameObject.GetComponent<Hitbox>());
                //set the layer so the game doesnt think we are something we arent and try to acsess stuff we dont have.
                dPhysicsBox.gameObject.layer = 3;
                //the DPhysicsBox from the HitboxCombo needs manualy init-ed
                dPhysicsBox.ManualInit();
                fixTrans = GetComponent<FixTransform>();
                //register stuff
                dPhysicsBox.RegisterCollisionCallback(this);
                Updater.RegisterUpdatable(this);
            }    


        }
        public void SetPos(Vec2 pos)
        {
            dPhysicsBox.position = pos;
            fixTrans.position = pos;
        }
        public void Register()
        {
            UUID = Plugin.NextUUID;
            Plugin.NextUUID++;
            SignalSystem.RegisterTrigger(LogicOutput);
        }
        public void SetExtents(Vec2 extents)
        {
            dPhysicsBox.SetExtents(extents);
            UnityEngine.Debug.Log("Exstents are " + dPhysicsBox.CalcExtents());
        }
        public override void Init()
        {
            if (gameObject.name != "TriggerObject")
            {
                UnityEngine.Debug.Log("Init");
            }
        }

        public void OnCollide(CollisionInformation collision)
        {
            if (gameObject.name != "TriggerObject")
            {
                if (LayerMask.NameToLayer("wall") == collision.layer)
                {
                    if (DettectPlatforms && collision.colliderPP.fixTrans != null && collision.colliderPP.fixTrans.IsDestroyed == false && collision.colliderPP.fixTrans.GetComponent<AnimateVelocity>() != null)
                    {
                        TurnOn();
                        return;
                    }
                    if (DettectBoulders && collision.colliderPP.fixTrans != null && collision.colliderPP.fixTrans.IsDestroyed == false && collision.colliderPP.fixTrans.GetComponent<Boulder>() != null)
                    {
                        TurnOn();
                        return;
                    }
                }
                if (LayerMask.NameToLayer("Player") == collision.layer && DettectPlayers)
                {
                    TurnOn();
                    return;
                }
                if (LayerMask.NameToLayer("item") == collision.layer)
                {
                    if (DettectGrenades && collision.colliderPP.fixTrans != null && collision.colliderPP.fixTrans.IsDestroyed == false && collision.colliderPP.fixTrans.GetComponent<Grenade>() != null)
                    {
                        TurnOn();
                        return;
                    }
                    if (DettectMissle && collision.colliderPP.fixTrans != null && collision.colliderPP.fixTrans.IsDestroyed == false && collision.colliderPP.fixTrans.GetComponent<Missile>() != null)
                    {
                        TurnOn();
                        return;
                    }
                    if (DettectSmokeGrenade && collision.colliderPP.fixTrans != null && collision.colliderPP.fixTrans.IsDestroyed == false && collision.colliderPP.fixTrans.GetComponent<SmokeGrenadeExplode2>() != null)
                    {
                        TurnOn();
                        return;
                    }
                    if (DettectMine && collision.colliderPP.fixTrans != null && collision.colliderPP.fixTrans.IsDestroyed == false && collision.colliderPP.fixTrans.GetComponent<Mine>() != null)
                    {
                        TurnOn();
                        return;
                    }
                }
                if (LayerMask.NameToLayer("Projectile") == collision.layer && DettectArrows)
                {
                    TurnOn();
                    return;
                }
                if (LayerMask.NameToLayer("NonLethalTerrain") == collision.layer)
                {
                    if (DettectEngine && collision.colliderPP.fixTrans != null && collision.colliderPP.fixTrans.IsDestroyed == false && collision.colliderPP.fixTrans.GetComponent<RocketEngine>() != null)
                    {
                        TurnOn();
                        return;
                    }
                    if (DettectTesla && collision.colliderPP.fixTrans != null && collision.colliderPP.fixTrans.IsDestroyed == false && collision.colliderPP.fixTrans.GetComponent<SimpleSparkNode>() != null)
                    {
                        TurnOn();
                        return;
                    }
                }
                if (LayerMask.NameToLayer("LethalTerrain") == collision.layer && DettectSpike)
                {
                    TurnOn();
                    return;
                }
                if (LayerMask.NameToLayer("Smoke") == collision.layer && DettectSmoke)
                {
                    TurnOn();
                    return;
                }
                if (LayerMask.NameToLayer("weapon") == collision.layer && DettectAbilityOrbs)
                {
                    TurnOn();
                    return;
                }
                if (LayerMask.NameToLayer("RigidBodyAffector") == collision.layer && DettectBlackHole)
                {
                    TurnOn();
                    return;
                }
            }
        }
        public void TurnOn()
        {
            Colliding = true;
            LogicOutput.WasOnLastTick = LogicOutput.IsOn;
            LogicOutput.IsOn = true;
        }

        public override void UpdateSim(Fix SimDeltaTime)
        {
            LogicOutput.WasOnLastTick = LogicOutput.IsOn;
            if (gameObject.name != "TriggerObject")
            {
                if (LogicOutput.IsOn && !Colliding)
                {
                    LogicOutput.WasOnLastTick = LogicOutput.IsOn;
                    LogicOutput.IsOn = false;
                }
                Colliding = false;
                //UnityEngine.Debug.Log("IsOn: " + IsOn);
                if (Visable)
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
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"Error Drawing Debug Lines: {ex}");
                    }
                }
            }
        }
    }
}