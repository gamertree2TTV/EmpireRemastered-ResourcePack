using System;
using System.Collections.Generic;
using UnityEngine;

namespace Glutherz
{
    public enum Slot { Weapon = 0, Helm = 1, Chest = 2, Boots = 3 }

    [Serializable]
    public class Affix
    {
        public string k;
        public int v;
    }

    [Serializable]
    public class Item
    {
        public int id;          // 0 = leerer Platz
        public Slot slot;
        public string type = "";
        public string name = "";
        public int tier, rar, q, plus;
        public int atk, def, hp;
        public float speed = 1f;
        public int value;
        public List<Affix> aff = new List<Affix>();

        public bool Empty { get { return id == 0; } }
        public float PlusMult { get { return 1f + 0.08f * plus; } }

        public string Title
        {
            get { return "<color=" + Db.RarColor[rar] + "><b>" + name + (plus > 0 ? " +" + plus : "") + "</b></color>"; }
        }

        public string Describe()
        {
            var parts = new List<string>();
            float m = PlusMult;
            if (atk > 0) parts.Add("Angriff " + Mathf.RoundToInt(atk * m));
            if (slot == Slot.Weapon) parts.Add("Tempo x" + speed.ToString("0.##"));
            if (def > 0) parts.Add("Rüstung " + Mathf.RoundToInt(def * m));
            if (hp > 0) parts.Add("Leben +" + Mathf.RoundToInt(hp * m));
            foreach (var a in aff) parts.Add(Db.AffixLabel(a));
            return Title + "  <size=14>[" + Db.Rar[rar] + "]</size>\n<size=15>" + string.Join(" · ", parts.ToArray()) + "</size>";
        }
    }

    public class Recipe
    {
        public string type; public Slot slot; public int ore;
        public float atk, def, hp, speed = 1f; public int crit; public string desc = "";
    }

    public class Stats
    {
        public int atk, def, hp, crit, ls, dbl;
        public float interval;
    }

    [Serializable]
    public class SaveData
    {
        public int lvl = 1, xp, pts, str, vit, agi, gold = 30, potions = 2;
        public int smithLvl = 1, smithXp;
        public int[] mats = { 6, 0, 0, 0, 0 };
        public int[] bosses = { 0, 0, 0, 0, 0 };
        public List<Item> inv = new List<Item>();
        public Item[] eq = { new Item(), new Item(), new Item(), new Item() };
        public int nextId = 1;
        public bool won;
    }

    /// <summary>Feste Spieldaten: Seltenheiten, Materialien, Rezepte, Gebiete.</summary>
    public static class Db
    {
        public static readonly string[] Rar = { "Gewöhnlich", "Selten", "Episch", "Einzigartig", "Legendär" };
        public static readonly string[] RarColor = { "#B9B2A8", "#5AA9E6", "#B678F0", "#FFC247", "#FF4D5E" };
        public static readonly float[] RarMult = { 1f, 1.3f, 1.7f, 2.3f, 3.2f };
        public static readonly int[] RarMin = { 0, 45, 65, 82, 100 };

        public static readonly string[] MatName = { "Eisenerz", "Mondsilber", "Frostkristall", "Glutobsidian", "Sternenstahl" };
        public static readonly string[] MatAdj = { "Eisen", "Mondsilber", "Frostkristall", "Obsidian", "Sternenstahl" };
        public static readonly Color[] MatColor =
        {
            new Color(0.55f, 0.55f, 0.6f), new Color(0.75f, 0.85f, 1f), new Color(0.5f, 0.9f, 1f),
            new Color(1f, 0.4f, 0.15f), new Color(0.8f, 0.6f, 1f)
        };
        public static readonly int[] TAtk = { 8, 18, 34, 60, 100 };
        public static readonly int[] TDef = { 4, 9, 17, 30, 50 };
        public static readonly int[] THp = { 20, 50, 110, 220, 420 };
        public static readonly int[] TGold = { 15, 50, 130, 300, 650 };

        public static readonly Recipe[] Recipes =
        {
            new Recipe { type = "Schwert", slot = Slot.Weapon, ore = 5, atk = 1f, speed = 1f, desc = "ausgewogen" },
            new Recipe { type = "Axt", slot = Slot.Weapon, ore = 5, atk = 1.4f, speed = 0.75f, desc = "wuchtig, langsam" },
            new Recipe { type = "Dolch", slot = Slot.Weapon, ore = 5, atk = 0.7f, speed = 1.45f, crit = 10, desc = "schnell, +10 % Krit" },
            new Recipe { type = "Helm", slot = Slot.Helm, ore = 4, def = 0.6f, hp = 0.5f },
            new Recipe { type = "Harnisch", slot = Slot.Chest, ore = 6, def = 1f, hp = 1f },
            new Recipe { type = "Stiefel", slot = Slot.Boots, ore = 3, def = 0.5f, hp = 0.4f },
        };
        public static readonly string[] SlotName = { "Waffe", "Helm", "Harnisch", "Stiefel" };

