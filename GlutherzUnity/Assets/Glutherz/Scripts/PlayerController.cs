using UnityEngine;

namespace Glutherz
{
    /// <summary>First-Person-Steuerung: Laufen, Umsehen, Zuschlagen, Blocken, Wuchtschlag, Heiltrank, Interagieren.</summary>
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController I;
        public static readonly Vector3 HomePos = new Vector3(0, 1.1f, -6f);

        public Camera cam;
        public float hp;
        public bool Dead { get; private set; }
        public bool Blocking { get; private set; }
        public int Zone { get; private set; }
        public float HeavyCd { get; private set; }
        public float PotionCd { get; private set; }
        public Interactable Near { get; private set; }

        CharacterController cc;
        Transform weapon;
        float pitch, vy, atkTimer, swing = -1f, swingMult = 1f, lastHurt = -99f, shake;
        bool swingDone;
        int lastZone = -2;
        const int IgnoreLayer = 2; // "Ignore Raycast"

        public static PlayerController Create()
        {
            var go = new GameObject("Spieler");
            go.layer = IgnoreLayer;
            go.transform.position = HomePos;
            var p = go.AddComponent<PlayerController>();
            p.cc = go.AddComponent<CharacterController>();
            p.cc.height = 1.8f;
            p.cc.radius = 0.4f;
            p.cc.center = Vector3.zero;

            var camGo = new GameObject("Kamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(go.transform, false);
            camGo.transform.localPosition = new Vector3(0, 0.7f, 0);
            p.cam = camGo.AddComponent<Camera>();
            p.cam.nearClipPlane = 0.05f;
            p.cam.farClipPlane = 400f;
            p.cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();
            Sfx.Init(camGo.AddComponent<AudioSource>());

            p.weapon = new GameObject("Waffe").transform;
            p.weapon.SetParent(camGo.transform, false);
            I = p;
            p.hp = Game.GetStats().hp;
            p.RebuildWeapon();
            return p;
        }

        void OnEnable() { Game.Changed += RebuildWeapon; }
        void OnDisable() { Game.Changed -= RebuildWeapon; }

        /// <summary>Baut das Waffenmodell in der Hand passend zur angelegten Waffe neu.</summary>
        public void RebuildWeapon()
        {
            if (weapon == null) return;
            for (int i = weapon.childCount - 1; i >= 0; i--) Destroy(weapon.GetChild(i).gameObject);
            var it = Game.S.eq[0];
            if (it == null || it.Empty) return;
            Color blade = Color.Lerp(Db.MatColor[it.tier], Prim.Hex(Db.RarColor[it.rar]), it.rar > 0 ? 0.45f : 0f);
            Color wood = new Color(0.35f, 0.22f, 0.12f);
            if (it.type == "Axt")
            {
                Prim.Make(PrimitiveType.Cube, weapon, new Vector3(0, 0.3f, 0), new Vector3(0.05f, 0.9f, 0.05f), wood, false);
                Prim.Make(PrimitiveType.Cube, weapon, new Vector3(0, 0.65f, 0.12f), new Vector3(0.04f, 0.28f, 0.3f), blade, false);
            }
            else
            {
                float len = it.type == "Dolch" ? 0.4f : 0.8f;
                Prim.Make(PrimitiveType.Cube, weapon, new Vector3(0, -0.02f, 0), new Vector3(0.05f, 0.2f, 0.05f), wood, false);
                Prim.Make(PrimitiveType.Cube, weapon, new Vector3(0, 0.09f, 0), new Vector3(0.05f, 0.03f, 0.22f), new Color(0.7f, 0.6f, 0.3f), false);
                Prim.Make(PrimitiveType.Cube, weapon, new Vector3(0, 0.11f + len / 2f, 0), new Vector3(0.025f, len, 0.08f), blade, false);
            }
            for (int i = 0; i < weapon.childCount; i++) weapon.GetChild(i).gameObject.layer = IgnoreLayer;
        }

        void Update()
        {
            var ui = GameUI.I;
            if (ui != null && ui.MenuOpen) return;
            float dt = Time.deltaTime;

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (In.Down(Act.Attack)) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
                return;
            }

            // Umsehen
            Vector2 look = In.Look();
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            shake = Mathf.Max(0f, shake - dt * 4f);
            cam.transform.localRotation = Quaternion.Euler(pitch + Random.Range(-shake, shake) * 3f, Random.Range(-shake, shake) * 3f, 0f);

            // Laufen
            Vector2 mv = In.Move();
            Vector3 dir = transform.right * mv.x + transform.forward * mv.y;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            float speed = Blocking ? 3f : (In.Held(Act.Sprint) ? 9f : 6f);
            if (cc.isGrounded)
            {
                if (vy < 0f) vy = -2f;
                if (In.Down(Act.Jump)) vy = 6.5f;
            }
            vy -= 20f * dt;
            cc.Move((dir * speed + Vector3.up * vy) * dt);
            if (transform.position.y < -20f) Teleport(HomePos);

            Zone = Db.ZoneAt(transform.position.z);
            if (Zone != lastZone)
            {
                lastZone = Zone;
                if (ui != null) ui.ShowZone(Zone < 0 ? "Aschenfurt" : Db.ZoneName[Zone]);
            }

            // Kampf
            Stats st = Game.GetStats();
            atkTimer -= dt;
            HeavyCd = Mathf.Max(0f, HeavyCd - dt);
            PotionCd = Mathf.Max(0f, PotionCd - dt);
            Blocking = In.Held(Act.Block) && swing < 0f;
            if (!Blocking && swing < 0f && atkTimer <= 0f)
            {
                if (In.Down(Act.Heavy) && HeavyCd <= 0f) { StartSwing(2.5f); HeavyCd = 6f; atkTimer = st.interval; }
                else if (In.Held(Act.Attack)) { StartSwing(1f); atkTimer = st.interval; }
            }
            if (swing >= 0f)
            {
                swing += dt / 0.24f;
                if (!swingDone && swing >= 0.45f) { swingDone = true; ResolveSwing(st); }
                if (swing >= 1f) swing = -1f;
            }
            if (In.Down(Act.Potion) && PotionCd <= 0f && Game.S.potions > 0 && hp < st.hp)
            {
                Game.S.potions--;
                PotionCd = 5f;
                int heal = Mathf.RoundToInt(st.hp * 0.4f);
                hp = Mathf.Min(st.hp, hp + heal);
                GameUI.Msg("<color=#6FCF7F>Heiltrank: +" + heal + " Leben</color>");
                Sfx.Play(Sfx.Pickup);
                Game.Touch();
            }
            if (Time.time - lastHurt > 6f) hp = Mathf.Min(st.hp, hp + st.hp * 0.04f * dt);
            hp = Mathf.Min(hp, st.hp);

            // Waffenhaltung
            Vector3 pos = new Vector3(0.32f, -0.32f, 0.55f);
            Quaternion rot = Quaternion.Euler(-8f, -12f, -6f);
            if (Blocking) { pos = new Vector3(0.05f, -0.28f, 0.5f); rot = Quaternion.Euler(0f, 0f, 75f); }
            else if (swing >= 0f)
            {
                float a = Mathf.Sin(swing * Mathf.PI);
                pos += new Vector3(-0.2f * a, 0.05f * a, 0.15f * a);
                rot = Quaternion.Euler(-8f + 95f * a, -12f - 35f * a, -6f);
            }
            weapon.localPosition = Vector3.Lerp(weapon.localPosition, pos, 22f * dt);
            weapon.localRotation = Quaternion.Slerp(weapon.localRotation, rot, 22f * dt);

            // Interaktion
            Near = null;
            float best = 3.5f;
            foreach (var it in Interactable.All)
            {
                Vector3 d = it.transform.position - transform.position;
                d.y = 0f;
                if (d.magnitude < best) { best = d.magnitude; Near = it; }
            }
            if (Near != null && In.Down(Act.Interact)) Near.Use();
        }

