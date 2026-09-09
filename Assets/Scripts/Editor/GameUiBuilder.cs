using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Devotion.SDK.Base;
using Devotion.SDK.DailyReward;
using Devotion.SDK.Managers;
using Devotion.SDK.UI;
using MineArena.Items;
using MineArena.Structs;
using MineArena.UI;
using MineArena.Windows;
using MineArena.Windows.Crafting;
using MineArena.Windows.InfoPopup;
using MineArena.Windows.SelectLevel;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static partial class GameUiBuilder
    {
        private const string UI = "Assets/DevotionSDK/Prefabs/UI/";
        private const string Art = "Assets/Art/UI/Expedition/";
        private const string CatalogPath = "Assets/Resources/UI/ShopCatalog.asset";
        private const string GiftsPath = "Assets/Resources/UI/PlaytimeRewards.asset";
        private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset");
        private static TMP_FontAsset Pixel => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset");
        private static Color Ink => C("514737");
        private static Color Muted => C("85775E");
        private static Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + "beige-" + name + ".png");
        private static LevelResourceChip Chip => AssetDatabase.LoadAssetAtPath<LevelResourceChip>("Assets/Prefabs/Windows/SelectLevelWindow/ResourceChip.prefab");

        [MenuItem("MineArena/UI/Rebuild All Beige Windows")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Directory.CreateDirectory("Assets/Resources/UI");
            AssetDatabase.Refresh();
            var catalog = Catalog();
            foreach (var offer in catalog.Offers)
            {
                string label = offer.Item.Name == "IronSword" ? "Железный меч" : offer.Item.Name == "IronHelmet" ? "Железный шлем" : offer.Item.Name == "IronChestplate" ? "Железный нагрудник" : null;
                if (label == null) continue;
                var item = new SerializedObject(offer.Item); var display = item.FindProperty("_displayName");
                if (string.IsNullOrEmpty(display.stringValue)) { display.stringValue = label; item.ApplyModifiedPropertiesWithoutUndo(); }
            }
            BuildShop(catalog);
            BuildSettings();
            BuildPlaytime();
            BuildDaily();
            BuildInventory();
            BuildPriceElement();
            BuildBuilding();
            BuildInfo();
            BuildComplete();
            BuildLoading();
            BuildHud(catalog);
            BuildWheel(catalog);
            RestyleExisting("Assets/Prefabs/Windows/WindowAchievements.prefab");
            BuildLevelProgress();
            BuildAchievementPopup();
            RestyleExisting(UI + "GodModeWindow.prefab");
            BuildCraftItem("Assets/Resources/Prefabs/Windows/Crafting/CraftingItemView.prefab");
            BuildCraftItem("Assets/Prefabs/Windows/Crafting/CraftingItemView.prefab");
            RestyleCrafting("Assets/Resources/Prefabs/Windows/Crafting/CraftingWindow.prefab");
            RestyleCrafting("Assets/Prefabs/Windows/Crafting/CraftingWindow.prefab");
            RestyleElementPrefabs();
            RegisterWindows();
            ApplyColorfulRevision();
            AssetDatabase.SaveAssets();
            RenderAll();
            RenderJournalPreviews();
            Debug.Log("[GameUI] All windows rebuilt, registered and rendered.");
        }

        private static ShopCatalog Catalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>(CatalogPath);
            if (catalog != null) return catalog;
            catalog = ScriptableObject.CreateInstance<ShopCatalog>();
            catalog.Currency = Item("Assets/ScriptableObjects/Configs/Drops/GoldOre.asset");
            AddOffer(catalog, "Assets/ScriptableObjects/Configs/Drops/Stone.asset", 32, 2);
            AddOffer(catalog, "Assets/ScriptableObjects/Configs/Drops/WoodOak.asset", 24, 2);
            AddOffer(catalog, "Assets/ScriptableObjects/Configs/Drops/WoodBirch.asset", 24, 2);
            AddOffer(catalog, "Assets/ScriptableObjects/Configs/Drops/CoalItem.asset", 12, 3);
            AddOffer(catalog, "Assets/ScriptableObjects/Configs/Drops/IronOre.asset", 8, 4);
            AddOffer(catalog, "Assets/ScriptableObjects/Configs/Drops/DiamondOre.asset", 2, 12);
            AddOffer(catalog, "Assets/ScriptableObjects/Configs/Equipment/Swords/IronSwordItem.asset", 1, 20);
            AddOffer(catalog, "Assets/ScriptableObjects/Configs/Equipment/Armor/IronHelmet.asset", 1, 16);
            AddOffer(catalog, "Assets/ScriptableObjects/Configs/Equipment/Armor/IronChestplate.asset", 1, 30);
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }
        private static ItemConfig Item(string path) => AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
        private static void AddOffer(ShopCatalog catalog, string path, int amount, int price)
        {
            var item = Item(path);
            if (item != null) catalog.Offers.Add(new ShopOffer { Item = item, Amount = amount, Price = price });
        }

        private static void BuildShop(ShopCatalog catalog)
        {
            string offerPath = "Assets/Resources/UI/ShopOffer.prefab";
            var offerRoot = Panel("ShopOffer", null, "card");
            offerRoot.sizeDelta = new Vector2(365, 215);
            var item = Object.Instantiate(Chip, offerRoot);
            Box((RectTransform)item.transform, 20, 18, 325, 68);
            var price = Text("Price", offerRoot, "Цена", 20, 20, 103, 325, 30);
            var buy = Button("Buy", offerRoot, "Купить", 20, 152, 325, 44);
            var offerView = offerRoot.gameObject.AddComponent<ShopOfferView>();
            Set(offerView, "item", item, "price", price, "status", buy.GetComponentInChildren<TMP_Text>(), "buy", buy);
            var saved = PrefabUtility.SaveAsPrefabAsset(offerRoot.gameObject, offerPath);
            Object.DestroyImmediate(offerRoot.gameObject);
            var root = Edit<ShopWindow>(UI + "ShopWindow.prefab");
            var frame = Window(root, "Лавка торговца", 1240, 910);
            var balance = Text("Balance", frame, catalog.Currency.DisplayName + ": 0", 23, 36, 108, 1150, 36);
            var scroll = Scroll(frame, "Offers", 34, 166, 1172, 626);
            var grid = scroll.content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(365, 215); grid.spacing = new Vector2(15, 16);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 3;
            var feedback = Text("Feedback", frame, "Покупки сразу попадают в инвентарь", 20, 36, 825, 1150, 42);
            var window = root.GetComponent<ShopWindow>();
            Set(window, "catalog", catalog, "offersRoot", scroll.content, "offerPrefab", saved.GetComponent<ShopOfferView>(), "balance", balance, "feedback", feedback);
            // Serialized examples make the prefab reviewable without starting gameplay.
            foreach (var offer in catalog.Offers)
                Object.Instantiate(saved.GetComponent<ShopOfferView>(), scroll.content).Bind(offer, catalog.Currency.DisplayName, false, true, () => { });
            Save(root, UI + "ShopWindow.prefab");
        }

        private static void BuildSettings()
        {
            var root = Edit<SettingsWindow>(UI + "SettingsWindow.prefab");
            var frame = Window(root, "Настройки", 980, 740);
            Text("Intro", frame, "Настройте звук под себя", 22, 38, 110, 900, 38);
            var master = SettingSlider(frame, "Общая громкость", 192);
            var music = SettingSlider(frame, "Музыка", 288);
            var effects = SettingSlider(frame, "Звуковые эффекты", 384);
            var controls = Panel("Controls", frame, "inset"); Box(controls, 38, 510, 904, 172);
            Text("Title", controls, "УПРАВЛЕНИЕ", 21, 20, 14, 850, 32, true);
            Text("Keys", controls, "WASD — движение     1–5 — быстрые слоты\nПеретащите предмет в слот, чтобы экипировать его.\nКнопки главного интерфейса открывают окна игры.", 21, 20, 53, 850, 100).enableWordWrapping = true;
            Set(root.GetComponent<SettingsWindow>(), "master", master, "music", music, "effects", effects);
            Save(root, UI + "SettingsWindow.prefab");
        }
        private static Slider SettingSlider(Transform frame, string title, float y)
        {
            Text(title, frame, title, 22, 38, y, 330, 40);
            return Slider(frame, title + "Slider", 390, y, 540, 40, true);
        }

        private static void BuildPlaytime()
        {
            var config = AssetDatabase.LoadAssetAtPath<PlaytimeRewardsConfig>(GiftsPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PlaytimeRewardsConfig>();
                config.Rewards.Add(new PlaytimeReward { Minutes = 5, Item = Item("Assets/ScriptableObjects/Configs/Drops/WoodOak.asset"), Amount = 16 });
                config.Rewards.Add(new PlaytimeReward { Minutes = 15, Item = Item("Assets/ScriptableObjects/Configs/Drops/Stone.asset"), Amount = 32 });
                config.Rewards.Add(new PlaytimeReward { Minutes = 30, Item = Item("Assets/ScriptableObjects/Configs/Drops/GoldOre.asset"), Amount = 5 });
                AssetDatabase.CreateAsset(config, GiftsPath);
            }
            BuildPlaytimeCards(config);
        }

        private static void BuildDaily()
        {
            var root = Edit<DailyGiftWIndow>(UI + "DailyGiftWindow.prefab");
            var frame = Window(root, "Ежедневный подарок", 1240, 820, false);
            PartyFrame(frame, "BB6190");
            var window = root.GetComponent<DailyGiftWIndow>();
            var title = frame.Find("Title").GetComponent<TMP_Text>();
            Text("Intro", frame, "Заходите каждый день — собирайте всю неделю наград", 23, 36, 118, 1160, 44);
            var scroller = Scroll(frame, "Days", 36, 190, 1168, 286);
            scroller.horizontal = true; scroller.vertical = false;
            scroller.content.sizeDelta = new Vector2(0, 286);
            scroller.content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            scroller.content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var horizontal = scroller.content.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 12; horizontal.childForceExpandWidth = false; horizontal.childForceExpandHeight = false;
            horizontal.childControlWidth = true; horizontal.childControlHeight = true;
            var config = AssetDatabase.LoadAssetAtPath<DailyRewardConfig>("Assets/ScriptableObjects/Configs/Game/DailyRewardsConfig.asset");
            var so = new SerializedObject(window); var slots = so.FindProperty("rewardSlots");
            slots.arraySize = config != null ? config.RewardsCount : 7;
            for (int i = 0; i < slots.arraySize; i++)
            {
                var slot = Panel("Day" + (i + 1), scroller.content, "card");
                slot.GetComponent<Image>().color = i == 0 ? C("FFE3A0") : Color.white;
                var le = slot.gameObject.AddComponent<LayoutElement>(); le.minWidth = le.preferredWidth = 150; le.minHeight = le.preferredHeight = 270;
                DecorateDay(slot, i);
                var day = Text("DayText", slot, "День " + (i + 1), 22, 10, 12, 130, 32, true); day.alignment = TextAlignmentOptions.Center; day.color = Color.white;
                var icon = Image("Icon", slot, null); Box(icon.rectTransform, 38, 55, 74, 74); icon.preserveAspect = true;
                var entry = config != null ? config.GetReward(i) : null; icon.sprite = entry?.Icon;
                AddBlock(slot, new Vector2(0, 40), 0.85f);
                var block = slot.GetComponentInChildren<ResourceIcon>(true);
                bool cube = entry?.ItemConfig is StackableItemConfig && entry.ItemConfig.BlockStyleIcon;
                block.gameObject.SetActive(cube); if (cube) block.SetResource((StackableItemConfig)entry.ItemConfig);
                icon.gameObject.SetActive(!cube);
                var amount = Text("AmountText", slot, entry != null ? "×" + entry.Amount : "", 24, 10, 145, 130, 38, true); amount.alignment = TextAlignmentOptions.Center;
                var itemName = Text("ItemName", slot, entry?.DisplayName ?? "", 16, 8, 180, 134, 34); itemName.alignment = TextAlignmentOptions.Center; itemName.enableWordWrapping = true;
                var state = Text("Status", slot, "", 13, 5, 244, 140, 20, true); state.alignment = TextAlignmentOptions.Center;
                var element = slots.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("statusText").objectReferenceValue = state;
                element.FindPropertyRelative("itemText").objectReferenceValue = itemName;
                element.FindPropertyRelative("root").objectReferenceValue = slot.gameObject;
                element.FindPropertyRelative("background").objectReferenceValue = slot.GetComponent<Image>();
                element.FindPropertyRelative("icon").objectReferenceValue = icon;
                element.FindPropertyRelative("blockIcon").objectReferenceValue = block;
                element.FindPropertyRelative("dayText").objectReferenceValue = day;
                element.FindPropertyRelative("amountText").objectReferenceValue = amount;
            }
            so.FindProperty("claimedSlotColor").colorValue = C("D4F3E3");
            so.FindProperty("currentSlotColor").colorValue = C("FFE3A0");
            so.FindProperty("futureSlotColor").colorValue = Color.white;
            so.ApplyModifiedPropertiesWithoutUndo();
            var rewardIcon = Image("CurrentReward", frame, config != null ? config.GetReward(0)?.Icon : null); Box(rewardIcon.rectTransform, 480, 540, 64, 64);
            AddBlock(frame, new Vector2(-108, -162), 0.85f);
            var currentBlock = frame.GetComponentsInChildren<ResourceIcon>(true).Last();
            var firstItem = config != null ? config.GetReward(0)?.ItemConfig : null;
            bool currentCube = firstItem is StackableItemConfig && firstItem.BlockStyleIcon;
            currentBlock.gameObject.SetActive(currentCube); if (currentCube) currentBlock.SetResource((StackableItemConfig)firstItem);
            rewardIcon.gameObject.SetActive(!currentCube);
            Set(window, "rewardBlockIcon", currentBlock);
            var rewardText = Text("RewardText", frame, "Награда за новый день", 25, 565, 547, 560, 48);
            var claim = Button("ClaimButton", frame, "Забрать", 360, 678, 520, 62);
            claim.GetComponent<Image>().sprite = AccentSprite("daily-claim", "E8BB59", "BC8435");
            var close = Button("CloseButton", frame, "×", 1142, 32, 60, 60, false);
            Set(window, "rewardsRoot", scroller.content, "rewardIcon", rewardIcon, "titleText", title, "rewardText", rewardText, "claimButton", claim, "closeButton", close);
            Save(root, UI + "DailyGiftWindow.prefab");
        }

        private static void BuildInventory()
        {
            var root = Edit<InventoryWindow>(UI + "InventoryWindow.prefab", false);
            var window = root.GetComponent<InventoryWindow>();
            var so = new SerializedObject(window);
            string[] fields = { "_inventoryGrid", "_equipHelmet", "_equipChest", "_equipLeggins", "_equipBoots", "_playerPreview" };
            var kept = fields.Select(f => ((Component)so.FindProperty(f).objectReferenceValue).transform).ToArray();
            Keep(root, kept);
            var frame = Window(root, "Инвентарь", 1280, 820);
            foreach (var t in kept) t.SetParent(frame, false);
            Text("EquipmentTitle", frame, "СНАРЯЖЕНИЕ", 22, 38, 116, 450, 34, true);
            Text("BagTitle", frame, "РЮКЗАК", 22, 536, 116, 690, 34, true);
            Box((RectTransform)kept[0], 536, 170, 704, 486);
            var grid = kept[0].GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(90, 86); grid.spacing = new Vector2(10, 12); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 7;
            for (int i = 1; i <= 4; i++) Box((RectTransform)kept[i], 38, 181 + (i - 1) * 114, 90, 90);
            Box((RectTransform)kept[5], 154, 170, 332, 490);
            foreach (var raw in kept[5].GetComponentsInChildren<RawImage>(true)) if (raw.texture == null) raw.color = Color.clear;
            Text("Hint", frame, "Перетаскивайте предметы в слоты экипировки и на панель быстрого доступа.", 21, 38, 704, 1190, 60).enableWordWrapping = true;
            Skin(root, false);
            Save(root, UI + "InventoryWindow.prefab");
        }

        private static void BuildPriceElement()
        {
            const string path = "Assets/Prefabs/Windows/BuildingWindow/PriceElement.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(235, 68);
            var image = root.GetComponent<Image>() ?? root.AddComponent<Image>(); image.sprite = S("slot"); image.type = UnityEngine.UI.Image.Type.Sliced;
            var icon = Image("Icon", rect, null); Box(icon.rectTransform, 8, 8, 52, 52); icon.preserveAspect = true;
            AddBlock(rect, new Vector2(-85, 0), 0.52f); var block = rect.GetComponentInChildren<ResourceIcon>(true);
            var blockRect = (RectTransform)block.transform;
            blockRect.anchorMin = blockRect.anchorMax = new Vector2(0, 1);
            blockRect.anchoredPosition = new Vector2(34, -34);
            var label = Text("Name", rect, "", 16, 70, 6, 188, 28);
            var amount = Text("Amount", rect, "", 21, 70, 36, 188, 26, true);
            Set(root.GetComponent<MineArena.Windows.Elements.BuildingPriceElement>(), "_resourceIcon", icon, "_amountText", amount, "_blockIcon", block, "_nameText", label);
            PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
        }

        private static void BuildBuilding()
        {
            var root = Edit<BuildingWindow>(UI + "BuildingWindow.prefab");
            var window = root.GetComponent<BuildingWindow>();
            var frame = Window(root, "Строительство", 1280, 790);
            Ribbon(frame, "587F79", Existing("item/iron_pickaxe.png"));
            var previewPanel = Panel("BuildingPreviewPanel", frame, "card"); Box(previewPanel, 32, 124, 338, 498);
            var name = Text("BuildingName", previewPanel, "Выберите здание", 29, 24, 18, 290, 68, true);
            var preview = Image("BuildingPreview", previewPanel, null); Box(preview.rectTransform, 18, 94, 302, 302); preview.preserveAspect = true; preview.raycastTarget = false;
            var caption = Text("PreviewCaption", previewPanel, "Внешний вид здания", 20, 24, 405, 290, 72); caption.alignment = TextAlignmentOptions.Center;
            var costPanel = Panel("ResourcesPanel", frame, "card"); Box(costPanel, 388, 124, 860, 242);
            Text("CostsTitle", costPanel, "НУЖНЫ РЕСУРСЫ", 23, 22, 18, 816, 36, true).color = C("A66D3D");
            var costs = Scroll(costPanel, "Costs", 22, 74, 816, 158).content; Grid(costs, 3, new Vector2(260, 68));
            var unlockPanel = Panel("UnlocksPanel", frame, "card"); Box(unlockPanel, 388, 382, 860, 240);
            Text("UnlocksTitle", unlockPanel, "ОТКРЫВАЕТ", 23, 22, 18, 816, 36, true).color = C("587F79");
            var unlocks = Scroll(unlockPanel, "Unlocks", 22, 74, 816, 158).content; Grid(unlocks, 3, new Vector2(260, 68));
            var empty = Text("NoUnlocks", unlockPanel, "У этого здания нет новых рецептов.", 21, 22, 88, 816, 80); empty.gameObject.SetActive(false);
            var footer = Panel("BuildActionPanel", frame, "card"); Box(footer, 32, 644, 1216, 112);
            var build = Button("Build", footer, "Построить", 724, 25, 468, 62);
            build.GetComponent<Image>().sprite = AccentSprite("building-action", "628B78", "365B50");
            var feedback = Text("Feedback", footer, "Выберите здание на участке", 22, 24, 22, 672, 68);
            UnityEventTools.AddPersistentListener(build.onClick, window.OnTryBuildClick);
            Set(window, "_buildingName", name, "_priceTransform", costs, "_opensTransform", unlocks, "_buildButton", build, "_feedback", feedback, "_preview", preview, "_previewCaption", caption, "_unlocksEmpty", empty);
            Save(root, UI + "BuildingWindow.prefab");
        }

        private static void BuildInfo()
        {
            var root = Edit<InfoPopupWindow>(UI + "InfoPopupWindow.prefab");
            var frame = Window(root, "Награда получена", 840, 560, false);
            var title = frame.Find("Title").GetComponent<TMP_Text>();
            var plate = Panel("RewardPlate", frame, "selected"); Box(plate, 352, 162, 136, 136);
            var icon = Image("Icon", plate, null); Box(icon.rectTransform, 20, 20, 96, 96); icon.preserveAspect = true;
            var description = Text("Description", frame, "Ваши ресурсы ждут в инвентаре", 24, 36, 329, 768, 54); description.alignment = TextAlignmentOptions.Center;
            var ok = Button("OK", frame, "Отлично!", 220, 432, 400, 62);
            Set(root.GetComponent<InfoPopupWindow>(), "_titleText", title, "_descriptionText", description, "_iconImage", icon, "_okButton", ok);
            Save(root, UI + "InfoPopupWindow.prefab");
        }

        private static void BuildComplete()
        {
            var root = Edit<LevelCompleteWindow>(UI + "LevelCompleteWindow.prefab");
            var frame = Window(root, "Экспедиция завершена", 1100, 760, false);
            Text("Subtitle", frame, "Добыча и награда за прохождение", 24, 36, 121, 1028, 40);
            var rewards = Scroll(frame, "Rewards", 36, 198, 1028, 360).content; Grid(rewards, 4, new Vector2(235, 68));
            var proceed = Button("Continue", frame, "Забрать и вернуться", 36, 642, 498, 62);
            var twice = Button("Double", frame, "Удвоить • реклама", 564, 642, 498, 62, false);
            Text("Hint", frame, "Награды добавятся в инвентарь при возвращении", 20, 36, 583, 1028, 35);
            Set(root.GetComponent<LevelCompleteWindow>(), "_titleText", frame.Find("Title").GetComponent<TMP_Text>(), "_resourcesRoot", rewards,
                "_continueButton", proceed, "_doubleRewardsButton", twice, "_rewardPrefab", Chip);
            Save(root, UI + "LevelCompleteWindow.prefab");
        }

        private static void BuildLoading()
        {
            var root = Edit<LoadingWindow>(UI + "LoadingWindow.prefab");
            var frame = Window(root, "Готовим экспедицию", 1220, 700, false);
            var image = Image("Landscape", frame, AssetDatabase.LoadAssetAtPath<Sprite>(Art + "expedition-landscape-v2.png"));
            Box(image.rectTransform, 36, 138, 1148, 345);
            var progress = Slider(frame, "Progress", 90, 550, 1040, 36, false);
            Text("Hint", frame, "Загружаем мир и размещаем ресурсы…", 23, 90, 604, 1040, 42);
            Set(root.GetComponent<LoadingWindow>(), "_progressBar", progress);
            Save(root, UI + "LoadingWindow.prefab");
        }

        private static void BuildHud(ShopCatalog catalog)
        {
            var root = Edit<PlayingWindow>(UI + "PlayingWindow.prefab", false);
            var player = Find(root.transform, "PlayerPanel"); var hotbar = Find(root.transform, "InventoryPanel");
            var popup = Find(root.transform, "AchievementPopup");
            Keep(root, new[] { player, hotbar, popup }.Where(t => t != null).ToArray());
            Stretch((RectTransform)root.transform);
            if (root.GetComponent<Image>() != null) Object.DestroyImmediate(root.GetComponent<Image>());
            Skin(root, false);
            if (player != null)
            {
                var rect = (RectTransform)player; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(26, -24);
                rect.sizeDelta = new Vector2(438, 170);
                var background = player.GetComponent<Image>() ?? player.gameObject.AddComponent<Image>(); background.sprite = S("panel"); background.type = UnityEngine.UI.Image.Type.Sliced; background.raycastTarget = false;
                Outline(rect); Brick(rect, 0.3f);
                var portrait = Find(player, "PlayerIcon"); Box((RectTransform)portrait, 14, 14, 100, 100);
                portrait.GetComponent<Image>().sprite = S("slot");
                var level = Find(portrait, "LvLIcon"); Box((RectTransform)level, 25, 102, 50, 34); level.GetComponent<Image>().sprite = S("selected"); level.GetComponent<Image>().color = Color.white;
                var name = Find(player, "PlayerName"); Box((RectTransform)name, 134, 12, 284, 34); name.GetComponent<TMP_Text>().fontSize = 24;
                var stats = Find(player, "PlayerStats"); Box((RectTransform)stats, 134, 53, 280, 74); stats.GetComponent<Image>().color = Color.clear;
                foreach (string barName in new[] { "HealthBar", "ArmorBar" })
                {
                    var bar = Find(stats, barName); Box((RectTransform)bar, 0, barName == "HealthBar" ? 0 : 39, 280, 30);
                    bar.GetComponent<Image>().sprite = S("inset");
                    foreach (var fill in bar.GetComponentsInChildren<Image>())
                        if (fill.type == UnityEngine.UI.Image.Type.Filled) { fill.sprite = TextureSprite(); fill.color = barName == "HealthBar" ? C("AC6249") : C("869388"); }
                    foreach (var text in bar.GetComponentsInChildren<TMP_Text>()) { text.color = C("FFFAE9"); text.fontSize = 18; }
                }
                var xp = Find(player, "LevelProgressBar"); Box((RectTransform)xp, 134, 140, 280, 10); xp.GetComponent<Image>().sprite = S("inset");
                var xpFill = Find(xp, "LevelProgressBarFilled").GetComponent<Image>(); xpFill.sprite = TextureSprite(); xpFill.color = C("8B9B54");
            }
            var nav = Panel("Navigation", root.transform, "panel");
            nav.anchorMin = nav.anchorMax = nav.pivot = new Vector2(1, 1); nav.sizeDelta = new Vector2(770, 70); nav.anchoredPosition = new Vector2(-28, -24);
            Outline(nav); Brick(nav, 0.3f);
            ActionButton(nav, "Рюкзак", GameUiDestination.Inventory, 12, 12, 140);
            ActionButton(nav, "Крафт", GameUiDestination.Crafting, 164, 12, 140);
            ActionButton(nav, "Магазин", GameUiDestination.Shop, 316, 12, 140);
            ActionButton(nav, "Задания", GameUiDestination.Achievements, 468, 12, 140);
            ActionButton(nav, "Звук", GameUiDestination.Settings, 620, 12, 136);
            var side = Panel("RewardsMenu", root.transform, "panel");
            side.anchorMin = side.anchorMax = side.pivot = new Vector2(1, 1); side.sizeDelta = new Vector2(238, 306); side.anchoredPosition = new Vector2(-28, -120); Outline(side); Brick(side, 0.3f);
            Text("Heading", side, "НАГРАДЫ", 21, 16, 14, 206, 32, true);
            ActionButton(side, "За новый день", GameUiDestination.Daily, 14, 62, 210);
            ActionButton(side, "За время игры", GameUiDestination.Playtime, 14, 122, 210);
            ActionButton(side, "Колесо удачи", GameUiDestination.Wheel, 14, 182, 210);
            var balance = Text("Wallet", side, catalog.Currency.DisplayName + ": 0", 18, 14, 246, 210, 38);
            Set(root.GetComponent<HudWallet>() ?? root.AddComponent<HudWallet>(), "catalog", catalog, "text", balance);
            if (root.GetComponent<PlaytimeRewardClock>() == null) root.AddComponent<PlaytimeRewardClock>();
            var expedition = Button("Expedition", root.transform, "Экспедиции", 0, 0, 250, 62);
            var er = (RectTransform)expedition.transform; er.anchorMin = er.anchorMax = er.pivot = new Vector2(1, 0); er.anchoredPosition = new Vector2(-28, 30);
            SetDestination(expedition, GameUiDestination.Levels);
            if (hotbar != null)
            {
                var rect = (RectTransform)hotbar; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0); rect.pivot = new Vector2(0.5f, 0); rect.anchoredPosition = new Vector2(0, 24);
                var slots = hotbar.GetComponentsInChildren<PlayingInventorySlotUI>(true);
                foreach (var slot in slots)
                {
                    if (slot.GetComponentInChildren<ResourceIcon>(true) == null) AddBlock(slot.transform, new Vector2(0, 0), 0.85f, true);
                    var block = slot.GetComponentInChildren<ResourceIcon>(true); block.SetSprite(null); block.gameObject.SetActive(false);
                }
                Set(root.GetComponent<PlayingWindow>(), "_activeSlotSprite", S("selected"), "_inactiveSlotSprite", S("slot"));
            }
            Save(root, UI + "PlayingWindow.prefab");
        }

        private static Sprite TextureSprite()
        {
            string path = Art + "beige-fill.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(4, 4); texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray()); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
                importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void BuildLevelProgress()
        {
            var root = Edit<LevelProgressWindow>(UI + "LevelProgressWindow.prefab", false);
            var window = root.GetComponent<LevelProgressWindow>();
            var so = new SerializedObject(window);
            var arrow = (RectTransform)so.FindProperty("_portalArrow").objectReferenceValue;
            Keep(root, new Transform[] { arrow });
            var frame = Panel("ProgressPanel", root.transform, "panel");
            frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 1); frame.pivot = new Vector2(0.5f, 1); frame.anchoredPosition = Vector2.zero; frame.sizeDelta = new Vector2(600, 80); Outline(frame);
            Text("Label", frame, "ЗАЧИСТКА", 18, 18, 8, 350, 25, true);
            var text = Text("Progress", frame, "0 / 0", 18, 432, 8, 150, 25); text.alignment = TextAlignmentOptions.Right;
            var progress = Slider(frame, "ProgressBar", 18, 43, 564, 18, false); progress.value = 0;
            Set(window, "_progressPanel", frame.gameObject, "_progressBar", progress, "_progressText", text);
            Save(root, UI + "LevelProgressWindow.prefab");
        }

        private static void BuildWheel(ShopCatalog catalog)
        {
            var root = Edit<FortuneWheelWindow>(UI + "FortuneWheelWindow.prefab", false);
            var wheel = Find(root.transform, "BackgroundWheel");
            Keep(root, new[] { wheel });
            var frame = Window(root, "Колесо удачи", 1320, 850);
            wheel.SetParent(frame, false);
            var wr = (RectTransform)wheel; wr.anchorMin = wr.anchorMax = new Vector2(0, 1); wr.pivot = new Vector2(0.5f, 0.5f); wr.anchoredPosition = new Vector2(398, -410); wr.sizeDelta = new Vector2(580, 580);
            wheel.GetComponent<Image>().sprite = WheelSprite(); wheel.GetComponent<Image>().color = Color.white;
            var pointerRect = Rect("Pointer", frame); Box(pointerRect, 362, 116, 72, 62);
            pointerRect.pivot = new Vector2(0.5f, 1f); pointerRect.anchoredPosition = new Vector2(398, -116);
            var pointer = pointerRect.gameObject.AddComponent<MineArena.UI.FortuneWheel.WheelPointerGraphic>();
            pointer.color = new Color(0.824f, 0.596f, 0.247f); pointer.raycastTarget = false;
            var spin = Button("Start", frame, "Крутить!", 120, 729, 555, 62);
            var timer = Text("FreeSpinText", frame, "Первое вращение бесплатно", 22, 756, 145, 520, 60); timer.enableWordWrapping = true;
            Text("BuyHeading", frame, "ДОПОЛНИТЕЛЬНЫЕ ПОПЫТКИ", 19, 756, 235, 520, 40, true);
            var purchases = new List<Button>();
            int[] amounts = { 1, 3, 5, 10 };
            for (int i = 0; i < amounts.Length; i++)
                purchases.Add(Button("BuySpin" + amounts[i], frame, amounts[i] + " вращ.  •  " + (amounts[i] * 5) + " зол. руды", 756, 303 + i * 86, 520, 62, false));
            Text("Hint", frame, "Награда добавится в инвентарь.\nДождитесь завершения вращения.", 21, 756, 693, 520, 82).enableWordWrapping = true;
            var window = root.GetComponent<FortuneWheelWindow>();
            Set(window, "_spinCurrency", catalog.Currency);
            Set(window, "_wheelTransform", wheel, "_spinButton", spin, "_spinButtonText", spin.GetComponentInChildren<TMP_Text>(), "_freeSpinTimerText", timer, "_pointerTransform", pointer.transform);
            var so = new SerializedObject(window); var array = so.FindProperty("_purchaseButtons"); array.arraySize = purchases.Count;
            for (int i = 0; i < purchases.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = purchases[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(root, UI + "FortuneWheelWindow.prefab");
        }

        private static Sprite WheelSprite()
        {
            string path = Art + "beige-wheel.png";
            var tex = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
            {
                float dx = x - 127.5f, dy = y - 127.5f, radius = Mathf.Sqrt(dx * dx + dy * dy);
                int sector = Mathf.FloorToInt((Mathf.Atan2(dy, dx) + Mathf.PI) / (Mathf.PI / 3));
                Color color = radius > 125 ? Color.clear : radius > 119 ? C("65553D") : radius > 114 ? C("E5C993") : sector % 2 == 0 ? C("E6D7B7") : C("A9B67B");
                if (radius < 14) color = C("65553D");
                tex.SetPixel(x, y, color);
            }
            tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path); imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
            imp.filterMode = FilterMode.Point; imp.mipmapEnabled = false; imp.textureCompression = TextureImporterCompression.Uncompressed; imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void RestyleExisting(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try { Skin(root, true); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static void BuildCraftItem(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            foreach (var layout in root.GetComponents<LayoutGroup>()) Object.DestroyImmediate(layout);
            var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(542, 94);
            var background = root.GetComponent<Image>(); background.sprite = S("card"); background.color = Color.white; background.type = UnityEngine.UI.Image.Type.Sliced;
            var le = root.GetComponent<LayoutElement>() ?? root.AddComponent<LayoutElement>(); le.minHeight = le.preferredHeight = 94;
            var selected = Panel("SelectionHighlight", rect, "selected"); Stretch(selected); selected.gameObject.SetActive(false);
            var iconSlot = Rect("IconSlot", rect); Box(iconSlot, 14, 17, 56, 56);
            var icon = Image("Icon", iconSlot, null); Stretch(icon.rectTransform); icon.preserveAspect = true; icon.gameObject.SetActive(false);
            AddBlock(iconSlot, Vector2.zero, 0.75f); var block = iconSlot.GetComponentInChildren<ResourceIcon>(true);
            var name = Text("Name", rect, "", 23, 86, 13, 410, 34);
            var meta = Text("Meta", rect, "", 17, 86, 51, 410, 30);
            var locked = Image("LockedState", rect, S("lock")); Box(locked.rectTransform, 509, 18, 20, 24); locked.color = Ink; locked.gameObject.SetActive(false);
            Set(root.GetComponent<CraftingItemView>(), "_button", root.GetComponent<Button>(), "_icon", icon, "_blockIcon", block, "_name", name, "_meta", meta,
                "_lockedState", locked.gameObject, "_selectionHighlight", selected.gameObject, "_canvasGroup", root.GetComponent<CanvasGroup>());
            PrefabUtility.SaveAsPrefabAsset(root, path); PrefabUtility.UnloadPrefabContents(root);
        }

        private static void RestyleCrafting(string path)
        {
            if (!File.Exists(path)) return;
            var root = Edit<CraftingWindow>(path);
            var window = root.GetComponent<CraftingWindow>();
            var frame = Window(root, "Мастерская", 1240, 830);
            var tabs = Scroll(frame, "Categories", 36, 124, 1168, 64);
            tabs.horizontal = true; tabs.vertical = false;
            tabs.content.sizeDelta = new Vector2(0, 64);
            var fitter = tabs.content.GetComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained; fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var horizontal = tabs.content.gameObject.AddComponent<HorizontalLayoutGroup>(); horizontal.spacing = 12; horizontal.childControlWidth = false; horizontal.childForceExpandWidth = false;
            var left = Panel("RecipesPanel", frame, "inset"); Box(left, 36, 210, 574, 576);
            Text("ListTitle", left, "РЕЦЕПТЫ", 22, 20, 16, 530, 40, true);
            var recipes = Scroll(left, "Recipes", 16, 72, 542, 482);
            var list = recipes.content.gameObject.AddComponent<VerticalLayoutGroup>(); list.spacing = 10; list.childControlHeight = false; list.childForceExpandHeight = false;
            var right = Panel("DetailsPanel", frame, "card"); Box(right, 630, 210, 574, 576);
            var slot = Panel("DetailIconSlot", right, "slot"); Box(slot, 24, 24, 108, 108);
            var icon = Image("DetailIcon", slot, null); Box(icon.rectTransform, 16, 16, 76, 76); icon.preserveAspect = true;
            icon.gameObject.SetActive(false);
            AddBlock(slot, Vector2.zero, 1.05f); var block = slot.GetComponentInChildren<ResourceIcon>(true);
            var name = Text("DetailName", right, "Выберите рецепт", 28, 152, 24, 396, 50, true);
            var requirement = Text("Requirement", right, "Доступность зависит от уровня здания", 20, 152, 83, 396, 55); requirement.enableWordWrapping = true;
            var description = Text("Description", right, "Создавайте снаряжение из добытых ресурсов", 22, 24, 144, 526, 72); description.enableWordWrapping = true;
            Text("CostsTitle", right, "НУЖНЫ РЕСУРСЫ", 22, 24, 222, 526, 36, true);
            var costs = Scroll(right, "Costs", 24, 264, 526, 144).content;
            var costList = costs.gameObject.AddComponent<VerticalLayoutGroup>(); costList.spacing = 8; costList.childControlWidth = true; costList.childForceExpandWidth = true; costList.childControlHeight = false; costList.childForceExpandHeight = false;
            var result = Text("Result", right, "", 20, 24, 412, 526, 42); result.enableWordWrapping = true;
            var batch = Rect("BatchControls", right); Box(batch, 24, 456, 526, 38);
            var singleBatch = Button("SingleBatch", batch, "×1", 0, 0, 112, 38, false);
            var allBatches = Button("AllBatches", batch, "Всё", 124, 0, 210, 38, false);
            Text("BatchDuration", batch, "Один таймер", 17, 350, 0, 176, 38);
            var craft = Button("Craft", right, "Создать", 24, 502, 526, 58);
            var empty = Text("EmptyState", right, "Нет доступных рецептов", 24, 24, 180, 526, 60); empty.gameObject.SetActive(false);
            Set(window, "_rootImage", root.GetComponent<Image>(), "_windowPanelImage", frame.GetComponent<Image>(), "_windowPanel", frame,
                "_tabsRoot", tabs.content, "_tabsPanelImage", null, "_leftPanelImage", left.GetComponent<Image>(), "_rightPanelImage", right.GetComponent<Image>(),
                "_listViewportImage", recipes.viewport.GetComponent<Image>(), "_itemsRoot", recipes.content, "_costsRoot", costs, "_detailIconSlotImage", slot.GetComponent<Image>(),
                "_detailIcon", icon, "_detailResourceIcon", block, "_detailName", name, "_detailDescription", description, "_detailRequirement", requirement,
                "_emptyState", empty, "_resultText", result, "_craftButton", craft, "_craftButtonImage", craft.GetComponent<Image>(), "_craftButtonLabel", craft.GetComponentInChildren<TMP_Text>(),
                "_batchControls", batch.gameObject, "_singleBatchButton", singleBatch, "_allBatchesButton", allBatches,
                "_resourceIconPrefab", AssetDatabase.LoadAssetAtPath<ResourceIcon>("Assets/Prefabs/Windows/ResourceIcon.prefab"),
                "_panelSprite", S("panel"), "_panelInsetSprite", S("inset"), "_slotSprite", S("slot"), "_slotSelectedSprite", S("selected"),
                "_buttonSprite", S("card"), "_buttonSelectedSprite", S("selected"), "_buttonDisabledSprite", S("inset"), "_craftButtonSprite", S("button"), "_placeholderIconSprite", S("slot"));
            Save(root, path);
        }
        private static void RestyleElementPrefabs()
        {
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Windows", "Assets/Resources/Prefabs/Windows", "Assets/Prefabs/Achievements" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                if (path.Contains("SelectLevelWindow") || path.EndsWith("ResourceIcon.prefab")) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab.GetComponent<BaseWindow>() != null) continue;
                RestyleExisting(path);
            }
        }

        private static void Skin(GameObject root, bool decorate)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.GetComponentInParent<ResourceIcon>() != null || image.type == UnityEngine.UI.Image.Type.Filled) continue;
                string name = image.name.ToLowerInvariant();
                string sprite = image.sprite != null ? image.sprite.name.ToLowerInvariant() : "";
                if (name.Contains("icon") || name.Contains("arrow") || name.Contains("pointer") || name.Contains("checkmark") || name.Contains("portrait") || name.Contains("pale") || name.Contains("landscape")) continue;
                bool button = image.GetComponent<Button>() != null;
                bool box = name.Contains("panel") || name.Contains("window") || name.Contains("background") || name.Contains("slot") || name.StartsWith("cell") || name.Contains("equip") ||
                    sprite.Contains("panel") || sprite.Contains("window") || sprite.Contains("button") || sprite.Contains("slot") || sprite.Contains("frame") || sprite.Contains("square");
                if (!button && !box) continue;
                var rect = image.rectTransform;
                bool overlay = image.gameObject == root || ((name == "background" || name == "bg") && rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one && rect.sizeDelta == Vector2.zero);
                if (overlay && root.GetComponent<BaseWindow>() != null) { image.sprite = null; image.color = new Color(0.15f, 0.13f, 0.09f, 0.78f); continue; }
                image.sprite = S(button ? "card" : name.Contains("cell") || name.Contains("slot") || name.Contains("equip") ? "slot" : "panel");
                image.type = UnityEngine.UI.Image.Type.Sliced; image.color = Color.white;
                if (decorate && rect.rect.width > 600 && rect.rect.height > 300 && image.GetComponentInParent<ScrollRect>() == null)
                { Outline(rect); Brick(rect, 0.25f); }
            }
            foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
            {
                label.color = Ink;
                bool title = label.name.ToLowerInvariant().Contains("title");
                label.font = title ? Pixel : Font;
                label.fontSharedMaterial = label.font.material;
                label.outlineWidth = 0;
                label.raycastTarget = false;
            }
            foreach (var label in root.GetComponentsInChildren<UnityEngine.UI.Text>(true)) label.color = Ink;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                if (button.name.ToLowerInvariant().Contains("close") && button.GetComponentInChildren<TMP_Text>(true) == null)
                {
                    var size = ((RectTransform)button.transform).rect.size;
                    var label = Text("CloseLabel", button.transform, "×", 25, 0, 0, size.x, size.y); label.alignment = TextAlignmentOptions.Center;
                }
        }

        private static void RegisterWindows()
        {
            string path = "Assets/DevotionSDK/Prefabs/Managers/UIManager.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var so = new SerializedObject(root.GetComponent<UIManager>()); var array = so.FindProperty("_windows");
                foreach (var name in new[] { "ShopWindow", "SettingsWindow", "LevelCompleteWindow", "PlaytimeGiftWindow" })
                {
                    var window = AssetDatabase.LoadAssetAtPath<GameObject>(UI + name + ".prefab").GetComponent<BaseWindow>();
                    bool exists = false;
                    for (int i = 0; i < array.arraySize; i++)
                        if (array.GetArrayElementAtIndex(i).objectReferenceValue is BaseWindow old && old.GetType() == window.GetType())
                        { array.GetArrayElementAtIndex(i).objectReferenceValue = window; exists = true; }
                    if (!exists) { array.InsertArrayElementAtIndex(array.arraySize); array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue = window; }
                }
                so.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static GameObject Edit<T>(string path, bool clear = true) where T : BaseWindow
        {
            if (!File.Exists(path))
            {
                var fresh = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(T)); fresh.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(fresh, path); Object.DestroyImmediate(fresh);
            }
            var root = PrefabUtility.LoadPrefabContents(path); root.SetActive(false);
            if (clear) for (int i = root.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            return root;
        }
        private static void Save(GameObject root, string path)
        {
            root.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        private static void Keep(GameObject root, Transform[] kept)
        {
            foreach (var t in kept) if (t != null) t.SetParent(root.transform, false);
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                if (!kept.Contains(root.transform.GetChild(i))) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        }
        private static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        private static RectTransform Window(GameObject root, string title, float width, float height, bool close = true)
        {
            Stretch((RectTransform)root.transform);
            var dim = root.GetComponent<Image>() ?? root.AddComponent<Image>(); dim.sprite = null; dim.color = new Color(0.15f, 0.13f, 0.09f, 0.78f); dim.raycastTarget = true;
            var frame = Panel("BeigeWindow", root.transform, "panel");
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0.5f, 0.5f); frame.sizeDelta = new Vector2(width, height); frame.anchoredPosition = Vector2.zero;
            Outline(frame); Brick(frame, 0.4f);
            Set(root.GetComponent<LevelWindowFit>() ?? root.AddComponent<LevelWindowFit>(), "panel", frame);
            Text("Title", frame, title, 36, 36, 32, width - 156, 54, true);
            var line = Image("Divider", frame, null); line.color = C("CCBA98"); Box(line.rectTransform, 36, 99, width - 72, 2);
            if (close)
            {
                var button = Button("Close", frame, "×", width - 98, 32, 60, 60, false);
                UnityEventTools.AddPersistentListener(button.onClick, root.GetComponent<BaseWindow>().CloseWindow);
            }
            return frame;
        }
        private static RectTransform Rect(string name, Transform parent)
        { var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; go.transform.SetParent(parent, false); return (RectTransform)go.transform; }
        private static RectTransform Panel(string name, Transform parent, string sprite)
        { return Image(name, parent, S(sprite)).rectTransform; }
        private static Image Image(string name, Transform parent, Sprite sprite)
        { var image = Rect(name, parent).gameObject.AddComponent<Image>(); image.sprite = sprite; image.type = sprite != null && sprite.border != Vector4.zero ? UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple; image.raycastTarget = false; return image; }
        private static TextMeshProUGUI Text(string name, Transform parent, string text, int size, float x, float y, float w, float h, bool pixel = false)
        {
            var rect = Rect(name, parent); Box(rect, x, y, w, h);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = pixel ? Pixel : Font; label.fontSize = size; label.text = text; label.color = Ink;
            label.enableWordWrapping = false; label.overflowMode = TextOverflowModes.Ellipsis; label.raycastTarget = false; label.alignment = TextAlignmentOptions.MidlineLeft; return label;
        }
        private static Button Button(string name, Transform parent, string text, float x, float y, float w, float h, bool primary = true)
        {
            var rect = Panel(name, parent, primary ? "button" : "card"); Box(rect, x, y, w, h);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); button.targetGraphic.raycastTarget = true;
            var colors = button.colors; colors.pressedColor = new Color(0.8f, 0.8f, 0.75f); colors.disabledColor = new Color(0.75f, 0.75f, 0.7f, 0.65f); button.colors = colors;
            var label = Text("Label", rect, text, h < 50 ? 20 : 25, 8, 0, w - 16, h, primary); label.alignment = TextAlignmentOptions.Center; label.color = primary ? C("FFFAE9") : Ink;
            return button;
        }
        private static void ActionButton(Transform parent, string text, GameUiDestination destination, float x, float y, float width)
        { SetDestination(Button(destination.ToString(), parent, text, x, y, width, 46, false), destination); }
        private static void SetDestination(Button button, GameUiDestination destination)
        { var so = new SerializedObject(button.gameObject.AddComponent<GameUiAction>()); so.FindProperty("destination").enumValueIndex = (int)destination; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static Slider Slider(Transform parent, string name, float x, float y, float width, float height, bool interactive)
        {
            var rect = Panel(name, parent, "inset"); Box(rect, x, y, width, height);
            var area = Rect("FillArea", rect); Stretch(area); area.offsetMin = new Vector2(5, 5); area.offsetMax = new Vector2(-5, -5);
            var fill = Panel("Fill", area, "button"); Stretch(fill);
            var slider = rect.gameObject.AddComponent<Slider>(); slider.fillRect = fill; slider.targetGraphic = rect.GetComponent<Image>(); slider.interactable = interactive; slider.value = 1;
            rect.GetComponent<Image>().raycastTarget = interactive;
            if (interactive)
            {
                var handleArea = Rect("HandleArea", rect); Stretch(handleArea); handleArea.offsetMin = new Vector2(10, 0); handleArea.offsetMax = new Vector2(-10, 0);
                var handle = Panel("Handle", handleArea, "card"); handle.sizeDelta = new Vector2(24, 8); slider.handleRect = handle; handle.GetComponent<Image>().raycastTarget = true;
            }
            return slider;
        }
        private static ScrollRect Scroll(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = Rect(name, parent); Box(rect, x, y, width, height);
            var viewport = Image("Viewport", rect, null); Stretch(viewport.rectTransform); viewport.color = Color.clear; viewport.raycastTarget = true; viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport.transform); content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0, 1); content.sizeDelta = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = rect.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport.rectTransform; scroll.content = content; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
            return scroll;
        }
        private static void Grid(RectTransform root, int columns, Vector2 size)
        { var grid = root.gameObject.AddComponent<GridLayoutGroup>(); grid.cellSize = size; grid.spacing = new Vector2(16, 14); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = columns; }
        private static void AddBlock(Transform parent, Vector2 position, float scale, bool centered = false)
        {
            var block = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/ResourceIcon.prefab"), parent);
            var rect = (RectTransform)block.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.localScale = Vector3.one * scale;
            foreach (var image in block.GetComponentsInChildren<Image>()) image.raycastTarget = false;
            block.SetActive(false);
        }
        private static void Outline(RectTransform rect)
        {
            var outline = rect.GetComponent<Outline>() ?? rect.gameObject.AddComponent<Outline>(); outline.effectColor = C("65553D"); outline.effectDistance = new Vector2(3, -3);
            var shadow = rect.GetComponents<Shadow>().FirstOrDefault(s => !(s is Outline)) ?? rect.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0.08f, 0.06f, 0.035f, 0.48f); shadow.effectDistance = new Vector2(8, -10);
        }
        private static void Brick(RectTransform parent, float opacity)
        {
            if (parent.Find("PaleBrickTexture") != null) return;
            var rect = Panel("PaleBrickTexture", parent, "brick"); Stretch(rect); rect.offsetMin = Vector2.one * 6; rect.offsetMax = -Vector2.one * 6;
            rect.GetComponent<Image>().type = UnityEngine.UI.Image.Type.Tiled; rect.GetComponent<Image>().color = new Color(1, 1, 1, opacity); rect.SetAsFirstSibling();
        }
        private static void Box(RectTransform rect, float x, float y, float w, float h)
        { rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); }
        private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
        private static Color C(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }
        private static void Set(Object target, params object[] values)
        { var so = new SerializedObject(target); for (int i = 0; i < values.Length; i += 2) so.FindProperty((string)values[i]).objectReferenceValue = (Object)values[i + 1]; so.ApplyModifiedPropertiesWithoutUndo(); }

        [MenuItem("MineArena/UI/Render All Windows")]
        public static void RenderAll()
        {
            Directory.CreateDirectory("Documentation/UI");
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { UI.TrimEnd('/'), "Assets/Prefabs/Windows", "Assets/Resources/Prefabs/Windows" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab.GetComponent<BaseWindow>() == null || prefab.GetComponent<BlackWindow>() != null) continue;
                Render(prefab, "Documentation/UI/" + prefab.name + (path.Contains("Resources/") ? "-resources" : "") + ".png");
            }
        }
        internal static void Render(GameObject prefab, string path, Action<GameObject> setup = null)
        {
            var scene = EditorSceneManager.NewPreviewScene(); RenderTexture target = null; Texture2D capture = null; var previous = RenderTexture.active;
            try
            {
                var cameraObject = new GameObject("PreviewCamera", typeof(Camera)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 540; camera.transform.position = new Vector3(0, 0, -100);
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = C("A8AD8C"); camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                var canvasObject = new GameObject("Canvas", typeof(Canvas)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = camera; ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920, 1080);
                var root = EditorUtility.IsPersistent(prefab) ? (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene) : Object.Instantiate(prefab);
                if (root.scene != scene) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.SetParent(canvas.transform, false); root.SetActive(true);
                setup?.Invoke(root);
                target = new RenderTexture(1920, 1080, 24); camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                foreach (var rect in root.GetComponentsInChildren<RectTransform>()) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                var fit = root.GetComponent<LevelWindowFit>(); if (fit != null) fit.SendMessage("LateUpdate");
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                capture = new Texture2D(1920, 1080, TextureFormat.RGB24, false); capture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); capture.Apply();
                File.WriteAllBytes(path, capture.EncodeToPNG()); camera.targetTexture = null;
            }
            finally { RenderTexture.active = previous; if (target != null) Object.DestroyImmediate(target); if (capture != null) Object.DestroyImmediate(capture); EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
