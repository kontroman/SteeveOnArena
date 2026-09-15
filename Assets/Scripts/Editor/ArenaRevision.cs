using System;
using System.IO;
using System.Linq;
using MineArena.Items;
using MineArena.Levels;
using MineArena.PlayerSystem;
using MineArena.SDK.UI;
using MineArena.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static class ArenaRevision
    {
        private const string Hud = "Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab";
        private const string Mine = "Assets/Prefabs/Arenas/LocationMine.prefab";

        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/arena-revision.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool mineOnly = File.ReadAllText(request).Trim() == "mine";
            File.Delete(request);
            try { if (mineOnly) ValidateMineLayers(); else Build(); }
            catch (Exception e) { File.AppendAllText("Documentation/arena-revision-validation.txt", "FAIL " + e); Debug.LogException(e); }
        };

        private static void ValidateMineLayers()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Mine);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var arena = instance.GetComponent<Arena>();
                var ores = instance.GetComponentsInChildren<InteractableObject>(true).Where(x => x.IsMineable).ToArray();
                var points = instance.GetComponentsInChildren<OreSpawnPoint>(true);
                if (ores.Length != 39 || points.Length != 39 || !arena.UseAuthoredResources)
                    throw new Exception("Expected 39 restored upper resources and authored-only spawning");
                foreach (var ore in ores)
                {
                    var marker = ore.GetComponentInParent<OreSpawnPoint>();
                    if (marker == null || ore.transform.position.y <= marker.transform.position.y + .5f)
                        throw new Exception("Resource is not above its lower spawn marker: " + ore.name);
                }
                var go = new GameObject("Mine generation validation");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var level = go.AddComponent<MineArena.Controllers.LevelController>();
                const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                typeof(MineArena.Controllers.LevelController).GetField("_currentArena", flags).SetValue(level, arena);
                typeof(MineArena.Controllers.LevelController).GetField("_currentConfig", flags).SetValue(level,
                    AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/ScriptableObjects/Levels/Level3_Mine.asset"));
                var before = Resources.FindObjectsOfTypeAll<InteractableObject>();
                level.GenerateOres(); level.GenerateOres();
                var added = Resources.FindObjectsOfTypeAll<InteractableObject>().Except(before).ToArray();
                foreach (var resource in added) Object.DestroyImmediate(resource.gameObject);
                if (added.Length != 0) throw new Exception("Generated a lower resource layer");
                foreach (var path in new[] { "Assets/Prefabs/Arenas/LocationVillage.prefab", "Assets/Prefabs/Arenas/LocationForest.prefab" })
                {
                    var other = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (other != null && other.GetComponent<Arena>().UseAuthoredResources)
                        throw new Exception("Other arena generation was disabled");
                }
                File.WriteAllText("Documentation/mine-layer-validation.txt", "PASS Restored all 39 original upper resources.\nPASS Each resource is above its lower spawn marker.\nPASS Two GenerateOres calls add no lower resources.\nPASS Other arenas retain procedural spawning.\n");
            }
            finally { UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene); }
        }

        [MenuItem("MineArena/UI/Apply Arena Revision")]
        public static void Build()
        {
            // Keep unsaved scene edits while updating the live player's reference too.
            var arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Objects/Projectile/Arrow.prefab");
            foreach (var attack in Object.FindObjectsOfType<PlayerAttack>(true))
            {
                var serialized = new SerializedObject(attack);
                serialized.FindProperty("_arrowProjectilePrefab").objectReferenceValue = arrowPrefab;
                serialized.ApplyModifiedProperties();
            }
            var mine = PrefabUtility.LoadPrefabContents(Mine);
            try
            {
                var arenaData = new SerializedObject(mine.GetComponent<Arena>());
                arenaData.FindProperty("_useAuthoredResources").boolValue = true;
                arenaData.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(mine, Mine);
            }
            finally { PrefabUtility.UnloadPrefabContents(mine); }

            var hud = PrefabUtility.LoadPrefabContents(Hud);
            try
            {
                var old = hud.transform.Find("ArenaExit");
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var font = hud.GetComponentInChildren<TMP_Text>(true).font;
                var root = Rect(hud.transform, "ArenaExit", Vector2.zero, Vector2.zero);
                Stretch(root);
                var exit = Button(root, font, "Exit", "Arena.Exit", new Vector2(-155, -170), new Vector2(250, 58), new Color32(139, 61, 48, 255));
                var exitRect = (RectTransform)exit.transform;
                exitRect.anchorMin = exitRect.anchorMax = Vector2.one;
                ArenaExitIconBuilder.Style(exit);
                var shade = Rect(root, "Confirmation", Vector2.zero, Vector2.zero);
                Stretch(shade);
                shade.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .72f);
                var canvas = shade.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 1000;
                shade.gameObject.AddComponent<GraphicRaycaster>();
                var panel = Rect(shade, "Panel", Vector2.zero, new Vector2(760, 370));
                panel.gameObject.AddComponent<Image>().color = new Color32(35, 49, 58, 255);
                Label(panel, font, "Title", "Arena.ExitTitle", new Vector2(0, 125), new Vector2(690, 60), 32);
                Label(panel, font, "Warning", "Arena.ExitWarning", new Vector2(0, 20), new Vector2(670, 140), 25);
                var cancel = Button(panel, font, "Cancel", "Arena.Stay", new Vector2(-175, -120), new Vector2(310, 62), new Color32(66, 125, 103, 255));
                var confirm = Button(panel, font, "Confirm", "Arena.ConfirmExit", new Vector2(175, -120), new Vector2(310, 62), new Color32(139, 61, 48, 255));
                ArenaExitIconBuilder.StyleConfirmation(shade);
                shade.gameObject.SetActive(false);
                var ui = root.gameObject.AddComponent<ArenaExitUI>();
                var data = new SerializedObject(ui);
                data.FindProperty("_exitButton").objectReferenceValue = exit;
                data.FindProperty("_confirmation").objectReferenceValue = shade.gameObject;
                data.FindProperty("_confirmButton").objectReferenceValue = confirm;
                data.FindProperty("_cancelButton").objectReferenceValue = cancel;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(hud, Hud);
            }
            finally { PrefabUtility.UnloadPrefabContents(hud); }
            const string progressPath = "Assets/DevotionSDK/Prefabs/UI/LevelProgressWindow.prefab";
            var progress = PrefabUtility.LoadPrefabContents(progressPath);
            try
            {
                var text = (TMP_Text)new SerializedObject(progress.GetComponent<MineArena.Windows.LevelProgressWindow>()).FindProperty("_progressText").objectReferenceValue;
                text.rectTransform.anchoredPosition = new Vector2(382, -8);
                text.rectTransform.sizeDelta = new Vector2(200, 25);
                text.enableAutoSizing = true;
                text.fontSizeMin = 14; text.fontSizeMax = 18;
                text.enableWordWrapping = false;
                PrefabUtility.SaveAsPrefabAsset(progress, progressPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(progress); }
            AssetDatabase.SaveAssets();
            Validate();
        }

        private static void Validate()
        {
            var mine = AssetDatabase.LoadAssetAtPath<GameObject>(Mine);
            int authored = mine.GetComponentsInChildren<InteractableObject>(true).Count(x => x.IsMineable);
            if (authored != 39 || !mine.GetComponent<Arena>().UseAuthoredResources) throw new Exception("Mine must retain 39 upper resources and disable lower generated resources");
            int points = mine.GetComponentsInChildren<OreSpawnPoint>(true).Length;
            if (points == 0) throw new Exception("Mine spawn points missing");
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenes/PlayerV2.0.prefab");
            var attack = player.GetComponentInChildren<PlayerAttack>(true);
            var arrow = new SerializedObject(attack).FindProperty("_arrowProjectilePrefab").objectReferenceValue as GameObject;
            if (arrow == null || arrow.GetComponent<Arrow>() == null || arrow.GetComponentsInChildren<Renderer>().Length == 0) throw new Exception("Player arrow missing");
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>(Hud);
            var ui = hud.GetComponentInChildren<ArenaExitUI>(true);
            var data = new SerializedObject(ui);
            foreach (string field in new[] { "_exitButton", "_confirmation", "_confirmButton", "_cancelButton" })
                if (data.FindProperty(field).objectReferenceValue == null) throw new Exception("Missing exit UI field " + field);
            File.WriteAllText("Documentation/arena-revision-validation.txt", $"PASS Player arrow prefab and renderers\nPASS Mine: retained {authored} upper resources and {points} markers; lower generated layer disabled\nPASS Arena exit prefab bindings\n");
            GameUiBuilder.Render(hud, "Documentation/UI/ArenaExit.png", preview =>
            {
                preview.transform.Find("GiftNavigation")?.gameObject.SetActive(false);
                preview.transform.Find("ArenaExit/Confirmation").gameObject.SetActive(true);
            });
            ValidateBehavior(hud);
        }

        private static void ValidateBehavior(GameObject hudPrefab)
        {
            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var previousRoot = Devotion.SDK.Controllers.GameRoot.Instance;
            var previousLevel = MineArena.Controllers.LevelController.Current;
            float previousTime = Time.timeScale;
            var dictionary = (System.Collections.Generic.Dictionary<string, string>)typeof(Devotion.SDK.Services.Localization.LocalizationService)
                .GetField("_localizationDictionary", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
            var savedDictionary = new System.Collections.Generic.Dictionary<string, string>(dictionary);
            void Check(bool condition, string message) { if (!condition) throw new Exception(message); File.AppendAllText("Documentation/arena-revision-validation.txt", "PASS " + message + "\n"); }
            try
            {
                var go = new GameObject("Arena validation fixture");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var root = go.AddComponent<Devotion.SDK.Controllers.GameRoot>();
                typeof(Devotion.SDK.Controllers.GameRoot).GetProperty("Instance").SetValue(null, root);
                var progress = new Devotion.SDK.Services.SaveSystem.Progress.PlayerProgress("arena-validation");
                progress.TutorialProgress.Initialized = true; progress.TutorialProgress.Step = Managers.TutorialStep.Complete;
                typeof(Devotion.SDK.Controllers.GameRoot).GetField("playerProgress", flags).SetValue(root, progress);
                typeof(Devotion.SDK.Controllers.GameRoot).GetField("gameConfig", flags).SetValue(root,
                    AssetDatabase.LoadAssetAtPath<MineArena.Structs.GameConfig>("Assets/ScriptableObjects/GameConfig.asset"));
                var inventory = go.AddComponent<MineArena.Managers.InventoryManager>();
                var managers = (System.Collections.Generic.Dictionary<Type, Devotion.SDK.Managers.BaseManager>)
                    typeof(Devotion.SDK.Controllers.GameRoot).GetField("_managers", flags).GetValue(root);
                managers[typeof(MineArena.Managers.InventoryManager)] = inventory;
                var level = go.AddComponent<MineArena.Controllers.LevelController>();
                var current = typeof(MineArena.Controllers.LevelController).GetProperty("Current");
                current.SetValue(null, level);
                var hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, scene);
                var playing = hud.GetComponent<Devotion.SDK.UI.PlayingWindow>();
                playing.RefreshTutorialVisibility();
                Check(!hud.transform.Find("GiftNavigation").gameObject.activeSelf, "Arena hides gift navigation");
                Check(hud.transform.Find("PlayerPanel").gameObject.activeSelf, "Arena retains player vitals");
                current.SetValue(null, null); playing.RefreshTutorialVisibility();
                Check(hud.transform.Find("GiftNavigation").gameObject.activeSelf, "Lobby restores gift navigation without reopening HUD");
                current.SetValue(null, level);
                var exit = hud.GetComponentInChildren<ArenaExitUI>(true);
                var confirmation = (GameObject)new SerializedObject(exit).FindProperty("_confirmation").objectReferenceValue;
                var exitButton = (Button)new SerializedObject(exit).FindProperty("_exitButton").objectReferenceValue;
                var refreshExit = typeof(ArenaExitUI).GetMethod("RefreshVisibility", flags);
                progress.TutorialProgress.Step = Managers.TutorialStep.EquipSword;
                refreshExit.Invoke(exit, null);
                Check(!exitButton.gameObject.activeSelf, "Tutorial hides arena exit icon");
                typeof(ArenaExitUI).GetMethod("OpenConfirmation", flags).Invoke(exit, null);
                Check(!confirmation.activeSelf, "Tutorial blocks direct exit confirmation");
                progress.TutorialProgress.Step = Managers.TutorialStep.Complete;
                refreshExit.Invoke(exit, null);
                Check(exitButton.gameObject.activeSelf, "Completing tutorial restores arena exit icon");
                Time.timeScale = .75f;
                typeof(ArenaExitUI).GetMethod("OpenConfirmation", flags).Invoke(exit, null);
                Check(confirmation.activeSelf && Time.timeScale == 0, "Exit warning pauses the arena");
                typeof(ArenaExitUI).GetMethod("Cancel", flags).Invoke(exit, null);
                Check(!confirmation.activeSelf && Time.timeScale == .75f, "Cancel restores previous game speed");
                var stone = AssetDatabase.LoadAssetAtPath<ItemConfig>("Assets/ScriptableObjects/Configs/Drops/Stone.asset");
                var coal = AssetDatabase.LoadAssetAtPath<ItemConfig>("Assets/ScriptableObjects/Configs/Drops/CoalItem.asset");
                progress.InventoryProgress.SavedResources[stone.Name] = 15;
                progress.InventoryProgress.SavedResources[coal.Name] = 2;
                progress.InventoryProgress.SavedResources["WoodSword"] = 1;
                level.RegisterCollectedResource(stone, 5); level.RegisterCollectedResource(coal, 4);
                typeof(MineArena.Controllers.LevelController).GetMethod("DiscardCollectedResources", flags).Invoke(level, null);
                Check(progress.InventoryProgress.SavedResources[stone.Name] == 10 && !progress.InventoryProgress.SavedResources.ContainsKey(coal.Name), "Abandon removes collected loot, clamping already spent resources to zero");
                Check(progress.InventoryProgress.SavedResources["WoodSword"] == 1 && level.CollectedResources.Count == 0, "Abandon retains equipment and clears expedition totals");
                typeof(MineArena.Controllers.LevelController).GetMethod("DiscardCollectedResources", flags).Invoke(level, null);
                Check(progress.InventoryProgress.SavedResources[stone.Name] == 10, "Loot discard cannot charge twice");
                var progressPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/UI/LevelProgressWindow.prefab");
                var window = ((GameObject)PrefabUtility.InstantiatePrefab(progressPrefab, scene)).GetComponent<MineArena.Windows.LevelProgressWindow>();
                var label = (TMP_Text)new SerializedObject(window).FindProperty("_progressText").objectReferenceValue;
                foreach (var file in Directory.GetFiles("Assets/Resources/Localization", "*.json"))
                {
                    var catalog = JsonUtility.FromJson<Devotion.SDK.Services.Localization.LocalizationData>(File.ReadAllText(file)).ToDictionary();
                    foreach (var key in new[] { "Arena.GoToPortal", "Arena.Exit", "Arena.ExitTitle", "Arena.ExitWarning", "Arena.Stay", "Arena.ConfirmExit" })
                        Check(catalog.ContainsKey(key) && !string.IsNullOrWhiteSpace(catalog[key]), Path.GetFileNameWithoutExtension(file) + " " + key);
                    dictionary["Arena.GoToPortal"] = catalog["Arena.GoToPortal"];
                    window.SetProgress(10, 10);
                    Check(label.text == catalog["Arena.GoToPortal"], "Full clear displays portal instruction in " + Path.GetFileNameWithoutExtension(file));
                }
                window.SetProgress(9, 10); Check(label.text == "9/10", "Partial clear retains counter");
                window.SetProgress(0, 0); Check(label.text == "0/0", "Empty total does not falsely report completion");
                var bowObject = new GameObject("Bow shot fixture");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(bowObject, scene);
                bowObject.transform.position = new Vector3(10000, 10000, 10000);
                var attack = bowObject.AddComponent<PlayerAttack>();
                var attackData = new SerializedObject(attack);
                attackData.FindProperty("_arrowProjectilePrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Objects/Projectile/Arrow.prefab");
                attackData.ApplyModifiedPropertiesWithoutUndo();
                var config = ScriptableObject.CreateInstance<AttackConfig>();
                Arrow shot = null;
                try
                {
                    config.BaseDamage = 25; config.AttackableLayers = 1 << 8;
                    typeof(PlayerAttack).GetField("_isAttacking", flags).SetValue(attack, true);
                    typeof(PlayerAttack).GetMethod("PreparePendingBowShot", flags).Invoke(attack, new object[] { config, bowObject.transform.position + Vector3.forward * 20 });
                    var before = Object.FindObjectsOfType<Arrow>().ToArray();
                    attack.HandleBowShootKeyframe();
                    shot = Object.FindObjectsOfType<Arrow>().Single(x => !before.Contains(x));
                    attack.HandleBowShootKeyframe();
                    Check(Object.FindObjectsOfType<Arrow>().Count(x => !before.Contains(x)) == 1, "Bow keyframe creates exactly one visible arrow");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(shot.gameObject, scene);
                    var start = shot.transform.position;
                    typeof(Projectile).GetMethod("Step", flags).Invoke(shot, new object[] { .1f });
                    Check(Vector3.Distance(shot.transform.position, start + Vector3.forward * 4) < .01f, "Player arrow flies at configured speed");
                }
                finally { if (shot != null) Object.DestroyImmediate(shot.gameObject); Object.DestroyImmediate(config); }
            }
            finally
            {
                Time.timeScale = previousTime;
                dictionary.Clear(); foreach (var entry in savedDictionary) dictionary[entry.Key] = entry.Value;
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
                typeof(Devotion.SDK.Controllers.GameRoot).GetProperty("Instance").SetValue(null, previousRoot);
                typeof(MineArena.Controllers.LevelController).GetProperty("Current").SetValue(null, previousLevel);
            }
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static void Label(Transform parent, TMP_FontAsset font, string name, string key, Vector2 position, Vector2 size, float fontSize)
        {
            var rect = Rect(parent, name, position, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.fontSize = fontSize; text.enableAutoSizing = true;
            text.fontSizeMin = 17; text.fontSizeMax = fontSize;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            var catalog = JsonUtility.FromJson<Devotion.SDK.Services.Localization.LocalizationData>(File.ReadAllText("Assets/Resources/Localization/Russian.json"));
            text.text = catalog.items.First(x => x.key == key).value;
            var localized = rect.gameObject.AddComponent<LocalizedTextMeshPro>();
            var data = new SerializedObject(localized);
            data.FindProperty("_localizationKey").stringValue = key;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button Button(Transform parent, TMP_FontAsset font, string name, string key, Vector2 position, Vector2 size, Color color)
        {
            var rect = Rect(parent, name, position, size);
            rect.gameObject.AddComponent<Image>().color = color;
            var button = rect.gameObject.AddComponent<MineArena.UI.AnimatedButton>();
            Label(rect, font, "Label", key, Vector2.zero, size - new Vector2(24, 8), 25);
            return button;
        }
    }
}
