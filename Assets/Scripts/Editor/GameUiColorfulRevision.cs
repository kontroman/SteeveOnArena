using System;
using System.IO;
using System.Linq;
using Achievements;
using Devotion.SDK.UI;
using MineArena.UI;
using MineArena.Windows;
using MineArena.Windows.SelectLevel;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Windows;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static partial class GameUiBuilder
    {
        private const string Accents = "Assets/Art/UI/Accents/";
        [MenuItem("MineArena/UI/Refresh Colorful UI")]
        public static void RefreshColorfulUI()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            ApplyColorfulRevision();
            AssetDatabase.SaveAssets();
            RenderAll();
            RenderJournalPreviews();
            Debug.Log("[ColorfulUI] Icons, quest journal, wheel and sensitivity rebuilt.");
        }
        private static void ApplyColorfulRevision()
        {
            Directory.CreateDirectory(Accents);
            AssetDatabase.Refresh();
            foreach (var path in Directory.GetFiles(Accents, "icon-*.png")) ImportSprite(path.Replace('\\', '/'), 128);
            RefineNavigation();
            BuildQuestJournal();
            EnrichWheel();
            ExtendSettings();
            AccentOtherWindows();
        }
        private static Sprite ImportSprite(string path, int size, Vector4 border = default)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.maxTextureSize = size;
            importer.alphaIsTransparency = true; importer.spriteBorder = border; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static Sprite AccentSprite(string name, string fill, string edge)
        {
            string path = Accents + name + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(24, 24, TextureFormat.RGBA32, false);
                var color = C(fill); var dark = C(edge);
                for (int y = 0; y < 24; y++) for (int x = 0; x < 24; x++)
                    texture.SetPixel(x, y, x < 2 || y < 2 || x > 21 || y > 21 ? dark : x == 2 || y == 21 ? Color.Lerp(color, Color.white, 0.4f) : x == 21 || y == 2 ? Color.Lerp(color, dark, 0.35f) : color);
                texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                return ImportSprite(path, 32, new Vector4(4, 4, 4, 4));
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static Sprite IconArt(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Accents + "icon-" + name + ".png");
        private static Sprite Existing(string path) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/" + path);
        private static void Remove(Transform root, string name)
        { var child = root.Find(name); if (child != null) Object.DestroyImmediate(child.gameObject); }

        private static Sprite DialIcon(bool compass)
        {
            string path = Accents + (compass ? "compass" : "clock") + ".png";
            if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                float dx = x - 15.5f, dy = y - 15.5f; float r = Mathf.Sqrt(dx * dx + dy * dy);
                Color color = r > 15 ? Color.clear : r > 13 ? C("624533") : r > 11 ? C("E4AF52") : C("F7EBC9");
                if (compass && Mathf.Abs(dx) < (11 - Mathf.Abs(dy)) * 0.38f) color = dy > 0 ? C("C65443") : C("397987");
                if (!compass && ((Mathf.Abs(dx) < 1.6f && dy >= -1 && dy < 9) || (Mathf.Abs(dy) < 1.6f && dx >= -1 && dx < 7))) color = C("356D79");
                tex.SetPixel(x, y, color);
            }
            tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            return ImportSprite(path, 32);
        }
        private static Button IconNav(Transform parent, string name, GameUiDestination destination, Sprite icon, float x, float y, string tint, bool hintLeft = false, float size = 76)
        {
            var button = Button(destination.ToString(), parent, "", x, y, size, size, false);
            Object.DestroyImmediate(button.GetComponentInChildren<TMP_Text>().gameObject);
            button.GetComponent<Image>().sprite = AccentSprite("nav-" + destination, tint, "736044");
            var art = Image("Icon", button.transform, icon); Box(art.rectTransform, 10, 10, size - 20, size - 20); art.preserveAspect = true;
            SetDestination(button, destination);
            var hint = Panel("Tooltip", button.transform, "card");
            hint.GetComponent<Image>().sprite = AccentSprite("tooltip", "324953", "21313B");
            Box(hint, hintLeft ? -248 : size - 232, hintLeft ? 15 : size + 12, 232, 44);
            var label = Text("Hint", hint, name, 20, 10, 0, 212, 44); label.color = C("FFF1D3"); label.alignment = TextAlignmentOptions.Center;
            hint.gameObject.SetActive(false);
            Set(button.gameObject.AddComponent<IconButtonHint>(), "hint", hint.gameObject);
            return button;
        }
        private static void RefineNavigation()
        {
            var root = Edit<PlayingWindow>(UI + "PlayingWindow.prefab", false);
            foreach (string child in new[] { "Navigation", "RewardsMenu", "Expedition", "Levels", "IconNavigation", "GiftNavigation", "CurrencyPouch" }) Remove(root.transform, child);
            var nav = Panel("IconNavigation", root.transform, "panel");
            nav.anchorMin = nav.anchorMax = nav.pivot = new Vector2(1, 1); nav.anchoredPosition = new Vector2(-28, -24); nav.sizeDelta = new Vector2(448, 96); Outline(nav);
            IconNav(nav, "Рюкзак", GameUiDestination.Inventory, IconArt("backpack"), 12, 10, "F4D9A5");
            IconNav(nav, "Мастерская", GameUiDestination.Crafting, Existing("Icons/Equipment/iron_pickaxe.png"), 98, 10, "B9D5C8");
            IconNav(nav, "Магазин", GameUiDestination.Shop, Existing("Icons/Items/gold_ingot.png"), 184, 10, "E8BF79");
            var questsButton = IconNav(nav, "Журнал заданий", GameUiDestination.Achievements, Existing("UI/knowledge_book.png"), 270, 10, "B4CFDA");
            var badge = Image("ReadyBadge", questsButton.transform, AccentSprite("badge", "D99542", "865427")); Box(badge.rectTransform, 51, -5, 30, 28);
            var badgeText = Text("Count", badge.transform, "", 17, 0, 0, 30, 28, true); badgeText.alignment = TextAlignmentOptions.Center; badgeText.color = Color.white;
            Set(questsButton.gameObject.AddComponent<QuestReadyBadge>(), "badge", badge.gameObject, "amount", badgeText); badge.gameObject.SetActive(false);
            IconNav(nav, "Настройки", GameUiDestination.Settings, IconArt("settings"), 356, 10, "D3CFDD");
            var gifts = Panel("GiftNavigation", root.transform, "panel");
            gifts.anchorMin = gifts.anchorMax = gifts.pivot = new Vector2(1, 1); gifts.anchoredPosition = new Vector2(-28, -204); gifts.sizeDelta = new Vector2(96, 274); Outline(gifts);
            IconNav(gifts, "Подарок за новый день", GameUiDestination.Daily, IconArt("gift"), 10, 10, "E6C1A0", true);
            IconNav(gifts, "Подарок за время игры", GameUiDestination.Playtime, DialIcon(false), 10, 98, "B8D7D2", true);
            IconNav(gifts, "Колесо удачи", GameUiDestination.Wheel, RichWheelSprite(), 10, 186, "DAC9DE", true);
            var wallet = Panel("CurrencyPouch", root.transform, "card");
            wallet.anchorMin = wallet.anchorMax = wallet.pivot = new Vector2(1, 1); wallet.anchoredPosition = new Vector2(-28, -140); wallet.sizeDelta = new Vector2(200, 48);
            var coin = Image("Gold", wallet, Existing("Icons/Items/gold_ingot.png")); Box(coin.rectTransform, 12, 7, 34, 34);
            var amount = Text("Balance", wallet, "0", 23, 60, 0, 128, 48, true);
            Set(root.GetComponent<HudWallet>(), "text", amount);
            var walletSettings = new SerializedObject(root.GetComponent<HudWallet>());
            walletSettings.FindProperty("amountOnly").boolValue = true;
            walletSettings.ApplyModifiedPropertiesWithoutUndo();
            var expedition = IconNav(root.transform, "Выбор экспедиции", GameUiDestination.Levels, DialIcon(true), 0, 0, "9CC7C4", true, 92);
            var er = (RectTransform)expedition.transform; er.anchorMin = er.anchorMax = er.pivot = new Vector2(1, 0); er.anchoredPosition = new Vector2(-28, 30);
            Save(root, UI + "PlayingWindow.prefab");
        }

        private static void Ribbon(RectTransform frame, string color, Sprite icon)
        {
            Remove(frame, "AccentRibbon");
            var ribbon = Image("AccentRibbon", frame, AccentSprite("ribbon-" + color, color, color)); Box(ribbon.rectTransform, 6, 6, frame.sizeDelta.x - 12, 94); ribbon.transform.SetSiblingIndex(1);
            var title = frame.Find("Title").GetComponent<TMP_Text>(); title.color = C("FFF4DA");
            if (icon != null)
            {
                var art = Image("Emblem", ribbon.transform, icon); Box(art.rectTransform, 26, 13, 68, 68); art.preserveAspect = true;
                var rect = title.rectTransform; rect.anchoredPosition = new Vector2(118, -32); rect.sizeDelta = new Vector2(frame.sizeDelta.x - 240, 54);
            }
        }
        private static void BuildQuestJournal()
        {
            var normal = AccentSprite("quest-paper", "F6EDD9", "CCB891");
            var selected = AccentSprite("quest-selected", "DCEAEC", "3D8692");
            string rowPath = "Assets/Resources/UI/QuestJournalRow.prefab";
            var row = Panel("QuestJournalRow", null, "card"); row.sizeDelta = new Vector2(450, 132); row.GetComponent<Image>().sprite = normal;
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 132;
            var button = row.gameObject.AddComponent<Button>(); button.targetGraphic = row.GetComponent<Image>(); button.targetGraphic.raycastTarget = true;
            var stripe = Image("StatusStripe", row, null); Box(stripe.rectTransform, 4, 6, 5, 120);
            var title = Text("Title", row, "", 23, 86, 12, 346, 34);
            var state = Text("State", row, "", 17, 86, 48, 346, 26);
            var iconSlot = Rect("TargetIcon", row); Box(iconSlot, 21, 22, 48, 48);
            var flat = Image("Icon", iconSlot, null); Stretch(flat.rectTransform); flat.preserveAspect = true;
            AddBlock(iconSlot, Vector2.zero, 0.68f); var cube = iconSlot.GetComponentInChildren<ResourceIcon>(true);
            var bar = Slider(row, "ProgressBar", 22, 99, 302, 12, false);
            var barText = Text("Progress", row, "0 / 0", 18, 334, 84, 96, 32); barText.alignment = TextAlignmentOptions.Right;
            var view = row.gameObject.AddComponent<QuestJournalRow>();
            Set(view, "title", title, "status", state, "progressText", barText, "progress", bar, "background", row.GetComponent<Image>(), "stripe", stripe,
                "flatIcon", flat, "blockIcon", cube, "button", button, "normal", normal, "selected", selected);
            var rowPrefab = PrefabUtility.SaveAsPrefabAsset(row.gameObject, rowPath).GetComponent<QuestJournalRow>(); Object.DestroyImmediate(row.gameObject);

            var root = Edit<WindowAchievements>("Assets/Prefabs/Windows/WindowAchievements.prefab");
            foreach (var component in root.GetComponents<AchievementsConstructor>()) Object.DestroyImmediate(component);
            var frame = Window(root, "Журнал заданий", 1320, 860);
            Ribbon(frame, "3B7582", Existing("UI/knowledge_book.png"));
            var summary = Text("Summary", frame, "Ваши цели, прогресс и награды", 21, 36, 115, 1248, 40);
            var filters = new Button[3]; var labels = new TMP_Text[3]; string[] names = { "В работе", "Можно забрать", "Завершено" };
            for (int i = 0; i < 3; i++) { filters[i] = Button("Filter" + i, frame, names[i], 36 + 422 * i, 169, 405, 52, false); labels[i] = filters[i].GetComponentInChildren<TMP_Text>(); }
            var scroll = Scroll(frame, "QuestList", 36, 249, 454, 571);
            var layout = scroll.content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 12; layout.childControlWidth = true; layout.childControlHeight = false; layout.childForceExpandHeight = false;
            var empty = Text("EmptyState", frame, "", 22, 56, 302, 404, 250); empty.enableWordWrapping = true;
            var detail = Panel("QuestDetails", frame, "card"); Box(detail, 516, 249, 768, 571);
            detail.GetComponent<Image>().sprite = normal;
            var seal = Image("Seal", detail, Existing("UI/knowledge_book.png")); Box(seal.rectTransform, 645, 20, 80, 80); seal.color = new Color(1, 1, 1, 0.22f); seal.preserveAspect = true;
            var detailTitle = Text("QuestName", detail, "Выберите поручение", 29, 28, 25, 600, 52, true);
            var difficulty = Text("Difficulty", detail, "", 19, 28, 78, 710, 28);
            var description = Text("Description", detail, "", 23, 28, 112, 710, 76); description.enableWordWrapping = true;
            var target = Object.Instantiate(Chip, detail); Box((RectTransform)target.transform, 28, 198, 400, 52);
            var progressLabel = Text("ProgressText", detail, "0 / 0", 24, 586, 208, 150, 42, true); progressLabel.alignment = TextAlignmentOptions.Right;
            var progress = Slider(detail, "ProgressBar", 28, 270, 708, 20, false);
            Text("RewardHeading", detail, "ВАША НАГРАДА", 19, 28, 299, 450, 30, true).color = C("A87132");
            var reward = Object.Instantiate(Chip, detail); Box((RectTransform)reward.transform, 28, 341, 338, 68);
            var claimStatus = Text("ClaimStatus", detail, "", 18, 28, 426, 708, 36);
            var claim = Button("Claim", detail, "Забрать награду", 28, 481, 708, 58);
            claim.GetComponent<Image>().sprite = AccentSprite("button-teal", "3D8990", "28555C");
            var window = root.GetComponent<WindowAchievements>();
            Set(window, "rowsRoot", scroll.content, "rowPrefab", rowPrefab, "summary", summary, "empty", empty, "details", detail.gameObject,
                "detailTitle", detailTitle, "description", description, "progressText", progressLabel, "progress", progress,
                "reward", reward, "target", target, "claim", claim, "claimStatus", claimStatus, "normalTab", S("card"), "selectedTab", selected, "difficulty", difficulty, "detailIcon", seal);
            var so = new SerializedObject(window);
            var fb = so.FindProperty("filters"); var fl = so.FindProperty("filterLabels"); fb.arraySize = fl.arraySize = 3;
            for (int i = 0; i < 3; i++) { fb.GetArrayElementAtIndex(i).objectReferenceValue = filters[i]; fl.GetArrayElementAtIndex(i).objectReferenceValue = labels[i]; }
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(root, "Assets/Prefabs/Windows/WindowAchievements.prefab");
        }
        private static Sprite RichWheelSprite()
        {
            string path = Accents + "festival-wheel.png";
            if (File.Exists(path)) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            Color[] palette = { C("987CB6"), C("6BAFA9"), C("D88973"), C("E2B75C"), C("768CB6"), C("91BCC8") };
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
            {
                float dx = x - 127.5f, dy = y - 127.5f, radius = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Repeat(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg + 30, 360);
                int sector = Mathf.FloorToInt(angle / 60);
                Color color = radius > 125 ? Color.clear : radius > 122 ? C("694633") : radius > 117 ? C("E4AF50") : radius > 113 ? C("FFE2A0") : palette[sector];
                if (radius < 110 && Mathf.Min(angle % 60, 60 - angle % 60) * radius < 95) color = C("ECCC83");
                if (radius > 115 && radius < 122 && Mathf.Abs(Mathf.Repeat(angle + 7.5f, 15) - 7.5f) < 1.2f) color = C("FFF5CF");
                if (radius < 20) color = radius > 16 ? C("694633") : C("E6B357");
                tex.SetPixel(x, y, color);
            }
            tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            return ImportSprite(path, 256);
        }
        private static void EnrichWheel()
        {
            var root = Edit<FortuneWheelWindow>(UI + "FortuneWheelWindow.prefab", false);
            var frame = (RectTransform)root.transform.Find("BeigeWindow");
            Ribbon(frame, "665177", RichWheelSprite());
            Find(root.transform, "BackgroundWheel").GetComponent<Image>().sprite = RichWheelSprite();
            var spin = Find(root.transform, "Start").GetComponent<Image>(); spin.sprite = AccentSprite("button-coral", "C77950", "805036");
            Remove(frame, "WheelBalance");
            var balancePanel = Panel("WheelBalance", frame, "card"); Box(balancePanel, 756, 196, 520, 38);
            var money = Image("Gold", balancePanel, Existing("Icons/Items/gold_ingot.png")); Box(money.rectTransform, 12, 5, 28, 28);
            var balance = Text("Balance", balancePanel, "Золотая руда: 0", 19, 54, 0, 450, 38);
            Set(root.GetComponent<HudWallet>() ?? root.AddComponent<HudWallet>(), "catalog", AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath), "text", balance);
            for (int i = 0; i < 4; i++)
            {
                int count = new[] { 1, 3, 5, 10 }[i]; var button = Find(root.transform, "BuySpin" + count).GetComponent<Button>();
                button.GetComponent<Image>().sprite = AccentSprite("wheel-buy-" + i, new[] { "D9E7D9", "CDE3E0", "DBD5E7", "ECD6A6" }[i], "B3A382");
            }
            var pointer = Find(root.transform, "Pointer").GetComponent<TMP_Text>(); pointer.color = C("D2983F");
            var hint = Find(root.transform, "Hint").GetComponent<TMP_Text>(); hint.text = "Крутите колесо и собирайте добычу!\nПредметы попадут в инвентарь.";
            Save(root, UI + "FortuneWheelWindow.prefab");
        }
        private static void ExtendSettings()
        {
            // Rebuild this window only; inventory and crafting retain the approved layout.
            var root = Edit<SettingsWindow>(UI + "SettingsWindow.prefab");
            var frame = Window(root, "Настройки", 1040, 820);
            Text("Intro", frame, "Звук и управление камерой", 22, 38, 112, 960, 38);
            var master = SettingSlider(frame, "Общая громкость", 185);
            var music = SettingSlider(frame, "Музыка", 272);
            var effects = SettingSlider(frame, "Звуковые эффекты", 359);
            var line = Image("DividerControls", frame, null); line.color = C("CDB88E"); Box(line.rectTransform, 38, 434, 964, 2);
            Text("ControlsTitle", frame, "КАМЕРА", 20, 38, 455, 780, 32, true).color = C("397987");
            Text("SensitivityLabel", frame, "Чувствительность\nприближения", 21, 38, 505, 335, 62).enableWordWrapping = true;
            var sensitivity = Slider(frame, "Sensitivity", 390, 513, 540, 40, true);
            sensitivity.minValue = CameraSensitivity.Minimum; sensitivity.maxValue = CameraSensitivity.Maximum; sensitivity.value = 1;
            var value = Text("SensitivityValue", frame, "1.00×", 18, 933, 513, 90, 40);
            Text("SensitivityHint", frame, "Скорость приближения камеры колёсиком мыши. 1× — обычная.", 19, 38, 585, 964, 42).enableWordWrapping = true;
            var hint = Panel("Controls", frame, "inset"); Box(hint, 38, 662, 964, 114);
            Text("Keys", hint, "WASD — движение   •   1–5 — быстрые слоты\nНаведите курсор на иконку меню, чтобы увидеть подсказку.", 20, 20, 19, 920, 76).enableWordWrapping = true;
            Set(root.GetComponent<SettingsWindow>(), "master", master, "music", music, "effects", effects, "sensitivity", sensitivity, "sensitivityValue", value);
            Save(root, UI + "SettingsWindow.prefab");
        }
        private static void AccentOtherWindows()
        {
            string[] windows = { "ShopWindow", "DailyGiftWindow", "PlaytimeGiftWindow", "LevelCompleteWindow" };
            string[] colors = { "9B6949", "BB6190", "7770B6", "65794C" };
            for (int i = 0; i < windows.Length; i++)
            {
                var path = UI + windows[i] + ".prefab"; var root = PrefabUtility.LoadPrefabContents(path);
                var frame = (RectTransform)root.transform.Find("BeigeWindow");
                Sprite icon = i == 0 ? Existing("Icons/Items/gold_ingot.png") : i == 3 ? DialIcon(true) : IconArt("gift");
                Ribbon(frame, colors[i], icon);
                PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
            }
        }
        private static void RenderJournalPreviews()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/WindowAchievements.prefab"), scene);
                var window = root.GetComponent<WindowAchievements>();
                var config = AssetDatabase.LoadAssetAtPath<MineArena.Structs.GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
                var quests = config.DataAchievements.Select((data, index) => new Achievement(data, index)).ToList();
                for (int i = 0; i < quests.Count; i++) quests[i].LoadData(new AchievementSaveData(i, i == 0 ? quests[i].MaxValueProgress : i == 2 ? quests[i].MaxValueProgress : 1, i == 2, i == 0));
                window.Bind(quests); window.SetFilter(0);
                Render(root, "Documentation/UI/WindowAchievements.png");
                window.SetFilter(1); Render(root, "Documentation/UI/Quests-ready.png");
                window.SetFilter(2); Render(root, "Documentation/UI/Quests-completed.png");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