        public static readonly string[] ZoneName = { "Flüsterwald", "Aschenmine", "Frostpass", "Glutschlund", "Sternenruine" };
        public static readonly string[][] MobName =
        {
            new[] { "Moosschleim", "Grauwolf" }, new[] { "Höhlenschrecken", "Stollenkobold" },
            new[] { "Eiswolf", "Schneegeist" }, new[] { "Feuerskorpion", "Aschenechse" },
            new[] { "Runenwächter", "Sternenschatten" }
        };
        public static readonly string[] BossName = { "Der Alte Eber", "Der Minenkönig", "Der Frostriese", "Der Lavawurm", "Der Gefallene Wächter" };
        public static readonly Color[] ZoneGround =
        {
            new Color(0.22f, 0.42f, 0.2f), new Color(0.3f, 0.27f, 0.25f), new Color(0.8f, 0.87f, 0.95f),
            new Color(0.25f, 0.1f, 0.08f), new Color(0.2f, 0.18f, 0.35f)
        };
        public static readonly Color[] MobColor =
        {
            new Color(0.4f, 0.75f, 0.3f), new Color(0.5f, 0.4f, 0.3f), new Color(0.6f, 0.8f, 1f),
            new Color(0.9f, 0.3f, 0.1f), new Color(0.6f, 0.4f, 0.9f)
        };
        public static readonly int[] ZHp = { 40, 140, 380, 900, 2000 };
        public static readonly int[] ZAtk = { 6, 16, 34, 65, 120 };
        public static readonly int[] ZDef = { 0, 4, 12, 26, 50 };
        public static readonly int[] ZXp = { 8, 25, 60, 140, 320 };
        public static readonly int[] ZGold = { 5, 14, 35, 80, 180 };

        static readonly string[] LegendNames = { "Glutherz", "Aschenbrand", "Sturmzahn", "Morgenrot", "Nachtschwur", "Eisenwille", "Funkenlied", "Donnergroll" };

        // Weltmaße: Dorf um z = 0, Gebiete reihen sich in +z-Richtung auf.
        public const float HalfWidth = 35f, VillageEnd = 30f, ZoneLen = 90f;
        public static float ZoneStart(int i) { return VillageEnd + i * ZoneLen; }
        public static int ZoneAt(float z)
        {
            if (z < VillageEnd) return -1;
            return Mathf.Clamp((int)((z - VillageEnd) / ZoneLen), 0, ZoneName.Length - 1);
        }

        public static string AffixLabel(Affix a)
        {
            switch (a.k)
            {
                case "crit": return "+" + a.v + " % Krit";
                case "ls": return a.v + " % Lebensraub";
                case "hp": return "+" + a.v + " Leben";
                case "atkp": return "+" + a.v + " % Angriff";
                case "def": return "+" + a.v + " Rüstung";
                case "spd": return "+" + a.v + " % Tempo";
                case "dbl": return "Doppelschlag " + a.v + " %";
                default: return a.k;
            }
        }

        static int RollAffix(string k, int tier)
        {
            switch (k)
            {
                case "crit": return UnityEngine.Random.Range(3, 9);
                case "ls": return UnityEngine.Random.Range(2, 6);
                case "hp": return UnityEngine.Random.Range(12, 21) * (tier + 1);
                case "def": return UnityEngine.Random.Range(2, 5) * (tier + 1);
                default: return UnityEngine.Random.Range(4, 11); // atkp, spd
            }
        }

        public static Item MakeItem(Recipe rec, int tier, int rar, float q)
        {
            float m = RarMult[rar] * (0.9f + q / 500f);
            var it = new Item
            {
                id = Game.S.nextId++, slot = rec.slot, type = rec.type, tier = tier, rar = rar, q = Mathf.RoundToInt(q),
                atk = Mathf.RoundToInt(TAtk[tier] * rec.atk * m),
                def = Mathf.RoundToInt(TDef[tier] * rec.def * m),
                hp = Mathf.RoundToInt(THp[tier] * rec.hp * m),
                speed = rec.speed, value = Mathf.RoundToInt(TGold[tier] * 0.4f * (rar + 1)),
            };
            if (rec.crit > 0) it.aff.Add(new Affix { k = "crit", v = rec.crit });
            var pool = rec.slot == Slot.Weapon
                ? new List<string> { "crit", "ls", "atkp", "spd" }
                : new List<string> { "hp", "def", "crit", "spd", "atkp" };
            if (rec.crit > 0) pool.Remove("crit");
            for (int i = 0; i < rar && pool.Count > 0; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                string k = pool[idx]; pool.RemoveAt(idx);
                it.aff.Add(new Affix { k = k, v = RollAffix(k, tier) });
            }
            string baseName = MatAdj[tier] + "-" + rec.type;
            if (rar == 4)
            {
                it.aff.Add(new Affix { k = "dbl", v = 20 });
                it.name = LegendNames[UnityEngine.Random.Range(0, LegendNames.Length)] + ", " + baseName;
            }
            else it.name = baseName;
            return it;
        }
    }

