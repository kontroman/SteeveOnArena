using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Managers;
using MineArena.Structs;
using MineArena.Windows;
using MineArena.Windows.Crafting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static partial class GameUiBuilder
    {
        [InitializeOnLoadMethod]
        private static void WatchProductionUi() => EditorApplication.update += () =>
        {
            if (File.Exists("Temp/stop-preview-play.request"))
            {
                File.Delete("Temp/stop-preview-play.request");
                EditorApplication.isPlaying = false;
                return;
            }
            if (!File.Exists("Temp/production-ui.request") || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete("Temp/production-ui.request");
            try { ReviseProductionUi(); }
            catch (Exception e) { File.WriteAllText("Documentation/production-ui-validation.txt", e.ToString()); Debug.LogException(e); }
        };

        [MenuItem("MineArena/UI/Refresh Production Windows")]
        public static void ReviseProductionUi()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || GameRoot.Instance != null) throw new InvalidOperationException("Run in Edit Mode without an active game.");
            BuildPriceElement(); BuildBuilding();
            if (File.Exists("Temp/rebake-building-previews.request"))
            {
                File.Delete("Temp/rebake-building-previews.request");
                foreach (var building in AssetDatabase.FindAssets("t:BuildingConfig").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<MineArena.Buildings.BuildingConfig>))
                {
                    var data = new SerializedObject(building);
                    for (int level = 0; level < building.Levels.Count; level++)
                    {
                        string path = "Assets/Art/UI/Buildings/" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(building)) + "-" + level + ".png";
                        BakeBuilding(building.Levels[level].ModelPrefab, path, building.name == "SmithBuilding" || building.name == "StorageBuilding" ? 180f : 0f); ImportSprite(path, 1024);
                        data.FindProperty("_levels").GetArrayElementAtIndex(level).FindPropertyRelative("_preview").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    }
                    data.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(building);
                }
            }
            RestyleCrafting("Assets/Resources/Prefabs/Windows/Crafting/CraftingWindow.prefab");
            RestyleCrafting("Assets/Prefabs/Windows/Crafting/CraftingWindow.prefab");
            var scene = EditorSceneManager.NewPreviewScene();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            try
            {
                var go = new GameObject("Transient production UI preview");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var root = go.AddComponent<GameRoot>();
                typeof(GameRoot).GetProperty("Instance").SetValue(null, root);
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
                var progress = new PlayerProgress("production-ui-preview");
                typeof(GameRoot).GetField("gameConfig", flags).SetValue(root, config);
                typeof(GameRoot).GetField("playerProgress", flags).SetValue(root, progress);
                var inventory = go.AddComponent<InventoryManager>();
                var buildings = go.AddComponent<BuildingManager>();
                var managers = (Dictionary<Type, BaseManager>)typeof(GameRoot).GetField("_managers", flags).GetValue(root);
                managers[typeof(InventoryManager)] = inventory; managers[typeof(BuildingManager)] = buildings;
                foreach (var b in config.BuildingsDatabase.AllBuildings) progress.BuildingProgress.SavedBuildings[b.BuildingName] = new BuildingSaveData(2, null);
                foreach (var item in config.ItemDatabase.AllItems) if (item != null) progress.InventoryProgress.SavedResources[item.Name] = 50;
                progress.InventoryProgress.FarmNextProductionUtcTicks = DateTime.UtcNow.AddSeconds(30).Ticks;
                inventory.InitManager();
                var iconObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/ResourceIcon.prefab"), scene);
                var iconRect = (RectTransform)iconObject.transform;
                var corners = new Vector3[4];
                foreach (float size in new[] { 30f, 52f, 108f })
                foreach (var pivot in new[] { new Vector2(0.5f, 0.5f), new Vector2(0, 1) })
                {
                    iconRect.pivot = pivot; iconRect.sizeDelta = new Vector2(size, size);
                    iconObject.GetComponent<MineArena.UI.ResourceIcon>().SetResource(config.ItemDatabase.GetStackableItemConfig("WoodOak"));
                    foreach (var face in iconObject.GetComponentsInChildren<UnityEngine.UI.Image>())
                    {
                        face.rectTransform.GetWorldCorners(corners);
                        foreach (var corner in corners)
                        {
                            var point = iconRect.InverseTransformPoint(corner);
                            if (!iconRect.rect.Contains(new Vector2(point.x, point.y))) throw new Exception("Cube face extends outside its icon slot at size " + size);
                        }
                    }
                }
                Object.DestroyImmediate(iconObject);
                var adapter = new ProjectCraftingAdapter(); adapter.Connect();
                var catalog = adapter.BuildCatalog();
                var farm = config.BuildingsDatabase.AllBuildings.First(b => b.name == "FarmBuilding");
                var smith = config.BuildingsDatabase.AllBuildings.First(b => b.name == "SmithBuilding");
                if (!smith.TryGetNextLevel(3, out var fourth) || fourth.Level != 4 || smith.TryGetNextLevel(4, out _)) throw new Exception("Smith level four progression is invalid.");
                if (catalog.SelectMany(c => c.Recipes).Any(r => r.Item.Name == "WoodSword")) throw new Exception("Wood sword must not be craftable.");
                var stone = catalog.SelectMany(c => c.Recipes).Single(r => r.Item.Name == "StoneSword");
                if (stone.SourceBuilding != smith || stone.RequiredBuildingLevel != 1) throw new Exception("Stone sword must unlock in smith level one.");
                if (!farm.Levels.Select(l => l.ProductionSeconds).SequenceEqual(new[] { 120f, 90f, 60f })) throw new Exception("Farm intervals must be doubled.");
                var farming = catalog.First(c => c.Recipes.Any(r => r.SourceBuilding == farm));
                if (farming.Recipes.Count != 4 || farming.Recipes.Any(r => !r.IsProduction || adapter.CanCraft(r))) throw new Exception("Farm must expose four automatic crops without manual crafting.");
                Directory.CreateDirectory("Documentation/UI");
                var window = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Windows/Crafting/CraftingWindow.prefab"), scene);
                var craft = window.GetComponent<CraftingWindow>(); craft.Initialize(farm);
                typeof(CraftingWindow).GetField("_pendingInitialBuilding", flags).SetValue(craft, farm);
                void PrepareCraft(GameObject rendered, MineArena.Buildings.BuildingConfig building)
                {
                    var view = rendered.GetComponent<CraftingWindow>();
                    foreach (string field in new[] { "_tabsRoot", "_itemsRoot", "_costsRoot" })
                    {
                        var content = (Transform)typeof(CraftingWindow).GetField(field, flags).GetValue(view);
                        for (int i = content.childCount - 1; i >= 0; i--) Object.DestroyImmediate(content.GetChild(i).gameObject);
                    }
                    ((ProjectCraftingAdapter)typeof(CraftingWindow).GetField("_adapter", flags).GetValue(view)).Connect();
                    view.Initialize(building);
                    typeof(CraftingWindow).GetMethod("RefreshCraftProgress", flags).Invoke(view, null);
                    Canvas.ForceUpdateCanvases();
                    foreach (var rect in rendered.GetComponentsInChildren<RectTransform>()) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    Canvas.ForceUpdateCanvases();
                    var costs = (Transform)typeof(CraftingWindow).GetField("_costsRoot", flags).GetValue(view);
                    foreach (Transform row in costs)
                    {
                        if (!row.name.StartsWith("Cost_")) continue;
                        if (((RectTransform)row).rect.width < 300) throw new Exception("Cost row failed to fill the available panel width.");
                        var label = (RectTransform)row.Find("Name");
                        var amount = (RectTransform)row.Find("Amount");
                        var icon = (RectTransform)row.Find("IconSlot");
                        Vector3[] labelCorners = new Vector3[4], amountCorners = new Vector3[4], iconCorners = new Vector3[4];
                        label.GetWorldCorners(labelCorners); amount.GetWorldCorners(amountCorners); icon.GetWorldCorners(iconCorners);
                        if (labelCorners[0].x < iconCorners[2].x || amountCorners[0].x < labelCorners[2].x) throw new Exception("Cost icon, label and amount overlap.");
                    }
                }
                Render(window, "Documentation/UI/Production-Farm.png", rendered => PrepareCraft(rendered, farm));
                Object.DestroyImmediate(window);
                window = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/Windows/Crafting/CraftingWindow.prefab"), scene);
                craft = window.GetComponent<CraftingWindow>();
                craft.Initialize(config.BuildingsDatabase.AllBuildings.First(b => b.name == "LumberjackBuilding"));
                typeof(CraftingWindow).GetField("_pendingInitialBuilding", flags).SetValue(craft, config.BuildingsDatabase.AllBuildings.First(b => b.name == "LumberjackBuilding"));
                Render(window, "Documentation/UI/Production-Workshop.png", rendered => PrepareCraft(rendered, config.BuildingsDatabase.AllBuildings.First(b => b.name == "LumberjackBuilding")));
                Object.DestroyImmediate(window);
                window = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "BuildingWindow.prefab"), scene);
                window.GetComponent<BuildingWindow>().InitializeBuilding(farm, null);
                Render(window, "Documentation/UI/Production-Building.png");
                Object.DestroyImmediate(window);
                progress.BuildingProgress.SavedBuildings[smith.BuildingName] = new BuildingSaveData(3, null);
                window = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "BuildingWindow.prefab"), scene);
                window.GetComponent<BuildingWindow>().InitializeBuilding(smith, null);
                Render(window, "Documentation/UI/Production-Smith-Level4.png");
                adapter.Disconnect();
                File.WriteAllText("Documentation/production-ui-validation.txt", "PASS Cube faces fit 30, 52 and 108 pixel slots with center and top-left pivots\nPASS Farm exposes four automatic crops\nPASS Cost rows fill panel width without overlapping icon, name and amount\nPASS Compact crafting and building prefabs rebuilt\nPASS Four window previews rendered\nPASS Wood sword removed; stone sword belongs to smith level one\nPASS Smith upgrades from three to four and stops at four\nPASS Farm production intervals are 120, 90 and 60 seconds\n");
            }
            finally { typeof(GameRoot).GetProperty("Instance").SetValue(null, null); EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
