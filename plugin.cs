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
using static UnityEngine.ParticleSystem.PlaybackState;
using System.Web;
using MapMaker.Lua_stuff;
using MoonSharp.Interpreter;
using Mono.Cecil.Cil;
using UnityEngine.Events;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using Entwined;
using System.Collections;
using MoonSharp.VsCodeDebugger;
namespace MapMaker
{
    [BepInDependency("com.entwinedteam.entwined")]
    [BepInPlugin("com.MLT.MapLoader", "MapLoader", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin instance;
        public static GameObject PlatformAbility;
        public static Transform levelt;
        public static StickyRoundedRectangle platformPrefab;
        public static ParticleSystem WavePrefab;
        public static List<ResizablePlatform> Platforms;
        public static int t;
        public static string mapsFolderPath; // Create blank folder path var
        public static int CurrentMapUUID;
        public static int CurrentMapIndex;
        public static Fix OneByOneBlockMass = Fix.One;
        public static string[] MapJsons;
        public static string[] MetaDataJsons;
        // Define a static logger instance
        public static ManualLogSource logger;
        public static bool UseCustomTexture = false;
        public static string CustomTextureName;
        //all the zipArchives in the same order as the MapJsons
        public static ZipArchive[] zipArchives = { };
        //my zip archives. not overiten when joining someone else.
        public static ZipArchive[] MyZipArchives = { };
        public static Sprite sprite;
        public static Material PlatformMat;
        public static Material GrassMat;
        public static GameObject SlimeCamObject;
        public static List<Drill.PlatformColors> CustomDrillColors;
        public static List<NamedSprite> CustomMatchoManSprites;
        public static int NextPlatformTypeValue = 5;
        public const int StartingNextPlatformTypeValue = 5;
        public static Sprite BoulderSprite;
        //used to make CustomBoulderSmokeColors start with a value.
        public static UnityEngine.Color[] ignore = { new UnityEngine.Color(1, 1, 1, 1) };
        public static List<UnityEngine.Color> CustomBoulderSmokeColors = new List<UnityEngine.Color>(ignore);
        public static AssetBundle MyAssetBundle;
        public static AssetBundle SpriteAssetBundle;
        public static PlatformApi.PlatformApi platformApi = new PlatformApi.PlatformApi();
        private static Trigger TriggerPrefab = null;
        private static Spawner SpawnerPrefab = null;
        private static DisappearPlatformsOnSignal DisappearPlatformsOnSignalPrefab = null;
        private static AndGate andGatePrefab = null;
        private static SignalDelay SignalDelayPrefab = null;
        private static OrGate OrGatePrefab = null;
        private static NotGate NotGatePrefab = null;
        private static ShootRay ShootRayPrefab = null;
        private static ShakePlatform ShakePlatformPrefab = null;
        private static DropPlayers DropPlayersPrefab = null;
        private static LuaMain LuaPrefab = null;
        public static SignalSystem signalSystem;
        public static int NextUUID = 0;
        public static string PluginPath;
        //used to fix a unity bug
        public static PlayerAverageCamera averageCamera;
        public static ShakableCamera shakableCamera;
        public static List<PlayerInput> playerInputs = new();
        public static bool FirstUpdate = true;
        //used to chose a map from the random bag
        public static List<int> MapIndexsUsed = new();
        //map ids
        public static readonly int GrassMapId = 0;
        public static readonly int SnowMapId = 21;
        public static readonly int SpaceMapId = 33;

        //used to make shakeable platform to know its being called by a blink gun.
        public static bool CurrentlyBlinking;
        //networking
        public static int NextMapIndex;
        //used for making the map bigger (replacing all refrences in the main game from scenebounds to this using transpilers)
        public static Fix Camera_XMin = (Fix)(-97.27f);

        public static Fix Camera_XMax = (Fix)97.6f;

        public static Fix Camera_YMax = (Fix)40f;

        private static Fix waterHeight = (Fix)(-11.3f);

        private static Fix spaceWaterHeight = (Fix)(-50f);

        public static Fix Camera_YMin = -(Fix)26L;

        public static Fix BlastZone_XMin = (Fix)(-105L);

        public static Fix BlastZone_XMax = (Fix)105L;

        public static Fix BlastZone_YMax = (Fix)58L;
        public enum MapIdCheckerThing
        {
            MapFoundWithId,
            NoMapFoundWithId,
            MultipleMapsFoundWithId
        }
        private void Awake()
        {
            instance = this;
            NetworkingStuff.Awake();
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
            PluginPath = Info.Location;
            //thanks almafa64 on discord for the path stuff.
            MyAssetBundle = AssetBundle.LoadFromFile(Path.GetDirectoryName(Info.Location) + "/mapmakerassets");
            string[] assetNames = MyAssetBundle.GetAllAssetNames();
            SpriteAssetBundle = AssetBundle.LoadFromFile(Path.GetDirectoryName(Info.Location) + "/mapmakericons");
            //MapUUIDChannel = new EntwinedPacketChannel<int>(this, new IntEntwiner());
            //MapUUIDChannel.OnMessage += OnGetUUID; 
            foreach (string name in assetNames)
            {
                Debug.Log("asset name is: " + name);
            }

        }
        public static bool IsReplay()
        {
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArgs.Length; i++)
            {
                if (commandLineArgs[i].EndsWith(".rep"))
                {
                    return true;
                }
            }
            return false;
        }

        public void Start()
        {
            ZipArchive[] zipArchives2;
            // if we are loading a replay the zip archive is already set
            if (!IsReplay())
            {
                //fill the MapJsons array up
                zipArchives2 = GetZipArchives();
            }
            else
            {
                zipArchives2 = zipArchives;
            }
            
            //Create a List for the json for a bit
            List<string> JsonList = new List<string>();
            List<string> MetaDataList = new();
            foreach (ZipArchive zipArchive in zipArchives2)
            {
                //get the first .boplmap file if there is multiple. (THERE SHOULD NEVER BE MULTIPLE .boplmap's IN ONE .zip)
                JsonList.Add(GetFileFromZipArchive(zipArchive, IsBoplMap)[0]);
                MetaDataList.Add(GetFileFromZipArchive(zipArchive, IsMetaDataFile)[0]);
            }
            MapJsons = JsonList.ToArray();
            MetaDataJsons = MetaDataList.ToArray();
            //find objects
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
            var objectsFound = 0;
            var ObjectsToFind = 4;
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
                    ShootBlink.collisionMask = obj.GetComponent<ShootQuantum>().collisionMask;
                    ShootBlink.onDissapearResizableWallMaterail = obj.GetComponent<ShootQuantum>().onDissapearResizableWallMaterail;
                    ShootBlink.onHitBlackHoleMaterial = obj.GetComponent<ShootQuantum>().onHitBlackHoleMaterial;
                    ShootBlink.onHitResizableWallMaterail = obj.GetComponent<ShootQuantum>().onHitResizableWallMaterail;
                    ShootBlink.onHitWallMaterail = obj.GetComponent<ShootQuantum>().onHitWallMaterail;
                    ShootBlink.QuantumTunnelPrefab = obj.GetComponent<ShootQuantum>().QuantumTunnelPrefab;
                    ShootBlink.RayCastEffect = obj.GetComponent<ShootQuantum>().RayCastEffect;
                    ShootBlink.raycastEffectSpacing = obj.GetComponent<ShootQuantum>().raycastEffectSpacing;
                    ShootBlink.RaycastParticleHitPrefab = obj.GetComponent<ShootQuantum>().RaycastParticleHitPrefab;
                    ShootBlink.WaterExplosion = obj.GetComponent<ShootQuantum>().WaterExplosion;
                    ShootBlink.RaycastParticlePrefab = obj.GetComponent<ShootQuantum>().RaycastParticlePrefab;
                    ShootBlink.WaterRing = obj.GetComponent<ShootQuantum>().WaterRing;
                    UnityEngine.Debug.Log("Found the object: " + obj.name);
                    objectsFound++;
                    if (objectsFound == ObjectsToFind)
                    {
                        break;
                    }
                }
                if (obj.name == "Growth ray")
                {
                    // Found the object with the desired name
                    // You can now store its reference or perform any other actions
                    ShootRay.GrowGameObjectPrefab = obj;
                    UnityEngine.Debug.Log("Found the object: " + obj.name);
                    objectsFound++;
                    if (objectsFound == ObjectsToFind)
                    {
                        break;
                    }
                }
                if (obj.name == "Shrink ray")
                {
                    // Found the object with the desired name
                    // You can now store its reference or perform any other actions
                    ShootRay.StrinkGameObjectPrefab = obj;
                    UnityEngine.Debug.Log("Found the object: " + obj.name);
                    objectsFound++;
                    if (objectsFound == ObjectsToFind)
                    {
                        break;
                    }
                }
            }
        }
        public void Update()
        {
            if (FirstUpdate)
            {
                FirstUpdate = false;
                if (!IsReplay())
                {
                    StartCoroutine(GetGrassMat());
                }
                

            }
        }
        IEnumerator GetGrassMat()
        {
            //enter the tutorial to get the grass mat
            SceneManager.LoadScene("Tutorial", LoadSceneMode.Single);
            //https://forum.unity.com/threads/how-to-wait-for-a-frame-in-c.24616/
            //wait one frame (no clue how this works but it does)
            yield return 0;
            var grass = GameObject.Find("AnimatedGrass");
            GrassMat = grass.gameObject.GetComponent<SpriteRenderer>().material;
            //exit the tutorial
            PlayerHandler.Get().PlayerList().Clear();
            TutorialGameHandler.isInTutorial = false;
            Updater.PreLevelLoad();
            SceneManager.LoadScene("MainMenu");
            Updater.PostLevelLoad();
            Debug.Log("got mat!");
        }
        public static bool IsBoplMap(string path)
        {
            if (path.EndsWith("boplmap")) return true;
            //will only be reached if its not a boplmap
            return false;
        }
        public static bool IsMetaDataFile(string path)
        {
            return path.EndsWith("MetaData.json");
        }
        //see if there is a custom map we should load (returns enum) (david) (this was annoying to make but at least i learned about predicits!)
        public static MapIdCheckerThing CheckIfWeHaveCustomMapWithMapId()
        {
            int[] MapIds = { };
            foreach (string MetaDataJson in MetaDataJsons)
            {
                try
                {
                    Dictionary<string, object> Dict = MiniJSON.Json.Deserialize(MetaDataJson) as Dictionary<string, object>;
                    //add it to a array to be checked
                    int mapid = Convert.ToInt32(Dict["MapUUID"]);
                    Debug.Log("Map has MapUUID of " + mapid);
                    MapIds = MapIds.Append(mapid).ToArray();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to get MapId from Json: {MetaDataJson} with exseptson: {ex}");
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
            if (ValueToCheck == CurrentMapUUID)
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
            if (MapJsons.Length != 0)
            {
                var i = CurrentMapIndex;
                var mapJson = MapJsons[CurrentMapIndex];
                try
                {
                    Dictionary<string, object> Meta = MiniJSON.Json.Deserialize(MetaDataJsons[i]) as Dictionary<string, object>;
                    if (Convert.ToInt32(Meta["MapUUID"]) == CurrentMapUUID)
                    {
                        Dictionary<string, object> Dict = MiniJSON.Json.Deserialize(mapJson) as Dictionary<string, object>;
                        SpawnPlatformsFromMap(Dict, i);
                        if (Dict.ContainsKey("AndGates"))
                        {
                            MoreJsonParceing.SpawnAndGates((List<object>)Dict["AndGates"]);
                        }
                        if (Dict.ContainsKey("OrGates"))
                        {
                            MoreJsonParceing.SpawnOrGates((List<object>)Dict["OrGates"]);
                        }
                        if (Dict.ContainsKey("NotGates"))
                        {
                            MoreJsonParceing.SpawnNotGates((List<object>)Dict["NotGates"]);
                        }
                        if (Dict.ContainsKey("DelayGates"))
                        {
                            MoreJsonParceing.SpawnDelayGates((List<object>)Dict["DelayGates"]);
                        }
                        if (Dict.ContainsKey("Triggers"))
                        {
                            MoreJsonParceing.SpawnTriggers((List<object>)Dict["Triggers"]);
                        }
                        if (Dict.ContainsKey("ShootBlinks"))
                        {
                            MoreJsonParceing.SpawnShootBlinks((List<object>)Dict["ShootBlinks"]);
                        }
                        if (Dict.ContainsKey("ShootGrows"))
                        {
                            MoreJsonParceing.SpawnShootGrows((List<object>)Dict["ShootGrows"]);
                        }
                        if (Dict.ContainsKey("ShootStrinks"))
                        {
                            MoreJsonParceing.SpawnShootStrinks((List<object>)Dict["ShootStrinks"]);
                        }
                        if (Dict.ContainsKey("Spawners"))
                        {
                            MoreJsonParceing.SpawnSpawners((List<object>)Dict["Spawners"]);
                        }
                        if (Dict.ContainsKey("LuaGates"))
                        {
                            //MoreJsonParceing.SpawnLuaGates((List<object>)Dict["LuaGates"], i);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load map from json: {mapJson} Error: {ex}");
                }
            }
        }

        public static void SpawnPlatformsFromMap(Dictionary<string, object> Dict, int index)
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
                Vec2[] Vecs = Vecs1.Count == 1 ? Enumerable.Repeat(Vecs1[0], 4).ToArray() : Vecs1.ToArray();
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
                    //level 5 and likely some outers have it called this for some reson.
                    if (obj.name == "PlayerList (1)")
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
            //Debug.Log("platforms set");
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
                    double ovalness01 = 1;
                    Vec2[] teamSpawns = new Vec2[4];
                    // Extract platform data (david)
                    Dictionary<string, object> transform = (Dictionary<string, object>)platform["transform"];
                    double x = Convert.ToDouble(transform["x"]);
                    double y = Convert.ToDouble(transform["y"]);
                    //defult to 0 rotatson incase the json is missing it
                    Fix rotatson = Fix.Zero;
                    if (platform.ContainsKey("rotation"))
                    {
                        rotatson = ConvertToRadians(Convert.ToDouble(platform["rotation"]));
                    }
                    //Debug.Log("getting IsPresetPatform");
                    bool IsPresetPatform = platform.ContainsKey("PresetPlatform");
                    //Debug.Log("IsPresetPatform is: " + IsPresetPatform);

                    //path stuff

                    PlatformApi.PlatformApi.PathType pathType = PlatformApi.PlatformApi.PathType.None;
                    if (platform.ContainsKey("AntiLockPlatform"))
                    {
                        pathType = PlatformApi.PlatformApi.PathType.AntiLockPlatform;
                    }
                    if (platform.ContainsKey("VectorFieldPlatform"))
                    {
                        pathType = PlatformApi.PlatformApi.PathType.VectorFieldPlatform;
                    }
                    //AntiLockPlatform
                    if (pathType == PlatformApi.PlatformApi.PathType.AntiLockPlatform)
                    {
                        var AntiLockPlatform = (Dictionary<string, object>)platform["AntiLockPlatform"];
                        OrbitForce = Convert.ToDouble(AntiLockPlatform["OrbitForce"]);
                        //object time! (objects are so confusing)
                        //convert object to list of objects
                        List<System.Object> OrbitPathObjects = (List<System.Object>)AntiLockPlatform["OrbitPath"];
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
                        isBird = (bool)AntiLockPlatform["isBird"];
                        DelaySeconds = Convert.ToDouble(AntiLockPlatform["DelaySeconds"]);
                    }
                    if (pathType == PlatformApi.PlatformApi.PathType.VectorFieldPlatform)
                    {
                        var VectorFieldPlatform = (Dictionary<string, object>)platform["VectorFieldPlatform"];
                        if (VectorFieldPlatform.ContainsKey("expandSpeed"))
                        {
                            expandSpeed = Convert.ToDouble(VectorFieldPlatform["expandSpeed"]);
                        }
                        if (VectorFieldPlatform.ContainsKey("centerPoint"))
                        {
                            var floats = ListOfObjectsToListOfFloats((List<object>)VectorFieldPlatform["centerPoint"]);
                            centerPoint = new Vec2(FloorToThousandnths(floats[0]), FloorToThousandnths(floats[1]));
                        }
                        if (VectorFieldPlatform.ContainsKey("normalSpeedFriction"))
                        {
                            normalSpeedFriction = Convert.ToDouble(VectorFieldPlatform["normalSpeedFriction"]);
                        }
                        if (VectorFieldPlatform.ContainsKey("DeadZoneDist"))
                        {
                            DeadZoneDist = Convert.ToDouble(VectorFieldPlatform["DeadZoneDist"]);
                        }
                        if (VectorFieldPlatform.ContainsKey("OrbitAccelerationMulitplier"))
                        {
                            OrbitAccelerationMulitplier = Convert.ToDouble(VectorFieldPlatform["OrbitAccelerationMulitplier"]);
                        }
                        if (VectorFieldPlatform.ContainsKey("targetRadius"))
                        {
                            targetRadius = Convert.ToDouble(VectorFieldPlatform["targetRadius"]);
                        }
                        if (VectorFieldPlatform.ContainsKey("ovalness01"))
                        {
                            ovalness01 = Convert.ToDouble(VectorFieldPlatform["ovalness01"]);
                        }
                        if (VectorFieldPlatform.ContainsKey("DelaySeconds"))
                        {
                            DelaySeconds = Convert.ToDouble(VectorFieldPlatform["DelaySeconds"]);
                        }
                    }

                    //if its a preset platform dont do any of this.
                    if (!IsPresetPatform)
                    {
                        Dictionary<string, object> size = (Dictionary<string, object>)platform["size"];
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
                        Dictionary<string, object> CustomTexture = null;
                        if (platform.ContainsKey("CustomTexture"))
                        {
                            CustomTexture = (Dictionary<string, object>)platform["CustomTexture"];
                        }
                        //custom Texture 
                        if (CustomTexture != null && CustomTexture.ContainsKey("CustomTextureName") && CustomTexture.ContainsKey("PixelsPerUnit"))
                        {
                            UseCustomTexture = true;
                        }
                        else
                        {
                            UseCustomTexture = false;
                        }
                        Debug.Log($"UseCustomTexture is {UseCustomTexture}");
                        if (UseCustomTexture)
                        {
                            float PixelsPerUnit = (float)Convert.ToDouble(CustomTexture["PixelsPerUnit"]);
                            CustomTextureName = (String)CustomTexture["CustomTextureName"];
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
                        if (platform.ContainsKey("CustomDrillColorAndBolderTexture"))
                        {
                            UseCustomDrillColorAndBolderTexture = true;
                        }
                        if (UseCustomDrillColorAndBolderTexture)
                        {
                            var CustomDrillColorAndBolderTexture = (Dictionary<string, object>)platform["CustomDrillColorAndBolderTexture"];
                            //get drill colors dict to pass.
                            var dict = (Dictionary<string, object>)CustomDrillColorAndBolderTexture["CustomDrillColors"];
                            //if this platform fails to generate then the custom boulder texsters will get mixed up.
                            var MyPlatformId = NextPlatformTypeValue;
                            NextPlatformTypeValue = NextPlatformTypeValue + 1;
                            Debug.Log("creating drill colors");
                            var colors = DrillColors(MyPlatformId, dict);
                            Debug.Log("drill colors created");
                            platformType = (PlatformType)MyPlatformId;
                            CustomDrillColors.Add(colors);
                            //custom Boulder time
                            float PixelsPerUnit = (float)Convert.ToDouble(CustomDrillColorAndBolderTexture["BoulderPixelsPerUnit"]);
                            CustomTextureName = (String)CustomDrillColorAndBolderTexture["CustomBoulderTexture"];
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
                            var BoulderSmokeColorList = ListOfObjectsToListOfFloats((List<object>)CustomDrillColorAndBolderTexture["BoulderSmokeColor"]);
                            UnityEngine.Color BoulderSmokeColor = new UnityEngine.Color(BoulderSmokeColorList[0], BoulderSmokeColorList[1], BoulderSmokeColorList[2], BoulderSmokeColorList[3]);
                            CustomBoulderSmokeColors.Add(BoulderSmokeColor);
                        }
                        if (CustomTexture != null && CustomTexture.ContainsKey("UseSlimeCam"))
                        {
                            UseSlimeCam = (bool)CustomTexture["UseSlimeCam"];
                        }
                        Vector4[] color2 = { color };
                        Vec2[] centerPoint2 = { centerPoint };
                        //spawn platform
                        var PlatformObject = PlatformApi.PlatformApi.SpawnPlatform((Fix)x, (Fix)y, (Fix)width, (Fix)height, (Fix)radius, (Fix)rotatson, CustomMassScale, color2, platformType, UseSlimeCam, sprite, pathType, OrbitForce, OrbitPath, DelaySeconds, isBird, orbitSpeed, expandSpeed, centerPoint2, normalSpeedFriction, DeadZoneDist, OrbitAccelerationMulitplier, targetRadius, ovalness01);
                        PlatformObject.GetComponent<WaterWaves>().WavePrefab = WavePrefab;
                        //signal time!
                        if (platform.ContainsKey("MovingPlatformSignalStuff"))
                        {
                            var movingPlatformSignalStuff = (Dictionary<string, object>)platform["MovingPlatformSignalStuff"];
                            var UUID = Convert.ToInt32(movingPlatformSignalStuff["InputUUID"]);
                            AddMovingPlatformSignalStuff(PlatformObject, UUID);
                        }
                        if (platform.ContainsKey("DisappearPlatformOnSignal"))
                        {
                            var disappearPlatformOnSignal = (Dictionary<string, object>)platform["DisappearPlatformOnSignal"];
                            var UUID = Convert.ToInt32(disappearPlatformOnSignal["InputUUID"]);
                            var SecondsToReapper = Convert.ToDouble(disappearPlatformOnSignal["SecondsToReapper"]);
                            var Delay = Convert.ToDouble(disappearPlatformOnSignal["Delay"]);
                            CreateDisappearPlatformsOnSignal(PlatformObject, UUID, (Fix)SecondsToReapper, (Fix)Delay);
                        }
                        if (platform.ContainsKey("ShakePlatform"))
                        {
                            var shakePlatform = (Dictionary<string, object>)platform["ShakePlatform"];
                            var UUID = Convert.ToInt32(shakePlatform["InputUUID"]);
                            var Duration = Convert.ToDouble(shakePlatform["Duration"]);
                            var OnlyActivateOnRise = Convert.ToBoolean(shakePlatform["OnlyActivateOnRise"]);
                            var ShakeAmount = Convert.ToDouble(shakePlatform["ShakeAmount"]);
                            CreateShakePlatform(PlatformObject, UUID, (Fix)Duration, OnlyActivateOnRise, (Fix)ShakeAmount);
                        }
                        if (platform.ContainsKey("DropPlayers"))
                        {
                            var DropPlayers = (Dictionary<string, object>)platform["DropPlayers"];
                            var UUID = Convert.ToInt32(DropPlayers["InputUUID"]);
                            var OnlyActivateOnRise = Convert.ToBoolean(DropPlayers["OnlyActivateOnRise"]);
                            var DropForce = Convert.ToDouble(DropPlayers["DropForce"]);
                            CreateDropPlayers(PlatformObject, UUID, (Fix)DropForce, OnlyActivateOnRise);
                        }
                        Debug.Log("Platform spawned successfully");
                    }

                    // if it is a preset platform then we do it difrently
                    else
                    {
                        Dictionary<string, object> PresetPlatform = (Dictionary<string, object>)platform["PresetPlatform"];
                        string PresetPlatformName = Convert.ToString(PresetPlatform["PresetPlatformName"]);
                        var Platform = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/" + PresetPlatformName + ".prefab");
                        //idk if this is gonna work to fix the posable desink as it is converted back to a float...
                        var x2 = FloorToThousandnths(x);
                        var y2 = FloorToThousandnths(y);
                        Vector3 pos = new Vector3((float)x2, (float)y2, 0);
                        //the rest of the FloorToThousandnths should work fine for fixing it though
                        //fix shader
                        Platform.GetComponent<SpriteRenderer>().material = PlatformMat;
                        //set home
                        PlatformApi.PlatformApi.SetHome(Platform, (Vec2)pos);
                        BoplBody body = Platform.GetComponent<BoplBody>();
                        //make the space junk not start with rotatsonal velosity
                        body.StartAngularVelocity = Fix.Zero;
                        Platform.GetComponent<WaterWaves>().WavePrefab = WavePrefab;
                        //scale object
                        var ScaleFactor = FloorToThousandnths(Convert.ToDouble(PresetPlatform["ScaleFactor"]));
                        if (Platform.GetComponent<GrowOnStart>() == null)
                        {
                            var GrowOnStartComp = Platform.AddComponent(typeof(GrowOnStart)) as GrowOnStart;
                            GrowOnStartComp.scaleUp = ScaleFactor;
                            //Debug.Log("added GrowOnStart");
                        }
                        else
                        {
                            Platform.GetComponent<GrowOnStart>().scaleUp = ScaleFactor;
                        }
                        if (ScaleFactor < Platform.GetComponent<DPhysicsRoundedRect>().MinScale)
                        {
                            Platform.GetComponent<DPhysicsRoundedRect>().MinScale = ScaleFactor;
                        }
                        if (ScaleFactor > Platform.GetComponent<DPhysicsRoundedRect>().MaxScale)
                        {
                            Platform.GetComponent<DPhysicsRoundedRect>().MaxScale = ScaleFactor;
                        }
                        //spawn object
                        //Debug.Log($"pos is {pos}");
                        Platform = UnityEngine.Object.Instantiate<GameObject>(Platform, pos, Quaternion.identity);
                        PlatformApi.PlatformApi.SetPos(Platform, (Vec2)pos);
                        //rotate object
                        StickyRoundedRectangle StickyRect = Platform.GetComponent<StickyRoundedRectangle>();
                        StickyRect.GetGroundBody().rotation = FloorToThousandnths((double)rotatson);
                        //fix mats
                        foreach (Transform t in Platform.transform)
                        {
                            //if its a grass
                            if (t.gameObject.name == "AnimatedGrass_0" || t.gameObject.name == "AnimatedGrass_0 (2)" || t.gameObject.name == "AnimatedGrass_0 (3)" || t.gameObject.name == "AnimatedGrass")
                            {
                                //set its mat
                                t.gameObject.GetComponent<SpriteRenderer>().material = GrassMat;
                            }
                        }
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
                        if (platform.ContainsKey("MovingPlatformSignalStuff"))
                        {
                            var movingPlatformSignalStuff = (Dictionary<string, object>)platform["MovingPlatformSignalStuff"];
                            var UUID = Convert.ToInt32(movingPlatformSignalStuff["InputUUID"]);
                            AddMovingPlatformSignalStuff(Platform, UUID);
                        }
                        if (platform.ContainsKey("DisappearPlatformOnSignal"))
                        {
                            var disappearPlatformOnSignal = (Dictionary<string, object>)platform["DisappearPlatformOnSignal"];
                            var UUID = Convert.ToInt32(disappearPlatformOnSignal["InputUUID"]);
                            var SecondsToReapper = Convert.ToDouble(disappearPlatformOnSignal["SecondsToReapper"]);
                            var Delay = Convert.ToDouble(disappearPlatformOnSignal["Delay"]);
                            CreateDisappearPlatformsOnSignal(Platform, UUID, (Fix)SecondsToReapper, (Fix)Delay);
                        }
                        if (platform.ContainsKey("ShakePlatform"))
                        {
                            var shakePlatform = (Dictionary<string, object>)platform["ShakePlatform"];
                            var UUID = Convert.ToInt32(shakePlatform["InputUUID"]);
                            var Duration = Convert.ToDouble(shakePlatform["Duration"]);
                            var OnlyActivateOnRise = Convert.ToBoolean(shakePlatform["OnlyActivateOnRise"]);
                            var ShakeAmount = Convert.ToDouble(shakePlatform["ShakeAmount"]);
                            CreateShakePlatform(Platform, UUID, (Fix)Duration, OnlyActivateOnRise, (Fix)ShakeAmount);
                        }
                        if (platform.ContainsKey("DropPlayers"))
                        {
                            var DropPlayers = (Dictionary<string, object>)platform["DropPlayers"];
                            var UUID = Convert.ToInt32(DropPlayers["InputUUID"]);
                            var OnlyActivateOnRise = Convert.ToBoolean(DropPlayers["OnlyActivateOnRise"]);
                            var DropForce = Convert.ToDouble(DropPlayers["DropForce"]);
                            CreateDropPlayers(Platform, UUID, (Fix)DropForce, OnlyActivateOnRise);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to spawn platform. Error: {ex}");
                }
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
                //remove all shootrays that are still around as they dont like unloading when the scene unloads for some reson.
                ShootRay[] allRays = Resources.FindObjectsOfTypeAll(typeof(ShootRay)) as ShootRay[];
                foreach (var Ray in allRays)
                {
                    Destroy(Ray.gameObject);
                    Destroy(Ray);
                }
            }
            if (IsLevelName(scene.name) && MapJsons.Length != 0)
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
                    spawnerGameObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    spawnerGameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    var spriteRender = spawnerGameObject.AddComponent<SpriteRenderer>();
                    var SpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/spawnericon.prefab");
                    spriteRender.sprite = SpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject DisappearGameObject = new GameObject("DisappearPlatformsObject");

                    // Add the components to the GameObject
                    DisappearGameObject.AddComponent<FixTransform>();
                    DisappearPlatformsOnSignalPrefab = DisappearGameObject.AddComponent<DisappearPlatformsOnSignal>();
                    //reset this at the begiening of every round.
                    DisappearPlatformsOnSignal.DisappearPlatformsOnSignals = new();
                    DisappearGameObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    DisappearGameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    spriteRender = DisappearGameObject.AddComponent<SpriteRenderer>();
                    SpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/disapearingplatform.prefab");
                    spriteRender.sprite = SpriteGameObject.GetComponent<SpriteRenderer>().sprite;
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
                    var AndGateSpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/andgate.prefab");
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
                    var SignalDelaySpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/delaygate.prefab");
                    SignalDelaySpriteRender.sprite = SignalDelaySpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject OrGateObject = new GameObject("OrGateObject");
                    // Add the components to the GameObject
                    OrGateObject.AddComponent<FixTransform>();
                    //put it offscreen
                    OrGateObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    OrGateObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    OrGatePrefab = OrGateObject.AddComponent<OrGate>();
                    var OrGateSpriteRender = OrGateObject.AddComponent<SpriteRenderer>();
                    var OrGateSpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/orgate.prefab");
                    OrGateSpriteRender.sprite = OrGateSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject NotGateObject = new GameObject("NotGateObject");
                    // Add the components to the GameObject
                    NotGateObject.AddComponent<FixTransform>();
                    //put it offscreen
                    NotGateObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    NotGateObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    NotGatePrefab = NotGateObject.AddComponent<NotGate>();
                    var NotGateSpriteRender = NotGateObject.AddComponent<SpriteRenderer>();
                    var NotGateSpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/notgate.prefab");
                    NotGateSpriteRender.sprite = NotGateSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject ShootRayGameObject = new GameObject("ShootRayObject");
                    // Add the components to the GameObject
                    ShootRayGameObject.AddComponent<FixTransform>();
                    ShootRayGameObject.AddComponent<ShootBlink>();
                    ShootRayPrefab = ShootRayGameObject.AddComponent<ShootRay>();
                    spriteRender.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    ShootRayGameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    spriteRender = ShootRayGameObject.AddComponent<SpriteRenderer>();


                    // Create a new GameObject
                    GameObject DropPlayersGameObject = new GameObject("DropPlayersObject");
                    // Add the components to the GameObject
                    DropPlayersGameObject.AddComponent<FixTransform>();
                    DropPlayersPrefab = DropPlayersGameObject.AddComponent<DropPlayers>();
                    DropPlayersGameObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    DropPlayersGameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    spriteRender = DropPlayersGameObject.AddComponent<SpriteRenderer>();
                    SpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/playerdrop.prefab");
                    spriteRender.sprite = SpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject ShakePlatformGameObject = new GameObject("ShakePlatformObject");
                    // Add the components to the GameObject
                    ShakePlatformGameObject.AddComponent<FixTransform>();
                    ShakePlatformPrefab = ShakePlatformGameObject.AddComponent<ShakePlatform>();
                    ShakePlatformGameObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    ShakePlatformGameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    var ShakePlatformRender = ShakePlatformGameObject.AddComponent<SpriteRenderer>();
                    var ShakePlatformSpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/shaker.prefab");
                    ShakePlatformRender.sprite = ShakePlatformSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject LuaGameObject = new GameObject("LuaTestObject");
                    // Add the components to the GameObject
                    LuaGameObject.AddComponent<FixTransform>();
                    LuaPrefab = LuaGameObject.AddComponent<LuaMain>();
                    LuaGameObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    LuaGameObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    var LuaRender = LuaGameObject.AddComponent<SpriteRenderer>();
                    var LuaSpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/luagate.prefab");
                    LuaRender.sprite = LuaSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject SignalSystemObject = new GameObject("SignalSystemObject");


                    // Add the components to the GameObject
                    SignalSystemObject.AddComponent<FixTransform>();
                    signalSystem = SignalSystemObject.AddComponent<SignalSystem>();
                    SignalSystem.LogicInputs = new List<LogicInput>();
                    SignalSystem.LogicOutputs = new List<LogicOutput>();
                    SignalSystem.LogicStartingOutputs = new List<LogicOutput>();
                    SignalSystem.LogicGatesToAlwaysUpdate = new List<LogicGate>();
                    SignalSystem.LineRenderers = new();
                    SignalSystem.LogicInputsThatAlwaysUpdateThereLineConnectsons = new();
                    SignalSystem.FirstUpdateOfTheRound = true;
                    SignalSystem.AllLogicGates = new();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in spawning triggers/spawners at scene load: {ex}");
                }
                NextUUID = 0;
                //TODO remove this when done testing signal stuff
                //TESTING START!
                Vec2[] path = { new Vec2(Fix.Zero, (Fix)10), new Vec2((Fix)10, (Fix)10) };
                Vec2[] center = { new Vec2((Fix)0, (Fix)15) };
                //var platform = PlatformApi.PlatformApi.SpawnPlatform((Fix)0, (Fix)10, (Fix)2, (Fix)2, (Fix)1, Fix.Zero, 0.05, null, PlatformType.slime, false, null, PlatformApi.PlatformApi.PathType.VectorFieldPlatform, 500, path, 0, false, 100, 100, center);
                //CreateTrigger(new Vec2((Fix)(-10), (Fix)30), new Vec2((Fix)10, (Fix)10), 0, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true);
                //CreateTrigger(new Vec2((Fix)10, (Fix)30), new Vec2((Fix)10, (Fix)10), 1, true);
                int[] UUids = { 4, 0 };
                //CreateOrGate(UUids, 6, new Vec2(Fix.Zero, (Fix)5), (Fix)0);
                //CreateNotGate(6, 2, new Vec2((Fix)5, (Fix)5), (Fix)0);
                int[] UUids2 = { 1, 5 };
                //CreateOrGate(UUids2, 7, new Vec2(Fix.Zero, (Fix)(-5)), (Fix)0);
                //CreateNotGate(7, 3, new Vec2((Fix)5, (Fix)(-5)), (Fix)0);
                //CreateSignalDelay(2, 5, (Fix)0, new Vec2((Fix)2, (Fix)(-2)), (Fix)180);
                //CreateSignalDelay(3, 4, (Fix)0, new Vec2((Fix)2, (Fix)(2)), (Fix)180);
                //AddMovingPlatformSignalStuff(platform, 2);
                //CreateDisappearPlatformsOnSignal(platform, 3, Fix.Zero, (Fix)2, false);
                //CreateShakePlatform(platform, 2, (Fix)0.5, true, (Fix)1);
                //CreateDropPlayers(platform, 2, (Fix)100, true);
                int[] UUids3 = { };
                int[] UUids4 = { };
                /*CreateLuaGate(UUids3, UUids4, new Vec2((Fix)10, (Fix)(10)), (Fix)0, @"
if (not first) then
    SpawnPlatform(0, 0, 1, 1, 1, 90, 1, 0, 0, 1)
end
first = true");*/
                //CreateShootBlink(3, new Vec2((Fix)(0), (Fix)20), (Fix)90, (Fix)360, (Fix)1, (Fix)1, (Fix)3, (Fix)2.5);
                //CreateShootGrow(3, new Vec2((Fix)(-30), (Fix)20), (Fix)90, (Fix)360, (Fix)50, (Fix)(0.4), (Fix)0.4);
                //CreateShootStrink(3, new Vec2((Fix)(30), (Fix)20), (Fix)90, (Fix)0, (Fix)(-500), (Fix)(-0.4), (Fix)(-0.4));
                //CreateSpawner(new Vec2((Fix)(0), (Fix)20), (Fix)1, Vec2.zero, Fix.Zero, new UnityEngine.Color(0, 1, 0, 0.5f), Spawner.ObjectSpawnType.Boulder, PlatformType.slime, true, 8, false);
                //MAKE SURE TO CALL THIS WHEN DONE CREATING SIGNAL STUFF!
                //signalSystem.SetUpDicts();
                //Debug.Log("signal stuff is done!");
                //TESTING END!
                /*var DoWeHaveMapWithMapId = CheckIfWeHaveCustomMapWithMapId();
                //error if there are multiple maps with the same id
                if (DoWeHaveMapWithMapId == MapIdCheckerThing.MultipleMapsFoundWithId)
                {
                    Debug.LogError($"ERROR! MULTIPLE MAPS WITH MAP UUID: {CurrentMapUUID} FOUND! UHAFYIGGAFYAIO");
                    return;
                }
                else
                {
                    if (DoWeHaveMapWithMapId == MapIdCheckerThing.NoMapFoundWithId)
                    {
                        Debug.Log("no custom map found for this map");
                        signalSystem.SetUpDicts();
                        return;
                    }
                }*/
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
                        WavePrefab = tplatform.gameObject.GetComponent<WaterWaves>().WavePrefab;
                    }
                    index++;
                    //distroy it
                    Updater.DestroyFix(tplatform.gameObject);
                }
                LoadMapsFromFolder();
                signalSystem.SetUpDicts();
                Debug.Log("signal stuff is done!");
            }
        }
        public static bool IsLevelName(String input)
        {
            Regex regex = new Regex("Level[0-9]+", RegexOptions.IgnoreCase);
            return regex.IsMatch(input);
        }
        //https://stormconsultancy.co.uk/blog/storm-news/convert-an-angle-in-degrees-to-radians-in-c/
        public static Fix ConvertToRadians(double angle)
        {
            return (Fix)PhysTools.DegreesToRadians * (Fix)angle;
        }
        //https://stackoverflow.com/questions/19167669/keep-only-numeric-value-from-a-string
        // simply replace the offending substrings with an empty string
        public static int GetMapIdFromSceneName(string s)
        {
            Regex rxNonDigits = new Regex(@"[^\d]+");
            if (string.IsNullOrEmpty(s)) return 0;
            string cleaned = rxNonDigits.Replace(s, "");
            //subtract 1 as scene names start with 1 but ids start with 0
            return int.Parse(cleaned) - 1;
        }
        //in part chatgpt code
        public static ZipArchive UnzipFile(string zipFilePath)
        {

            // Open the zip file for reading
            FileStream zipStream = new FileStream(zipFilePath, FileMode.Open);
            ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
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
                //Debug.Log("check if its a drectory");
                if (entry.FullName.EndsWith("/"))
                    continue;
                //Debug.Log("it isnt a drectory");
                //see if it is valid (if the predicate returns true)
                string[] path = { entry.FullName };
                string[] ValidPathArray = Array.FindAll(path, predicate);
                if (ValidPathArray.Length != 0)
                {
                    //Debug.Log("about to read the contents of entry");
                    // Read the contents of the entry
                    using (StreamReader reader = new StreamReader(entry.Open()))
                    {
                        //Debug.Log("reading the contents of entry");
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
                //Debug.Log("check if its a drectory");
                if (entry.FullName.EndsWith("/"))
                    continue;
                //Debug.Log("it isnt a drectory");
                //see if it is valid (if the predicate returns true)
                string[] path = { entry.FullName };
                string[] ValidPathArray = Array.FindAll(path, predicate);
                if (ValidPathArray.Length != 0)
                {
                    //Debug.Log("about to read the contents of entry");
                    // Read the contents of the entry
                    using (var entryStream = entry.Open())
                    using (var memoryStream = new MemoryStream())
                    {
                        //Debug.Log("reading the contents of entry");
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
            MyZipArchives = zipArchives;
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
        public static int RandomBagLevel()
        {
            var TimesToGoForwords = UnityEngine.Random.Range(0, Plugin.MapJsons.Length - MapIndexsUsed.Count);
            //if we used all of them reset it.
            if (Plugin.MapJsons.Length == MapIndexsUsed.Count)
            {
                MapIndexsUsed = new();
            }
            byte Index = 0;
            //we start at index 0
            while ((int)Index < Plugin.MapJsons.Length)
            {
                //if we havent used the id Index yet
                if (!MapIndexsUsed.Contains(Index))
                {
                    //if we have finished going forwords
                    if (TimesToGoForwords == 0)
                    {
                        //we add the index and return the new map id
                        MapIndexsUsed.Add(Index);
                        return Index;
                    }
                    //if we havent finished going forwords then we go back 1
                    TimesToGoForwords -= 1;
                }
                //incress the index by 1 so its not a infanent loop
                Index += 1;
            }
            //this will never happen
            throw new Exception("random bag broke");
        }
        public static Spawner CreateSpawner(Vec2 Pos, Fix SimTimeBetweenSpawns, Vec2 SpawningVelocity, Fix angularVelocity, UnityEngine.Color color, Spawner.ObjectSpawnType spawnType = Spawner.ObjectSpawnType.None, PlatformType BoulderType = PlatformType.grass, bool UseSignal = false, int Signal = 0, bool IsTriggerSignal = false)
        {
            var spawner = FixTransform.InstantiateFixed<Spawner>(SpawnerPrefab, Pos);
            spawner.spawnType = spawnType;
            spawner.UseSignal = UseSignal;
            if (UseSignal)
            {
                var input = new LogicInput
                {
                    UUid = Signal,
                    gate = spawner,
                    IsOn = false,
                    Owner = spawner.gameObject
                };
                spawner.InputSignals.Add(input);
            }
            spawner.IsTriggerSignal = IsTriggerSignal;
            spawner.BoulderType = BoulderType;
            spawner.SimTimeBetweenSpawns = SimTimeBetweenSpawns;
            spawner.velocity = SpawningVelocity;
            spawner.angularVelocity = angularVelocity;
            spawner.ArrowOrBoulderColor = color;
            spawner.Register();
            return spawner;
        }
        public static Trigger CreateTrigger(Vec2 Pos, Vec2 Extents, int Signal, bool visable, bool DettectAbilityOrbs, bool DettectArrows, bool DettectBlackHole, bool DettectBoulders, bool DettectEngine, bool DettectGrenades, bool DettectMine, bool DettectMissle, bool DettectPlatforms, bool DettectPlayers, bool DettectSmoke, bool DettectSmokeGrenade, bool DettectSpike, bool DettectTesla)
        {

            var trigger = FixTransform.InstantiateFixed<Trigger>(TriggerPrefab, new Vec2(Fix.Zero, (Fix)30));
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
            trigger.Visable = visable;
            trigger.DettectAbilityOrbs = DettectAbilityOrbs;
            trigger.DettectArrows = DettectArrows;
            trigger.DettectBlackHole = DettectBlackHole;
            trigger.DettectBoulders = DettectBoulders;
            trigger.DettectEngine = DettectEngine;
            trigger.DettectGrenades = DettectGrenades;
            trigger.DettectMine = DettectMine;
            trigger.DettectMissle = DettectMissle;
            trigger.DettectPlatforms = DettectPlatforms;
            trigger.DettectPlayers = DettectPlayers;
            trigger.DettectSmoke = DettectSmoke;
            trigger.DettectSmokeGrenade = DettectSmokeGrenade;
            trigger.DettectSpike = DettectSpike;
            trigger.DettectTesla = DettectTesla;
            trigger.Register();
            return trigger;
        }
        public static DisappearPlatformsOnSignal CreateDisappearPlatformsOnSignal(GameObject platform, int Signal, Fix SecondsToReapper, Fix delay, bool SignalIsInverse = false)
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
        public static ShootRay CreateShootBlink(int InputUUid, Vec2 pos, Fix rot, Fix VarenceInDegrees, Fix BlinkWallDelay, Fix BlinkMinPlayerDuration, Fix BlinkWallDuration, Fix BlinkWallShake)
        {
            var shootRay = FixTransform.InstantiateFixed<ShootRay>(ShootRayPrefab, pos, (Fix)ConvertToRadians((double)rot));
            //icon
            var spriteRender = shootRay.GetComponent<SpriteRenderer>();
            var SpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/blinkemitter.prefab");
            spriteRender.sprite = SpriteGameObject.GetComponent<SpriteRenderer>().sprite;

            var input = new LogicInput
            {
                UUid = InputUUid,
                gate = shootRay,
                IsOn = false,
                Owner = shootRay.gameObject
            };
            shootRay.InputSignals.Add(input);
            shootRay.rayType = ShootRay.RayType.Blink;
            shootRay.VarenceInDegrees = VarenceInDegrees;
            shootRay.BlinkWallDelay = BlinkWallDelay;
            shootRay.BlinkMinPlayerDuration = BlinkMinPlayerDuration;
            shootRay.BlinkWallDuration = BlinkWallDuration;
            shootRay.BlinkWallShake = BlinkWallShake;
            shootRay.Register();
            return shootRay;
        }
        public static ShootRay CreateShootGrow(int InputUUid, Vec2 pos, Fix rot, Fix VarenceInDegrees, Fix blackHoleGrowth, Fix ScaleMultiplyer, Fix PlayerMultiplyer)
        {
            var shootRay = FixTransform.InstantiateFixed<ShootRay>(ShootRayPrefab, pos, (Fix)ConvertToRadians((double)rot));
            //icon stuff
            var spriteRender = shootRay.GetComponent<SpriteRenderer>();
            var SpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/growemitter.prefab");
            spriteRender.sprite = SpriteGameObject.GetComponent<SpriteRenderer>().sprite;

            var input = new LogicInput
            {
                UUid = InputUUid,
                gate = shootRay,
                IsOn = false,
                Owner = shootRay.gameObject
            };
            shootRay.InputSignals.Add(input);
            shootRay.rayType = ShootRay.RayType.Grow;
            shootRay.VarenceInDegrees = VarenceInDegrees;
            shootRay.blackHoleGrowth = blackHoleGrowth;
            shootRay.ScaleMultiplyer = ScaleMultiplyer;
            shootRay.PlayerMultiplyer = PlayerMultiplyer;
            shootRay.smallNonPlayersMultiplier = ScaleMultiplyer;
            shootRay.Register();
            return shootRay;
        }
        public static ShootRay CreateShootStrink(int InputUUid, Vec2 pos, Fix rot, Fix VarenceInDegrees, Fix blackHoleGrowth, Fix ScaleMultiplyer, Fix PlayerMultiplyer)
        {
            var shootRay = FixTransform.InstantiateFixed<ShootRay>(ShootRayPrefab, pos, (Fix)ConvertToRadians((double)rot));
            //icon stuff
            var spriteRender = shootRay.GetComponent<SpriteRenderer>();
            var SpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/shrinkemitter.prefab");
            spriteRender.sprite = SpriteGameObject.GetComponent<SpriteRenderer>().sprite;
            var input = new LogicInput
            {
                UUid = InputUUid,
                gate = shootRay,
                IsOn = false,
                Owner = shootRay.gameObject
            };
            shootRay.InputSignals.Add(input);
            shootRay.rayType = ShootRay.RayType.Shrink;
            shootRay.VarenceInDegrees = VarenceInDegrees;
            shootRay.blackHoleGrowth = blackHoleGrowth;
            shootRay.ScaleMultiplyer = ScaleMultiplyer;
            shootRay.PlayerMultiplyer = PlayerMultiplyer;
            shootRay.smallNonPlayersMultiplier = ScaleMultiplyer;
            shootRay.Register();
            return shootRay;
        }
        public static ShakePlatform CreateShakePlatform(GameObject platform, int InputUUid, Fix duration, bool OnlyActivateOnRise, Fix shakeAmount)
        {
            var shakePlatform = FixTransform.InstantiateFixed<ShakePlatform>(ShakePlatformPrefab, (Vec2)platform.transform.position);
            var input = new LogicInput
            {
                UUid = InputUUid,
                gate = shakePlatform,
                IsOn = false,
                Owner = shakePlatform.gameObject
            };
            shakePlatform.InputSignals.Add(input);
            shakePlatform.duration = duration;
            shakePlatform.shakablePlatform = platform.GetComponent<ShakablePlatform>();
            shakePlatform.OnlyActivateOnRise = OnlyActivateOnRise;
            shakePlatform.shakeAmount = shakeAmount;
            shakePlatform.Register();
            return shakePlatform;
        }
        public static DropPlayers CreateDropPlayers(GameObject platform, int InputUUid, Fix DropForce, bool OnlyActivateOnRise)
        {
            var dropPlayers = FixTransform.InstantiateFixed<DropPlayers>(DropPlayersPrefab, (Vec2)platform.transform.position);
            var input = new LogicInput
            {
                UUid = InputUUid,
                gate = dropPlayers,
                IsOn = false,
                Owner = dropPlayers.gameObject
            };
            dropPlayers.InputSignals.Add(input);
            dropPlayers.DropForce = DropForce;
            dropPlayers.stickyRoundedRectangle = platform.GetComponent<StickyRoundedRectangle>();
            dropPlayers.OnlyActivateOnRise = OnlyActivateOnRise;
            dropPlayers.Register();
            return dropPlayers;
        }
        public static LuaMain CreateLuaGate(int[] InputUUids, int[] OutputUUids, Vec2 pos, Fix rot, string LuaCode, int ZipIndex)
        {
            var Lua = FixTransform.InstantiateFixed<LuaMain>(LuaPrefab, pos, (Fix)ConvertToRadians((double)rot));
            var LogicInputs = new List<LogicInput>();
            foreach (var InputSignal in InputUUids)
            {
                var input = new LogicInput
                {
                    UUid = InputSignal,
                    gate = Lua,
                    IsOn = false,
                    Owner = Lua.gameObject
                };
                LogicInputs.Add(input);
            }
            var LogicOutputs = new List<LogicOutput>();
            foreach (var OutputSignal in OutputUUids)
            {
                var output = new LogicOutput
                {
                    UUid = OutputSignal,
                    gate = Lua,
                    IsOn = false,
                    Owner = Lua.gameObject
                };
                LogicOutputs.Add(output);
            }
            Lua.InputSignals.AddRange(LogicInputs);
            Lua.OutputSignals.AddRange(LogicOutputs);
            Lua.code = LuaCode;
            Lua.ZipIndex = ZipIndex;
            Lua.Register();
            return Lua;
        }
        //lua stuff
        public static DynValue exec1(CallbackArguments args, string funcName, Func<double, double> func, MoonSharp.Interpreter.CoreLib.MathModule __instance)
        {
            return MoonSharp.Interpreter.CoreLib.MathModule.exec1(args, funcName, func);
        }
        public static DynValue exec2(CallbackArguments args, string funcName, Func<double, double, double> func, MoonSharp.Interpreter.CoreLib.MathModule __instance)
        {
            return MoonSharp.Interpreter.CoreLib.MathModule.exec2(args, funcName, func);
        }
        public static DynValue exec2n(CallbackArguments args, string funcName, double defVal, Func<double, double, double> func, MoonSharp.Interpreter.CoreLib.MathModule __instance)
        {
            return MoonSharp.Interpreter.CoreLib.MathModule.exec2n(args, funcName, defVal, func);
        }
        public static DynValue execaccum(CallbackArguments args, string funcName, Func<double, double, double> func, MoonSharp.Interpreter.CoreLib.MathModule __instance)
        {
            return MoonSharp.Interpreter.CoreLib.MathModule.execaccum(args, funcName, func);
        }
        public static Fix Tanh(Fix d)
        {
            var cosh = (Fix.Pow((Fix)2.718281828459045, (Fix)(d)) + Fix.Pow((Fix)2.718281828459045, (Fix)(-d))) / (Fix)2;
            var sinh = (Fix.Pow((Fix)2.718281828459045, (Fix)d) - Fix.Pow((Fix)2.718281828459045, (Fix)(-d))) / (Fix)2;
            return sinh / cosh;
        }
    }
}