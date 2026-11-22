using UnityEngine;

namespace Glutherz
{
    /// <summary>
    /// Baut beim Start das komplette Spiel im Code auf: Welt, Spieler, Gegner und Oberfläche.
    /// Es muss nichts in der Szene platziert werden – einfach auf Play drücken.
    /// </summary>
    public static class GameBootstrap
    {
        static readonly GameObject[] gates = new GameObject[4];
        static Transform world;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            Time.timeScale = 1f;
            Interactable.All.Clear();
            Game.Load();

            foreach (var c in Camera.allCameras) c.gameObject.SetActive(false);

            bool hasSun = false;
            foreach (var l in Resources.FindObjectsOfTypeAll<Light>())
                if (l.type == LightType.Directional && l.gameObject.scene.IsValid() && l.gameObject.activeInHierarchy) hasSun = true;
            if (!hasSun)
            {
                var sun = new GameObject("Sonne").AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.2f;
                sun.shadows = LightShadows.Soft;
                sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.55f, 0.6f, 0.68f);

            world = new GameObject("Welt").transform;
            Random.InitState(20261122); // feste Landschaft, damit die Welt jedes Mal gleich aussieht
            BuildTerrain();
            BuildVillage();
            for (int i = 0; i < Db.ZoneName.Length; i++) BuildZone(i);
            Random.InitState((int)(System.DateTime.Now.Ticks & 0x7fffffff));

            PlayerController.Create();
            GameUI.Create();
            for (int i = 0; i < Db.ZoneName.Length; i++)
            {
                var sp = new GameObject("Spawner " + Db.ZoneName[i]).AddComponent<ZoneSpawner>();
                sp.transform.SetParent(world, false);
                sp.zone = i;
            }
        }

        public static void OpenGate(int zone)
        {
            if (zone >= 0 && zone < gates.Length && gates[zone] != null) gates[zone].SetActive(false);
        }

        public static void ResetGates()
        {
            for (int i = 0; i < gates.Length; i++)
                if (gates[i] != null) gates[i].SetActive(Game.S.bosses[i] == 0);
        }

        static void BuildTerrain()
        {
            float w = Db.HalfWidth * 2f, end = Db.ZoneStart(Db.ZoneName.Length);
            Prim.Make(PrimitiveType.Cube, world, new Vector3(0, -0.5f, 0), new Vector3(w, 1, Db.VillageEnd * 2f), new Color(0.45f, 0.4f, 0.3f), true).name = "Dorfboden";
            for (int i = 0; i < Db.ZoneName.Length; i++)
                Prim.Make(PrimitiveType.Cube, world, new Vector3(0, -0.5f, Db.ZoneStart(i) + Db.ZoneLen / 2f), new Vector3(w, 1, Db.ZoneLen), Db.ZoneGround[i], true).name = "Boden " + Db.ZoneName[i];

            // Felswände rundherum
            Color rock = new Color(0.25f, 0.24f, 0.23f);
            float len = end + Db.VillageEnd, mid = (end - Db.VillageEnd) / 2f;
            Prim.Make(PrimitiveType.Cube, world, new Vector3(Db.HalfWidth + 2f, 7, mid), new Vector3(4, 16, len + 8), rock, true);
            Prim.Make(PrimitiveType.Cube, world, new Vector3(-Db.HalfWidth - 2f, 7, mid), new Vector3(4, 16, len + 8), rock, true);
            Prim.Make(PrimitiveType.Cube, world, new Vector3(0, 7, -Db.VillageEnd - 2f), new Vector3(w + 8, 16, 4), rock, true);
            Prim.Make(PrimitiveType.Cube, world, new Vector3(0, 7, end + 2f), new Vector3(w + 8, 16, 4), rock, true);

            // Siegel zwischen den Gebieten: verschwinden, sobald der Boss davor besiegt ist.
            for (int i = 0; i < gates.Length; i++)
            {
                var g = Prim.Make(PrimitiveType.Cube, world, new Vector3(0, 5, Db.ZoneStart(i + 1)), new Vector3(w, 12, 1.2f), new Color(0.9f, 0.3f, 0.15f), true);
                g.name = "Siegel " + (i + 1);
                var glow = new GameObject("Schein").AddComponent<Light>();
                glow.transform.SetParent(g.transform, false);
                glow.transform.localPosition = new Vector3(0, -0.2f, -3f);
                glow.type = LightType.Point; glow.color = new Color(1f, 0.4f, 0.2f); glow.range = 25f; glow.intensity = 3f;
                gates[i] = g;
            }
            ResetGates();
        }

