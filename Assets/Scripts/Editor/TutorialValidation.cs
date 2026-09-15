using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.SaveSystem;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.AI;
using MineArena.Items;
using MineArena.Levels;
using MineArena.Managers;
using MineArena.Structs;
using MineArena.Windows;
using MineArena.Windows.Crafting;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static class TutorialValidation
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [InitializeOnLoadMethod]
        static void Watch() => EditorApplication.update += () =>
        {
            if (File.Exists("Temp/tutorial-theme.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                File.Delete("Temp/tutorial-theme.request");
                BuildTheme();
            }
            if (!File.Exists("Temp/validate-tutorial.request") || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete("Temp/validate-tutorial.request");
            try { Validate(); }
            catch (Exception e) { File.WriteAllText("Documentation/tutorial-validation.txt", "FAIL " + e); Debug.LogException(e); }
        };

        [MenuItem("MineArena/UI/Refresh Tutorial Theme")]
        public static void BuildTheme()
        {
            const string path = "Assets/Resources/UI/TutorialTheme.asset";
            var theme = AssetDatabase.LoadAssetAtPath<MineArena.UI.TutorialTheme>(path);
            if (theme == null) { theme = ScriptableObject.CreateInstance<MineArena.UI.TutorialTheme>(); AssetDatabase.CreateAsset(theme, path); }
            theme.Panel = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Expedition/beige-panel.png");
            theme.Ribbon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Accents/ribbon-3B7582.png");
            theme.Button = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Accents/button-teal.png");
            theme.ResourceIcon = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/ResourceIcon.prefab").GetComponent<MineArena.UI.ResourceIcon>();
            theme.BodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset");
            theme.HeadingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset");
            EditorUtility.SetDirty(theme); AssetDatabase.SaveAssets();
            var level = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/ScriptableObjects/Levels/Level1_Village.asset");
            var hint = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath("180f6820cb539534ba5ed2121a0b7f19"));
            foreach (var spawn in level.ResourceSpawnConfigs)
            {
                var billboard = spawn.Resource != null ? spawn.Resource.GetComponent<BillboardCanvas>() : null;
                if (billboard == null || hint == null) continue;
                var data = new SerializedObject(billboard);
                if (data.FindProperty("_billboardCanvas").objectReferenceValue != null) continue;
                data.FindProperty("_billboardCanvas").objectReferenceValue = hint;
                data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(billboard);
            }
            AssetDatabase.SaveAssets();
        }

        [MenuItem("MineArena/Validation/Tutorial")]
        public static void Validate()
        {
            if (Application.isPlaying || GameRoot.Instance != null || Object.FindObjectsOfType<SaveService>(true).Any(s => s.IsLoaded))
                throw new InvalidOperationException("Tutorial validation requires Edit Mode with no loaded player save.");
            var scene = EditorSceneManager.NewPreviewScene();
            var lines = new List<string>();
            float previousTimeScale = Time.timeScale;
            var previousEventSystem = EventSystem.current;
            GameObject overlay = null;
            RenderTexture raycastTarget = null;
            void Check(bool valid, string message) { if (!valid) throw new Exception(message); lines.Add("PASS " + message); }
            try
            {
                var go = new GameObject("Isolated tutorial validation");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var root = go.AddComponent<GameRoot>(); typeof(GameRoot).GetProperty("Instance").SetValue(null, root);
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
                var progress = new PlayerProgress("tutorial-validation");
                void SetProgress(PlayerProgress p) { progress = p; typeof(GameRoot).GetField("playerProgress", Private).SetValue(root, p); }
                typeof(GameRoot).GetField("gameConfig", Private).SetValue(root, config); SetProgress(progress);
                var inventory = go.AddComponent<InventoryManager>(); var buildings = go.AddComponent<BuildingManager>();
                var managers = (Dictionary<Type, BaseManager>)typeof(GameRoot).GetField("_managers", Private).GetValue(root);
                managers[typeof(InventoryManager)] = inventory; managers[typeof(BuildingManager)] = buildings;
                inventory.InitManager();
                var tutor = go.AddComponent<TutorialService>(); tutor.Initialize();
                Check(TutorialService.Active && progress.TutorialProgress.Step == TutorialStep.Portal, "Fresh save starts at the portal");
                Check(!TutorialService.AllowWindow(typeof(SelectLevelWindow)) && !TutorialService.AllowLevel(0), "HUD cannot bypass entering the portal");
                Check(!TutorialService.AllowWindow(typeof(CraftingWindow)) && !TutorialService.AllowBuilding(TutorialService.Workshop), "No building or crafting before the expedition");
                Check(TutorialService.EnterPortal() && TutorialService.AllowLevel(0) && !TutorialService.AllowLevel(1), "Portal permits only the first expedition");
                TutorialService.BeginLevel();
                var wave = go.AddComponent<WaveSpawner>(); int normalCount = config.Levels[0].EncounterWaves.Sum(w => w.MobCount);
                var previewRoutine = (IEnumerator)typeof(WaveSpawner).GetMethod("SpawnWaves", Private).Invoke(wave, null);
                Check(previewRoutine.MoveNext() && previewRoutine.Current == null && previewRoutine.MoveNext() && previewRoutine.Current == null, "Unconfigured scene spawner cannot release preview waves");
                wave.Configure(config.Levels[0].EncounterWaves);
                Check(wave.TotalMobCount == 1 && normalCount == config.Levels[0].EncounterWaves.Sum(w => w.MobCount), "Tutorial has one enemy without changing the level asset");
                var routine = (IEnumerator)typeof(WaveSpawner).GetMethod("SpawnWaves", Private).Invoke(wave, null);
                Check(routine.MoveNext() && routine.Current is IEnumerator && (float)typeof(WaveSpawner).GetField("_startDelay", Private).GetValue(wave) == 0, "Tutorial schedules its zombie immediately before resource collection");
                TutorialService.EnemyKilled();
                var wood = config.ItemDatabase.GetItemConfig("WoodOak");
                TutorialService.CollectedResource(wood);
                Check(progress.TutorialProgress.Step == TutorialStep.Mine, "Kill or loose pickup cannot skip the mining step");
                var dormantObject = new GameObject("Dormant tutorial zombie", typeof(MobMovement), typeof(MobCombat), typeof(Mob));
                dormantObject.transform.SetParent(go.transform);
                var dormantMob = dormantObject.GetComponent<Mob>();
                typeof(Mob).GetField("_mobMovement", Private).SetValue(dormantMob, dormantObject.GetComponent<MobMovement>());
                typeof(Mob).GetField("_mobCombat", Private).SetValue(dormantMob, dormantObject.GetComponent<MobCombat>());
                dormantMob.SetTutorialDormant(true);
                Check(dormantMob.TutorialDormant && !dormantObject.GetComponent<MobMovement>().enabled && !dormantObject.GetComponent<MobCombat>().enabled, "Visible tutorial zombie has movement and attacks disabled before mining");
                TutorialService.StartedMining();
                Check(!progress.TutorialProgress.Mined && progress.TutorialProgress.Step == TutorialStep.Mine &&
                    typeof(TutorialService).GetMethod("FindTarget", Private).Invoke(tutor, null) == null, "Arrow is hidden when mining starts, before the resource is broken");
                TutorialService.MinedBlock();
                Check(typeof(TutorialService).GetMethod("FindTarget", Private).Invoke(tutor, null) == null, "Mining arrow disappears after the block breaks, before pickup");
                TutorialService.CollectedResource(wood);
                typeof(Mob).GetMethod("Update", Private).Invoke(dormantMob, null);
                Check(!dormantMob.TutorialDormant && dormantObject.GetComponent<MobMovement>().enabled && dormantObject.GetComponent<MobCombat>().enabled, "Collecting resource activates the existing zombie");
                Object.DestroyImmediate(dormantObject);
                Check(progress.TutorialProgress.Step == TutorialStep.Kill, "Mining plus pickup begins combat");
                var saved = JsonUtility.ToJson(progress);
                SetProgress(JsonUtility.FromJson<PlayerProgress>(saved)); tutor.Initialize();
                Check(progress.TutorialProgress.Step == TutorialStep.Portal && progress.TutorialProgress.Collected, "Restart in the lobby retains completed mining and restores portal guidance");
                TutorialService.EnterPortal(); TutorialService.BeginLevel();
                Check(progress.TutorialProgress.Step == TutorialStep.Kill, "Retry continues with the unfinished fight");
                TutorialService.EnemyKilled();
                Check(progress.TutorialProgress.Step == TutorialStep.Exit, "Enemy death unlocks extraction guidance");
                var extractionObject = new GameObject("Tutorial extraction validation", typeof(RectTransform), typeof(LevelCompleteWindow));
                try
                {
                    var extraction = extractionObject.GetComponent<LevelCompleteWindow>();
                    int claimed = 0, returned = 0, doubled = 0;
                    extraction.Setup(new Dictionary<ItemConfig, int>(), () => claimed++, () => doubled++, true, () => returned++);
                    Check(typeof(TutorialService).GetMethod("FindTarget", Private).Invoke(tutor, null) == extraction.TutorialTarget, "Portal spotlight targets claim-and-return button");
                    var backButton = (UnityEngine.UI.Button)typeof(LevelCompleteWindow).GetField("_returnButton", Private).GetValue(extraction);
                    var doubleButton = (UnityEngine.UI.Button)typeof(LevelCompleteWindow).GetField("_doubleRewardsButton", Private).GetValue(extraction);
                    Check(!backButton.interactable && !doubleButton.interactable && extraction.TutorialTarget.GetComponent<UnityEngine.UI.Button>().interactable, "Only claim-and-return is interactable in tutorial extraction");
                    typeof(LevelCompleteWindow).GetMethod("HandleReturnClicked", Private).Invoke(extraction, null);
                    typeof(LevelCompleteWindow).GetMethod("HandleDoubleRewardsClicked", Private).Invoke(extraction, null);
                    typeof(LevelCompleteWindow).GetMethod("HandleContinueClicked", Private).Invoke(extraction, null);
                    Check(claimed == 1 && returned == 0 && doubled == 0, "Tutorial extraction rejects alternate handlers and claims once");
                }
                finally { Object.DestroyImmediate(extractionObject); }
                var reward = new Dictionary<ItemConfig, int>(); TutorialService.EnsureFirstBuildingReward(reward);
                var workshop = TutorialService.Workshop;
                var smith = TutorialService.Smith;
                var sword = config.ItemDatabase.GetItemConfig("StoneSword");
                var planks = config.ItemDatabase.GetItemConfig("Planks");
                var costs = smith.GetLevelByNumber(1).RequiredResources;
                Check(costs.Concat(sword.CraftCosts).GroupBy(c => c.Resource).All(g => reward[g.Key] >= g.Sum(c => c.Amount)), "First-level reward covers smith PLUS sword, including shared stone costs");
                Check(reward[config.ItemDatabase.GetItemConfig("Stick")] >= 1, "First-level reward includes the sword stick");
                foreach (var entry in reward) inventory.AddItemById(entry.Key.Name, entry.Value);
                progress.TutorialProgress.SmithSuppliesGranted = true;
                TutorialService.SetStep(TutorialStep.BuildSmith);
                Check(!TutorialService.AllowBuilding(workshop) && TutorialService.AllowBuilding(smith), "Only smith construction is taught");
                Check(inventory.TryConsumeResources(costs), "First-level reward pays for smith without mined resources");
                progress.BuildingProgress.SavedBuildings[smith.BuildingName] = new BuildingSaveData(1, null);
                tutor.Initialize();
                Check(progress.TutorialProgress.Step == TutorialStep.CraftSword, "Saved smith resumes directly at sword crafting");
                var refresh = typeof(TutorialService).GetMethod("RefreshExtendedProgress", BindingFlags.Static | BindingFlags.NonPublic);
                refresh.Invoke(null, null);
                Check(inventory.GetItemAmount("Stone") == 8 && inventory.GetItemAmount("Stick") == 1, "After construction exactly eight stone and one stick remain");
                typeof(TutorialService).GetField("_acknowledged", Private).SetValue(tutor, TutorialStep.CraftSword);
                var craftingObject = (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Prefabs/Windows/Crafting/CraftingWindow"), scene);
                craftingObject.SetActive(true);
                var crafting = craftingObject.GetComponent<CraftingWindow>();
                ((ProjectCraftingAdapter)typeof(CraftingWindow).GetField("_adapter", Private).GetValue(crafting)).Connect();
                crafting.Initialize(smith);
                Check(TutorialService.AllowWindow(typeof(CraftingWindow)), "Tutorial crafting window is permitted");
                var costRoot = (RectTransform)typeof(CraftingWindow).GetField("_costsRoot", Private).GetValue(crafting);
                Canvas.ForceUpdateCanvases();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(costRoot);
                foreach (var cost in sword.CraftCosts) {
                    var amount = costRoot.Find("Cost_" + cost.Resource.Name + "/Amount").GetComponent<TMP_Text>();
                    Check(amount.text == $"{cost.Amount}/{cost.Amount}", "Actual craft row shows available/required for " + cost.Resource.Name + ": " + amount.text);
                    Check(amount.GetComponent<UnityEngine.UI.LayoutElement>().minWidth >= 90, "Ingredient quantity has reserved width");
                }
                Object.DestroyImmediate(crafting.gameObject);
                var production = go.GetComponent<CraftProductionService>();
                Check(!production.TryStart(planks, planks.CraftAmount) && !production.TryStart(config.ItemDatabase.GetItemConfig("IronChestplate"), 1), "Planks and armor cannot consume tutorial resources");
                Check(production.TryStart(sword, 1), "Remaining materials pay for the sword");
                SetProgress(JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(progress))); inventory.InitManager();
                Check(production.Job != null && progress.TutorialProgress.Step == TutorialStep.CraftSword, "Paid sword craft survives save/load");
                production.Job.ReadyUtcTicks = DateTime.UtcNow.AddSeconds(-1).Ticks;
                typeof(CraftProductionService).GetMethod("Update", Private).Invoke(production, null);
                Check(progress.TutorialProgress.Step == TutorialStep.EquipSword && inventory.GetItemAmount("StoneSword") == 1 && !TutorialService.ClaimGift(), "Sword output goes directly to equipment lesson");
                var hud = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab"), scene);
                var playing = hud.GetComponent<Devotion.SDK.UI.PlayingWindow>();
                playing.RefreshTutorialVisibility();
                var inventoryAction = hud.GetComponentsInChildren<MineArena.UI.GameUiAction>().FirstOrDefault(action => action.Destination == MineArena.UI.GameUiDestination.Inventory);
                Check(inventoryAction != null && inventoryAction.gameObject.activeInHierarchy && TutorialService.AllowHud(inventoryAction.Destination), "Inventory HUD action is visible and permitted during equipment lesson");
                progress.InventoryProgress.SetQuickSlotItemId(1, "StoneSword");
                progress.InventoryProgress.SetSelectedQuickSlotIndex(1); refresh.Invoke(null, null);
                Check(progress.TutorialProgress.Step == TutorialStep.Potion && inventory.GetItemAmount("HealingPotion") == 1, "Selected sword unlocks potion lesson and one potion");
                progress.InventoryProgress.SetQuickSlotItemId(2, "HealingPotion"); refresh.Invoke(null, null);
                Check(progress.TutorialProgress.Step == TutorialStep.Rewards && TutorialService.AllowWindow(typeof(Devotion.SDK.UI.DailyGiftWIndow)), "Potion assignment unlocks real rewards window");
                playing.RefreshTutorialVisibility();
                Check(hud.transform.Find("GiftNavigation").gameObject.activeInHierarchy, "Rewards navigation appears when the lesson changes");
                foreach (var step in new[] { TutorialStep.PlaytimeRewards, TutorialStep.FortuneWheel })
                {
                    TutorialService.SetStep(step); playing.RefreshTutorialVisibility();
                    var destination = step == TutorialStep.PlaytimeRewards ? MineArena.UI.GameUiDestination.Playtime : MineArena.UI.GameUiDestination.Wheel;
                    var action = hud.GetComponentsInChildren<MineArena.UI.GameUiAction>().FirstOrDefault(a => a.Destination == destination);
                    Check(action != null && action.gameObject.activeInHierarchy && TutorialService.AllowHud(destination), "Reward HUD is visible and allowed: " + step);
                    Check(TutorialService.AllowWindow(step == TutorialStep.PlaytimeRewards ? typeof(Devotion.SDK.UI.PlaytimeGiftWindow) : typeof(Devotion.SDK.UI.FortuneWheelWindow)), "Reward lesson permits its window: " + step);
                }
                Object.DestroyImmediate(hud);
                TutorialService.SetStep(TutorialStep.Gift);
                int beforeWood = inventory.GetItemAmount("WoodOak"), beforeStone = inventory.GetItemAmount("Stone");
                Check(TutorialService.ClaimGift(), "Final gift can be claimed");
                Check(inventory.GetItemAmount("IronChestplate") == 1, "Final gift includes one iron chestplate");
                int giftWood = inventory.GetItemAmount("WoodOak"), giftStone = inventory.GetItemAmount("Stone");
                Check(giftWood == beforeWood && giftStone == beforeStone && inventory.GetItemAmount("HealingPotion") == 1, "Final gift adds no resources or healing potions");
                SetProgress(JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(progress))); inventory.InitManager(); tutor.Initialize();
                Check(!TutorialService.Active && !TutorialService.ClaimGift() && inventory.GetItemAmount("WoodOak") == giftWood, "Saved completion prevents duplicate gift grants");
                wave.Configure(config.Levels[0].EncounterWaves);
                Check(wave.TotalMobCount == normalCount && TutorialService.AllowLevel(1) && TutorialService.AllowCraft(planks), "Free play restores normal encounters and actions");
                SetProgress(new PlayerProgress("existing-player")); progress.LevelsProgress.UnlockLevel(1); tutor.Initialize();
                Check(!TutorialService.Active, "Existing expedition progress is not forced through onboarding");
                SetProgress(new PlayerProgress("existing-settlement")); progress.BuildingProgress.SavedBuildings[workshop.BuildingName] = new BuildingSaveData(1, null); tutor.Initialize();
                Check(!TutorialService.Active, "Existing settlement is not forced through onboarding");
                foreach (var legacy in new[] { TutorialStep.Build, TutorialStep.Craft, TutorialStep.CraftArmor, TutorialStep.EquipArmor })
                {
                    SetProgress(new PlayerProgress("legacy-tutorial"));
                    progress.TutorialProgress.Initialized = true;
                    progress.TutorialProgress.Step = legacy;
                    tutor.Initialize();
                    Check(progress.TutorialProgress.Step == (legacy == TutorialStep.EquipArmor ? TutorialStep.EquipSword : TutorialStep.BuildSmith), "Legacy checkpoint migrates without workshop or armor: " + legacy);
                }
                var arena = config.Levels[0].LevelPrefab.GetComponent<Arena>();
                Check(arena != null && arena.PlayerSpawnPosition != null && arena.PortalSpawnPoint != null && arena.GetComponentsInChildren<OreSpawnPoint>(true).Length > 0, "Tutorial arena has player, ore and extraction spawn points");
                Check(config.Levels[0].ResourceSpawnConfigs.Any(r => r.Resource != null && r.Resource.GetComponent<InteractableObject>()?.IsMineable == true), "First level has a mineable resource prefab");
                foreach (TutorialStep step in TutorialService.Steps)
                    if (step != TutorialStep.Complete) Check(!string.IsNullOrWhiteSpace(TutorialService.Instructions(step)), "Guidance text for " + step);
                typeof(TutorialService).GetMethod("CreateOverlay", Private).Invoke(tutor, null);
                overlay = (GameObject)typeof(TutorialService).GetField("_overlay", Private).GetValue(tutor);
                typeof(TutorialService).GetMethod("UpdateIllustration", Private).Invoke(tutor, new object[] { TutorialStep.Mine });
                var blockIllustration = overlay.GetComponentInChildren<MineArena.UI.ResourceIcon>(true);
                Check(blockIllustration != null && blockIllustration.gameObject.activeSelf && !overlay.transform.Find("Tutorial popup/Step illustration").GetComponent<UnityEngine.UI.Image>().enabled, "Mining popup uses three-face ResourceIcon instead of a flat texture");
                Check(blockIllustration.GetComponentsInChildren<UnityEngine.UI.Image>().Count(i => i.enabled && i.sprite != null) == 3, "All three resource faces are present");
                typeof(TutorialService).GetMethod("UpdateIllustration", Private).Invoke(tutor, new object[] { TutorialStep.Gift });
                Check(!blockIllustration.gameObject.activeSelf && overlay.transform.Find("Tutorial popup/Step illustration").GetComponent<UnityEngine.UI.Image>().sprite == config.ItemDatabase.GetItemConfig("IronChestplate").Icon, "Final popup illustrates the awarded chestplate");
                var lessonObject = new GameObject("Inventory continuity validation", typeof(RectTransform), typeof(MineArena.UI.InventoryWindow));
                var slotObject = new GameObject("Quick slot validation", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(Devotion.SDK.UI.PlayingInventorySlotUI));
                try
                {
                    var cellObject = new GameObject("Source cell", typeof(RectTransform), typeof(MineArena.UI.InventoryCellUI));
                    cellObject.transform.SetParent(lessonObject.transform);
                    var cell = cellObject.GetComponent<MineArena.UI.InventoryCellUI>();
                    progress.InventoryProgress.SetQuickSlotItemId(0, "");
                    foreach (var step in new[] { TutorialStep.EquipSword, TutorialStep.Potion })
                    {
                        progress.TutorialProgress.Step = step;
                        string id = step == TutorialStep.EquipSword ? "StoneSword" : "HealingPotion";
                        cell.Setup(new Item(id, null, config.ItemDatabase.GetItemConfig(id).Icon));
                        typeof(TutorialService).GetMethod("UpdateDragLesson", Private).Invoke(tutor, new object[] { true });
                        var hand = (RectTransform)typeof(TutorialService).GetField("_dragHand", Private).GetValue(tutor);
                        Check(hand.gameObject.activeSelf && !hand.GetComponent<TutorialHandGraphic>().raycastTarget, "Animated hand is visible and passes pointer events for " + id);
                    }
                    typeof(TutorialService).GetMethod("KeepInventoryLessonOpen", Private).Invoke(tutor, null);
                    Check(!TutorialService.AwaitingConfirmation && lessonObject.activeSelf, "Potion lesson keeps inventory open without another blocking popup");
                }
                finally { Object.DestroyImmediate(lessonObject); Object.DestroyImmediate(slotObject); }
                var circleObject = new GameObject("Circular spotlight validation", typeof(RectTransform), typeof(TutorialSpotlightGraphic));
                circleObject.transform.SetParent(overlay.transform, false);
                var circle = circleObject.GetComponent<TutorialSpotlightGraphic>();
                circle.rectTransform.sizeDelta = new Vector2(1920, 1080); circle.Focus(Vector2.zero, 90);
                var centerPoint = RectTransformUtility.WorldToScreenPoint(null, circle.transform.position);
                var outsidePoint = RectTransformUtility.WorldToScreenPoint(null, circle.transform.TransformPoint(new Vector3(150, 0)));
                Check(!circle.IsRaycastLocationValid(centerPoint, null) && circle.IsRaycastLocationValid(outsidePoint, null), "Circular spotlight passes clicks inside and blocks outside");
                circle.FocusRect(new Rect(-100, -30, 200, 60));
                var rectangleOutside = RectTransformUtility.WorldToScreenPoint(null, circle.transform.TransformPoint(new Vector3(0, 60)));
                Check(!circle.IsRaycastLocationValid(centerPoint, null) && circle.IsRaycastLocationValid(rectangleOutside, null), "Window button uses a rectangular clickable hole");
                GameUiBuilder.Render(circleObject, "Documentation/UI/Tutorial-Hand.png", clone => {
                    clone.transform.localPosition = Vector3.zero;
                    clone.transform.localScale = Vector3.one;
                    clone.GetComponent<TutorialSpotlightGraphic>().Focus(Vector2.zero, 180);
                    var example = new GameObject("Hand", typeof(RectTransform), typeof(TutorialHandGraphic));
                    example.transform.SetParent(clone.transform, false);
                    ((RectTransform)example.transform).sizeDelta = new Vector2(168, 224);
                    ((RectTransform)example.transform).anchoredPosition = new Vector2(32, -38);
                    Canvas.ForceUpdateCanvases();
                    foreach (var graphic in clone.GetComponentsInChildren<UnityEngine.UI.Graphic>())
                    {
                        var mesh = graphic.canvasRenderer.GetMesh();
                        Check(mesh != null && mesh.vertexCount > 0 && !graphic.canvasRenderer.cull, "Custom tutorial graphic produces a visible mesh: " + graphic.GetType().Name);
                    }
                });
                Object.DestroyImmediate(circleObject);
                var coach = overlay.transform.Find("Coach").gameObject;
                Check(!coach.transform.Find("Claim tutorial gift").gameObject.activeSelf, "Quest panel has no workshop shortcut");
                var body = coach.transform.Find("Instructions").GetComponent<TMP_Text>();
                var theme = Resources.Load<MineArena.UI.TutorialTheme>("UI/TutorialTheme");
                Check(theme != null && coach.GetComponent<UnityEngine.UI.Image>().sprite == theme.Panel, "Upper coach uses the shared light window frame");
                var marker = overlay.transform.Find("Target arrow").GetComponent<TMP_Text>();
                Check(marker.font == theme.HeadingFont && marker.fontSize == 40 && marker.color == Color.yellow, "Distance label uses forty-point UI font and pure yellow");
                Check(body.font != null && body.font == theme.BodyFont, "Tutorial uses the project's Cyrillic font");
                foreach (TutorialStep step in TutorialService.Steps)
                    if (step != TutorialStep.Complete)
                        Check(body.GetPreferredValues(TutorialService.Instructions(step), 792, 1000).y <= 64, "Guidance fits the panel: " + step);
                coach.transform.Find("Title").GetComponent<TMP_Text>().text = "ПЕРВЫЕ ШАГИ • 6 / 14";
                coach.transform.Find("Instructions").GetComponent<TMP_Text>().text = TutorialService.Instructions(TutorialStep.BuildSmith);
                coach.transform.Find("Claim tutorial gift").gameObject.SetActive(false);
                GameUiBuilder.Render(coach, "Documentation/UI/Tutorial-Coach.png");
                coach.transform.Find("Title").GetComponent<TMP_Text>().text = "ПЕРВЫЕ ШАГИ • 14 / 14";
                body.text = TutorialService.Instructions(TutorialStep.Gift);
                coach.transform.Find("Claim tutorial gift").gameObject.SetActive(false);
                GameUiBuilder.Render(coach, "Documentation/UI/Tutorial-Gift.png");
                var popup = overlay.transform.Find("Tutorial popup").gameObject;
                var popupBody = popup.transform.Find("Popup instructions").GetComponent<TMP_Text>();
                popup.transform.Find("Popup title").GetComponent<TMP_Text>().text = "ПЕРВЫЕ ШАГИ • 1 / 14";
                popupBody.text = TutorialService.Instructions(TutorialStep.Portal);
                popup.transform.Find("Confirm/Label").GetComponent<TMP_Text>().text = "Понятно!";
                popup.transform.Find("Step illustration").GetComponent<UnityEngine.UI.Image>().sprite = TutorialService.StepIllustration(TutorialStep.Portal);
                Check(popup.GetComponent<UnityEngine.UI.Image>().sprite != null, "Popup uses the existing illustrated UI frame");
                foreach (TutorialStep step in TutorialService.Steps)
                    if (step != TutorialStep.Complete) Check(popupBody.GetPreferredValues(TutorialService.Instructions(step), 724, 1000).y <= 132, "Popup copy fits: " + step);
                GameUiBuilder.Render(popup, "Documentation/UI/Tutorial-Popup.png");
                typeof(TutorialService).GetMethod("UpdateIllustration", Private).Invoke(tutor, new object[] { TutorialStep.BuildSmith });
                popupBody.text = TutorialService.Instructions(TutorialStep.BuildSmith);
                popup.transform.Find("Popup title").GetComponent<TMP_Text>().text = popup.transform.Find("Popup title").GetComponent<TMP_Text>().text.Replace("1 / 14", "6 / 14");
                GameUiBuilder.Render(popup, "Documentation/UI/Tutorial-Building.png");
                typeof(TutorialService).GetMethod("UpdateIllustration", Private).Invoke(tutor, new object[] { TutorialStep.Mine });
                popupBody.text = TutorialService.Instructions(TutorialStep.Mine);
                popup.transform.Find("Popup title").GetComponent<TMP_Text>().text = "ПЕРВЫЕ ШАГИ • 3 / 14";
                GameUiBuilder.Render(popup, "Documentation/UI/Tutorial-Mining.png");

                SetProgress(new PlayerProgress("popup-queue")); progress.TutorialProgress.Initialized = true; progress.TutorialProgress.Step = TutorialStep.BuildSmith;
                typeof(TutorialService).GetField("_acknowledged", Private).SetValue(tutor, TutorialStep.Portal);
                var ui = go.AddComponent<UIManager>(); managers[typeof(UIManager)] = ui;
                var canvas = new GameObject("Queue test canvas", typeof(Canvas)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvas, scene);
                typeof(UIManager).GetField("_mainCanvas", Private).SetValue(ui, canvas.GetComponent<Canvas>());
                var dormant = new GameObject("Queue test building"); dormant.SetActive(false); dormant.transform.SetParent(canvas.transform);
                var buildingView = dormant.AddComponent<BuildingWindow>();
                ((Dictionary<Type, Devotion.SDK.Base.BaseWindow>)typeof(UIManager).GetField("_cachedWindows", Private).GetValue(ui))[typeof(BuildingWindow)] = buildingView;
                Check(ui.OpenWindow<BuildingWindow>() == buildingView && !dormant.activeSelf, "Window request returns an inactive instance while popup is pending");
                ui.OpenWindow<BuildingWindow>();
                var queue = (Dictionary<Type, Devotion.SDK.Base.BaseWindow>)typeof(UIManager).GetField("_tutorialPending", Private).GetValue(ui);
                Check(queue.Count == 1, "Repeated trigger requests are coalesced");
                typeof(TutorialService).GetField("_shown", Private).SetValue(tutor, TutorialStep.BuildSmith);
                typeof(TutorialService).GetField("_popupOpenedAt", Private).SetValue(tutor, Time.unscaledTime - 1);
                typeof(TutorialService).GetField("_paused", Private).SetValue(tutor, true);
                typeof(TutorialService).GetField("_oldTimeScale", Private).SetValue(tutor, .75f);
                Time.timeScale = 0;
                popup.SetActive(true); tutor.ConfirmPopup();
                Check(Mathf.Approximately(Time.timeScale, .75f) && TutorialService.BlocksInput, "Confirmation restores the previous clock speed and suppresses click-through");
                Time.timeScale = previousTimeScale;
                typeof(UIManager).GetMethod("Update", Private).Invoke(ui, null);
                Check(!TutorialService.AwaitingConfirmation && dormant.activeSelf && queue.Count == 0, "Confirming the popup releases exactly one queued window");
                ui.CloseWindow<BuildingWindow>();
                typeof(TutorialService).GetField("_acknowledged", Private).SetValue(tutor, TutorialStep.Portal);
                ui.OpenWindow<BuildingWindow>(); ui.CloseWindow<BuildingWindow>();
                Check(queue.Count == 0 && !dormant.activeSelf, "Leaving a trigger cancels its deferred window");
                ui.OpenWindow<BuildingWindow>();
                progress.TutorialProgress.Step = TutorialStep.CraftSword;
                typeof(TutorialService).GetField("_acknowledged", Private).SetValue(tutor, TutorialStep.CraftSword);
                typeof(UIManager).GetMethod("Update", Private).Invoke(ui, null);
                Check(queue.Count == 0 && !dormant.activeSelf, "Outdated stage windows never reopen");
                var resolveInput = typeof(UIManager).GetMethod("ResolveInputSystem", Private);
                var input = (EventSystem)resolveInput.Invoke(ui, new object[] { Array.Empty<EventSystem>() });
                input.UpdateModules();
                Check(input.isActiveAndEnabled && input.GetComponent<StandaloneInputModule>().isActiveAndEnabled && input.transform.IsChildOf(ui.transform), "Scene without EventSystem gets a persistent active input module");
                progress.TutorialProgress.Step = TutorialStep.Mine;
                typeof(TutorialService).GetField("_shown", Private).SetValue(tutor, TutorialStep.Mine);
                typeof(TutorialService).GetField("_popupOpenedAt", Private).SetValue(tutor, Time.unscaledTime - 1);
                typeof(TutorialService).GetField("_paused", Private).SetValue(tutor, true);
                typeof(TutorialService).GetField("_oldTimeScale", Private).SetValue(tutor, .75f);
                popup.SetActive(true); overlay.transform.Find("Popup backdrop").gameObject.SetActive(true);
                // Screen-space overlay canvases do not receive native draw depths in Edit Mode.
                // Render the same live hierarchy through a camera before testing actual UI hit ordering.
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(overlay, scene);
                var rayCameraObject = new GameObject("Raycast camera", typeof(Camera)); rayCameraObject.transform.SetParent(go.transform);
                var rayCamera = rayCameraObject.GetComponent<Camera>(); rayCamera.orthographic = true; rayCamera.orthographicSize = 540; rayCamera.transform.position = new Vector3(0, 0, -100);
                rayCamera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                raycastTarget = new RenderTexture(1920, 1080, 24); rayCamera.targetTexture = raycastTarget;
                var overlayCanvas = overlay.GetComponent<Canvas>(); overlayCanvas.renderMode = RenderMode.ScreenSpaceCamera; overlayCanvas.worldCamera = rayCamera; overlayCanvas.planeDistance = 1;
                Time.timeScale = 0; Canvas.ForceUpdateCanvases();
                rayCamera.Render();
                var confirm = popup.transform.Find("Confirm").gameObject;
                var pointer = new PointerEventData(input) { button = PointerEventData.InputButton.Left,
                    position = RectTransformUtility.WorldToScreenPoint(rayCamera, confirm.transform.position) };
                // BaseRaycaster registration is Play Mode-only; exercise its actual hit test directly in this fixture.
                var hits = new List<RaycastResult>(); overlay.GetComponent<UnityEngine.UI.GraphicRaycaster>().Raycast(pointer, hits);
                Check(hits.Count > 0 && hits[0].gameObject == confirm, "Step three confirmation receives the top UI raycast while time is paused: hits=" + string.Join(",", hits.Select(h => h.gameObject.name)) + "; point=" + pointer.position + "; screen=" + Screen.width + "x" + Screen.height + "; depth=" + confirm.GetComponent<UnityEngine.UI.Image>().depth);
                ExecuteEvents.Execute(confirm, pointer, ExecuteEvents.pointerClickHandler);
                Check(!TutorialService.AwaitingConfirmation && !popup.activeSelf && Mathf.Approximately(Time.timeScale, .75f), "Real pointer-click handler dismisses mining popup and unpauses gameplay");
                Time.timeScale = previousTimeScale;
                input.gameObject.SetActive(false);
                var sceneInputObject = new GameObject("Scene input test", typeof(EventSystem), typeof(StandaloneInputModule)); sceneInputObject.transform.SetParent(go.transform);
                var sceneInput = sceneInputObject.GetComponent<EventSystem>();
                var selected = (EventSystem)resolveInput.Invoke(ui, new object[] { new[] { sceneInput } });
                Check(selected == sceneInput && !input.gameObject.activeSelf, "Returning to a scene with its own input disables the fallback");
                Object.DestroyImmediate(sceneInputObject);
                selected = (EventSystem)resolveInput.Invoke(ui, new object[] { Array.Empty<EventSystem>() });
                Check(selected == input && input.gameObject.activeSelf, "Repeated scene changes reuse one fallback input system");
                ui.CloseAllWindows();
                progress.TutorialProgress.Step = TutorialStep.Complete;
                var canvasField = typeof(UIManager).GetField("_mainCanvas", Private);
                var ensureCanvas = typeof(UIManager).GetMethod("EnsureMainCanvas", Private);
                canvasField.SetValue(ui, null);
                ensureCanvas.Invoke(ui, new object[] { Array.Empty<Canvas>() });
                var recoveredCanvas = (Canvas)canvasField.GetValue(ui);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(recoveredCanvas.gameObject, scene);
                Check(recoveredCanvas.isActiveAndEnabled && recoveredCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() != null, "Missing scene canvas is replaced with interactive UI canvas");
                var selectionPrefab = AssetDatabase.LoadAssetAtPath<SelectLevelWindow>("Assets/DevotionSDK/Prefabs/UI/SelectLevelWindow.prefab");
                typeof(UIManager).GetField("_windows", Private).SetValue(ui, new List<Devotion.SDK.Base.BaseWindow> { selectionPrefab });
                var selection = ui.OpenWindow<SelectLevelWindow>();
                Check(selection != null && selection.gameObject.activeInHierarchy && selection.transform.parent == recoveredCanvas.transform, "Portal level window opens on recovered canvas");
                Check(!TutorialService.WorldGuidanceVisible, "Opening the portal window hides world guidance arrows");
                ui.CloseWindow<SelectLevelWindow>();
                recoveredCanvas.gameObject.SetActive(false);
                canvasField.SetValue(ui, null);
                ensureCanvas.Invoke(ui, new object[] { new[] { recoveredCanvas } });
                Check(recoveredCanvas.gameObject.activeSelf && ui.OpenWindow<SelectLevelWindow>() == selection, "Inactive main canvas and cached portal window are reused");
                Object.DestroyImmediate(recoveredCanvas.gameObject);
                ensureCanvas.Invoke(ui, new object[] { Array.Empty<Canvas>() });
                recoveredCanvas = (Canvas)canvasField.GetValue(ui);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(recoveredCanvas.gameObject, scene);
                selection = ui.OpenWindow<SelectLevelWindow>();
                Check(selection != null && selection.gameObject.activeInHierarchy, "Destroyed canvas and cached portal window are recreated");
                Object.DestroyImmediate(recoveredCanvas.gameObject);
                File.WriteAllLines("Documentation/tutorial-validation.txt", lines);
                Debug.Log("Tutorial validation: " + lines.Count + " checks passed.");
            }
            finally
            {
                if (overlay != null) Object.DestroyImmediate(overlay);
                typeof(GameRoot).GetProperty("Instance").SetValue(null, null);
                EditorSceneManager.ClosePreviewScene(scene);
                if (raycastTarget != null) Object.DestroyImmediate(raycastTarget);
                Time.timeScale = previousTimeScale;
                if (previousEventSystem != null) EventSystem.current = previousEventSystem;
            }
        }
    }
}
