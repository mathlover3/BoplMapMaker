using BoplFixedMath;
using HarmonyLib;
using UnityEngine;

namespace MapMaker.Lua_stuff
{
    public class LuaSpawneraaaaaaaaaaaa : MonoBehaviour
    {
        private static GameObject BowObject;
        private static BoplBody arrow;
        private static BoplBody grenade;
        private static SpikeAttack spikePrefab;

        private static BoplBody mine;
        private static BlackHole blackHole;
        private static DynamicAbilityPickup AbilityPickup;
        private static BoplBody SmokeGrenade;
        private static Explosion MissleExplosion;
        public static Material WhiteSlimeMat;
        public void Awake()
        {
            UnityEngine.Debug.Log("LuaSpawner Awake");
            //only do all of this if it hasnt already been done.
            if (BowObject == null || arrow == null || grenade == null || AbilityPickup == null || blackHole == null || spikePrefab == null)
            {


                GameObject[] allObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
                UnityEngine.Debug.Log("Getting Game Objects");
                var objectsFound = 0;
                var ObjectsToFind = 6;
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
                    if (obj.name == "Mine")
                    {
                        // Found the object with the desired name
                        // You can now store its reference or perform any other actions
                        mine = obj.GetComponent<BoplBody>();
                        UnityEngine.Debug.Log("Found the object: " + obj.name);
                        objectsFound++;
                        if (objectsFound == ObjectsToFind)
                        {
                            break;
                        }
                    }
                    if (obj.name == "BlackHole2")
                    {
                        // Found the object with the desired name
                        // You can now store its reference or perform any other actions
                        blackHole = obj.GetComponent<BlackHole>();
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
                    if (obj.name == "SpikeAttack")
                    {
                        // Found the object with the desired name
                        // You can now store its reference or perform any other actions
                        UnityEngine.Debug.Log("Found the object: " + obj.name);
                        spikePrefab = obj.GetComponent<SpikeAttack>();
                        objectsFound++;
                        if (objectsFound == ObjectsToFind)
                        {
                            break;
                        }
                    }
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
            Explosion,
            BlackHole
        }
        public static BoplBody SpawnArrow(Vec2 pos, Fix scale, Vec2 StartVel, Color color)
        {
            BoplBody boplBody = FixTransform.InstantiateFixed<BoplBody>(arrow, pos);
            boplBody.Scale = scale;
            boplBody.StartVelocity = StartVel;
            boplBody.GetComponent<SpriteRenderer>().material = WhiteSlimeMat;
            boplBody.GetComponent<SpriteRenderer>().color = color;
            return boplBody;
        }
        public static BoplBody SpawnMine(Vec2 pos, Fix scale, Vec2 StartVel, Color color, bool chase)
        {
            BoplBody boplBody = FixTransform.InstantiateFixed<BoplBody>(mine, pos);
            boplBody.Scale = scale;
            boplBody.StartVelocity = StartVel;
            boplBody.rotation = CalculateAngle(StartVel);
            Mine mineObj = boplBody.GetComponent<Mine>();
            if (!chase) mineObj.item.OwnerId = 255;
            mineObj.item.OwnerId = 256;
            mineObj.SetMaterial(WhiteSlimeMat);
            mineObj.ScansFor = 256;
            mineObj.Light.color = color;

            return boplBody;
        }
        public static BoplBody SpawnSpike(Fix surfacePosX, Fix surfacePosY, Fix offset, StickyRoundedRectangle attachedGround, Fix scale)
        {
            var spikeObj = FixTransform.InstantiateFixed<SpikeAttack>(spikePrefab, new Vec2(surfacePosX, surfacePosY));
            spikeObj.Initialize(new Vec2(surfacePosX, surfacePosY), offset, attachedGround, scale);
            return spikeObj.hitbox.body;
        }
        public static BoplBody SpawnSpikeFromPercentAroundSurface(Fix percentAroundSurface, Fix offset, StickyRoundedRectangle attachedGround, Fix scale)
        {

            // for some reason a value of 1 doesn't loop back to 0 and instead just goes in the middle of the wrong side
            // similar thing for 0 not going to the correct place,
            // so percentAroundSurface has to be manually clamped to the range of (val above 0 but low as possible) to (val below 1 but high as possible)
            if (percentAroundSurface >= 1)
            {
                percentAroundSurface = (Fix)(1 - Fix.Precision);
            }
            if (percentAroundSurface <= 0)
            {
                // Fix.Precision for some reason isn't low enough.
                percentAroundSurface = (Fix)(Fix.Precision * 2);
            }
            var spikePos = attachedGround.PositionFromLocalPlayerPos(percentAroundSurface, (Fix)1);
            var spikeObj = FixTransform.InstantiateFixed<SpikeAttack>(spikePrefab, new Vec2(spikePos.x, spikePos.y));
            spikeObj.Initialize(new Vec2(spikePos.x, spikePos.y), offset, attachedGround, scale, false);

            var platformBodyPos = attachedGround.GetGroundBody().position;

            attachedGround.alignRotation(spikeObj.hitbox.body);
            spikeObj.UpdateRelativeOrientation();

            spikeObj.groundOrientationAtCastTime = spikeObj.groundOrientationAtCastTime + spikeObj.hitbox.GetBody().relativeOrientation + Fix.Pi;
            spikeObj.UpdateRelativeOrientation();


            return spikeObj.hitbox.body;
        }
        /*
                public static BlackHole SpawnBlackHole(Vec2 pos, Fix scale)
                {
                    BlackHole blackHole2 = FixTransform.InstantiateFixed<BlackHole>(blackHole, pos);
                    blackHole2.Grow(Fix.One/(scale-blackHole2.growth), Fix.Zero);
                    return blackHole2;
                }
                */
        public static BlackHole SpawnBlackHole(Vec2 pos)
        {
            BlackHole blackHole2 = FixTransform.InstantiateFixed<BlackHole>(blackHole, pos);
            return blackHole2;
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

                return angleRadians;
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