        void StartSwing(float mult)
        {
            swing = 0f; swingDone = false; swingMult = mult;
            Sfx.Play(Sfx.Swing);
        }

        void ResolveSwing(Stats st)
        {
            int mask = ~(1 << IgnoreLayer);
            if (swingMult > 1.5f)
            {
                // Wuchtschlag trifft alles in einem Bogen vor dem Spieler.
                Vector3 center = cam.transform.position + cam.transform.forward * 2f;
                var done = new System.Collections.Generic.HashSet<IDamageable>();
                foreach (var c in Physics.OverlapSphere(center, 2.6f, mask))
                {
                    var t = c.GetComponentInParent<IDamageable>();
                    if (t != null && done.Add(t)) Strike(t, st, swingMult);
                }
                shake = 0.5f;
                return;
            }
            RaycastHit hit;
            if (Physics.SphereCast(cam.transform.position, 0.35f, cam.transform.forward, out hit, 3.1f, mask))
            {
                var t = hit.collider.GetComponentInParent<IDamageable>();
                if (t == null) return;
                Strike(t, st, 1f);
                if (st.dbl > 0 && Random.Range(0, 100) < st.dbl && t.HpFraction > 0f)
                {
                    Strike(t, st, 1f);
                    GameUI.Msg("Doppelschlag!");
                }
            }
        }

        void Strike(IDamageable t, Stats st, float mult)
        {
            bool crit = Random.Range(0, 100) < st.crit;
            int dmg = Mathf.Max(1, Mathf.RoundToInt(st.atk * mult * Random.Range(0.9f, 1.1f) * (crit ? 1.8f : 1f) - t.Defense * 0.5f));
            if (GameUI.I != null) GameUI.I.SetTarget(t, dmg, crit);
            t.TakeHit(dmg, crit);
            if (st.ls > 0) hp = Mathf.Min(st.hp, hp + Mathf.Ceil(dmg * st.ls / 100f));
            shake = Mathf.Max(shake, crit ? 0.35f : 0.15f);
        }

        public void TakeDamage(int raw, string from)
        {
            if (Dead) return;
            Stats st = Game.GetStats();
            int dmg = Mathf.Max(1, Mathf.RoundToInt(raw - st.def * 0.5f));
            if (Blocking) dmg = Mathf.CeilToInt(dmg * 0.35f);
            hp -= dmg;
            lastHurt = Time.time;
            shake = 0.6f;
            Sfx.Play(Blocking ? Sfx.Clang : Sfx.Hurt, 0.8f);
            if (GameUI.I != null) GameUI.I.HurtFlash(Blocking ? 0.15f : 0.4f);
            if (hp > 0f) return;

            int lost = Game.S.gold / 10;
            Game.S.gold -= lost;
            GameUI.Msg("<color=#F0837C>" + from + " hat dich besiegt. Du erwachst in Aschenfurt. -" + lost + " Gold</color>");
            Teleport(HomePos);
            hp = st.hp;
            Game.Touch();
        }

        public void Teleport(Vector3 pos)
        {
            cc.enabled = false;
            transform.position = pos;
            cc.enabled = true;
            vy = 0f;
        }
    }
}
