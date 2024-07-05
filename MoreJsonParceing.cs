using BoplFixedMath;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker
{
    public class MoreJsonParceing
    {
        public static void SpawnAndGates(List<object> gates)
        {
            foreach (Dictionary<String, object> gate in gates)
            {
                var inputs = ListOfObjectsToListOfInt((List<object>)gate["InputUUIDs"]);
                var inputArray = inputs.ToArray();
                var outputUUID = Convert.ToInt32(gate["OutputUUID"]);
                var pos = (Dictionary<String, object>)gate["Pos"];
                var Vec2Pos = new Vec2((Fix)Convert.ToDouble(pos["x"]), (Fix)Convert.ToDouble(pos["y"]));
                var rot = (Fix)Convert.ToDouble(gate["Rotation"]);
                Plugin.CreateAndGate(inputArray, outputUUID, Vec2Pos, rot);
            }
        }
        public static void SpawnOrGates(List<object> gates)
        {
            foreach (Dictionary<String, object> gate in gates)
            {
                var inputs = ListOfObjectsToListOfInt((List<object>)gate["InputUUIDs"]);
                var inputArray = inputs.ToArray();
                var outputUUID = Convert.ToInt32(gate["OutputUUID"]);
                var pos = (Dictionary<String, object>)gate["Pos"];
                var Vec2Pos = new Vec2((Fix)Convert.ToDouble(pos["x"]), (Fix)Convert.ToDouble(pos["y"]));
                var rot = (Fix)Convert.ToDouble(gate["Rotation"]);
                Plugin.CreateOrGate(inputArray, outputUUID, Vec2Pos, rot);
            }
        }
        public static void SpawnNotGates(List<object> gates)
        {
            foreach (Dictionary<String, object> gate in gates)
            {
                var inputUUID = Convert.ToInt32(gate["InputUUID"]);
                var outputUUID = Convert.ToInt32(gate["OutputUUID"]);
                var pos = (Dictionary<String, object>)gate["Pos"];
                var Vec2Pos = new Vec2((Fix)Convert.ToDouble(pos["x"]), (Fix)Convert.ToDouble(pos["y"]));
                var rot = (Fix)Convert.ToDouble(gate["Rotation"]);
                Plugin.CreateNotGate(inputUUID, outputUUID, Vec2Pos, rot);
            }
        }
        public static void SpawnDelayGates(List<object> gates)
        {
            foreach (Dictionary<String, object> gate in gates)
            {
                var inputUUID = Convert.ToInt32(gate["InputUUID"]);
                var outputUUID = Convert.ToInt32(gate["OutputUUID"]);
                var pos = (Dictionary<String, object>)gate["Pos"];
                var delay = (Fix)Convert.ToDouble(gate["Delay"]);
                var Vec2Pos = new Vec2((Fix)Convert.ToDouble(pos["x"]), (Fix)Convert.ToDouble(pos["y"]));
                var rot = (Fix)Convert.ToDouble(gate["Rotation"]);
                Plugin.CreateSignalDelay(inputUUID, outputUUID, delay, Vec2Pos, rot);
            }
        }
        public static void SpawnTriggers(List<object> triggers)
        {
            foreach (Dictionary<String, object> trigger in triggers)
            {
                var outputUUID = Convert.ToInt32(trigger["OutputUUID"]);
                var pos = (Dictionary<String, object>)trigger["Pos"];
                var Vec2Pos = new Vec2((Fix)Convert.ToDouble(pos["x"]), (Fix)Convert.ToDouble(pos["y"]));
                var rot = (Fix)Convert.ToDouble(trigger["Rotation"]);
                var Extents = (Dictionary<String, object>)trigger["Extents"];
                var Vec2Extents = new Vec2((Fix)Convert.ToDouble(Extents["x"]), (Fix)Convert.ToDouble(Extents["y"]));
                var Visable = Convert.ToBoolean(trigger["Visable"]);
                var DettectAbilityOrbs = Convert.ToBoolean(trigger["DettectAbilityOrbs"]);
                var DettectArrows = Convert.ToBoolean(trigger["DettectArrows"]);
                var DettectBlackHole = Convert.ToBoolean(trigger["DettectBlackHole"]);
                var DettectBoulders = Convert.ToBoolean(trigger["DettectBoulders"]);
                var DettectEngine = Convert.ToBoolean(trigger["DettectEngine"]);
                var DettectGrenades = Convert.ToBoolean(trigger["DettectGrenades"]);
                var DettectMine = Convert.ToBoolean(trigger["DettectMine"]);
                var DettectMissle = Convert.ToBoolean(trigger["DettectMissle"]);
                var DettectPlatforms = Convert.ToBoolean(trigger["DettectPlatforms"]);
                var DettectPlayers = Convert.ToBoolean(trigger["DettectPlayers"]);
                var DettectSmoke = Convert.ToBoolean(trigger["DettectSmoke"]);
                var DettectSmokeGrenade = Convert.ToBoolean(trigger["DettectSmokeGrenade"]);
                var DettectSpike = Convert.ToBoolean(trigger["DettectSpike"]);
                var DettectTesla = Convert.ToBoolean(trigger["DettectTesla"]);
                Plugin.CreateTrigger(Vec2Pos, Vec2Extents, outputUUID, Visable, DettectAbilityOrbs, DettectArrows, DettectBlackHole, DettectBoulders, DettectEngine, DettectGrenades, DettectMine, DettectMissle, DettectPlatforms, DettectPlayers, DettectSmoke, DettectSmokeGrenade, DettectSpike, DettectTesla);
            }
        }
        public static Fix FloorToThousandnths(double value)
        {
            return Fix.Floor(((Fix)value) * (Fix)1000) / (Fix)1000;
        }
        public static List<Fix> ListOfObjectsToListOfFix(List<object> ObjectList)
        {
            List<Fix> Floats = new List<Fix>();
            for (int i = 0; i < ObjectList.Count; i++)
            {
                Floats.Add((Fix)Convert.ToDouble(ObjectList[i]));
            }
            return Floats;
        }
        public static List<int> ListOfObjectsToListOfInt(List<object> ObjectList)
        {
            List<int> Floats = new List<int>();
            for (int i = 0; i < ObjectList.Count; i++)
            {
                Floats.Add((int)Convert.ToDouble(ObjectList[i]));
            }
            return Floats;
        }
    }
}
