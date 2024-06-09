using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BoplFixedMath;
using UnityEngine;

namespace MapMaker
{
    internal class DisappearPlatformsOnSignal : MonoUpdatable
    {
        public GameObject platform = null;
        public ushort Signal = 0;
        public bool SignalIsInverse = false;
        //if true it only is gone when the signal/inverse signal is on and if its not it imeaditly comes back. if false it takes SecondsToReapper seconds to reapear.
        public bool DisappearOnlyWhenSignal = true;
        public Fix SecondsToReapper = Fix.One;
        // delay before it disapers. gives a little bit of a warning if you want to.
        public Fix delay = Fix.One;
        //if true it only disapers when the signal first turns on. then after SecondsToReapper seconds it reapers even if the signal is still on.
        //if you try to activate the signal agien while its still reappering then it doesnt change the timer.
        public bool OnlyDisappearWhenSignalTurnsOn = false;
        //prefabs set in awake in the plugin.cs
        public static Material onHitResizableWallMaterail;
        public static Material onHitWallMaterail;
        public static QuantumTunnel QuantumTunnelPrefab;
        public QuantumTunnel currentTunnel = null;
        private Fix TimeDelayed;
        private Fix age;
        private bool delaying = false;
        private Material originalMaterial;
        private bool update1 = true;
        public void Awake()
        {
            Updater.RegisterUpdatable(this);
        }
        public override void Init()
        {

        }

        public override void UpdateSim(Fix SimDeltaTime)
        {


            if (!(gameObject.name == "DisappearPlatformsObject"))
            {
                //update1
                if (update1)
                {
                    originalMaterial = platform.GetComponent<SpriteRenderer>().material;
                    update1 = false;
                }
                var IsSignalOn = SignalSystem.IsSignalOn(Signal);
                var IsOn = (IsSignalOn && !SignalIsInverse || !IsSignalOn && SignalIsInverse);
                //if its being activated and its not being delayed already and its not already disapered
                if (IsOn && !delaying && platform.activeInHierarchy)
                {
                    delaying = true;
                }
                //if its time to reapper
                if (age - delay > SecondsToReapper)
                {
                    platform.SetActive(true);
                    TimeDelayed = Fix.Zero;
                    age = Fix.Zero;
                    platform.GetComponent<SpriteRenderer>().material = originalMaterial;
                }
                //if its time to reapper
                if (age - delay > Fix.Zero && DisappearOnlyWhenSignal)
                {
                    platform.SetActive(true);
                    TimeDelayed = Fix.Zero;
                    age = Fix.Zero;
                    platform.GetComponent<SpriteRenderer>().material = originalMaterial;
                }
                if (!IsOn || OnlyDisappearWhenSignalTurnsOn)
                {
                    age += GameTime.PlayerTimeScale * SimDeltaTime;
                }

                //if we are in the delay stage
                if (delaying)
                {
                    UnityEngine.Debug.Log("delaying");
                    TimeDelayed += GameTime.PlayerTimeScale * SimDeltaTime;
                    UnityEngine.Debug.Log("TimeDelayed: " + TimeDelayed);
                    if (platform.CompareTag("ResizablePlatform"))
                    {
                        Material material = onHitResizableWallMaterail;
                        platform.GetComponent<SpriteRenderer>();
                        DPhysicsRoundedRect component2 = platform.GetComponent<DPhysicsRoundedRect>();
                        material.SetFloat("_Scale", platform.transform.localScale.x);
                        material.SetFloat("_BevelRadius", (float)component2.radius);
                        Vec2 vec2 = component2.CalcExtents();
                        material.SetFloat("_RHeight", (float)vec2.y);
                        material.SetFloat("_RWidth", (float)vec2.x);
                        platform.GetComponent<SpriteRenderer>().material = material;
                    }
                    else
                    {
                        platform.GetComponent<SpriteRenderer>().material = onHitWallMaterail;
                    }
                }


                //if the delay is done
                if (TimeDelayed > delay)
                {
                    //if this isnt a OnlyDisappearWhenSignalTurnsOn one.
                    if (!OnlyDisappearWhenSignalTurnsOn)
                    {
                        UnityEngine.Debug.Log("making platform disaper.");
                        if (!platform)
                        {
                            UnityEngine.Debug.Log("GAME OBJECT IS NULL!");
                        }
                        delaying = false;
                        platform.SetActive(false);
                    }
                }
            }
        }
    }
}
