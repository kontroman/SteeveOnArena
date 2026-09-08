using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MineArena.Buildings;
using MineArena.Items;
using MineArena.Levels;
using UnityEditor;
using UnityEngine;

namespace MineArena.Editor
{
    public static class BalanceValidation
    {
        [MenuItem("MineArena/Balance/Validate Progression")]
        public static void Validate()
        {
            var results = new List<string>();
            void Check(bool valid, string message)
            {
                if (!valid) throw new InvalidOperationException(message);
                results.Add("PASS " + message);
            }
            var items = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/ScriptableObjects/Configs/Game/ItemDatabase.asset");
            var buildings = AssetDatabase.LoadAssetAtPath<BuildingsDatabase>("Assets/ScriptableObjects/Configs/Game/BuildingsDatabase.asset");
            Check(items != null && buildings != null, "Databases loaded");
            Check(items.AllItems.All(i => i != null), "No missing inventory references");
            Check(items.AllItems.Select(i => i.Name).Distinct().Count() == items.AllItems.Count, "Unique inventory IDs");
            Check(buildings.AllBuildings.Count == 5, "Five buildings registered");
            Check(buildings.AllBuildings.Select(b => b.BuildingName).Distinct().Count() == 5, "Unique building save keys");
            var craftables = items.AllItems.Where(i => i.CraftCosts != null && i.CraftCosts.Count > 0).ToArray();
            foreach (var item in craftables)
            {
                Check(item.CraftCosts.All(c => c.Resource != null && c.Amount > 0 && items.AllItems.Contains(c.Resource)), item.Name + " valid costs");
                Check(item.CraftAmount > 0, item.Name + " positive batch");
            }
            foreach (var building in buildings.AllBuildings)
            {
                int maximum = building.name == "SmithBuilding" ? 4 : 3;
                Check(building.Levels.Count == maximum, building.name + " expected levels");
                Check(!building.TryGetNextLevel(maximum, out _), building.name + " max level cannot upgrade");
                Check(building.TryGetNextLevel(1, out var next) && next.Level == 2, building.name + " upgrade target");
                foreach (var level in building.Levels)
                {
                    Check(level.ModelPrefab != null && level.Preview != null, building.name + " model and preview " + level.Level);
                    Check(level.RequiredResources.All(c => c.Resource != null && c.Amount > 0), building.name + " positive price " + level.Level);
                    Check(level.Unlocks.All(i => i != null && craftables.Contains(i)), building.name + " valid unlocks " + level.Level);
                }
            }
            var wood = items.GetStackableItemConfig("WoodOak");
            var stack = new StackableItem(wood, 150);
            Check(stack.CurrentStack == 150, "Reload preserves more than 64 resources");
            Check(stack.CanStackWith(new StackableItem(wood, 1)), "Large resource totals merge into one inventory entry");
            stack.RemoveFromStack(90);
            Check(stack.CurrentStack == 60, "Large resource costs consume the expected amount");
            var legacy = new Devotion.SDK.Services.SaveSystem.Progress.InventoryProgress();
            legacy.SavedResources["Fiber"] = 8;
            legacy.SavedResources["Rope"] = 3;
            legacy.SavedResources["String"] = 2;
            legacy.SavedResources["Resin"] = 5;
            legacy.MigrateLegacyMaterials();
            Check(legacy.SavedResources["String"] == 13 && legacy.SavedResources["CoalItem"] == 5, "Legacy material totals are preserved");
            Check(!legacy.SavedResources.ContainsKey("Fiber") && !legacy.SavedResources.ContainsKey("Rope") && !legacy.SavedResources.ContainsKey("Resin"), "Legacy inventory IDs are removed");
            legacy.MigrateLegacyMaterials();
            Check(legacy.SavedResources["String"] == 13 && legacy.SavedResources["CoalItem"] == 5, "Material migration is idempotent");
            var levels = AssetDatabase.FindAssets("t:LevelConfig").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<LevelConfig>).OrderBy(l => l.name).ToArray();
            foreach (var level in levels)
            {
                Check(level.PortalPrefab != null, level.name + " exit portal");
                Check(level.EncounterWaves.Count == 3 && level.EncounterWaves.All(w => w.MobCount > 0 && w.MobTypes.Count > 0), level.name + " three populated waves");
                Check(level.RewardResources.All(r => r.Item != null && r.Amount > 0 && items.AllItems.Contains(r.Item)), level.name + " rewards resolve");
            }
            Directory.CreateDirectory("Documentation");
            File.WriteAllLines("Documentation/balance-unity-validation.txt", results);
            Debug.Log("Balance validation: " + results.Count + " checks passed.");
        }
    }
}
