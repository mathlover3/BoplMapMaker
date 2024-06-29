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
    public class DisappearPlatformsOnSignal : LogicGate
    {
        //used so that the blink gun knows when its about to reapear if it should do its reapear animatson or not.
        public static List<DisappearPlatformsOnSignal> DisappearPlatformsOnSignals = new();
        public GameObject platform = null;
        public bool SignalIsInverse = false;
        public Fix SecondsToReapper = Fix.One;
        // delay before it disapers. gives a little bit of a warning if you want to.
        public Fix delay = Fix.One;
        //prefabs set in awake in the plugin.cs
        public static Material onHitResizableWallMaterail;
        public static Material onHitWallMaterail;
        public static QuantumTunnel QuantumTunnelPrefab;
        public QuantumTunnel currentTunnel = null;
        //public so that the blink patch can acsess it
        public Fix TimeDelayed;
        private Fix age;
        private bool delaying = false;
        private Material originalMaterial;
        private bool update1 = true;
        private bool ShouldBeActive = true;
        public void Register()
        {
            UUID = Plugin.NextUUID;
            Plugin.NextUUID++;
            DisappearPlatformsOnSignals.Add(this);
            SignalSystem.RegisterLogicGate(this);
            //must always run so that it can do its logic
            SignalSystem.RegisterGateThatAlwaysRuns(this);
            //should update its connectson line so it always points to the platform
            SignalSystem.RegisterInputThatUpdatesConnectson(InputSignals[0]);
        }
        public bool IsOn()
        {
            return InputSignals[0].IsOn;
        }

        public override void Logic(Fix SimDeltaTime)
        {
            //platform is null if its been eaten by a black hole
            if (!(gameObject.name == "DisappearPlatformsObject") && platform != null)
            {
                //update1
                if (update1)
                {
                    originalMaterial = platform.GetComponent<SpriteRenderer>().material;
                    update1 = false;
                }
                QuantumTunnel quantumTunnel = null;
                for (int i = 0; i < ShootQuantum.spawnedQuantumTunnels.Count; i++)
                {
                    if (ShootQuantum.spawnedQuantumTunnels[i].Victim.GetInstanceID() == platform.GetInstanceID())
                    {
                        quantumTunnel = ShootQuantum.spawnedQuantumTunnels[i];
                    }
                }
                var IsOnReal = (IsOn() && !SignalIsInverse || !IsOn() && SignalIsInverse);
                //if its being activated and its not being delayed already and its not already disapered by us
                if (IsOnReal && !delaying && ShouldBeActive)
                {
                    delaying = true;
                }
                //if its time to reapper
                if (age - delay > SecondsToReapper && !quantumTunnel)
                {
                    ShouldBeActive = true;
                    platform.SetActive(true);
                    TimeDelayed = Fix.Zero;
                    age = Fix.Zero;
                    platform.GetComponent<SpriteRenderer>().material = originalMaterial;
                }
                //if its time to reapper
                if (age > SecondsToReapper && !quantumTunnel)
                {
                    ShouldBeActive = true;
                    platform.SetActive(true);
                    TimeDelayed = Fix.Zero;
                    age = Fix.Zero;
                    platform.GetComponent<SpriteRenderer>().material = originalMaterial;
                }
                if (!IsOnReal)
                {
                    age += GameTime.PlayerTimeScale * SimDeltaTime;
                }

                //if we are in the delay stage
                if (delaying)
                {
                    //UnityEngine.Debug.Log("delaying");
                    TimeDelayed += GameTime.PlayerTimeScale * SimDeltaTime;
                    //UnityEngine.Debug.Log("TimeDelayed: " + TimeDelayed);
                    age = Fix.Zero;
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


                //if the delay is done and theres not a blink ray removing it.
                if (TimeDelayed > delay && !quantumTunnel)
                {
                    //UnityEngine.Debug.Log("making platform disaper.");
                    if (!platform)
                    {
                        UnityEngine.Debug.Log("GAME OBJECT IS NULL!");
                    }
                    delaying = false;
                    ShouldBeActive = false;
                    platform.SetActive(false);
                }
            }
        }
    }
}
