using BepInEx;
using BepInEx.Logging;
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
using BepInEx.Configuration;
using System.IO.Compression;
using System.Runtime.Remoting.Contexts;
using System.Text;
using UnityEngine.UI;

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
        public static Fix OneByOneBlockMass = Fix.One;
        public static string[] MapJsons;
        // Define a static logger instance
        public static ManualLogSource logger;
        public static bool UseCustomTexture = false;
        public static string CustomTextureName;
        //all the zipArchives in the same order as the MapJsons
        public static ZipArchive[] zipArchives = { };
        public static Sprite sprite;
        public static Material PlatformMat;
        public enum MapIdCheckerThing
        {
            MapFoundWithId,
            NoMapFoundWithId,
            MultipleMapsFoundWithId
        }
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
                Debug.Log("Maps folder created.");
            }
            
            
        }
        public void Start()
        {
            //fill the MapJsons array up
            ZipArchive[] zipArchives = GetZipArchives();
            //Create a List for the json for a bit
            List<string> JsonList = new List<string>();
            foreach (ZipArchive zipArchive in zipArchives)
            {
                //get the first .boplmap file if there is multiple. (THERE SHOULD NEVER BE MULTIPLE .boplmap's IN ONE .zip)
                JsonList.Add(GetFileFromZipArchive(zipArchive, IsBoplMap)[0]);
            }
            MapJsons = JsonList.ToArray();
        }
        public static bool IsBoplMap(string path)
        {
            if (path.EndsWith("boplmap")) return true;
            //will only be reached if its not a boplmap
            return false;
        }
        //see if there is a custom map we should load (returns enum) (david) (this was annoying to make but at least i learned about predicits!)
        public static MapIdCheckerThing CheckIfWeHaveCustomMapWithMapId()
        {
            int[] MapIds = {};
            foreach (string mapJson in MapJsons)
            {
                try
                {
                    Dictionary<string, object> Dict = MiniJSON.Json.Deserialize(mapJson) as Dictionary<string, object>;
                    //add it to a array to be checked
                    int mapid = int.Parse((string)Dict["mapId"]);
                    Debug.Log("Map has Mapid of " +  mapid);
                    MapIds = MapIds.Append(mapid).ToArray();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to get MapId from Json: {mapJson} with exseptson: {ex}" );
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
            var i = 0;
            foreach (string mapJson in MapJsons)
            {
                try
                {
                    Dictionary<string, object> Dict = MiniJSON.Json.Deserialize(mapJson) as Dictionary<string, object>;
                    if (int.Parse((string)Dict["mapId"]) == CurrentMapId)
                    {
                        SpawnPlatformsFromMap(mapJson, i);
                    }
                    
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load map from json: {mapJson} Error: {ex.Message}");
                }
                i++;
            }
        }

        public static void SpawnPlatformsFromMap(string mapJson, int index)
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
                sprite = null;
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
                    bool UseCustomMass = false;
                    float Red = 1;
                    float Green = 1;
                    float Blue = 1;
                    float Opacity = 1;
                    Vector4 color;
                    //reset UseCustomTexture so the value for 1 platform doesnt blead trough to anouter
                    UseCustomTexture = false;
                    Fix Mass = (Fix)0;

                    //defult to 0 rotatson incase the json is missing it
                    double rotatson = 0;
                    if (platform.ContainsKey("rotation"))
                    { 
                        rotatson = ConvertToRadians((double)platform["rotation"]);
                    }
                    //custom mass
                    if (platform.ContainsKey("UseCustomMass"))
                    {
                        UseCustomMass = (bool)platform["UseCustomMass"];
                    }
                    if (platform.ContainsKey("CustomMass") && UseCustomMass)
                    {
                        Mass = (Fix)Convert.ToDouble(platform["CustomMass"]);
                    }
                    else
                    {
                        Mass = CalculateMassOfPlatform((Fix)width, (Fix)height, (Fix)radius);
                    }
                    //custom Texture 
                    if (platform.ContainsKey("UseCustomTexture") && platform.ContainsKey("CustomTextureName") && platform.ContainsKey("PixelsPerUnit"))
                    {
                        UseCustomTexture = (bool)platform["UseCustomTexture"];
                    }
                    Debug.Log($"UseCustomTexture is {UseCustomTexture}");
                    if (UseCustomTexture)
                    {
                        float PixelsPerUnit = (float)Convert.ToDouble(platform["PixelsPerUnit"]);
                        CustomTextureName = (String)platform["CustomTextureName"];
                        Debug.Log(CustomTextureName);
                        //doesnt work if there are multiple files ending with the file name
                        //TODO: make it so that if a sprite for it with the pramiters alredy exsits use that. as creating a sprite from raw data is costly
                        Byte[] filedata;
                        Byte[][] filedatas = GetFileFromZipArchiveBytes(zipArchives[index], IsCustomTexture);
                        if (filedatas.Length > 0)
                        {
                            filedata = filedatas[0];
                            Debug.Log($"filedata is {filedata}");
                            sprite = IMG2Sprite.LoadNewSprite(filedata, PixelsPerUnit);
                            Debug.Log($"sprite is {sprite}");
                        }
                        else
                        {
                            logger.LogError($"ERROR NO FILE NAMED {CustomTextureName}");
                            Debug.LogError($"ERROR NO FILE NAMED {CustomTextureName}");
                            return;
                        }

                    }
                    if (platform.ContainsKey("Red"))
                    {
                        Red = (float)Convert.ToDouble(platform["Red"]);
                    }
                    if (platform.ContainsKey("Green"))
                    {
                        Green = (float)Convert.ToDouble(platform["Green"]);
                    }
                    if (platform.ContainsKey("Blue"))
                    {
                        Blue = (float)Convert.ToDouble(platform["Blue"]);
                    }
                    if (platform.ContainsKey("Opacity"))
                    {
                        Opacity = (float)Convert.ToDouble(platform["Opacity"]);
                    }
                    color = new Vector4(Red, Green, Blue, Opacity);
                    // Spawn platform
                    if (!UseCustomTexture)
                    {
                        SpawnPlatform((Fix)x, (Fix)y, (Fix)width, (Fix)height, (Fix)radius, (Fix)rotatson, Mass, color);
                    }
                    else
                    {

                        SpawnPlatform((Fix)x, (Fix)y, (Fix)width, (Fix)height, (Fix)radius, (Fix)rotatson, Mass, sprite, color);
                    }
                    
                    Debug.Log("Platform spawned successfully");
                }
                catch (Exception ex)
                {   
                    Debug.LogError($"Failed to spawn platform. Error: {ex.Message}");
                }
            }
        }
        public static bool IsCustomTexture(string textureName)
        {
            return textureName.EndsWith(CustomTextureName);
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
                    Debug.LogError($"ERROR! MULTIPLE MAPS WITH MAP ID: {CurrentMapId} FOUND! UHAFYIGGAFYAIO");
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
                    //steal matual
                    PlatformMat = tplatform.gameObject.GetComponent<SpriteRenderer>().material;
                    Updater.DestroyFix(tplatform.gameObject);
                }
                LoadMapsFromFolder();
            }
        }
        //no sprite
        public static void SpawnPlatform(Fix X, Fix Y, Fix Width, Fix Height, Fix Radius, Fix rotatson, Fix mass, Vector4 color)
        {
            // Spawn platform (david - and now melon)
            var StickyRect = FixTransform.InstantiateFixed<StickyRoundedRectangle>(platformPrefab, new Vec2(X, Y));
            StickyRect.rr.Scale = Fix.One;
            var platform = StickyRect.GetComponent<ResizablePlatform>();
            platform.GetComponent<DPhysicsRoundedRect>().ManualInit();
            ResizePlatform(platform, Width, Height, Radius);
            //45 degrees
            StickyRect.GetGroundBody().up = new Vec2(rotatson);
            AccessTools.Field(typeof(BoplBody), "mass").SetValue(StickyRect.GetGroundBody(), mass);
            //color
            SpriteRenderer spriteRenderer = (SpriteRenderer)AccessTools.Field(typeof(StickyRoundedRectangle), "spriteRen").GetValue(StickyRect);
            spriteRenderer.color = color;
            Debug.Log("Spawned platform at position (" + X + ", " + Y + ") with dimensions (" + Width + ", " + Height + ") and radius " + Radius);
        }
        //with sprite
        public static void SpawnPlatform(Fix X, Fix Y, Fix Width, Fix Height, Fix Radius, Fix rotatson, Fix mass, Sprite sprite, Vector4 color)
        {
            // Spawn platform (david - and now melon)
            var StickyRect = FixTransform.InstantiateFixed<StickyRoundedRectangle>(platformPrefab, new Vec2(X, Y));
            StickyRect.rr.Scale = Fix.One;
            var platform = StickyRect.GetComponent<ResizablePlatform>();
            platform.GetComponent<DPhysicsRoundedRect>().ManualInit();
            ResizePlatform(platform, Width, Height, Radius);
            //45 degrees
            StickyRect.GetGroundBody().up = new Vec2(rotatson);
            AccessTools.Field(typeof(BoplBody), "mass").SetValue(StickyRect.GetGroundBody(), mass);
            SpriteRenderer spriteRenderer = (SpriteRenderer)AccessTools.Field(typeof(StickyRoundedRectangle), "spriteRen").GetValue(StickyRect);
            //TODO remove sprite object on scene change
            spriteRenderer.sprite = sprite;
            spriteRenderer.material = PlatformMat;
            spriteRenderer.color = color;
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

        public static Fix CalculateMassOfPlatform(Fix Width, Fix Height, Fix Radius)
        {
            //multiply by 2 because Width and Height are just distances from the center 
            var TrueWidth = Width * (Fix)2 + Radius;
            var TrueHeight = Height * (Fix)2 + Radius;
            var Area = TrueWidth * TrueHeight;
            //if it is a circle
            if (Width == (Fix)0.05 && Height == (Fix)0.05)
            {
                //A=Pi*R^2
                //there is no exsponent for Fixes
                Area = Fix.Pi * Radius * Radius;
            }
            return Area * OneByOneBlockMass;

        }
        //in part chatgpt code
        public static ZipArchive UnzipFile(string zipFilePath)
        {

            // Open the zip file for reading
            FileStream zipStream = new FileStream(zipFilePath, FileMode.Open);
            ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            
                // Iterate through each entry in the zip file
                /*foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    // If entry is a directory, skip it
                    if (entry.FullName.EndsWith("/"))
                        continue;

                    // Read the contents of the entry
                    using (StreamReader reader = new StreamReader(entry.Open()))
                    {
                        string contents = reader.ReadToEnd();
                        Console.WriteLine($"Contents of {entry.FullName}:");
                        Console.WriteLine(contents);
                    }
                }*/
                return archive;
            
        }
        //finds all the files with a path that the predicate acsepts as a string array 
        public static string[] GetFileFromZipArchive(ZipArchive archive, Predicate<string> predicate)
        {
            Debug.Log("enter GetFileFromZipArchive");
            string[] data = { };
            // Iterate through each entry in the zip file
            //archive is disposed of at this point for some reson
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                // If entry is a directory, skip it
                Debug.Log("check if its a drectory");
                if (entry.FullName.EndsWith("/"))
                    continue;
                Debug.Log("it isnt a drectory");
                //see if it is valid (if the predicate returns true)
                string[] path = { entry.FullName };
                string[] ValidPathArray = Array.FindAll(path, predicate);
                if (ValidPathArray.Length != 0)
                {
                    Debug.Log("about to read the contents of entry");
                    // Read the contents of the entry
                    using (StreamReader reader = new StreamReader(entry.Open()))
                    {
                        Debug.Log("reading the contents of entry");
                        string contents = reader.ReadToEnd();
                        //add the contents to data
                        data = data.Append(contents).ToArray();
                    }
                }
            }
            return data;
        }
        public static Byte[][] GetFileFromZipArchiveBytes(ZipArchive archive, Predicate<string> predicate)
        {
            Debug.Log("enter GetFileFromZipArchive");
            Byte[][] data = { };
            // Iterate through each entry in the zip file
            //archive is disposed of at this point for some reson
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                // If entry is a directory, skip it
                Debug.Log("check if its a drectory");
                if (entry.FullName.EndsWith("/"))
                    continue;
                Debug.Log("it isnt a drectory");
                //see if it is valid (if the predicate returns true)
                string[] path = { entry.FullName };
                string[] ValidPathArray = Array.FindAll(path, predicate);
                if (ValidPathArray.Length != 0)
                {
                    Debug.Log("about to read the contents of entry");
                    // Read the contents of the entry
                    using (var entryStream = entry.Open())
                    using (var memoryStream = new MemoryStream())
                    {
                        Debug.Log("reading the contents of entry");
                        entryStream.CopyTo(memoryStream);
                        //add the contents to data
                        data = data.Append(memoryStream.ToArray()).ToArray();
                    }
                }
            }
            return data;
        }


        //gets all of the .zip files from the maps folder and turns them into a array of ZipArchive's (david) ONLY CALL ON START!
        public static ZipArchive[] GetZipArchives()
        {
            string[] MapZipFiles = Directory.GetFiles(mapsFolderPath, "*.zip");
            Debug.Log($"{MapZipFiles.Length} .zip's");
            foreach (string zipFile in MapZipFiles)
            {
                zipArchives = zipArchives.Append(UnzipFile(zipFile)).ToArray();

            }
            Debug.Log($"zipArchivesLength is {zipArchives.Length}");
            return zipArchives;

        }
    }
}
