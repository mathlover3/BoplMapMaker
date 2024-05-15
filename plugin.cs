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
using System.Reflection.Emit;

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
        public static GameObject SlimeCamObject;
        public static List<Drill.PlatformColors> CustomDrillColors;
        public static List<NamedSprite> CustomMatchoManSprites;
        public static int NextPlatformTypeValue = 5;
        public const int StartingNextPlatformTypeValue = 5;
        public static Sprite BoulderSprite;
        //used to make CustomBoulderSmokeColors start with a value.
        internal static UnityEngine.Color[] ignore = {new UnityEngine.Color(1,1,1,1)};
        public static List<UnityEngine.Color> CustomBoulderSmokeColors = new List<UnityEngine.Color>(ignore);
        public static AssetBundle MyAssetBundle;
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
            //thanks almafa64 on discord for the path stuff.
            MyAssetBundle = AssetBundle.LoadFromFile(Path.GetDirectoryName(Info.Location) + "/assetbundle");
            string[] assetNames = MyAssetBundle.GetAllAssetNames();
            foreach (string name in assetNames)
            {
                Debug.Log("asset name is: " +  name);
            }
            //load the slime cam for use in spawning platforms with slimecam
            SlimeCamObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/slimetrailcam.prefab");
            
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
            //empty the list of Drill colors so the indexs start at 0 agien
            CustomDrillColors = new List<Drill.PlatformColors>();
            NextPlatformTypeValue = StartingNextPlatformTypeValue;
            CustomBoulderSmokeColors = new List<UnityEngine.Color>(ignore);
            CustomMatchoManSprites = new List<NamedSprite>();
            foreach (Dictionary<String, object> platform in platforms)
            {
                sprite = null;
                BoulderSprite = null;
                
                try
                {
                    //set optsonal values to null/0/defults 
                    Fix OrbitForce = Fix.Zero;
                    Vec2[] OrbitPath = null;
                    Fix DelaySeconds = Fix.Zero;
                    bool isBird = false;
                    Fix orbitSpeed = (Fix)100;
                    Fix expandSpeed = (Fix)100;
                    Vec2 centerPoint = new Vec2(Fix.Zero, Fix.Zero);
                    Fix normalSpeedFriction = (Fix)1;
                    Fix DeadZoneDist = (Fix)1;
                    Fix OrbitAccelerationMulitplier = (Fix)1;
                    Fix targetRadius = (Fix)5;
                    Fix ovalness01 = (Fix)1;
                    // Extract platform data (david)
                    Dictionary<string, object> transform = (Dictionary<string, object>)platform["transform"];
                    Dictionary<string, object> size = (Dictionary<string, object>)platform["size"];

                    //doesnt work if any of them is a int for some reson? invalid cast error. // PLEASE USE TO CODE TO MAKE IT WORK!
                    double x = Convert.ToDouble(transform["x"]);
                    double y = Convert.ToDouble(transform["y"]);
                    //defult to 0 rotatson incase the json is missing it
                    double rotatson = 0;
                    if (platform.ContainsKey("rotation"))
                    {
                        rotatson = ConvertToRadians(Convert.ToDouble(platform["rotation"]));
                    }
                    Debug.Log("getting IsPresetPatform");
                    bool IsPresetPatform = (bool)platform["IsPresetPatform"];
                    Debug.Log("IsPresetPatform is: " + IsPresetPatform);

                    //path stuff

                    PathType pathType = PathType.None;
                    if (platform.ContainsKey("PathType"))
                    {
                        if (Convert.ToString(platform["PathType"]) == "AntiLockPlatform")
                        {
                            pathType = PathType.AntiLockPlatform;
                        }
                        else if (Convert.ToString(platform["PathType"]) == "VectorFieldPlatform")
                        {
                            pathType = PathType.VectorFieldPlatform;
                        }
                    }
                    //AntiLockPlatform
                    if (pathType == PathType.AntiLockPlatform)
                    {
                        OrbitForce = (Fix)Convert.ToDouble(platform["OrbitForce"]);
                        //object time! (objects are so confusing)
                        //convert object to list of objects
                        List<System.Object> OrbitPathObjects = (List<System.Object>)platform["OrbitPath"];
                        //now to convert eatch object in the list to a list of 2 objects
                        List<Vec2> Vecs1 = new List<Vec2>();
                        for (int i = 0; i < OrbitPathObjects.Count; i++)
                        {
                            var obj = (List<System.Object>)OrbitPathObjects[i];
                            var floatList = ListOfObjectsToListOfFloats(obj);
                            var floatVec = new Vec2((Fix)floatList[0], (Fix)floatList[1]);
                            Vecs1.Add(floatVec);
                        }
                        Vec2[] Vecs = Vecs1.ToArray();
                        Debug.Log("orbit path decoded");

                        //now we have a Vec2 array for orbit path
                        OrbitPath = Vecs;
                        //the rest is easy
                        isBird = (bool)platform["isBird"];
                    }
                    //this is used in both types of paths
                    if (platform.ContainsKey("DelaySeconds"))
                    {
                        DelaySeconds = (Fix)Convert.ToDouble(platform["DelaySeconds"]);
                    }
                    if (platform.ContainsKey("expandSpeed"))
                    {
                        expandSpeed = (Fix)Convert.ToDouble(platform["expandSpeed"]);
                    }
                    if (platform.ContainsKey("centerPoint"))
                    {
                        var floats = ListOfObjectsToListOfFloats((List<object>)platform["centerPoint"]);
                        centerPoint = new Vec2((Fix)floats[0], (Fix)floats[1]);
                    }
                    if (platform.ContainsKey("normalSpeedFriction"))
                    {
                        normalSpeedFriction = (Fix)Convert.ToDouble(platform["normalSpeedFriction"]);
                    }
                    if (platform.ContainsKey("DeadZoneDist"))
                    {
                        DeadZoneDist = (Fix)Convert.ToDouble(platform["DeadZoneDist"]);
                    }
                    if (platform.ContainsKey("OrbitAccelerationMulitplier"))
                    {
                        OrbitAccelerationMulitplier = (Fix)Convert.ToDouble(platform["OrbitAccelerationMulitplier"]);
                    }
                    if (platform.ContainsKey("targetRadius"))
                    {
                        targetRadius = (Fix)Convert.ToDouble(platform["targetRadius"]);
                    }
                    if (platform.ContainsKey("ovalness01"))
                    {
                        ovalness01 = (Fix)Convert.ToDouble(platform["ovalness01"]);
                    }
                    //if its a preset platform dont do any of this.
                    if (!IsPresetPatform)
                    {
                        double width = Convert.ToDouble(size["width"]);
                        double height = Convert.ToDouble(size["height"]);
                        double radius = Convert.ToDouble(platform["radius"]);
                        bool UseCustomMass = false;
                        float Red = 1;
                        float Green = 1;
                        float Blue = 1;
                        float Opacity = 1;
                        bool circle = false;
                        bool UseCustomDrillColorAndBolderTexture = false;
                        bool UseSlimeCam = false;
                        PlatformType platformType = PlatformType.slime;
                        Vector4 color;
                        //reset UseCustomTexture so the value for 1 platform doesnt blead trough to anouter
                        UseCustomTexture = false;
                        Fix Mass = (Fix)0;


                        //custom mass
                        if (platform.ContainsKey("UseCustomMass"))
                        {
                            UseCustomMass = (bool)platform["UseCustomMass"];
                        }
                        if (platform.ContainsKey("CustomMass") && UseCustomMass)
                        {
                            Mass = (Fix)Convert.ToDouble(platform["CustomMass"]);
                        }
                        //is it a circle
                        if (platform.ContainsKey("shape"))
                        {
                            if (Convert.ToString(platform["shape"]) == "circle")
                            {
                                circle = true;
                            }
                        }
                        else
                        {
                            Mass = CalculateMassOfPlatform((Fix)width, (Fix)height, (Fix)radius, circle);
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
                        //color
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
                        //UseCustomDrillColorAndBolderTexture
                        if (platform.ContainsKey("UseCustomDrillColorAndBolderTexture"))
                        {
                            UseCustomDrillColorAndBolderTexture = (bool)platform["UseCustomDrillColorAndBolderTexture"];
                        }
                        if (UseCustomDrillColorAndBolderTexture)
                        {
                            //get drill colors dict to pass.
                            var dict = (Dictionary<string, object>)platform["CustomDrillColors"];
                            //if this platform fails to generate then the custom boulder texsters will get mixed up.
                            var MyPlatformId = NextPlatformTypeValue;
                            NextPlatformTypeValue = NextPlatformTypeValue + 1;
                            Debug.Log("creating drill colors");
                            var colors = DrillColors(MyPlatformId, dict);
                            Debug.Log("drill colors created");
                            platformType = (PlatformType)MyPlatformId;
                            CustomDrillColors.Add(colors);
                            //custom Boulder time
                            float PixelsPerUnit = (float)Convert.ToDouble(platform["BoulderPixelsPerUnit"]);
                            CustomTextureName = (String)platform["CustomBoulderTexture"];
                            //Debug.Log(CustomTextureName);
                            //doesnt work if there are multiple files ending with the file name
                            //TODO: make it so that if a sprite for it with the pramiters alredy exsits use that. as creating a sprite from raw data is costly
                            Byte[] filedata;
                            Byte[][] filedatas = GetFileFromZipArchiveBytes(zipArchives[index], IsCustomTexture);
                            if (filedatas.Length > 0)
                            {
                                filedata = filedatas[0];
                                Debug.Log($"filedata length is {filedata.Length}");
                                BoulderSprite = IMG2Sprite.LoadNewSprite(filedata, PixelsPerUnit);
                                Debug.Log($"sprite is {BoulderSprite}");
                                NamedSprite namedSprite = new NamedSprite(CustomTextureName, BoulderSprite, true);
                                Debug.Log("NamedSprite generated");
                                CustomMatchoManSprites.Add(namedSprite);
                                Debug.Log("Added NamedSprite to CustomMatchoManSprites");
                            }
                            else
                            {
                                logger.LogError($"ERROR NO FILE NAMED {CustomTextureName}");
                                Debug.LogError($"ERROR NO FILE NAMED {CustomTextureName}");
                                return;
                            }
                            var BoulderSmokeColorList = ListOfObjectsToListOfFloats((List<object>)platform["BoulderSmokeColor"]);
                            UnityEngine.Color BoulderSmokeColor = new UnityEngine.Color(BoulderSmokeColorList[0], BoulderSmokeColorList[1], BoulderSmokeColorList[2], BoulderSmokeColorList[3]);
                            CustomBoulderSmokeColors.Add(BoulderSmokeColor);
                        }
                        if (platform.ContainsKey("UseSlimeCam"))
                        {
                            UseSlimeCam = (bool)platform["UseSlimeCam"];
                        }
                        //spawn platform
                        SpawnPlatform((Fix)x, (Fix)y, (Fix)width, (Fix)height, (Fix)radius, (Fix)rotatson, Mass, color, platformType, UseSlimeCam, sprite, pathType, OrbitForce, OrbitPath, DelaySeconds, isBird, orbitSpeed, expandSpeed, centerPoint, normalSpeedFriction, DeadZoneDist, OrbitAccelerationMulitplier, targetRadius, ovalness01);

                        Debug.Log("Platform spawned successfully");
                    }
                    
                    // if it is a preset platform then we do it difrently
                    else
                    {
                        string PresetPlatformName = Convert.ToString(platform["PresetPlatformName"]);
                        var Platform = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/"+ PresetPlatformName + ".prefab");
                        Vector3 pos = new Vector3 ((float)x, (float)y, 0);
                        //fix shader
                        Platform.GetComponent<SpriteRenderer>().material = PlatformMat;
                        //scale object
                        //AddComponent(typeof(namespace.className)); 
                        var ScaleFactor = (Fix)Convert.ToDouble(platform["ScaleFactor"]);
                        var GrowOnStartComp = Platform.AddComponent(typeof(GrowOnStart)) as GrowOnStart;
                        GrowOnStartComp.scaleUp = ScaleFactor;
                        Debug.Log("added GrowOnStart");
                        //spawn object
                        Platform = UnityEngine.Object.Instantiate<GameObject>(Platform, pos, Quaternion.identity);
                        //rotate object
                        StickyRoundedRectangle StickyRect = Platform.GetComponent<StickyRoundedRectangle>();
                        StickyRect.GetGroundBody().rotation = (Fix)rotatson;
                        if (pathType == PathType.AntiLockPlatform)
                        {
                            //antilock platform
                            var AntiLockPlatformComp = Platform.AddComponent(typeof(AntiLockPlatform)) as AntiLockPlatform;
                            AntiLockPlatformComp.OrbitForce = OrbitForce;
                            AntiLockPlatformComp.OrbitPath = OrbitPath;
                            AntiLockPlatformComp.DelaySeconds = DelaySeconds;
                            AntiLockPlatformComp.isBird = isBird;
                        }
                    }

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
            if (IsLevelName(scene.name))
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
                var index = 0;
                foreach (Transform tplatform in levelt)
                {
                    //if its the first platform then steal some stuff from it before distroying it.
                    if (index == 0)
                    {
                        //steal matual 
                        PlatformMat = tplatform.gameObject.GetComponent<SpriteRenderer>().material;
                    }
                    index++;
                    //distroy it
                    Updater.DestroyFix(tplatform.gameObject);
                }
                LoadMapsFromFolder();
            }
        }
        //with sprite
        public static void SpawnPlatform(Fix X, Fix Y, Fix Width, Fix Height, Fix Radius, Fix rotatson, Fix mass, Vector4 color, PlatformType platformType, bool UseSlimeCam, Sprite sprite, PathType pathType, Fix OrbitForce, Vec2[] OrbitPath, Fix DelaySeconds, bool isBird,Fix orbitSpeed, Fix expandSpeed, Vec2 centerPoint, Fix normalSpeedFriction, Fix DeadZoneDist, Fix OrbitAccelerationMulitplier, Fix targetRadius, Fix ovalness01)
        {
            // Spawn platform (david - and now melon)
            var StickyRect = FixTransform.InstantiateFixed<StickyRoundedRectangle>(platformPrefab, new Vec2(X, Y));
            StickyRect.rr.Scale = Fix.One;
            var platform = StickyRect.GetComponent<ResizablePlatform>();
            platform.GetComponent<DPhysicsRoundedRect>().ManualInit();
            ResizePlatform(platform, Width, Height, Radius);
            //rotatson (in radiens)
            StickyRect.GetGroundBody().up = new Vec2(rotatson);
            AccessTools.Field(typeof(BoplBody), "mass").SetValue(StickyRect.GetGroundBody(), mass);
            SpriteRenderer spriteRenderer = (SpriteRenderer)AccessTools.Field(typeof(StickyRoundedRectangle), "spriteRen").GetValue(StickyRect);
            //TODO remove sprite object on scene change
            if (sprite != null)
            {
                spriteRenderer.sprite = sprite;
                spriteRenderer.material = PlatformMat;

            }
            spriteRenderer.color = color;
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
                AntiLockPlatformComp.OrbitForce = OrbitForce;
                AntiLockPlatformComp.OrbitPath = OrbitPath;
                AntiLockPlatformComp.DelaySeconds = DelaySeconds;
                AntiLockPlatformComp.isBird = isBird;
            }
            if (pathType == PathType.VectorFieldPlatform)
            {
                var VectorFieldPlatformComp = platform.gameObject.AddComponent(typeof(VectorFieldPlatform)) as VectorFieldPlatform;
                VectorFieldPlatformComp.centerPoint = centerPoint;
                VectorFieldPlatformComp.DeadZoneDist = DeadZoneDist;
                VectorFieldPlatformComp.DelaySeconds = DelaySeconds;
                VectorFieldPlatformComp.expandSpeed = expandSpeed;
                VectorFieldPlatformComp.normalSpeedFriction = normalSpeedFriction;
                VectorFieldPlatformComp.OrbitAccelerationMulitplier = OrbitAccelerationMulitplier;
                VectorFieldPlatformComp.orbitSpeed = orbitSpeed;
                VectorFieldPlatformComp.ovalness01 = ovalness01;
            }
            Debug.Log("Spawned platform at position (" + X + ", " + Y + ") with dimensions (" + Width + ", " + Height + ") and radius " + Radius);
        }

        public static void Update()
        {
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

        public static Fix CalculateMassOfPlatform(Fix Width, Fix Height, Fix Radius, bool circle)
        {
            //multiply by 2 because Width and Height are just distances from the center 
            var TrueWidth = Width * (Fix)2 + Radius;
            var TrueHeight = Height * (Fix)2 + Radius;
            var Area = TrueWidth * TrueHeight;
            //if it is a circle
            if (circle)
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
        //get the custom drill color from a dicsanry
        public static Drill.PlatformColors DrillColors(int PlatformType, Dictionary<string, object> dict)
        {
            var colors = new Drill.PlatformColors();
            //convert them to List<float> instead of object. (objects are so confusing)
            List<object> ColorDarkObjectList = (List<object>)dict["ColorDark"];
            List<object> ColorMediumObjectList = (List<object>)dict["ColorMedium"];
            List<object> ColorLightObjectList = (List<object>)dict["ColorLight"];
            List<float> ColorDarkFloats = ListOfObjectsToListOfFloats(ColorDarkObjectList);
            List<float> ColorMediumFloats = ListOfObjectsToListOfFloats(ColorMediumObjectList);
            List<float> ColorLightFloats = ListOfObjectsToListOfFloats(ColorLightObjectList);
            UnityEngine.Color ColorDark = new UnityEngine.Color(ColorDarkFloats[0], ColorDarkFloats[1], ColorDarkFloats[2], ColorDarkFloats[3]);
            UnityEngine.Color ColorMedium = new UnityEngine.Color(ColorMediumFloats[0], ColorMediumFloats[1], ColorMediumFloats[2], ColorMediumFloats[3]);
            UnityEngine.Color ColorLight = new UnityEngine.Color(ColorLightFloats[0], ColorLightFloats[1], ColorLightFloats[2], ColorLightFloats[3]);
            colors.dark = ColorDark;
            colors.medium = ColorMedium;
            colors.light = ColorLight;
            //used to define the custom platform type. will be used whenever we drill it
            colors.type = (PlatformType)PlatformType;
            return colors;
        }
        public static List<float> ListOfObjectsToListOfFloats(List<object> ObjectList)
        {
            List<float> Floats = new List<float>();
            for (int i = 0; i < ObjectList.Count; i++)
            {
                Floats.Add((float)Convert.ToDouble(ObjectList[i]));
            }
            return Floats;
        }
        public enum PathType
        { 
            None,
            AntiLockPlatform,
            VectorFieldPlatform
        }
    }
    [HarmonyPatch(typeof(MachoThrow2))]
    public class MachoThrow2Patches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        private static void Awake_MapMaker_Plug(MachoThrow2 __instance)
        {
            Debug.Log("MatchoThrow2");
            //if there is something to add
            if (Plugin.CustomMatchoManSprites.Count != 0)
            {
                __instance.boulders.sprites.AddRange(Plugin.CustomMatchoManSprites);
            }
            var ColorList = new List<UnityEngine.Color>(__instance.boulderSmokeColors);
            ColorList.AddRange(Plugin.CustomBoulderSmokeColors);
            __instance.boulderSmokeColors = ColorList.ToArray();
        }
    }
    [HarmonyPatch(typeof(Drill))]
    public class DrillPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        private static void Awake_MapMaker_Plug(Drill __instance)
        {
            Debug.Log("Drill");
            //if there is something to add
            if (Plugin.CustomDrillColors.Count != 0)
            {
                __instance.platformDependentColors.AddRange(Plugin.CustomDrillColors);
            }

        }
    }
}
