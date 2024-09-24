using BoplFixedMath;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Steamworks;
using Steamworks.Data;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;
using System.IO.Compression;
using MapMaker.Lua_stuff;
using MoonSharp.Interpreter;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Linq;
using System.IO;

namespace MapMaker
{
    public class Patches
    {
        [HarmonyPatch(typeof(MachoThrow2))]
        public class MachoThrow2Patches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPrefix]
            private static void Awake_MapMaker_Plug(MachoThrow2 __instance)
            {
                Debug.Log("MatchoThrow2");
                //if there is something to add
                if (Plugin.CustomMatchoManSprites != null && Plugin.CustomMatchoManSprites.Count != 0)
                {
                    __instance.boulders.sprites.AddRange(Plugin.CustomMatchoManSprites);
                }
                var ColorList = new List<UnityEngine.Color>(__instance.boulderSmokeColors);
                ColorList.AddRange(Plugin.CustomBoulderSmokeColors);
                __instance.boulderSmokeColors = ColorList.ToArray();
            }
        }
        [HarmonyPatch(typeof(Drill))]
        public class DrillPatches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPrefix]
            private static void Awake_MapMaker_Plug(Drill __instance)
            {
                Debug.Log("Drill");
                //if there is something to add
                if (Plugin.CustomDrillColors != null && Plugin.CustomDrillColors.Count != 0)
                {
                    __instance.platformDependentColors.AddRange(Plugin.CustomDrillColors);
                }

            }
        }
        //these 2 things happen before the logic gate stuff has a chance to run for the frame so it will be 1 frame behind.
        [HarmonyPatch(typeof(AntiLockPlatform))]
        public class AntiLockPlatformPatches
        {
            [HarmonyPatch("UpdateSim")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(AntiLockPlatform __instance)
            {
                if (__instance.GetComponent<MovingPlatformSignalStuff>() != null)
                {
                    var SignalStuff = __instance.GetComponent<MovingPlatformSignalStuff>();
                    //if its on and its not inverted
                    if (!SignalStuff.SignalIsInverted && SignalStuff.IsOn())
                    {
                        //contenue the path
                        return true;
                    }
                    //if the signal is off and it is inverted
                    if (SignalStuff.SignalIsInverted && SignalStuff.IsOn())
                    {
                        //contenue the path
                        return true;
                    }
                    //signal is off and it isnt inverted or signal is on and it is inverted
                    return false;
                }
                //no MovingPlatformSignalStuff comp found
                return true;
            }
        }
        [HarmonyPatch(typeof(VectorFieldPlatform))]
        public class VectorFieldPlatformPatches
        {
            [HarmonyPatch("UpdateSim")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(VectorFieldPlatform __instance)
            {
                if (__instance.GetComponent<MovingPlatformSignalStuff>() != null)
                {
                    var SignalStuff = __instance.GetComponent<MovingPlatformSignalStuff>();
                    //if its on and its not inverted
                    if (!SignalStuff.SignalIsInverted && SignalStuff.IsOn())
                    {
                        //contenue the path
                        return true;
                    }
                    //if the signal is off and it is inverted
                    if (SignalStuff.SignalIsInverted && SignalStuff.IsOn())
                    {
                        //contenue the path
                        return true;
                    }
                    //signal is off and it isnt inverted or signal is on and it is inverted
                    return false;
                }
                //no MovingPlatformSignalStuff comp found
                return true;
            }
        }
        [HarmonyPatch(typeof(QuantumTunnel))]
        public class QuantumTunnelPatches
        {
            [HarmonyPatch("UpdateSim")]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug(QuantumTunnel __instance)
            {
                //for all of the DisappearPlatformsOnSignals check if the platform is the same as the Victim
                foreach (var Disappear in DisappearPlatformsOnSignal.DisappearPlatformsOnSignals)
                {
                    var VictimId = __instance.Victim.GetInstanceID();
                    var DissappearId = Disappear.platform.GetInstanceID();
                    if (VictimId == DissappearId)
                    {
                        //if we are effecting the same platform and the Disappear platform is going to make the platform disapear when we are done
                        //then dont have the reapearing animatson
                        //delay it by the time it takes to do the animatsons so we dont stop mid animatson
                        Fix time = (Fix)__instance.ScaleAnim.keys[__instance.ScaleAnim.keys.Length - 1].time;
                        Fix time2 = (Fix)__instance.OpacityAnim.keys[__instance.OpacityAnim.keys.Length - 1].time;
                        var ExstraDelay = Fix.Max(time, time2);
                        //the __instance.IsInitialized is so that it works fine if delay is 0/less then ExstraDelay
                        if (Disappear.TimeDelayed > Disappear.delay - ExstraDelay && (__instance.IsInitialized || __instance.age > __instance.LifeSpan))
                        {
                            var spriteRen = __instance.GetComponent<SpriteRenderer>();
                            __instance.transform.localScale = new Vector3(__instance.originalScale.x, __instance.originalScale.y, __instance.originalScale.z);
                            __instance.spriteRen.color = new UnityEngine.Color(spriteRen.color.r, spriteRen.color.g, spriteRen.color.b, 0);
                            __instance.Victim.SetActive(false);
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(SpikeAttack))]
        public class SpikeAttackPatches
        {
            [HarmonyPatch("OnCollide")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(CollisionInformation collision, SpikeAttack __instance)
            {
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
                if (__instance.timePassed < __instance.pushTime && !GameTime.IsTimeStopped() && collision.colliderPP.monobehaviourCollider != null && !collision.colliderPP.monobehaviourCollider.IsDestroyed && collision.colliderPP.monobehaviourCollider.initHasBeenCalled && collision.colliderPP.instanceId != __instance.attachedGround.gameObject.GetInstanceID())
                {
                    if (collision.layer == LayerMask.NameToLayer("wall"))
                    {
                        Vec2 v = Vec2.NormalizedSafe(collision.contactPoint - __instance.fixTrans.position);
                        collision.colliderPP.monobehaviourCollider.AddForceAtPosition(v * __instance.knockAwayWallStr, collision.contactPoint, ForceMode2D.Force);
                        if (!__instance.pushedThisFrame)
                        {
                            __instance.hitbox.AddForceAtPosition(-v * __instance.knockAwayWallStr * __instance.selfPushMultiplier, collision.contactPoint, ForceMode2D.Force);
                            __instance.pushedThisFrame = true;
                            return false;
                        }
                    }
                    //dont react if its a trigger
                    else if (collision.layer != (LayerMask)3)
                    {
                        Vec2 v2 = Vec2.NormalizedSafe(collision.contactPoint - __instance.fixTrans.position);

                        collision.colliderPP.monobehaviourCollider.AddForceAtPosition(v2 * __instance.knockAwayStr, collision.contactPoint, ForceMode2D.Force);

                        if (!__instance.pushedThisFrame)
                        {
                            __instance.hitbox.AddForceAtPosition(-v2 * __instance.knockAwayStr * __instance.selfPushMultiplier, collision.contactPoint, ForceMode2D.Force);
                            __instance.pushedThisFrame = true;
                        }
                    }
                }
                return false;
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
            }
        }

        [HarmonyPatch(typeof(SmokeGrenadeExplode2))]
        public class SmokeGrenadeExplode2Patches
        {
            [HarmonyPatch("OnCollide")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(CollisionInformation collision, SmokeGrenadeExplode2 __instance)
            {
                if (!__instance.grenade.DetonatesOnOwner || __instance.IsDestroyed)
                {
                    return false;
                }
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
                if (collision.layer == LayerMask.NameToLayer("Projectile") && collision.colliderPP.fixTrans != null)
                {
                    Projectile component = collision.colliderPP.fixTrans.GetComponent<Projectile>();
                    if (component != null && !component.IgnitesExplosives)
                    {
                        return false;
                    }
                }
                if (collision.layer == (LayerMask)3)
                {
                    return false;
                }
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
                __instance.Detonate();
                return false;
            }
        }
        [HarmonyPatch(typeof(ShakablePlatform))]
        public class ShakablePlatformPatches
        {
            [HarmonyPatch("AddShake")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(ShakablePlatform __instance, Fix duration, Fix shakeAmount, int shakePriority = 1, Material newMaterialDuringShake = null, AnimationCurveFixed shakeCurve = null)
            {
                //for all of the DisappearPlatformsOnSignals check if the platform is the same as the platform we are attached to.
                foreach (var Quantum in ShootQuantum.spawnedQuantumTunnels)
                {
                    if (Quantum != null && Quantum.Victim != null)
                    {
                        var VictimId = Quantum.Victim.GetInstanceID();
                        var DissappearId = __instance.gameObject.GetInstanceID();
                        //if this is already being blinked and its not being called from a blink dont shake it as if its shorter then it will go back to normal too soon.
                        if (VictimId == DissappearId && !Plugin.CurrentlyBlinking)
                        {
                            return false;
                        }
                    }
                }
                if (shakePriority >= __instance.currentShakePriority)
                {
                    return true;
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(ShootScaleChange))]
        public class ShootScaleChangePatches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPrefix]
            //so that it doesnt error out when copying the component for the ShootRay.
            private static bool Awake_MapMaker_Plug(ShootScaleChange __instance)
            {
                if (__instance.RaycastParticlePrefab != null)
                {
                    return true;
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(ShootQuantum))]
        public class ShootQuantumPatches
        {
            [HarmonyPatch("Shoot")]
            [HarmonyPrefix]
            private static void Shoot(ShootQuantum __instance, Vec2 firepointFIX, Vec2 directionFIX, ref bool hasFired, int playerId, bool alreadyHitWater = false)
            {
                Plugin.CurrentlyBlinking = true;
            }
            [HarmonyPatch("Shoot")]
            [HarmonyPostfix]
            private static void Shoot2(ShootQuantum __instance, Vec2 firepointFIX, Vec2 directionFIX, ref bool hasFired, int playerId, bool alreadyHitWater = false)
            {
                Plugin.CurrentlyBlinking = false;
            }
        }
        [HarmonyPatch(typeof(MoonSharp.Interpreter.CoreLib.MathModule))]
        public class MoonSharpPatches
        {

            [HarmonyPatch("abs")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void abs(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec1(args, "abs", d => (double)Fix.Abs((Fix)d), __instance);
            }
            [HarmonyPatch("acos")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void acos(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec1(args, "acos", d => (double)Fix.Acos((Fix)d), __instance);
            }
            [HarmonyPatch("asin")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void asin(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                Debug.Log("asin");
                //Fix doesnt have a asin so thanks to chatgpt i use Acon and sqrt and exsponents to get Asin
                __result = Plugin.exec1(args, "asin", d => (double)Fix.Acos(Fix.Sqrt(Fix.One - Fix.Pow2((Fix)d))), __instance);
            }
            [HarmonyPatch("atan")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void atan(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec1(args, "atan", d => (double)Fix.Atan((Fix)d), __instance);
            }
            [HarmonyPatch("atan2")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void atan2(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec2(args, "atan2", (d1, d2) => (double)Fix.Atan2((Fix)d1, (Fix)d2), __instance);
            }
            [HarmonyPatch("ceil")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void ceil(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec1(args, "ceil", d => (double)Fix.Ceiling((Fix)d), __instance);
            }
            [HarmonyPatch("cos")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void cos(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec1(args, "cos", d => (double)Fix.Cos((Fix)d), __instance);
            }
            [HarmonyPatch("cosh")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void cosh(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                Debug.Log("cosh");
                //i sure hope this works...
                __result = Plugin.exec1(args, "cosh", d => (double)((Fix.Pow((Fix)2.718281828459045, (Fix)(d)) + Fix.Pow((Fix)2.718281828459045, (Fix)(-d))) / (Fix)2), __instance);
            }
            [HarmonyPatch("deg")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void deg(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec1(args, "deg", d => (double)((Fix)d * (Fix)PhysTools.RadiansToDegrees), __instance);
            }
            [HarmonyPatch("exp")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void exp(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                Debug.Log("exp");
                //i sure hope this works...
                __result = Plugin.exec1(args, "exp", d => (double)Fix.Pow((Fix)2.718281828459045, (Fix)d), __instance);
            }
            [HarmonyPatch("floor")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void floor(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec1(args, "floor", d => (double)Fix.Floor((Fix)d), __instance);
            }
            [HarmonyPatch("fmod")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void fmod(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec2(args, "fmod", (d1, d2) => (double)Fix.SlowMod((Fix)d1, (Fix)d2), __instance);
            }
            [HarmonyPatch("ldexp")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void ldexp(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                Debug.Log("ldexp");
                __result = Plugin.exec2(args, "ldexp", (d1, d2) => (double)((Fix)d1 * Fix.Pow((Fix)2, (Fix)d2)), __instance);
            }
            [HarmonyPatch("log")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void log(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                Debug.Log("log");
                //i THINK Log2(n) is the same as ln(n)???
                __result = Plugin.exec2n(args, "log", Math.E, (d1, d2) => (double)(Fix.Log2((Fix)d1) / Fix.Log2((Fix)d2)), __instance);
            }
            [HarmonyPatch("max")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void max(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.execaccum(args, "max", (d1, d2) => (double)Fix.Max((Fix)d1, (Fix)d2), __instance);
            }
            [HarmonyPatch("min")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void min(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.execaccum(args, "min", (d1, d2) => (double)Fix.Min((Fix)d1, (Fix)d2), __instance);
            }
            [HarmonyPatch("modf")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void modf(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                Debug.Log("modf");
                DynValue arg = args.AsType(0, "modf", DataType.Number, false);
                __result = DynValue.NewTuple(DynValue.NewNumber((double)Fix.Floor((Fix)arg.Number)), DynValue.NewNumber((double)((Fix)arg.Number - Fix.Floor((Fix)arg.Number))));
            }
            [HarmonyPatch("pow")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void pow(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec2(args, "pow", (d1, d2) => (double)Fix.Pow((Fix)d1, (Fix)d2), __instance);
            }
            [HarmonyPatch("rad")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void rad(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                Debug.Log("rad");
                __result = Plugin.exec1(args, "rad", d => (double)((Fix)d * (Fix)PhysTools.RadiansToDegrees), __instance);
            }
            [HarmonyPatch("random")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void random(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                double d;
                DynValue m = args.AsType(0, "random", DataType.Number, true);
                DynValue n = args.AsType(1, "random", DataType.Number, true);
                Fix a = n.IsNil() ? (Fix)1 : (Fix)n.Number;
                Fix b = (Fix)m.Number;
                if (a < b)
                    d = (double)Updater.RandomFix(a, b);
                else
                    d = (double)Updater.RandomFix(b, a);
                __result = DynValue.NewNumber(d);
            }
            [HarmonyPatch("sin")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void sin(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec1(args, "sin", d => (double)(Fix.Sin((Fix)d)), __instance);
            }
            //Fix.Pow((Fix)2.718281828459045, (Fix)d)
            [HarmonyPatch("sinh")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void sinh(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                Debug.Log("sinh");
                __result = Plugin.exec1(args, "sinh", d => (double)((Fix.Pow((Fix)2.718281828459045, (Fix)d) - Fix.Pow((Fix)2.718281828459045, (Fix)(-d))) / (Fix)2), __instance);
            }
            [HarmonyPatch("sqrt")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void sqrt(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec1(args, "sqrt", d => (double)(Fix.Sqrt((Fix)d)), __instance);
            }
            [HarmonyPatch("tan")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void tan(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec1(args, "tan", d => (double)(Fix.Tan((Fix)d)), __instance);
            }
            [HarmonyPatch("tanh")]
            [HarmonyPostfix]
            //make it use Fix math instead of floating point math.
            private static void tanh(MoonSharp.Interpreter.CoreLib.MathModule __instance, ref DynValue __result, ScriptExecutionContext executionContext, CallbackArguments args)
            {
                __result = Plugin.exec1(args, "tanh", d => (double)Plugin.Tanh((Fix)d), __instance);
            }
        }
        [HarmonyPatch(typeof(MoonSharp.Interpreter.Tree.Expressions.BinaryOperatorExpression))]
        public class MoonSharpPatchThatHopefulyDoesntDoAnything
        {
            [HarmonyPatch("EvalArithmetic")]
            [HarmonyPostfix]
            //instance is a object as the type is private. and funcsons cant take in a type that chages at runtime drectly
            public static void EvalArithmetic(DynValue v1, DynValue v2, MoonSharp.Interpreter.Tree.Expressions.BinaryOperatorExpression __instance)
            {
                //the Operator enum just HAS to be private... ugg lets hope this works
                // Access the private field
                Debug.Log("MATH");
                FieldInfo privateField = __instance.GetType().GetField("Operator", BindingFlags.NonPublic | BindingFlags.Instance);
                object privateFieldValue = privateField.GetValue(__instance);
                int OperatorValue = (int)privateFieldValue;
                Debug.Log("OPERATOR VALUE IS: " + OperatorValue);
                throw new InvalidOperationException("IT TURNS OUT MoonSharp.Interpreter.Tree.Expressions.BinaryOperatorExpression.EvalArithmetic IS USED! WHO KNEW??? PLS PATCH IT SO IT USES FIX!!!");
            }
        }
        [HarmonyPatch(typeof(MoonSharp.Interpreter.Execution.VM.Processor))]
        public class MoonSharpMainMathOperatorsPatch
        {
            [HarmonyPatch("ExecAdd")]
            [HarmonyPostfix]
            public static void ExecAdd(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
            {
                var m_ValueStack = __instance.m_ValueStack;
                DynValue r = m_ValueStack.Pop().ToScalar();
                DynValue l = m_ValueStack.Pop().ToScalar();

                double? rn = r.CastToNumber();
                double? ln = l.CastToNumber();

                if (ln.HasValue && rn.HasValue)
                {
                    m_ValueStack.Push(DynValue.NewNumber((double)((Fix)ln.Value + (Fix)rn.Value)));
                    __result = instructionPtr;
                    return;
                }
                else
                {
                    int ip = __instance.Internal_InvokeBinaryMetaMethod(l, r, "__add", instructionPtr);
                    if (ip >= 0) __result = ip;
                    else throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                }
            }
            [HarmonyPatch("ExecAdd")]
            [HarmonyPrefix]
            public static bool ExecAddPrefix(Instruction i, int instructionPtr)
            {
                return false;
            }
            [HarmonyPatch("ExecSub")]
            [HarmonyPostfix]
            public static void ExecSub(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
            {
                var m_ValueStack = __instance.m_ValueStack;
                DynValue r = m_ValueStack.Pop().ToScalar();
                DynValue l = m_ValueStack.Pop().ToScalar();

                double? rn = r.CastToNumber();
                double? ln = l.CastToNumber();

                if (ln.HasValue && rn.HasValue)
                {
                    m_ValueStack.Push(DynValue.NewNumber((double)((Fix)ln.Value - (Fix)rn.Value)));
                    __result = instructionPtr;
                    return;
                }
                else
                {
                    int ip = __instance.Internal_InvokeBinaryMetaMethod(l, r, "__sub", instructionPtr);
                    if (ip >= 0) __result = ip;
                    else throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                }
            }
            [HarmonyPatch("ExecSub")]
            [HarmonyPrefix]
            public static bool ExecSubPrefix(Instruction i, int instructionPtr)
            {
                return false;
            }
            [HarmonyPatch("ExecMul")]
            [HarmonyPostfix]
            public static void ExecMul(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
            {
                var m_ValueStack = __instance.m_ValueStack;
                DynValue r = m_ValueStack.Pop().ToScalar();
                DynValue l = m_ValueStack.Pop().ToScalar();

                double? rn = r.CastToNumber();
                double? ln = l.CastToNumber();

                if (ln.HasValue && rn.HasValue)
                {
                    m_ValueStack.Push(DynValue.NewNumber((double)((Fix)ln.Value * (Fix)rn.Value)));
                    __result = instructionPtr;
                    return;
                }
                else
                {
                    int ip = __instance.Internal_InvokeBinaryMetaMethod(l, r, "__mul", instructionPtr);
                    if (ip >= 0) __result = ip;
                    else throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                }
            }
            [HarmonyPatch("ExecMul")]
            [HarmonyPrefix]
            public static bool ExecMulPrefix(Instruction i, int instructionPtr)
            {
                return false;
            }
            [HarmonyPatch("ExecMod")]
            [HarmonyPostfix]
            public static void ExecMod(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
            {
                var m_ValueStack = __instance.m_ValueStack;
                DynValue r = m_ValueStack.Pop().ToScalar();
                DynValue l = m_ValueStack.Pop().ToScalar();

                double? rn = r.CastToNumber();
                double? ln = l.CastToNumber();

                if (ln.HasValue && rn.HasValue)
                {
                    m_ValueStack.Push(DynValue.NewNumber((double)(Fix.SlowMod((Fix)ln.Value, (Fix)rn.Value))));
                    __result = instructionPtr;
                    return;
                }
                else
                {
                    int ip = __instance.Internal_InvokeBinaryMetaMethod(l, r, "__mod", instructionPtr);
                    if (ip >= 0) __result = ip;
                    else throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                }
            }
            [HarmonyPatch("ExecMod")]
            [HarmonyPrefix]
            public static bool ExecModPrefix(Instruction i, int instructionPtr)
            {
                return false;
            }
            [HarmonyPatch("ExecDiv")]
            [HarmonyPostfix]
            public static void ExecDiv(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
            {
                var m_ValueStack = __instance.m_ValueStack;
                DynValue r = m_ValueStack.Pop().ToScalar();
                DynValue l = m_ValueStack.Pop().ToScalar();

                double? rn = r.CastToNumber();
                double? ln = l.CastToNumber();
                try
                {


                    if (ln.HasValue && rn.HasValue)
                    {
                        m_ValueStack.Push(DynValue.NewNumber((double)((Fix)ln.Value / (Fix)rn.Value)));
                        __result = instructionPtr;
                        return;
                    }
                    else
                    {
                        int ip = __instance.Internal_InvokeBinaryMetaMethod(l, r, "__div", instructionPtr);
                        if (ip >= 0) __result = ip;
                        else throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                    }
                }
                catch (Exception e)
                {
                    throw new ScriptRuntimeException(e.Message);
                }
            }
            [HarmonyPatch("ExecDiv")]
            [HarmonyPrefix]
            public static bool ExecDivPrefix(Instruction i, int instructionPtr)
            {
                return false;
            }
            [HarmonyPatch("ExecPower")]
            [HarmonyPostfix]
            public static void ExecPower(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
            {
                var m_ValueStack = __instance.m_ValueStack;
                DynValue r = m_ValueStack.Pop().ToScalar();
                DynValue l = m_ValueStack.Pop().ToScalar();

                double? rn = r.CastToNumber();
                double? ln = l.CastToNumber();

                if (ln.HasValue && rn.HasValue)
                {
                    m_ValueStack.Push(DynValue.NewNumber((double)Fix.Pow((Fix)ln.Value, (Fix)rn.Value)));
                    __result = instructionPtr;
                    return;
                }
                else
                {
                    int ip = __instance.Internal_InvokeBinaryMetaMethod(l, r, "__pow", instructionPtr);
                    if (ip >= 0) __result = ip;
                    else throw ScriptRuntimeException.ArithmeticOnNonNumber(l, r);
                }
            }
            [HarmonyPatch("ExecPower")]
            [HarmonyPrefix]
            public static bool ExecPowerPrefix(Instruction i, int instructionPtr)
            {
                return false;
            }
            [HarmonyPatch("ExecNeg")]
            [HarmonyPostfix]
            public static void ExecNeg(Instruction i, int instructionPtr, MoonSharp.Interpreter.Execution.VM.Processor __instance, ref int __result)
            {
                var m_ValueStack = __instance.m_ValueStack;
                DynValue r = m_ValueStack.Pop().ToScalar();
                double? rn = r.CastToNumber();

                if (rn.HasValue)
                {
                    m_ValueStack.Push(DynValue.NewNumber(-rn.Value));
                    __result = instructionPtr;
                    return;
                }
                else
                {
                    int ip = __instance.Internal_InvokeUnaryMetaMethod(r, "__unm", instructionPtr);
                    if (ip >= 0) __result = ip;
                    else throw ScriptRuntimeException.ArithmeticOnNonNumber(r);
                }
            }
            [HarmonyPatch("ExecNeg")]
            [HarmonyPrefix]
            public static bool ExecNegPrefix(Instruction i, int instructionPtr)
            {
                return false;
            }
        }

        [HarmonyPatch(typeof(HookshotInstant))]
        public class HookshotInstantPatches
        {
            [HarmonyPatch("UseAbility")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(HookshotInstant __instance)
            {
                __instance.playerInfo = __instance.instantAbility.playerInfo;
                if (__instance.ropeBody != null && __instance.playerInfo.ropeBody != null && __instance.ropeBody.enabled && __instance.playerInfo.ropeBody.enabled && __instance.playerInfo.ropeBody.number == __instance.firedRopeNumber && (int)__instance.ropeBody.ownerId == __instance.playerInfo.playerId)
                {
                    if (__instance.tickWhenUsedLast + 60 < Updater.SimulationTicks)
                    {
                        AudioManager.Get().Play("reelInFire");
                    }
                    __instance.tickWhenUsedLast = Updater.SimulationTicks;
                    int num = __instance.playerInfo.topAttachment ? (__instance.ropeBody.segmentCount - 1) : 0;
                    __instance.framesSinceReelIn++;
                    if (__instance.framesSinceReelIn >= __instance.framesBetweenReelIns)
                    {
                        __instance.ropeBody.segmentSeparation = Fix.Max(__instance.ropeBody.segmentSeparation - __instance.ReelInSpeed / (Fix)((long)__instance.ropeBody.segmentCount), __instance.reeledInSegmentSeparation);
                        bool flag = true;
                        if (__instance.ropeBody.segmentSeparation < __instance.separationBeforeReelinDeletion)
                        {
                            flag = __instance.ropeBody.ReelInSegment(8);
                        }
                        if (!flag)
                        {
                            Vec2 u = Vec2.NormalizedSafe(__instance.ropeBody.segment[num] - __instance.playerInfo.slimeController.body.position) * (Fix)0.1;
                            if (!__instance.playerInfo.isGrounded && __instance.ropeBody.hookHasArrived)
                            {
                                __instance.playerInfo.slimeController.body.position = __instance.ropeBody.segment[num] + u;
                            }
                            __instance.ropeBody.enabled = false;
                            __instance.ropeBody.disabledThisFrame = true;
                            __instance.ropeBody = null;
                            AudioManager.Get().Play("hookshotLetGo");
                        }
                        __instance.framesSinceReelIn = 0;
                        return false;
                    }
                }
                else
                {
                    __instance.FireHook();
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(Missile))]
        public class MisslePatches
        {
            [HarmonyPatch("OnCollide")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(CollisionInformation collision, Missile __instance)
            {
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
                if (collision.layer == LayerMask.NameToLayer("RigidBodyAffector") || collision.layer == LayerMask.NameToLayer("Rope") || collision.layer == (LayerMask)3)
                {
                    return false;
                }
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
                FixTransform.InstantiateFixed<Explosion>(__instance.onHitExplosionPrefab, __instance.body.position).GetComponent<IPhysicsCollider>().Scale = __instance.fixTrans.Scale;
                if (!string.IsNullOrEmpty(__instance.soundEffectOnCol))
                {
                    AudioManager.Get().Play(__instance.soundEffectOnCol);
                }
                Updater.DestroyFix(__instance.gameObject);
                return false;
            }
        }
        [HarmonyPatch(typeof(RopeHook))]
        public class RopeHookPatches
        {
            [HarmonyPatch("OnCollide")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(CollisionInformation collision, RopeHook __instance)
            {
                //if its a trigger dont do anything.
#pragma warning disable Harmony003 // Harmony non-ref patch parameters modified
                if (collision.layer == (LayerMask)3)
                {
                    return false;
                }
#pragma warning restore Harmony003 // Harmony non-ref patch parameters modified
                return true;
            }
        }
        [HarmonyPatch(typeof(GameSession))]
        public class GameSessionPatches
        {
            [HarmonyPatch("RandomBagLevel")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(GameSession __instance)
            {
                Debug.Log("RandomBagLevel");
                //its max exsclusive min inclusinve
                if (Plugin.MapJsons.Length != 0)
                {
                    Plugin.CurrentMapIndex = Plugin.RandomBagLevel();
                    Dictionary<string, object> MetaData = MiniJSON.Json.Deserialize(Plugin.MetaDataJsons[Plugin.CurrentMapIndex]) as Dictionary<string, object>;
                    var type = Convert.ToString(MetaData["MapType"]);
                    switch (type)
                    {
                        case "space":
                            GameSession.currentLevel = (byte)Plugin.SpaceMapId;
                            SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                            break;
                        case "snow":
                            GameSession.currentLevel = (byte)Plugin.SnowMapId;
                            SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                            break;
                        default:
                            GameSession.currentLevel = (byte)Plugin.GrassMapId;
                            SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                            break;
                    }
                    var UUID = Convert.ToInt32(MetaData["MapUUID"]);
                    Plugin.CurrentMapUUID = UUID;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(CharacterSelectHandler))]
        public class CharacterSelectHandlerPatches
        {
            [HarmonyPatch("TryStartGame_inner")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(CharacterSelectHandler __instance)
            {
                if (CharacterSelectHandler.startButtonAvailable && CharacterSelectHandler.allReadyForMoreThanOneFrame)
                {
                    AudioManager audioManager = AudioManager.Get();
                    if (audioManager != null)
                    {
                        audioManager.Play("startGame");
                    }
                    CharacterSelectHandler.startButtonAvailable = false;
                    List<Player> list = PlayerHandler.Get().PlayerList();
                    list.Clear();
                    int num = 1;
                    NamedSpriteList abilityIcons = SteamManager.instance.abilityIcons;
                    for (int i = 0; i < __instance.characterSelectBoxes.Length; i++)
                    {
                        if (__instance.characterSelectBoxes[i].menuState == CharSelectMenu.ready)
                        {
                            PlayerInit playerInit = __instance.characterSelectBoxes[i].playerInit;
                            Player player = new Player(num, playerInit.team);
                            player.Color = __instance.playerColors[playerInit.color].playerMaterial;
                            player.UsesKeyboardAndMouse = playerInit.usesKeyboardMouse;
                            player.CanUseAbilities = true;
                            player.inputDevice = playerInit.inputDevice;
                            player.Abilities = new List<GameObject>(3);
                            player.AbilityIcons = new List<Sprite>(3);
                            player.Abilities.Add(abilityIcons.sprites[playerInit.ability0].associatedGameObject);
                            player.AbilityIcons.Add(abilityIcons.sprites[playerInit.ability0].sprite);
                            Settings settings = Settings.Get();
                            if (settings != null && settings.NumberOfAbilities > 1)
                            {
                                player.Abilities.Add(abilityIcons.sprites[playerInit.ability1].associatedGameObject);
                                player.AbilityIcons.Add(abilityIcons.sprites[playerInit.ability1].sprite);
                            }
                            Settings settings2 = Settings.Get();
                            if (settings2 != null && settings2.NumberOfAbilities > 2)
                            {
                                player.Abilities.Add(abilityIcons.sprites[playerInit.ability2].associatedGameObject);
                                player.AbilityIcons.Add(abilityIcons.sprites[playerInit.ability2].sprite);
                            }
                            player.CustomKeyBinding = playerInit.keybindOverride;
                            num++;
                            list.Add(player);
                        }
                    }
                    GameSession.Init();
                    //SceneManager.LoadScene("Level1");
                    Debug.Log("TryStartGame_inner");
                    if (Plugin.MapJsons.Length != 0)
                    {
                        //its max exsclusive min inclusinve
                        Plugin.CurrentMapIndex = Plugin.RandomBagLevel();
                        Dictionary<string, object> MetaData = MiniJSON.Json.Deserialize(Plugin.MetaDataJsons[Plugin.CurrentMapIndex]) as Dictionary<string, object>;
                        var type = Convert.ToString(MetaData["MapType"]);
                        switch (type)
                        {
                            case "space":
                                GameSession.currentLevel = (byte)Plugin.SpaceMapId;
                                SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                                break;
                            case "snow":
                                GameSession.currentLevel = (byte)Plugin.SnowMapId;
                                SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                                break;
                            default:
                                GameSession.currentLevel = (byte)Plugin.GrassMapId;
                                SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                                break;
                        }
                        var UUID = Convert.ToInt32(MetaData["MapUUID"]);
                        Plugin.CurrentMapUUID = UUID;
                        SceneManager.LoadScene((int)(6 + GameSession.CurrentLevel()), LoadSceneMode.Single);
                    }
                    else SceneManager.LoadScene("Level1");

                    if (!WinnerTriangleCanvas.HasBeenSpawned)
                    {
                        SceneManager.LoadScene("winnerTriangle", LoadSceneMode.Additive);
                    }
                    Debug.Log(WinnerTriangleCanvas.instance);
                }
                return false;
            }
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug2(CharacterSelectHandler __instance)
            {
                if (!Plugin.IsInTestMode)
                {
                    GameSessionHandler.LeaveGame(false, false);
                }
                
            }
        }
        [HarmonyPatch(typeof(CharacterSelectHandler_online))]
        public class CharacterSelectHandler_onlinePatches
        {
            [HarmonyPatch("ForceStartGame")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(CharacterSelectHandler_online __instance, ref PlayerColors pcs)
            {
                MonoBehaviour.print("FORCE START GAME");
                if (pcs == null)
                {
                    pcs = CharacterSelectHandler_online.selfRef.playerColors;
                }
                if (!Plugin.IsReplay())
                {
                    SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                }

                StartRequestPacket startParameters = SteamManager.startParameters;
                Updater.ReInit();
                List<Player> list = new List<Player>();
                Updater.InitSeed(startParameters.seed);
                if (startParameters.nrOfPlayers > 0)
                {
                    list.Add(CharacterSelectHandler_online.InitPlayer(1, startParameters.p1_color, startParameters.p1_team, startParameters.p1_ability1, startParameters.p1_ability2, startParameters.p1_ability3, (int)startParameters.nrOfAbilites, pcs));
                }
                if (startParameters.nrOfPlayers > 1)
                {
                    list.Add(CharacterSelectHandler_online.InitPlayer(2, startParameters.p2_color, startParameters.p2_team, startParameters.p2_ability1, startParameters.p2_ability2, startParameters.p2_ability3, (int)startParameters.nrOfAbilites, pcs));
                }
                if (startParameters.nrOfPlayers > 2)
                {
                    list.Add(CharacterSelectHandler_online.InitPlayer(3, startParameters.p3_color, startParameters.p3_team, startParameters.p3_ability1, startParameters.p3_ability2, startParameters.p3_ability3, (int)startParameters.nrOfAbilites, pcs));
                }
                if (startParameters.nrOfPlayers > 3)
                {
                    list.Add(CharacterSelectHandler_online.InitPlayer(4, startParameters.p4_color, startParameters.p4_team, startParameters.p4_ability1, startParameters.p4_ability2, startParameters.p4_ability3, (int)startParameters.nrOfAbilites, pcs));
                }
                Player player = null;
                if (GameLobby.isPlayingAReplay)
                {
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i].Id == 1)
                        {
                            player = list[i];
                            break;
                        }
                    }
                }
                else if (startParameters.p1_id == SteamClient.SteamId)
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j].Id == 1)
                        {
                            player = list[j];
                            break;
                        }
                    }
                }
                else if (startParameters.p2_id == SteamClient.SteamId)
                {
                    for (int k = 0; k < list.Count; k++)
                    {
                        if (list[k].Id == 2)
                        {
                            player = list[k];
                            break;
                        }
                    }
                }
                else if (startParameters.p3_id == SteamClient.SteamId)
                {
                    for (int l = 0; l < list.Count; l++)
                    {
                        if (list[l].Id == 3)
                        {
                            player = list[l];
                            break;
                        }
                    }
                }
                else if (startParameters.p4_id == SteamClient.SteamId)
                {
                    for (int m = 0; m < list.Count; m++)
                    {
                        if (list[m].Id == 4)
                        {
                            player = list[m];
                            break;
                        }
                    }
                }
                for (int n = 0; n < list.Count; n++)
                {
                    switch (list[n].Id)
                    {
                        case 1:
                            list[n].steamId = startParameters.p1_id;
                            break;
                        case 2:
                            list[n].steamId = startParameters.p2_id;
                            break;
                        case 3:
                            list[n].steamId = startParameters.p3_id;
                            break;
                        case 4:
                            list[n].steamId = startParameters.p4_id;
                            break;
                    }
                }
                player.IsLocalPlayer = true;
                player.inputDevice = CharacterSelectHandler_online.localPlayerInit.inputDevice;
                player.UsesKeyboardAndMouse = CharacterSelectHandler_online.localPlayerInit.usesKeyboardMouse;
                player.CustomKeyBinding = CharacterSelectHandler_online.localPlayerInit.keybindOverride;
                CharacterSelectHandler_online.startButtonAvailable = false;
                PlayerHandler.Get().SetPlayerList(list);
                SteamManager.instance.StartHostedGame();
                AudioManager audioManager = AudioManager.Get();
                if (audioManager != null)
                {
                    audioManager.Play("startGame");
                }
                GameSession.Init();
                if (GameLobby.isPlayingAReplay)
                {
                    SceneManager.LoadScene((int)(startParameters.currentLevel + 6));
                }
                else
                {
                    SceneManager.LoadScene((int)(startParameters.currentLevel + 6));
                }
                if (!WinnerTriangleCanvas.HasBeenSpawned)
                {
                    SceneManager.LoadScene("winnerTriangle", LoadSceneMode.Additive);
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(SteamManager))]
        public class SteamManagerPatches
        {
            [HarmonyPatch("OnLobbyMemberJoinedCallback")]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug(ref Lobby lobby, Friend friend, SteamManager __instance)
            {
                if (__instance.currentLobby.Id != lobby.Id)
                {
                    return;
                }
                //if we own the lobby send the new player the first map
                if (SteamManager.LocalPlayerIsLobbyOwner)
                {
                    //if someone joins us in test mode put the maps back to normal
                    if (Plugin.IsInTestMode)
                    {
                        Debug.Log("someone joined us in test mode, putting the maps back to normal before sending them any maps");
                        //fill the MapJsons array up
                        ZipArchive[] zipArchives = Plugin.MyZipArchives;
                        Plugin.zipArchives = Plugin.MyZipArchives;
                        //Create a List for the json for a bit
                        List<string> JsonList = new List<string>();
                        List<string> MetaDataList = new();
                        foreach (ZipArchive zipArchive in zipArchives)
                        {
                            //get the first .boplmap file if there is multiple. (THERE SHOULD NEVER BE MULTIPLE .boplmap's IN ONE .zip)
                            JsonList.Add(Plugin.GetFileFromZipArchive(zipArchive, Plugin.IsBoplMap)[0]);
                            MetaDataList.Add(Plugin.GetFileFromZipArchive(zipArchive, Plugin.IsMetaDataFile)[0]);
                        }
                        Plugin.MapJsons = JsonList.ToArray();
                        Plugin.MetaDataJsons = MetaDataList.ToArray();
                    }
                    Plugin.NextMapIndex = Plugin.RandomBagLevel();
                    ZipArchivePacket zipArchivePacket = new ZipArchivePacket
                    {
                        zip = Plugin.MyZipArchives[Plugin.NextMapIndex],
                        length = Plugin.MyZipArchives.Length,
                        id = Plugin.NextMapIndex
                    };
                    NetworkingStuff.ZipChannel.SendMessage(zipArchivePacket);
                    //set all this stuff
                    NetworkingStuff.MilisecondsToDelayBeforeResendingZip = NetworkingStuff.GetDelayForResendingZip();
                    NetworkingStuff.HasRecevedLatestZip = new();
                }
            }
            [HarmonyPatch("OnLevelWasLoaded")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(int level, SteamManager __instance)
            {
                Debug.Log($"set network client to {Host.host}, is it null: {Host.host == null}");
                __instance.networkClient = Host.host;
                return false;
            }
            [HarmonyPatch("HostGame")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug2(ref PlayerInit hostPlayer, SteamManager __instance)
            {
                
                Plugin.CurrentMapIndex = Plugin.NextMapIndex;
                Debug.Log($"starting first game on map {Plugin.CurrentMapIndex}");
                Plugin.NextMapIndex = Plugin.RandomBagLevel();
                Debug.Log($"next map will be {Plugin.NextMapIndex}");
                ZipArchivePacket zipArchivePacket = new ZipArchivePacket
                {
                    zip = Plugin.MyZipArchives[Plugin.NextMapIndex],
                    length = Plugin.MyZipArchives.Length,
                    id = Plugin.NextMapIndex
                };
                NetworkingStuff.ZipChannel.SendMessage(zipArchivePacket);
                //set all this stuff
                NetworkingStuff.MilisecondsToDelayBeforeResendingZip = NetworkingStuff.GetDelayForResendingZip();
                NetworkingStuff.HasRecevedLatestZip = new();
                //its max exsclusive min inclusinve
                if (Plugin.MapJsons.Length != 0)
                {
                    UnityEngine.Debug.Log($"we have {Plugin.MapJsons.Length} maps");
                    UnityEngine.Debug.Log($"map index is {Plugin.CurrentMapIndex}");
                    Dictionary<string, object> MetaData = MiniJSON.Json.Deserialize(Plugin.MetaDataJsons[Plugin.CurrentMapIndex]) as Dictionary<string, object>;
                    var type = Convert.ToString(MetaData["MapType"]);
                    UnityEngine.Debug.Log("getting map type");
                    switch (type)
                    {
                        case "space":
                            GameSession.currentLevel = (byte)Plugin.SpaceMapId;
                            SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                            break;
                        case "snow":
                            GameSession.currentLevel = (byte)Plugin.SnowMapId;
                            SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                            break;
                        default:
                            GameSession.currentLevel = (byte)Plugin.GrassMapId;
                            SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                            break;
                    }
                    var UUID = Convert.ToInt32(MetaData["MapUUID"]);
                    Plugin.CurrentMapUUID = UUID;
                }
                __instance.currentLobby.SetData("LFM", "0");
                __instance.currentLobby.SetFriendsOnly();
                __instance.currentLobby.SetJoinable(false);
                SteamManager.startParameters = default(StartRequestPacket);
                ushort num = __instance.nextStartGameSeq;
                __instance.nextStartGameSeq = (ushort)(num + 1);
                SteamManager.startParameters.seqNum = num;
                SteamManager.startParameters.nrOfPlayers = (byte)(__instance.connectedPlayers.Count + 1);
                SteamManager.startParameters.nrOfAbilites = (byte)Settings.Get().NumberOfAbilities;
                SteamManager.startParameters.currentLevel = GameSession.CurrentLevel();
                SteamManager.startParameters.seed = (uint)Environment.TickCount;
                SteamManager.startParameters.p1_id = SteamClient.SteamId;
                SteamManager.startParameters.p1_team = (byte)hostPlayer.team;
                SteamManager.startParameters.p1_color = (byte)hostPlayer.color;
                SteamManager.startParameters.p1_ability1 = (byte)hostPlayer.ability0;
                SteamManager.startParameters.p1_ability2 = (byte)hostPlayer.ability1;
                SteamManager.startParameters.p1_ability3 = (byte)hostPlayer.ability2;
                if (__instance.connectedPlayers.Count > 0)
                {
                    SteamManager.startParameters.p2_id = __instance.connectedPlayers[0].id;
                    SteamManager.startParameters.p2_team = __instance.connectedPlayers[0].lobby_team;
                    SteamManager.startParameters.p2_color = (byte)__instance.connectedPlayers[0].lobby_color;
                    SteamManager.startParameters.p2_ability1 = __instance.connectedPlayers[0].lobby_ability1;
                    SteamManager.startParameters.p2_ability2 = __instance.connectedPlayers[0].lobby_ability2;
                    SteamManager.startParameters.p2_ability3 = __instance.connectedPlayers[0].lobby_ability3;
                }
                if (__instance.connectedPlayers.Count > 1)
                {
                    SteamManager.startParameters.p3_id = __instance.connectedPlayers[1].id;
                    SteamManager.startParameters.p3_team = __instance.connectedPlayers[1].lobby_team;
                    SteamManager.startParameters.p3_color = (byte)__instance.connectedPlayers[1].lobby_color;
                    SteamManager.startParameters.p3_ability1 = __instance.connectedPlayers[1].lobby_ability1;
                    SteamManager.startParameters.p3_ability2 = __instance.connectedPlayers[1].lobby_ability2;
                    SteamManager.startParameters.p3_ability3 = __instance.connectedPlayers[1].lobby_ability3;
                }
                if (__instance.connectedPlayers.Count > 2)
                {
                    SteamManager.startParameters.p4_id = __instance.connectedPlayers[2].id;
                    SteamManager.startParameters.p4_team = __instance.connectedPlayers[2].lobby_team;
                    SteamManager.startParameters.p4_color = (byte)__instance.connectedPlayers[2].lobby_color;
                    SteamManager.startParameters.p4_ability1 = __instance.connectedPlayers[2].lobby_ability1;
                    SteamManager.startParameters.p4_ability2 = __instance.connectedPlayers[2].lobby_ability2;
                    SteamManager.startParameters.p4_ability3 = __instance.connectedPlayers[2].lobby_ability3;
                }
                byte b = (byte)(SteamManager.instance.dlc.HasDLC() ? 1 : 0);
                for (int i = 0; i < __instance.connectedPlayers.Count; i++)
                {
                    if (__instance.connectedPlayers[i].ownsFullGame)
                    {
                        b = (byte)((int)b | 1 << i + 1);
                    }
                }
                SteamManager.startParameters.isDemoMask = b;
                SteamManager.instance.EncodeCurrentStartParameters_forReplay(ref SteamManager.instance.networkClient.EncodedStartRequest, SteamManager.startParameters, false);
                var betterStartRequestPacket = new BetterStartRequestPacket
                {
                    startRequest = SteamManager.startParameters,
                    MapIndex = Plugin.CurrentMapIndex
                };
                NetworkingStuff.StartChannel.SendMessage(betterStartRequestPacket);
                return false;
            }
            [HarmonyPatch("HostNextLevel")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug2(Player hostPlayer, NamedSpriteList abilityIcons, SteamManager __instance)
            {
                Plugin.CurrentMapIndex = Plugin.NextMapIndex;
                Debug.Log($"starting next level on map {Plugin.CurrentMapIndex}");
                Plugin.NextMapIndex = Plugin.RandomBagLevel();
                Debug.Log($"next map is {Plugin.NextMapIndex}");
                ZipArchivePacket zipArchivePacket = new ZipArchivePacket
                {
                    zip = Plugin.MyZipArchives[Plugin.NextMapIndex],
                    length = Plugin.MyZipArchives.Length,
                    id = Plugin.NextMapIndex
                };
                NetworkingStuff.ZipChannel.SendMessage(zipArchivePacket);
                //set all this stuff
                NetworkingStuff.MilisecondsToDelayBeforeResendingZip = NetworkingStuff.GetDelayForResendingZip();
                NetworkingStuff.HasRecevedLatestZip = new();
                //its max exsclusive min inclusinve
                if (Plugin.MapJsons.Length != 0)
                {
                    UnityEngine.Debug.Log($"we have {Plugin.MapJsons.Length} maps");
                    UnityEngine.Debug.Log($"map index is {Plugin.CurrentMapIndex}");
                    Dictionary<string, object> MetaData = MiniJSON.Json.Deserialize(Plugin.MetaDataJsons[Plugin.CurrentMapIndex]) as Dictionary<string, object>;
                    var type = Convert.ToString(MetaData["MapType"]);
                    UnityEngine.Debug.Log("getting map type");
                    switch (type)
                    {
                        case "space":
                            GameSession.currentLevel = (byte)Plugin.SpaceMapId;
                            SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                            break;
                        case "snow":
                            GameSession.currentLevel = (byte)Plugin.SnowMapId;
                            SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                            break;
                        default:
                            GameSession.currentLevel = (byte)Plugin.GrassMapId;
                            SteamManager.startParameters.currentLevel = GameSession.currentLevel;
                            break;
                    }
                    var UUID = Convert.ToInt32(MetaData["MapUUID"]);
                    Plugin.CurrentMapUUID = UUID;
                }
                GameSession.CurrentLevel();
                SteamManager.startParameters.frameBufferSize = (byte)Host.CurrentDelayBufferSize;
                SteamManager.startParameters.seed = (uint)Environment.TickCount;
                SteamManager.startParameters.nrOfPlayers = (byte)(__instance.connectedPlayers.Count + 1);
                Debug.Log($"nrOfPlayers is {SteamManager.startParameters.nrOfPlayers}");
                SteamManager.startParameters.currentLevel = GameSession.CurrentLevel();
                SteamManager.startParameters.p1_ability1 = (byte)abilityIcons.IndexOf(hostPlayer.Abilities[0].name);
                if (Settings.Get().NumberOfAbilities > 1)
                {
                    SteamManager.startParameters.p1_ability2 = (byte)abilityIcons.IndexOf(hostPlayer.Abilities[1].name);
                }
                if (Settings.Get().NumberOfAbilities > 2)
                {
                    SteamManager.startParameters.p1_ability3 = (byte)abilityIcons.IndexOf(hostPlayer.Abilities[2].name);
                }
                if (__instance.connectedPlayers.Count > 0)
                {
                    SteamManager.startParameters.p2_ability1 = __instance.connectedPlayers[0].lobby_ability1;
                    SteamManager.startParameters.p2_ability2 = __instance.connectedPlayers[0].lobby_ability2;
                    SteamManager.startParameters.p2_ability3 = __instance.connectedPlayers[0].lobby_ability3;
                }
                if (__instance.connectedPlayers.Count > 1)
                {
                    SteamManager.startParameters.p3_ability1 = __instance.connectedPlayers[1].lobby_ability1;
                    SteamManager.startParameters.p3_ability2 = __instance.connectedPlayers[1].lobby_ability2;
                    SteamManager.startParameters.p3_ability3 = __instance.connectedPlayers[1].lobby_ability3;
                }
                if (__instance.connectedPlayers.Count > 2)
                {
                    SteamManager.startParameters.p4_ability1 = __instance.connectedPlayers[2].lobby_ability1;
                    SteamManager.startParameters.p4_ability2 = __instance.connectedPlayers[2].lobby_ability2;
                    SteamManager.startParameters.p4_ability3 = __instance.connectedPlayers[2].lobby_ability3;
                }
                SteamManager.instance.EncodeCurrentStartParameters_forReplay(ref SteamManager.instance.networkClient.EncodedStartRequest, SteamManager.startParameters, false);
                var betterStartRequestPacket = new BetterStartRequestPacket
                {
                    startRequest = SteamManager.startParameters,
                    MapIndex = Plugin.CurrentMapIndex
                };
                NetworkingStuff.StartChannel.SendMessage(betterStartRequestPacket); 
                return false;
            }
        }
        [HarmonyPatch(typeof(GameSessionHandler))]
        public class GameSessionHandlerPatches
        {
            [HarmonyPatch("LeaveGame")]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug(GameSessionHandler __instance)
            {
                if (!Plugin.IsInTestMode)
                {
                    Debug.Log($"number of players is {SteamManager.startParameters.nrOfPlayers}");
                    //throw new NotImplementedException();
                    //fill the MapJsons array up
                    ZipArchive[] zipArchives = Plugin.MyZipArchives;
                    Plugin.zipArchives = Plugin.MyZipArchives;
                    //Create a List for the json for a bit
                    List<string> JsonList = new List<string>();
                    List<string> MetaDataList = new();
                    foreach (ZipArchive zipArchive in zipArchives)
                    {
                        //get the first .boplmap file if there is multiple. (THERE SHOULD NEVER BE MULTIPLE .boplmap's IN ONE .zip)
                        JsonList.Add(Plugin.GetFileFromZipArchive(zipArchive, Plugin.IsBoplMap)[0]);
                        MetaDataList.Add(Plugin.GetFileFromZipArchive(zipArchive, Plugin.IsMetaDataFile)[0]);
                    }
                    Plugin.MapJsons = JsonList.ToArray();
                    Plugin.MetaDataJsons = MetaDataList.ToArray();
                }
            }
            [HarmonyPatch("AnimateOutLevel")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug2(GameSessionHandler __instance)
            {
                //idk why but unity crashes when it calls FindObjectOfType sometimes when online so this should hopefuly fix that.
                Plugin.averageCamera.enabled = false;
                for (int i = 0; i < __instance.grounds.Length; i++)
                {
                    if (!(__instance.grounds[i] == null) && !__instance.grounds[i].IsDestroyed)
                    {
                        AnimateVelocity component = __instance.grounds[i].ThisGameObject().GetComponent<AnimateVelocity>();
                        if (component != null)
                        {
                            component.enabled = false;
                        }
                    }
                }
                for (int j = 0; j < __instance.startPositions.Length; j++)
                {
                    if (!(__instance.grounds[j] == null) && !__instance.grounds[j].IsDestroyed)
                    {
                        __instance.targetPositions[j] = __instance.grounds[j].GetComponent<FixTransform>().position;
                    }
                }
                for (int k = 0; k < __instance._playedArrivalSound.Length; k++)
                {
                    __instance._playedArrivalSound[k] = false;
                }
                __instance._t = __instance.startProgressOfAnimateInGrounds;
                __instance.levelAnimationRoutine.WhileLoop(new Func<Fix, bool>(__instance.AnimateOutLoop));
                return false;
            }
        }
        [HarmonyPatch(typeof(PlayerAverageCamera))]
        public class PlayerAverageCameraPatches
        {
            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug(PlayerAverageCamera __instance)
            {
                Plugin.averageCamera = __instance;
                //__instance.UpdateY = true;
            }
            [HarmonyPatch("UpdateCamera")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug2(PlayerAverageCamera __instance)
            {
                List<Player> list = PlayerHandler.Get().PlayerList();
                Vector2 vector = default(Vector2);
                float d = (float)list.Count;
                float num = 0f;
                float num2 = 0f;
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].IsAlive)
                    {
                        Vector2 vector2 = (Vector2)list[i].Position;
                        vector += vector2;
                        num = Mathf.Max(num, vector2.x);
                        num2 = Mathf.Min(num2, vector2.x);
                    }
                }
                Vec2[] playerSpawns_readonly = GameSessionHandler.playerSpawns_readonly;
                vector /= d;
                if (__instance.useMinMaxForCameraX)
                {
                    vector = new Vector2((num2 + num) * 0.5f, vector.y);
                }
                float num3 = Mathf.Min(__instance.maxDeltaTime, Time.unscaledDeltaTime);
                Vector2 vector3 = (1f - __instance.weight * num3) * (Vector2)__instance.transform.localPosition + __instance.weight * num3 * vector;
                float num4 = __instance.camera.orthographicSize * __instance.camera.aspect + __instance.outsideLevelX;
                vector3.x = Mathf.Max((float)SceneBounds.Camera_XMin + num4, vector3.x);
                vector3.x = Mathf.Min((float)SceneBounds.Camera_XMax - num4, vector3.x);
                vector3.x = __instance.RoundToNearestPixel(vector3.x);
                vector3.y = Mathf.Max((float)SceneBounds.WaterHeight + __instance.MinHeightAboveFloor, vector3.y);
                vector3.y = Mathf.Min((float)SceneBounds.Camera_YMax, vector3.y);
                vector3.y = __instance.RoundToNearestPixel(vector3.y);
                if (!__instance.UpdateX)
                {
                    vector3.x = __instance.transform.position.x;
                }
                if (!__instance.UpdateY)
                {
                    vector3.y = __instance.transform.position.y;
                }
                Vector3 position = new Vector3(vector3.x, vector3.y, __instance.transform.position.z);
                __instance.transform.position = position;
                float num5 = 0f;
                float num6 = 0f;
                for (int j = 0; j < list.Count; j++)
                {
                    Vector2 vector4 = (Vector2)list[j].Position;
                    float b = Mathf.Abs(vector4.x - vector.x);
                    num5 = Mathf.Max(num5, b);
                    b = Mathf.Abs(vector4.y - vector.y);
                    num6 = Mathf.Max(num6, b);
                }
                float num7 = (float)(Screen.width / Screen.height);
                num5 *= num7;
                float num8 = Mathf.Max(num5, num6);
                num8 += __instance.extraZoomRoom;
                if (num8 > __instance.MAX_ZOOM)
                {
                    num8 = __instance.MAX_ZOOM;
                }
                if (num8 < __instance.MIN_ZOOM)
                {
                    num8 = __instance.MIN_ZOOM;
                }
                float num9 = Mathf.Clamp((1f - __instance.zoomWeight * num3) * __instance.camera.orthographicSize + __instance.zoomWeight * num3 * num8, __instance.MIN_ZOOM, __instance.MAX_ZOOM);
                if (__instance.camera.orthographicSize != num9)
                {
                    __instance.camera.orthographicSize = num9;
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(Host))]
        public class HostPatches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug(Host __instance)
            {
                if (__instance.gameObject.scene.name == "DontDestroyOnLoad")
                {
                    Host.host = __instance;
                    Debug.Log($"host is {__instance.gameObject.name}");
                }

            }
            [HarmonyPatch("Init")]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug2(Host __instance)
            {
                try
                {
                    Debug.Log(__instance.gameObject.name);
                }
                catch
                {
                    Debug.LogError("Host isnt on a gameobject????");
                }
                //throw new NotImplementedException();
            }
            [HarmonyPatch("SaveReplay")]
            [HarmonyPrefix]
            private static bool SaveReplay_MapMaker_Plug(Host __instance)
            {
                Updater.gameHasStopped = true;
                if (Host.recordReplay && __instance.inputRecording.Count != 0 && !(SteamManager.instance == null))
                {
                    string arg = Application.persistentDataPath + "/replays/";
                    byte[] bytes = NetworkTools.SerializeReplay_compressed(__instance.inputRecording, __instance.EncodedStartRequest);
                    ZipArchive zip = Plugin.zipArchives[Plugin.CurrentMapIndex];
                    //chatgpt code
                    using (var memoryStream = new MemoryStream())
                    {
                        // Create a new ZipArchive in the MemoryStream
                        using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                        {
                            foreach (var entry in zip.Entries)
                            {
                                var newEntry = zipArchive.CreateEntry(entry.FullName);
                                using (var entryStream = entry.Open())
                                using (var newEntryStream = newEntry.Open())
                                {
                                    entryStream.CopyTo(newEntryStream);
                                }
                            }
                        }
                        //my code
                        //first the length
                        var bytes2 = BitConverter.GetBytes(memoryStream.ToArray().Length).ToList();
                        //now the zip
                        bytes2.AddRange(memoryStream.ToArray().ToList());
                        //then the main replay stuff
                        bytes2.AddRange(bytes);

                        Host.replaysSaved++;
                        string str = arg + Host.replaysSaved + ".rep";
                        File.WriteAllBytes(arg + Host.replaysSaved + ".rep", bytes2.ToArray());
                        Debug.Log("saved replay " + str);
                    }
                }
                __instance.inputRecording.Clear();
                return false;
            }

        }
        [HarmonyPatch(typeof(ShakableCamera))]
        public class ShakableCameraPatches
        {
            [HarmonyPatch("Start")]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug(ShakableCamera __instance)
            {
                Plugin.shakableCamera = __instance;
            }
        }
        [HarmonyPatch(typeof(Beam))]
        public class BeamPatches
        {
            static FieldInfo f_Shakeable_Field = AccessTools.Field(typeof(Plugin), nameof(Plugin.shakableCamera));
            static MethodInfo m_Find_Object_Of_Type = SymbolExtensions.GetMethodInfo(() => UnityEngine.Object.FindObjectOfType<ShakableCamera>());

            [HarmonyPatch("Awake")]
            [HarmonyTranspiler]
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                var found = false;
                foreach (var instruction in instructions)
                {
                    if (instruction.Calls(m_Find_Object_Of_Type))
                    {
                        yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Ldsfld, f_Shakeable_Field);
                        found = true;
                    }
                    else { yield return instruction; }
                    
                }
                if (found is false)
                    Debug.LogError("error beam awake transpiler failed");
                
            }
        }
        [HarmonyPatch(typeof(ControlPlatform))]
        public class ControlPlatformPatches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(ControlPlatform __instance)
            {
                Updater.RegisterUpdatable(__instance);
                __instance.animator = __instance.GetComponent<SpriteAnimator>();
                __instance.spriteRen = __instance.GetComponent<SpriteRenderer>();
                __instance.body = __instance.GetComponent<PlayerBody>();
                __instance.ability = __instance.GetComponent<Ability>();
                __instance.physics = __instance.GetComponent<PlayerPhysics>();
                __instance.shakeCam = Plugin.shakableCamera;
                __instance.hurtbox = __instance.GetComponent<DPhysicsBox>();
                return false;
            }
            static FieldInfo f_Plugin_Cam_X_Min_Field = AccessTools.Field(typeof(Plugin), nameof(Plugin.Camera_XMin));
            static FieldInfo f_Old_Cam_X_Min_Field = AccessTools.Field(typeof(SceneBounds), nameof(SceneBounds.Camera_XMin));
            static FieldInfo f_Plugin_Cam_X_Max_Field = AccessTools.Field(typeof(Plugin), nameof(Plugin.Camera_XMax));
            static FieldInfo f_Old_Cam_X_Max_Field = AccessTools.Field(typeof(SceneBounds), nameof(SceneBounds.Camera_XMax));
            static FieldInfo f_Plugin_Cam_Y_Max_Field = AccessTools.Field(typeof(Plugin), nameof(Plugin.Camera_YMax));
            static FieldInfo f_Old_Cam_Y_Max_Field = AccessTools.Field(typeof(SceneBounds), nameof(SceneBounds.Camera_YMax));
            [HarmonyPatch("UpdateSim")]
            [HarmonyTranspiler]

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var found = 0;
                foreach (var instruction in instructions)
                {
                    if (instruction.LoadsField(f_Old_Cam_X_Min_Field))
                    {
                        yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Ldsfld, f_Plugin_Cam_X_Min_Field);
                        found = found + 1;
                    }
                    else if (instruction.LoadsField(f_Old_Cam_X_Max_Field))
                    {
                        yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Ldsfld, f_Plugin_Cam_X_Max_Field);
                        found = found + 1;
                    }
                    else if (instruction.LoadsField(f_Old_Cam_Y_Max_Field))
                    {
                        yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Ldsfld, f_Plugin_Cam_Y_Max_Field);
                        found = found + 1;
                    }
                    else { yield return instruction; }
                }
                if (found < 3)
                    Debug.LogError("ControlPlatform transpiler failed");
            }
        }
        [HarmonyPatch(typeof(CastSpell))]
        public class CastSpellPatches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(CastSpell __instance)
            {
                Updater.RegisterUpdatable(__instance);
                __instance.playerCol = __instance.GetComponent<PlayerCollision>();
                __instance.animator = __instance.GetComponent<SpriteAnimator>();
                __instance.spriteRen = __instance.GetComponent<SpriteRenderer>();
                __instance.body = __instance.GetComponent<PlayerBody>();
                __instance.ability = __instance.GetComponent<Ability>();
                __instance.physics = __instance.GetComponent<PlayerPhysics>();
                __instance.bigCollider = __instance.GetComponent<DPhysicsCircle>();
                __instance.shakeCam = Plugin.shakableCamera;
                __instance.fixTrans = __instance.GetComponent<FixTransform>();
                return false;
            }
        }
        [HarmonyPatch(typeof(InputUpdater))]
        public class InputUpdaterPatches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug(InputUpdater __instance)
            {
                Plugin.playerInputs.Add(__instance.gameObject.GetComponent<PlayerInput>());
            }
        }
        [HarmonyPatch(typeof(CursorUpdater))]
        public class CursorUpdaterPatches
        {
            [HarmonyPatch("initialize")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(CursorUpdater __instance)
            {
                if (__instance.alwaysHideCursorInstead)
                {
                    Cursor.SetCursor(null, Vector2.one / 2f, CursorMode.Auto);
                    Cursor.visible = false;
                    return false;
                }
                if (!Cursor.visible)
                {
                    Cursor.visible = true;
                }
                //clean up the input list
                List<PlayerInput> inputs = new();
                foreach (var input in Plugin.playerInputs)
                {
                    if (input != null)
                    {
                        inputs.Add(input);
                    }
                }
                Plugin.playerInputs = inputs;
                PlayerInput[] array = inputs.ToArray();
                if (array.Length == 0)
                {
                    __instance.SetCursor(__instance.defaultCursorColor);
                    return false;
                }
                for (int i = 0; i < array.Length; i++)
                {
                    if (array[i].currentControlScheme.Equals("KeyboardAndMouse"))
                    {
                        int claimerId = array[i].GetComponent<InputUpdater>().GetClaimerId();
                        Player player = PlayerHandler.Get().GetPlayer(claimerId);
                        if (player != null)
                        {
                            UnityEngine.Color color = player.Color.GetColor("_ShadowColor");
                            __instance.SetCursor(color);
                        }
                        return false;
                    }
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(MenuAbilitySelector))]
        public class MenuAbilitySelectorPatches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPrefix]
            private static bool Awake_MapMaker_Plug(MenuAbilitySelector __instance)
            {
                __instance.mgas = __instance.GetComponent<MidGameAbilitySelect>();
                List<Player> list = new List<Player>();
                list.Add(new Player(1, 1));
                PlayerHandler.Get().SetPlayerList(list);
                List<GameObject> list2 = new List<GameObject>();
                list2.Add(__instance.mgas.AbilityIcons.sprites[0].associatedGameObject);
                list2.Add(__instance.mgas.AbilityIcons.sprites[1].associatedGameObject);
                list2.Add(__instance.mgas.AbilityIcons.sprites[2].associatedGameObject);
                list[0].Abilities = list2;
                List<Sprite> list3 = new List<Sprite>();
                list3.Add(__instance.mgas.AbilityIcons.sprites[0].sprite);
                list3.Add(__instance.mgas.AbilityIcons.sprites[1].sprite);
                list3.Add(__instance.mgas.AbilityIcons.sprites[2].sprite);
                list[0].AbilityIcons = list3;
                __instance.mgas.SetPlayer(1);
                __instance.playerId = 1;
                //clean up the input list
                List<PlayerInput> inputs = new();
                foreach (var input in Plugin.playerInputs)
                {
                    if (input != null)
                    {
                        inputs.Add(input);
                    }
                }
                Plugin.playerInputs = inputs;
                //get the InputUpdaters 
                List<InputUpdater> updaters = new();
                foreach (var input in inputs)
                {
                    updaters.Add(input.gameObject.GetComponent<InputUpdater>());
                }
                InputUpdater[] array = updaters.ToArray();
                //InputUpdater[] array = UnityEngine.Object.FindObjectsOfType<InputUpdater>();
                int num = 0;
                while (num < array.Length && num < list.Count)
                {
                    array[num].Init(list[num].Id);
                    num++;
                }
                return false;
            }
        }
        [HarmonyPatch(typeof(SlimeController))]
        public class SlimeControllerPatches
        {
            [HarmonyPatch("Awake")]
            [HarmonyPrefix]
            private static void Awake_MapMaker_Plug(SlimeController __instance)
            {
                LuaMain.players.Add(__instance.GetComponent<PlayerPhysics>());
            }
        }
        [HarmonyPatch(typeof(DetPhysics))]
        public class DetPhysicsPatches
        {
            [HarmonyPatch("SyncGameObject")]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug(DetPhysics __instance, IShape shape, ref PhysicsParent pp, ref PhysicsBody body)
            {
                if (pp.fixTrans.gameObject.name == "Player(Clone)")
                {
                    //Debug.Log($"SyncGameObject set players pos to x: {pp.fixTrans.position.x} and y {pp.fixTrans.position.y} at time {Updater.SimTimeSinceLevelLoaded}");
                }
            }
        }
        [HarmonyPatch(typeof(DPhysicsBox))]
        public class DPhysicsBoxPatches
        {
            [HarmonyPatch("position", MethodType.Setter)]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug(Vec2 value, DPhysicsBox __instance)
            {
                if (__instance.pp.fixTrans.gameObject.name == "Player(Clone)")
                {
                    //Debug.Log($"DPhysicsBox set players pos to x: {__instance.pp.fixTrans.position.x} and y {__instance.pp.fixTrans.position.y} stack trace: {UnityEngine.StackTraceUtility.ExtractStackTrace()} at time {Updater.SimTimeSinceLevelLoaded}");
                }
            }
        }
        [HarmonyPatch(typeof(DPhysicsCircle))]
        public class DPhysicsCirclePatches
        {
            [HarmonyPatch("position", MethodType.Setter)]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug(Vec2 value, DPhysicsCircle __instance)
            {
                if (__instance.pp.fixTrans.gameObject.name == "Player(Clone)")
                {
                    //Debug.Log($"DPhysicsCircle set players pos to x: {__instance.pp.fixTrans.position.x} and y {__instance.pp.fixTrans.position.y} stack trace: {UnityEngine.StackTraceUtility.ExtractStackTrace()} at time {Updater.SimTimeSinceLevelLoaded}");
                }
            }
        }
        [HarmonyPatch(typeof(PlayerBody))]
        public class PlayerBodyPatches
        {
            [HarmonyPatch("position", MethodType.Setter)]
            [HarmonyPostfix]
            private static void Awake_MapMaker_Plug(Vec2 value, PlayerBody __instance)
            {
                if (__instance.fixTransform.gameObject.name == "Player(Clone)")
                {
                    //Debug.Log($"PlayerBody set players pos to x: {__instance.fixTransform.position.x} and y {__instance.fixTransform.position.y} stack trace: {UnityEngine.StackTraceUtility.ExtractStackTrace()} at time {Updater.SimTimeSinceLevelLoaded}");
                }
            }
        }
        [HarmonyPatch(typeof(NetworkTools))]
        public class NetworkToolsPatches
        {
            private static StartRequestPacket ReadStartRequestReplay(byte[] data, ref byte[] uintConversionHelperArray, ref byte[] ulongConversionHelperArray, ref byte[] ushortConversionHelperArray)
            {
                //edited chatgpt code (tryed myself for a while but eventualy just asked chatgpt to fix my code lol)
                // Extract the ZIP size (first 4 bytes)
                byte[] zipSizeBytes = new byte[4];
                Array.Copy(data, 0, zipSizeBytes, 0, 4);
                int zipSize = BitConverter.ToInt32(zipSizeBytes, 0);

                // Extract the ZIP archive bytes
                byte[] zipArchiveBytes = new byte[zipSize];
                Array.Copy(data, 4, zipArchiveBytes, 0, zipSize);

                // Create a memory stream for the ZIP archive
                var memoryStream = new MemoryStream(zipArchiveBytes);
                var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Read);
                // Replace the plugin's zip archive with the loaded one
                Plugin.zipArchives = new[] { zipArchive };
                Plugin.CurrentMapIndex = 0;
                //my code
                var __result = default(StartRequestPacket);
                int num = zipSize + 4;
                //game code
                ushortConversionHelperArray[0] = data[num++];
                ushortConversionHelperArray[1] = data[num++];
                __result.seqNum = NetworkTools.SwapBytesIfLittleEndian(BitConverter.ToUInt16(ushortConversionHelperArray, 0));
                uintConversionHelperArray[0] = data[num++];
                uintConversionHelperArray[1] = data[num++];
                uintConversionHelperArray[2] = data[num++];
                uintConversionHelperArray[3] = data[num++];
                __result.seed = NetworkTools.SwapBytesIfLittleEndian(BitConverter.ToUInt32(uintConversionHelperArray, 0));
                __result.nrOfPlayers = data[num++];
                __result.nrOfAbilites = data[num++];
                __result.currentLevel = data[num++];
                __result.frameBufferSize = data[num++];
                __result.isDemoMask = data[num++];
                ulongConversionHelperArray[0] = data[num++];
                ulongConversionHelperArray[1] = data[num++];
                ulongConversionHelperArray[2] = data[num++];
                ulongConversionHelperArray[3] = data[num++];
                ulongConversionHelperArray[4] = data[num++];
                ulongConversionHelperArray[5] = data[num++];
                ulongConversionHelperArray[6] = data[num++];
                ulongConversionHelperArray[7] = data[num++];
                __result.p1_id = NetworkTools.SwapBytesIfLittleEndian(BitConverter.ToUInt64(ulongConversionHelperArray, 0));
                ulongConversionHelperArray[0] = data[num++];
                ulongConversionHelperArray[1] = data[num++];
                ulongConversionHelperArray[2] = data[num++];
                ulongConversionHelperArray[3] = data[num++];
                ulongConversionHelperArray[4] = data[num++];
                ulongConversionHelperArray[5] = data[num++];
                ulongConversionHelperArray[6] = data[num++];
                ulongConversionHelperArray[7] = data[num++];
                __result.p2_id = NetworkTools.SwapBytesIfLittleEndian(BitConverter.ToUInt64(ulongConversionHelperArray, 0));
                ulongConversionHelperArray[0] = data[num++];
                ulongConversionHelperArray[1] = data[num++];
                ulongConversionHelperArray[2] = data[num++];
                ulongConversionHelperArray[3] = data[num++];
                ulongConversionHelperArray[4] = data[num++];
                ulongConversionHelperArray[5] = data[num++];
                ulongConversionHelperArray[6] = data[num++];
                ulongConversionHelperArray[7] = data[num++];
                __result.p3_id = NetworkTools.SwapBytesIfLittleEndian(BitConverter.ToUInt64(ulongConversionHelperArray, 0));
                ulongConversionHelperArray[0] = data[num++];
                ulongConversionHelperArray[1] = data[num++];
                ulongConversionHelperArray[2] = data[num++];
                ulongConversionHelperArray[3] = data[num++];
                ulongConversionHelperArray[4] = data[num++];
                ulongConversionHelperArray[5] = data[num++];
                ulongConversionHelperArray[6] = data[num++];
                ulongConversionHelperArray[7] = data[num++];
                __result.p4_id = NetworkTools.SwapBytesIfLittleEndian(BitConverter.ToUInt64(ulongConversionHelperArray, 0));
                __result.p1_color = data[num++];
                __result.p2_color = data[num++];
                __result.p3_color = data[num++];
                __result.p4_color = data[num++];
                __result.p1_team = data[num++];
                __result.p2_team = data[num++];
                __result.p3_team = data[num++];
                __result.p4_team = data[num++];
                __result.p1_ability1 = data[num++];
                __result.p1_ability2 = data[num++];
                __result.p1_ability3 = data[num++];
                __result.p2_ability1 = data[num++];
                __result.p2_ability2 = data[num++];
                __result.p2_ability3 = data[num++];
                __result.p3_ability1 = data[num++];
                __result.p3_ability2 = data[num++];
                __result.p3_ability3 = data[num++];
                __result.p4_ability1 = data[num++];
                __result.p4_ability2 = data[num++];
                __result.p4_ability3 = data[num++];
                return __result;
            }
            //this isnt a patch but is part of one so im putting it here
            public static int GetReplayDataOffset(byte[] data)
            {
                byte[] ZipSizeBytes = { data[0], data[1], data[2], data[3] };
                var ZipSize = BitConverter.ToInt32(ZipSizeBytes, 0);
                return ZipSize + 4 + 105;
            }
            [HarmonyPatch("ReadCompressedReplay")]
            [HarmonyPrefix]
            private static bool ReadCompressedReplay(byte[] compressedReplay, out StartRequestPacket startRequest)
            {
                //go to the postfix
                startRequest = default(StartRequestPacket);
                return false;
            }
            [HarmonyPatch("ReadCompressedReplay")]
            [HarmonyPostfix]
            private static void ReadCompressedReplay2(byte[] compressedReplay, out StartRequestPacket startRequest, ref Queue<InputPacketQuad> __result)
            {
                byte[] array = new byte[2];
                byte[] array2 = new byte[4];
                byte[] array3 = new byte[8];
                List<InputPacketQuad> list = new List<InputPacketQuad>();
                startRequest = ReadStartRequestReplay(compressedReplay, ref array2, ref array3, ref array);
                int i = GetReplayDataOffset(compressedReplay);
                InputPacketQuad inputPacketQuad = default(InputPacketQuad);
                int num = 0;
                while (i < compressedReplay.Length)
                {
                    for (int j = 0; j < 32; j++)
                    {
                        InputPacketQuad item = inputPacketQuad;
                        list.Add(item);
                    }
                    i = NetworkTools.read32InputPacketsForPlayer1(list, inputPacketQuad, compressedReplay, i, ref array2);
                    i = NetworkTools.read32InputPacketsForPlayer2(list, inputPacketQuad, compressedReplay, i, ref array2);
                    i = NetworkTools.read32InputPacketsForPlayer3(list, inputPacketQuad, compressedReplay, i, ref array2);
                    i = NetworkTools.read32InputPacketsForPlayer4(list, inputPacketQuad, compressedReplay, i, ref array2);
                    num += 32;
                    inputPacketQuad = list[num - 1];
                }
                for (int k = 0; k < list.Count; k++)
                {
                    InputPacketQuad value = list[k];
                    value.p1.seqNumber = (uint)(k + 1);
                    value.p2.seqNumber = (uint)(k + 1);
                    value.p3.seqNumber = (uint)(k + 1);
                    value.p4.seqNumber = (uint)(k + 1);
                    list[k] = value;
                }
                Queue<InputPacketQuad> queue = new Queue<InputPacketQuad>();
                for (int l = 0; l < list.Count; l++)
                {
                    queue.Enqueue(list[l]);
                }
                __result = queue;
            }
        }


    }
}
