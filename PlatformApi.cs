using BoplFixedMath;
using HarmonyLib;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MapMaker
{
    public class PlatformApi
    {
        private static StickyRoundedRectangle platformPrefab;
        private static Object SlimeCamObject;
        public static Material PlatformMat;
        public static Logger Logger = new Logger();
        public enum PathType
        {
            None,
            AntiLockPlatform,
            VectorFieldPlatform
        }
        public void Awake()
        {
            
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("Awake");
            //get the platform prefab out of the Platform ability gameobject (david) DO NOT REMOVE!
            //chatgpt code to get the Platform ability object
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
            Debug.Log("getting platform object");
            GameObject PlatformAbility = null;
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "Platform")
                {
                    //store its reference
                    PlatformAbility = obj;
                    Debug.Log("Found the Platform object");
                    break;
                }
            }
            if (PlatformAbility)
            {
                var platformTransform = PlatformAbility.GetComponent(typeof(PlatformTransform)) as PlatformTransform;
                platformPrefab = platformTransform.platformPrefab;
            }
            // TODO add this here when its a seprite plugin.
            //thanks almafa64 on discord for the path stuff.
            /*
            MyAssetBundle = AssetBundle.LoadFromFile(Path.GetDirectoryName(Info.Location) + "/assetbundle");
            //load the slime cam for use in spawning platforms with slimecam
            SlimeCamObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/slimetrailcam.prefab");
            */
        }
        //if you are spawning platforms in OnSceneLoaded in your plugin call it manualy at the start of OnSceneLoaded in your plugin. 
        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsLevelName(scene.name))
            {
                Debug.Log("OnSceneLoaded");
                //TODO remove this part when seperating
                SlimeCamObject = Plugin.SlimeCamObject;
                //GetChild(int index);
                var level = GameObject.Find("Level");
                if (level)
                {
                    var plat = level.transform.GetChild(0);
                    if (plat)
                    {
                        PlatformMat = plat.gameObject.GetComponent<SpriteRenderer>().material;
                        Debug.Log("mat is " + PlatformMat + " and its name is " + PlatformMat.name);
                    }
                    else
                    {
                        Logger.LogWarning("tag?", "Couldnt Find Platfrom to steal Platform Mat from. this can happen if you remove all platforms on scene load. pls manualy steal a platfrom mat and set PlatformApi.PlatformMat to it.");
                        Debug.Log("Couldnt Find Platfrom to steal Platform Mat from. this can happen if you remove all platforms on scene load. pls manualy steal a platfrom mat and set PlatformApi.PlatformMat to it.");
                    }
                }
            }
        }
        public static bool IsLevelName(String input)
        {
            Regex regex = new Regex("Level[0-9]+", RegexOptions.IgnoreCase);
            return regex.IsMatch(input);
        }
        public static GameObject SpawnPlatform(Fix X, Fix Y, Fix Width, Fix Height, Fix Radius, Fix rotatson, double CustomMassScale = 0.05, Vector4[] color = null, PlatformType platformType = PlatformType.slime, bool UseSlimeCam = false, Sprite sprite = null, PathType pathType = PathType.None, double OrbitForce = 1, Vec2[] OrbitPath = null, double DelaySeconds = 1, bool isBird = false, double orbitSpeed = 100, double expandSpeed = 100, Vec2[] centerPoint = null, double normalSpeedFriction = 1, double DeadZoneDist = 1, double OrbitAccelerationMulitplier = 1, double targetRadius = 5, double ovalness01 = 1)
        {
            // Spawn platform (david - and now melon)
            var StickyRect = FixTransform.InstantiateFixed<StickyRoundedRectangle>(platformPrefab, new Vec2(X, Y));
            Debug.Log("platfromPrefab spawned");
            StickyRect.rr.Scale = Fix.One;
            var platform = StickyRect.GetComponent<ResizablePlatform>();
            platform.GetComponent<DPhysicsRoundedRect>().ManualInit();
            ResizePlatform(platform, Width, Height, Radius);
            //rotatson (in radiens)
            StickyRect.GetGroundBody().up = new Vec2(rotatson);
            //mass
            platform.MassPerArea = (Fix)CustomMassScale;
            Debug.Log("MassPerArea is " + platform.MassPerArea);
            SpriteRenderer spriteRenderer = (SpriteRenderer)AccessTools.Field(typeof(StickyRoundedRectangle), "spriteRen").GetValue(StickyRect);
            //TODO remove sprite object on scene change
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
                Debug.Log("platform mat is " + PlatformMat);
                spriteRenderer.material = PlatformMat;

            }
            if (color != null)
            {
                spriteRenderer.color = color[0];
            }

            //PlatformType
            StickyRect.platformType = platformType;
            //slime cam
            if (UseSlimeCam)
            {
                spriteRenderer.material = PlatformMat;

                var transform = StickyRect.transform;
                UnityEngine.Object.Instantiate(SlimeCamObject, transform);
                transform.gameObject.tag = "ground";
            }

            var ShakeablePlatform = platform.GetComponent<ShakablePlatform>();
            AccessTools.Field(typeof(ShakablePlatform), "originalMaterial").SetValue(ShakeablePlatform, spriteRenderer.material);
            //moving platform
            if (pathType == PathType.AntiLockPlatform)
            {
                //antilock platform
                var AntiLockPlatformComp = platform.gameObject.AddComponent(typeof(AntiLockPlatform)) as AntiLockPlatform;
                AntiLockPlatformComp.OrbitForce = (Fix)OrbitForce;
                AntiLockPlatformComp.OrbitPath = OrbitPath;
                AntiLockPlatformComp.DelaySeconds = (Fix)DelaySeconds;
                AntiLockPlatformComp.isBird = isBird;
            }
            if (pathType == PathType.VectorFieldPlatform)
            {
                var centerPointReal = Vec2.zero;
                if (centerPoint != null)
                {
                    centerPointReal = centerPoint[0];
                }
                var VectorFieldPlatformComp = platform.gameObject.AddComponent(typeof(VectorFieldPlatform)) as VectorFieldPlatform;
                VectorFieldPlatformComp.centerPoint = centerPointReal;
                VectorFieldPlatformComp.DeadZoneDist = (Fix)DeadZoneDist;
                VectorFieldPlatformComp.DelaySeconds = (Fix)DelaySeconds;
                VectorFieldPlatformComp.expandSpeed = (Fix)expandSpeed;
                VectorFieldPlatformComp.normalSpeedFriction = (Fix)normalSpeedFriction;
                VectorFieldPlatformComp.OrbitAccelerationMulitplier = (Fix)OrbitAccelerationMulitplier;
                VectorFieldPlatformComp.orbitSpeed = (Fix)orbitSpeed;
                VectorFieldPlatformComp.ovalness01 = (Fix)ovalness01;
            }
            Debug.Log("Spawned platform at position (" + X + ", " + Y + ") with dimensions (" + Width + ", " + Height + ") and radius " + Radius);
            return StickyRect.transform.gameObject;
        }
        //this can be called anytime the object is active. this means you can have animated levels with shape changing platforms
        public static void ResizePlatform(ResizablePlatform platform, Fix newWidth, Fix newHeight, Fix newRadius)
        {
            platform.ResizePlatform(newHeight, newWidth, newRadius, true);
        }
    }
}
