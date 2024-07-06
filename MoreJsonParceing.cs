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
        public static void SpawnShootBlinks(List<object> ShootBlinks)
        {
            foreach (Dictionary<String, object> ShootBlink in ShootBlinks)
            {
                var InputUUID = Convert.ToInt32(ShootBlink["InputUUID"]);
                var pos = (Dictionary<String, object>)ShootBlink["Pos"];
                var Vec2Pos = new Vec2((Fix)Convert.ToDouble(pos["x"]), (Fix)Convert.ToDouble(pos["y"]));
                var rot = (Fix)Convert.ToDouble(ShootBlink["Rotation"]);
                var Varence = (Fix)Convert.ToDouble(ShootBlink["Varence"]);
                var PlayerDuration = (Fix)Convert.ToDouble(ShootBlink["PlayerDuration"]);
                var WallDelay = (Fix)Convert.ToDouble(ShootBlink["WallDelay"]);
                var WallShake = (Fix)Convert.ToDouble(ShootBlink["WallShake"]);
                var WallDuration = (Fix)Convert.ToDouble(ShootBlink["WallDuration"]);
                Plugin.CreateShootBlink(InputUUID, Vec2Pos, rot, Varence, WallDelay, PlayerDuration, WallDuration, WallShake);
            }
        }
        public static void SpawnShootGrows(List<object> ShootGrows)
        {
            foreach (Dictionary<String, object> ShootGrow in ShootGrows)
            {
                var InputUUID = Convert.ToInt32(ShootGrow["InputUUID"]);
                var pos = (Dictionary<String, object>)ShootGrow["Pos"];
                var Vec2Pos = new Vec2((Fix)Convert.ToDouble(pos["x"]), (Fix)Convert.ToDouble(pos["y"]));
                var rot = (Fix)Convert.ToDouble(ShootGrow["Rotation"]);
                var Varence = (Fix)Convert.ToDouble(ShootGrow["Varence"]);
                var blackHoleGrowth = (Fix)Convert.ToDouble(ShootGrow["blackHoleGrowth"]);
                var ScaleMultiplyer = (Fix)Convert.ToDouble(ShootGrow["ScaleMultiplyer"]);
                var PlayerMultiplyer = (Fix)Convert.ToDouble(ShootGrow["PlayerMultiplyer"]);
                Plugin.CreateShootGrow(InputUUID, Vec2Pos, rot, Varence, blackHoleGrowth, ScaleMultiplyer, PlayerMultiplyer);
            }
        }
        public static void SpawnShootStrinks(List<object> ShootStrinks)
        {
            foreach (Dictionary<String, object> ShootStrink in ShootStrinks)
            {
                var InputUUID = Convert.ToInt32(ShootStrink["InputUUID"]);
                var pos = (Dictionary<String, object>)ShootStrink["Pos"];
                var Vec2Pos = new Vec2((Fix)Convert.ToDouble(pos["x"]), (Fix)Convert.ToDouble(pos["y"]));
                var rot = (Fix)Convert.ToDouble(ShootStrink["Rotation"]);
                var Varence = (Fix)Convert.ToDouble(ShootStrink["Varence"]);
                var blackHoleGrowth = (Fix)Convert.ToDouble(ShootStrink["blackHoleGrowth"]);
                var ScaleMultiplyer = (Fix)Convert.ToDouble(ShootStrink["ScaleMultiplyer"]);
                var PlayerMultiplyer = (Fix)Convert.ToDouble(ShootStrink["PlayerMultiplyer"]);
                Plugin.CreateShootStrink(InputUUID, Vec2Pos, rot, Varence, blackHoleGrowth, ScaleMultiplyer, PlayerMultiplyer);
            }
        }
        public static void SpawnSpawners(List<object> Spawners)
        {
            foreach (Dictionary<String, object> Spawner2 in Spawners)
            {
                var InputUUID = Convert.ToInt32(Spawner2["InputUUID"]);
                var pos = (Dictionary<String, object>)Spawner2["Pos"];
                var Vec2Pos = new Vec2((Fix)Convert.ToDouble(pos["x"]), (Fix)Convert.ToDouble(pos["y"]));
                var SpawningVelocity = (Dictionary<String, object>)Spawner2["SpawningVelocity"];
                var Vec2SpawningVelocity = new Vec2((Fix)Convert.ToDouble(SpawningVelocity["x"]), (Fix)Convert.ToDouble(SpawningVelocity["y"]));
                var TimeBetweenSpawns = (Fix)Convert.ToDouble(Spawner2["TimeBetweenSpawns"]);
                var angularVelocity = (Fix)Convert.ToDouble(Spawner2["angularVelocity"]);
                var UseSignal = Convert.ToBoolean(Spawner2["UseSignal"]);
                var IsTriggerSignal = Convert.ToBoolean(Spawner2["IsTriggerSignal"]);
                var ColorList = ListOfObjectsToListOfFix((List<object>)Spawner2["Color"]);
                var Color = new UnityEngine.Color((float)ColorList[0], (float)ColorList[1], (float)ColorList[2], (float)ColorList[3]);
                var SpawnTypeString = Convert.ToString(Spawner2["SpawnType"]);
                var SpawnType = Spawner.ObjectSpawnType.None;
                switch (SpawnTypeString)
                {
                    case "Boulder":
                        SpawnType = Spawner.ObjectSpawnType.Boulder;
                        break;
                    case "Arrow":
                        SpawnType = Spawner.ObjectSpawnType.Arrow;
                        break;
                    case "Grenade":
                        SpawnType = Spawner.ObjectSpawnType.Grenade;
                        break;
                    case "AbilityOrb":
                        SpawnType = Spawner.ObjectSpawnType.AbilityOrb;
                        break;
                    case "SmokeGrenade":
                        SpawnType = Spawner.ObjectSpawnType.SmokeGrenade;
                        break;
                    case "Explosion":
                        SpawnType = Spawner.ObjectSpawnType.Explosion;
                        break;
                }
                var BoulderTypeString = Convert.ToString(Spawner2["BoulderType"]);
                var BoulderType = PlatformType.grass;
                switch (BoulderTypeString)
                {
                    case "snow":
                        BoulderType = PlatformType.snow;
                        break;
                    case "ice":
                        BoulderType = PlatformType.ice;
                        break;
                    case "space":
                        BoulderType = PlatformType.space;
                        break;
                    case "slime":
                        BoulderType = PlatformType.slime;
                        break;
                }
                Plugin.CreateSpawner(Vec2Pos, TimeBetweenSpawns, Vec2SpawningVelocity, angularVelocity, Color, SpawnType, BoulderType, UseSignal, InputUUID, IsTriggerSignal);
            }
        }
        public static string FileName = "";
        public static void SpawnLuaGates(List<object> gates, int index)
        {
            foreach (Dictionary<String, object> gate in gates)
            {
                var inputs = ListOfObjectsToListOfInt((List<object>)gate["InputUUIDs"]);
                var inputArray = inputs.ToArray();
                var outputs = ListOfObjectsToListOfInt((List<object>)gate["InputUUIDs"]);
                var outputArray = outputs.ToArray();
                var pos = (Dictionary<String, object>)gate["Pos"];
                var Vec2Pos = new Vec2((Fix)Convert.ToDouble(pos["x"]), (Fix)Convert.ToDouble(pos["y"]));
                var rot = (Fix)Convert.ToDouble(gate["Rotation"]);
                FileName = Convert.ToString(gate["LuaCodeFileName"]);
                var file = Plugin.GetFileFromZipArchiveBytes(Plugin.zipArchives[index], DoesFileHaveSameName)[0];
                var Code = System.Text.Encoding.Default.GetString(file);
                Plugin.CreateLuaGate(inputArray, outputArray, Vec2Pos, rot, Code);
            }
        }
        public static bool DoesFileHaveSameName(string filepath)
        {
            return filepath.EndsWith(FileName);
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
