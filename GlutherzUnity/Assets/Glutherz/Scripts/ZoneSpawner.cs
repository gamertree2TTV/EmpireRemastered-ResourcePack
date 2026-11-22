using System.Collections.Generic;
using UnityEngine;

namespace Glutherz
{
    /// <summary>Hält ein Gebiet bevölkert: normale Gegner und den Boss vor dem Siegel.</summary>
    public class ZoneSpawner : MonoBehaviour
    {
        public int zone;
        const int MaxMobs = 7;
        readonly List<Enemy> mobs = new List<Enemy>();
        Enemy bossRef;
        float mobTimer, bossTimer;

        void Start()
        {
            for (int i = 0; i < MaxMobs; i++) SpawnMob(false);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (mobs.Count < MaxMobs)
            {
                mobTimer -= dt;
                if (mobTimer <= 0f) { SpawnMob(true); mobTimer = 6f; }
            }
            if (bossRef == null)
            {
                bossTimer -= dt;
                if (bossTimer <= 0f)
                {
                    float z = Db.ZoneStart(zone) + Db.ZoneLen - 12f;
                    bossRef = Enemy.Create(zone, true, new Vector3(0, 0.1f, z), this);
                    bossRef.transform.rotation = Quaternion.Euler(0, 180, 0);
                }
            }
        }

        void SpawnMob(bool awayFromPlayer)
        {
            float z0 = Db.ZoneStart(zone);
            for (int attempt = 0; attempt < 8; attempt++)
            {
                var pos = new Vector3(Random.Range(-28f, 28f), 0.1f, Random.Range(z0 + 10f, z0 + Db.ZoneLen - 24f));
                var p = PlayerController.I;
                if (awayFromPlayer && p != null && (p.transform.position - pos).magnitude < 20f) continue;
                mobs.Add(Enemy.Create(zone, false, pos, this));
                return;
            }
        }

        public void OnDeath(Enemy e)
        {
            if (e == bossRef) { bossRef = null; bossTimer = 45f; }
            else mobs.Remove(e);
        }
    }
}
