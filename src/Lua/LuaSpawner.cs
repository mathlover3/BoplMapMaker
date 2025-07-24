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
using System.Net.Configuration;
using TMPro;

namespace MapMaker.Lua_stuff
{
    public class LuaSpawner : MonoBehaviour
    {
        private static GameObject BowObject;
        private static BoplBody arrow;
        private static BoplBody grenade;
        private static BoplBody mine;
        private static BoplBody missile;
        private static BlackHole blackHole;
        private static DynamicAbilityPickup AbilityPickup;
        private static BoplBody SmokeGrenade;
        private static Explosion MissleExplosion;
        public static Material WhiteSlimeMat;

        private static SpikeAttack spikePrefab;

        public void Awake()
        {
            UnityEngine.Debug.Log("LuaSpawner Awake");
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
            UnityEngine.Debug.Log("Getting Game Objects");
            var numObjectsFound = 0;
            var numObjectsToFind = 8;
            // I hate how messy this is but it can't easily be flattened into a loop because they all do slightly different things
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "Bow" && BowObject == null)
                {
                    // Found the object with the desired name
                    // You can now store its reference or perform any other actions
                    BowObject = obj;
                    UnityEngine.Debug.Log("Found the object: " + obj.name);
                    numObjectsFound++;
                    if (numObjectsFound == numObjectsToFind)
                    {
                        break;
                    }
                }
                if (obj.name == "Mine" && mine == null)
                {
                    // Found the object with the desired name
                    // You can now store its reference or perform any other actions
                    mine = obj.GetComponent<BoplBody>();
                    UnityEngine.Debug.Log("Found the object: " + obj.name);
                    numObjectsFound++;
                    if (numObjectsFound == numObjectsToFind)
                    {
                        break;
                    }
                }
                if (obj.name == "BlackHole2" && blackHole == null)
                {
                    // Found the object with the desired name
                    // You can now store its reference or perform any other actions
                    blackHole = obj.GetComponent<BlackHole>();
                    UnityEngine.Debug.Log("Found the object: " + obj.name);
                    numObjectsFound++;
                    if (numObjectsFound == numObjectsToFind)
                    {
                        break;
                    }
                }
                if (obj.name == "AbilityPickup_Dynamic" && AbilityPickup == null)
                {
                    // Found the object with the desired name
                    // You can now store its reference or perform any other actions
                    UnityEngine.Debug.Log("Found the object: " + obj.name);
                    AbilityPickup = obj.GetComponent<DynamicAbilityPickup>();
                    numObjectsFound++;
                    if (numObjectsFound == numObjectsToFind)
                    {
                        break;
                    }
                }
                if (obj.name == "Smoke" && SmokeGrenade == null)
                {
                    // Found the object with the desired name
                    // You can now store its reference or perform any other actions
                    UnityEngine.Debug.Log("Found the object: " + obj.name);
                    SmokeGrenade = obj.GetComponent<ThrowItem2>().ItemPrefab;
                    numObjectsFound++;
                    if (numObjectsFound == numObjectsToFind)
                    {
                        break;
                    }
                }
                if (obj.name == "Spark" && (MissleExplosion == null || missile == null))
                {
                    // Found the object with the desired name
                    // You can now store its reference or perform any other actions
                    UnityEngine.Debug.Log("Found the object: " + obj.name);
                    MissleExplosion = obj.GetComponent<Missile>().onHitExplosionPrefab;
                    missile = obj.GetComponent<BoplBody>();
                    numObjectsFound+=2;
                    if (numObjectsFound == numObjectsToFind)
                    {
                        break;
                    }
                }
                if (obj.name == "SpikeAttack" && spikePrefab == null)
                {
                    // Found the object with the desired name
                    // You can now store its reference or perform any other actions
                    UnityEngine.Debug.Log("Found the object: " + obj.name);
                    spikePrefab = obj.GetComponent<SpikeAttack>();
                    numObjectsFound++;
                    if (numObjectsFound == numObjectsToFind)
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
            BlackHole,
            Mine,
            Missile,
            Text
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
        // NOT TO BE CONFUSED WITH SpawnSpike(percentAroundSurface)! SpawnSpikeAtPosition(x, y) is a much more "advanced" function.
        // I don't want to just delete it from the API completely because I think it could be useful to somebody, but I imagine it's less useful in most cases.
        public static BoplBody SpawnSpikeAtPosition(Fix surfacePosX, Fix surfacePosY, Fix offset, StickyRoundedRectangle attachedGround, Fix scale)
        {
            var spikeObj = FixTransform.InstantiateFixed<SpikeAttack>(spikePrefab, new Vec2(surfacePosX, surfacePosY));

            // looking at the game's `Spike` script, in the CastSpike() function.
            DPhysicsRoundedRect groundRect = attachedGround.GetComponent<DPhysicsRoundedRect>();
            FixTransform SpikeFixTrans = spikeObj.GetComponent<FixTransform>();
            SpikeFixTrans.transform.SetParent(groundRect.transform);

            spikeObj.Initialize(new Vec2(surfacePosX, surfacePosY), offset, attachedGround, scale, true);

            attachedGround.alignRotation(spikeObj.hitbox.body);
            spikeObj.UpdateRelativeOrientation();

            spikeObj.groundOrientationAtCastTime = spikeObj.groundOrientationAtCastTime + spikeObj.hitbox.GetBody().relativeOrientation + Fix.Pi;
            spikeObj.UpdateRelativeOrientation();

            return spikeObj.hitbox.body;
        }
        public static BoplBody SpawnSpike(Fix percentAroundSurface, Fix offset, StickyRoundedRectangle attachedGround, Fix scale)
        {
            // for some reason a value of 1 doesn't loop back to 0 and instead just goes in the middle of the wrong side
            // similar thing for 0 not going to the correct place,
            // so percentAroundSurface has to be manually clamped to the range of (val above 0 but low as possible) to (val below 1 but high as possible)
            // there's also some weird positioning going on with 0.5. I guess this is a bopl bug with platform zones.
            // So I guess I'll just add 
            if (percentAroundSurface >= 1)
            {
                percentAroundSurface = (Fix)(1 - Fix.Precision);
            }
            else
            {
                // Fix.Precision for some reason isn't low enough, at least for 0
                percentAroundSurface += (Fix)(Fix.Precision * 2);
            }


            var spikePos = attachedGround.PositionFromLocalPlayerPos(percentAroundSurface, (Fix)1);
            var spikeObj = FixTransform.InstantiateFixed<SpikeAttack>(spikePrefab, new Vec2(spikePos.x, spikePos.y));

            // looking at the game's `Spike` script, in the CastSpike() function.
            DPhysicsRoundedRect groundRect = attachedGround.GetComponent<DPhysicsRoundedRect>();
            FixTransform SpikeFixTrans = spikeObj.GetComponent<FixTransform>();
            SpikeFixTrans.transform.SetParent(groundRect.transform);

            spikeObj.Initialize(new Vec2(spikePos.x, spikePos.y), offset, attachedGround, scale, true);

            attachedGround.alignRotation(spikeObj.hitbox.body);
            spikeObj.UpdateRelativeOrientation();

            spikeObj.groundOrientationAtCastTime = spikeObj.groundOrientationAtCastTime + spikeObj.hitbox.GetBody().relativeOrientation + Fix.Pi;
            spikeObj.UpdateRelativeOrientation();


            return spikeObj.hitbox.body;
        }

        public static BoplBody SpawnMine(Vec2 pos, Fix scale, Vec2 StartVel, Fix chaseRadius, bool chase)
        {
            BoplBody boplBody = FixTransform.InstantiateFixed<BoplBody>(mine, pos);
            boplBody.Scale = scale;
            boplBody.StartVelocity = StartVel;
            Mine mineObj = boplBody.GetComponent<Mine>();
            mineObj.item.OwnerId = 256;
            if (!chase) mineObj.item.OwnerId = 255;
            mineObj.SetMaterial(WhiteSlimeMat);
            mineObj.ScansFor = 256;
            mineObj.scanRadius = chaseRadius;

            return boplBody;
        }

        public static BoplBody SpawnMissile(Vec2 pos, Fix scale, Vec2 StartVel, Color color)
        {
            BoplBody boplBody = FixTransform.InstantiateFixed<BoplBody>(missile, pos);
            boplBody.Scale = scale;
            boplBody.StartVelocity = StartVel;
            boplBody.fixTrans.rotation = -CalculateAngle(StartVel);
            boplBody.GetComponent<SpriteRenderer>().material = WhiteSlimeMat;
            boplBody.GetComponent<SpriteRenderer>().color = color;
            //boplBody.rotation = CalculateAngle(StartVel);

            return boplBody;
        }

        public static BlackHole SpawnBlackHole(Vec2 pos, Fix size)
        {
            BlackHole blackHole2 = FixTransform.InstantiateFixed<BlackHole>(blackHole, pos);
            blackHole2.GrowIncrementally(size - Fix.One);
            return blackHole2;
        }

        public static TextMeshPro SpawnText(Vec2 pos, Fix rotation, Fix scale, string contents, Color color)
        {
            TextMeshPro text = new GameObject(contents, typeof(RectTransform), typeof(MeshRenderer), typeof(CanvasRenderer), typeof(MeshFilter), typeof(TextMeshPro), typeof(TMP_SpriteAnimator)).GetComponent<TextMeshPro>();
            text.text = contents;
            text.color = color;
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;

            text.rectTransform.position = new Vector3((float)pos.x, (float)pos.y, 0);
            text.rectTransform.rotation = Quaternion.Euler(0, 0, (float)rotation);
            text.rectTransform.localScale = Vector3.one * (float)scale;
            Vector2 preferredSize = text.GetPreferredValues();
            text.rectTransform.sizeDelta = preferredSize;
            return text;
        }

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
        public static BoplBody SpawnGrenade(Vec2 pos, Fix angle, Fix scale, Vec2 StartVel, Fix StartAngularVelocity, Fix FuseSeconds)
        {
            BoplBody boplBody = FixTransform.InstantiateFixed<BoplBody>(grenade, pos, angle);
            boplBody.Scale = scale;
            boplBody.StartVelocity = StartVel;
            boplBody.rotation = angle;
            boplBody.StartAngularVelocity = StartAngularVelocity;
            Item component = boplBody.GetComponent<Item>();
            component.OwnerId = 255;
            var Grenade = component.GetComponent<Grenade>();

            if (FuseSeconds != (Fix)(-1))
            {
                Grenade.timedExplosion = true;
                Grenade.selfDestructDelay = FuseSeconds;
                Grenade.detonationTime = FuseSeconds;
            }

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
