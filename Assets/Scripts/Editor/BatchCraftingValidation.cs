using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Buildings;
using MineArena.Items;
using MineArena.Managers;
using MineArena.Structs;
using MineArena.Windows.Crafting;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static partial class GameUiBuilder
    {
        [InitializeOnLoadMethod]
        private static void WatchBatchCraftingValidation() => EditorApplication.update += () =>
        {
            const string request = "Temp/batch-crafting.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(request);
            try { ValidateBatchCrafting(); }
            catch (Exception e) { File.WriteAllText("Documentation/batch-crafting-validation.txt", e.ToString()); Debug.LogException(e); }
        };

        [MenuItem("MineArena/Validation/Batch Crafting And Resource Balance")]
        public static void ValidateBatchCrafting()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || GameRoot.Instance != null)
                throw new InvalidOperationException("Run in Edit Mode without an active game.");
            RestyleCrafting("Assets/Resources/Prefabs/Windows/Crafting/CraftingWindow.prefab");
            RestyleCrafting("Assets/Prefabs/Windows/Crafting/CraftingWindow.prefab");
            var logs = new List<string>();
            void Check(bool ok, string message) { if (!ok) throw new Exception(message); logs.Add("PASS " + message); }
            var scene = EditorSceneManager.NewPreviewScene();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                var go = new GameObject("BatchValidationContext"); go.SetActive(false);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var root = go.AddComponent<GameRoot>();
                typeof(GameRoot).GetProperty("Instance").SetValue(null, root);
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
                var progress = new PlayerProgress("batch-crafting-validation");
                typeof(GameRoot).GetField("gameConfig", flags).SetValue(root, config);
                typeof(GameRoot).GetField("playerProgress", flags).SetValue(root, progress);
                var inventory = go.AddComponent<InventoryManager>();
                var buildings = go.AddComponent<BuildingManager>();
                var managers = (Dictionary<Type, BaseManager>)typeof(GameRoot).GetField("_managers", flags).GetValue(root);
                managers[typeof(InventoryManager)] = inventory; managers[typeof(BuildingManager)] = buildings;
                foreach (var b in config.BuildingsDatabase.AllBuildings)
                    if (b.name != "FarmBuilding") progress.BuildingProgress.SavedBuildings[b.BuildingName] = new BuildingSaveData(b.Levels.Count, null);
                var iron = config.ItemDatabase.GetStackableItemConfig("IronIngot");
                var ore = config.ItemDatabase.GetStackableItemConfig("IronOre");
                Check(iron.CraftCosts.Count == 1 && iron.CraftCosts[0].Resource == ore && iron.CraftCosts[0].Amount == 1 && iron.CraftAmount == 1, "Iron recipe is one ore to one ingot");
                void Reset(int amount)
                {
                    progress.InventoryProgress.PendingCraft = null;
                    progress.InventoryProgress.SavedResources.Clear();
                    progress.InventoryProgress.SavedResources[ore.Name] = amount;
                    inventory.InitManager();
                }
                Reset(15);
                var service = inventory.GetComponent<CraftProductionService>();
                Check(service.MaxBatches(iron, 1) == 15, "15 ore allows 15 batches");
                Check(service.TryStart(iron, 1), "Single craft starts");
                Check(inventory.GetItemAmount(ore.Name) == 14 && service.Job.Amount == 1, "Single craft consumes one ore");
                Check((service.Job.ReadyUtcTicks - service.Job.StartedUtcTicks) == TimeSpan.FromSeconds(7).Ticks, "Single craft takes seven seconds");
                Reset(15);
                Check(!service.TryStart(iron, 1, 16) && !service.TryStart(iron, 1, 0), "Invalid batch sizes rejected");
                Check(inventory.GetItemAmount(ore.Name) == 15 && service.Job == null, "Rejected craft changes nothing");
                Check(service.TryStart(iron, 1, 15), "Craft-all starts");
                Check(inventory.GetItemAmount(ore.Name) == 0 && service.Job.Amount == 15, "Craft-all consumes 15 ore for 15 ingots");
                Check(!service.TryStart(iron, 1), "Second order rejected while busy");
                Check((service.Job.ReadyUtcTicks - service.Job.StartedUtcTicks) == TimeSpan.FromSeconds(7).Ticks, "Craft-all also takes seven seconds");
                var saved = JsonUtility.FromJson<InventoryProgress>(JsonUtility.ToJson(progress.InventoryProgress));
                Check(saved.PendingCraft.Amount == 15 && saved.PendingCraft.ReadyUtcTicks == service.Job.ReadyUtcTicks, "Save round-trip preserves batch output and deadline");
                progress.InventoryProgress.PendingCraft = saved.PendingCraft;
                service.Job.ReadyUtcTicks = DateTime.UtcNow.Ticks - 1;
                void Poll() { typeof(CraftProductionService).GetField("_nextPoll", flags).SetValue(service, float.NegativeInfinity); typeof(CraftProductionService).GetMethod("Update", flags).Invoke(service, null); }
                Poll(); Poll();
                Check(service.Job == null && inventory.GetItemAmount(iron.Name) == 15, "Restored order awards exactly once (actual=" + inventory.GetItemAmount(iron.Name) + ", pending=" + (service.Job != null) + ")");
                var oak = config.ItemDatabase.GetStackableItemConfig("WoodOak");
                var birch = config.ItemDatabase.GetStackableItemConfig("WoodBirch");
                var repeated = new[] { new ResourceRequired(oak, 2), new ResourceRequired(birch, 2) };
                Check(CraftBatchUtility.MaxBatches(repeated, new Item[] { new StackableItem(oak, 3), new StackableItem(birch, 2) }, 1) == 1, "Shared resource categories summed before calculating maximum");
                var gold = config.ItemDatabase.GetStackableItemConfig("GoldIngot");
                Check(CraftBatchUtility.MaxBatches(gold.CraftCosts, new Item[] {
                    new StackableItem(config.ItemDatabase.GetStackableItemConfig("GoldOre"), 15),
                    new StackableItem(config.ItemDatabase.GetStackableItemConfig("CoalItem"), 4) }, 1) == 2, "Fuel is the limiting ingredient for batch smelting");
                Check(CraftBatchUtility.MaxBatches(new[] { new ResourceRequired(ore, 1) }, new Item[] { new StackableItem(ore, int.MaxValue) }, 4) == int.MaxValue / 4, "Batch output cannot overflow integer range");
                Reset(0); Check(service.MaxBatches(iron, 1) == 0, "No resources means no batches");
                Reset(15);
                progress.InventoryProgress.SavedResources[iron.Name] = int.MaxValue - 2;
                inventory.InitManager();
                Check(service.MaxBatches(iron, 1) == 2, "Existing output limits remaining integer capacity");
                Reset(15);
                progress.TutorialProgress.Initialized = true;
                progress.TutorialProgress.Step = TutorialStep.Craft;
                Check(service.MaxBatches(iron, 1) == 1, "Tutorial limits crafting to one batch");
                progress.TutorialProgress.Step = TutorialStep.Complete;
                foreach (var level in config.Levels)
                {
                    Check(level.ResourceSpawnConfigs.All(s => s.Resource != null && s.SpawnChance > 0), level.name + " has valid positive resource weights");
                    Check(Mathf.Abs(level.ResourceSpawnConfigs.Sum(s => s.SpawnChance) - 1) < 0.0001f, level.name + " weights total one");
                    foreach (var spawn in level.ResourceSpawnConfigs)
                    {
                        Check(spawn.Resource.transform.parent == null, spawn.Resource.name + " references prefab root");
                        Check(spawn.Resource.GetComponent<InteractableObject>() != null, spawn.Resource.name + " can be harvested");
                    }
                }
                var controller = go.AddComponent<MineArena.Controllers.LevelController>();
                var selection = typeof(MineArena.Controllers.LevelController).GetMethod("SelectResourceByChance", flags);
                var weights = new List<MineArena.Levels.ResourceSpawnConfig>();
                foreach (var source in config.Levels[0].ResourceSpawnConfigs.Take(3))
                {
                    var entry = new MineArena.Levels.ResourceSpawnConfig();
                    typeof(MineArena.Levels.ResourceSpawnConfig).GetField("resource", flags).SetValue(entry, source.Resource);
                    typeof(MineArena.Levels.ResourceSpawnConfig).GetField("spawnChance", flags).SetValue(entry, .8f);
                    weights.Add(entry);
                }
                var randomState = UnityEngine.Random.state;
                var samples = new int[3];
                try
                {
                    UnityEngine.Random.InitState(20260908);
                    for (int i = 0; i < 1200; i++)
                    {
                        var chosen = (MineArena.Levels.ResourceSpawnConfig)selection.Invoke(controller, new object[] { weights });
                        samples[weights.IndexOf(chosen)]++;
                    }
                }
                finally { UnityEngine.Random.state = randomState; }
                Check(samples.All(n => n > 300 && n < 500), "Weights totaling more than one are normalized; all entries spawn");
                Reset(15);
                var adapter = new ProjectCraftingAdapter(); adapter.Connect();
                var recipe = adapter.BuildCatalog().SelectMany(c => c.Recipes).Single(r => r.Item == iron);
                var smith = recipe.SourceBuilding;
                progress.BuildingProgress.SavedBuildings.Remove(smith.BuildingName);
                Check(!adapter.TryCraft(recipe, 15).Success, "Locked recipe rejects batch craft");
                progress.BuildingProgress.SavedBuildings[smith.BuildingName] = new BuildingSaveData(4, null);
                adapter.Disconnect();
                foreach (var path in new[] { "Assets/Resources/Prefabs/Windows/Crafting/CraftingWindow.prefab", "Assets/Prefabs/Windows/Crafting/CraftingWindow.prefab" })
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var so = new SerializedObject(prefab.GetComponent<CraftingWindow>());
                    Check(so.FindProperty("_batchControls").objectReferenceValue != null && so.FindProperty("_allBatchesButton").objectReferenceValue != null, path + " has wired batch controls");
                }
                var craftPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Windows/Crafting/CraftingWindow.prefab");
                void Prepare(GameObject rendered, bool all)
                {
                    var window = rendered.GetComponent<CraftingWindow>();
                    foreach (var field in new[] { "_tabsRoot", "_itemsRoot", "_costsRoot" })
                    {
                        var content = (Transform)typeof(CraftingWindow).GetField(field, flags).GetValue(window);
                        for (int i = content.childCount - 1; i >= 0; i--) Object.DestroyImmediate(content.GetChild(i).gameObject);
                    }
                    ((ProjectCraftingAdapter)typeof(CraftingWindow).GetField("_adapter", flags).GetValue(window)).Connect();
                    window.Initialize(smith);
                    typeof(CraftingWindow).GetMethod("SelectRecipe", flags).Invoke(window, new object[] { recipe });
                    if (all) ((Button)typeof(CraftingWindow).GetField("_allBatchesButton", flags).GetValue(window)).onClick.Invoke();
                    typeof(CraftingWindow).GetMethod("RefreshCraftProgress", flags).Invoke(window, null);
                    var label = (TMP_Text)typeof(CraftingWindow).GetField("_craftButtonLabel", flags).GetValue(window);
                    Check(label.text.Contains(all ? "×15" : "×1"), "UI shows expected output before craft");
                }
                Render(craftPrefab, "Documentation/UI/Crafting-Single.png", r => Prepare(r, false));
                Render(craftPrefab, "Documentation/UI/Crafting-All.png", r => Prepare(r, true));
                AssetDatabase.SaveAssets();
                File.WriteAllLines("Documentation/batch-crafting-validation.txt", logs);
                Debug.Log("[BatchCrafting] " + logs.Count + " checks passed.");
            }
            finally { typeof(GameRoot).GetProperty("Instance").SetValue(null, null); EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
