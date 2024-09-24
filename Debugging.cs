using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MapMaker
{
    public class Debugging
    {
        public static void Awake()
        {
            Type[] TypesToPatch = { typeof(Beam), typeof(BeamObject), typeof(BoplBody), typeof(DestroyIfOutsideSceneBounds), typeof(DetPhysics), typeof(DPhysicsBox), typeof(DPhysicsCircle), typeof(DPhysicsRoundedRect), typeof(DuplicatePlatformEffect), typeof(GameSessionHandler), typeof(InstantAbility), typeof(MeteorSmash), typeof(PlaceRocketEngine), typeof(PlaceSparkNode), typeof(PlatformTransform), typeof(PlayerBody), typeof(QuantumTunnel), typeof(ReviveParticle), typeof(Rope), typeof(RopeAttachment), typeof(Shake), typeof(SimpleSparkNode), typeof(TeleportedObjectEffect), typeof(TeleportIndicator), typeof(Updater), typeof(vibrate) };

            foreach (Type type in TypesToPatch)
            {
                Plugin.logger.LogInfo("Patching " + type.FullName);
                MethodInfo[] methods = type.GetMethods();

                foreach (MethodInfo method in methods)
                {
                    if (method.DeclaringType != type)
                    {
                        continue; // skip inherited methods
                    }

                    if (!method.HasMethodBody())
                    {
                        continue;
                    }

                    try
                    {
                        Plugin.harmony.Patch(method, transpiler: new HarmonyMethod(typeof(DebbugingTranspiler), nameof(DebbugingTranspiler.FieldTranspiler)));
                    }
                    catch (Exception e)
                    {
                        Plugin.logger.LogWarning("Failed to patch " + type.FullName + "::" + method.Name + ": " + e);
                    }
                }
            }

        }
    }
    public static class DebbugingTranspiler
    {
        static FieldInfo f_Pos_Field = AccessTools.Field(typeof(FixTransform), nameof(FixTransform.position));
        static MethodInfo m_LoggingFunc = AccessTools.Method(typeof(DebbugingTranspiler), nameof(DebbugingTranspiler.Logging));
        public static IEnumerable<CodeInstruction> FieldTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            foreach (var instruction in instructions)
            {
                if (instruction.StoresField(f_Pos_Field))
                {
                    //if it stores something to the fixtrasforms posison then insert our code
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Dup);
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Ldarg_0);
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Call, m_LoggingFunc);
                }
                yield return instruction;
            }

        }
        public static void Logging(Vec2 newPos, UnityEngine.Component component)
        {
            var gameobj = component.gameObject;
            if (gameobj.name == "Player(Clone)")
            {
                UnityEngine.Debug.Log($"{UnityEngine.StackTraceUtility.ExtractStackTrace()} is changing player pos, old value: {newPos}, at time: {Updater.SimTimeSinceLevelLoaded}");
            }
        }
    }
}
