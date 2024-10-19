using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using BoplFixedMath;
using System.IO.Compression;
using System.IO;

namespace MapMaker
{
    public class Debugging
    {
        public static void Awake()
        {
            Type[] allTypesInGameAssembly = typeof(Beam).Assembly.GetTypes();
            List<Type> allTypes = new List<Type>();
            allTypes.AddRange(allTypesInGameAssembly);
            allTypes.Remove(typeof(FixTransform));
            allTypesInGameAssembly = allTypes.ToArray();
            allTypesInGameAssembly = Array.FindAll(allTypesInGameAssembly, IsAllowed);
            allTypes = allTypesInGameAssembly.ToList();
            allTypes.Remove(typeof(Circle));
            allTypes.Remove(typeof(Box));
            allTypes.Remove(typeof(RoundedRect));
            Type[] TypesToPatch = { typeof(Beam), typeof(BeamObject), typeof(BlackHoleClap), typeof(BoplBody), typeof(BowTransform), typeof(CastSpell), typeof(ControlPlatform), typeof(Dash), typeof(DestroyIfOutsideSceneBounds), typeof(DetPhysics), typeof(DPhysicsBox), typeof(DPhysicsCircle), typeof(DPhysicsRoundedRect), typeof(Drill), typeof(DuplicatePlatformEffect), typeof(GameSessionHandler), typeof(Gravity), typeof(GunTransform), typeof(HookshotInstant), typeof(InstantAbility), typeof(MeteorSmash), typeof(PlaceRocketEngine), typeof(PlaceSparkNode), typeof(PlatformTransform), typeof(PlayerBody), typeof(QuantumTunnel), typeof(ReviveParticle), typeof(Rope), typeof(RopeAttachment), typeof(Shake), typeof(SimpleSparkNode), typeof(TeleportedObjectEffect), typeof(TeleportIndicator), typeof(Updater), typeof(vibrate) };

            foreach (Type type in allTypes)
            {
                //skip more internal stuff or if its a class in a class
                if (type.FullName.Contains("<PrivateImplementationDetails>") || type.FullName.Contains("+"))
                {
                    continue;
                }
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
                    //if its a async meathed or a iterator skip it
                    if (method.IsDefined(typeof(AsyncStateMachineAttribute), false) || method.IsDefined(typeof(IteratorStateMachineAttribute), false))
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
        public static bool IsAllowed(Type type)
        {
            //skip more internal stuff or if its a class in a class
            return !type.IsGenericType && !type.IsDefined(typeof(CompilerGeneratedAttribute), false) && !type.FullName.Contains("<PrivateImplementationDetails>") && !type.FullName.Contains("+");
        }
        public static void LogDif(List<Type> list, List<Type> list2)
        {
            foreach (Type type in list)
            {
                if (!list2.Contains(type))
                {
                    UnityEngine.Debug.Log($"type is difrent: {type}");
                }
            }
        }

    }

    public static class DebbugingTranspiler
    {
        static FieldInfo f_Pos_Field = AccessTools.Field(typeof(FixTransform), nameof(FixTransform.position));
        static FieldInfo f_ExternalVel_Field = AccessTools.Field(typeof(PlayerBody), nameof(PlayerBody.externalVelocity));
        static FieldInfo f_selfImposedVel_Field = AccessTools.Field(typeof(PlayerBody), nameof(PlayerBody.selfImposedVelocity));
        static MethodInfo m_LoggingPosFunc = AccessTools.Method(typeof(DebbugingTranspiler), nameof(DebbugingTranspiler.LoggingPos));
        static MethodInfo m_LoggingExternalVelFunc = AccessTools.Method(typeof(DebbugingTranspiler), nameof(DebbugingTranspiler.LoggingExternalVel));
        static MethodInfo m_LoggingselfImposedVelocityFunc = AccessTools.Method(typeof(DebbugingTranspiler), nameof(DebbugingTranspiler.LoggingselfImposedVelocity));
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
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Call, m_LoggingPosFunc);
                }
                /*if (instruction.StoresField(f_ExternalVel_Field))
                {
                    //if it stores something to the fixtrasforms posison then insert our code
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Dup);
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Ldarg_0);
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Call, m_LoggingExternalVelFunc);
                }
                if (instruction.StoresField(f_selfImposedVel_Field))
                {
                    //if it stores something to the fixtrasforms posison then insert our code
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Dup);
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Ldarg_0);
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Call, m_LoggingselfImposedVelocityFunc);
                }*/
                yield return instruction;
            }

        }
        public static void LoggingPos(Vec2 newPos, UnityEngine.Component component)
        {
            var gameobj = component.gameObject;
            if (gameobj.name == "Player(Clone)")
            {
                UnityEngine.Debug.Log($"{UnityEngine.StackTraceUtility.ExtractStackTrace()} is changing player pos, new value: {newPos}, at time: {Updater.SimTimeSinceLevelLoaded}");
            }
        }
        public static void LoggingExternalVel(Vec2 newExternalVel, UnityEngine.Component component)
        {
            var gameobj = component.gameObject;
            if (gameobj.name == "Player(Clone)")
            {
                UnityEngine.Debug.Log($"{UnityEngine.StackTraceUtility.ExtractStackTrace()} is changing player externalVelocity, new value: {newExternalVel}, at time: {Updater.SimTimeSinceLevelLoaded}");
            }
        }
        public static void LoggingselfImposedVelocity(Vec2 newselfImposedVelocity, UnityEngine.Component component)
        {
            var gameobj = component.gameObject;
            if (gameobj.name == "Player(Clone)")
            {
                UnityEngine.Debug.Log($"{UnityEngine.StackTraceUtility.ExtractStackTrace()} is changing player selfImposedVelocity, new value: {newselfImposedVelocity}, at time: {Updater.SimTimeSinceLevelLoaded}");
            }
        }
    }
    /*[HarmonyPatch(typeof(SlimeController))]
    public class SlimeControllerPatches
    {
        [HarmonyPatch("UpdateSim")]
        [HarmonyPrefix]
        private static void UpdateSim(SlimeController __instance, Fix simDeltaTime)
        {
            Player player = PlayerHandler.Get().GetPlayer(__instance.playerNumber);
            UnityEngine.Debug.Log($"jump: {player.jumpButton_PressedThisFrame()}, abilitys 1, 2 and 3: {player.AbilityButtonIsDown(0)},{player.AbilityButtonIsDown(1)}, {player.AbilityButtonIsDown(2)}, input vector: {__instance.inputVector}, at time: {Updater.SimTimeSinceLevelLoaded}");
        }
    }*/
}
