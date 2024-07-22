using AsmResolver.PE.DotNet.ReadyToRun;
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
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem.XR;
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
            script.Globals["SpawnGrenade"] = (object)SpawnGrenadeDouble;
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
            script.Globals["ShootBlink"] = (object)ShootBlink;
            script.Globals["ShootGrow"] = (object)ShootGrow;
            script.Globals["ShootShrink"] = (object)ShootShrink;
            script.Globals["GetDeltaTime"] = (object)GetDeltaTime;
            script.Globals["GetTimeSenceLevelLoad"] = (object)GetTimeSenceLevelLoad;
            script.Globals["IsTimeStopped"] = (object)IsTimeStopped;
            script.Globals["GetInputValueWithId"] = (object)GetInputValueWithId;
            script.Globals["SetOutputWithId"] = (object)SetOutputWithId;
            script.Globals["GetFileFromMapFile"] = (object)GetFileFromMapFile;
            script.Options.DebugPrint = s => { Console.WriteLine(s); };
            // Register just MyClass, explicitely.
            UserData.RegisterProxyType<LuaPlayerPhysicsProxy, PlayerPhysics>(r => new LuaPlayerPhysicsProxy(r));
            UserData.RegisterProxyType<PlatformProxy, StickyRoundedRectangle>(r => new PlatformProxy(r));
            UserData.RegisterProxyType<BoplBodyProxy, BoplBody>(r => new BoplBodyProxy(r));
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
                return DynValue.Nil;
            }
            catch (MoonSharp.Interpreter.SyntaxErrorException e)
            {
                Console.WriteLine($"ERROR PARSING LUA SCRIPT {Name} Error: {e.DecoratedMessage}");
                return DynValue.Nil;
            }
            catch (InternalErrorException e)
            {
                Console.WriteLine($"CONGRATS! YOU BROKE THE INTERPITER IN SCRIPT {Name} Error: {e.DecoratedMessage} pls send me the map so i can report the bug.");
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
        public static BoplBody SpawnArrowDouble(double posX, double posY, double scale, double StartVelX, double StartVelY, float R, float G, float B, float A)
        {
            return SpawnArrow((Fix)posX, (Fix)posY, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY, R, G, B, A);
        }
        public static BoplBody SpawnArrow(Fix posX, Fix posY, Fix scale, Fix StartVelX, Fix StartVelY, float R, float G, float B, float A)
        {
            return LuaSpawner.SpawnArrow(new Vec2(posX, posY), scale, new Vec2(StartVelX, StartVelY), new Color(R,G,B,A));
        }
        public static BoplBody SpawnGrenadeDouble(double posX, double posY, double scale, double StartVelX, double StartVelY, double StartAngularVelocity)
        {
            return SpawnGrenade((Fix)posX, (Fix)posY, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY, (Fix)StartAngularVelocity);
        }
        public static BoplBody SpawnGrenade(Fix posX, Fix posY, Fix scale, Fix StartVelX, Fix StartVelY, Fix StartAngularVelocity)
        {
            return LuaSpawner.SpawnGrenade(new Vec2(posX, posY), Fix.Zero, scale, new Vec2(StartVelX, StartVelY), StartAngularVelocity);
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
            //PlayerList (1)
            var playerlist = GameObject.Find("PlayerList");
            if (playerlist == null)
            {
                playerlist = GameObject.Find("PlayerList (1)");
            }
            var players = playerlist.transform;
            List<PlayerPhysics> Players = new();
            foreach (Transform player in players)
            {
                if (player.gameObject.name == "Player(Clone)")
                {
                    Players.Add(player.gameObject.GetComponent<PlayerPhysics>());
                }
            }
            //DynValue.FromObject
            return DynValue.NewTuple(
                DynValue.NewNumber(Players.Count),
                DynValue.FromObject(script, Players)
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
            StickyRoundedRectangle[] allObjects = Resources.FindObjectsOfTypeAll(typeof(StickyRoundedRectangle)) as StickyRoundedRectangle[];
            List<StickyRoundedRectangle> result = new List<StickyRoundedRectangle>(allObjects);
            return DynValue.NewTuple(
                DynValue.NewNumber(result.Count),
                DynValue.FromObject(script, result)
            );
        }
        public static double GetDeltaTime()
        {
            return (double)deltaTime;
        }
        public static double GetTimeSenceLevelLoad()
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
        public string GetClassType()
        {
            return "Player";
        }
        [MoonSharpHidden]
        public static Ability StringToAbility(string str)
        {
            switch (str)
            {
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
                case "Missle":
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
                default:
                    throw new ScriptRuntimeException($"{str} is not a valid ability");
            }
        }
        [MoonSharpHidden]
        public static string AbilityToString(Ability ability)
        {
            switch (ability)
            {
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
                    return "Missle";
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
                default:
                    return "Unknown/Modded/None";
            }
        }
        public enum Ability
        {
            None = 0,
            Roll = 1,
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
            Beam = 28
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
            shakable = p.gameObject.GetComponent<ShakablePlatform>();
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
            return (double)PlatformApi.PlatformApi.GetScale(target.gameObject);
        }
        public void SetScale(double scale)
        {
            PlatformApi.PlatformApi.SetScale(target.gameObject, (Fix)scale);
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
        public void ShakePlatform(double Duratson, double ShakeAmount)
        {
            shakable.AddShake((Fix)Duratson, (Fix)ShakeAmount);
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
            return target.gameObject.GetComponent<Boulder>() != null;
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
            if (target.GetComponent<SimpleSparkNode>() != null) { type = "Telsa"; }
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
            return target.physicsCollider.IsDestroyed;
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
        public void Destroy()
        {
            Updater.DestroyFix(target.gameObject);
        }
        public void SetColor(float R, float G, float B, float A)
        {
            var render = target.GetComponent<SpriteRenderer>();
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
}
