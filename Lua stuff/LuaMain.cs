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
    public class LuaMain : MonoBehaviour
    {
        public void Awake()
        {
            Dictionary<string, object> paramiters = new Dictionary<string, object>
            {
                { "mynumber", 5 }
            };
            RunScript(@"    
        debug.setmetatable(0, {
            __mul = function(a, b)
                return custom_multiply(a, b)
            end
        })
        
		-- defines a factorial function
		function fact (n)
			if (n == 0) then
				return 1
			else
				return n*fact(n - 2 + 1)
			end
		end

		return fact(mynumber)", paramiters);
        }
        public static DynValue RunScript(string scriptCode, Dictionary<string, object> paramiters)
        {

            Script script = new Script();
            foreach (var Key in paramiters.Keys)
            {
                script.Globals[Key] = paramiters[Key];
            }


            DynValue res = script.DoString(scriptCode);
            foreach (var Key in script.Globals.Keys)
            {
                var value = script.Globals[Key];
                UnityEngine.Debug.Log(value);
                if (value.ToString() == "MoonSharp.Interpreter.CallbackFunction")
                {
                    var func = (MoonSharp.Interpreter.CallbackFunction)value;
                    UnityEngine.Debug.Log(func.Name);
                }
            }
            UnityEngine.Debug.Log(res.Number);
            return res;
        }
    }
}
