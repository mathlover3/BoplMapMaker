using BepInEx;
using BoplFixedMath;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using MonoMod.Utils;
using System.IO;
using MiniJSON;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;
using System.Linq;
using System.Drawing;

namespace MapMaker
{
    [BepInPlugin("com.MLT.MapLoader", "MapLoader", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static GameObject PlatformAbility;
        public static Transform levelt;
        public static StickyRoundedRectangle platformPrefab;
        public static List<ResizablePlatform> Platforms;
        public static int t;
        public static string mapsFolderPath; // Create blank folder path var
        public static int CurrentMapId;
        private void Awake()
        {
            Logger.LogInfo("MapLoader Has been loaded");
            Harmony harmony = new Harmony("com.MLT.MapLoader");

            Logger.LogInfo("Harmony harmony = new Harmony -- Melon, 2024");
            harmony.PatchAll(); // Patch Harmony
            Logger.LogInfo("MapMaker Patch Compleate!");

            SceneManager.sceneLoaded += OnSceneLoaded;

            mapsFolderPath = Path.Combine(Paths.PluginPath, "Maps");

            if (!Directory.Exists(mapsFolderPath))
            {
                Directory.CreateDirectory(mapsFolderPath);
                Logger.LogInfo("Maps folder created.");
            }
        }
        //see if there is a custom map we should load (returns enum) (david) (this was annoying to make but at least i learned about predicits!)
        public static MapIdCheckerThing CheckIfWeHaveCustomMapWithMapId()
        {
            string[] mapFiles = Directory.GetFiles(mapsFolderPath, "*.boplmap");
            int[] MapIds = {};
            foreach (string mapFile in mapFiles)
            {
                try
                {
                    string mapJson = File.ReadAllText(mapFile);
                    Debug.Log($"Loaded map from file: {Path.GetFileName(mapFile)} to check if it has map id {CurrentMapId}");
                    Dictionary<string, object> Dict = MiniJSON.Json.Deserialize(mapJson) as Dictionary<string, object>;
                    //add it to a array to be checked
                    int mapid = int.Parse((string)Dict["mapId"]);
                    Debug.Log("Map has Mapid of " +  mapid);
                    MapIds = MapIds.Append(mapid).ToArray();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to get MapId from file: {Path.GetFileName(mapFile)}. Error: {ex.Message}");
                }
            }
            //define a predicit (basicly a funcsion that checks if a value meets a critera. in this case being = to CurrentMapId)
            Predicate<int> predicate = ValueEqualsCurrentMapId;
            //get a list of map ids that match the current map
            int[] ValidMapIds = Array.FindAll(MapIds, predicate);
            Debug.Log("MapIds: " + MapIds.Length + " ValidMapIds: " + ValidMapIds.Length);
            if (ValidMapIds.Length > 0)
            {
                if (ValidMapIds.Length > 1)
                {
                    return MapIdCheckerThing.MultipleMapsFoundWithId;
                }
                else
                {
                    return MapIdCheckerThing.MapFoundWithId;
                }
            }
            else return MapIdCheckerThing.NoMapFoundWithId;


        }
        //check if value = CurrentMapId. used for CheckIfWeHaveCustomMapWithMapId
        public static bool ValueEqualsCurrentMapId(int ValueToCheck)
        {
            if (ValueToCheck == CurrentMapId)
            { 
                return true; 
            }
            else 
            {
                return false; 
            }
        }
        //CALL ONLY ON LEVEL LOAD!
        public static void LoadMapsFromFolder()
        {
            string[] mapFiles = Directory.GetFiles(mapsFolderPath, "*.boplmap");
            foreach (string mapFile in mapFiles)
            {
                try
                {
                    string mapJson = File.ReadAllText(mapFile);
                    Debug.Log($"Loaded map from file: {Path.GetFileName(mapFile)}");
                    Dictionary<string, object> Dict = MiniJSON.Json.Deserialize(mapJson) as Dictionary<string, object>;
                    if (int.Parse((string)Dict["mapId"]) == CurrentMapId)
                    {
                        SpawnPlatformsFromMap(mapJson);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load map from file: {Path.GetFileName(mapFile)}. Error: {ex.Message}");
                }
            }
        }

        public static void SpawnPlatformsFromMap(string mapJson)
        {
            //get the platform prefab out of the Platform ability gameobject (david) DO NOT REMOVE!
            //chatgpt code to get the Platform ability object
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
            Debug.Log("getting platform object");
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "Platform")
                {
                    // Found the object with the desired name and HideAndDontSave flag
                    // You can now store its reference or perform any other actions
                    PlatformAbility = obj;
                    Debug.Log("Found the object: " + obj.name);
                    break;
                }
            }
            var platformTransform = PlatformAbility.GetComponent(typeof(PlatformTransform)) as PlatformTransform;
            platformPrefab = platformTransform.platformPrefab;
            //turn the json into a dicsanary. (david+chatgpt) dont remove it as it works.
            Dictionary<string, object> Dict = MiniJSON.Json.Deserialize(mapJson) as Dictionary<string, object>;
            List<object> platforms = (List<object>)Dict["platforms"];
            Debug.Log("platforms set");
            foreach (Dictionary<String, object> platform in platforms)
            {
                try
                {
                    // Extract platform data (david)
                    Dictionary<string, object> transform = (Dictionary<string, object>)platform["transform"];
                    Dictionary<string, object> size = (Dictionary<string, object>)platform["size"];

                    //doesnt work if any of them is a int for some reson? invalid cast error. // PLEASE USE TO CODE TO MAKE IT WORK!
                    double x = Convert.ToDouble(transform["x"]);
                    double y = Convert.ToDouble(transform["y"]);
                    double width = Convert.ToDouble(size["width"]);
                    double height = Convert.ToDouble(size["height"]);
                    double radius = Convert.ToDouble(platform["radius"]);

                    //defult to 0 rotatson incase the json is missing it
                    double rotatson = 0;
                    if (platform.ContainsKey("rotation"))
                    { 
                        rotatson = ConvertToRadians((double)platform["rotation"]);
                    }
                    // Spawn platform
                    SpawnPlatform((Fix)x, (Fix)y, (Fix)width, (Fix)height, (Fix)radius, (Fix)rotatson);
                    Debug.Log("Platform spawned successfully");
                }
                catch (Exception ex)
                {   
                    Debug.LogError($"Failed to spawn platform. Error: {ex.Message}");
                }
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("OnSceneLoaded: " + scene.name);
            if (IsLevelName(scene.name)) // TODO: Check level, Replace with mapId from MapMaker Thing
            {
                CurrentMapId = GetMapIdFromSceneName(scene.name);
                var DoWeHaveMapWithMapId = CheckIfWeHaveCustomMapWithMapId();
                //error if there are multiple maps with the same id
                if (DoWeHaveMapWithMapId == MapIdCheckerThing.MultipleMapsFoundWithId)
                {
                    Debug.LogError("ERROR! MULTIPLE MAPS WITH THE GIVEM MAP ID FOUND! UHAFYIGGAFYAIO");
                    return;
                }
                else
                {
                    if (DoWeHaveMapWithMapId == MapIdCheckerThing.NoMapFoundWithId)
                    {
                        Debug.Log("no custom map found for this map");
                        return;
                    }
                }
                //find the platforms and remove them (shadow + david)
                levelt = GameObject.Find("Level").transform;
                foreach (Transform tplatform in levelt)
                {
                    Updater.DestroyFix(tplatform.gameObject);
                }
                LoadMapsFromFolder();
            }
        }

        public static void SpawnPlatform(Fix X, Fix Y, Fix Width, Fix Height, Fix Radius, Fix rotatson)
        {
            // Spawn platform (david - and now melon)
            var StickyRect = FixTransform.InstantiateFixed<StickyRoundedRectangle>(platformPrefab, new Vec2(X, Y));
            StickyRect.rr.Scale = Fix.One;
            var platform = StickyRect.GetComponent<ResizablePlatform>();
            platform.GetComponent<DPhysicsRoundedRect>().ManualInit();
            ResizePlatform(platform, Width, Height, Radius);
            //45 degrees
            StickyRect.GetGroundBody().up = new Vec2(rotatson);
            Debug.Log("Spawned platform at position (" + X + ", " + Y + ") with dimensions (" + Width + ", " + Height + ") and radius " + Radius);
        }

        public static void Update()
        {
            //ignore this its broken
            //if (Platforms.Count > 0)
            //{
            //    ResizePlatform(Platforms[0], (Fix)0.1, (Fix)0.1, (Fix)(5 + t * 0.05));
            //    t++;
            //}
        }

        //this can be called anytime the object is active. this means you can have animated levels with shape changing platforms
        public static void ResizePlatform(ResizablePlatform platform, Fix newWidth, Fix newHeight, Fix newRadius)
        {
            platform.ResizePlatform(newHeight, newWidth, newRadius, true);
        }
        public static bool IsLevelName(String input)
        {
            Regex regex = new Regex("Level[0-9]+", RegexOptions.IgnoreCase);
            return regex.IsMatch(input);
        }
        //https://stormconsultancy.co.uk/blog/storm-news/convert-an-angle-in-degrees-to-radians-in-c/
        public static double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }
        //https://stackoverflow.com/questions/19167669/keep-only-numeric-value-from-a-string
        // simply replace the offending substrings with an empty string
        public static int GetMapIdFromSceneName(string s)
        {
            Regex rxNonDigits = new Regex(@"[^\d]+");
            if (string.IsNullOrEmpty(s)) return 0;
            string cleaned = rxNonDigits.Replace(s, "");
            //subtract 1 as scene names start with 1 but ids start with 0
            return int.Parse(cleaned)-1;
        }
        public enum MapIdCheckerThing
        {
            MapFoundWithId,
            NoMapFoundWithId,
            MultipleMapsFoundWithId
        }

    }
}
