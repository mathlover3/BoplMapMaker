using BoplFixedMath;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static UnityEngine.UIElements.UIRAtlasAllocator;
using UnityEngine.UIElements;
using UnityEngine;

namespace MapMaker.Lua_stuff
{
    public class LuaSpawner : MonoBehaviour
    {
        private static GameObject BowObject;
        private static BoplBody arrow;
        private static BoplBody grenade;
        private static DynamicAbilityPickup AbilityPickup;
        private static BoplBody SmokeGrenade;
        private static Explosion MissleExplosion;
        public static Material WhiteSlimeMat;
        public void Awake()
        {
            UnityEngine.Debug.Log("LuaSpawner Awake");
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
                        Spawner.WhiteSlimeMat = mat;
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
        public static BoplBody SpawnArrow(Vec2 pos, Fix scale, Vec2 StartVel, Color color)
        {
            BoplBody boplBody = FixTransform.InstantiateFixed<BoplBody>(arrow, pos);
            boplBody.Scale = scale;
            boplBody.StartVelocity = StartVel;
            boplBody.rotation = CalculateAngle(StartVel);
            boplBody.GetComponent<SpriteRenderer>().material = WhiteSlimeMat;
            boplBody.GetComponent<SpriteRenderer>().color = color;
            return boplBody;
        }
        //modifyed chatgpt code
        public static Fix CalculateAngle(Vec2 vec2)
        {
            // Vector (0, 1)
            Fix refX = Fix.Zero;
            Fix refY = Fix.One;
            if (vec2.y != 0)
            {
                // Dot product
                Fix dotProduct = vec2.x * refX + vec2.y * refY;

                // Magnitudes
                Fix magnitudeA = Fix.Sqrt(vec2.x * vec2.x + vec2.y * vec2.y);
                Fix magnitudeB = Fix.Sqrt(refX * refX + refY * refY);

                // Angle in radians
                Fix angleRadians = Fix.Acos(dotProduct / (magnitudeA * magnitudeB));

                // Convert to degrees
                Fix angleDegrees = angleRadians * ((Fix)180.0 / (Fix)Fix.PI);

                return angleDegrees;
            }
            return Fix.Zero;
        }
        public static BoplBody SpawnGrenade(Vec2 pos, Fix angle, Fix scale, Vec2 StartVel, Fix StartAngularVelocity)
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
            return boplBody;
        }
        public static void SpawnAbilityPickup(Vec2 pos, Fix scale, Vec2 StartVel)
        {
            DynamicAbilityPickup dynamicAbilityPickup = FixTransform.InstantiateFixed<DynamicAbilityPickup>(AbilityPickup, pos);
            dynamicAbilityPickup.InitPickup(null, null, StartVel);
            dynamicAbilityPickup.SwapToRandomAbility();
            var body = dynamicAbilityPickup.GetComponent<BoplBody>();
            body.Scale = scale;
        }
        public static BoplBody SpawnSmokeGrenade(Vec2 pos, Fix angle, Fix scale, Vec2 StartVel, Fix StartAngularVelocity)
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
            return boplBody;
        }
        public static void SpawnNormalExplosion(Vec2 pos, Fix scale)
        {
            AudioManager.Get().Play("explosion");
            FixTransform.InstantiateFixed<Explosion>(MissleExplosion, pos).GetComponent<IPhysicsCollider>().Scale = scale;
        }
    }
}