    /// <summary>Laufender Spielstand und Regeln (Werte, Erfahrung, Speichern).</summary>
    public static class Game
    {
        const string SaveKey = "glutherz-save-v1";
        public static SaveData S;
        public static event Action Changed;

        public static void Load()
        {
            S = null;
            string raw = PlayerPrefs.GetString(SaveKey, "");
            if (raw.Length > 0)
            {
                try { S = JsonUtility.FromJson<SaveData>(raw); } catch (Exception) { S = null; }
            }
            if (S == null || S.eq == null || S.eq.Length != 4 || S.mats == null || S.mats.Length != 5 || S.bosses == null || S.bosses.Length != 5) NewGame();
        }

        public static void NewGame()
        {
            S = new SaveData();
            S.eq[0] = new Item { id = S.nextId++, slot = Slot.Weapon, type = "Schwert", name = "Rostiges Schwert", atk = 4, speed = 1f, value = 2 };
            Save();
        }

        public static void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(S));
            PlayerPrefs.Save();
        }

        /// <summary>Nach jeder Änderung aufrufen: speichert und aktualisiert die Anzeige.</summary>
        public static void Touch()
        {
            Save();
            if (Changed != null) Changed();
        }

        public static int XpNeed(int l) { return Mathf.RoundToInt(30f * Mathf.Pow(l, 1.5f)); }
        public static int SmithNeed(int l) { return 40 * l; }
        public static int ZonesOpen
        {
            get { int n = 1; for (int i = 0; i < 4; i++) if (S.bosses[i] > 0) n = i + 2; return n; }
        }
        public static int PotionPrice { get { return 20 * ZonesOpen; } }

        public static Stats GetStats()
        {
            float atk = 4 + S.lvl + S.str * 2, def = 0, hp = 90 + S.lvl * 10 + S.vit * 12;
            float crit = 5 + S.agi, spd = S.agi * 1.5f, ls = 0, dbl = 0, atkp = 0, wspeed = 1f;
            foreach (var it in S.eq)
            {
                if (it == null || it.Empty) continue;
                float m = it.PlusMult;
                atk += it.atk * m; def += it.def * m; hp += it.hp * m;
                if (it.slot == Slot.Weapon) wspeed = it.speed;
                foreach (var a in it.aff)
                {
                    switch (a.k)
                    {
                        case "hp": hp += a.v; break;
                        case "def": def += a.v; break;
                        case "crit": crit += a.v; break;
                        case "ls": ls += a.v; break;
                        case "atkp": atkp += a.v; break;
                        case "spd": spd += a.v; break;
                        case "dbl": dbl += a.v; break;
                    }
                }
            }
            return new Stats
            {
                atk = Mathf.RoundToInt(atk * (1f + atkp / 100f)), def = Mathf.RoundToInt(def), hp = Mathf.RoundToInt(hp),
                crit = Mathf.Min(75, Mathf.RoundToInt(crit)), ls = Mathf.Min(40, Mathf.RoundToInt(ls)), dbl = Mathf.RoundToInt(dbl),
                interval = 0.75f / (wspeed * (1f + spd / 100f)),
            };
        }

        public static void GainXp(int n)
        {
            S.xp += n;
            while (S.xp >= XpNeed(S.lvl))
            {
                S.xp -= XpNeed(S.lvl); S.lvl++; S.pts += 3;
                GameUI.Msg("<color=#FFC247>Stufe " + S.lvl + "! +3 Attributspunkte (Tab)</color>");
                Sfx.Play(Sfx.LevelUp);
            }
        }

        public static void GainSmithXp(int n)
        {
            if (S.smithLvl >= 25) return;
            S.smithXp += n;
            while (S.smithLvl < 25 && S.smithXp >= SmithNeed(S.smithLvl))
            {
                S.smithXp -= SmithNeed(S.smithLvl); S.smithLvl++;
                GameUI.Msg("<color=#FFC247>Schmiedekunst Stufe " + S.smithLvl + "!</color>");
            }
        }

        public static int EnhanceGold(Item it) { return Mathf.RoundToInt(Db.TGold[it.tier] * 0.3f * (it.plus + 1)); }
        public static int EnhanceOre(Item it) { return 1 + it.plus / 3; }
        public static int EnhanceChance(Item it) { return Mathf.Max(15, 100 - it.plus * 10); }
    }
}