        static void BuildVillage()
        {
            var village = new GameObject("Aschenfurt").transform;
            village.SetParent(world, false);
            Color wall = new Color(0.8f, 0.72f, 0.6f), roof = new Color(0.45f, 0.2f, 0.15f);
            Vector3[] spots = { new Vector3(-18, 0, -12), new Vector3(-20, 0, 8), new Vector3(20, 0, -14), new Vector3(22, 0, 14), new Vector3(-6, 0, -22) };
            foreach (var s in spots)
            {
                Prim.Make(PrimitiveType.Cube, village, s + new Vector3(0, 2, 0), new Vector3(7, 4, 6), wall, true);
                Prim.Make(PrimitiveType.Cube, village, s + new Vector3(0, 4.4f, 0), new Vector3(8, 0.8f, 7), roof, false);
                Prim.Make(PrimitiveType.Cube, village, s + new Vector3(0, 5.1f, 0), new Vector3(5.5f, 0.8f, 5), roof, false);
                Prim.Make(PrimitiveType.Cube, village, s + new Vector3(0, 5.8f, 0), new Vector3(3, 0.7f, 3), roof, false);
            }

            // Schmiede: Esse mit Glut, Dach auf Pfosten und der Amboss zum Benutzen.
            Vector3 f = new Vector3(9, 0, 4);
            Color dark = new Color(0.2f, 0.2f, 0.22f), wood = new Color(0.35f, 0.22f, 0.12f);
            Prim.Make(PrimitiveType.Cube, village, f + new Vector3(2.5f, 1.2f, 2), new Vector3(2.6f, 2.4f, 2.6f), dark, true);
            Prim.Make(PrimitiveType.Cube, village, f + new Vector3(2.5f, 3.4f, 2), new Vector3(1.2f, 2.2f, 1.2f), dark, false);
            Prim.Make(PrimitiveType.Cube, village, f + new Vector3(1.15f, 1.1f, 2), new Vector3(0.1f, 0.9f, 1.4f), new Color(1f, 0.45f, 0.1f), false);
            var fire = new GameObject("Glut").AddComponent<Light>();
            fire.transform.SetParent(village, false);
            fire.transform.position = f + new Vector3(0.6f, 1.4f, 2);
            fire.type = LightType.Point; fire.color = new Color(1f, 0.5f, 0.15f); fire.range = 14f; fire.intensity = 4f;
            foreach (var corner in new[] { new Vector3(-2.5f, 0, -2.5f), new Vector3(4.2f, 0, -2.5f), new Vector3(-2.5f, 0, 3.8f), new Vector3(4.2f, 0, 3.8f) })
                Prim.Make(PrimitiveType.Cube, village, f + corner + new Vector3(0, 2, 0), new Vector3(0.3f, 4, 0.3f), wood, true);
            Prim.Make(PrimitiveType.Cube, village, f + new Vector3(0.85f, 4.2f, 0.65f), new Vector3(8, 0.3f, 7.5f), roof, false);

            var anvil = new GameObject("Amboss");
            anvil.transform.SetParent(village, false);
            anvil.transform.position = f;
            Prim.Make(PrimitiveType.Cube, anvil.transform, new Vector3(0, 0.35f, 0), new Vector3(0.6f, 0.7f, 0.6f), wood, true);
            Prim.Make(PrimitiveType.Cube, anvil.transform, new Vector3(0, 0.85f, 0), new Vector3(1.3f, 0.3f, 0.5f), dark, true);
            anvil.AddComponent<ForgeStation>();

            // Wegweiser Richtung Flüsterwald
            Prim.Make(PrimitiveType.Cube, village, new Vector3(3, 1.2f, 24), new Vector3(0.2f, 2.4f, 0.2f), wood, true);
            Prim.Make(PrimitiveType.Cube, village, new Vector3(3, 2.1f, 24.3f), new Vector3(1.6f, 0.4f, 0.1f), wood, false);
        }

