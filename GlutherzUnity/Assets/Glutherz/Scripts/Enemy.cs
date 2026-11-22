using UnityEngine;

namespace Glutherz
{
    /// <summary>Gegner: wandert umher, verfolgt den Spieler im eigenen Gebiet und greift mit kurzer Ausholbewegung an.</summary>
    public class Enemy : MonoBehaviour, IDamageable
    {
        public int zone;
        public bool boss;
        public ZoneSpawner owner;

        string title;
        int maxHp, hp, atk, def;
        float scale, speed, atkTimer, windup = -1f, flash, wanderTimer;
        Vector3 home, wanderDir;
        CharacterController cc;
        Transform model;
        Renderer body;
        Color baseColor;
        bool dead;

        public string Title { get { return title; } }
        public int Defense { get { return def; } }
        public float HpFraction { get { return Mathf.Clamp01(hp / (float)maxHp); } }

        public static Enemy Create(int zone, bool boss, Vector3 pos, ZoneSpawner owner)
        {
            int kind = Random.Range(0, 2);
            var go = new GameObject(boss ? "Boss" : "Gegner");
            go.transform.position = pos;
            var e = go.AddComponent<Enemy>();
            e.zone = zone; e.boss = boss; e.owner = owner; e.home = pos;
            e.title = boss ? Db.BossName[zone] : Db.MobName[zone][kind];
            e.scale = boss ? 2.3f : Random.Range(0.9f, 1.15f);
            float v = Random.Range(0.85f, 1.15f);
            e.maxHp = e.hp = Mathf.RoundToInt(Db.ZHp[zone] * v * (boss ? 5f : 1f));
            e.atk = Mathf.RoundToInt(Db.ZAtk[zone] * (boss ? 1.6f : 1f));
            e.def = Mathf.RoundToInt(Db.ZDef[zone] * (boss ? 1.3f : 1f));
            e.speed = boss ? 3.2f : 3.6f + zone * 0.25f;
            e.baseColor = boss ? Db.MobColor[zone] * 0.6f : Db.MobColor[zone];
            e.baseColor.a = 1f;

            e.model = new GameObject("Model").transform;
            e.model.SetParent(go.transform, false);
            e.model.localScale = Vector3.one * e.scale;
            GameObject bodyGo;
            float eyeY;
            if (kind == 0 && !boss)
            {
                // gedrungener Vierbeiner / Schleim
                bodyGo = Prim.Make(PrimitiveType.Sphere, e.model, new Vector3(0, 0.7f, 0), new Vector3(1.3f, 1.2f, 1.6f), e.baseColor, false);
                eyeY = 0.95f;
            }
            else
            {
                bodyGo = Prim.Make(PrimitiveType.Capsule, e.model, new Vector3(0, 1f, 0), new Vector3(0.9f, 1f, 0.9f), e.baseColor, false);
                Prim.Make(PrimitiveType.Sphere, e.model, new Vector3(0, 2.05f, 0), Vector3.one * 0.75f, e.baseColor * 1.15f, false);
                Prim.Make(PrimitiveType.Cube, e.model, new Vector3(0.6f, 1.1f, 0.15f), new Vector3(0.25f, 0.9f, 0.25f), e.baseColor * 0.8f, false);
                Prim.Make(PrimitiveType.Cube, e.model, new Vector3(-0.6f, 1.1f, 0.15f), new Vector3(0.25f, 0.9f, 0.25f), e.baseColor * 0.8f, false);
                eyeY = 2.1f;
            }
            float eyeZ = kind == 0 && !boss ? 0.72f : 0.32f;
            Color eye = boss ? new Color(1f, 0.85f, 0.1f) : new Color(0.95f, 0.1f, 0.1f);
            Prim.Make(PrimitiveType.Sphere, e.model, new Vector3(0.18f, eyeY, eyeZ), Vector3.one * 0.14f, eye, false);
            Prim.Make(PrimitiveType.Sphere, e.model, new Vector3(-0.18f, eyeY, eyeZ), Vector3.one * 0.14f, eye, false);
            if (boss)
            {
                Color horn = new Color(0.9f, 0.85f, 0.7f);
                Prim.Make(PrimitiveType.Cylinder, e.model, new Vector3(0.3f, 2.5f, 0), new Vector3(0.12f, 0.3f, 0.12f), horn, false).transform.localRotation = Quaternion.Euler(0, 0, -25);
                Prim.Make(PrimitiveType.Cylinder, e.model, new Vector3(-0.3f, 2.5f, 0), new Vector3(0.12f, 0.3f, 0.12f), horn, false).transform.localRotation = Quaternion.Euler(0, 0, 25);
            }
            e.body = bodyGo.GetComponent<Renderer>();

            e.cc = go.AddComponent<CharacterController>();
            e.cc.height = 2f * e.scale;
            e.cc.radius = 0.55f * e.scale;
            e.cc.center = new Vector3(0, e.scale + 0.05f, 0);
            return e;
        }

