using BepInEx;
using BepInEx.Logging;
using BoplFixedMath;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;
using System.Linq;
using System.IO.Compression;
using UnityEngine.UI;
using MapMaker.Lua_stuff;
using MoonSharp.Interpreter;
using UnityEngine.Events;
using System.Collections;
using static MapMaker.PipeStuff;
using TMPro;
using MapMaker.utils;
using System.Data;
namespace MapMaker
{
    [BepInDependency("com.entwinedteam.entwined")]
    [BepInPlugin("com.MLT.MapLoader", "MapLoader", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {   
        // Developer mode! PLEASE TURN OFF BEFORE BUILDING!
        public static bool DeveloperMode = false;

        public static Plugin instance;
        public static Harmony harmony;
        public static GameObject PlatformAbility;
        public static Transform levelt;
        public static StickyRoundedRectangle platformPrefab;
        public static ParticleSystem WavePrefab;
        public static List<ResizablePlatform> Platforms;
        public static int t;
        public static string mapsFolderPath; 
        public static int CurrentMapUUID;
        public static int CurrentMapIndex;
        public static Fix OneByOneBlockMass = Fix.One;
        public static string[] MapJsons;
        public static string[] MetaDataJsons;
        // Define a static logger instance SOMEONE PLEASE MAKE A LOGGER LATER!
        public static ManualLogSource logger;
        public static bool UseCustomTexture = false;
        public static string CustomTextureName;
        // All the zipArchives in the same order as the MapJsons
        public static ZipArchive[] zipArchives = { };
        // My zip archives. not overiten when joining someone else.
        public static ZipArchive[] MyZipArchives = { };
        public static Sprite sprite;
        public static Material PlatformMat;
        public static Material GrassMat;
        public static GameObject SlimeCamObject;
        public static List<Drill.PlatformColors> CustomDrillColors;
        public static List<NamedSprite> CustomMatchoManSprites;
        public static int NextPlatformTypeValue = 6;
        public const int StartingNextPlatformTypeValue = 6;
        public static Sprite BoulderSprite;
        //used to make CustomBoulderSmokeColors start with a value.
        public static UnityEngine.Color[] ignore = { new(1, 1, 1, 1) };
        public static List<UnityEngine.Color> CustomBoulderSmokeColors = new(ignore);
        public static AssetBundle MyAssetBundle;
        public static AssetBundle SpriteAssetBundle;
        public static PlatformApi.PlatformApi platformApi = new();
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
        // Used to fix a unity bug
        public static PlayerAverageCamera averageCamera;
        public static ShakableCamera shakableCamera;
        public static List<PlayerInput> playerInputs = new();
        public static bool FirstUpdate = true;
        //used to chose a map from the random bag
        public static List<int> MapIndexsLeft = new();

        // Map Ids
        public static readonly int GrassMapId = 0;
        public static readonly int SnowMapId = 21;
        public static readonly int SpaceMapId = 33;

        // Used by shakeable platform
        public static bool CurrentlyBlinking;
        // Networking
        public static int NextMapIndex;
        // Pipes & Testing
        internal static PipeStuff.PipeResponder pipeResponder;
        // If true it doesnt automaticly reset the map data when entering the singleplayer area. instead it does it when someone joins you.
        public static bool IsInTestMode = false;

        // Used for making the map bigger (replacing all refrences in the main game from scenebounds to this using transpilers)
        public static Fix Camera_XMin = (Fix)(-97.27f);

        public static Fix Camera_XMax = (Fix)97.6f;

        public static Fix Camera_YMax = (Fix)40f;

        private static Fix waterHeight = (Fix)(-11.3f);

        private static Fix spaceWaterHeight = (Fix)(-50f);

        public static Fix Camera_YMin = -(Fix)26L;

        public static Fix BlastZone_XMin = (Fix)(-105L);

        public static Fix BlastZone_XMax = (Fix)105L;

        public static Fix BlastZone_YMax = (Fix)58L;
        // Used to keep the inputs from one level from incorectly being used for a difrent level.
        public static byte CurrentLevelIdForInputsOnlineThingy = 0;
        public enum MapIdCheckerThing
        {
            MapFoundWithId,
            NoMapFoundWithId,
            MultipleMapsFoundWithId
        }

        //public static bool noMapsCheckHasSpawnedText = false;

        public static TextMeshPro maplessText;

        private void Awake()
        {

            // Define the instance
            instance = this;

            // Load the plugin
            NetworkingStuff.Awake();

            Logger.LogInfo("MapLoader Has been loaded");
            harmony = new Harmony("com.MLT.MapLoader");

            // Patch Harmony
            Logger.LogInfo("Harmony harmony = new Harmony -- Melon, 2024");
            logger = Logger;
            harmony.PatchAll(); 

            // Debugging.Awake();
            Logger.LogInfo("MapMaker Patch Compleate!");

            SceneManager.sceneLoaded += OnSceneLoaded;

            // Create the maps directory if it doesn't exist
            mapsFolderPath = Path.Combine(Paths.PluginPath, "Maps");

            if (!Directory.Exists(mapsFolderPath))
            {
                Directory.CreateDirectory(mapsFolderPath);
                Debug.Log("Maps folder created.");
            }
            PluginPath = Info.Location;

            // Load the asset bundle
            // Thanks almafa64 on discord for help.
            MyAssetBundle = AssetBundle.LoadFromFile(Path.GetDirectoryName(Info.Location) + "/mapmakerassets");
            string[] assetNames = MyAssetBundle.GetAllAssetNames();
            SpriteAssetBundle = AssetBundle.LoadFromFile(Path.GetDirectoryName(Info.Location) + "/mapmakericons");
            if (DeveloperMode) {
                foreach (string name in assetNames)
                {
                    Debug.Log("Asset: " + name + " loaded!");
                }
            }

            // Create the pipe responder
            pipeResponder = new PipeResponder();
            pipeResponder.StartPipe();
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
            Logger.LogWarning("######## Start() called");
            ZipArchive[] zipArchives2;
            // If we are loading a replay the zip archive is already set
            if (!IsReplay())
            {
                //fill the MapJsons array up
                zipArchives2 = GetZipArchives();
            }
            else
            {
                zipArchives2 = zipArchives;
            }
            
            // Create a List for the json for a bit
            List<string> JsonList = [];
            List<string> MetaDataList = [];
            foreach (ZipArchive zipArchive in zipArchives2)
            {
                // Get the first .boplmap file if there is multiple. (THERE SHOULD NEVER BE MULTIPLE .boplmap's IN ONE .zip)
                try
                {
                    JsonList.Add(GetFileFromZipArchive(zipArchive, IsBoplMap)[0]);
                    MetaDataList.Add(GetFileFromZipArchive(zipArchive, IsMetaDataFile)[0]);
                }
                // IndexOutOfRangeException will be thrown if this zip file isn't a bopl map or is corrupt
                catch (IndexOutOfRangeException e)
                {
                    List<ZipArchive> ListzipArchives2 = zipArchives2.ToList();
                    int zipArchiveIndex = Array.IndexOf(zipArchives2, zipArchive) ;
                    ListzipArchives2.Remove(zipArchive);
                    zipArchives2 = ListzipArchives2.ToArray();
                    MyZipArchives = ListzipArchives2.ToArray();
                    Logger.LogWarning("There is an invalid map in your maps folder.");
                }
            }
            if (zipArchives2.Length == 0)
            {
                logger.LogError("NO MAPS LOADED! map maker won\'t function correctly unless you add maps.");
            }
            MapJsons = [.. JsonList];
            MetaDataJsons = [.. MetaDataList];

            // Find the objects
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
            NetworkingStuff.Update();
        }
        private void OnApplicationQuit()
        {
            // Cancel the pipe thread when the application is closing
            PipeResponder._cancellationTokenSource?.Cancel();

            // Now connect to it so it cansules the thread
            var pipe = new NamedPipeClientStream(".", "testpipe", PipeDirection.InOut);
            pipe.Connect(250);
            pipe.Close();
        }
        IEnumerator GetGrassMat()
        {
            // Enter the tutorial
            SceneManager.LoadScene("Tutorial", LoadSceneMode.Single);

            // Wait for the scene to load
            yield return 0;

            // Find the objects
            var grass = GameObject.Find("AnimatedGrass");
            GrassMat = grass.gameObject.GetComponent<SpriteRenderer>().material;

            // Exit the tutorial
            PlayerHandler.Get().PlayerList().Clear();
            TutorialGameHandler.isInTutorial = false;
            Updater.PreLevelLoad();
            SceneManager.LoadScene("MainMenu");
            Updater.PostLevelLoad();
            if (DeveloperMode)
            {
                Debug.Log("Touched Grass!");
            }

        }
        public static bool IsBoplMap(string path)
        {
            return path.EndsWith("boplmap", StringComparison.OrdinalIgnoreCase);
        }
        public static bool IsMetaDataFile(string path)
        {
            return path.EndsWith("MetaData.json");
        }
        
        // Check if there is a custom map we should load (returns enum) (david) (this was annoying to make but at least i learned about predicits!)
        public static MapIdCheckerThing CheckIfWeHaveCustomMapWithMapId()
        {
            int[] MapIds = [];
            foreach (string MetaDataJson in MetaDataJsons)
            {
                try
                {
                    Dictionary<string, object> Dict = MiniJSON.Json.Deserialize(MetaDataJson) as Dictionary<string, object>;
                    // Add it to a array to be checked
                    int mapid = Convert.ToInt32(Dict["MapUUID"]);
                    Debug.Log("Map has MapUUID of " + mapid);
                    MapIds = [.. MapIds, mapid];
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to get MapId from Json: {MetaDataJson} with exception: {ex}");
                }
            }
            // Define a predicit (basically a function that checks if a value meets a critera. in this case being = to CurrentMapId)
            Predicate<int> predicate = ValueEqualsCurrentMapId;

            // Get a list of map ids that match the current map
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
        // Check if value = CurrentMapId. used for CheckIfWeHaveCustomMapWithMapId
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
        // CALL ONLY ON LEVEL LOAD!
        public static void LoadMapsFromFolder()
        {
            // Increment it when a new level is loaded.
            Debug.Log($"New level loaded! level id is now {CurrentLevelIdForInputsOnlineThingy}");
            if (MapJsons.Length != 0)
            {
                var i = CurrentMapIndex;
                var mapJson = MapJsons[CurrentMapIndex];
                try
                {
                    Dictionary<string, object> Meta = MiniJSON.Json.Deserialize(MetaDataJsons[i]) as Dictionary<string, object>;
                    Debug.Log(MetaDataJsons[i]);
                    var mapName = Meta["MapName"] as string;
                    if (Convert.ToInt32(Meta["MapUUID"]) == CurrentMapUUID || IsReplay() || IsInTestMode)
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
                            MoreJsonParceing.SpawnLuaGates((List<object>)Dict["LuaGates"], i);
                        }

                        // display map title
                        TextMeshPro mapTitle = LuaSpawner.SpawnText(new Vec2(Fix.Zero, (Fix)36), Fix.Zero, (Fix)(24/18), mapName, Color.white);
                        mapTitle.gameObject.AddComponent<FadeOutText>();
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
            //spawn point stuff
            if (Dict.ContainsKey("teamSpawns"))
            {
                // Object time! (objects are so confusing)
                // Convert object to list of objects
                List<System.Object> OrbitPathObjects = (List<System.Object>)Dict["teamSpawns"];

                // Now to convert each object in the list to a list of 2 objects
                List<Vec2> Vecs1 = [];
                for (int i = 0; i < OrbitPathObjects.Count; i++)
                {
                    var obj = (List<System.Object>)OrbitPathObjects[i];
                    var floatList = ListOfObjectsToListOfFloats(obj);
                    var floatVec = new Vec2((Fix)floatList[0], FloorToThousandnths(floatList[1]));
                    Vecs1.Add(floatVec);
                }
                Vec2[] Vecs = Vecs1.Count == 1 ? Enumerable.Repeat(Vecs1[0], 4).ToArray() : [.. Vecs1];
                
                // Get the PlayerList
                // Set it to null to avoid using unasigned local var error. it will be assigend when the code runs unless somthing goes very badly.
                GameObject PlayerList = null;
                GameObject[] allObjects = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name == "PlayerList")
                    {
                        // Store its reference
                        PlayerList = obj;
                        Debug.Log("Found the PlayerList");
                        break;
                    }
                    // Level 5 and likely some others have it called this for some reson. (Melon, He probably copied in and accidentily pasted it twice.)
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
                GameSessionHandler.playerSpawns_readonly = Vecs;
            }
            List<object> platforms = (List<object>)Dict["platforms"];

            // Empty the list of Drill colors so the index start at 0 again
            CustomDrillColors = [];
            NextPlatformTypeValue = StartingNextPlatformTypeValue;
            CustomBoulderSmokeColors = new List<UnityEngine.Color>(ignore);
            CustomMatchoManSprites = new List<NamedSprite>();
            foreach (Dictionary<String, object> platform in platforms)
            {
                sprite = null;
                BoulderSprite = null;

                try
                {
                    // Set optional values to null/0/defults 
                    double OrbitForce = 0;
                    Vec2[] OrbitPath = null;
                    double DelaySeconds = 0;
                    double orbitSpeed = 100;
                    double expandSpeed = 100;
                    Vec2 centerPoint = new Vec2(Fix.Zero, Fix.Zero);
                    double normalSpeedFriction = 1;
                    double DeadZoneDist = 1;
                    double OrbitAccelerationMulitplier = 1;
                    double targetRadius = 5;
                    double ovalness01 = 1;
                    Vec2[] teamSpawns = new Vec2[4];
                    Dictionary<string, object> transform = (Dictionary<string, object>)platform["transform"];
                    double x = Convert.ToDouble(transform["x"]);
                    double y = Convert.ToDouble(transform["y"]);
                    Fix rotatson = Fix.Zero;
                    if (platform.ContainsKey("rotation"))
                    {
                        rotatson = ConvertToRadians(Convert.ToDouble(platform["rotation"]));
                    }
                    bool IsPresetPatform = platform.ContainsKey("PresetPlatform");

                    // Paths
                    PlatformApi.PlatformApi.PathType pathType = PlatformApi.PlatformApi.PathType.None;
                    if (platform.ContainsKey("AntiLockPlatform"))
                    {
                        pathType = PlatformApi.PlatformApi.PathType.AntiLockPlatform;
                    }
                    if (platform.ContainsKey("VectorFieldPlatform"))
                    {
                        pathType = PlatformApi.PlatformApi.PathType.VectorFieldPlatform;
                    }
                    // AntiLockPlatform (Moving platforms)
                    if (pathType == PlatformApi.PlatformApi.PathType.AntiLockPlatform)
                    {
                        var AntiLockPlatform = (Dictionary<string, object>)platform["AntiLockPlatform"];
                        OrbitForce = Convert.ToDouble(AntiLockPlatform["OrbitForce"]);
                        // Object time! (objects are so confusing) (Melon, Really david? A second time?)
                        // Convert object to list of objects
                        List<System.Object> OrbitPathObjects = (List<System.Object>)AntiLockPlatform["OrbitPath"];

                        // Now to convert each object in the list to a list of 2 objects
                        List<Vec2> Vecs1 = [];
                        for (int i = 0; i < OrbitPathObjects.Count; i++)
                        {
                            var obj = (List<System.Object>)OrbitPathObjects[i];
                            var floatList = ListOfObjectsToListOfFloats(obj);
                            var floatVec = new Vec2(FloorToThousandnths(floatList[0]), FloorToThousandnths(floatList[1]));
                            Vecs1.Add(floatVec);
                        }
                        Vec2[] Vecs = [.. Vecs1];
                        // Debug.Log("orbit path decoded");

                        // Now we have a Vec2 array of the path
                        OrbitPath = Vecs;
                        DelaySeconds = Convert.ToDouble(AntiLockPlatform["DelaySeconds"]);
                    }
                    if (pathType == PlatformApi.PlatformApi.PathType.VectorFieldPlatform)
                    {
                        var VectorFieldPlatform = (Dictionary<string, object>)platform["VectorFieldPlatform"];
                        if (VectorFieldPlatform.TryGetValue("expandSpeed", out object expandSpeedObj))
                        {
                            expandSpeed = Convert.ToDouble(expandSpeedObj);
                        }
                        if (VectorFieldPlatform.TryGetValue("centerPoint", out object centerPointObj))
                        {
                            var floats = ListOfObjectsToListOfFloats((List<object>)centerPointObj);
                            centerPoint = new Vec2(FloorToThousandnths(floats[0]), FloorToThousandnths(floats[1]));
                        }
                        if (VectorFieldPlatform.TryGetValue("normalSpeedFriction", out object normalSpeedFrictionObj))
                        {
                            normalSpeedFriction = Convert.ToDouble(normalSpeedFrictionObj);
                        }
                        if (VectorFieldPlatform.TryGetValue("DeadZoneDist", out object DeadZoneDistObj))
                        {
                            DeadZoneDist = Convert.ToDouble(DeadZoneDistObj);
                        }
                        if (VectorFieldPlatform.TryGetValue("OrbitAccelerationMulitplier", out object OrbitAccelerationMulitplierObj))
                        {
                            OrbitAccelerationMulitplier = Convert.ToDouble(OrbitAccelerationMulitplierObj);
                        }
                        if (VectorFieldPlatform.TryGetValue("targetRadius", out object targetRadiusObj))
                        {
                            targetRadius = Convert.ToDouble(targetRadiusObj);
                        }
                        if (VectorFieldPlatform.TryGetValue("ovalness01", out object ovalness01Obj))
                        {
                            ovalness01 = Convert.ToDouble(ovalness01Obj);
                        }
                        if (VectorFieldPlatform.TryGetValue("DelaySeconds", out object DelaySecondsObj))
                        {
                            DelaySeconds = Convert.ToDouble(DelaySecondsObj);
                        }
                    }

                    // If its a preset platform dont do any of this.
                    if (!IsPresetPatform)
                    {
                        Dictionary<string, object> size = (Dictionary<string, object>)platform["size"];
                        double width = Convert.ToDouble(size["width"]);
                        double height = Convert.ToDouble(size["height"]);
                        double radius = Convert.ToDouble(platform["radius"]);
                        bool UseCustomMassScale = false;
                        bool UseCustomDrillColorAndBolderTexture = false;
                        bool UseSlimeCam = false;
                        PlatformType platformType = PlatformType.slime;
                        Vector4 color;
                        UseCustomTexture = false;
                        double CustomMassScale = 0.05;

                        // Custom mass
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

                        // Custom Texture 
                        UseCustomTexture = CustomTexture != null && CustomTexture.ContainsKey("CustomTextureName") && CustomTexture.ContainsKey("PixelsPerUnit");
                        Debug.Log($"UseCustomTexture is {UseCustomTexture}");
                        if (UseCustomTexture)
                        {
                            float PixelsPerUnit = (float)Convert.ToDouble(CustomTexture["PixelsPerUnit"]);
                            CustomTextureName = (String)CustomTexture["CustomTextureName"];
                            Debug.Log(CustomTextureName);

                            // Doesnt work if there are multiple files ending with the file name
                            //TODO: make it so that if a sprite for it with the pramiters alredy exsits use that. as creating a sprite from raw data is costly
                            Byte[] filedata;
                            Byte[][] filedatas = GetFileFromZipArchiveBytes(zipArchives[index], IsCustomTexture);
                            if (filedatas.Length > 0)
                            {
                                filedata = filedatas[0];
                                Debug.Log($"Filedata is {filedata}");
                                sprite = IMG2Sprite.LoadNewSprite(filedata, PixelsPerUnit);
                                Debug.Log($"sprite is {sprite}");
                            }
                            else
                            {
                                logger.LogError($"Error: no file named {CustomTextureName}");
                                Debug.LogError($"Error: no file named {CustomTextureName}");
                                return;
                            }
                        }

                        // Custom color
                        color = new Vector4(
                            platform.TryGetValue("Red", out object redVal) ? (float)Convert.ToDouble(redVal) : 1f,
                            platform.TryGetValue("Green", out object greenVal) ? (float)Convert.ToDouble(greenVal) : 1f,
                            platform.TryGetValue("Blue", out object blueVal) ? (float)Convert.ToDouble(blueVal) : 1f,
                            platform.TryGetValue("Opacity", out object opacityVal) ? (float)Convert.ToDouble(opacityVal) : 1f
                        );

                        // UseCustomDrillColorAndBolderTexture
                        if (platform.ContainsKey("CustomDrillColorAndBolderTexture"))
                        {
                            UseCustomDrillColorAndBolderTexture = true;
                        }
                        if (UseCustomDrillColorAndBolderTexture)
                        {
                            var CustomDrillColorAndBolderTexture = (Dictionary<string, object>)platform["CustomDrillColorAndBolderTexture"];

                            // Get drill colors dict to pass.
                            var dict = (Dictionary<string, object>)CustomDrillColorAndBolderTexture["CustomDrillColors"];

                            // If this platform fails to generate then the custom boulder textures will get mixed up.
                            var MyPlatformId = NextPlatformTypeValue;
                            NextPlatformTypeValue++;
                            Debug.Log("Creating drill colors");
                            var colors = DrillColors(MyPlatformId, dict);
                            Debug.Log("Drill colors created");
                            platformType = (PlatformType)MyPlatformId;
                            CustomDrillColors.Add(colors);

                            // Custom Boulder time
                            float PixelsPerUnit = (float)Convert.ToDouble(CustomDrillColorAndBolderTexture["BoulderPixelsPerUnit"]);
                            CustomTextureName = (String)CustomDrillColorAndBolderTexture["CustomBoulderTexture"];

                            // Doesnt work if there are multiple files ending with the file name (Melon, I swear ive seen this before)
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
                                logger.LogError($"Error: no file named {CustomTextureName}");
                                Debug.LogError($"Error: no file named {CustomTextureName}");
                                return;
                            }
                            var BoulderSmokeColorList = ListOfObjectsToListOfFloats((List<object>)CustomDrillColorAndBolderTexture["BoulderSmokeColor"]);
                            UnityEngine.Color BoulderSmokeColor = new(BoulderSmokeColorList[0], BoulderSmokeColorList[1], BoulderSmokeColorList[2], BoulderSmokeColorList[3]);
                            CustomBoulderSmokeColors.Add(BoulderSmokeColor);
                        }
                        if (CustomTexture != null && CustomTexture.ContainsKey("UseSlimeCam"))
                        {
                            UseSlimeCam = (bool)CustomTexture["UseSlimeCam"];
                        }
                        Vector4[] color2 = [color];
                        Vec2[] centerPoint2 = [centerPoint];
                        
                        // Spawn Platform
                        var PlatformObject = PlatformApi.PlatformApi.SpawnPlatform((Fix)x, (Fix)y, (Fix)width, (Fix)height, (Fix)radius, (Fix)rotatson, CustomMassScale, color2, platformType, UseSlimeCam, sprite, pathType, OrbitForce, OrbitPath, DelaySeconds, orbitSpeed, expandSpeed, centerPoint2, normalSpeedFriction, DeadZoneDist, OrbitAccelerationMulitplier, targetRadius, ovalness01);
                        PlatformObject.GetComponent<WaterWaves>().WavePrefab = WavePrefab;

                        // Add Moving Platform Signal Stuff
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
                    }

                    // Load Preset Platform
                    else
                    {
                        Dictionary<string, object> PresetPlatform = (Dictionary<string, object>)platform["PresetPlatform"];
                        string PresetPlatformName = Convert.ToString(PresetPlatform["PresetPlatformName"]);
                        var Platform = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/" + PresetPlatformName + ".prefab");

                        // Set position
                        var x2 = FloorToThousandnths(x);
                        var y2 = FloorToThousandnths(y);
                        Vector3 pos = new((float)x2, (float)y2, 0);

                        // Fix shader
                        Platform.GetComponent<SpriteRenderer>().material = PlatformMat;

                        // Set home
                        PlatformApi.PlatformApi.SetHome(Platform, (Vec2)pos);
                        BoplBody body = Platform.GetComponent<BoplBody>();

                        // Make the space junk not start with rotatsonal velosity
                        body.StartAngularVelocity = Fix.Zero;
                        Platform.GetComponent<WaterWaves>().WavePrefab = WavePrefab;

                        // Scale object
                        var ScaleFactor = FloorToThousandnths(Convert.ToDouble(PresetPlatform["ScaleFactor"]));
                        if (Platform.GetComponent<GrowOnStart>() == null)
                        {
                            var GrowOnStartComp = Platform.AddComponent(typeof(GrowOnStart)) as GrowOnStart;
                            GrowOnStartComp.scaleUp = ScaleFactor;
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

                        // Spawn Object
                        Platform = FixTransform.InstantiateFixed(Platform, (Vec2)pos);
                        PlatformApi.PlatformApi.SetPos(Platform, (Vec2)pos);

                        // Rotate Object
                        StickyRoundedRectangle StickyRect = Platform.GetComponent<StickyRoundedRectangle>();
                        StickyRect.GetGroundBody().rotation = FloorToThousandnths((double)rotatson);

                        // Fix Materials
                        foreach (Transform t in Platform.transform)
                        {
                            // Fix Grass
                            if (t.gameObject.name == "AnimatedGrass_0" || t.gameObject.name == "AnimatedGrass_0 (2)" || t.gameObject.name == "AnimatedGrass_0 (3)" || t.gameObject.name == "AnimatedGrass")
                            {
                                // Set the material
                                t.gameObject.GetComponent<SpriteRenderer>().material = GrassMat;
                            }
                        }
                        if (pathType == PlatformApi.PlatformApi.PathType.AntiLockPlatform)
                        {
                            // Anti Lock Platform
                            var AntiLockPlatformComp = Platform.AddComponent(typeof(AntiLockPlatform)) as AntiLockPlatform;
                            AntiLockPlatformComp.OrbitForce = FloorToThousandnths(OrbitForce);
                            AntiLockPlatformComp.OrbitPath = OrbitPath;
                            AntiLockPlatformComp.DelaySeconds = FloorToThousandnths(DelaySeconds);

                        }
                        if (pathType == PlatformApi.PlatformApi.PathType.VectorFieldPlatform)
                        {
                            PlatformApi.PlatformApi.AddVectorFieldPlatform(Platform, FloorToThousandnths(DelaySeconds), FloorToThousandnths(orbitSpeed), FloorToThousandnths(expandSpeed), centerPoint, FloorToThousandnths(normalSpeedFriction), FloorToThousandnths(DeadZoneDist), FloorToThousandnths(OrbitAccelerationMulitplier), FloorToThousandnths(targetRadius), FloorToThousandnths(ovalness01));
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
                // Remove all shootrays that are still around as they dont like unloading when the scene unloads for some reason.
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
                    GameObject triggerGameObject = new("TriggerObject");

                    // Add the FixTransform and Trigger components to the GameObject
                    triggerGameObject.AddComponent<FixTransform>();

                    TriggerPrefab = triggerGameObject.AddComponent<Trigger>();
                    
                    // Create a new GameObject
                    GameObject spawnerGameObject = new("SpawnerObject");

                    // Add the components to the GameObject
                    spawnerGameObject.AddComponent<FixTransform>();
                    SpawnerPrefab = spawnerGameObject.AddComponent<Spawner>();
                    spawnerGameObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    spawnerGameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    var spriteRender = spawnerGameObject.AddComponent<SpriteRenderer>();
                    var SpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/spawnericon.prefab");
                    spriteRender.sprite = SpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject DisappearGameObject = new("DisappearPlatformsObject");

                    // Add the components to the GameObject
                    DisappearGameObject.AddComponent<FixTransform>();
                    DisappearPlatformsOnSignalPrefab = DisappearGameObject.AddComponent<DisappearPlatformsOnSignal>();

                    // Reset this at the begiening of every round.
                    DisappearPlatformsOnSignal.DisappearPlatformsOnSignals = new();
                    DisappearGameObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    DisappearGameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    spriteRender = DisappearGameObject.AddComponent<SpriteRenderer>();
                    SpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/disapearingplatform.prefab");
                    spriteRender.sprite = SpriteGameObject.GetComponent<SpriteRenderer>().sprite;
                    
                    // Create a new GameObject
                    GameObject AndGateObject = new("AndGateObject");

                    // Add the components to the GameObject
                    AndGateObject.AddComponent<FixTransform>();

                    // Put it offscreen
                    AndGateObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    AndGateObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    andGatePrefab = AndGateObject.AddComponent<AndGate>();
                    var AndGateRender = AndGateObject.AddComponent<SpriteRenderer>();
                    Debug.Log(MyAssetBundle.LoadAsset("assets/assetbundleswanted/andgate.prefab"));
                    var AndGateSpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/andgate.prefab");
                    AndGateRender.sprite = AndGateSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject SignalDelayObject = new("SignalDelayObject");

                    // Add the components to the GameObject
                    SignalDelayObject.AddComponent<FixTransform>();

                    // Put it offscreen
                    SignalDelayObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    SignalDelayObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    SignalDelayPrefab = SignalDelayObject.AddComponent<SignalDelay>();
                    var SignalDelaySpriteRender = SignalDelayObject.AddComponent<SpriteRenderer>();
                    var SignalDelaySpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/delaygate.prefab");
                    SignalDelaySpriteRender.sprite = SignalDelaySpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject OrGateObject = new("OrGateObject");

                    // Add the components to the GameObject
                    OrGateObject.AddComponent<FixTransform>();

                    // Put it offscreen
                    OrGateObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    OrGateObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    OrGatePrefab = OrGateObject.AddComponent<OrGate>();
                    var OrGateSpriteRender = OrGateObject.AddComponent<SpriteRenderer>();
                    var OrGateSpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/orgate.prefab");
                    OrGateSpriteRender.sprite = OrGateSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject NotGateObject = new("NotGateObject");

                    // Add the components to the GameObject
                    NotGateObject.AddComponent<FixTransform>();

                    // Put it offscreen
                    NotGateObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    NotGateObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    NotGatePrefab = NotGateObject.AddComponent<NotGate>();
                    var NotGateSpriteRender = NotGateObject.AddComponent<SpriteRenderer>();
                    var NotGateSpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/notgate.prefab");
                    NotGateSpriteRender.sprite = NotGateSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject ShootRayGameObject = new("ShootRayObject");

                    // Add the components to the GameObject
                    ShootRayGameObject.AddComponent<FixTransform>();
                    ShootRayGameObject.AddComponent<ShootBlink>();
                    ShootRayPrefab = ShootRayGameObject.AddComponent<ShootRay>();
                    spriteRender.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    ShootRayGameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    spriteRender = ShootRayGameObject.AddComponent<SpriteRenderer>();


                    // Create a new GameObject
                    GameObject DropPlayersGameObject = new("DropPlayersObject");
                    
                    // Add the components to the GameObject
                    DropPlayersGameObject.AddComponent<FixTransform>();
                    DropPlayersPrefab = DropPlayersGameObject.AddComponent<DropPlayers>();
                    DropPlayersGameObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    DropPlayersGameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    spriteRender = DropPlayersGameObject.AddComponent<SpriteRenderer>();
                    SpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/playerdrop.prefab");
                    spriteRender.sprite = SpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject ShakePlatformGameObject = new("ShakePlatformObject");
                    
                    // Add the components to the GameObject
                    ShakePlatformGameObject.AddComponent<FixTransform>();
                    ShakePlatformPrefab = ShakePlatformGameObject.AddComponent<ShakePlatform>();
                    ShakePlatformGameObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    ShakePlatformGameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    var ShakePlatformRender = ShakePlatformGameObject.AddComponent<SpriteRenderer>();
                    var ShakePlatformSpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/shaker.prefab");
                    ShakePlatformRender.sprite = ShakePlatformSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject LuaGameObject = new("LuaTestObject");
                    
                    // Add the components to the GameObject
                    LuaGameObject.AddComponent<FixTransform>();
                    LuaPrefab = LuaGameObject.AddComponent<LuaMain>();
                    LuaGameObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    LuaGameObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    var LuaRender = LuaGameObject.AddComponent<SpriteRenderer>();
                    var LuaSpriteGameObject = (GameObject)SpriteAssetBundle.LoadAsset("assets/assetbundleswanted/luagate.prefab");
                    LuaRender.sprite = LuaSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject SignalSystemObject = new("SignalSystemObject");

                    // Add the components to the GameObject
                    SignalSystemObject.AddComponent<FixTransform>();
                    signalSystem = SignalSystemObject.AddComponent<SignalSystem>();
                    SignalSystem.LogicInputs = [];
                    SignalSystem.LogicOutputs = [];
                    SignalSystem.LogicStartingOutputs = [];
                    SignalSystem.LogicGatesToAlwaysUpdate = [];
                    SignalSystem.LineRenderers = [];
                    SignalSystem.LogicInputsThatAlwaysUpdateThereLineConnectsons = [];
                    SignalSystem.FirstUpdateOfTheRound = true;
                    SignalSystem.AllLogicGates = [];
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in spawning triggers/spawners at scene load: {ex}");
                }
                NextUUID = 0;
                
                //TODO remove this when done testing signal stuff
                //TESTING START!
                Vec2[] path = { new(Fix.Zero, (Fix)10), new((Fix)10, (Fix)10) };
                Vec2[] center = { new((Fix)0, (Fix)15) };
                //var platform = PlatformApi.PlatformApi.SpawnPlatform((Fix)0, (Fix)10, (Fix)2, (Fix)2, (Fix)1, Fix.Zero, 0.05, null, PlatformType.slime, false, null, PlatformApi.PlatformApi.PathType.VectorFieldPlatform, 500, path, 0, false, 100, 100, center);
                //CreateTrigger(new Vec2((Fix)(-10), (Fix)30), new Vec2((Fix)10, (Fix)10), 0, true, true, true, true, true, true, true, true, true, true, true, true, true, true, true);
                //CreateTrigger(new Vec2((Fix)10, (Fix)30), new Vec2((Fix)10, (Fix)10), 1, true);
                int[] UUids = [4, 0];
                //CreateOrGate(UUids, 6, new Vec2(Fix.Zero, (Fix)5), (Fix)0);
                //CreateNotGate(6, 2, new Vec2((Fix)5, (Fix)5), (Fix)0);
                int[] UUids2 = [1, 5];
                //CreateOrGate(UUids2, 7, new Vec2(Fix.Zero, (Fix)(-5)), (Fix)0);
                //CreateNotGate(7, 3, new Vec2((Fix)5, (Fix)(-5)), (Fix)0);
                //CreateSignalDelay(2, 5, (Fix)0, new Vec2((Fix)2, (Fix)(-2)), (Fix)180);
                //CreateSignalDelay(3, 4, (Fix)0, new Vec2((Fix)2, (Fix)(2)), (Fix)180);
                //AddMovingPlatformSignalStuff(platform, 2);
                //CreateDisappearPlatformsOnSignal(platform, 3, Fix.Zero, (Fix)2, false);
                //CreateShakePlatform(platform, 2, (Fix)0.5, true, (Fix)1);
                //CreateDropPlayers(platform, 2, (Fix)100, true);
                int[] UUids3 = [];
                int[] UUids4 = [];
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

                // Find the platforms and remove them (shadow + david)
                levelt = GameObject.Find("Level").transform;
                var index = 0;
                foreach (Transform tplatform in levelt)
                {
                    // If its first, commit robery
                    if (index == 0)
                    {
                        // Steal material 
                        PlatformMat = tplatform.gameObject.GetComponent<SpriteRenderer>().material;
                        WavePrefab = tplatform.gameObject.GetComponent<WaterWaves>().WavePrefab;
                    }
                    index++;

                    // End its life
                    Updater.DestroyFix(tplatform.gameObject);
                }
                LoadMapsFromFolder();
                signalSystem.SetUpDicts();
                Debug.Log("Signal stuff has loaded!");
            }
            if (scene.name == "MainMenu")
            {
                var menu = GameObject.Find("Tutorial").transform;
                var buttonPrefab = MyAssetBundle.LoadAsset<GameObject>("assets/assetbundleswanted/mapmaker button.prefab");
                var discordPrefab = MyAssetBundle.LoadAsset<GameObject>("assets/assetbundleswanted/discord button.prefab");
                var websitePrefab = MyAssetBundle.LoadAsset<GameObject>("assets/assetbundleswanted/website button 1.prefab");

                GameObject CreateButton(string name, GameObject prefab, Transform parent, Vector3 localPosition, Vector3 localScale, UnityAction onClick)
                {
                    var buttonObject = Instantiate(prefab, parent);
                    buttonObject.transform.localPosition = localPosition;
                    buttonObject.transform.localScale = localScale;
                    buttonObject.GetComponent<Button>().onClick.AddListener(onClick);
                    return buttonObject;
                }

                CreateButton("MapMaker", buttonPrefab, menu, new Vector3(800, 35), new Vector3(3.5f, 3.5f), OnClickDocs);
                CreateButton("Get Maps", websitePrefab, menu, new Vector3(-800, 35), new Vector3(3.5f, 3.5f), OnClickMap);
                CreateButton("Discord", discordPrefab, GameObject.Find("discord-link").transform, new Vector3(75, 25), new Vector3(0.20f, 0.20f), OnClickDiscord);
            }

        }
        public static void OnClickMap()
        {
            System.Diagnostics.Process.Start("https://map-maker.abstractmelon.net/");
        }
        public static void OnClickDocs()
        {
            System.Diagnostics.Process.Start("https://map-maker.abstractmelon.net/docs/");
        }
        public static void OnClickDiscord()
        {
            System.Diagnostics.Process.Start("https://discord.gg/kjhePG5rnq");
        }
        public static bool IsLevelName(String input)
        {
            Regex regex = new("Level[0-9]+", RegexOptions.IgnoreCase);
            return regex.IsMatch(input);
        }

        // Some stolen code
        // https://stormconsultancy.co.uk/blog/storm-news/convert-an-angle-in-degrees-to-radians-in-c/
        public static Fix ConvertToRadians(double angle)
        {
            return (Fix)PhysTools.DegreesToRadians * (Fix)angle;
        }
         // Some stolen code
        // https://stackoverflow.com/questions/19167669/keep-only-numeric-value-from-a-string
        // Simply replace the offending substrings with an empty string
        public static int GetMapIdFromSceneName(string s)
        {
            Regex rxNonDigits = new(@"[^\d]+");
            if (string.IsNullOrEmpty(s)) return 0;
            string cleaned = rxNonDigits.Replace(s, "");

            // Remove one as maps use a zero based index
            return int.Parse(cleaned) - 1;
        }
        
        // Generated by ai :skull:
        public static ZipArchive UnzipFile(string zipFilePath)
        {

            // Open the zip file for reading
            FileStream zipStream = new(zipFilePath, FileMode.Open);
            ZipArchive archive = new(zipStream, ZipArchiveMode.Read);
            return archive;

        }

        // Finds all the files with a path that the predicate accepts as a string array 
        public static string[] GetFileFromZipArchive(ZipArchive archive, Predicate<string> predicate)
        {
            Debug.Log("enter GetFileFromZipArchive");
            string[] data = [];

            // Iterate through each entry in the zip file
            // Archive is disposed of at this point for some reson
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                // If entry is a directory, skip it
                if (entry.FullName.EndsWith("/"))
                    continue;
                string[] path = { entry.FullName };
                string[] ValidPathArray = Array.FindAll(path, predicate);
                if (ValidPathArray.Length != 0)
                {
                    // Read the contents of the entry
                    using StreamReader reader = new(entry.Open());
                    string contents = reader.ReadToEnd();
                    data = [.. data, contents];
                }
            }
            return data;
        }

        public static Byte[][] GetFileFromZipArchiveBytes(ZipArchive archive, Predicate<string> predicate)
        {
            Debug.Log("enter GetFileFromZipArchive");
            Byte[][] data = [];
            
            // Iterate through each entry in the zip file
            // Archive is disposed of at this point for some reson
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                // If entry is a directory, skip it
                if (entry.FullName.EndsWith("/"))
                    continue;
                string[] path = { entry.FullName };
                string[] ValidPathArray = Array.FindAll(path, predicate);
                if (ValidPathArray.Length != 0)
                {
                    // Read the contents of the entry
                    using var entryStream = entry.Open();
                    using var memoryStream = new MemoryStream();
                    entryStream.CopyTo(memoryStream);
                    data = data.Append(memoryStream.ToArray()).ToArray();
                }
            }
            return data;
        }


        // Gets all of the .zip files from the maps folder and turns them into a array of ZipArchive's (david) ONLY CALL ON START!
        public static ZipArchive[] GetZipArchives()
        {
            string[] MapZipFiles = Directory.GetFiles(mapsFolderPath, "*.zip");
            Debug.Log($"{MapZipFiles.Length} .zip's");
            foreach (string zipFile in MapZipFiles)
            {
                zipArchives = [.. zipArchives, UnzipFile(zipFile)];
                if (Path.GetFileName(zipFile) == "TESTING.zip")
                {
                    zipArchives = [zipArchives[zipArchives.Length-1]];
                    break;
                }
            }
            Debug.Log($"zipArchivesLength is {zipArchives.Length}");
            MyZipArchives = zipArchives;
            return zipArchives;
        }
        // Get custom drill color
        public static Drill.PlatformColors DrillColors(int PlatformType, Dictionary<string, object> dict)
        {
            var colors = new Drill.PlatformColors();
            // Convert them to List<float> instead of object. (objects are so confusing)
            List<object> ColorDarkObjectList = (List<object>)dict["ColorDark"];
            List<object> ColorMediumObjectList = (List<object>)dict["ColorMedium"];
            List<object> ColorLightObjectList = (List<object>)dict["ColorLight"];
            List<float> ColorDarkFloats = ListOfObjectsToListOfFloats(ColorDarkObjectList);
            List<float> ColorMediumFloats = ListOfObjectsToListOfFloats(ColorMediumObjectList);
            List<float> ColorLightFloats = ListOfObjectsToListOfFloats(ColorLightObjectList);
            UnityEngine.Color ColorDark = new(ColorDarkFloats[0], ColorDarkFloats[1], ColorDarkFloats[2], ColorDarkFloats[3]);
            UnityEngine.Color ColorMedium = new(ColorMediumFloats[0], ColorMediumFloats[1], ColorMediumFloats[2], ColorMediumFloats[3]);
            UnityEngine.Color ColorLight = new(ColorLightFloats[0], ColorLightFloats[1], ColorLightFloats[2], ColorLightFloats[3]);
            colors.dark = ColorDark;
            colors.medium = ColorMedium;
            colors.light = ColorLight;

            // Used to define the custom platform type. will be used whenever we drill it
            colors.type = (PlatformType)PlatformType;
            return colors;
        }
        public static List<float> ListOfObjectsToListOfFloats(List<object> ObjectList)
        {
            List<float> Floats = [];
            for (int i = 0; i < ObjectList.Count; i++)
            {
                Floats.Add((float)Convert.ToDouble(ObjectList[i]));
            }
            return Floats;
        }
        public static int RandomBagLevel()
        {
            if (MapIndexsLeft.Count == 0)
            {
                var i = 0;
                foreach (var _ in zipArchives)
                {
                    MapIndexsLeft.Add(i);
                    i++;
                }
            }
            var NewMapIndexIndex = UnityEngine.Random.Range(0, MapIndexsLeft.Count);
            var NewMapIndex = MapIndexsLeft[NewMapIndexIndex];
            MapIndexsLeft.Remove(NewMapIndex);
            return NewMapIndex;
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
        // Lua stuff
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



// ITS OVER!
// HOLY SH*T, THAT TOOK A LONG TIME
// Anyways insert random antimality thing here:

/*
    Don't Believe what you see, see what you believe.
    - Antimality.
*/