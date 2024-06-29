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
        public void Awake()
        {
            if (LuaSpawner == null)
            {
                //if there is none we create one for everyone.
                LuaSpawner = gameObject.AddComponent<LuaSpawner>();
            }


        }
        public static Script SetUpScriptFuncsons()
        {
            Script script = new Script();
            script.Globals["SpawnArrow"] = (object)SpawnArrowDouble;
            script.Globals["SpawnGrenade"] = (object)SpawnGrenadeDouble;
            script.Globals["SpawnAbilityPickup"] = (object)SpawnAbilityPickupDouble;
            script.Globals["SpawnSmokeGrenade"] = (object)SpawnSmokeGrenadeDouble;
            script.Globals["SpawnExplosion"] = (object)SpawnExplosionDouble;
            // Register just MyClass, explicitely.
            UserData.RegisterType<Player>();
            if (PlayerHandler.instance.GetPlayer(0) != null)
            {
                DynValue obj = UserData.Create(PlayerHandler.instance.GetPlayer(0));
                script.Globals.Set("obj", obj);
            }

            return script;
        }
        public void Register()
        {
            UUID = Plugin.NextUUID;
            Plugin.NextUUID++;
            SignalSystem.RegisterLogicGate(this);
            SignalSystem.RegisterGateThatAlwaysRuns(this);
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

        public override void Logic(Fix SimDeltaTime)
        {
            Dictionary<string, object> paramiters = new Dictionary<string, object>
            {
                { "mynumber", 5 }
            };
            RunScript(@"    
		-- defines a factorial function
        -- SpawnArrow(20, 30, 5, 10, 10, 1)
        -- SpawnGrenade(1, 30, 0, 5, 1, 10, 1)
        -- SpawnAbilityPickup(10, 40, 5, 0, 20)
        -- SpawnSmokeGrenade(-10, 30, 90, 1, -5, 0, 180)
        -- SpawnExplosion(-20, 30, 1)
	    if (obj != nil) then
		    obj.Scale = 2
		end
		return mynumber + 1 - 3 / 2", paramiters, SetUpScriptFuncsons());
        }
    }
}