        void Update()
        {
            var p = PlayerController.I;
            if (dead || p == null) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (flash > 0f)
            {
                flash -= dt;
                body.material.color = flash > 0f ? Color.white : baseColor;
            }

            Vector3 to = p.transform.position - transform.position;
            to.y = 0f;
            float dist = to.magnitude;
            float reach = 1.2f + 0.9f * scale;
            bool aggro = !p.Dead && p.Zone == zone && dist < (boss ? 26f : 16f);
            Vector3 move = Vector3.zero;
            atkTimer -= dt;

            if (windup >= 0f)
            {
                // Ausholen: der Gegner bläht sich kurz auf – Zeit zum Blocken oder Ausweichen.
                windup -= dt;
                model.localScale = Vector3.one * scale * (1f + 0.18f * Mathf.Sin((0.45f - windup) / 0.45f * Mathf.PI));
                if (windup < 0f)
                {
                    windup = -1f;
                    model.localScale = Vector3.one * scale;
                    atkTimer = boss ? 1.5f : 1.8f;
                    if (dist < reach * 1.35f && !p.Dead)
                        p.TakeDamage(Mathf.RoundToInt(atk * Random.Range(0.85f, 1.15f)), title);
                }
            }
            else if (aggro)
            {
                if (dist > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), 10f * dt);
                if (dist > reach) move = to.normalized * speed;
                else if (atkTimer <= 0f) windup = 0.45f;
            }
            else
            {
                wanderTimer -= dt;
                Vector3 back = home - transform.position;
                back.y = 0f;
                if (back.magnitude > 9f) wanderDir = back.normalized;
                else if (wanderTimer <= 0f)
                {
                    wanderTimer = Random.Range(2f, 5f);
                    Vector2 r = Random.insideUnitCircle;
                    wanderDir = Random.value < 0.4f ? Vector3.zero : new Vector3(r.x, 0, r.y).normalized;
                }
                move = wanderDir * speed * 0.35f;
                if (move.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 5f * dt);
            }
            cc.SimpleMove(move);
        }

        public void TakeHit(int damage, bool crit)
        {
            if (dead) return;
            hp -= damage;
            flash = 0.12f;
            body.material.color = Color.white;
            Sfx.Play(Sfx.Hit, crit ? 1f : 0.7f);
            var p = PlayerController.I;
            if (!boss && p != null)
            {
                Vector3 away = transform.position - p.transform.position;
                away.y = 0f;
                cc.Move(away.normalized * 0.35f);
            }
            if (hp <= 0) Die();
        }

        void Die()
        {
            dead = true;
            var s = Game.S;
            int k = boss ? 8 : 1;
            int gold = Mathf.RoundToInt(Db.ZGold[zone] * k * Random.Range(0.8f, 1.2f));
            int ore = boss ? 8 : (Random.value < 0.4f ? 2 : 1);
            s.gold += gold;
            s.mats[zone] += ore;
            GameUI.Msg(title + " besiegt: +" + (Db.ZXp[zone] * k) + " EP, +" + gold + " Gold, +" + ore + " " + Db.MatName[zone]);
            Sfx.Play(Sfx.Pickup);
            Game.GainXp(Db.ZXp[zone] * k);
            if (boss)
            {
                bool first = s.bosses[zone] == 0;
                s.bosses[zone]++;
                if (first && zone < Db.ZoneName.Length - 1)
                {
                    GameBootstrap.OpenGate(zone);
                    GameUI.Msg("<color=#FFC247>Das Siegel bricht – der Weg nach " + Db.ZoneName[zone + 1] + " ist frei!</color>");
                }
                if (zone == Db.ZoneName.Length - 1 && !s.won)
                {
                    s.won = true;
                    GameUI.Msg("<color=#FF4D5E><b>Der Gefallene Wächter ist bezwungen. Aschenfurt feiert seinen größten Schmied!</b></color>");
                }
            }
            Game.Touch();
            if (owner != null) owner.OnDeath(this);
            Destroy(gameObject);
        }
    }
}
