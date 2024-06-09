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
        public static void SpawnBoulders(List<object> boulders)
        {
            foreach (Dictionary<String, object> boulder in boulders)
            {
                try
                {
                    //transform
                    Dictionary<string, object> transform = (Dictionary<string, object>)boulder["transform"];
                    Fix x = FloorToThousandnths(Convert.ToDouble(transform["x"]));
                    Fix y = FloorToThousandnths(Convert.ToDouble(transform["y"]));
                    //StartingVelocity
                    Dictionary<string, object> StartingVelocity = (Dictionary<string, object>)boulder["StartingVelocity"];
                    Fix VelX = FloorToThousandnths(Convert.ToDouble(StartingVelocity["x"]));
                    Fix VelY = FloorToThousandnths(Convert.ToDouble(StartingVelocity["y"]));
                    //color
                    List<float> floats = ListOfObjectsToListOfFloats((List<object>)boulder["color"]);
                    UnityEngine.Color Color = new UnityEngine.Color(floats[0], floats[1], floats[2], floats[3]);
                    //scale
                    Fix scale = FloorToThousandnths(Convert.ToDouble(boulder["scale"]));
                    //type
                    PlatformType platformType = PlatformType.grass;
                    if (boulder.ContainsKey("type"))
                    {
                        switch (Convert.ToString(boulder["type"])) 
                        {
                            case "grass":
                                platformType = PlatformType.grass; break;
                            case "snow":
                                platformType = PlatformType.snow; break;
                            case "ice":
                                platformType = PlatformType.ice; break;
                            case "space":
                                platformType = PlatformType.space; break;
                            case "slime":
                                platformType = PlatformType.slime; break;
                        }
                    }
                    Boulder NewBoulder = PlatformApi.PlatformApi.SpawnBoulder(new Vec2(x, y), scale, platformType, Color);
                    var dphysicsRoundedRect = NewBoulder.hitbox;
                    dphysicsRoundedRect.velocity = new Vec2(VelX, VelY) * dphysicsRoundedRect.inverseMass;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Failed to spawn boulder. Error: {ex.Message}");
                }
                
            }
        }
        public static Fix FloorToThousandnths(double value)
        {
            return Fix.Floor(((Fix)value) * (Fix)1000) / (Fix)1000;
        }
        public static List<float> ListOfObjectsToListOfFloats(List<object> list)
        {
            return MapMaker.Plugin.ListOfObjectsToListOfFloats(list);
        }
    }
}