        static void BuildZone(int zone)
        {
            var root = new GameObject(Db.ZoneName[zone]).transform;
            root.SetParent(world, false);
            float z0 = Db.ZoneStart(zone);
            for (int i = 0; i < 46; i++)
            {
                var pos = new Vector3(Random.Range(-32f, 32f), 0, Random.Range(z0 + 3f, z0 + Db.ZoneLen - 3f));
                if (Mathf.Abs(pos.x) < 5f) pos.x += pos.x < 0 ? -6f : 6f;      // Mittelweg frei halten
                if (pos.z > z0 + Db.ZoneLen - 26f && Mathf.Abs(pos.x) < 16f) continue; // Platz für den Bosskampf
                Decorate(zone, root, pos);
            }
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                OreNode.Create(zone, new Vector3(side * Random.Range(8f, 30f), 0, z0 + 10f + i * 11f + Random.Range(-3f, 3f)), root);
            }
        }

        static void Decorate(int zone, Transform root, Vector3 pos)
        {
            float s = Random.Range(0.8f, 1.5f);
            switch (zone)
            {
                case 0: // Bäume
                    Prim.Make(PrimitiveType.Cylinder, root, pos + new Vector3(0, 1.6f * s, 0), new Vector3(0.5f * s, 1.6f * s, 0.5f * s), new Color(0.35f, 0.22f, 0.12f), true);
                    Prim.Make(PrimitiveType.Sphere, root, pos + new Vector3(0, 4.2f * s, 0), Vector3.one * 3.6f * s, new Color(0.13f, Random.Range(0.35f, 0.5f), 0.14f), false);
                    break;
                case 1: // Felsbrocken
                    Prim.Make(PrimitiveType.Cube, root, pos + new Vector3(0, 0.8f * s, 0), new Vector3(2.2f, 2f, 1.8f) * s, new Color(0.33f, 0.31f, 0.3f), true)
                        .transform.localRotation = Quaternion.Euler(Random.Range(-15f, 15f), Random.Range(0f, 360f), Random.Range(-15f, 15f));
                    break;
                case 2: // Eisnadeln
                    Prim.Make(PrimitiveType.Cube, root, pos + new Vector3(0, 2.2f * s, 0), new Vector3(0.9f, 5f, 0.9f) * s, new Color(0.65f, 0.9f, 1f), true)
                        .transform.localRotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f));
                    break;
                case 3: // Obsidiansäulen und Lavatümpel
                    if (Random.value < 0.35f)
                        Prim.Make(PrimitiveType.Cylinder, root, pos + new Vector3(0, 0.02f, 0), new Vector3(3f * s, 0.02f, 3f * s), new Color(1f, 0.4f, 0.05f), false);
                    else
                        Prim.Make(PrimitiveType.Cube, root, pos + new Vector3(0, 2f * s, 0), new Vector3(1.4f, 4.2f, 1.4f) * s, new Color(0.08f, 0.06f, 0.08f), true)
                            .transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), Random.Range(0f, 360f), Random.Range(-8f, 8f));
                    break;
                default: // Ruinensäulen
                    float h = Random.Range(1.2f, 4f);
                    Prim.Make(PrimitiveType.Cylinder, root, pos + new Vector3(0, h, 0), new Vector3(1.1f, h, 1.1f), new Color(0.75f, 0.72f, 0.85f), true);
                    Prim.Make(PrimitiveType.Cube, root, pos + new Vector3(0, 0.15f, 0), new Vector3(1.8f, 0.3f, 1.8f), new Color(0.6f, 0.58f, 0.7f), false);
                    break;
            }
        }
    }
}
