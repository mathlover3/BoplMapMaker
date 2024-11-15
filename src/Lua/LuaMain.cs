//using AsmResolver.PE.DotNet.Metadata.Tables.Rows;
//using AsmResolver.PE.DotNet.ReadyToRun;
using BoplFixedMath;
using MonoMod.Utils;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Debugging;
using MoonSharp.VsCodeDebugger;
using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Configuration;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem.Composites;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;
using UnityEngine.UIElements;
using static MapMaker.Lua_stuff.LuaPlayerPhysicsProxy;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace MapMaker.Lua_stuff
{
    public class LuaMain : LogicGate
    {
        public static LuaSpawner LuaSpawner;
        public static ShootBlink ShootBlinkObject;
        public string Name = "Lua default name";
        public string code = null;
        public Script script;
        public static MoonSharpVsCodeDebugServer server;
        public static bool UseDebugServer = false;
        private static Fix deltaTime = Fix.Zero;
        private static string FileNameToGet;
        public int ZipIndex;
        //handled in plugin
        public static List<PlayerPhysics> players = new();
        public void Awake()
        {
            if (LuaSpawner == null)
            {
                //if there is none we create one for everyone.
                LuaSpawner = gameObject.AddComponent<LuaSpawner>();
            }
            if (ShootBlinkObject == null)
            {
                //if there is none we create one for everyone.
                ShootBlinkObject = gameObject.AddComponent<ShootBlink>();
            }
        }
        void OnDestroy()
        {
            if (UseDebugServer)
            {
                //remove this script from the debuger.
                server.Detach(script);
            }
        }
        public Script SetUpScriptFuncsons()
        {
            //dont want to let people use librays that would give them acsess outside of the game like os and io now do we? also no time package eather as thats just asking for desinks.
            Script script = new Script(CoreModules.Preset_HardSandbox | CoreModules.ErrorHandling | CoreModules.Coroutine | CoreModules.Metatables);
            script.Globals["SpawnArrow"] = (object)SpawnArrowDouble;
            script.Globals["SpawnSpike"] = (object)SpawnSpike;
            script.Globals["SpawnSpikeAtPosition"] = (object)SpawnSpikeAtPosition;
            script.Globals["SpawnGrenade"] = (object)SpawnGrenadeDouble;
            script.Globals["SpawnMine"] = (object)SpawnMineDouble;
            script.Globals["SpawnMissile"] = (object)SpawnMissileDouble;
            script.Globals["SpawnBlackHole"] = (object)SpawnBlackHoleDouble;
            script.Globals["SpawnAbilityPickup"] = (object)SpawnAbilityPickupDouble;
            script.Globals["SpawnSmokeGrenade"] = (object)SpawnSmokeGrenadeDouble;
            script.Globals["SpawnExplosion"] = (object)SpawnExplosionDouble;
            script.Globals["SpawnBoulder"] = (object)SpawnBoulderDouble;
            script.Globals["SpawnPlatform"] = (object)SpawnPlatform;
            script.Globals["RaycastRoundedRect"] = (object)RaycastRoundedRect;
            script.Globals["GetClosestPlayer"] = (object)GetClosestPlayer;
            script.Globals["GetAllPlatforms"] = (object)GetAllPlatforms;
            script.Globals["GetAllPlayers"] = (object)GetAllPlayers;
            script.Globals["GetAllBoplBodys"] = (object)GetAllBoplBodys;
            script.Globals["GetAllBlackHoles"] = (object)GetAllBlackHoles;
            script.Globals["ShootBlink"] = (object)ShootBlink;
            script.Globals["ShootGrow"] = (object)ShootGrow;
            script.Globals["ShootShrink"] = (object)ShootShrink;
            script.Globals["GetDeltaTime"] = (object)GetDeltaTime;
            script.Globals["GetTimeSinceLevelLoad"] = (object)GetTimeSinceLevelLoad;
            script.Globals["IsTimeStopped"] = (object)IsTimeStopped;
            script.Globals["GetInputValueWithId"] = (object)GetInputValueWithId;
            script.Globals["SetOutputWithId"] = (object)SetOutputWithId;
            script.Globals["GetFileFromMapFile"] = (object)GetFileFromMapFile;
            script.Options.DebugPrint = s => { Console.WriteLine(s); };
            // Register just MyClass, explicitely.
            UserData.RegisterProxyType<LuaPlayerPhysicsProxy, PlayerPhysics>(r => new LuaPlayerPhysicsProxy(r));
            UserData.RegisterProxyType<PlatformProxy, StickyRoundedRectangle>(r => new PlatformProxy(r));
            UserData.RegisterProxyType<BoplBodyProxy, BoplBody>(r => new BoplBodyProxy(r));
            UserData.RegisterProxyType<BlackHoleProxy, BlackHole>(r => new BlackHoleProxy(r));
            return script;
        }
        public void Register()
        {
            UUID = Plugin.NextUUID;
            Plugin.NextUUID++;
            SignalSystem.RegisterLogicGate(this);
            SignalSystem.RegisterGateThatAlwaysRuns(this);
        }
        public DynValue RunScript(string scriptCode, Dictionary<string, object> paramiters, Script script)
        {
            foreach (var Key in paramiters.Keys)
            {
                script.Globals[Key] = paramiters[Key];
            }
            try
            {
                DynValue res = script.DoString(scriptCode);
                switch (res.Type)
                {
                    case DataType.String:
                        //UnityEngine.Debug.Log(res.String);
                        break;
                    case DataType.Boolean:
                        //UnityEngine.Debug.Log(res.Boolean);
                        break;
                    case DataType.Number:
                        //UnityEngine.Debug.Log(res.Number);
                        break;
                    default:
                        //UnityEngine.Debug.Log(res.Type);
                        break;
                }
                return res;
            }
            catch (ScriptRuntimeException e)
            {
                Console.WriteLine($"ERROR IN LUA SCRIPT {Name} Error: {e.DecoratedMessage}");
                Plugin.logger.LogError($"ERROR IN LUA SCRIPT {Name} Error: {e.DecoratedMessage}");
                return DynValue.Nil;
            }
            catch (MoonSharp.Interpreter.SyntaxErrorException e)
            {
                Console.WriteLine($"ERROR PARSING LUA SCRIPT {Name} Error: {e.DecoratedMessage}");
                Plugin.logger.LogError($"ERROR PARSING LUA SCRIPT {Name} Error: {e.DecoratedMessage}");
                return DynValue.Nil;
            }
            catch (InternalErrorException e)
            {
                Console.WriteLine($"CONGRATS! YOU BROKE THE INTERPITER IN SCRIPT {Name} Error: {e} pls send me the map and perferably also the replay so i can report the bug.");              
                UnityEngine.Debug.LogError($"CONGRATS! YOU BROKE THE INTERPITER IN SCRIPT {Name} Error: {e} pls send me the map and perferably also the replay so i can report the bug.");
                Plugin.logger.LogError($"CONGRATS! YOU BROKE THE INTERPITER IN SCRIPT {Name} Error: {e} pls send me the map and perferably also the replay so i can report the bug.");
                return DynValue.Nil;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Congrats! you found a error in my code! pls send the replay of this to me so i can fix it. and the error, err: {e} ");
                UnityEngine.Debug.LogError($"Congrats! you found a error in my code! pls send the replay of this to me so i can fix it. and the error, err: {e} ");
                Plugin.logger.LogError($"Congrats! you found a error in my code! pls send the replay of this to me so i can fix it. and the error, err: {e} ");
                return DynValue.Nil;
            }
            /*foreach (var Key in script.Globals.Keys)
            {
                var value = script.Globals[Key];
                UnityEngine.Debug.Log(value);
                if (value.ToString() == "MoonSharp.Interpreter.CallbackFunction")
                {
                    var func = (MoonSharp.Interpreter.CallbackFunction)value;
                    UnityEngine.Debug.Log(func.Name);
                }
            }*/

        }
        public static BoplBody SpawnSpike(StickyRoundedRectangle attachedGround, double percentAroundSurface, double scale, double offset)
        {
            if (attachedGround == null)
            {
                throw new ScriptRuntimeException("attachedGround can't be nil when spawning a spike.");
            }
            // for some reason negative offset values push the spike forward and vice versa, so I reverse the sign here to make it more intuitive in the API
            return LuaSpawner.SpawnSpike((Fix)percentAroundSurface, (Fix)(offset*-1), attachedGround, (Fix)scale);
        }

        public static BoplBody SpawnSpikeAtPosition(StickyRoundedRectangle attachedGround, double surfacePosX, double surfacePosY, double scale, double offset)
        {
            if (attachedGround == null)
            {
                throw new ScriptRuntimeException("attachedGround can't be nil when spawning a spike.");
            }
            return LuaSpawner.SpawnSpikeAtPosition((Fix)surfacePosX, (Fix)surfacePosY, (Fix)(offset*-1), attachedGround, (Fix)scale);
        }
        public static BoplBody SpawnArrowDouble(double posX, double posY, double scale, double StartVelX, double StartVelY, float R, float G, float B, float A)
        {
            return SpawnArrow((Fix)posX, (Fix)posY, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY, R, G, B, A);
        }
        public static BoplBody SpawnArrow(Fix posX, Fix posY, Fix scale, Fix StartVelX, Fix StartVelY, float R, float G, float B, float A)
        {
            return LuaSpawner.SpawnArrow(new Vec2(posX, posY), scale, new Vec2(StartVelX, StartVelY), new Color(R,G,B,A));
        }
        public static BoplBody SpawnMissileDouble(double posX, double posY, double scale, double StartVelX, double StartVelY, float R, float G, float B, float A)
        {
            return SpawnMissile((Fix)posX, (Fix)posY, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY, R, G, B, A);
        }
        public static BoplBody SpawnMissile(Fix posX, Fix posY, Fix scale, Fix StartVelX, Fix StartVelY, float R, float G, float B, float A)
        {
            return LuaSpawner.SpawnMissile(new Vec2(posX, posY), scale, new Vec2(StartVelX, StartVelY), new Color(R, G, B, A));
        }
        public static BoplBody SpawnMineDouble(double posX, double posY, double scale, double StartVelX, double StartVelY, double chaseRadius, bool chase)
        {
            return SpawnMine((Fix)posX, (Fix)posY, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY, (Fix)chaseRadius, chase);
        }
        public static BoplBody SpawnMine(Fix posX, Fix posY, Fix scale, Fix StartVelX, Fix StartVelY, Fix chaseRadius, bool chase)
        {
            return LuaSpawner.SpawnMine(new Vec2(posX, posY), scale, new Vec2(StartVelX, StartVelY), chaseRadius, chase);
        }
        public static BoplBody SpawnGrenadeDouble(double posX, double posY, double scale, double StartVelX, double StartVelY, double StartAngularVelocity, double FuseSeconds = -1)
        {
            return SpawnGrenade((Fix)posX, (Fix)posY, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY, (Fix)StartAngularVelocity, (Fix)FuseSeconds);
        }
        public static BoplBody SpawnGrenade(Fix posX, Fix posY, Fix scale, Fix StartVelX, Fix StartVelY, Fix StartAngularVelocity, Fix FuseSeconds)
        {
            return LuaSpawner.SpawnGrenade(new Vec2(posX, posY), Fix.Zero, scale, new Vec2(StartVelX, StartVelY), StartAngularVelocity, FuseSeconds);
        }
        public static BlackHole SpawnBlackHoleDouble(double posX, double posY, double size)
        {
            return SpawnBlackHole((Fix)posX, (Fix)posY, (Fix)size);
        }
        public static BlackHole SpawnBlackHole(Fix posX, Fix posY, Fix size)
        {
            return LuaSpawner.SpawnBlackHole(new Vec2(posX, posY), size);
        }
        public static void SpawnAbilityPickupDouble(double posX, double posY, double scale, double StartVelX, double StartVelY)
        {
            SpawnAbilityPickup((Fix)posX, (Fix)posY, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY);
        }
        public static void SpawnAbilityPickup(Fix posX, Fix posY, Fix scale, Fix StartVelX, Fix StartVelY)
        {
            LuaSpawner.SpawnAbilityPickup(new Vec2(posX, posY), scale, new Vec2(StartVelX, StartVelY));
        }
        public static BoplBody SpawnSmokeGrenadeDouble(double posX, double posY, double scale, double StartVelX, double StartVelY, double StartAngularVelocity)
        {
            return SpawnSmokeGrenade((Fix)posX, (Fix)posY, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY, (Fix)StartAngularVelocity);
        }
        public static BoplBody SpawnSmokeGrenade(Fix posX, Fix posY, Fix scale, Fix StartVelX, Fix StartVelY, Fix StartAngularVelocity)
        {
            return LuaSpawner.SpawnSmokeGrenade(new Vec2(posX, posY), Fix.Zero, scale, new Vec2(StartVelX, StartVelY), StartAngularVelocity);
        }
        public static void SpawnExplosionDouble(double posX, double posY, double scale)
        {
            SpawnExplosion((Fix)posX, (Fix)posY, (Fix)scale);
        }
        public static void SpawnExplosion(Fix posX, Fix posY, Fix scale)
        {
            LuaSpawner.SpawnNormalExplosion(new Vec2(posX, posY), scale);
        }
        public static StickyRoundedRectangle SpawnBoulderDouble(double posX, double posY, double scale, double StartVelX, double StartVelY, double StartAngularVelocity, string type, float R, float G, float B, float A)
        {
            PlatformType platformType;
            var color = Color.white;
            switch (type)
            {
                case "snow":
                    platformType = PlatformType.snow;
                    break;
                case "grass":
                    platformType = PlatformType.grass;
                    break;
                case "ice":
                    platformType = PlatformType.ice;
                    break;
                case "space":
                    platformType = PlatformType.space;
                    break;
                case "slime":
                    platformType = PlatformType.slime;
                    color = new Color(R, G, B, A);
                    break;
                case "robot":
                    platformType = PlatformType.robot;
                    break;
                default:
                    throw new ScriptRuntimeException($"{type} is not a valid platform type.");
            }
            return SpawnBoulder((Fix)posX, (Fix)posY, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY, (Fix)StartAngularVelocity, platformType, color);
        }
        public static StickyRoundedRectangle SpawnBoulder(Fix posX, Fix posY, Fix scale, Fix StartVelX, Fix StartVelY, Fix StartAngularVelocity, PlatformType type, Color color)
        {
            var boulder = PlatformApi.PlatformApi.SpawnBoulder(new Vec2(posX, posY), scale, type, color);
            var dphysicsRoundedRect = boulder.hitbox;
            dphysicsRoundedRect.velocity = new Vec2(StartVelX, StartVelY);
            dphysicsRoundedRect.angularVelocity = StartAngularVelocity;
            return dphysicsRoundedRect.stickyRR;
        }
        public static StickyRoundedRectangle SpawnPlatform(double posX, double posY, double Width, double Height, double Radius, double Rot, float R, float G, float B, float A)
        {
            var plat = PlatformApi.PlatformApi.SpawnPlatform((Fix)posX, (Fix)posY, (Fix)Width, (Fix)Height, (Fix)Radius, (Fix)Rot * (Fix)PhysTools.DegreesToRadians);
            var color = new Color(R, G, B, A);
            plat.GetComponent<SpriteRenderer>().color = color;
            return plat.GetComponent<StickyRoundedRectangle>();
        }
        public static DynValue RaycastRoundedRect(double posX, double posY, double angle, double maxDist)
        {
            var pos = new Vec2((Fix)posX, (Fix)posY);
            var angleRads = (Fix)angle * (Fix)PhysTools.DegreesToRadians;
            var dir = new Vec2((Fix)angleRads);
            var dist = (Fix)maxDist;
            var result = DetPhysics.Get().RaycastToClosestRoundedRect(pos, dir, dist);
            DynValue platform;

            if (result.pp.fixTrans != null && result.pp.fixTrans.gameObject != null)
            {
                var sticky = result.pp.fixTrans.gameObject.GetComponent<StickyRoundedRectangle>();
                platform = UserData.Create(sticky);
            }
            else
            {
                platform = DynValue.Nil;
            }
            return DynValue.NewTuple(
                DynValue.NewNumber((double)result.nearDist),
                platform
            );

        }
        public static DynValue GetClosestPlayer(double posX, double posY)
        {
            //PlayerList (1)
            var playerlist = GameObject.Find("PlayerList");
            if (playerlist == null)
            {
                playerlist = GameObject.Find("PlayerList (1)");
            }
            var players = playerlist.transform;
            PlayerPhysics CurrentPlayer = null;
            Fix bestDist = Fix.MaxValue;
            foreach (Transform player in players)
            {
                if (player.gameObject.name == "Player(Clone)")
                {
                    var trans = player.gameObject.GetComponent<FixTransform>();
                    var pos = trans.position;
                    var pos2 = new Vec2((Fix)posX, (Fix)posY);
                    var dist = Vec2.Distance(pos, pos2);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        CurrentPlayer = player.gameObject.GetComponent<PlayerPhysics>();
                    }
                }
            }
            if (CurrentPlayer != null)
            {
                return UserData.Create(CurrentPlayer);
            }
            return DynValue.Nil;
        }
        public static DynValue GetAllPlayers(Script script)
        {
            List<PlayerPhysics> Players = new();
            //lists are refrece types so i cant drectly set it and instead must copy it manualy
            foreach (PlayerPhysics player in players)
            {
                Players.Add(player);
            }
            //remove any invalid players from the players list
            foreach (PlayerPhysics player in players)
            {
                if (player == null)
                {
                    Players.Remove(player);
                    //end this iteratson of the loop
                    continue;
                }
                try
                {
                    if (player.gameObject == null)
                    {
                        //this code will never be reaced as if theres no gameobject just the act of doing player.gameObject cause a null ref error even if player isnt null. but just in case
                        Players.Remove(player);
                    }
                }
                catch
                {
                    Players.Remove(player);
                }
            }
            players = Players;
            //DynValue.FromObject
            return DynValue.NewTuple(
                DynValue.NewNumber(players.Count),
                DynValue.FromObject(script, players)
            );
        }
        public static DynValue GetAllBoplBodys(Script script)
        {
            BoplBody[] allObjects = Resources.FindObjectsOfTypeAll(typeof(BoplBody)) as BoplBody[];
            List<BoplBody> result = new();
            foreach (var body in allObjects)
            {
                if (body.gameObject.scene.name != null && body.HasBeenInitialized && !body.physicsCollider.IsDestroyed)
                {
                    result.Add(body);
                }
            }
            return DynValue.NewTuple(
                DynValue.NewNumber(result.Count),
                DynValue.FromObject(script, result)
            );
        }

        public static DynValue GetAllBlackHoles(Script script)
        {
            BlackHole[] allHoles = Resources.FindObjectsOfTypeAll(typeof(BlackHole)) as BlackHole[];
            List<BlackHole> result = new();
            foreach (var hole in allHoles)
            {
                if (hole.gameObject.scene.name != null && hole.dCircle.initHasBeenCalled && !hole.GetComponent<FixTransform>().IsDestroyed)
                {
                    result.Add(hole);
                }
            }
            return DynValue.NewTuple(
                DynValue.NewNumber(result.Count),
                DynValue.FromObject(script, result)
            );
        }

        public static void ShootBlink(double posX, double posY, double Angle, double minPlayerDuration, double WallDuration, double WallDelay, double WallShake)
        {
            ShootBlinkObject.minPlayerDuration = (Fix)minPlayerDuration;
            ShootBlinkObject.WallDelay = (Fix)WallDelay;
            ShootBlinkObject.WallDuration = (Fix)WallDuration;
            ShootBlinkObject.WallShake = (Fix)WallShake;
            var dir = (Fix)Angle * (Fix)PhysTools.DegreesToRadians;
            var ignore = false;
            ShootBlinkObject.Shoot(new Vec2((Fix)posX, (Fix)posY), new Vec2(dir), ref ignore);
        }
        public void ShootGrow(double posX, double posY, double Angle, double ScaleMultiplyer, double PlayerMultiplyer, double blackHoleGrowth)
        {
            //if we already have one we remove it as it might be a strink one.
            ShootScaleChange shootScaleChange = GetComponent<ShootScaleChange>();
            if (shootScaleChange != null)
            {
                Destroy(shootScaleChange);
            }
            var ShootScaleChangeComp = ShootRay.GrowGameObjectPrefab.GetComponent<ShootScaleChange>();
            shootScaleChange = ShootRay.CopyComponent<ShootScaleChange>(ShootScaleChangeComp, gameObject);
            shootScaleChange.Awake();
            var scaleChanger = ShootScaleChangeComp.ScaleChangerPrefab;
            scaleChanger.multiplier = (Fix)ScaleMultiplyer;
            scaleChanger.PlayerMultiplier = (Fix)PlayerMultiplyer;
            scaleChanger.smallNonPlayersMultiplier = (Fix)ScaleMultiplyer;
            shootScaleChange.ScaleChangerPrefab = scaleChanger;
            //rot is in radiens
            var rot = (Fix)Angle * (Fix)PhysTools.DegreesToRadians;
            var rotVec = new Vec2(rot);
            shootScaleChange.blackHoleGrowthInverse01 = Fix.One / (Fix)blackHoleGrowth;
            bool ignore = false;
            shootScaleChange.Shoot(new Vec2((Fix)posX, (Fix)posY), rotVec, ref ignore, 255);
        }
        public void ShootShrink(double posX, double posY, double Angle, double ScaleMultiplyer, double PlayerMultiplyer, double blackHoleGrowth)
        {
            //if we already have one we remove it as it might be a strink one.
            ShootScaleChange shootScaleChange = GetComponent<ShootScaleChange>();
            if (shootScaleChange != null)
            {
                Destroy(shootScaleChange);
            }
            var ShootScaleChangeComp = ShootRay.StrinkGameObjectPrefab.GetComponent<ShootScaleChange>();
            shootScaleChange = ShootRay.CopyComponent<ShootScaleChange>(ShootScaleChangeComp, gameObject);
            shootScaleChange.Awake();
            var scaleChanger = ShootScaleChangeComp.ScaleChangerPrefab;
            scaleChanger.multiplier = (Fix)ScaleMultiplyer;
            scaleChanger.PlayerMultiplier = (Fix)PlayerMultiplyer;
            scaleChanger.smallNonPlayersMultiplier = (Fix)ScaleMultiplyer;
            shootScaleChange.ScaleChangerPrefab = scaleChanger;
            //rot is in radiens
            var rot = (Fix)Angle * (Fix)PhysTools.DegreesToRadians;
            var rotVec = new Vec2(rot);
            shootScaleChange.blackHoleGrowthInverse01 = Fix.One / (Fix)blackHoleGrowth;
            bool ignore = false;
            shootScaleChange.Shoot(new Vec2((Fix)posX, (Fix)posY), rotVec, ref ignore, 255);
        }
        public static DynValue GetAllPlatforms(Script script)
        {
            List<StickyRoundedRectangle> result = new();
            foreach (GameObject platform in PlatformApi.PlatformApi.PlatformList)
            {
                if (platform != null)
                {
                    var sticky = platform.GetComponent<StickyRoundedRectangle>();
                    if (sticky != null)
                    {
                        result.Add(sticky);
                    }
                }
            }
            return DynValue.NewTuple(
                DynValue.NewNumber(result.Count),
                DynValue.FromObject(script, result)
            );
        }
        public static double GetDeltaTime()
        {
            return (double)deltaTime;
        }
        public static double GetTimeSinceLevelLoad()
        {
            return (double)Updater.SimTimeSinceLevelLoaded;
        }
        public bool GetInputValueWithId(int id)
        {
            if (InputSignals.Count < id || id < 1)
            {
                throw new ScriptRuntimeException($"Logic gate {Name} doesnt have input with id {id} it only has {InputSignals.Count} inputs");
            }
            return InputSignals[id - 1].IsOn;
        }
        public void SetOutputWithId(int id, bool value)
        {
            if (OutputSignals.Count < id || id < 1)
            {
                throw new ScriptRuntimeException($"Logic gate {Name} doesnt have ouput with id {id} it only has {OutputSignals.Count} ouputs");
            }
            OutputSignals[id - 1].IsOn = value;
        }
        public bool IsTimeStopped()
        {
            return GameTime.IsTimeStopped();
        }
        public List<byte> GetFileFromMapFile(string FileName)
        {
            FileNameToGet = FileName;
            var File = Plugin.GetFileFromZipArchiveBytes(Plugin.zipArchives[ZipIndex], IsCorrectFile);
            if (File.Length == 0)
            {
                throw new ScriptRuntimeException($"File {FileName} doesnt exsit in map file");
            }
            return File[0].ToList();
        }
        public static bool IsCorrectFile(string path)
        {
            if (path == FileNameToGet) return true;
            //will only be reached if its not a boplmap
            return false;
        }
        public override void Logic(Fix SimDeltaTime)
        {
            if (script == null)
            {
                script = SetUpScriptFuncsons();
                if (UseDebugServer)
                {
                    server.AttachToScript(script, Name);
                    UnityEngine.Debug.Log($"attaching script {Name} to debug server");
                }
            }
            Dictionary<string, object> paramiters = new Dictionary<string, object>();
            deltaTime = SimDeltaTime;
            RunScript(code, paramiters, script);
        }
        public static DynValue Vec2ToTuple(Vec2 vec2)
        {
            return DynValue.NewTuple(
                DynValue.NewNumber((double)vec2.x),
                DynValue.NewNumber((double)vec2.y)
            );
        }
    }
    public class LuaPlayerPhysicsProxy
    {
        PlayerPhysics target;
        PlayerBody body;

        [MoonSharpHidden]
        public LuaPlayerPhysicsProxy(PlayerPhysics p)
        {
            target = p;
            body = p.body;
        }
        public double GetSpeed() { return (double)target.Speed; }
        public double GetGroundedSpeed() { return (double)target.groundedSpeed; }
        public double GetMaxSpeed() { return (double)target.maxSpeed; }
        public double GetJumpStrength() { return (double)target.jumpStrength; }
        public double GetAccel() { return (double)target.accel; }
        public double GetGravityAccel() { return (double)target.gravity_accel; }
        public double GetGravityMaxFallSpeed() { return (double)target.gravity_maxFallSpeed; }
        public double GetJumpExtraXStrength() { return (double)target.jumpExtraXStrength; }
        public double GetJumpKeptMomentum() { return (double)target.jumpKeptMomentum; }
        public DynValue GetPosition(Script script) { return LuaMain.Vec2ToTuple(body.position); }
        public void SetPosition(double PosX, double PosY) 
        {
            target.UnGround();
            body.position = new Vec2((Fix)PosX, (Fix)PosY);
        }
        public double GetScale() { return (double)body.fixtrans.Scale; }
        public void SetScale(double scale) { PlayerHandler.Get().GetPlayer(body.idHolder.GetPlayerId()).Scale = (Fix)scale; }
        public void GetAirAccel(double NewValue) {target.airAccel = (Fix)NewValue; }
        public double GetMass() { return (double)(Fix.One / target.inverseMass01); }
        public void SetSpeed(double NewValue) {target.Speed = (Fix)NewValue; }
        public void SetGroundedSpeed(double NewValue) {target.groundedSpeed = (Fix)NewValue; }
        public void SetMaxSpeed(double NewValue) {target.maxSpeed = (Fix)NewValue; }
        public void SetJumpStrength(double NewValue) {target.jumpStrength = (Fix)NewValue; }
        public void SetAccel(double NewValue) {target.accel = (Fix)NewValue; }
        public void SetGravityAccel(double NewValue) {target.gravity_accel = (Fix)NewValue; }
        public void SetGravityMaxFallSpeed(double NewValue) {target.gravity_maxFallSpeed = (Fix)NewValue; }
        public void SetJumpExtraXStrength(double NewValue) {target.jumpExtraXStrength = (Fix)NewValue; }
        public void SetJumpKeptMomentum(double NewValue) {target.jumpKeptMomentum = (Fix)NewValue; }
        public void SetAirAccel(double NewValue) {target.airAccel = (Fix)NewValue; }
        public void SetMass(double NewValue) { target.inverseMass01 = Fix.One / (Fix)NewValue; }
        public void AddForce(double ForceX, double ForceY)
        {
            target.AddForce(new Vec2((Fix)ForceX, (Fix)ForceY));
        }
        public string GetAbility(int index)
        {
            if (index >= 1 && index <= 3)
            {
                var TrueIndex = index - 1;
                var SlimeControaler = target.gameObject.GetComponent<SlimeController>();
                var icons = SteamManager.instance.abilityIconsFull;
                var AbilitySprite = SlimeControaler.AbilityReadyIndicators[TrueIndex].spriteRen.sprite;
                var AbilityIndex = icons.IndexOf(AbilitySprite);
                return AbilityToString((Ability)AbilityIndex);
            }
            else
            {
                throw new ScriptRuntimeException($"ability slot {index} is not a valid ability slot. only ability slots 1, 2 and 3 are valid");
            }
        }
        public void SetAbility(int index, string ability, bool PlayAbilityPickupSound)
        {
            if (index >= 1 && index <= 3)
            {
                //code attapted from https://github.com/almafa64/almafa64-bopl-mods/blob/master/AbilityRandomizer/Plugin.cs
                var TrueIndex = index - 1;
                var SlimeControaler = target.gameObject.GetComponent<SlimeController>();
                var icons = SteamManager.instance.abilityIconsFull;
                var Ability = StringToAbility(ability);
                var NamedSprite = icons.sprites[(int)Ability];
                GameObject AbilityPrefab = NamedSprite.associatedGameObject;
                GameObject AbilityObject = FixTransform.InstantiateFixed(AbilityPrefab, Vec2.zero, Fix.Zero);
                AbilityMonoBehaviour abilityMonoBehaviour = AbilityObject.GetComponent<AbilityMonoBehaviour>();
                if (SlimeControaler.abilities.Count == 3)
                {
                    SlimeControaler.abilities[TrueIndex] = abilityMonoBehaviour;
                    AbilityReadyIndicator indicator = SlimeControaler.AbilityReadyIndicators[TrueIndex];
                    PlayerHandler.Get().GetPlayer(SlimeControaler.playerNumber).CurrentAbilities[TrueIndex] = AbilityPrefab;
                    indicator.SetSprite(NamedSprite.sprite, true);
                    indicator.ResetAnimation();
                    SlimeControaler.abilityCooldownTimers[TrueIndex] = (Fix)0;
                    if (PlayAbilityPickupSound)
                    {
                        AudioManager.Get().Play("abilityPickup");
                    }
                }
                else if (SlimeControaler.abilities.Count > 0 && SlimeControaler.AbilityReadyIndicators[0] != null)
                {
                    SlimeControaler.abilities.Add(abilityMonoBehaviour);
                    PlayerHandler.Get().GetPlayer(SlimeControaler.playerNumber).CurrentAbilities.Add(AbilityPrefab);
                    SlimeControaler.AbilityReadyIndicators[SlimeControaler.abilities.Count - 1].SetSprite(NamedSprite.sprite, true);
                    SlimeControaler.AbilityReadyIndicators[SlimeControaler.abilities.Count - 1].ResetAnimation();
                    SlimeControaler.abilityCooldownTimers[SlimeControaler.abilities.Count - 1] = (Fix)0;
                    for (int i = 0; i < SlimeControaler.abilities.Count; i++)
                    {
                        if (SlimeControaler.abilities[i] == null || SlimeControaler.abilities[i].IsDestroyed)
                        {
                            return;
                        }
                        SlimeControaler.AbilityReadyIndicators[i].gameObject.SetActive(true);
                        SlimeControaler.AbilityReadyIndicators[i].InstantlySyncTransform();
                    }
                }
            }
            else
            {
                throw new ScriptRuntimeException($"ability slot {index} is not a valid ability slot. only ability slots 1, 2 and 3 are valid");
            }
            

        }
        public int GetAbilityCount()
        {
            return target.gameObject.GetComponent<SlimeController>().abilities.Count;
        }
        public double GetAbilityCooldownRemaining(int index)
        {
            if (index >= 1 && index <= 3)
            {
                var slimeController = target.gameObject.GetComponent<SlimeController>();
                if (slimeController.abilities.Count == 0)
                {
                    return 1000000;
                }
                if (slimeController.abilities.Count == 1)
                {
                    if (index == 1) return (double)(slimeController.abilities[0].GetCooldown() - slimeController.abilityCooldownTimers[0]);
                    else return 1000000;
                }
                if (slimeController.abilities.Count == 2)
                {
                    if (index == 1) return (double)(slimeController.abilities[0].GetCooldown() - slimeController.abilityCooldownTimers[0]);
                    if (index == 2) return (double)(slimeController.abilities[1].GetCooldown() - slimeController.abilityCooldownTimers[1]);
                    else return 1000000;
                }
                return (double)(slimeController.abilities[index - 1].GetCooldown() - slimeController.abilityCooldownTimers[index - 1]);
            }
            else throw new ScriptRuntimeException($"index {index} is not a valid ability index. valid indexs are 1, 2 and 3");

        }
        public void SetAbilityCooldownRemaining(int index, double NewRemainingCooldown)
        {
            if (index >= 1 && index <= 3)
            {
                var slimeController = target.gameObject.GetComponent<SlimeController>();
                if (slimeController.abilities.Count == 1)
                {
                    if (index == 1)
                    {
                        slimeController.abilityCooldownTimers[0] = slimeController.abilities[0].GetCooldown() - (Fix)NewRemainingCooldown;
                    }
                }
                if (slimeController.abilities.Count == 2)
                {
                    if (index == 1) slimeController.abilityCooldownTimers[0] = slimeController.abilities[0].GetCooldown() - (Fix)NewRemainingCooldown;
                    if (index == 2) slimeController.abilityCooldownTimers[1] = slimeController.abilities[1].GetCooldown() - (Fix)NewRemainingCooldown;
                }
                if (slimeController.abilities.Count == 3)
                {
                    slimeController.abilityCooldownTimers[index - 1] = slimeController.abilities[index - 1].GetCooldown() - (Fix)NewRemainingCooldown;
                }
            }
            else throw new ScriptRuntimeException($"index {index} is not a valid ability index. valid indexs are 1, 2 and 3");

        }
        public double GetAbilityMaxCooldown(int index)
        {
            if (index >= 1 && index <= 3)
            {
                var slimeController = target.gameObject.GetComponent<SlimeController>();
                if (slimeController.abilities.Count == 0)
                {
                    return 1000000;
                }
                if (slimeController.abilities.Count == 1)
                {
                    if (index == 1) return (double)(slimeController.abilities[0].GetCooldown());
                    else return 1000000;
                }
                if (slimeController.abilities.Count == 2)
                {
                    if (index == 1) return (double)(slimeController.abilities[0].GetCooldown());
                    if (index == 2) return (double)(slimeController.abilities[1].GetCooldown());
                    else return 1000000;
                }
                return (double)(slimeController.abilities[index - 1].GetCooldown());
            }
            else throw new ScriptRuntimeException($"index {index} is not a valid ability index. valid indexs are 1, 2 and 3");
        }
        public bool IsDisappeared()
        {
            return !target.gameObject.activeInHierarchy;
        }
        public DynValue GetPlatform()
        {
            if (target.attachedGround != null && target.isGrounded)
            {
                return UserData.Create(target.attachedGround);
            }
            else { return DynValue.Nil; }
        }
        public string GetClassType()
        {
            return "Player";
        }
        [MoonSharpHidden]
        public static Ability StringToAbility(string str)
        {
            switch (str)
            {
                case "Random":
                    return Ability.Random;
                case "Roll":
                    return Ability.Roll;
                case "Dash":
                    return Ability.Dash;
                case "Grenade":
                    return Ability.Grenade;
                case "Bow":
                    return Ability.Bow;
                case "Engine":
                    return Ability.Engine;
                case "Blink":
                    return Ability.Blink;
                case "Gust":
                    return Ability.Gust;
                case "Grow":
                    return Ability.Grow;
                case "Rock":
                    return Ability.Rock;
                case "Missile":
                    return Ability.Missle;
                case "Spike":
                    return Ability.Spike;
                case "TimeStop":
                    return Ability.TimeStop;
                case "SmokeGrenade":
                    return Ability.SmokeGrenade;
                case "Platform":
                    return Ability.Platform;
                case "Revive":
                    return Ability.Revive;
                case "Shrink":
                    return Ability.Shrink;
                case "BlackHole":
                    return Ability.BlackHole;
                case "Invisibility":
                    return Ability.Invisibility;
                case "Meteor":
                    return Ability.Meteor;
                case "Macho":
                    return Ability.Macho;
                case "Push":
                    return Ability.Push;
                case "Tesla":
                    return Ability.Tesla;
                case "Mine":
                    return Ability.Mine;
                case "Teleport":
                    return Ability.Teleport;
                case "Drill":
                    return Ability.Drill;
                case "Grapple":
                    return Ability.Grapple;
                case "Beam":
                    return Ability.Beam;
                case "Duplicator":
                    return Ability.Duplicator;
                default:
                    throw new ScriptRuntimeException($"{str} is not a valid ability");
            }
        }
        [MoonSharpHidden]
        public static string AbilityToString(Ability ability)
        {
            switch (ability)
            {
                case Ability.Random:
                    return "Random";
                case Ability.Roll:
                    return "Roll";
                case Ability.Dash:
                    return "Dash";
                case Ability.Grenade:
                    return "Grenade";
                case Ability.Bow:
                    return "Bow";
                case Ability.Engine:
                    return "Engine";
                case Ability.Blink:
                    return "Blink";
                case Ability.Gust:
                    return "Gust";
                case Ability.Grow:
                    return "Grow";
                case Ability.Rock:
                    return "Rock";
                case Ability.Missle:
                    return "Missile";
                case Ability.Spike:
                    return "Spike";
                case Ability.TimeStop:
                    return "TimeStop";
                case Ability.SmokeGrenade:
                    return "SmokeGrenade";
                case Ability.Platform:
                    return "Platform";
                case Ability.Revive:
                    return "Revive";
                case Ability.Shrink:
                    return "Shrink";
                case Ability.BlackHole:
                    return "BlackHole";
                case Ability.Invisibility:
                    return "Invisibility";
                case Ability.Meteor:
                    return "Meteor";
                case Ability.Macho:
                    return "Macho";
                case Ability.Push:
                    return "Push";
                case Ability.Tesla:
                    return "Tesla";
                case Ability.Mine:
                    return "Mine";
                case Ability.Teleport:
                    return "Teleport";
                case Ability.Drill:
                    return "Drill";
                case Ability.Grapple:
                    return "Grapple";
                case Ability.Beam:
                    return "Beam";
                case Ability.Duplicator:
                    return "Duplicator";
                default:
                    return "Unknown/Modded/None";
            }
        }
        public enum Ability
        {
            None = 0,
            Random = 1,
            Dash = 2,
            Grenade = 3,
            Bow = 4,
            Engine = 5,
            Blink = 6,
            Gust = 7,
            Grow = 8,
            Rock = 9,
            Missle = 10,
            Spike = 11,
            TimeStop = 12,
            SmokeGrenade = 13,
            Platform = 14,
            Revive = 15,
            Roll = 16,
            Shrink = 17,
            BlackHole = 18,
            Invisibility = 19,
            Meteor = 20,
            Macho = 21,
            Push = 22,
            Tesla = 23,
            Mine = 24,
            Teleport = 25,
            Drill = 26,
            Grapple = 27,
            Beam = 28,
            Duplicator = 29
        }
    }
    public class PlatformProxy
    {
        public StickyRoundedRectangle target;
        public ShakablePlatform shakable;
        [MoonSharpHidden]
        public PlatformProxy(StickyRoundedRectangle p)
        {
            target = p;
            try
            {
                shakable = p.gameObject.GetComponent<ShakablePlatform>();
            }
            catch
            {
                //do nothing. the reson we have to do it like this is that if a component doesnt have a gameobject just the act of doing...
                //p.gameObject causes a null ref error.
            }
            
        }
        public string GetClassType()
        {
            return "Platform";
        }
        public DynValue GetHome()
        {
            if (!IsBoulder())
            {
                return LuaMain.Vec2ToTuple(PlatformApi.PlatformApi.GetHome(target.gameObject));
            }
            else
            {
                throw new ScriptRuntimeException("Can't call GetHome on a Platform object that is a boulder. check IsBoulder before calling!");
            }
        }
        public double GetHomeRot()
        {
            if (!IsBoulder())
            {
                return (double)(PlatformApi.PlatformApi.GetHomeRot(target.gameObject) * (Fix)PhysTools.RadiansToDegrees);
            }
            else
            {
                throw new ScriptRuntimeException("Can't call GetHomeRot on a Platform object that is a boulder. check IsBoulder before calling!");
            }
        }
        public double GetScale()
        {
            return (double)target.GetComponent<BoplBody>().Scale;
        }
        public void SetScale(double scale)
        {
            target.GetComponent<BoplBody>().Scale = (Fix)scale;
        }
        public double GetBaseScale()
        {
            return (double)target.baseScaleForPlatform;
        }
        public void SetBaseScale(double scale)
        {
            target.baseScaleForPlatform = (Fix)scale;
        }
        public double GetMaxScale()
        {
            return (double)target.DPhysicsShape().MaxScale;
        }
        public void SetMaxScale(double scale)
        {
            target.DPhysicsShape().MaxScale = (Fix)scale;
        }
        public double GetMinScale()
        {
            return (double)target.DPhysicsShape().MinScale;
        }
        public void SetMinScale(double scale)
        {
            target.DPhysicsShape().MinScale = (Fix)scale;
        }
        public string GetPlatformType()
        {
            switch (target.platformType)
            {
                case PlatformType.grass:
                    return "grass";
                case PlatformType.snow:
                    return "snow";
                case PlatformType.ice:
                    return "ice";
                case PlatformType.space:
                    return "space";
                case PlatformType.robot:
                    return "robot";
                case PlatformType.slime:
                    return "slime";
                default:
                    return "custom";

            }
        }
        public void SetHome(double PosX, double PosY)
        {
            if (!IsBoulder())
            {
                PlatformApi.PlatformApi.SetHome(target.gameObject, new Vec2((Fix)PosX, (Fix)PosY));
            }
            else
            {
                throw new ScriptRuntimeException("Can't call SetHome on a Platform object that is a boulder. check IsBoulder before calling!");
            }
        }
        public void SetHomeRot(double NewRot)
        {
            if (!IsBoulder())
            {
                PlatformApi.PlatformApi.SetHomeRot(target.gameObject, (Fix)NewRot * (Fix)PhysTools.DegreesToRadians);
            }
            else
            {
                throw new ScriptRuntimeException("Can't call SetHomeRot on a Platform object that is a boulder. check IsBoulder before calling!");
            }
        }
        public void MakeVectorField(double centerX, double centerY, double delaySeconds, double orbitSpeed, double expandSpeed, double normalSpeedFriction, double DeadZoneDist, double OrbitAccelerationMulitplier, double targetRadius, double ovalness01)
        {
            if (IsBoulder())
            {
                throw new ScriptRuntimeException("Can't call MakeVectorField on a Platform object that is a boulder. check IsBoulder before calling!");
            }
            GameObject platformObject = target.gameObject;
            PlatformApi.PlatformApi.AddVectorFieldPlatform(platformObject, (Fix)delaySeconds, (Fix)orbitSpeed, (Fix)expandSpeed, new Vec2((Fix)centerX, (Fix)centerY), (Fix)normalSpeedFriction, (Fix)DeadZoneDist, (Fix)OrbitAccelerationMulitplier, (Fix)targetRadius, (Fix)ovalness01);
            Vec2 pos = PlatformApi.PlatformApi.GetHome(platformObject);
            PlatformApi.PlatformApi.SetHome(platformObject, pos + new Vec2((Fix)1, (Fix)0));
        }
        public void ShakePlatform(double Duratson, double ShakeAmount)
        {
            if (shakable)
            {
                shakable.AddShake((Fix)Duratson, (Fix)ShakeAmount);
            }
        }
        public void DropAllPlayers(double DropForce)
        {
            target.DropAllAttachedPlayers(255, (Fix)DropForce);
        }
        public DynValue GetBoplBody()
        {
            var boplBody = target.GetGroundBody();
            if (boplBody)
            {
                return UserData.Create(boplBody);
            }
            return DynValue.Nil;
        }
        public bool IsBoulder()
        {
            return target.GetComponent<Boulder>() != null;
        }
        public bool IsResizable()
        {
            return target.GetComponent<ResizablePlatform>() != null;
        }
        public void ResizePlatform(double Width, double Height, double Radius)
        {
            if (IsResizable())
            {
                target.GetComponent<ResizablePlatform>().ResizePlatform((Fix)Height, (Fix)Width, (Fix)Radius);
            }
            else
            {
                throw new ScriptRuntimeException("called ResizePlatform on a platform thats not Resizable. check IsResizable() before calling!");
            }
        }
        public DynValue GetPlatformSize()
        {
            var exstents = target.extents;
            var r = target.rr.radius;
            return DynValue.NewTuple(
    DynValue.NewNumber((double)exstents.x),
    DynValue.NewNumber((double)exstents.y),
    DynValue.NewNumber((double)r)
);
        }
        public DynValue GetTrueWidthAndHeight()
        {
            var bounds = target.GetBoundingRect();
            var MaxX = Fix.Max(bounds.bl.x, bounds.br.x);
            MaxX = Fix.Max(MaxX, bounds.tl.x);
            MaxX = Fix.Max(MaxX, bounds.tr.x);
            var MinX = Fix.Min(bounds.bl.x, bounds.br.x);
            MinX = Fix.Min(MinX, bounds.tl.x);
            MinX = Fix.Min(MinX, bounds.tr.x);
            var MaxY = Fix.Max(bounds.bl.y, bounds.br.y);
            MaxY = Fix.Max(MaxY, bounds.tl.y);
            MaxY = Fix.Max(MaxY, bounds.tr.y);
            var MinY = Fix.Min(bounds.bl.y, bounds.br.y);
            MinY = Fix.Min(MinY, bounds.tl.y);
            MinY = Fix.Min(MinY, bounds.tr.y);
            var Width = Fix.Abs(MaxX - MinX);
            var Height = Fix.Abs(MaxY - MinY);
            return DynValue.NewTuple(
    DynValue.NewNumber((double)Width),
    DynValue.NewNumber((double)Height)
);
        }
    }

    public class BoplBodyProxy
    {
        public BoplBody target;
        public string type = "Unknown/Modded";
        [MoonSharpHidden]
        public BoplBodyProxy(BoplBody p)
        {
            target = p;
            if (target.GetComponent<Grenade>() != null) { type = "Grenade"; }
            if (target.GetComponent<Arrow>() != null) { type = "Arrow"; }
            if (target.GetComponent<RocketEngine>() != null) { type = "RocketEngine"; }
            if (target.GetComponent<Mine>() != null) { type = "Mine"; }
            if (target.GetComponent<SimpleSparkNode>() != null) { type = "Tesla"; }
            if (target.GetComponent<DynamicAbilityPickup>() != null) { type = "AbilityPickup"; }
            if (target.GetComponent<Missile>() != null) { type = "Missile"; }
            if (target.GetComponent<Boulder>() != null) { type = "MatchoBoulder"; }
            if (target.GetComponent<SpikeAttack>() != null) { type = "Spike"; }
            if (target.GetComponent<BounceBall>() != null) { type = "Rock"; }
            if (target.GetComponent<FlammableSmoke>() != null) { type = "Smoke"; }
            if (target.GetComponent<SmokeGrenadeExplode2>() != null) { type = "Smoke Grenade"; }
            if (target.GetComponent<ShakablePlatform>() != null) { type = "Platform"; }

        }
        public string GetClassType()
        {
            return "BoplBody";
        }
        public string GetObjectType()
        {
            return type;
        }
        public bool HasBeenInitialized()
        {
            return target.HasBeenInitialized;
        }
        public bool IsBeingDestroyed()
        {
            return target.physicsCollider.IsDestroyed || target.gameObject == null;
        }
        public bool CanTrigger()
        {
            return type == "Grenade" || type == "RocketEngine" || type == "Mine" || type == "AbilityPickup" || type == "Missile" || type == "Smoke Grenade" || type == "Smoke";
        }
        public DynValue GetPos()
        {
            return LuaMain.Vec2ToTuple(target.position);
        }
        public double GetRot()
        {
            return (double)(target.rotation * (Fix)PhysTools.RadiansToDegrees);
        }
        public double GetScale()
        {
            if (!target.HasBeenInitialized)
            {
                throw new ScriptRuntimeException("called GetScale on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called GetScale on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            return (double)target.Scale;
        }
        public DynValue GetVelocity()
        {
            if (!target.HasBeenInitialized)
            {
                throw new ScriptRuntimeException("called GetVelocity on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called GetVelocity on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            return LuaMain.Vec2ToTuple(target.velocity);
        }
        public double GetMass() 
        {
            if (!target.HasBeenInitialized)
            {
                throw new ScriptRuntimeException("called GetMass on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called GetMass on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            return (double)(Fix.One / target.InverseMass);
        }
        public void SetPos(double x, double y)
        {

            target.position = new Vec2((Fix)x, (Fix)y);
        }
        public void SetRot(double Rot)
        {
            var rot = Fix.SlowMod((Fix)Rot, (Fix)360);
            target.rotation = (Fix)Rot * (Fix)PhysTools.DegreesToRadians;
        }
        public void SetScale(double Scale)
        {
            if (!target.HasBeenInitialized)
            {
                throw new ScriptRuntimeException("called SetScale on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called SetScale on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            target.Scale = (Fix)Scale;
        }
        public void SetVelocity(double VelX, double VelY)
        {
            if (!target.HasBeenInitialized)
            {
                throw new ScriptRuntimeException("called SetVelocity on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called SetVelocity on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            target.velocity = new Vec2((Fix)VelX, (Fix)VelY);
        }
        public void SetMass(double Mass) 
        {
            if (!target.HasBeenInitialized)
            {
                throw new ScriptRuntimeException("called SetMass on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called SetMass on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            target.InverseMass = Fix.One / (Fix)Mass; 
        }
        //these dont work due to them only being used to create the PhysicsBody when the object is created.
        //if i realy want to add this i would have to replace the physics body in PhysicsBodyList.physicsBodies for the PhysicsBodyList that holds the object
        //witch depends on if its a DPhysicsBox, a DPhysicsRect, or a DPhysicsRoundedRect. also dont froget to update the CompositeBody.combinedBody if it is a CompositeBody.
        //but thankfuly those are the only 2 spots you would need to change because those are the only 2 spots that store PhysicsBody's.
        //feel free to do this if you want too. if you do also add some more PhysicsBody propertys too.
        //the reson it needs to be done this way is because PhysicsBody is a struct not a class and structs are passed by copying and pasting them whenever you edit them.
        //unlike classes that are passed by refrence.
        //unless ofc im just stupid and this isnt how it works at all and this code does work witch looking at the games code the more and more likely it seems that it would work.
        //looking at the games code it seems to drectly set stuff and it works a lot everywhere in the games code. like in DPhysicsBox.set_velocity(Vec2).
        //problum with doing that is i would need to know if its a DPhysicsBox, a DPhysicsRect, or a DPhysicsRoundedRect.
        //witch is probaly posable with some cursed try catchs with casts inside them. or some way in c# i dont know of to check what class is inhariting a given interface.
        //oh wow this is a long comment. lol. didnt intend that. this comment took like 10 minites to write lol
        /*public double GetBouncyness()
        {
            if (!target.HasBeenInitialized)
            {
                throw new ScriptRuntimeException("called GetBouncyness on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called GetBouncyness on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            return (double)target.bounciness;
        }
        public void SetBouncyness(double newbouncyness)
        {
            if (!target.HasBeenInitialized)
            {
                throw new ScriptRuntimeException("called SetBouncyness on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called SetBouncyness on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            target.bounciness = (Fix)newbouncyness;
        }
        public double GetGravityScale()
        {
            if (!target.HasBeenInitialized)
            {
                throw new ScriptRuntimeException("called GetGravityScale on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called GetGravityScale on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            return (double)target.gravityScale;
        }
        public void SetGravityScale(double newGravity)
        {
            if (!target.HasBeenInitialized)
            {
                throw new ScriptRuntimeException("called SetGravityScale on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called SetGravityScale on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            target.gravityScale = (Fix)newGravity;
        }*/
        public void AddForce(double ForceX, double ForceY)
        {
            if (!target.HasBeenInitialized)
            {
                throw new ScriptRuntimeException("called AddForce on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called AddForce on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            target.AddForce(new Vec2((Fix)ForceX, (Fix)ForceY));
        }
        public void Trigger()
        {
            if (!target.HasBeenInitialized)
            {
                throw new ScriptRuntimeException("called Trigger on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called Trigger on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            if (!CanTrigger())
            {
                throw new ScriptRuntimeException("called Trigger on a BoplBody that isn't triggerable. make sure its triggerable before calling by calling CanTrigger()");
            }
            if (type == "Grenade")
            {
                target.GetComponent<Grenade>().Detonate();
            }
            else if (type == "Mine")
            {
                target.GetComponent<Mine>().Detonate();
            }
            else if (type == "Missile")
            {
                Missile missile = target.GetComponent<Missile>();
                FixTransform.InstantiateFixed<Explosion>(missile.onHitExplosionPrefab, missile.body.position).GetComponent<IPhysicsCollider>().Scale = missile.fixTrans.Scale;
	            if (!string.IsNullOrEmpty(missile.soundEffectOnCol))
	            {
	            	AudioManager.Get().Play(missile.soundEffectOnCol);
	            }
	            Updater.DestroyFix(missile.gameObject);
            }
            else if (type == "Smoke Grenade")
            {
                target.GetComponent<SmokeGrenadeExplode2>().Detonate();
            }
            else if (type == "RocketEngine")
            {
                target.GetComponent<RocketEngine>().StartEngine();
            }
            else if (type == "AbilityPickup")
            {
                target.GetComponent<DynamicAbilityPickup>().SwapToRandomAbility();
            }
            else if (type == "Smoke")
            {
                target.GetComponent<FlammableSmoke>().Ignite();
            }

        }
        public void Destroy()
        {
            Updater.DestroyFix(target.gameObject);
        }
        public void SetColor(float R, float G, float B, float A)
        {
            var render = target.GetComponent<SpriteRenderer>();
            if (type == "Mine")
            {
                target.GetComponent<Mine>().Light.color = new Color(R, G, B, A);
            }
            if (render != null)
            {
                render.color = new Color(R, G, B, A);
            }
        }
        public bool IsDisappeared()
        {
            return !target.gameObject.activeInHierarchy;
        }
    }

    public class BlackHoleProxy
    {
        public BlackHole target;

        [MoonSharpHidden]
        public BlackHoleProxy(BlackHole p)
        {
            target = p;

        }
        public string GetClassType()
        {
            return "BlackHole";
        }
        public string GetObjectType()
        {
            return "BlackHole";
        }
        public bool HasBeenInitialized()
        {
            return target.dCircle.initHasBeenCalled;
        }
        public bool IsBeingDestroyed()
        {
            return target.dCircle.fixTrans.IsDestroyed || target.gameObject == null;
        }
        public DynValue GetPos()
        {
            if (!HasBeenInitialized())
            {
                throw new ScriptRuntimeException("called GetPos on a BlackHole before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called GetPos on a BlackHole when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            return LuaMain.Vec2ToTuple(target.GetComponent<FixTransform>().position);
        }


        public double GetMass() 
        {
            if (!HasBeenInitialized())
            {
                throw new ScriptRuntimeException("called GetMass on a BlackHole before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called GetMass on a BlackHole when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            return (double)(Fix.One/target.GetMass());
        }
        public void SetPos(double x, double y)
        {
            if (!HasBeenInitialized())
            {
                throw new ScriptRuntimeException("called SetPos on a BlackHole before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called SetPos on a BlackHole when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }

            target.GetComponent<FixTransform>().position = new Vec2((Fix)x, (Fix)y);
        }
        
        public void Grow(double amount)
        {
            if (!HasBeenInitialized())
            {
                throw new ScriptRuntimeException("called SetScale on a BlackHole before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called SetScale on a BlackHole when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            target.GrowIncrementally((Fix)amount);
        }
        
        public void SetVelocity(double VelX, double VelY)
        {
            if (!HasBeenInitialized())
            {
                throw new ScriptRuntimeException("called SetVelocity on a BlackHole before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called SetVelocity on a BlackHole when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            target.velocity = new Vec2((Fix)VelX, (Fix)VelY);
        }
        public void SetMass(double Mass) 
        {
            if (!HasBeenInitialized())
            {
                throw new ScriptRuntimeException("called SetMass on a BlackHole before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called SetMass on a BlackHole when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            target.SetMass((Fix)(1/Mass)); 
        }
        public void AddForce(double ForceX, double ForceY)
        {
            if (!HasBeenInitialized())
            {
                throw new ScriptRuntimeException("called AddForce on a BoplBody before it was initialized. make sure it has been initialized before calling by calling HasBeenInitialized()");
            }
            if (IsBeingDestroyed())
            {
                throw new ScriptRuntimeException("called AddForce on a BoplBody when it was being Destroyed. make sure its not being Destroyed before calling by calling IsBeingDestroyed()");
            }
            target.AddForce(new Vec2((Fix)ForceX, (Fix)ForceY));
        }
        public void Destroy()
        {
            Updater.DestroyFix(target.gameObject);
        }
        public bool IsDisappeared()
        {
            return !target.gameObject.activeInHierarchy;
        }
    }
}
