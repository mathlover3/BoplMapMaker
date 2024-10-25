using BoplFixedMath;
using HarmonyLib;
using MapMaker.Lua_stuff;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace MapMaker
{
    public class Spawner : LogicGate
    {
        private static GameObject BowObject;
        private static BoplBody arrow;
        private static BoplBody grenade;
        private static DynamicAbilityPickup AbilityPickup;
        private static BoplBody SmokeGrenade;
        private static Explosion MissleExplosion;
        public static Material WhiteSlimeMat;
        public ObjectSpawnType spawnType = ObjectSpawnType.None;
        public Fix SimTimeBetweenSpawns = Fix.One;
        public Fix angle = Fix.Zero;
        public Fix scale = Fix.One;
        public Color ArrowOrBoulderColor = Color.white;
        public Vec2 velocity = new Vec2(Fix.Zero, Fix.Zero);
        public Fix angularVelocity = Fix.Zero;
        public PlatformType BoulderType = PlatformType.grass;
        private FixTransform fixTransform;
        private Fix RelitiveSimTime;
        public bool UseSignal = false;
        //if true then it will only activate on rising edge.
        public bool IsTriggerSignal = false;
        private bool HasTrigged = false;

        public void Awake()
        {
            UnityEngine.Debug.Log("Spawner Awake");
            fixTransform = (FixTransform)GetComponent(typeof(FixTransform));
            //only do all of this if it hasnt already been done.
            if (BowObject == null || arrow == null || grenade == null || AbilityPickup == null)
            {


                GameObject[] allObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
                UnityEngine.Debug.Log("getting Bow object");
                var objectsFound = 0;
                var ObjectsToFind = 4;
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name == "Bow")
                    {
                        // Found the object with the desired name
                        // You can now store its reference or perform any other actions
                        BowObject = obj;
                        UnityEngine.Debug.Log("Found the object: " + obj.name);
                        objectsFound++;
                        if (objectsFound == ObjectsToFind)
                        {
                            break;
                        }
                    }
                    if (obj.name == "AbilityPickup_Dynamic")
                    {
                        // Found the object with the desired name
                        // You can now store its reference or perform any other actions
                        UnityEngine.Debug.Log("Found the object: " + obj.name);
                        AbilityPickup = obj.GetComponent<DynamicAbilityPickup>();
                        objectsFound++;
                        if (objectsFound == ObjectsToFind)
                        {
                            break;
                        }
                    }
                    if (obj.name == "Smoke")
                    {
                        // Found the object with the desired name
                        // You can now store its reference or perform any other actions
                        UnityEngine.Debug.Log("Found the object: " + obj.name);
                        SmokeGrenade = obj.GetComponent<ThrowItem2>().ItemPrefab;
                        objectsFound++;
                        if (objectsFound == ObjectsToFind)
                        {
                            break;
                        }
                    }
                    if (obj.name == "Spark")
                    {
                        // Found the object with the desired name
                        // You can now store its reference or perform any other actions
                        UnityEngine.Debug.Log("Found the object: " + obj.name);
                        MissleExplosion = obj.GetComponent<Missile>().onHitExplosionPrefab;
                        objectsFound++;
                        if (objectsFound == ObjectsToFind)
                        {
                            break;
                        }
                    }
                    //Smoke
                }
                UnityEngine.Debug.Log("getting Grenade");
                ThrowItem2[] allThrowItem2 = Resources.FindObjectsOfTypeAll(typeof(ThrowItem2)) as ThrowItem2[];
                foreach (ThrowItem2 obj in allThrowItem2)
                {
                    if (obj.name == "Grenade")
                    {
                        // Found the object with the desired name
                        // You can now store its reference or perform any other actions
                        UnityEngine.Debug.Log("Found the object: " + obj.name);
                        grenade = obj.ItemPrefab;
                    }
                }
                //get the BowTransform
                var BowTransform = BowObject.GetComponent(typeof(BowTransform)) as BowTransform;
                //get the Arrow prefab from the BowTransform
                arrow = (BoplBody)AccessTools.Field(typeof(BowTransform), "Arrow").GetValue(BowTransform);
            }
            if (WhiteSlimeMat == null)
            {
                Material[] allMaterials = Resources.FindObjectsOfTypeAll(typeof(Material)) as Material[];
                foreach (Material mat in allMaterials)
                {
                    if (mat.name == "whiteSlime")
                    {
                        WhiteSlimeMat = mat;
                        LuaSpawner.WhiteSlimeMat = mat;
                    }
                }
            }
        }
        public void Register()
        {
            UUID = Plugin.NextUUID;
            Plugin.NextUUID++;
            SignalSystem.RegisterLogicGate(this);
            SignalSystem.RegisterGateThatAlwaysRuns(this);
        }
        public bool IsOn()
        {
            return InputSignals[0].IsOn;
        }
        public enum ObjectSpawnType
        {
            None,
            Boulder,
            Arrow,
            Grenade,
            AbilityOrb,
            SmokeGrenade,
            Explosion
        }
        public void SpawnArrow(Vec2 pos, Fix scale, Vec2 StartVel, Fix StartAngularVelocity)
        {
            BoplBody boplBody = FixTransform.InstantiateFixed<BoplBody>(arrow, pos, angle);
            boplBody.Scale = scale;
            boplBody.StartVelocity = StartVel;
            boplBody.rotation = CalculateAngle(StartVel);
            boplBody.StartAngularVelocity = StartAngularVelocity;
            boplBody.GetComponent<SpriteRenderer>().material = WhiteSlimeMat;
            boplBody.GetComponent<SpriteRenderer>().color = ArrowOrBoulderColor;
        }
        //modifyed chatgpt code
        public static Fix CalculateAngle(Vec2 vec2)
        {
            // Vector (0, 1)
            Fix refX = Fix.Zero;
            Fix refY = Fix.One;

            // Dot product
            Fix dotProduct = vec2.x * refX + vec2.y * refY;

            // Magnitudes
            Fix magnitudeA = Fix.Sqrt(vec2.x * vec2.x + vec2.y * vec2.y);
            Fix magnitudeB = Fix.Sqrt(refX * refX + refY * refY);

            // Angle in radians
            Fix angleRadians = Fix.Acos(dotProduct / (magnitudeA * magnitudeB));

            return angleRadians;
        }
        public void SpawnGrenade(Vec2 pos, Fix angle, Fix scale, Vec2 StartVel, Fix StartAngularVelocity)
        {
            BoplBody boplBody = FixTransform.InstantiateFixed<BoplBody>(grenade, pos, angle);
            boplBody.Scale = scale;
            boplBody.StartVelocity = StartVel;
            boplBody.rotation = angle;
            boplBody.StartAngularVelocity = StartAngularVelocity;
            Item component = boplBody.GetComponent<Item>();
            component.OwnerId = 255;
            var Grenade = component.GetComponent<Grenade>();

            Grenade.hasBeenThrown = true;
            DPhysicsCircle dphysicsCircle = (boplBody != null) ? boplBody.GetComponent<DPhysicsCircle>() : null;
            if (dphysicsCircle != null && !dphysicsCircle.IsDestroyed)
            {
                if (!dphysicsCircle.initHasBeenCalled)
                {
                    dphysicsCircle.ManualInit();
                }
            }
        }
        public void SpawnAbilityPickup(Vec2 pos, Fix scale, Vec2 StartVel)
        {
            DynamicAbilityPickup dynamicAbilityPickup = FixTransform.InstantiateFixed<DynamicAbilityPickup>(AbilityPickup, pos);
            dynamicAbilityPickup.InitPickup(null, null, StartVel);
            dynamicAbilityPickup.SwapToRandomAbility();
            var body = dynamicAbilityPickup.GetComponent<BoplBody>();
            body.Scale = scale;
        }
        public void SpawnSmokeGrenade(Vec2 pos, Fix angle, Fix scale, Vec2 StartVel, Fix StartAngularVelocity)
        {
            BoplBody boplBody = FixTransform.InstantiateFixed<BoplBody>(SmokeGrenade, pos, angle);
            boplBody.Scale = scale;
            boplBody.StartVelocity = StartVel;
            boplBody.rotation = angle;
            boplBody.StartAngularVelocity = StartAngularVelocity;
            Item component = boplBody.GetComponent<Item>();
            var Grenade = component.GetComponent<Grenade>();
            Grenade.DetonatesOnOwner = true;
            Grenade.hasBeenThrown = true;
            DPhysicsCircle dphysicsCircle = (boplBody != null) ? boplBody.GetComponent<DPhysicsCircle>() : null;
            if (dphysicsCircle != null && !dphysicsCircle.IsDestroyed)
            {
                if (!dphysicsCircle.initHasBeenCalled)
                {
                    dphysicsCircle.ManualInit();
                }
            }
        }
        public void SpawnNormalExplosion(Vec2 pos, Fix scale)
        {
            FixTransform.InstantiateFixed<Explosion>(MissleExplosion, pos).GetComponent<IPhysicsCollider>().Scale = scale;
        }
        public void SpawnMyItem()
        {
            Vec2 pos = fixTransform.position;
            switch (spawnType)
            {
                case ObjectSpawnType.Boulder:
                    if (BoulderType != PlatformType.slime)
                    {
                        ArrowOrBoulderColor = Color.white;
                    }
                    var boulder = PlatformApi.PlatformApi.SpawnBoulder(pos, scale, BoulderType, ArrowOrBoulderColor);
                    var dphysicsRoundedRect = boulder.hitbox;
                    dphysicsRoundedRect.velocity = velocity;
                    dphysicsRoundedRect.angularVelocity = angularVelocity;
                    // * dphysicsRoundedRect.inverseMass;
                    break;
                case ObjectSpawnType.Arrow:
                    SpawnArrow(pos, scale, velocity, angularVelocity);
                    break;
                case ObjectSpawnType.Grenade:
                    SpawnGrenade(pos, angle, scale, velocity, angularVelocity);
                    break;
                case ObjectSpawnType.AbilityOrb:
                    SpawnAbilityPickup(pos, scale, velocity);
                    break;
                case ObjectSpawnType.SmokeGrenade:
                    SpawnSmokeGrenade(pos, angle, scale, velocity, angularVelocity);
                    break;
                case ObjectSpawnType.Explosion:
                    SpawnNormalExplosion(pos, scale);
                    break;
            }
        }

        public override void Logic(Fix SimDeltaTime)
        {
            if (gameObject.name != "SpawnerObject")
            {
                if (!GameTime.IsTimeStopped() && PlatformApi.PlatformApi.gameInProgress && !UseSignal)
                {

                    RelitiveSimTime = RelitiveSimTime + SimDeltaTime;
                    if (RelitiveSimTime > SimTimeBetweenSpawns)
                    {
                        RelitiveSimTime = RelitiveSimTime - SimTimeBetweenSpawns;
                        SpawnMyItem();
                    }
                    return;
                }
                if (!GameTime.IsTimeStopped() && PlatformApi.PlatformApi.gameInProgress && UseSignal && IsOn())
                {
                    //if its in trigger signal mode and it hasnt trigged yet
                    if (IsTriggerSignal && !HasTrigged)
                    {
                        SpawnMyItem();
                        HasTrigged = true;
                    }
                    //if its not in trigger signal mode
                    if (!IsTriggerSignal)
                    {
                        RelitiveSimTime = RelitiveSimTime + SimDeltaTime;
                        if (RelitiveSimTime > SimTimeBetweenSpawns)
                        {
                            RelitiveSimTime = RelitiveSimTime - SimTimeBetweenSpawns;
                            SpawnMyItem();
                        }
                    }
                }
                //if we use signal but its off then reset HasTrigged;
                if (!GameTime.IsTimeStopped() && PlatformApi.PlatformApi.gameInProgress && UseSignal && !IsOn())
                {
                    HasTrigged = false;
                }
            }
        }
    }
}
