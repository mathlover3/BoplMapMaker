using BoplFixedMath;
using System.Collections.Generic;
using UnityEngine;

// Token: 0x0200007B RID: 123
//literaly just a copy paste from dnspy so that i can remove all of the attachments to being a ability or having a player that created it.
public class ShootBlink : MonoBehaviour
{
    // Token: 0x06000469 RID: 1129 RVA: 0x0002FB38 File Offset: 0x0002DD38
    private void Awake()
    {
        this.rayParticle = UnityEngine.Object.Instantiate<ParticleSystem>(ShootBlink.RaycastParticlePrefab);
        this.hitParticle = UnityEngine.Object.Instantiate<ParticleSystem>(ShootBlink.RaycastParticleHitPrefab);
        this.rayParticle2 = UnityEngine.Object.Instantiate<ParticleSystem>(ShootBlink.RaycastParticlePrefab);
        this.rayDensity = this.rayParticle.emission.GetBurst(0).count.constant / this.rayParticle.shape.scale.x;
    }

    // Token: 0x0600046A RID: 1130 RVA: 0x0002FBD8 File Offset: 0x0002DDD8
    public void Shoot(Vec2 firepointFIX, Vec2 directionFIX, ref bool hasFired, bool alreadyHitWater = false)
    {
        Vec2 vec = directionFIX;
        AudioManager.Get().Play("laserShoot");
        Debug.DrawRay((Vector2)firepointFIX, (float)this.maxDistance * (Vector2)vec, new Color(255f, 255f, 0f));
        RaycastInformation raycastInformation = DetPhysics.Get().PointCheckAllRoundedRects(firepointFIX);
        if (!raycastInformation)
        {
            raycastInformation = DetPhysics.Get().RaycastToClosest(firepointFIX, vec, this.maxDistance, ShootBlink.collisionMask);
        }
        if (!raycastInformation && firepointFIX.y <= SceneBounds.WaterHeight && !alreadyHitWater)
        {
            this.spawnRayCastEffect((Vector3)firepointFIX, (Vector3)vec, (float)raycastInformation.nearDist, false, Vec2.up, true);
            this.rayParticle.Stop();
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(ShootBlink.WaterRing);
            AudioManager.Get().Play("waterExplosion");
            gameObject.transform.position = new Vector3(ShootBlink.WaterRing.transform.position.x + (float)raycastInformation.nearPos.x, ShootBlink.WaterRing.transform.position.y, ShootBlink.WaterRing.transform.position.z);
            return;
        }
        if (raycastInformation)
        {
            Vec2 normal = Vec2.NormalizedSafe(raycastInformation.pp.monobehaviourCollider.NormalAtPoint(raycastInformation.nearPos));
            if (raycastInformation.layer == LayerMask.NameToLayer("Water") && !alreadyHitWater)
            {
                this.spawnRayCastEffect((Vector3)firepointFIX, (Vector3)vec, (float)raycastInformation.nearDist, false, normal, true);
                this.rayParticle.Stop();
                vec = new Vec2(vec.x, vec.y * -Fix.One);
                this.Shoot(raycastInformation.nearPos, vec, ref hasFired, true);
                Debug.DrawRay((Vector2)raycastInformation.nearPos, (Vector2)(vec * this.maxDistance), Color.magenta);
                GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(ShootBlink.WaterRing);
                AudioManager.Get().Play("waterExplosion");
                gameObject2.transform.position = new Vector3(ShootBlink.WaterRing.transform.position.x + (float)raycastInformation.nearPos.x, ShootBlink.WaterRing.transform.position.y, ShootBlink.WaterRing.transform.position.z);
                return;
            }
            if (raycastInformation.layer == LayerMask.NameToLayer("EffectorZone") || raycastInformation.layer == LayerMask.NameToLayer("weapon"))
            {
                GameObject gameObject3 = raycastInformation.pp.fixTrans.gameObject;
                QuantumTunnel quantumTunnel = FixTransform.InstantiateFixed<QuantumTunnel>(ShootBlink.QuantumTunnelPrefab, raycastInformation.pp.fixTrans.position);
                if (!raycastInformation.pp.fixTrans.gameObject.CompareTag("InvincibilityZone"))
                {
                    quantumTunnel.Init(gameObject3, this.WallDuration, null, false);
                }
            }
            else
            {
                GameObject gameObject4 = raycastInformation.pp.fixTrans.gameObject;
                QuantumTunnel quantumTunnel2 = null;
                for (int i = 0; i < ShootQuantum.spawnedQuantumTunnels.Count; i++)
                {
                    if (ShootQuantum.spawnedQuantumTunnels[i].Victim.GetInstanceID() == gameObject4.GetInstanceID())
                    {
                        quantumTunnel2 = ShootQuantum.spawnedQuantumTunnels[i];
                    }
                }
                if (quantumTunnel2 == null)
                {
                    quantumTunnel2 = FixTransform.InstantiateFixed<QuantumTunnel>(ShootBlink.QuantumTunnelPrefab, raycastInformation.pp.fixTrans.position);
                    ShootQuantum.spawnedQuantumTunnels.Add(quantumTunnel2);
                }
                if (gameObject4.layer == LayerMask.NameToLayer("wall"))
                {
                    ShakablePlatform component = gameObject4.GetComponent<ShakablePlatform>();
                    if (gameObject4.CompareTag("ResizablePlatform"))
                    {
                        Material material = ShootBlink.onHitResizableWallMaterail;
                        gameObject4.GetComponent<SpriteRenderer>();
                        DPhysicsRoundedRect component2 = gameObject4.GetComponent<DPhysicsRoundedRect>();
                        material.SetFloat("_Scale", gameObject4.transform.localScale.x);
                        material.SetFloat("_BevelRadius", (float)component2.radius);
                        Vec2 vec2 = component2.CalcExtents();
                        material.SetFloat("_RHeight", (float)vec2.y);
                        material.SetFloat("_RWidth", (float)vec2.x);
                        component.AddShake(this.WallDelay, this.WallShake, 10, material, null);
                        quantumTunnel2.DelayedInit(gameObject4, this.WallDuration, this.WallDelay, ShootBlink.onDissapearResizableWallMaterail);
                    }
                    else
                    {
                        component.AddShake(this.WallDelay, this.WallShake, 10, ShootBlink.onHitWallMaterail, null);
                        quantumTunnel2.DelayedInit(gameObject4, this.WallDuration, this.WallDelay, null);
                    }
                }
                else if (gameObject4.layer == LayerMask.NameToLayer("RigidBodyAffector"))
                {
                    if (gameObject4.GetComponent<BlackHole>() != null)
                    {
                        quantumTunnel2.Init(gameObject4, this.WallDuration, ShootBlink.onHitBlackHoleMaterial, false);
                    }
                    else
                    {
                        quantumTunnel2.Init(gameObject4, this.WallDuration, null, false);
                    }
                }
                else if (gameObject4.layer == LayerMask.NameToLayer("Player"))
                {
                    IPlayerIdHolder component3 = gameObject4.GetComponent<IPlayerIdHolder>();
                    Player player = PlayerHandler.Get().GetPlayer(component3.GetPlayerId());
                    if (player != null)
                    {
                        int timesHitByBlinkgunThisRound = player.timesHitByBlinkgunThisRound;
                        Player player2 = player;
                        int timesHitByBlinkgunThisRound2 = player2.timesHitByBlinkgunThisRound;
                        player2.timesHitByBlinkgunThisRound = timesHitByBlinkgunThisRound2 + 1;
                    }
                    Fix lifeSpan = Fix.Max(this.minPlayerDuration, (Fix)0.3 * this.WallDuration);
                    quantumTunnel2.Init(gameObject4, lifeSpan, null, true);
                }
                else
                {
                    quantumTunnel2.Init(gameObject4, this.WallDuration, null, false);
                }
            }
            this.spawnRayCastEffect((Vector2)firepointFIX, (Vector2)vec, (float)raycastInformation.nearDist, true, normal, false);
        }
        else
        {
            this.spawnRayCastEffect((Vector2)firepointFIX, (Vector2)vec, (float)this.maxDistance, false, Vec2.up, false);
        }
        hasFired = true;
    }

