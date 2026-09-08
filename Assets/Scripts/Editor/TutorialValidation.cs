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
                Check(routine.MoveNext() && routine.Current == null, "Enemy spawning waits for resource collection");
                TutorialService.EnemyKilled();
                var wood = config.ItemDatabase.GetItemConfig("WoodOak");
                TutorialService.CollectedResource(wood);
                Check(progress.TutorialProgress.Step == TutorialStep.Mine, "Kill or loose pickup cannot skip the mining step");
                TutorialService.MinedBlock(); TutorialService.CollectedResource(wood);
                Check(progress.TutorialProgress.Step == TutorialStep.Kill && routine.MoveNext() && routine.Current is WaitForSeconds, "Mining plus pickup releases the combat wave");
                var saved = JsonUtility.ToJson(progress);
                SetProgress(JsonUtility.FromJson<PlayerProgress>(saved)); tutor.Initialize();
                Check(progress.TutorialProgress.Step == TutorialStep.Portal && progress.TutorialProgress.Collected, "Restart in the lobby retains completed mining and restores portal guidance");
                TutorialService.EnterPortal(); TutorialService.BeginLevel();
                Check(progress.TutorialProgress.Step == TutorialStep.Kill, "Retry continues with the unfinished fight");
                TutorialService.EnemyKilled();
                Check(progress.TutorialProgress.Step == TutorialStep.Exit, "Enemy death unlocks extraction guidance");
                var reward = new Dictionary<ItemConfig, int>(); TutorialService.EnsureFirstBuildingReward(reward);
                var workshop = TutorialService.Workshop; var costs = workshop.GetLevelByNumber(1).RequiredResources;
                Check(costs.All(c => reward[c.Resource] >= c.Amount), "Even an empty completion reward covers every workshop cost");
                foreach (var entry in reward) inventory.AddItemById(entry.Key.Name, entry.Value);
                TutorialService.SetStep(TutorialStep.Build);
                Check(!buildings.TryBuild(config.BuildingsDatabase.AllBuildings.First(b => b != workshop), go.transform) && !buildings.TryUpgrade(workshop, go.transform), "Other buildings and upgrades are rejected");
                Check(inventory.TryConsumeResources(costs), "Guaranteed completion grant pays for the workshop with no mined resources or ads");
                progress.BuildingProgress.SavedBuildings[workshop.BuildingName] = new BuildingSaveData(1, null);
                tutor.Initialize();
                Check(progress.TutorialProgress.Step == TutorialStep.Craft, "Saved construction recovers the crafting checkpoint");
                typeof(TutorialService).GetField("_acknowledged", Private).SetValue(tutor, TutorialStep.Craft);
                var production = go.GetComponent<CraftProductionService>();
                var planks = config.ItemDatabase.GetItemConfig("Planks");
                Check(!production.TryStart(config.ItemDatabase.GetItemConfig("Stick"), 1), "Other recipes cannot consume tutorial funds");
                Check(production.TryStart(planks, planks.CraftAmount), "Resources left after construction pay for the first planks");
                Check(progress.TutorialProgress.Step == TutorialStep.Craft && !TutorialService.ClaimGift(), "Starting a craft does not complete the tutorial early");
                SetProgress(JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(progress))); inventory.InitManager();
                Check(production.Job != null && progress.TutorialProgress.Step == TutorialStep.Craft, "Paid tutorial craft survives save/load");
                production.Job.ReadyUtcTicks = DateTime.UtcNow.AddSeconds(-1).Ticks;
                typeof(CraftProductionService).GetMethod("Update", Private).Invoke(production, null);
                Check(progress.TutorialProgress.Step == TutorialStep.Gift && inventory.GetItemAmount("Planks") == planks.CraftAmount, "Craft completion grants planks and unlocks the gift together");
                Check(TutorialService.ClaimGift(), "Gift can be claimed after the completed craft");
                int giftWood = inventory.GetItemAmount("WoodOak"), giftStone = inventory.GetItemAmount("Stone");
                Check(giftWood == 16 && giftStone == 12 && inventory.GetItemAmount("HealingPotion") == 2, "Gift includes sixteen oak, twelve stone and two healing potions");
                SetProgress(JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(progress))); inventory.InitManager(); tutor.Initialize();
                Check(!TutorialService.Active && !TutorialService.ClaimGift() && inventory.GetItemAmount("WoodOak") == giftWood, "Saved completion prevents duplicate gift grants");
                wave.Configure(config.Levels[0].EncounterWaves);
                Check(wave.TotalMobCount == normalCount && TutorialService.AllowLevel(1) && TutorialService.AllowCraft(planks), "Free play restores normal encounters and actions");
                SetProgress(new PlayerProgress("existing-player")); progress.LevelsProgress.UnlockLevel(1); tutor.Initialize();
                Check(!TutorialService.Active, "Existing expedition progress is not forced through onboarding");
                SetProgress(new PlayerProgress("existing-settlement")); progress.BuildingProgress.SavedBuildings[workshop.BuildingName] = new BuildingSaveData(1, null); tutor.Initialize();
                Check(!TutorialService.Active, "Existing settlement is not forced through onboarding");
                var arena = config.Levels[0].LevelPrefab.GetComponent<Arena>();
                Check(arena != null && arena.PlayerSpawnPosition != null && arena.PortalSpawnPoint != null && arena.GetComponentsInChildren<OreSpawnPoint>(true).Length > 0, "Tutorial arena has player, ore and extraction spawn points");
                Check(config.Levels[0].ResourceSpawnConfigs.Any(r => r.Resource != null && r.Resource.GetComponent<InteractableObject>()?.IsMineable == true), "First level has a mineable resource prefab");
                foreach (TutorialStep step in Enum.GetValues(typeof(TutorialStep)))
                    if (step != TutorialStep.Complete) Check(!string.IsNullOrWhiteSpace(TutorialService.Instructions(step)), "Guidance text for " + step);
                typeof(TutorialService).GetMethod("CreateOverlay", Private).Invoke(tutor, null);
                overlay = (GameObject)typeof(TutorialService).GetField("_overlay", Private).GetValue(tutor);
                var coach = overlay.transform.Find("Coach").gameObject;
                var body = coach.transform.Find("Instructions").GetComponent<TMP_Text>();
                var theme = Resources.Load<MineArena.UI.TutorialTheme>("UI/TutorialTheme");
                Check(theme != null && coach.GetComponent<UnityEngine.UI.Image>().sprite == theme.Panel, "Upper coach uses the shared light window frame");
                var marker = overlay.transform.Find("Target arrow").GetComponent<TMP_Text>();
                Check(marker.font == theme.HeadingFont && marker.fontSize == 40 && marker.color == Color.yellow, "Distance label uses forty-point UI font and pure yellow");
                Check(body.font != null && body.font.name.StartsWith("Rubik"), "Tutorial uses the project's Cyrillic font");
                foreach (TutorialStep step in Enum.GetValues(typeof(TutorialStep)))
                    if (step != TutorialStep.Complete)
                        Check(body.GetPreferredValues(TutorialService.Instructions(step), 792, 1000).y <= 64, "Guidance fits the panel: " + step);
                coach.transform.Find("Title").GetComponent<TMP_Text>().text = "ПЕРВЫЕ ШАГИ • 6 / 8";
                coach.transform.Find("Instructions").GetComponent<TMP_Text>().text = TutorialService.Instructions(TutorialStep.Build);
                coach.transform.Find("Claim tutorial gift").gameObject.SetActive(false);
                GameUiBuilder.Render(coach, "Documentation/UI/Tutorial-Coach.png");
                coach.transform.Find("Title").GetComponent<TMP_Text>().text = "ПЕРВЫЕ ШАГИ • 8 / 8";
                body.text = TutorialService.Instructions(TutorialStep.Gift);
                coach.transform.Find("Claim tutorial gift").gameObject.SetActive(true);
                GameUiBuilder.Render(coach, "Documentation/UI/Tutorial-Gift.png");
                var popup = overlay.transform.Find("Tutorial popup").gameObject;
                var popupBody = popup.transform.Find("Popup instructions").GetComponent<TMP_Text>();
                popup.transform.Find("Popup title").GetComponent<TMP_Text>().text = "ПЕРВЫЕ ШАГИ • 1 / 8";
                popupBody.text = TutorialService.Instructions(TutorialStep.Portal);
                popup.transform.Find("Confirm/Label").GetComponent<TMP_Text>().text = "Понятно!";
                popup.transform.Find("Step illustration").GetComponent<UnityEngine.UI.Image>().sprite = TutorialService.StepIllustration(TutorialStep.Portal);
                Check(popup.GetComponent<UnityEngine.UI.Image>().sprite != null, "Popup uses the existing illustrated UI frame");
                foreach (TutorialStep step in Enum.GetValues(typeof(TutorialStep)))
                    if (step != TutorialStep.Complete) Check(popupBody.GetPreferredValues(TutorialService.Instructions(step), 724, 1000).y <= 132, "Popup copy fits: " + step);
                GameUiBuilder.Render(popup, "Documentation/UI/Tutorial-Popup.png");

                SetProgress(new PlayerProgress("popup-queue")); progress.TutorialProgress.Initialized = true; progress.TutorialProgress.Step = TutorialStep.Build;
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
                typeof(TutorialService).GetField("_shown", Private).SetValue(tutor, TutorialStep.Build);
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
                progress.TutorialProgress.Step = TutorialStep.Craft;
                typeof(TutorialService).GetField("_acknowledged", Private).SetValue(tutor, TutorialStep.Craft);
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
