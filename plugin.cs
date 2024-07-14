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

        //used to fix a unity bug
        public static PlayerAverageCamera averageCamera;
        public static ShakableCamera shakableCamera;
        //make it a weak refrence?
        public static List<PlayerInput> playerInputs = new();
        public static bool FirstUpdate = true;
        //map ids
        public static readonly int GrassMapId = 0;
        public static readonly int SnowMapId = 21;
        public static readonly int SpaceMapId = 33;

        //used to make shakeable platform to know its being called by a blink gun.
        public static bool CurrentlyBlinking;
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
            //thanks almafa64 on discord for the path stuff.
            MyAssetBundle = AssetBundle.LoadFromFile(Path.GetDirectoryName(Info.Location) + "/mapmakerassets");
            string[] assetNames = MyAssetBundle.GetAllAssetNames();
            //MapUUIDChannel = new EntwinedPacketChannel<int>(this, new IntEntwiner());
            //MapUUIDChannel.OnMessage += OnGetUUID; 
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
            List<string> MetaDataList = new();
            foreach (ZipArchive zipArchive in zipArchives)
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
                StartCoroutine(GetGrassMat());
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
                            MoreJsonParceing.SpawnLuaGates((List<object>)Dict["LuaGates"], i);
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
                    Debug.Log("getting IsPresetPatform");
                    bool IsPresetPatform = platform.ContainsKey("PresetPlatform");
                    Debug.Log("IsPresetPatform is: " + IsPresetPatform);

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

                        //scale object
                        var ScaleFactor = FloorToThousandnths(Convert.ToDouble(PresetPlatform["ScaleFactor"]));
                        var GrowOnStartComp = Platform.AddComponent(typeof(GrowOnStart)) as GrowOnStart;
                        GrowOnStartComp.scaleUp = ScaleFactor;
                        Debug.Log("added GrowOnStart");
                        //spawn object
                        Debug.Log($"pos is {pos}");
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
                    var SpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/spawnericon.prefab");
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
                    SpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/disapearingplatform.prefab");
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
                    // Add the components to the GameObject
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
                    // Add the components to the GameObject
                    NotGateObject.AddComponent<FixTransform>();
                    //put it offscreen
                    NotGateObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    NotGateObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    NotGatePrefab = NotGateObject.AddComponent<NotGate>();
                    var NotGateSpriteRender = NotGateObject.AddComponent<SpriteRenderer>();
                    var NotGateSpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/notgate.prefab");
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
                    SpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/playerdrop.prefab");
                    spriteRender.sprite = SpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject ShakePlatformGameObject = new GameObject("ShakePlatformObject");
                    // Add the components to the GameObject
                    ShakePlatformGameObject.AddComponent<FixTransform>();
                    ShakePlatformPrefab = ShakePlatformGameObject.AddComponent<ShakePlatform>();
                    ShakePlatformGameObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    ShakePlatformGameObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
                    var ShakePlatformRender = ShakePlatformGameObject.AddComponent<SpriteRenderer>();
                    var ShakePlatformSpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/shaker.prefab");
                    ShakePlatformRender.sprite = ShakePlatformSpriteGameObject.GetComponent<SpriteRenderer>().sprite;

                    // Create a new GameObject
                    GameObject LuaGameObject = new GameObject("LuaTestObject");
                    // Add the components to the GameObject
                    LuaGameObject.AddComponent<FixTransform>();
                    LuaPrefab = LuaGameObject.AddComponent<LuaMain>();
                    LuaGameObject.GetComponent<FixTransform>().position = new Vec2((Fix)1000, (Fix)1000);
                    LuaGameObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    var LuaRender = LuaGameObject.AddComponent<SpriteRenderer>();
                    var LuaSpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/luagate.prefab");
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
                //CreateTrigger(new Vec2((Fix)(-10), (Fix)30), new Vec2((Fix)10, (Fix)10), 0, true);
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
                CreateLuaGate(UUids3, UUids4, new Vec2((Fix)10, (Fix)(10)), (Fix)0, @"
player = GetClosestPlayer(0, 0)
if (player ~= nil) then
    player.SetAbility(2, ""Beam"", false)
end");
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
            var SpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/blinkemitter.prefab");
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
            var SpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/growemitter.prefab");
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
            var SpriteGameObject = (GameObject)MyAssetBundle.LoadAsset("assets/assetbundleswanted/shrinkemitter.prefab");
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
        public static LuaMain CreateLuaGate(int[] InputUUids, int[] OutputUUids, Vec2 pos, Fix rot, string LuaCode)
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
            Lua.Register();
            return Lua;
        }
        //lua stuff
        public static DynValue exec1(CallbackArguments args, string funcName, Func<double, double> func, MoonSharp.Interpreter.CoreLib.MathModule __instance)
        {
            MethodInfo dynMethod = typeof(MoonSharp.Interpreter.CoreLib.MathModule).GetMethod("exec1",
BindingFlags.NonPublic | BindingFlags.Static);
            return (DynValue)dynMethod.Invoke(null, new object[] { args, funcName, func });
        }
        public static DynValue exec2(CallbackArguments args, string funcName, Func<double, double, double> func, MoonSharp.Interpreter.CoreLib.MathModule __instance)
        {
            MethodInfo dynMethod = typeof(MoonSharp.Interpreter.CoreLib.MathModule).GetMethod("exec2",
BindingFlags.NonPublic | BindingFlags.Static);
            return (DynValue)dynMethod.Invoke(null, new object[] { args, funcName, func });
        }
        public static DynValue exec2n(CallbackArguments args, string funcName, double defVal, Func<double, double, double> func, MoonSharp.Interpreter.CoreLib.MathModule __instance)
        {
            MethodInfo dynMethod = typeof(MoonSharp.Interpreter.CoreLib.MathModule).GetMethod("exec2n",
BindingFlags.NonPublic | BindingFlags.Static);
            return (DynValue)dynMethod.Invoke(null, new object[] { args, funcName, func });
        }
        public static DynValue execaccum(CallbackArguments args, string funcName, Func<double, double, double> func, MoonSharp.Interpreter.CoreLib.MathModule __instance)
        {
            MethodInfo dynMethod = typeof(MoonSharp.Interpreter.CoreLib.MathModule).GetMethod("execaccum",
BindingFlags.NonPublic | BindingFlags.Static);
            return (DynValue)dynMethod.Invoke(null, new object[] { args, funcName, func });
        }
        public static Fix Tanh(Fix d)
        {
            var cosh = (Fix.Pow((Fix)2.718281828459045, (Fix)(d)) + Fix.Pow((Fix)2.718281828459045, (Fix)(-d))) / (Fix)2;
            var sinh = (Fix.Pow((Fix)2.718281828459045, (Fix)d) - Fix.Pow((Fix)2.718281828459045, (Fix)(-d))) / (Fix)2;
            return sinh / cosh;
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
            if (Plugin.CustomMatchoManSprites != null && Plugin.CustomMatchoManSprites.Count != 0)
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
            if (Plugin.CustomDrillColors != null && Plugin.CustomDrillColors.Count != 0)
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
    [HarmonyPatch(typeof(QuantumTunnel))]
    public class QuantumTunnelPatches
    {
        [HarmonyPatch("UpdateSim")]
        [HarmonyPostfix]
        private static void Awake_MapMaker_Plug(QuantumTunnel __instance)
        {
            //for all of the DisappearPlatformsOnSignals check if the platform is the same as the Victim
            foreach (var Disappear in DisappearPlatformsOnSignal.DisappearPlatformsOnSignals)
            {
                var VictimId = __instance.Victim.GetInstanceID();
                var DissappearId = Disappear.platform.GetInstanceID();
                if (VictimId == DissappearId)
                {
                    //if we are effecting the same platform and the Disappear platform is going to make the platform disapear when we are done
                    //then dont have the reapearing animatson
                    //delay it by the time it takes to do the animatsons so we dont stop mid animatson
                    float time = __instance.ScaleAnim.keys[__instance.ScaleAnim.keys.Length - 1].time;
                    float time2 = __instance.OpacityAnim.keys[__instance.OpacityAnim.keys.Length - 1].time;
                    var ExstraDelay = Mathf.Max(time, time2);
                    //the __instance.IsInitialized is so that it works fine if delay is 0/less then ExstraDelay
                    if (Disappear.TimeDelayed > Disappear.delay - (Fix)ExstraDelay && (__instance.IsInitialized || __instance.age > __instance.LifeSpan))
                    {
                        var spriteRen = __instance.GetComponent<SpriteRenderer>();
                        __instance.transform.localScale = new Vector3(__instance.originalScale.x, __instance.originalScale.y, __instance.originalScale.z);
                        __instance.spriteRen.color = new UnityEngine.Color(spriteRen.color.r, spriteRen.color.g, spriteRen.color.b, 0);
                        __instance.Victim.SetActive(false);
                    }
                }
            }
        }
    }
    [HarmonyPatch(typeof(SpikeAttack))]
    public class SpikeAttackPatches
    {
        [HarmonyPatch("OnCollide")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(CollisionInformation collision, SpikeAttack __instance)
        {
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
            if (__instance.timePassed < __instance.pushTime && !GameTime.IsTimeStopped() && collision.colliderPP.monobehaviourCollider != null && !collision.colliderPP.monobehaviourCollider.IsDestroyed && collision.colliderPP.monobehaviourCollider.initHasBeenCalled && collision.colliderPP.instanceId != __instance.attachedGround.gameObject.GetInstanceID())
            {
                if (collision.layer == LayerMask.NameToLayer("wall"))
                {
                    Vec2 v = Vec2.NormalizedSafe(collision.contactPoint - __instance.fixTrans.position);
                    collision.colliderPP.monobehaviourCollider.AddForceAtPosition(v * __instance.knockAwayWallStr, collision.contactPoint, ForceMode2D.Force);
                    if (!__instance.pushedThisFrame)
                    {
                        __instance.hitbox.AddForceAtPosition(-v * __instance.knockAwayWallStr * __instance.selfPushMultiplier, collision.contactPoint, ForceMode2D.Force);
                        __instance.pushedThisFrame = true;
                        return false;
                    }
                }
                //dont react if its a trigger
                else if (collision.layer != (LayerMask)3)
                {
                    Vec2 v2 = Vec2.NormalizedSafe(collision.contactPoint - __instance.fixTrans.position);

                    collision.colliderPP.monobehaviourCollider.AddForceAtPosition(v2 * __instance.knockAwayStr, collision.contactPoint, ForceMode2D.Force);

                    if (!__instance.pushedThisFrame)
                    {
                        __instance.hitbox.AddForceAtPosition(-v2 * __instance.knockAwayStr * __instance.selfPushMultiplier, collision.contactPoint, ForceMode2D.Force);
                        __instance.pushedThisFrame = true;
                    }
                }
            }
            return false;
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
        }
    }

    [HarmonyPatch(typeof(SmokeGrenadeExplode2))]
    public class SmokeGrenadeExplode2Patches
    {
        [HarmonyPatch("OnCollide")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(CollisionInformation collision, SmokeGrenadeExplode2 __instance)
        {
            if (!__instance.grenade.DetonatesOnOwner || __instance.IsDestroyed)
            {
                return false;
            }
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
            if (collision.layer == LayerMask.NameToLayer("Projectile") && collision.colliderPP.fixTrans != null)
            {
                Projectile component = collision.colliderPP.fixTrans.GetComponent<Projectile>();
                if (component != null && !component.IgnitesExplosives)
                {
                    return false;
                }
            }
            if (collision.layer == (LayerMask)3)
            {
                return false;
            }
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
            __instance.Detonate();
            return false;
        }
    }
    [HarmonyPatch(typeof(ShakablePlatform))]
    public class ShakablePlatformPatches
    {
        [HarmonyPatch("AddShake")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(ShakablePlatform __instance, Fix duration, Fix shakeAmount, int shakePriority = 1, Material newMaterialDuringShake = null, AnimationCurveFixed shakeCurve = null)
        {
            //for all of the DisappearPlatformsOnSignals check if the platform is the same as the platform we are attached to.
            foreach (var Quantum in ShootQuantum.spawnedQuantumTunnels)
            {
                if (Quantum != null && Quantum.Victim != null)
                {
                    var VictimId = Quantum.Victim.GetInstanceID();
                    var DissappearId = __instance.gameObject.GetInstanceID();
                    //if this is already being blinked and its not being called from a blink dont shake it as if its shorter then it will go back to normal too soon.
                    if (VictimId == DissappearId && !Plugin.CurrentlyBlinking)
                    {
                        return false;
                    }
                }
            }
            if (shakePriority >= __instance.currentShakePriority)
            {
                return true;
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(ShootScaleChange))]
    public class ShootScaleChangePatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        //so that it doesnt error out when copying the component for the ShootRay.
        private static bool Awake_MapMaker_Plug(ShootScaleChange __instance)
        {
            if (__instance.RaycastParticlePrefab != null)
            {
                return true;
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(ShootQuantum))]
    public class ShootQuantumPatches
    {
        [HarmonyPatch("Shoot")]
        [HarmonyPrefix]
        private static void Shoot(ShootQuantum __instance, Vec2 firepointFIX, Vec2 directionFIX, ref bool hasFired, int playerId, bool alreadyHitWater = false)
        {
            Plugin.CurrentlyBlinking = true;
        }
        [HarmonyPatch("Shoot")]
        [HarmonyPostfix]
        private static void Shoot2(ShootQuantum __instance, Vec2 firepointFIX, Vec2 directionFIX, ref bool hasFired, int playerId, bool alreadyHitWater = false)
        {
            Plugin.CurrentlyBlinking = false;
        }
    }
    [HarmonyPatch(typeof(MoonSharp.Interpreter.CoreLib.MathModule))]
    public class MoonSharpPatches
    {

        [HarmonyPatch("abs")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void abs(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec1(args, "abs", d => (double)Fix.Abs((Fix)d), __instance);
        }
        [HarmonyPatch("acos")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void acos(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec1(args, "acos", d => (double)Fix.Acos((Fix)d), __instance);
        }
        [HarmonyPatch("asin")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void asin(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            Debug.Log("asin");
            //Fix doesnt have a asin so thanks to chatgpt i use Acon and sqrt and exsponents to get Asin
            __result = Plugin.exec1(args, "asin", d => (double)Fix.Acos(Fix.Sqrt(Fix.One - Fix.Pow2((Fix)d))), __instance);
        }
        [HarmonyPatch("atan")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void atan(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec1(args, "atan", d => (double)Fix.Atan((Fix)d), __instance);
        }
        [HarmonyPatch("atan2")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void atan2(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec2(args, "atan2", (d1, d2) => (double)Fix.Atan2((Fix)d1, (Fix)d2), __instance);
        }
        [HarmonyPatch("ceil")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void ceil(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec1(args, "ceil", d => (double)Fix.Ceiling((Fix)d), __instance);
        }
        [HarmonyPatch("cos")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void cos(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec1(args, "cos", d => (double)Fix.Cos((Fix)d), __instance);
        }
        [HarmonyPatch("cosh")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void cosh(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            Debug.Log("cosh");
            //i sure hope this works...
            __result = Plugin.exec1(args, "cosh", d => (double)((Fix.Pow((Fix)2.718281828459045, (Fix)(d)) + Fix.Pow((Fix)2.718281828459045, (Fix)(-d))) / (Fix)2), __instance);
        }
        [HarmonyPatch("deg")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void deg(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec1(args, "deg", d => (double)((Fix)d * (Fix)PhysTools.RadiansToDegrees), __instance);
        }
        [HarmonyPatch("exp")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void exp(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            Debug.Log("exp");
            //i sure hope this works...
            __result = Plugin.exec1(args, "exp", d => (double)Fix.Pow((Fix)2.718281828459045, (Fix)d), __instance);
        }
        [HarmonyPatch("floor")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void floor(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec1(args, "floor", d => (double)Fix.Floor((Fix)d), __instance);
        }
        [HarmonyPatch("fmod")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void fmod(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec2(args, "fmod", (d1, d2) => (double)Fix.SlowMod((Fix)d1, (Fix)d2), __instance);
        }
        [HarmonyPatch("ldexp")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void ldexp(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            Debug.Log("ldexp");
            __result = Plugin.exec2(args, "ldexp", (d1, d2) => (double)((Fix)d1 * Fix.Pow((Fix)2, (Fix)d2)), __instance);
        }
        [HarmonyPatch("log")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void log(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            Debug.Log("log");
            //i THINK Log2(n) is the same as ln(n)???
            __result = Plugin.exec2n(args, "log", Math.E, (d1, d2) => (double)(Fix.Log2((Fix)d1) / Fix.Log2((Fix)d2)), __instance);
        }
        [HarmonyPatch("max")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void max(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.execaccum(args, "max", (d1, d2) => (double)Fix.Max((Fix)d1, (Fix)d2), __instance);
        }
        [HarmonyPatch("min")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void min(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.execaccum(args, "min", (d1, d2) => (double)Fix.Min((Fix)d1, (Fix)d2), __instance);
        }
        [HarmonyPatch("modf")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void modf(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            Debug.Log("modf");
            DynValue arg = args.AsType(0, "modf", DataType.Number, false);
            __result = DynValue.NewTuple(DynValue.NewNumber((double)Fix.Floor((Fix)arg.Number)), DynValue.NewNumber((double)((Fix)arg.Number - Fix.Floor((Fix)arg.Number))));
        }
        [HarmonyPatch("pow")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void pow(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec2(args, "pow", (d1, d2) => (double)Fix.Pow((Fix)d1, (Fix)d2), __instance);
        }
        [HarmonyPatch("rad")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void rad(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            Debug.Log("rad");
            __result = Plugin.exec1(args, "rad", d => (double)((Fix)d * (Fix)PhysTools.RadiansToDegrees), __instance);
        }
        [HarmonyPatch("random")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void random(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            double d;
            DynValue m = args.AsType(0, "random", DataType.Number, true);
            DynValue n = args.AsType(1, "random", DataType.Number, true);
            Fix a = n.IsNil() ? (Fix)1 : (Fix)n.Number;
            Fix b = (Fix)m.Number;
            if (a < b)
                d = (double)Updater.RandomFix(a, b);
            else
                d = (double)Updater.RandomFix(b, a);
            __result = DynValue.NewNumber(d);
        }
        [HarmonyPatch("sin")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void sin(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec1(args, "sin", d => (double)(Fix.Sin((Fix)d)), __instance);
        }
        //Fix.Pow((Fix)2.718281828459045, (Fix)d)
        [HarmonyPatch("sinh")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void sinh(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            Debug.Log("sinh");
            __result = Plugin.exec1(args, "sinh", d => (double)((Fix.Pow((Fix)2.718281828459045, (Fix)d) - Fix.Pow((Fix)2.718281828459045, (Fix)(-d))) / (Fix)2), __instance);
        }
        [HarmonyPatch("sqrt")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void sqrt(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec1(args, "sqrt", d => (double)(Fix.Sqrt((Fix)d)), __instance);
        }
        [HarmonyPatch("tan")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void tan(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec1(args, "tan", d => (double)(Fix.Tan((Fix)d)), __instance);
        }
        [HarmonyPatch("tanh")]
        [HarmonyPostfix]
        //make it use Fix math instead of floating point math.
        private static void tanh(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
        {
            __result = Plugin.exec1(args, "tanh", d => (double)Plugin.Tanh((Fix)d), __instance);
        }
    }
    [HarmonyPatch(typeof(MoonSharp.Interpreter.Tree.Expressions.BinaryOperatorExpression))]
    public class MoonSharpPatchThatHopefulyDoesntDoAnything
    {
        [HarmonyPatch("EvalArithmetic")]
        [HarmonyPostfix]
        //instance is a object as the type is private. and funcsons cant take in a type that chages at runtime drectly
        public static void EvalArithmetic(DynValue v1, DynValue v2, MoonSharp.Interpreter.Tree.Expressions.BinaryOperatorExpression __instance)
        {
            //the Operator enum just HAS to be private... ugg lets hope this works
            // Access the private field
            Debug.Log("MATH");
            FieldInfo privateField = __instance.GetType().GetField("Operator", BindingFlags.NonPublic | BindingFlags.Instance);
            object privateFieldValue = privateField.GetValue(__instance);
            int OperatorValue = (int)privateFieldValue;
            Debug.Log("OPERATOR VALUE IS: " + OperatorValue);
            throw new InvalidOperationException("IT TURNS OUT MoonSharp.Interpreter.Tree.Expressions.BinaryOperatorExpression.EvalArithmetic IS USED! WHO KNEW??? PLS PATCH IT SO IT USES FIX!!!");
        }
    }
    [HarmonyPatch(typeof(MoonSharp.Interpreter.Execution.VM.Processor))]
    public class MoonSharpMainMathOperatorsPatch
    {
        [HarmonyPatch("ExecAdd")]
        [HarmonyPostfix]
        public static void ExecAdd(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
        {
            var m_ValueStack = __instance.m_ValueStack;
            DynValue r = m_ValueStack.Pop().ToScalar();
            DynValue l = m_ValueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                m_ValueStack.Push(DynValue.NewNumber((double)((Fix)ln.Value + (Fix)rn.Value)));
                __result = instructionPtr;
                return;
            }
            else
            {
                int ip = __instance.Internal_InvokeBinaryMetaMethod(l, r, "__add", instructionPtr);
                if (ip >= 0) __result = ip;
                else throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
            }
        }
        [HarmonyPatch("ExecAdd")]
        [HarmonyPrefix]
        public static bool ExecAddPrefix(Instruction i, int instructionPtr)
        {
            return false;
        }
        [HarmonyPatch("ExecSub")]
        [HarmonyPostfix]
        public static void ExecSub(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
        {
            var m_ValueStack = __instance.m_ValueStack;
            DynValue r = m_ValueStack.Pop().ToScalar();
            DynValue l = m_ValueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                m_ValueStack.Push(DynValue.NewNumber((double)((Fix)ln.Value - (Fix)rn.Value)));
                __result = instructionPtr;
                return;
            }
            else
            {
                int ip = __instance.Internal_InvokeBinaryMetaMethod(l, r, "__sub", instructionPtr);
                if (ip >= 0) __result = ip;
                else throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
            }
        }
        [HarmonyPatch("ExecSub")]
        [HarmonyPrefix]
        public static bool ExecSubPrefix(Instruction i, int instructionPtr)
        {
            return false;
        }
        [HarmonyPatch("ExecMul")]
        [HarmonyPostfix]
        public static void ExecMul(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
        {
            var m_ValueStack = __instance.m_ValueStack;
            DynValue r = m_ValueStack.Pop().ToScalar();
            DynValue l = m_ValueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                m_ValueStack.Push(DynValue.NewNumber((double)((Fix)ln.Value * (Fix)rn.Value)));
                __result = instructionPtr;
                return;
            }
            else
            {
                int ip = __instance.Internal_InvokeBinaryMetaMethod(l, r, "__mul", instructionPtr);
                if (ip >= 0) __result = ip;
                else throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
            }
        }
        [HarmonyPatch("ExecMul")]
        [HarmonyPrefix]
        public static bool ExecMulPrefix(Instruction i, int instructionPtr)
        {
            return false;
        }
        [HarmonyPatch("ExecMod")]
        [HarmonyPostfix]
        public static void ExecMod(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
        {
            var m_ValueStack = __instance.m_ValueStack;
            DynValue r = m_ValueStack.Pop().ToScalar();
            DynValue l = m_ValueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                m_ValueStack.Push(DynValue.NewNumber((double)((Fix)ln.Value % (Fix)rn.Value)));
                __result = instructionPtr;
                return;
            }
            else
            {
                int ip = __instance.Internal_InvokeBinaryMetaMethod(l, r, "__mod", instructionPtr);
                if (ip >= 0) __result = ip;
                else throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
            }
        }
        [HarmonyPatch("ExecMod")]
        [HarmonyPrefix]
        public static bool ExecModPrefix(Instruction i, int instructionPtr)
        {
            return false;
        }
        [HarmonyPatch("ExecDiv")]
        [HarmonyPostfix]
        public static void ExecDiv(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
        {
            var m_ValueStack = __instance.m_ValueStack;
            DynValue r = m_ValueStack.Pop().ToScalar();
            DynValue l = m_ValueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                m_ValueStack.Push(DynValue.NewNumber((double)((Fix)ln.Value / (Fix)rn.Value)));
                __result = instructionPtr;
                return;
            }
            else
            {
                int ip = __instance.Internal_InvokeBinaryMetaMethod(l, r, "__div", instructionPtr);
                if (ip >= 0) __result = ip;
                else throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
            }
        }
        [HarmonyPatch("ExecDiv")]
        [HarmonyPrefix]
        public static bool ExecDivPrefix(Instruction i, int instructionPtr)
        {
            return false;
        }
        [HarmonyPatch("ExecPower")]
        [HarmonyPostfix]
        public static void ExecPower(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
        {
            var m_ValueStack = __instance.m_ValueStack;
            DynValue r = m_ValueStack.Pop().ToScalar();
            DynValue l = m_ValueStack.Pop().ToScalar();

            double? rn = r.CastToNumber();
            double? ln = l.CastToNumber();

            if (ln.HasValue && rn.HasValue)
            {
                m_ValueStack.Push(DynValue.NewNumber((double)Fix.Pow((Fix)ln.Value, (Fix)rn.Value)));
                __result = instructionPtr;
                return;
            }
            else
            {
                int ip = __instance.Internal_InvokeBinaryMetaMethod(l, r, "__pow", instructionPtr);
                if (ip >= 0) __result = ip;
                else throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
            }
        }
        [HarmonyPatch("ExecPower")]
        [HarmonyPrefix]
        public static bool ExecPowerPrefix(Instruction i, int instructionPtr)
        {
            return false;
        }
        [HarmonyPatch("ExecNeg")]
        [HarmonyPostfix]
        public static void ExecNeg(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
        {
            var m_ValueStack = __instance.m_ValueStack;
            DynValue r = m_ValueStack.Pop().ToScalar();
            double? rn = r.CastToNumber();

            if (rn.HasValue)
            {
                m_ValueStack.Push(DynValue.NewNumber(-rn.Value));
                __result = instructionPtr;
                return;
            }
            else
            {
                int ip = __instance.Internal_InvokeUnaryMetaMethod(r, "__unm", instructionPtr);
                if (ip >= 0) __result = ip;
                else throw ScriptRuntimeException.ArithmeticOnNonNumber(r);
            }
        }
        [HarmonyPatch("ExecNeg")]
        [HarmonyPrefix]
        public static bool ExecNegPrefix(Instruction i, int instructionPtr)
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(HookshotInstant))]
    public class HookshotInstantPatches
    {
        [HarmonyPatch("UseAbility")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(HookshotInstant __instance)
        {
            __instance.playerInfo = __instance.instantAbility.playerInfo;
            if (__instance.ropeBody != null && __instance.playerInfo.ropeBody != null && __instance.ropeBody.enabled && __instance.playerInfo.ropeBody.enabled && __instance.playerInfo.ropeBody.number == __instance.firedRopeNumber && (int)__instance.ropeBody.ownerId == __instance.playerInfo.playerId)
            {
                if (__instance.tickWhenUsedLast + 60 < Updater.SimulationTicks)
                {
                    AudioManager.Get().Play("reelInFire");
                }
                __instance.tickWhenUsedLast = Updater.SimulationTicks;
                int num = __instance.playerInfo.topAttachment ? (__instance.ropeBody.segmentCount - 1) : 0;
                __instance.framesSinceReelIn++;
                if (__instance.framesSinceReelIn >= __instance.framesBetweenReelIns)
                {
                    __instance.ropeBody.segmentSeparation = Fix.Max(__instance.ropeBody.segmentSeparation - __instance.ReelInSpeed / (Fix)((long)__instance.ropeBody.segmentCount), __instance.reeledInSegmentSeparation);
                    bool flag = true;
                    if (__instance.ropeBody.segmentSeparation < __instance.separationBeforeReelinDeletion)
                    {
                        flag = __instance.ropeBody.ReelInSegment(__instance.playerInfo.topAttachment);
                    }
                    if (!flag)
                    {
                        Vec2 u = Vec2.NormalizedSafe(__instance.ropeBody.segment[num] - __instance.playerInfo.slimeController.body.position) * (Fix)0.1;
                        if (!__instance.playerInfo.isGrounded && __instance.ropeBody.hookHasArrived)
                        {
                            __instance.playerInfo.slimeController.body.position = __instance.ropeBody.segment[num] + u;
                        }
                        __instance.ropeBody.enabled = false;
                        __instance.ropeBody.disabledThisFrame = true;
                        __instance.ropeBody = null;
                        AudioManager.Get().Play("hookshotLetGo");
                    }
                    __instance.framesSinceReelIn = 0;
                    return false;
                }
            }
            else
            {
                __instance.FireHook();
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(Missile))]
    public class MisslePatches
    {
        [HarmonyPatch("OnCollide")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(CollisionInformation collision, Missile __instance)
        {
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
            if (collision.layer == LayerMask.NameToLayer("RigidBodyAffector") || collision.layer == LayerMask.NameToLayer("Rope") || collision.layer == (LayerMask)3)
            {
                return false;
            }
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
            FixTransform.InstantiateFixed<Explosion>(__instance.onHitExplosionPrefab, __instance.body.position).GetComponent<IPhysicsCollider>().Scale = __instance.fixTrans.Scale;
            if (!string.IsNullOrEmpty(__instance.soundEffectOnCol))
            {
                AudioManager.Get().Play(__instance.soundEffectOnCol);
            }
            Updater.DestroyFix(__instance.gameObject);
            return false;
        }
    }
    [HarmonyPatch(typeof(RopeHook))]
    public class RopeHookPatches
    {
        [HarmonyPatch("OnCollide")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(CollisionInformation collision, RopeHook __instance)
        {
            //if its a trigger dont do anything.
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
            if (collision.layer == (LayerMask)3)
            {
                return false;
            }
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
            return true;
        }
    }
    [HarmonyPatch(typeof(GameSession))]
    public class GameSessionPatches
    {
        [HarmonyPatch("RandomBagLevel")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(GameSession __instance)
        {
            Debug.Log("RandomBagLevel");
            //its max exsclusive min inclusinve
            if (Plugin.MapJsons.Length != 0)
            {
                Plugin.CurrentMapIndex = UnityEngine.Random.Range(0, Plugin.MapJsons.Length);
                Dictionary<string, object> MetaData = MiniJSON.Json.Deserialize(Plugin.MetaDataJsons[Plugin.CurrentMapIndex]) as Dictionary<string, object>;
                var type = Convert.ToString(MetaData["MapType"]);
                switch (type)
                {
                    case "space":
                        GameSession.currentLevel = (byte)Plugin.SpaceMapId;
                        break;
                    case "snow":
                        GameSession.currentLevel = (byte)Plugin.SnowMapId;
                        break;
                    default:
                        GameSession.currentLevel = (byte)Plugin.GrassMapId;
                        break;
                }
                var UUID = Convert.ToInt32(MetaData["MapUUID"]);
                Plugin.CurrentMapUUID = UUID;
                return false;
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(CharacterSelectHandler))]
    public class CharacterSelectHandlerPatches
    {
        [HarmonyPatch("TryStartGame_inner")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(CharacterSelectHandler __instance)
        {
            if (CharacterSelectHandler.startButtonAvailable && CharacterSelectHandler.allReadyForMoreThanOneFrame)
            {
                AudioManager audioManager = AudioManager.Get();
                if (audioManager != null)
                {
                    audioManager.Play("startGame");
                }
                CharacterSelectHandler.startButtonAvailable = false;
                List<Player> list = PlayerHandler.Get().PlayerList();
                list.Clear();
                int num = 1;
                NamedSpriteList abilityIcons = SteamManager.instance.abilityIcons;
                for (int i = 0; i < __instance.characterSelectBoxes.Length; i++)
                {
                    if (__instance.characterSelectBoxes[i].menuState == CharSelectMenu.ready)
                    {
                        PlayerInit playerInit = __instance.characterSelectBoxes[i].playerInit;
                        Player player = new Player(num, playerInit.team);
                        player.Color = __instance.playerColors[playerInit.color].playerMaterial;
                        player.UsesKeyboardAndMouse = playerInit.usesKeyboardMouse;
                        player.CanUseAbilities = true;
                        player.inputDevice = playerInit.inputDevice;
                        player.Abilities = new List<GameObject>(3);
                        player.AbilityIcons = new List<Sprite>(3);
                        player.Abilities.Add(abilityIcons.sprites[playerInit.ability0].associatedGameObject);
                        player.AbilityIcons.Add(abilityIcons.sprites[playerInit.ability0].sprite);
                        Settings settings = Settings.Get();
                        if (settings != null && settings.NumberOfAbilities > 1)
                        {
                            player.Abilities.Add(abilityIcons.sprites[playerInit.ability1].associatedGameObject);
                            player.AbilityIcons.Add(abilityIcons.sprites[playerInit.ability1].sprite);
                        }
                        Settings settings2 = Settings.Get();
                        if (settings2 != null && settings2.NumberOfAbilities > 2)
                        {
                            player.Abilities.Add(abilityIcons.sprites[playerInit.ability2].associatedGameObject);
                            player.AbilityIcons.Add(abilityIcons.sprites[playerInit.ability2].sprite);
                        }
                        player.CustomKeyBinding = playerInit.keybindOverride;
                        num++;
                        list.Add(player);
                    }
                }
                GameSession.Init();
                //SceneManager.LoadScene("Level1");
                Debug.Log("TryStartGame_inner");
                if (Plugin.MapJsons.Length != 0)
                {
                    //its max exsclusive min inclusinve
                    Plugin.CurrentMapIndex = UnityEngine.Random.Range(0, Plugin.MapJsons.Length);
                    Dictionary<string, object> MetaData = MiniJSON.Json.Deserialize(Plugin.MetaDataJsons[Plugin.CurrentMapIndex]) as Dictionary<string, object>;
                    var type = Convert.ToString(MetaData["MapType"]);
                    switch (type)
                    {
                        case "space":
                            GameSession.currentLevel = (byte)Plugin.SpaceMapId;
                            break;
                        case "snow":
                            GameSession.currentLevel = (byte)Plugin.SnowMapId;
                            break;
                        default:
                            GameSession.currentLevel = (byte)Plugin.GrassMapId;
                            break;
                    }
                    var UUID = Convert.ToInt32(MetaData["MapUUID"]);
                    Plugin.CurrentMapUUID = UUID;
                    SceneManager.LoadScene((int)(6 + GameSession.CurrentLevel()), LoadSceneMode.Single);
                }
                else SceneManager.LoadScene("Level1");

                if (!WinnerTriangleCanvas.HasBeenSpawned)
                {
                    SceneManager.LoadScene("winnerTriangle", LoadSceneMode.Additive);
                }
                Debug.Log(WinnerTriangleCanvas.instance);
            }
            return false;
        }
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void Awake_MapMaker_Plug2(CharacterSelectHandler __instance)
        {
            GameSessionHandler.LeaveGame(false, false);
        }
    }
    [HarmonyPatch(typeof(CharacterSelectHandler_online))]
    public class CharacterSelectHandler_onlinePatches
    {
        [HarmonyPatch("ForceStartGame")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(CharacterSelectHandler_online __instance, ref PlayerColors pcs)
        {
            MonoBehaviour.print("FORCE START GAME");
            if (pcs == null)
            {
                pcs = CharacterSelectHandler_online.selfRef.playerColors;
            }
            SteamManager.startParameters.currentLevel = GameSession.currentLevel;
            StartRequestPacket startParameters = SteamManager.startParameters;
            Updater.ReInit();
            List<Player> list = new List<Player>();
            Updater.InitSeed(startParameters.seed);
            if (startParameters.nrOfPlayers > 0)
            {
                list.Add(CharacterSelectHandler_online.InitPlayer(1, startParameters.p1_color, startParameters.p1_team, startParameters.p1_ability1, startParameters.p1_ability2, startParameters.p1_ability3, (int)startParameters.nrOfAbilites, pcs));
            }
            if (startParameters.nrOfPlayers > 1)
            {
                list.Add(CharacterSelectHandler_online.InitPlayer(2, startParameters.p2_color, startParameters.p2_team, startParameters.p2_ability1, startParameters.p2_ability2, startParameters.p2_ability3, (int)startParameters.nrOfAbilites, pcs));
            }
            if (startParameters.nrOfPlayers > 2)
            {
                list.Add(CharacterSelectHandler_online.InitPlayer(3, startParameters.p3_color, startParameters.p3_team, startParameters.p3_ability1, startParameters.p3_ability2, startParameters.p3_ability3, (int)startParameters.nrOfAbilites, pcs));
            }
            if (startParameters.nrOfPlayers > 3)
            {
                list.Add(CharacterSelectHandler_online.InitPlayer(4, startParameters.p4_color, startParameters.p4_team, startParameters.p4_ability1, startParameters.p4_ability2, startParameters.p4_ability3, (int)startParameters.nrOfAbilites, pcs));
            }
            Player player = null;
            if (GameLobby.isPlayingAReplay)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Id == 1)
                    {
                        player = list[i];
                        break;
                    }
                }
            }
            else if (startParameters.p1_id == SteamClient.SteamId)
            {
                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j].Id == 1)
                    {
                        player = list[j];
                        break;
                    }
                }
            }
            else if (startParameters.p2_id == SteamClient.SteamId)
            {
                for (int k = 0; k < list.Count; k++)
                {
                    if (list[k].Id == 2)
                    {
                        player = list[k];
                        break;
                    }
                }
            }
            else if (startParameters.p3_id == SteamClient.SteamId)
            {
                for (int l = 0; l < list.Count; l++)
                {
                    if (list[l].Id == 3)
                    {
                        player = list[l];
                        break;
                    }
                }
            }
            else if (startParameters.p4_id == SteamClient.SteamId)
            {
                for (int m = 0; m < list.Count; m++)
                {
                    if (list[m].Id == 4)
                    {
                        player = list[m];
                        break;
                    }
                }
            }
            for (int n = 0; n < list.Count; n++)
            {
                switch (list[n].Id)
                {
                    case 1:
                        list[n].steamId = startParameters.p1_id;
                        break;
                    case 2:
                        list[n].steamId = startParameters.p2_id;
                        break;
                    case 3:
                        list[n].steamId = startParameters.p3_id;
                        break;
                    case 4:
                        list[n].steamId = startParameters.p4_id;
                        break;
                }
            }
            player.IsLocalPlayer = true;
            player.inputDevice = CharacterSelectHandler_online.localPlayerInit.inputDevice;
            player.UsesKeyboardAndMouse = CharacterSelectHandler_online.localPlayerInit.usesKeyboardMouse;
            player.CustomKeyBinding = CharacterSelectHandler_online.localPlayerInit.keybindOverride;
            CharacterSelectHandler_online.startButtonAvailable = false;
            PlayerHandler.Get().SetPlayerList(list);
            SteamManager.instance.StartHostedGame();
            AudioManager audioManager = AudioManager.Get();
            if (audioManager != null)
            {
                audioManager.Play("startGame");
            }
            GameSession.Init();
            if (GameLobby.isPlayingAReplay)
            {
                SceneManager.LoadScene((int)(startParameters.currentLevel + 6));
            }
            else
            {
                SceneManager.LoadScene((int)(startParameters.currentLevel + 6));
            }
            if (!WinnerTriangleCanvas.HasBeenSpawned)
            {
                SceneManager.LoadScene("winnerTriangle", LoadSceneMode.Additive);
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(SteamManager))]
    public class SteamManagerPatches
    {
        [HarmonyPatch("OnLobbyMemberJoinedCallback")]
        [HarmonyPostfix]
        private static void Awake_MapMaker_Plug(Lobby lobby, Friend friend, SteamManager __instance)
        {
            if (__instance.currentLobby.Id != lobby.Id)
            {
                return;
            }
            //if we own the lobby send the new player the list of our map uuids in order so we can know if we both have the same maps.
            if (SteamManager.LocalPlayerIsLobbyOwner)
            {
                List<int> UUIDs = new List<int>();
                foreach(string json in Plugin.MetaDataJsons)
                {
                    Dictionary<string, object> Meta = MiniJSON.Json.Deserialize(json) as Dictionary<string, object>;
                    UUIDs.Add(Convert.ToInt32(Meta["MapUUID"]));
                }
                NetworkingStuff.MapUUIDsChannel.SendMessage(UUIDs.ToArray());
            }
        }
        [HarmonyPatch("OnLevelWasLoaded")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(int level, SteamManager __instance)
        {
            __instance.networkClient = Host.host;
            return false;
        }
        [HarmonyPatch("HostGame")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug2(PlayerInit hostPlayer, SteamManager __instance)
        {
            Plugin.CurrentMapIndex = UnityEngine.Random.Range(0, Plugin.MapJsons.Length);
            //its max exsclusive min inclusinve
            if (Plugin.MapJsons.Length != 0)
            {
                UnityEngine.Debug.Log($"we have {Plugin.MapJsons.Length} maps");
                UnityEngine.Debug.Log($"map index is {Plugin.CurrentMapIndex}");
                Dictionary<string, object> MetaData = MiniJSON.Json.Deserialize(Plugin.MetaDataJsons[Plugin.CurrentMapIndex]) as Dictionary<string, object>;
                var type = Convert.ToString(MetaData["MapType"]);
                UnityEngine.Debug.Log("getting map type");
                switch (type)
                {
                    case "space":
                        GameSession.currentLevel = (byte)Plugin.SpaceMapId;
                        break;
                    case "snow":
                        GameSession.currentLevel = (byte)Plugin.SnowMapId;
                        break;
                    default:
                        GameSession.currentLevel = (byte)Plugin.GrassMapId;
                        break;
                }
                var UUID = Convert.ToInt32(MetaData["MapUUID"]);
                Plugin.CurrentMapUUID = UUID;
            }
            __instance.currentLobby.SetData("LFM", "0");
            __instance.currentLobby.SetFriendsOnly();
            __instance.currentLobby.SetJoinable(false);
            SteamManager.startParameters = default(StartRequestPacket);
            ushort num = __instance.nextStartGameSeq;
            __instance.nextStartGameSeq = (ushort)(num + 1);
            SteamManager.startParameters.seqNum = num;
            SteamManager.startParameters.nrOfPlayers = (byte)(__instance.connectedPlayers.Count + 1);
            SteamManager.startParameters.nrOfAbilites = (byte)Settings.Get().NumberOfAbilities;
            SteamManager.startParameters.currentLevel = GameSession.CurrentLevel();
            SteamManager.startParameters.seed = (uint)Environment.TickCount;
            SteamManager.startParameters.p1_id = SteamClient.SteamId;
            SteamManager.startParameters.p1_team = (byte)hostPlayer.team;
            SteamManager.startParameters.p1_color = (byte)hostPlayer.color;
            SteamManager.startParameters.p1_ability1 = (byte)hostPlayer.ability0;
            SteamManager.startParameters.p1_ability2 = (byte)hostPlayer.ability1;
            SteamManager.startParameters.p1_ability3 = (byte)hostPlayer.ability2;
            if (__instance.connectedPlayers.Count > 0)
            {
                SteamManager.startParameters.p2_id = __instance.connectedPlayers[0].id;
                SteamManager.startParameters.p2_team = __instance.connectedPlayers[0].lobby_team;
                SteamManager.startParameters.p2_color = (byte)__instance.connectedPlayers[0].lobby_color;
                SteamManager.startParameters.p2_ability1 = __instance.connectedPlayers[0].lobby_ability1;
                SteamManager.startParameters.p2_ability2 = __instance.connectedPlayers[0].lobby_ability2;
                SteamManager.startParameters.p2_ability3 = __instance.connectedPlayers[0].lobby_ability3;
            }
            if (__instance.connectedPlayers.Count > 1)
            {
                SteamManager.startParameters.p3_id = __instance.connectedPlayers[1].id;
                SteamManager.startParameters.p3_team = __instance.connectedPlayers[1].lobby_team;
                SteamManager.startParameters.p3_color = (byte)__instance.connectedPlayers[1].lobby_color;
                SteamManager.startParameters.p3_ability1 = __instance.connectedPlayers[1].lobby_ability1;
                SteamManager.startParameters.p3_ability2 = __instance.connectedPlayers[1].lobby_ability2;
                SteamManager.startParameters.p3_ability3 = __instance.connectedPlayers[1].lobby_ability3;
            }
            if (__instance.connectedPlayers.Count > 2)
            {
                SteamManager.startParameters.p4_id = __instance.connectedPlayers[2].id;
                SteamManager.startParameters.p4_team = __instance.connectedPlayers[2].lobby_team;
                SteamManager.startParameters.p4_color = (byte)__instance.connectedPlayers[2].lobby_color;
                SteamManager.startParameters.p4_ability1 = __instance.connectedPlayers[2].lobby_ability1;
                SteamManager.startParameters.p4_ability2 = __instance.connectedPlayers[2].lobby_ability2;
                SteamManager.startParameters.p4_ability3 = __instance.connectedPlayers[2].lobby_ability3;
            }
            byte b = (byte)(SteamManager.instance.dlc.HasDLC() ? 1 : 0);
            for (int i = 0; i < __instance.connectedPlayers.Count; i++)
            {
                if (__instance.connectedPlayers[i].ownsFullGame)
                {
                    b = (byte)((int)b | 1 << i + 1);
                }
            }
            SteamManager.startParameters.isDemoMask = b;
            SteamManager.instance.EncodeCurrentStartParameters_forReplay(ref SteamManager.instance.networkClient.EncodedStartRequest, SteamManager.startParameters, false);
            var betterStartRequestPacket = new BetterStartRequestPacket
            {
                startRequest = SteamManager.startParameters,
                MapIndex = Plugin.CurrentMapIndex
            };
            NetworkingStuff.StartChannel.SendMessage(betterStartRequestPacket);
            return false;
        }
        [HarmonyPatch("HostNextLevel")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug2(Player hostPlayer, NamedSpriteList abilityIcons, SteamManager __instance)
        {
            Plugin.CurrentMapIndex = UnityEngine.Random.Range(0, Plugin.MapJsons.Length);
            //its max exsclusive min inclusinve
            if (Plugin.MapJsons.Length != 0)
            {
                UnityEngine.Debug.Log($"we have {Plugin.MapJsons.Length} maps");
                UnityEngine.Debug.Log($"map index is {Plugin.CurrentMapIndex}");
                Dictionary<string, object> MetaData = MiniJSON.Json.Deserialize(Plugin.MetaDataJsons[Plugin.CurrentMapIndex]) as Dictionary<string, object>;
                var type = Convert.ToString(MetaData["MapType"]);
                UnityEngine.Debug.Log("getting map type");
                switch (type)
                {
                    case "space":
                        GameSession.currentLevel = (byte)Plugin.SpaceMapId;
                        break;
                    case "snow":
                        GameSession.currentLevel = (byte)Plugin.SnowMapId;
                        break;
                    default:
                        GameSession.currentLevel = (byte)Plugin.GrassMapId;
                        break;
                }
                var UUID = Convert.ToInt32(MetaData["MapUUID"]);
                Plugin.CurrentMapUUID = UUID;
            }
            GameSession.CurrentLevel();
            SteamManager.startParameters.frameBufferSize = (byte)Host.CurrentDelayBufferSize;
            SteamManager.startParameters.seed = (uint)Environment.TickCount;
            SteamManager.startParameters.nrOfPlayers = (byte)(__instance.connectedPlayers.Count + 1);
            Debug.Log($"nrOfPlayers is {SteamManager.startParameters.nrOfPlayers}");
            SteamManager.startParameters.currentLevel = GameSession.CurrentLevel();
            SteamManager.startParameters.p1_ability1 = (byte)abilityIcons.IndexOf(hostPlayer.Abilities[0].name);
            if (Settings.Get().NumberOfAbilities > 1)
            {
                SteamManager.startParameters.p1_ability2 = (byte)abilityIcons.IndexOf(hostPlayer.Abilities[1].name);
            }
            if (Settings.Get().NumberOfAbilities > 2)
            {
                SteamManager.startParameters.p1_ability3 = (byte)abilityIcons.IndexOf(hostPlayer.Abilities[2].name);
            }
            if (__instance.connectedPlayers.Count > 0)
            {
                SteamManager.startParameters.p2_ability1 = __instance.connectedPlayers[0].lobby_ability1;
                SteamManager.startParameters.p2_ability2 = __instance.connectedPlayers[0].lobby_ability2;
                SteamManager.startParameters.p2_ability3 = __instance.connectedPlayers[0].lobby_ability3;
            }
            if (__instance.connectedPlayers.Count > 1)
            {
                SteamManager.startParameters.p3_ability1 = __instance.connectedPlayers[1].lobby_ability1;
                SteamManager.startParameters.p3_ability2 = __instance.connectedPlayers[1].lobby_ability2;
                SteamManager.startParameters.p3_ability3 = __instance.connectedPlayers[1].lobby_ability3;
            }
            if (__instance.connectedPlayers.Count > 2)
            {
                SteamManager.startParameters.p4_ability1 = __instance.connectedPlayers[2].lobby_ability1;
                SteamManager.startParameters.p4_ability2 = __instance.connectedPlayers[2].lobby_ability2;
                SteamManager.startParameters.p4_ability3 = __instance.connectedPlayers[2].lobby_ability3;
            }
            SteamManager.instance.EncodeCurrentStartParameters_forReplay(ref SteamManager.instance.networkClient.EncodedStartRequest, SteamManager.startParameters, false);
            var betterStartRequestPacket = new BetterStartRequestPacket
            {
                startRequest = SteamManager.startParameters,
                MapIndex = Plugin.CurrentMapIndex
            };
            NetworkingStuff.StartChannel.SendMessage(betterStartRequestPacket);
            return false;
        }
    }
    [HarmonyPatch(typeof(GameSessionHandler))]
    public class GameSessionHandlerPatches
    {
        [HarmonyPatch("LeaveGame")]
        [HarmonyPostfix]
        private static void Awake_MapMaker_Plug(GameSessionHandler __instance)
        {
            Debug.Log($"number of players is {SteamManager.startParameters.nrOfPlayers}");
            //throw new NotImplementedException();
            //fill the MapJsons array up
            ZipArchive[] zipArchives = Plugin.MyZipArchives;
            Plugin.zipArchives = Plugin.MyZipArchives;
            //Create a List for the json for a bit
            List<string> JsonList = new List<string>();
            List<string> MetaDataList = new();
            foreach (ZipArchive zipArchive in zipArchives)
            {
                //get the first .boplmap file if there is multiple. (THERE SHOULD NEVER BE MULTIPLE .boplmap's IN ONE .zip)
                JsonList.Add(Plugin.GetFileFromZipArchive(zipArchive, Plugin.IsBoplMap)[0]);
                MetaDataList.Add(Plugin.GetFileFromZipArchive(zipArchive, Plugin.IsMetaDataFile)[0]);
            }
            Plugin.MapJsons = JsonList.ToArray();
            Plugin.MetaDataJsons = MetaDataList.ToArray();
        }
        [HarmonyPatch("AnimateOutLevel")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug2(GameSessionHandler __instance)
        {
            //idk why but unity crashes when it calls FindObjectOfType sometimes when online so this should hopefuly fix that.
            Plugin.averageCamera.enabled = false;
            for (int i = 0; i < __instance.grounds.Length; i++)
            {
                if (!(__instance.grounds[i] == null) && !__instance.grounds[i].IsDestroyed)
                {
                    AnimateVelocity component = __instance.grounds[i].ThisGameObject().GetComponent<AnimateVelocity>();
                    if (component != null)
                    {
                        component.enabled = false;
                    }
                }
            }
            for (int j = 0; j < __instance.startPositions.Length; j++)
            {
                if (!(__instance.grounds[j] == null) && !__instance.grounds[j].IsDestroyed)
                {
                    __instance.targetPositions[j] = __instance.grounds[j].GetComponent<FixTransform>().position;
                }
            }
            for (int k = 0; k < __instance._playedArrivalSound.Length; k++)
            {
                __instance._playedArrivalSound[k] = false;
            }
            __instance._t = __instance.startProgressOfAnimateInGrounds;
            __instance.levelAnimationRoutine.WhileLoop(new Func<Fix, bool>(__instance.AnimateOutLoop));
            return false;
        }
    }
    [HarmonyPatch(typeof(PlayerAverageCamera))]
    public class PlayerAverageCameraPatches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void Awake_MapMaker_Plug(PlayerAverageCamera __instance)
        {
            Plugin.averageCamera = __instance;
        }
    }
    [HarmonyPatch(typeof(Host))]
    public class HostPatches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void Awake_MapMaker_Plug(Host __instance)
        {
            if (__instance.gameObject.scene.name == "DontDestroyOnLoad")
            {
                Host.host = __instance;
                Debug.Log($"host is {__instance.gameObject.name}");
            }

        }
        [HarmonyPatch("Init")]
        [HarmonyPostfix]
        private static void Awake_MapMaker_Plug2(Host __instance)
        {
            try
            {
                Debug.Log(__instance.gameObject.name);
            }
            catch
            {
                Debug.LogError("Host isnt on a gameobject????");
            }
            //throw new NotImplementedException();
        }
    }
    [HarmonyPatch(typeof(ShakableCamera))]
    public class ShakableCameraPatches
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void Awake_MapMaker_Plug(ShakableCamera __instance)
        {
            Plugin.shakableCamera = __instance;
        }
    }
    [HarmonyPatch(typeof(Beam))]
    public class BeamPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(Beam __instance)
        {
            Updater.RegisterUpdatable(__instance);
            __instance.animator = __instance.GetComponent<SpriteAnimator>();
            __instance.spriteRen = __instance.GetComponent<SpriteRenderer>();
            __instance.body = __instance.GetComponent<PlayerBody>();
            __instance.ability = __instance.GetComponent<Ability>();
            __instance.physics = __instance.GetComponent<PlayerPhysics>();
            __instance.shakeCam = Plugin.shakableCamera;
            __instance.hurtbox = __instance.GetComponent<DPhysicsBox>();
            __instance.origFixTransOffset = __instance.GetComponent<FixTransform>().offset;
            __instance.origExtents = __instance.hurtbox.CalcExtents();
            __instance.physics.OnAttachedToGround += __instance.OnGrounded;
            __instance.physics.OnNoLongerGrounded += __instance.OnUngrounded;
            return false;
        }
    }
    [HarmonyPatch(typeof(ControlPlatform))]
    public class ControlPlatformPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(ControlPlatform __instance)
        {
            Updater.RegisterUpdatable(__instance);
            __instance.animator = __instance.GetComponent<SpriteAnimator>();
            __instance.spriteRen = __instance.GetComponent<SpriteRenderer>();
            __instance.body = __instance.GetComponent<PlayerBody>();
            __instance.ability = __instance.GetComponent<Ability>();
            __instance.physics = __instance.GetComponent<PlayerPhysics>();
            __instance.shakeCam = Plugin.shakableCamera;
            __instance.hurtbox = __instance.GetComponent<DPhysicsBox>();
            return false;
        }
    }
    [HarmonyPatch(typeof(CastSpell))]
    public class CastSpellPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(CastSpell __instance)
        {
            Updater.RegisterUpdatable(__instance);
            __instance.playerCol = __instance.GetComponent<PlayerCollision>();
            __instance.animator = __instance.GetComponent<SpriteAnimator>();
            __instance.spriteRen = __instance.GetComponent<SpriteRenderer>();
            __instance.body = __instance.GetComponent<PlayerBody>();
            __instance.ability = __instance.GetComponent<Ability>();
            __instance.physics = __instance.GetComponent<PlayerPhysics>();
            __instance.bigCollider = __instance.GetComponent<DPhysicsCircle>();
            __instance.shakeCam = Plugin.shakableCamera;
            __instance.fixTrans = __instance.GetComponent<FixTransform>();
            return false;
        }
    }
    [HarmonyPatch(typeof(InputUpdater))]
    public class InputUpdaterPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPostfix]
        private static void Awake_MapMaker_Plug(InputUpdater __instance)
        {
            Plugin.playerInputs.Add(__instance.gameObject.GetComponent<PlayerInput>());
        }
    }
    [HarmonyPatch(typeof(CursorUpdater))]
    public class CursorUpdaterPatches
    {
        [HarmonyPatch("initialize")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(CursorUpdater __instance)
        {
            if (__instance.alwaysHideCursorInstead)
            {
                Cursor.SetCursor(null, Vector2.one / 2f, CursorMode.Auto);
                Cursor.visible = false;
                return false;
            }
            if (!Cursor.visible)
            {
                Cursor.visible = true;
            }
            //clean up the input list
            List<PlayerInput> inputs = new();
            foreach (var input in Plugin.playerInputs)
            {
                if (input != null)
                {
                    inputs.Add(input);
                }
            }
            Plugin.playerInputs = inputs;
            PlayerInput[] array = inputs.ToArray();
            if (array.Length == 0)
            {
                __instance.SetCursor(__instance.defaultCursorColor);
                return false;
            }
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].currentControlScheme.Equals("KeyboardAndMouse"))
                {
                    int claimerId = array[i].GetComponent<InputUpdater>().GetClaimerId();
                    Player player = PlayerHandler.Get().GetPlayer(claimerId);
                    if (player != null)
                    {
                        UnityEngine.Color color = player.Color.GetColor("_ShadowColor");
                        __instance.SetCursor(color);
                    }
                    return false;
                }
            }
            return false;
        }
    }
    [HarmonyPatch(typeof(MenuAbilitySelector))]
    public class MenuAbilitySelectorPatches
    {
        [HarmonyPatch("Awake")]
        [HarmonyPrefix]
        private static bool Awake_MapMaker_Plug(MenuAbilitySelector __instance)
        {
            __instance.mgas = __instance.GetComponent<MidGameAbilitySelect>();
            List<Player> list = new List<Player>();
            list.Add(new Player(1, 1));
            PlayerHandler.Get().SetPlayerList(list);
            List<GameObject> list2 = new List<GameObject>();
            list2.Add(__instance.mgas.AbilityIcons.sprites[0].associatedGameObject);
            list2.Add(__instance.mgas.AbilityIcons.sprites[1].associatedGameObject);
            list2.Add(__instance.mgas.AbilityIcons.sprites[2].associatedGameObject);
            list[0].Abilities = list2;
            List<Sprite> list3 = new List<Sprite>();
            list3.Add(__instance.mgas.AbilityIcons.sprites[0].sprite);
            list3.Add(__instance.mgas.AbilityIcons.sprites[1].sprite);
            list3.Add(__instance.mgas.AbilityIcons.sprites[2].sprite);
            list[0].AbilityIcons = list3;
            __instance.mgas.SetPlayer(1);
            __instance.playerId = 1;
            //clean up the input list
            List<PlayerInput> inputs = new();
            foreach (var input in Plugin.playerInputs)
            {
                if (input != null)
                {
                    inputs.Add(input);
                }
            }
            Plugin.playerInputs = inputs;
            //get the InputUpdaters 
            List<InputUpdater> updaters = new();
            foreach (var input in inputs)
            {
                updaters.Add(input.gameObject.GetComponent<InputUpdater>());
            }
            InputUpdater[] array = updaters.ToArray();
            //InputUpdater[] array = UnityEngine.Object.FindObjectsOfType<InputUpdater>();
            int num = 0;
            while (num < array.Length && num < list.Count)
            {
                array[num].Init(list[num].Id);
                num++;
            }
            return false;
        }
    }
}