    // Token: 0x0600046B RID: 1131 RVA: 0x00030280 File Offset: 0x0002E480
    private void spawnRayCastEffect(Vector2 start, Vector2 dir, float dist, bool didHit, Vec2 normal, bool reflected = false)
    {
        ParticleSystem particleSystem = reflected ? this.rayParticle2 : this.rayParticle;
        ParticleSystem.ShapeModule shape = particleSystem.shape;
        ParticleSystem.EmissionModule emission = particleSystem.emission;
        ParticleSystem.Burst burst = emission.GetBurst(0);
        shape.scale = new Vector3(dist, shape.scale.y, shape.scale.z);
        particleSystem.transform.right = dir;
        particleSystem.transform.position = start + dir * dist * 0.5f;
        burst.count = dist * this.rayDensity;
        emission.SetBurst(0, burst);
        particleSystem.Play();
        if (reflected)
        {
            this.hitParticle.transform.position = start + dir * dist;
            this.hitParticle.transform.up = (Vector3)normal;
            this.hitParticle.Simulate(0.16f);
            this.hitParticle.Play();
        }
        if (didHit)
        {
            this.hitParticle.Stop();
            this.hitParticle.transform.position = start + dir * dist;
            this.hitParticle.transform.up = (Vector3)normal;
            this.hitParticle.Play();
        }
    }

    // Token: 0x0400054B RID: 1355
    private Fix maxDistance = (Fix)100L;

    // Token: 0x0400054C RID: 1356
    public static LayerMask collisionMask;

    // Token: 0x0400054D RID: 1357
    public static float raycastEffectSpacing;

    // Token: 0x0400054E RID: 1358
    public static GameObject WaterExplosion;

    // Token: 0x0400054F RID: 1359
    public static GameObject WaterRing;

    // Token: 0x04000550 RID: 1360
    public static GameObject RayCastEffect;

    // Token: 0x04000551 RID: 1361
    public static QuantumTunnel QuantumTunnelPrefab;

    // Token: 0x04000552 RID: 1362
    public static ParticleSystem RaycastParticlePrefab;

    // Token: 0x04000553 RID: 1363
    public static ParticleSystem RaycastParticleHitPrefab;

    // Token: 0x04000554 RID: 1364
    public Fix minPlayerDuration = (Fix)0.5;

    // Token: 0x04000555 RID: 1365
    public Fix WallDuration = (Fix)3L;

    // Token: 0x04000556 RID: 1366
    public Fix WallDelay = (Fix)1f;

    // Token: 0x04000557 RID: 1367
    public Fix WallShake = (Fix)1f;

    // Token: 0x04000558 RID: 1368
    public static Material onHitWallMaterail;

    // Token: 0x04000559 RID: 1369
    public static Material onHitResizableWallMaterail;

    // Token: 0x0400055A RID: 1370
    public static Material onDissapearResizableWallMaterail;

    // Token: 0x0400055B RID: 1371
    public static Material onHitBlackHoleMaterial;

    // Token: 0x0400055D RID: 1373
    private ParticleSystem rayParticle;

    // Token: 0x0400055E RID: 1374
    private ParticleSystem hitParticle;

    // Token: 0x0400055F RID: 1375
    private ParticleSystem rayParticle2;

    // Token: 0x04000560 RID: 1376
    private float rayDensity;
}
