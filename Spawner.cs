using BoplFixedMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace MapMaker
{
    internal class Spawner : MonoUpdatable
    {
        public ObjectSpawnType spawnType = ObjectSpawnType.None;
        public Fix SimTimeBetweenSpawns = Fix.One;
        public Fix scale = Fix.One;
        public Color color = Color.white;
        public Vec2 velocity = new Vec2(Fix.Zero, Fix.Zero);
        public PlatformType BoulderType = PlatformType.grass;
        private FixTransform fixTransform;
        private Fix RelitiveSimTime;
        public bool UseSignal = false;
        //up to 256 signals.
        public byte Signal = 0;
        public void Awake()
        {
            UnityEngine.Debug.Log("Spawner Awake");
            Updater.RegisterUpdatable(this);
            fixTransform = (FixTransform)this.GetComponent(typeof(FixTransform));
        }
        public override void Init()
        {
        }

        public override void UpdateSim(Fix SimDeltaTime)
        {
            RelitiveSimTime = RelitiveSimTime + SimDeltaTime;
            if (RelitiveSimTime > SimTimeBetweenSpawns)
            {
                RelitiveSimTime = RelitiveSimTime - SimTimeBetweenSpawns;
                Vec2 pos = fixTransform.position;
                switch (spawnType)
                {
                    case ObjectSpawnType.Boulder:
                        var boulder = PlatformApi.PlatformApi.SpawnBoulder(pos, scale, BoulderType, color);

                        break;
                }
            }
        }
        public enum ObjectSpawnType
        {
            None,
            Boulder,
            Arrow,
            Grenade,
            AbilityOrb,
            Mine,
            Smoke,
            Gust,
            Explosion
        }
    }
}
