using BoplFixedMath;
using MoonSharp.Interpreter;
using System;
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
        public bool OnlyActivateOnInputChange = false;
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
            script.Globals["print"] = (Action<string>)print;
            // Register just MyClass, explicitely.
            UserData.RegisterProxyType<LuaPlayerPhysicsProxy, PlayerPhysics>(r => new LuaPlayerPhysicsProxy(r));
            UserData.RegisterProxyType<PlatformProxy, StickyRoundedRectangle>(r => new PlatformProxy(r));
            UserData.RegisterProxyType<BoulderProxy, StickyRoundedRectangle>(r => new BoulderProxy(r));
            return script;
        }
        public void Register()
        {
            UUID = Plugin.NextUUID;
            Plugin.NextUUID++;
            SignalSystem.RegisterLogicGate(this);
            if (!OnlyActivateOnInputChange)
            {
                SignalSystem.RegisterGateThatAlwaysRuns(this);
            }
        }
        public static DynValue RunScript(string scriptCode, Dictionary<string, object> paramiters, Script script)
        {
            foreach (var Key in paramiters.Keys)
            {
                script.Globals[Key] = paramiters[Key];
            }


            DynValue res = script.DoString(scriptCode);
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
            UnityEngine.Debug.Log(res.Number);
            return res;
        }
        public static void print(string text)
        {
            UnityEngine.Debug.Log(text);
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
            var dir = new Vec2((Fix)angle);
            var dist = (Fix)maxDist;
            var result = DetPhysics.Get().RaycastToClosestRoundedRect(pos, dir, dist);
            DynValue platform;

            if (result.pp.fixTrans != null && result.pp.fixTrans.gameObject != null)
            {
                var roundedRect = result.pp.fixTrans.gameObject.GetComponent<StickyRoundedRectangle>();
                var shakable = result.pp.fixTrans.gameObject.GetComponent<ShakablePlatform>();
                //if its a platform not a boulder
                if (shakable != null)
                {
                    platform = UserData.Create(new PlatformProxy(roundedRect));
                }
                else
                {
                    platform = UserData.Create(new BoulderProxy(roundedRect));
                }
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
        public static Table Vec2ToTable(Vec2 vec2, Script script)
        {
            Table tbl = new Table(script);
            tbl["x"] = (double)vec2.x;
            tbl["y"] = (double)vec2.y;
            return tbl;
        }
        public static Vec2 TableToVec2(Table table)
        {
            Vec2 vec = new Vec2();
            if (table.Get("x") != DynValue.Nil && table.Get("y") != DynValue.Nil) 
            {
                if (table.Get("x").Type == DataType.Number && table.Get("y").Type == DataType.Number)
                {
                    vec.x = (Fix)table.Get("x").Number;
                    vec.y = (Fix)table.Get("y").Number;
                    return vec;
                }
                else { throw new Exception("TABLE TO TURN INTO VEC2 HAS A VALUE OF x/y THATS NOT A NUMBER!"); }
            }
            else { throw new Exception("TABLE TO TURN INTO VEC2 HAS A VALUE OF x/y THATS NIL!"); }
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
        public Table GetVelocity(Script script) { return LuaMain.Vec2ToTable(body.Velocity, script); }
        public Table GetPosition(Script script) { return LuaMain.Vec2ToTable(body.position, script); }
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
        public Table GetPos(Script script)
        {
            return LuaMain.Vec2ToTable(PlatformApi.PlatformApi.GetPos(target.gameObject), script);
        }
        public double GetRot(Script script)
        {
            return (double)PlatformApi.PlatformApi.GetRot(target.gameObject);
        }
        public Table GetHome(Script script)
        {
            return LuaMain.Vec2ToTable(PlatformApi.PlatformApi.GetHome(target.gameObject), script);
        }
        public double GetHomeRot(Script script)
        {
            return (double)PlatformApi.PlatformApi.GetHomeRot(target.gameObject);
        }
    }
    public class BoulderProxy
    {
        public StickyRoundedRectangle target;
        [MoonSharpHidden]
        public BoulderProxy(StickyRoundedRectangle p)
        {
            target = p;
        }
        public string GetClassType()
        {
            return "Boulder";
        }
    }

}
