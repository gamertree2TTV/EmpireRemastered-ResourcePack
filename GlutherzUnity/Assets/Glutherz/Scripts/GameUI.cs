using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Glutherz
{
    /// <summary>Komplette Oberfläche, im Code aufgebaut: HUD, Menü (Schmiede, Inventar, Charakter, Reisen) und Schmiede-Minispiel.</summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI I;

        Font font;
        // HUD
        RectTransform hpFill, xpFill, targetFill, crosshair;
        Text hpText, infoText, promptText, zoneText, msgText, targetText, skillText;
        GameObject targetRoot;
        Image hurt;
        float zoneTimer, targetTimer, hurtAlpha;
        IDamageable target;
        string targetLine = "";
        readonly List<KeyValuePair<string, float>> msgs = new List<KeyValuePair<string, float>>();
        // Menü
        GameObject menu;
        Transform content, tabBar;
        Text menuTitle;
        int tab;
        bool atForge, confirmReset;
        Item lastForged;
        // Schmiede-Minispiel
        GameObject forgePanel;
        RectTransform zoneRect, sweetRect, markerRect;
        Text forgeTitle, strikeText;
        bool forging;
        Recipe fRec;
        int fTier, fDir = 1;
        float fPos, fCenter, fWidth, fSpeed, fDelay;
        readonly List<int> fScores = new List<int>();
        const int Strikes = 4;

        static readonly Color PanelCol = new Color(0.09f, 0.07f, 0.06f, 0.94f);
        static readonly Color RowCol = new Color(0.17f, 0.14f, 0.11f, 1f);
        static readonly Color BtnCol = new Color(0.85f, 0.42f, 0.14f, 1f);
        static readonly Color TextCol = new Color(0.95f, 0.9f, 0.85f, 1f);

        public bool MenuOpen { get { return forging || (menu != null && menu.activeSelf); } }

        public static void Msg(string text)
        {
            if (I != null) I.msgs.Add(new KeyValuePair<string, float>(text, Time.unscaledTime + 6f));
        }

        public static GameUI Create()
        {
            var go = new GameObject("GlutherzUI");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
            var ui = go.AddComponent<GameUI>();
            I = ui;
            ui.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (ui.font == null) ui.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            ui.BuildHud();
            ui.BuildMenu();
            ui.BuildForgePanel();
            ui.RefreshInfo();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Msg("WASD laufen · Maus umsehen · Linksklick schlagen · Rechtsklick blocken");
            Msg("Q Wuchtschlag · R Heiltrank · E benutzen · Tab Inventar");
            Msg("Besiege Gegner für Erz und schmiede am Amboss bessere Ausrüstung.");
            return ui;
        }

        void OnEnable() { Game.Changed += RefreshInfo; }
        void OnDisable() { Game.Changed -= RefreshInfo; }

        // ------------------------------------------------------------ Bausteine
        RectTransform Box(string name, Transform parent, float x0, float y0, float x1, float y1)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        Image Img(string name, Transform parent, Color color, float x0, float y0, float x1, float y1, bool raycast = false)
        {
            var img = Box(name, parent, x0, y0, x1, y1).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        Text Txt(string name, Transform parent, string s, int size, TextAnchor anchor, float x0, float y0, float x1, float y1)
        {
            var t = Box(name, parent, x0, y0, x1, y1).gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = TextCol;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.text = s;
            return t;
        }

        Button Btn(Transform parent, string label, Action onClick, bool enabled, float width)
        {
            var img = Box("Button", parent, 0, 0, 1, 1).gameObject.AddComponent<Image>();
            img.color = BtnCol;
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.interactable = enabled;
            if (onClick != null) b.onClick.AddListener(() => onClick());
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.minWidth = width; le.preferredWidth = width; le.minHeight = 46; le.preferredHeight = 46;
            var t = Txt("Label", img.transform, label, 17, TextAnchor.MiddleCenter, 0, 0, 1, 1);
            t.color = enabled ? new Color(0.1f, 0.05f, 0.02f) : new Color(0.25f, 0.2f, 0.18f);
            return b;
        }

        static void SetFill(RectTransform fill, float f)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp01(f), 1f);
        }

        // ------------------------------------------------------------ HUD
        void BuildHud()
        {
            Transform root = transform;
            hurt = Img("Treffer", root, new Color(0.8f, 0f, 0f, 0f), 0, 0, 1, 1);

            Img("LebenBG", root, new Color(0, 0, 0, 0.6f), 0.02f, 0.045f, 0.27f, 0.075f);
            hpFill = Img("Leben", root, new Color(0.85f, 0.26f, 0.23f), 0.02f, 0.045f, 0.27f, 0.075f).rectTransform;
            var hpArea = Box("LebenArea", root, 0.02f, 0.045f, 0.27f, 0.075f);
            hpFill.SetParent(hpArea, false);
            hpFill.anchorMin = Vector2.zero; hpFill.anchorMax = Vector2.one;
            hpText = Txt("LebenText", hpArea, "", 18, TextAnchor.MiddleCenter, 0, 0, 1, 1);

            var xpArea = Img("EPBG", root, new Color(0, 0, 0, 0.6f), 0.02f, 0.03f, 0.27f, 0.04f).rectTransform;
            xpFill = Img("EP", xpArea, new Color(0.35f, 0.66f, 0.9f), 0, 0, 1, 1).rectTransform;

            infoText = Txt("Info", root, "", 19, TextAnchor.LowerLeft, 0.02f, 0.085f, 0.7f, 0.15f);
            skillText = Txt("Skills", root, "", 19, TextAnchor.LowerRight, 0.6f, 0.03f, 0.98f, 0.12f);
            msgText = Txt("Meldungen", root, "", 19, TextAnchor.LowerLeft, 0.02f, 0.17f, 0.55f, 0.5f);
            promptText = Txt("Hinweis", root, "", 26, TextAnchor.MiddleCenter, 0.25f, 0.34f, 0.75f, 0.4f);
            zoneText = Txt("Gebiet", root, "", 44, TextAnchor.MiddleCenter, 0.2f, 0.72f, 0.8f, 0.8f);

            targetRoot = Box("Ziel", root, 0.36f, 0.88f, 0.64f, 0.96f).gameObject;
            targetText = Txt("ZielText", targetRoot.transform, "", 20, TextAnchor.MiddleCenter, 0, 0.45f, 1, 1);
            var tb = Img("ZielBG", targetRoot.transform, new Color(0, 0, 0, 0.6f), 0, 0.1f, 1, 0.4f).rectTransform;
            targetFill = Img("ZielLeben", tb, new Color(0.85f, 0.26f, 0.23f), 0, 0, 1, 1).rectTransform;
            targetRoot.SetActive(false);

            crosshair = Img("Fadenkreuz", root, new Color(1, 1, 1, 0.8f), 0.5f, 0.5f, 0.5f, 0.5f).rectTransform;
            crosshair.sizeDelta = new Vector2(6, 6);
        }

        void RefreshInfo()
        {
            if (infoText == null) return;
            var s = Game.S;
            var sb = new System.Text.StringBuilder();
            sb.Append("Stufe ").Append(s.lvl).Append("  ·  Gold ").Append(s.gold).Append("  ·  Tränke ").Append(s.potions);
            if (s.pts > 0) sb.Append("  ·  <color=#FFC247>").Append(s.pts).Append(" Punkte frei (Tab)</color>");
            sb.Append("\n");
            for (int i = 0; i < 5; i++)
                if (s.mats[i] > 0) sb.Append(Db.MatName[i]).Append(" ").Append(s.mats[i]).Append("   ");
            infoText.text = sb.ToString();
            SetFill(xpFill, s.xp / (float)Game.XpNeed(s.lvl));
        }

        public void ShowZone(string name)
        {
            zoneText.text = name;
            zoneTimer = 3f;
        }

        public void HurtFlash(float strength) { hurtAlpha = strength; }

        public void SetTarget(IDamageable t, int dmg, bool crit)
        {
            target = t;
            targetTimer = 4f;
            targetLine = t.Title + "   <color=" + (crit ? "#FFC247>-" + dmg + " Kritisch!" : "#FFFFFF>-" + dmg) + "</color>";
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            var p = PlayerController.I;

            if (forging) UpdateForge(dt);
            else if (menu.activeSelf) { if (In.Down(Act.Menu) || In.Down(Act.Cancel)) CloseMenu(); }
            else if (In.Down(Act.Menu)) OpenMenu(false);

            if (p != null)
            {
                Stats st = Game.GetStats();
                SetFill(hpFill, p.hp / st.hp);
                hpText.text = Mathf.CeilToInt(Mathf.Max(0f, p.hp)) + " / " + st.hp;
                skillText.text = (p.Blocking ? "<color=#5AA9E6>Blockt</color>\n" : "")
                    + "[Q] Wuchtschlag " + (p.HeavyCd > 0f ? p.HeavyCd.ToString("0.0") + " s" : "<color=#6FCF7F>bereit</color>")
                    + "\n[R] Heiltrank x" + Game.S.potions + (p.PotionCd > 0f ? " (" + p.PotionCd.ToString("0.0") + " s)" : "");
                promptText.text = !MenuOpen && p.Near != null ? p.Near.Prompt : "";
            }
            crosshair.gameObject.SetActive(!MenuOpen);

            zoneTimer -= dt;
            var zc = zoneText.color; zc.a = Mathf.Clamp01(zoneTimer); zoneText.color = zc;

            hurtAlpha = Mathf.Max(0f, hurtAlpha - dt * 1.5f);
            var hc = hurt.color; hc.a = hurtAlpha; hurt.color = hc;

            targetTimer -= dt;
            bool show = targetTimer > 0f && target != null && !(target is UnityEngine.Object && (UnityEngine.Object)target == null);
            if (show)
            {
                targetText.text = targetLine;
                SetFill(targetFill, target.HpFraction);
            }
            else target = null;
            targetRoot.SetActive(show || targetTimer > 0f);
            if (!show && targetTimer > 0f) { targetText.text = targetLine; SetFill(targetFill, 0f); }

            msgs.RemoveAll(m => m.Value < Time.unscaledTime);
            while (msgs.Count > 7) msgs.RemoveAt(0);
            var sb = new System.Text.StringBuilder();
            foreach (var m in msgs) sb.Append(m.Key).Append('\n');
            msgText.text = sb.ToString();
        }

        // ------------------------------------------------------------ Menü
        void BuildMenu()
        {
            var panel = Img("Menue", transform, PanelCol, 0.14f, 0.08f, 0.86f, 0.92f, true);
            menu = panel.gameObject;
            menuTitle = Txt("Titel", menu.transform, "", 30, TextAnchor.MiddleCenter, 0, 0.93f, 1, 1);
            tabBar = Box("Tabs", menu.transform, 0.02f, 0.86f, 0.98f, 0.925f);
            var h = tabBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            Txt("Fuss", menu.transform, "Tab / Esc: schließen  ·  Mausrad: blättern", 16, TextAnchor.MiddleCenter, 0, 0, 1, 0.05f).color = new Color(0.65f, 0.58f, 0.5f);

            var scrollImg = Img("Liste", menu.transform, new Color(0, 0, 0, 0.25f), 0.02f, 0.06f, 0.98f, 0.85f, true);
            scrollImg.gameObject.AddComponent<RectMask2D>();
            var scroll = scrollImg.gameObject.AddComponent<ScrollRect>();
            var c = Box("Inhalt", scrollImg.transform, 0, 1, 1, 1);
            c.pivot = new Vector2(0.5f, 1f);
            var v = c.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(10, 10, 10, 10);
            v.spacing = 6; v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            var fit = c.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = c;
            scroll.viewport = scrollImg.rectTransform;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            content = c;
            menu.SetActive(false);
        }

        public void OpenMenu(bool forge)
        {
            if (forging) return;
            atForge = forge;
            tab = forge ? 0 : 1;
            confirmReset = false;
            menu.SetActive(true);
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Rebuild();
        }

        public void CloseMenu()
        {
            menu.SetActive(false);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Header(string text)
        {
            var t = Txt("Kopf", content, text, 22, TextAnchor.MiddleLeft, 0, 0, 1, 1);
            t.color = new Color(1f, 0.7f, 0.3f);
            t.gameObject.AddComponent<LayoutElement>().minHeight = 34;
        }

        Transform Row(string text)
        {
            var img = Img("Zeile", content, RowCol, 0, 0, 1, 1);
            var h = img.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(12, 12, 8, 8);
            h.spacing = 8; h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            var t = Txt("Text", img.transform, text, 19, TextAnchor.MiddleLeft, 0, 0, 1, 1);
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1; le.minHeight = 46;
            return img.transform;
        }

        void Do(Action change)
        {
            change();
            Game.Touch();
            Rebuild();
        }

        void Rebuild()
        {
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            for (int i = tabBar.childCount - 1; i >= 0; i--) Destroy(tabBar.GetChild(i).gameObject);
            menuTitle.text = atForge ? "Schmiede von Aschenfurt" : "Rucksack";
            string[] names = { "Schmieden", "Inventar", "Charakter", "Reisen & Händlerin" };
            for (int i = 0; i < names.Length; i++)
            {
                if (!atForge && (i == 0 || i == 3)) continue;
                int idx = i;
                var b = Btn(tabBar, names[i], () => { tab = idx; confirmReset = false; Rebuild(); }, true, 100);
                if (i == tab) b.GetComponent<Image>().color = new Color(1f, 0.75f, 0.35f);
            }
            if (tab == 0) BuildForgeTab();
            else if (tab == 1) BuildInventoryTab();
            else if (tab == 2) BuildCharacterTab();
            else BuildTravelTab();
        }

        void BuildForgeTab()
        {
            var s = Game.S;
            if (lastForged != null)
            {
                Header("Zuletzt geschmiedet (Qualität " + lastForged.q + " %)");
                Row(lastForged.Describe());
            }
            Header("Schmiedekunst Stufe " + s.smithLvl + (s.smithLvl >= 25 ? " (Maximum)" : "  –  " + s.smithXp + " / " + Game.SmithNeed(s.smithLvl) + " EP"));
            for (int t = 0; t < Game.ZonesOpen; t++)
            {
                Header(Db.MatName[t] + "  (Vorrat " + s.mats[t] + ")");
                for (int r = 0; r < Db.Recipes.Length; r++)
                {
                    var rec = Db.Recipes[r];
                    string info = rec.slot == Slot.Weapon
                        ? "Angriff ~" + Mathf.RoundToInt(Db.TAtk[t] * rec.atk) + " · " + rec.desc
                        : "Rüstung ~" + Mathf.RoundToInt(Db.TDef[t] * rec.def) + " · Leben ~" + Mathf.RoundToInt(Db.THp[t] * rec.hp);
                    var row = Row("<b>" + Db.MatAdj[t] + "-" + rec.type + "</b>   <size=15>" + info + "\nKosten: " + rec.ore + " " + Db.MatName[t] + ", " + Db.TGold[t] + " Gold</size>");
                    bool ok = s.mats[t] >= rec.ore && s.gold >= Db.TGold[t];
                    int ri = r, tier = t;
                    Btn(row, "Schmieden", () => StartForge(ri, tier), ok, 170);
                }
            }
        }

        void ItemButtons(Transform row, Item it, bool equipped)
        {
            var s = Game.S;
            if (!equipped)
                Btn(row, "Anlegen", () => Do(() =>
                {
                    s.inv.Remove(it);
                    var old = s.eq[(int)it.slot];
                    s.eq[(int)it.slot] = it;
                    if (old != null && !old.Empty) s.inv.Add(old);
                }), true, 120);
            if (it.plus < 10)
            {
                int g = Game.EnhanceGold(it), o = Game.EnhanceOre(it);
                bool can = s.gold >= g && s.mats[it.tier] >= o;
                Btn(row, "Verstärken " + Game.EnhanceChance(it) + " %\n<size=13>" + g + " Gold, " + o + " " + Db.MatName[it.tier] + "</size>", () => Do(() =>
                {
                    s.gold -= g; s.mats[it.tier] -= o;
                    if (UnityEngine.Random.Range(0, 100) < Game.EnhanceChance(it))
                    {
                        it.plus++;
                        Msg("Verstärkung gelungen: " + it.name + " +" + it.plus);
                        Sfx.Play(Sfx.Clang);
                        Game.GainSmithXp(5 * (it.tier + 1));
                    }
                    else Msg("Die Verstärkung ist fehlgeschlagen.");
                }), can, 210);
            }
            if (!equipped) Btn(row, "Verkaufen\n<size=13>" + it.value + " Gold</size>", () => Do(() => { s.inv.Remove(it); s.gold += it.value; }), true, 130);
        }

        void BuildInventoryTab()
        {
            var s = Game.S;
            Header("Angelegt");
            for (int i = 0; i < 4; i++)
            {
                var it = s.eq[i];
                if (it == null || it.Empty) Row("<color=#A89684>" + Db.SlotName[i] + ": leer</color>");
                else ItemButtons(Row(it.Describe()), it, true);
            }
            Header("Rucksack (" + s.inv.Count + ")");
            if (s.inv.Count == 0) Row("<color=#A89684>Leer. Schmiede dir etwas am Amboss!</color>");
            var sorted = new List<Item>(s.inv);
            sorted.Sort((a, b) => a.tier != b.tier ? b.tier.CompareTo(a.tier) : b.rar.CompareTo(a.rar));
            foreach (var it in sorted) ItemButtons(Row(it.Describe()), it, false);
        }

        void BuildCharacterTab()
        {
            var s = Game.S;
            Stats st = Game.GetStats();
            Header("Attribute  (" + s.pts + " Punkte frei)");
            Btn(Row("Stärke <b>" + s.str + "</b>   <size=15>+2 Angriff je Punkt</size>"), "+1", () => Do(() => { s.str++; s.pts--; }), s.pts > 0, 80);
            Btn(Row("Ausdauer <b>" + s.vit + "</b>   <size=15>+12 Leben je Punkt</size>"), "+1", () => Do(() => { s.vit++; s.pts--; }), s.pts > 0, 80);
            Btn(Row("Geschick <b>" + s.agi + "</b>   <size=15>+1 % Krit, +1,5 % Tempo je Punkt</size>"), "+1", () => Do(() => { s.agi++; s.pts--; }), s.pts > 0, 80);
            Header("Werte");
            Row("Stufe " + s.lvl + "  (" + s.xp + " / " + Game.XpNeed(s.lvl) + " EP)\nAngriff " + st.atk + "  ·  Rüstung " + st.def + "  ·  Leben " + st.hp
                + "\nKritische Treffer " + st.crit + " %  ·  Schläge pro Sekunde " + (1f / st.interval).ToString("0.00")
                + "\nLebensraub " + st.ls + " %  ·  Doppelschlag " + st.dbl + " %");
            Header("Sonstiges");
            Btn(Row("Heimkehr nach Aschenfurt"), "Heimkehren", () =>
            {
                CloseMenu();
                if (PlayerController.I != null) PlayerController.I.Teleport(PlayerController.HomePos);
            }, true, 170);
            Btn(Row(confirmReset ? "<color=#FF4D5E>Wirklich den gesamten Spielstand löschen?</color>" : "Neues Spiel beginnen"),
                confirmReset ? "Ja, löschen" : "Neues Spiel", () =>
                {
                    if (!confirmReset) { confirmReset = true; Rebuild(); return; }
                    Game.NewGame();
                    lastForged = null;
                    GameBootstrap.ResetGates();
                    CloseMenu();
                    if (PlayerController.I != null)
                    {
                        PlayerController.I.Teleport(PlayerController.HomePos);
                        PlayerController.I.hp = Game.GetStats().hp;
                    }
                    Game.Touch();
                }, true, 170);
        }

        void BuildTravelTab()
        {
            var s = Game.S;
            int price = Game.PotionPrice;
            Header("Händlerin Mirla");
            var row = Row("<b>Heiltrank</b>   <size=15>heilt 40 % deines Lebens · du besitzt " + s.potions + "</size>");
            Btn(row, "1 kaufen\n<size=13>" + price + " Gold</size>", () => Do(() => { s.gold -= price; s.potions++; }), s.gold >= price, 150);
            Btn(row, "5 kaufen\n<size=13>" + price * 5 + " Gold</size>", () => Do(() => { s.gold -= price * 5; s.potions += 5; }), s.gold >= price * 5, 150);
            Header("Reisen");
            for (int i = 0; i < Game.ZonesOpen; i++)
            {
                int z = i;
                string state = s.bosses[i] > 0 ? "Boss besiegt (" + s.bosses[i] + "x)" : "Boss: " + Db.BossName[i];
                Btn(Row("<b>" + Db.ZoneName[i] + "</b>   <size=15>Beute: " + Db.MatName[i] + " · " + state + "</size>"), "Reisen", () =>
                {
                    CloseMenu();
                    if (PlayerController.I != null) PlayerController.I.Teleport(new Vector3(0, 1.1f, Db.ZoneStart(z) + 4f));
                }, true, 150);
            }
        }

        // ------------------------------------------------------------ Schmiede-Minispiel
        void BuildForgePanel()
        {
            var panel = Img("Amboss", transform, PanelCol, 0.22f, 0.32f, 0.78f, 0.68f, true);
            forgePanel = panel.gameObject;
            forgeTitle = Txt("Titel", forgePanel.transform, "", 28, TextAnchor.MiddleCenter, 0, 0.78f, 1, 0.98f);
            Txt("Hilfe", forgePanel.transform, "Schlag zu (Leertaste oder Linksklick), wenn der weiße Strich in der hellen Mitte der Glutzone steht.",
                18, TextAnchor.MiddleCenter, 0.05f, 0.6f, 0.95f, 0.78f).color = new Color(0.7f, 0.63f, 0.55f);
            var bar = Img("Leiste", forgePanel.transform, new Color(0.04f, 0.03f, 0.03f), 0.05f, 0.36f, 0.95f, 0.56f).rectTransform;
            zoneRect = Img("Glut", bar, new Color(1f, 0.48f, 0.18f, 0.45f), 0, 0, 1, 1).rectTransform;
            sweetRect = Img("Mitte", bar, new Color(1f, 0.85f, 0.45f, 0.95f), 0, 0, 1, 1).rectTransform;
            markerRect = Img("Strich", bar, Color.white, 0, 0, 0, 1).rectTransform;
            markerRect.sizeDelta = new Vector2(6, 0);
            strikeText = Txt("Schlaege", forgePanel.transform, "", 22, TextAnchor.MiddleCenter, 0.05f, 0.05f, 0.95f, 0.3f);
            forgePanel.SetActive(false);
        }

        void StartForge(int recipe, int tier)
        {
            var s = Game.S;
            var rec = Db.Recipes[recipe];
            if (s.mats[tier] < rec.ore || s.gold < Db.TGold[tier]) return;
            s.mats[tier] -= rec.ore;
            s.gold -= Db.TGold[tier];
            Game.Touch();
            fRec = rec; fTier = tier; fPos = 0f; fDir = 1; fDelay = 0.35f;
            fScores.Clear();
            forging = true;
            menu.SetActive(false);
            forgePanel.SetActive(true);
            forgeTitle.text = Db.MatAdj[tier] + "-" + rec.type + " schmieden";
            NewForgeZone();
        }

        void NewForgeZone()
        {
            fWidth = 0.17f - fTier * 0.015f;
            fCenter = UnityEngine.Random.Range(0.2f, 0.8f);
            fSpeed = 0.75f + fScores.Count * 0.22f + fTier * 0.08f;
            float sweet = fWidth * 0.18f;
            zoneRect.anchorMin = new Vector2(fCenter - fWidth / 2f, 0); zoneRect.anchorMax = new Vector2(fCenter + fWidth / 2f, 1);
            sweetRect.anchorMin = new Vector2(fCenter - sweet, 0); sweetRect.anchorMax = new Vector2(fCenter + sweet, 1);
            var parts = new List<string>();
            for (int i = 0; i < Strikes; i++)
                parts.Add(i < fScores.Count ? (fScores[i] == 100 ? "<color=#FFC247>Perfekt</color>" : fScores[i] + " %") : "Schlag " + (i + 1));
            strikeText.text = string.Join("     ", parts.ToArray());
        }

        void UpdateForge(float dt)
        {
            dt = Mathf.Min(dt, 0.05f);
            fPos += fDir * fSpeed * dt;
            if (fPos > 1f) { fPos = 2f - fPos; fDir = -1; }
            if (fPos < 0f) { fPos = -fPos; fDir = 1; }
            markerRect.anchorMin = new Vector2(fPos, 0);
            markerRect.anchorMax = new Vector2(fPos, 1);
            fDelay -= dt;
            if (fDelay > 0f || !(In.Down(Act.Jump) || In.Down(Act.Attack))) return;

            float d = Mathf.Abs(fPos - fCenter), sweet = fWidth * 0.18f;
            int score = d <= sweet ? 100 : Mathf.Max(0, Mathf.RoundToInt(100f - (d - sweet) / 0.22f * 100f));
            fScores.Add(score);
            fDelay = 0.15f;
            Sfx.Play(Sfx.Clang, score == 100 ? 1f : 0.6f);
            if (fScores.Count < Strikes) { NewForgeZone(); return; }

            float q = 0f;
            foreach (int sc in fScores) q += sc;
            q /= Strikes;
            float roll = q * 0.8f + Game.S.smithLvl * 1.2f + UnityEngine.Random.Range(0f, 20f);
            int rar = 0;
            for (int i = 0; i < Db.RarMin.Length; i++) if (roll >= Db.RarMin[i]) rar = i;
            var item = Db.MakeItem(fRec, fTier, rar, q);
            Game.S.inv.Add(item);
            lastForged = item;
            Game.GainSmithXp(Mathf.RoundToInt((10f + q / 5f) * (fTier + 1)));
            Msg("Geschmiedet: " + item.Title + " [" + Db.Rar[rar] + "]");
            if (rar >= 3) Sfx.Play(Sfx.LevelUp);
            Game.Touch();
            forging = false;
            forgePanel.SetActive(false);
            menu.SetActive(true);
            Rebuild();
        }
    }
}
