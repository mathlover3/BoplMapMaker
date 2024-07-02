using BoplFixedMath;
using MonoMod.Utils;
using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MapMaker.Lua_stuff
{
    public class LuaMain : LogicGate
    {
        public static LuaSpawner LuaSpawner;
        public string Name = "Lua default name";
        public string code = null;
        private static PlayerPhysics player;
        public Script script;
        public void Awake()
        {
            player = null;
            if (LuaSpawner == null)
            {
                //if there is none we create one for everyone.
                LuaSpawner = gameObject.AddComponent<LuaSpawner>();
            }
        }
        public static Script SetUpScriptFuncsons()
        {
            //dont want to let people use librays that would give them acsess outside of the game like os and io now do we? also no time package eather as thats just asking for desinks.
            Script script = new Script(CoreModules.Preset_HardSandbox | CoreModules.Metatables | CoreModules.ErrorHandling | CoreModules.Coroutine | CoreModules.Dynamic);
            script.Globals["SpawnArrow"] = (object)SpawnArrowDouble;
            script.Globals["SpawnGrenade"] = (object)SpawnGrenadeDouble;
            script.Globals["SpawnAbilityPickup"] = (object)SpawnAbilityPickupDouble;
            script.Globals["SpawnSmokeGrenade"] = (object)SpawnSmokeGrenadeDouble;
            script.Globals["SpawnExplosion"] = (object)SpawnExplosionDouble;
            script.Globals["RaycastRoundedRect"] = (object)RaycastRoundedRect;
            script.Globals["GetClosestPlayer"] = (object)GetClosestPlayer;
            script.Globals["GetAllPlatforms"] = (object)GetAllPlatforms;
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
                        UnityEngine.Debug.Log(res.String);
                        break;
                    case DataType.Boolean:
                        UnityEngine.Debug.Log(res.Boolean);
                        break;
                    case DataType.Number:
                        UnityEngine.Debug.Log(res.Number);
                        break;
                    default:
                        UnityEngine.Debug.Log(res.Type);
                        break;
                }
                return res;
            }
            catch (ScriptRuntimeException e)
            {
                Console.WriteLine($"ERROR IN LUA SCRIPT {Name} Error: {e.DecoratedMessage}");
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
        public static void SpawnArrowDouble(double posX, double posY, double scale, double StartVelX, double StartVelY, double StartAngularVelocity)
        {
            SpawnArrow((Fix)posX, (Fix)posY, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY, (Fix)StartAngularVelocity);
        }
        public static void SpawnArrow(Fix posX, Fix posY, Fix scale, Fix StartVelX, Fix StartVelY, Fix StartAngularVelocity)
        {
            LuaSpawner.SpawnArrow(new Vec2(posX, posY), scale, new Vec2(StartVelX, StartVelY), StartAngularVelocity);
        }
        public static void SpawnGrenadeDouble(double posX, double posY, double angle, double scale, double StartVelX, double StartVelY, double StartAngularVelocity)
        {
            SpawnGrenade((Fix)posX, (Fix)posY, (Fix)angle, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY, (Fix)StartAngularVelocity);
        }
        public static void SpawnGrenade(Fix posX, Fix posY, Fix angle, Fix scale, Fix StartVelX, Fix StartVelY, Fix StartAngularVelocity)
        {
            LuaSpawner.SpawnGrenade(new Vec2(posX, posY), angle, scale, new Vec2(StartVelX, StartVelY), StartAngularVelocity);
        }
        public static void SpawnAbilityPickupDouble(double posX, double posY, double scale, double StartVelX, double StartVelY)
        {
            SpawnAbilityPickup((Fix)posX, (Fix)posY, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY);
        }
        public static void SpawnAbilityPickup(Fix posX, Fix posY, Fix scale, Fix StartVelX, Fix StartVelY)
        {
            LuaSpawner.SpawnAbilityPickup(new Vec2(posX, posY), scale, new Vec2(StartVelX, StartVelY));
        }
        public static void SpawnSmokeGrenadeDouble(double posX, double posY, double angle, double scale, double StartVelX, double StartVelY, double StartAngularVelocity)
        {
            SpawnSmokeGrenade((Fix)posX, (Fix)posY, (Fix)angle, (Fix)scale, (Fix)StartVelX, (Fix)StartVelY, (Fix)StartAngularVelocity);
        }
        public static void SpawnSmokeGrenade(Fix posX, Fix posY, Fix angle, Fix scale, Fix StartVelX, Fix StartVelY, Fix StartAngularVelocity)
        {
            LuaSpawner.SpawnSmokeGrenade(new Vec2(posX, posY), angle, scale, new Vec2(StartVelX, StartVelY), StartAngularVelocity);
        }
        public static void SpawnExplosionDouble(double posX, double posY, double scale)
        {
            SpawnExplosion((Fix)posX, (Fix)posY, (Fix)scale);
        }
        public static void SpawnExplosion(Fix posX, Fix posY, Fix scale)
        {
            LuaSpawner.SpawnNormalExplosion(new Vec2(posX, posY), scale);
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
            var players = GameObject.Find("PlayerList").transform;
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
        public static List<StickyRoundedRectangle> GetAllPlatforms()
        {
            StickyRoundedRectangle[] allObjects = Resources.FindObjectsOfTypeAll(typeof(StickyRoundedRectangle)) as StickyRoundedRectangle[];
            List<StickyRoundedRectangle> result = new List<StickyRoundedRectangle>(allObjects);
            return result;
        }
        public override void Logic(Fix SimDeltaTime)
        {
            if (script == null)
            {
                script = SetUpScriptFuncsons();
            }
            Dictionary<string, object> paramiters = new Dictionary<string, object>
            {
                { "mynumber", 5 }
            };
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
        public void SetActive(bool active)
        {
            target.gameObject.SetActive(active);
        }
        public string GetClassType()
        {
            return "Player";
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
        public void SetActive(bool active)
        {
            target.gameObject.SetActive(active);
        }
        public bool IsActive()
        {
            return target.gameObject.activeInHierarchy;
        }
    }
    public class BoplBodyProxy
    {
        public BoplBody target;
        [MoonSharpHidden]
        public BoplBodyProxy(BoplBody p)
        {
            target = p;
        }
        public string GetClassType()
        {
            return "BoplBody";
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
            return (double)target.Scale;
        }
        public DynValue GetVelocity()
        {
            return LuaMain.Vec2ToTuple(target.velocity);
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
            target.Scale = (Fix)Scale;
        }
        public void SetVelocity(double VelX, double VelY)
        {
            target.velocity = new Vec2((Fix)VelX, (Fix)VelY);
        }
        public void AddForce(double ForceX, double ForceY)
        {
            target.AddForce(new Vec2((Fix)ForceX, (Fix)ForceY));
        }
    }
}
