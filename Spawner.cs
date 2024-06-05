using BoplFixedMath;
using HarmonyLib;
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
    internal class Spawner : MonoUpdatable
    {
        private static GameObject BowObject;
        private static BoplBody arrow;
        private static BoplBody grenade;
        private static DynamicAbilityPickup AbilityPickup;
        private static BoplBody SmokeGrenade;
        private static Explosion MissleExplosion;

        public ObjectSpawnType spawnType = ObjectSpawnType.None;
        public Fix SimTimeBetweenSpawns = Fix.One;
        public Fix angle = Fix.Zero;
        public Fix scale = Fix.One;
        public Color color = Color.white;
        public Vec2 velocity = new Vec2(Fix.Zero, Fix.Zero);
        public Fix angularVelocity = Fix.Zero;
        public PlatformType BoulderType = PlatformType.grass;
        private FixTransform fixTransform;
        private Fix RelitiveSimTime;
        public bool UseSignal = false;
        //up to 256 signals.
        public byte Signal = 0;
        public void Awake()
        {
            UnityEngine.Debug.Log("Spawner Awake");
            Updater.RegisterUpdatable(this);
            fixTransform = (FixTransform)this.GetComponent(typeof(FixTransform));
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
        }
        public override void Init()
        {
        }

        public override void UpdateSim(Fix SimDeltaTime)
        {
            if (!GameTime.IsTimeStopped() && PlatformApi.PlatformApi.gameInProgress)
            {
                RelitiveSimTime = RelitiveSimTime + SimDeltaTime;
                if (RelitiveSimTime > SimTimeBetweenSpawns)
                {
                    RelitiveSimTime = RelitiveSimTime - SimTimeBetweenSpawns;
                    Vec2 pos = fixTransform.position;
                    switch (spawnType)
                    {
                        case ObjectSpawnType.Boulder:
                            var boulder = PlatformApi.PlatformApi.SpawnBoulder(pos, scale, BoulderType, color);
                            var dphysicsRoundedRect = boulder.hitbox;
                            dphysicsRoundedRect.velocity = velocity;
                            dphysicsRoundedRect.angularVelocity = angularVelocity;
                                // * dphysicsRoundedRect.inverseMass;
                            break;
                        case ObjectSpawnType.Arrow:
                            SpawnArrow(pos, angle, scale, velocity, angularVelocity);
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
            }
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
        public void SpawnArrow(Vec2 pos, Fix angle, Fix scale, Vec2 StartVel, Fix StartAngularVelocity)
        {
            BoplBody boplBody = FixTransform.InstantiateFixed<BoplBody>(arrow, pos, angle);
            boplBody.Scale = scale;
            boplBody.StartVelocity = StartVel;
            boplBody.rotation = angle;
            boplBody.StartAngularVelocity = StartAngularVelocity;
        }
        public void SpawnGrenade(Vec2 pos, Fix angle, Fix scale, Vec2 StartVel, Fix StartAngularVelocity)
        {
            BoplBody boplBody = FixTransform.InstantiateFixed<BoplBody>(grenade, pos, angle);
            boplBody.Scale = scale;
            boplBody.StartVelocity = StartVel;
            boplBody.rotation = angle;
            boplBody.StartAngularVelocity = StartAngularVelocity;
            Item component = boplBody.GetComponent<Item>();
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
    }
}
