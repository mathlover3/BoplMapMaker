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
using PlatformApi;

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
        public static UnityEngine.Color[] ignore = {new UnityEngine.Color(1,1,1,1)};
        public static List<UnityEngine.Color> CustomBoulderSmokeColors = new List<UnityEngine.Color>(ignore);
        public static AssetBundle MyAssetBundle;
        public static PlatformApi.PlatformApi platformApi = new PlatformApi.PlatformApi();
        private static Trigger TriggerPrefab = null;
        private static Spawner SpawnerPrefab = null;
        private static DisappearPlatformsOnSignal DisappearPlatformsOnSignalPrefab = null;
        private static AndGate andGatePrefab = null;
        private static SignalDelay SignalDelayPrefab = null;
        private static OrGate OrGatePrefab = null;
        private static NotGate NotGatePrefab = null;
        public static SignalSystem signalSystem;
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
                Debug.Log("asset name is: " + name);
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
            //find objects
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
            UnityEngine.Debug.Log("getting Bow object");
            var objectsFound = 0;
            var ObjectsToFind = 2;
            foreach (GameObject obj in allObjects)
            {
                if (obj.name == "invisibleHitbox")
                {
                    // Found the object with the desired name
                    // You can now store its reference or perform any other actions
                    Trigger.DPhysicsBoxPrefab = obj.GetComponent<DPhysicsBox>();
                    UnityEngine.Debug.Log("Found the object: " + obj.name);
                    objectsFound++;
                    if (objectsFound == ObjectsToFind)
                    {
                        break;
                    }
                }
                if (obj.name == "Blink gun")
                {
                    // Found the object with the desired name
                    // You can now store its reference or perform any other actions
                    DisappearPlatformsOnSignal.QuantumTunnelPrefab = obj.GetComponent<ShootQuantum>().QuantumTunnelPrefab;
                    DisappearPlatformsOnSignal.onHitResizableWallMaterail = obj.GetComponent<ShootQuantum>().onHitResizableWallMaterail;
                    DisappearPlatformsOnSignal.onHitWallMaterail = obj.GetComponent<ShootQuantum>().onHitWallMaterail;
                    UnityEngine.Debug.Log("Found the object: " + obj.name);
                    objectsFound++;
                    if (objectsFound == ObjectsToFind)
                    {
                        break;
                    }
                }
                //Blink gun
            }
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
                    //store its reference
                    PlatformAbility = obj;
                    Debug.Log("Found the object: " + obj.name);
                    break;
                }
            }
            var platformTransform = PlatformAbility.GetComponent(typeof(PlatformTransform)) as PlatformTransform;
            platformPrefab = platformTransform.platformPrefab;
            //turn the json into a dicsanary. (david+chatgpt) dont remove it as it works.
            Dictionary<string, object> Dict = MiniJSON.Json.Deserialize(mapJson) as Dictionary<string, object>;
            //spawn point stuff

            if (Dict.ContainsKey("teamSpawns"))
            {
                //object time! (objects are so confusing)
                //convert object to list of objects
                List<System.Object> OrbitPathObjects = (List<System.Object>)Dict["teamSpawns"];
                //now to convert eatch object in the list to a list of 2 objects
                List<Vec2> Vecs1 = new List<Vec2>();
                for (int i = 0; i < OrbitPathObjects.Count; i++)
                {
                    var obj = (List<System.Object>)OrbitPathObjects[i];
                    var floatList = ListOfObjectsToListOfFloats(obj);
                    var floatVec = new Vec2((Fix)floatList[0], FloorToThousandnths(floatList[1]));
                    Vecs1.Add(floatVec);
                }
                Vec2[] Vecs = Vecs1.ToArray();
                //get the PlayerList
                //set it to null to avoid using unasigned local var error. it will be assigend when the code runs unless somthing goes very badly.
                GameObject PlayerList = null;
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name == "PlayerList")
                    {
                        //store its reference
                        PlayerList = obj;
                        Debug.Log("Found the PlayerList");
                        break;
                    }
                }
                GameSessionHandler handler = PlayerList.GetComponent(typeof(GameSessionHandler)) as GameSessionHandler;
                handler.teamSpawns = Vecs;
                //this is a static so i must set it from the type not a instance. also its not readonly like the name sugests.
                GameSessionHandler.playerSpawns_readonly = Vecs;
            }
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
                    double OrbitForce = 0;
                    Vec2[] OrbitPath = null;
                    double DelaySeconds = 0;
                    bool isBird = false;
                    double orbitSpeed = 100;
                    double expandSpeed = 100;
                    Vec2 centerPoint = new Vec2(Fix.Zero, Fix.Zero);
                    double normalSpeedFriction = 1;
                    double DeadZoneDist = 1;
                    double OrbitAccelerationMulitplier = 1;
                    double targetRadius = 5;
                    double ovalness01 =  1;
                    Vec2[] teamSpawns = new Vec2[4];
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

                    PlatformApi.PlatformApi.PathType pathType = PlatformApi.PlatformApi.PathType.None;
                    if (platform.ContainsKey("PathType"))
                    {
                        if (Convert.ToString(platform["PathType"]) == "AntiLockPlatform")
                        {
                            pathType = PlatformApi.PlatformApi.PathType.AntiLockPlatform;
                        }
                        else if (Convert.ToString(platform["PathType"]) == "VectorFieldPlatform")
                        {
                            pathType = PlatformApi.PlatformApi.PathType.VectorFieldPlatform;
                        }
                    }
                    //AntiLockPlatform
                    if (pathType == PlatformApi.PlatformApi.PathType.AntiLockPlatform)
                    {
                        OrbitForce = Convert.ToDouble(platform["OrbitForce"]);
                        //object time! (objects are so confusing)
                        //convert object to list of objects
                        List<System.Object> OrbitPathObjects = (List<System.Object>)platform["OrbitPath"];
                        //now to convert eatch object in the list to a list of 2 objects
                        List<Vec2> Vecs1 = new List<Vec2>();
                        for (int i = 0; i < OrbitPathObjects.Count; i++)
                        {
                            var obj = (List<System.Object>)OrbitPathObjects[i];
                            var floatList = ListOfObjectsToListOfFloats(obj);
                            var floatVec = new Vec2(FloorToThousandnths(floatList[0]), FloorToThousandnths(floatList[1]));
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
                        DelaySeconds = Convert.ToDouble(platform["DelaySeconds"]);
                    }
                    if (platform.ContainsKey("expandSpeed"))
                    {
                        expandSpeed = Convert.ToDouble(platform["expandSpeed"]);
                    }
                    if (platform.ContainsKey("centerPoint"))
                    {
                        var floats = ListOfObjectsToListOfFloats((List<object>)platform["centerPoint"]);
                        centerPoint = new Vec2(FloorToThousandnths(floats[0]), FloorToThousandnths(floats[1]));
                    }
                    if (platform.ContainsKey("normalSpeedFriction"))
                    {
                        normalSpeedFriction = Convert.ToDouble(platform["normalSpeedFriction"]);
                    }
                    if (platform.ContainsKey("DeadZoneDist"))
                    {
                        DeadZoneDist = Convert.ToDouble(platform["DeadZoneDist"]);
                    }
                    if (platform.ContainsKey("OrbitAccelerationMulitplier"))
                    {
                        OrbitAccelerationMulitplier = Convert.ToDouble(platform["OrbitAccelerationMulitplier"]);
                    }
                    if (platform.ContainsKey("targetRadius"))
                    {
                        targetRadius = Convert.ToDouble(platform["targetRadius"]);
                    }
                    if (platform.ContainsKey("ovalness01"))
                    {
                        ovalness01 = Convert.ToDouble(platform["ovalness01"]);
                    }
                    //if its a preset platform dont do any of this.
                    if (!IsPresetPatform)
                    {
                        double width = Convert.ToDouble(size["width"]);
                        double height = Convert.ToDouble(size["height"]);
                        double radius = Convert.ToDouble(platform["radius"]);
                        bool UseCustomMassScale = false;
                        float Red = 1;
                        float Green = 1;
                        float Blue = 1;
                        float Opacity = 1;
                        bool UseCustomDrillColorAndBolderTexture = false;
                        bool UseSlimeCam = false;
                        PlatformType platformType = PlatformType.slime;
                        Vector4 color;
                        //reset UseCustomTexture so the value for 1 platform doesnt blead trough to anouter
                        UseCustomTexture = false;
                        double CustomMassScale = 0.05;


                        //custom mass
                        if (platform.ContainsKey("UseCustomMassScale"))
                        {
                            UseCustomMassScale = (bool)platform["UseCustomMassScale"];
                        }
                        if (platform.ContainsKey("CustomMassScale") && UseCustomMassScale)
                        {
                            CustomMassScale = Convert.ToDouble(platform["CustomMassScale"]);
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
                        Vector4[] color2 = {color};
                        Vec2[] centerPoint2 = {centerPoint};
                        //spawn platform
                        PlatformApi.PlatformApi.SpawnPlatform((Fix)x, (Fix)y, (Fix)width, (Fix)height, (Fix)radius, (Fix)rotatson, CustomMassScale, color2, platformType, UseSlimeCam, sprite, pathType, OrbitForce, OrbitPath, DelaySeconds, isBird, orbitSpeed, expandSpeed, centerPoint2, normalSpeedFriction, DeadZoneDist, OrbitAccelerationMulitplier, targetRadius, ovalness01);

                        Debug.Log("Platform spawned successfully");
                    }
                    
                    // if it is a preset platform then we do it difrently
                    else
                    {
                        string PresetPlatformName = Convert.ToString(platform["PresetPlatformName"]);
                        var Platform = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/"+ PresetPlatformName + ".prefab");
                        //idk if this is gonna work to fix the posable desink as it is converted back to a float...
                        var x2 = FloorToThousandnths(x);
                        var y2 = FloorToThousandnths(y);
                        Vector3 pos = new Vector3 ((float)x2, (float)y2, 0);
                        //the rest of the FloorToThousandnths should work fine for fixing it though
                        //fix shader
                        Platform.GetComponent<SpriteRenderer>().material = PlatformMat;
                        //scale object
                        //AddComponent(typeof(namespace.className)); 
                        var ScaleFactor = FloorToThousandnths(Convert.ToDouble(platform["ScaleFactor"]));
                        var GrowOnStartComp = Platform.AddComponent(typeof(GrowOnStart)) as GrowOnStart;
                        GrowOnStartComp.scaleUp = ScaleFactor;
                        Debug.Log("added GrowOnStart");
                        //spawn object
                        Platform = UnityEngine.Object.Instantiate<GameObject>(Platform, pos, Quaternion.identity);
                        //rotate object
                        StickyRoundedRectangle StickyRect = Platform.GetComponent<StickyRoundedRectangle>();
                        StickyRect.GetGroundBody().rotation = FloorToThousandnths(rotatson);
                        if (pathType == PlatformApi.PlatformApi.PathType.AntiLockPlatform)
                        {
                            //antilock platform
                            var AntiLockPlatformComp = Platform.AddComponent(typeof(AntiLockPlatform)) as AntiLockPlatform;
                            AntiLockPlatformComp.OrbitForce = FloorToThousandnths(OrbitForce);
                            AntiLockPlatformComp.OrbitPath = OrbitPath;
                            AntiLockPlatformComp.DelaySeconds = FloorToThousandnths(DelaySeconds);
                            AntiLockPlatformComp.isBird = isBird;
                        }
                        if (pathType == PlatformApi.PlatformApi.PathType.VectorFieldPlatform)
                        {
                            var VectorFieldPlatformComp = Platform.AddComponent(typeof(VectorFieldPlatform)) as VectorFieldPlatform;
                            VectorFieldPlatformComp.centerPoint = centerPoint;
                            VectorFieldPlatformComp.DeadZoneDist = FloorToThousandnths(DeadZoneDist);
                            VectorFieldPlatformComp.DelaySeconds = FloorToThousandnths(DelaySeconds);
                            VectorFieldPlatformComp.expandSpeed = FloorToThousandnths(expandSpeed);
                            VectorFieldPlatformComp.normalSpeedFriction = FloorToThousandnths(normalSpeedFriction);
                            VectorFieldPlatformComp.OrbitAccelerationMulitplier = FloorToThousandnths(OrbitAccelerationMulitplier);
                            VectorFieldPlatformComp.orbitSpeed = FloorToThousandnths(orbitSpeed);
                            VectorFieldPlatformComp.ovalness01 = FloorToThousandnths(ovalness01);
                        }
                    }

                }
                catch (Exception ex)
                {   
                    Debug.LogError($"Failed to spawn platform. Error: {ex.Message}");
                }
            }
            if (Dict.ContainsKey("boulders"))
            {
                List<object> boulders = (List<object>)Dict["boulders"];
                MapMaker.MoreJsonParceing.SpawnBoulders(boulders);
            }



        }
        public static Fix FloorToThousandnths(double value)
        {
            return Fix.Floor(((Fix)value) * (Fix)1000) / (Fix)1000;
        }
        public static bool IsCustomTexture(string textureName)
        {
            return textureName.EndsWith(CustomTextureName);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            platformApi.OnSceneLoaded(scene, mode);
            Debug.Log("OnSceneLoaded: " + scene.name);
            if (IsLevelName(scene.name))
            {
                
                try
                {

                    // Create a new GameObject
                    GameObject triggerGameObject = new GameObject("TriggerObject");

                    // Add the FixTransform and Trigger components to the GameObject
                    triggerGameObject.AddComponent<FixTransform>();

                    TriggerPrefab = triggerGameObject.AddComponent<Trigger>();
                    // Create a new GameObject
                    GameObject spawnerGameObject = new GameObject("SpawnerObject");

                    // Add the components to the GameObject
                    spawnerGameObject.AddComponent<FixTransform>();
                    SpawnerPrefab = spawnerGameObject.AddComponent<Spawner>();
                    // Create a new GameObject
                    GameObject DisappearGameObject = new GameObject("DisappearPlatformsObject");

                    // Add the components to the GameObject
                    DisappearGameObject.AddComponent<FixTransform>();
                    DisappearPlatformsOnSignalPrefab = DisappearGameObject.AddComponent<DisappearPlatformsOnSignal>();

                    // Create a new GameObject
                    GameObject AndGateObject = new GameObject("AndGateObject");

                    // Add the components to the GameObject
                    AndGateObject.AddComponent<FixTransform>();
                    //put it offscreen
                    AndGateObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    AndGateObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    andGatePrefab = AndGateObject.AddComponent<AndGate>();
                    var AndGateRender = AndGateObject.AddComponent<SpriteRenderer>();
                    Debug.Log(MyAssetBundle.LoadAsset("assets/assetbundleswanted/andgate.prefab"));
                    var AndGateSpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/andgate.prefab");
                    AndGateRender.sprite = AndGateSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject SignalDelayObject = new GameObject("SignalDelayObject");
                    // Add the components to the GameObject
                    SignalDelayObject.AddComponent<FixTransform>();
                    //put it offscreen
                    SignalDelayObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    SignalDelayObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    SignalDelayPrefab = SignalDelayObject.AddComponent<SignalDelay>();
                    var SignalDelaySpriteRender = SignalDelayObject.AddComponent<SpriteRenderer>();
                    var SignalDelaySpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/delaygate.prefab");
                    SignalDelaySpriteRender.sprite = SignalDelaySpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject OrGateObject = new GameObject("OrGateObject");
                    // Add the FixTransform and Spawner components to the GameObject
                    OrGateObject.AddComponent<FixTransform>();
                    //put it offscreen
                    OrGateObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    OrGateObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    OrGatePrefab = OrGateObject.AddComponent<OrGate>();
                    var OrGateSpriteRender = OrGateObject.AddComponent<SpriteRenderer>();
                    var OrGateSpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/orgate.prefab");
                    OrGateSpriteRender.sprite = OrGateSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject NotGateObject = new GameObject("NotGateObject");
                    // Add the FixTransform and Spawner components to the GameObject
                    NotGateObject.AddComponent<FixTransform>();
                    //put it offscreen
                    NotGateObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    NotGateObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    NotGatePrefab = NotGateObject.AddComponent<NotGate>();
                    var NotGateSpriteRender = NotGateObject.AddComponent<SpriteRenderer>();
                    var NotGateSpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/notgate.prefab");
                    NotGateSpriteRender.sprite = NotGateSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject SignalSystemObject = new GameObject("SignalSystemObject");


                    // Add the FixTransform and Spawner components to the GameObject
                    SignalSystemObject.AddComponent<FixTransform>();
                    signalSystem = SignalSystemObject.AddComponent<SignalSystem>();
                    SignalSystem.LogicInputs = new List<LogicInput>();
                    SignalSystem.LogicOutputs = new List<LogicOutput>();
                    SignalSystem.LogicStartingOutputs = new List<LogicOutput>();
                    SignalSystem.LogicGatesToAlwaysUpdate = new List<LogicGate>();
                    SignalSystem.LineRenderers = new();
                    SignalSystem.LogicInputsThatAlwaysUpdateThereLineConnectsons = new();
                    SignalSystem.FirstUpdateOfTheRound = true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in spawning triggers/spawners at scene load: {ex}");
                }
                //TODO remove this when done testing with spawners
                //TESTING START!
                Vec2[] path = { new Vec2(Fix.Zero, (Fix)10), new Vec2((Fix)10, (Fix)10) };
                Vec2[] center = { new Vec2((Fix)0, (Fix)15) };
                var platform = PlatformApi.PlatformApi.SpawnPlatform((Fix)0, (Fix)10, (Fix)2, (Fix)2, (Fix)1, Fix.Zero, 0.05, null, PlatformType.slime, false, null, PlatformApi.PlatformApi.PathType.VectorFieldPlatform, 500, path, 0, false, 100, 100, center);
                List<int> layers = new List<int>
                {
                    LayerMask.NameToLayer("Player")
                };
                CreateTrigger(layers, new Vec2((Fix)(-10), (Fix)30), new Vec2((Fix)10, (Fix)10), 0);
                CreateTrigger(layers, new Vec2((Fix)10, (Fix)30), new Vec2((Fix)10, (Fix)10), 1);
                int[] UUids = { 0, 4 };
                CreateOrGate(UUids, 6, new Vec2(Fix.Zero, (Fix)5), (Fix)0);
                CreateNotGate(6, 2, new Vec2((Fix)5, (Fix)5), (Fix)0);
                int[] UUids2 = { 1, 5 };
                CreateOrGate(UUids2, 7, new Vec2(Fix.Zero, (Fix)(-5)), (Fix)0);
                CreateNotGate(7, 3, new Vec2((Fix)5, (Fix)(-5)), (Fix)0);
                CreateSignalDelay(2, 5, Fix.Zero, new Vec2((Fix)2, (Fix)(-2)), (Fix)180);
                CreateSignalDelay(3, 4, Fix.Zero, new Vec2((Fix)2, (Fix)(2)), (Fix)180);
                AddMovingPlatformSignalStuff(platform, 2);
                CreateDisappearPlatformsOnSignal(platform, 3, Fix.Zero, Fix.One, false, false, false);
                //MAKE SURE TO CALL THIS WHEN DONE CREATING SIGNAL STUFF!
                signalSystem.SetUpDicts();
                Debug.Log("signal stuff is done!");
                //TESTING END!
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
        public static Spawner CreateSpawner(Vec2 Pos, Fix SimTimeBetweenSpawns, Vec2 SpawningVelocity, Fix angularVelocity, Spawner.ObjectSpawnType spawnType = Spawner.ObjectSpawnType.None, PlatformType BoulderType = PlatformType.grass, bool UseSignal = false, int Signal = 0, bool IsTriggerSignal = false)
        {
            var spawner = FixTransform.InstantiateFixed<Spawner>(SpawnerPrefab, Pos);
            spawner.spawnType = spawnType;
            spawner.UseSignal = UseSignal;
            var input = new LogicInput
            {
                UUid = Signal,
                gate = spawner,
                IsOn = false,
                Owner = spawner.gameObject
            };
            spawner.InputSignals.Add(input);
            spawner.IsTriggerSignal = IsTriggerSignal;
            spawner.BoulderType = BoulderType;
            spawner.SimTimeBetweenSpawns = SimTimeBetweenSpawns;
            spawner.velocity = SpawningVelocity;
            spawner.angularVelocity = angularVelocity;
            spawner.Register();
            return spawner;
        }
        public static Trigger CreateTrigger(List<int> LayersToDetect, Vec2 Pos, Vec2 Extents, ushort Signal)
        {
            var trigger = FixTransform.InstantiateFixed<Trigger>(TriggerPrefab, new Vec2(Fix.Zero, (Fix)30));
            trigger.layersToDetect = LayersToDetect;
            var output = new LogicOutput
            {
                UUid = Signal,
                gate = null,
                IsOn = false,
                Owner = trigger.gameObject
            };
            trigger.LogicOutput = output;
            trigger.SetPos(Pos);
            trigger.SetExtents(Extents);
            trigger.Register();
            return trigger;
        }
        public static DisappearPlatformsOnSignal CreateDisappearPlatformsOnSignal(GameObject platform, int Signal, Fix SecondsToReapper, Fix delay,  bool SignalIsInverse = false, bool DisappearOnlyWhenSignal = false, bool OnlyDisappearWhenSignalTurnsOn = false)
        {
            var Disappear = FixTransform.InstantiateFixed<DisappearPlatformsOnSignal>(DisappearPlatformsOnSignalPrefab, (Vec2)platform.transform.position);
            Disappear.platform = platform;
            var input = new LogicInput
            {
                UUid = Signal,
                gate = Disappear,
                IsOn = false,
                Owner = Disappear.gameObject
            };
            Disappear.InputSignals.Add(input);
            Disappear.SignalIsInverse = SignalIsInverse;
            Disappear.delay = delay;
            Disappear.SecondsToReapper = SecondsToReapper;
            Disappear.DisappearOnlyWhenSignal = DisappearOnlyWhenSignal;
            Disappear.OnlyDisappearWhenSignalTurnsOn = OnlyDisappearWhenSignalTurnsOn;
            Disappear.Register();
            return Disappear;

        }
        public static MovingPlatformSignalStuff AddMovingPlatformSignalStuff(GameObject platform, int Signal, bool SignalIsInverted = false)
        {
            var SignalStuff = platform.AddComponent<MovingPlatformSignalStuff>();
            var input = new LogicInput
            {
                UUid = Signal,
                gate = SignalStuff,
                IsOn = false,
                Owner = SignalStuff.gameObject
            };
            SignalStuff.InputSignals.Add(input);
            SignalStuff.SignalIsInverted = SignalIsInverted;
            SignalStuff.Register();
            return SignalStuff;

        }
        public static AndGate CreateAndGate(int[] InputUUids, int OutputUUid, Vec2 pos, Fix rot)
        {
            
            var And = FixTransform.InstantiateFixed<AndGate>(andGatePrefab, pos, (Fix)ConvertToRadians((double)rot));
            var LogicInputs = new List<LogicInput>();
            foreach (var InputSignal in InputUUids)
            {
                var input = new LogicInput
                {
                    UUid = InputSignal,
                    gate = And,
                    IsOn = false,
                    Owner = And.gameObject
                };
                LogicInputs.Add(input);
            }
            var output = new LogicOutput
            {
                UUid = OutputUUid,
                gate = And,
                IsOn = false,
                Owner = And.gameObject
            };
            And.InputSignals.AddRange(LogicInputs);
            And.OutputSignals.Add(output);
            And.Register();
            return And;
        }
        public static SignalDelay CreateSignalDelay(int InputSignal, int OutputSignal, Fix delay, Vec2 pos, Fix rot)
        {
            var Delay = FixTransform.InstantiateFixed<SignalDelay>(SignalDelayPrefab, pos, (Fix)ConvertToRadians((double)rot));
            var input = new LogicInput
            {
                UUid = InputSignal,
                gate = Delay,
                IsOn = false,
                Owner = Delay.gameObject
            };
            var output = new LogicOutput
            {
                UUid = OutputSignal,
                gate = Delay,
                IsOn = false,
                Owner = Delay.gameObject
            };
            Delay.delay = delay;
            Delay.InputSignals.Add(input);
            Delay.OutputSignals.Add(output);
            Delay.Register();
            return Delay;

        }
        public static OrGate CreateOrGate(int[] InputUUids, int OutputUUid, Vec2 pos, Fix rot) 
        {
            var Or = FixTransform.InstantiateFixed<OrGate>(OrGatePrefab, pos, (Fix)ConvertToRadians((double)rot));
            var LogicInputs = new List<LogicInput>();
            foreach (var InputSignal in InputUUids)
            {
                var input = new LogicInput
                {
                    UUid = InputSignal,
                    gate = Or,
                    IsOn = false,
                    Owner = Or.gameObject
                };
                LogicInputs.Add(input);
            }
            var output = new LogicOutput
            {
                UUid = OutputUUid,
                gate = Or,
                IsOn = false,
                Owner = Or.gameObject
            };
            Or.InputSignals.AddRange(LogicInputs);
            Or.OutputSignals.Add(output);
            Or.Register();
            return Or;
        }
        public static NotGate CreateNotGate(int InputUUid, int OutputUUid, Vec2 pos, Fix rot)
        {
            var Not = FixTransform.InstantiateFixed<NotGate>(NotGatePrefab, pos, (Fix)ConvertToRadians((double)rot));
            var input = new LogicInput
            {
                UUid = InputUUid,
                gate = Not,
                IsOn = false,
                Owner = Not.gameObject
            };
            var output = new LogicOutput
            {
                UUid = OutputUUid,
                gate = Not,
                IsOn = false,
                Owner = Not.gameObject
            };
            Not.InputSignals.Add(input);
            Not.OutputSignals.Add(output);
            Not.Register();
            return Not;
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
    //these 2 things happen before the logic gate stuff has a chance to run for the frame so it will be 1 frame behind.
    [HarmonyPatch(typeof(AntiLockPlatform))]
    public class AntiLockPlatformPatches
    {
        [HarmonyPatch("UpdateSim")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(AntiLockPlatform __instance)
        {
            if (__instance.GetComponent<MovingPlatformSignalStuff>() != null)
            {
                var SignalStuff = __instance.GetComponent<MovingPlatformSignalStuff>();
                //if its on and its not inverted
                if (!SignalStuff.SignalIsInverted && SignalStuff.IsOn())
                {
                    //contenue the path
                    return true;
                }
                //if the signal is off and it is inverted
                if (SignalStuff.SignalIsInverted && SignalStuff.IsOn())
                {
                    //contenue the path
                    return true;
                }
                //signal is off and it isnt inverted or signal is on and it is inverted
                return false;
            }
            //no MovingPlatformSignalStuff comp found
            return true;
        }
    }
    [HarmonyPatch(typeof(VectorFieldPlatform))]
    public class VectorFieldPlatformPatches
    {
        [HarmonyPatch("UpdateSim")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(VectorFieldPlatform __instance)
        {
            if (__instance.GetComponent<MovingPlatformSignalStuff>() != null)
            {
                var SignalStuff = __instance.GetComponent<MovingPlatformSignalStuff>();
                //if its on and its not inverted
                if (!SignalStuff.SignalIsInverted && SignalStuff.IsOn())
                {
                    //contenue the path
                    return true;
                }
                //if the signal is off and it is inverted
                if (SignalStuff.SignalIsInverted && SignalStuff.IsOn())
                {
                    //contenue the path
                    return true;
                }
                //signal is off and it isnt inverted or signal is on and it is inverted
                return false;
            }
            //no MovingPlatformSignalStuff comp found
            return true;
        }
    }
}
