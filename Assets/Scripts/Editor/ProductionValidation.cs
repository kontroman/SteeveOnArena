using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.SaveSystem;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Items;
using MineArena.Managers;
using MineArena.PlayerSystem;
using MineArena.Structs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static class ProductionValidation
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [InitializeOnLoadMethod]
        static void Schedule() => EditorApplication.update += () =>
        {
            if (File.Exists("Temp/repair-production.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                File.Delete("Temp/repair-production.request");
                RebindPotions();
            }
            if (!File.Exists("Temp/validate-production.request")) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete("Temp/validate-production.request");
            try { Validate(); }
            catch (Exception e) { File.WriteAllText("Documentation/production-unity-validation.txt", "FAIL " + e); Debug.LogException(e); }
        };

        [MenuItem("MineArena/Balance/Rebind Potion Assets")]
        public static void RebindPotions()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string[] ids = { "HealingPotion", "RegenerationPotion", "SpeedPotion" };
            var db = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/ScriptableObjects/Configs/Game/ItemDatabase.asset");
            var serialized = new SerializedObject(db);
            var entries = serialized.FindProperty("allItems");
            foreach (string id in ids)
            {
                var potion = AssetDatabase.LoadAssetAtPath<PotionConfig>("Assets/ScriptableObjects/Configs/Drops/" + id + ".asset");
                bool found = false;
                for (int i = 0; i < entries.arraySize; i++) if (entries.GetArrayElementAtIndex(i).objectReferenceValue == potion) found = true;
                if (found) continue;
                int slot = -1;
                for (int i = 0; i < entries.arraySize; i++) if (entries.GetArrayElementAtIndex(i).objectReferenceValue == null) { slot = i; break; }
                if (slot < 0) { slot = entries.arraySize; entries.arraySize++; }
                entries.GetArrayElementAtIndex(slot).objectReferenceValue = potion;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(db);
            var lab = AssetDatabase.LoadAssetAtPath<MineArena.Buildings.BuildingConfig>("Assets/ScriptableObjects/Configs/Buildings/AlchemyBuilding.asset");
            var labData = new SerializedObject(lab);
            string[][] unlocks = { new[] { "Glass", "GlassBottle", "GlisteringMelon", "HealingPotion" }, new[] { "SpeedPotion" }, new[] { "RegenerationPotion" } };
            for (int level = 0; level < unlocks.Length; level++)
            {
                var list = labData.FindProperty("_levels").GetArrayElementAtIndex(level).FindPropertyRelative("_unlocks");
                list.arraySize = unlocks[level].Length;
                for (int i = 0; i < list.arraySize; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<ItemConfig>("Assets/ScriptableObjects/Configs/Drops/" + unlocks[level][i] + ".asset");
            }
            labData.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(lab);
            db.Initialize();
        }

        [MenuItem("MineArena/Balance/Validate Production")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || GameRoot.Instance != null || Object.FindObjectsOfType<SaveService>().Any(s => s.IsLoaded))
                throw new InvalidOperationException("Run in Edit Mode without an initialized save service.");
            var lines = new List<string>();
            void Check(bool ok, string message) { if (!ok) throw new Exception(message); lines.Add("PASS " + message); }
            var scene = EditorSceneManager.NewPreviewScene();
            var instance = typeof(GameRoot).GetProperty("Instance");
            try
            {
                foreach (string id in new[] { "HealingPotion", "RegenerationPotion", "SpeedPotion" })
                    AssetDatabase.ImportAsset("Assets/ScriptableObjects/Configs/Drops/" + id + ".asset", ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset("Assets/ScriptableObjects/Configs/Game/ItemDatabase.asset", ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset("Assets/ScriptableObjects/Configs/Buildings/AlchemyBuilding.asset", ImportAssetOptions.ForceUpdate);
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
                config.ItemDatabase.Initialize();
                foreach (string id in new[] { "WoodOak", "WoodBirch", "WoodDark", "NetheriteOre" })
                {
                    var block = AssetDatabase.LoadAssetAtPath<StackableItemConfig>("Assets/ScriptableObjects/Configs/Drops/" + id + ".asset");
                    Check(block.BlockStyleIcon && block.TopIcon != null && block.SideIcon != null && block.TopIcon != block.SideIcon, id + " separate top and side sprites imported");
                }
                foreach (string id in new[] { "Wheat", "SugarCane", "NetherWart", "Wool", "Glass" })
                {
                    var node = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Objects/Harvest/" + id + "Harvest.prefab");
                    Check(node != null && node.GetComponent<Collider>() != null && node.GetComponent<MeshFilter>().sharedMesh != null, id + " harvest mesh and collider imported");
                    Check(node.GetComponentsInChildren<Transform>(true).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), id + " harvest scripts resolve");
                }
                foreach (string id in new[] { "String", "Leather", "Wool", "Glass", "Wheat", "SugarCane", "NetherWart", "Sugar", "GlassBottle", "MelonSlice", "GlisteringMelon", "GhastTear", "HealingPotion", "RegenerationPotion", "SpeedPotion", "Stick", "NetheriteScrap", "IronIngot", "GoldIngot", "NetheriteIngot" })
                {
                    var item = config.ItemDatabase.GetItemConfig(id);
                    Check(item != null && item.Icon != null && item.Prefab != null, id + " icon and pickup imported (item=" + (item != null) + ", icon=" + (item != null && item.Icon != null) + ", pickup=" + (item != null && item.Prefab != null) + ")");
                    Check(item.Prefab.GetComponentsInChildren<Transform>(true).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), id + " no missing scripts");
                    Check(item.Prefab.GetComponent<ItemInteractor>().ItemConfig == item, id + " pickup resolves its inventory item");
                    Check(item.Prefab.GetComponentsInChildren<Renderer>(true).All(r => r.sharedMaterials.All(m => m != null && m.shader != null && !ShaderUtil.ShaderHasError(m.shader))), id + " materials and shaders compile");
                }
                var go = new GameObject("Transient production validation");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var root = go.AddComponent<GameRoot>(); instance.SetValue(null, root);
                var progress = new PlayerProgress("production-validation-transient");
                Check(JsonUtility.FromJson<InventoryProgress>(JsonUtility.ToJson(progress.InventoryProgress)).PendingCraft == null, "Empty saved job does not block future crafting");
                typeof(GameRoot).GetField("gameConfig", Private).SetValue(root, config);
                typeof(GameRoot).GetField("playerProgress", Private).SetValue(root, progress);
                var inventory = go.AddComponent<InventoryManager>();
                var buildings = go.AddComponent<BuildingManager>();
                var managers = (Dictionary<Type, BaseManager>)typeof(GameRoot).GetField("_managers", Private).GetValue(root);
                managers[typeof(InventoryManager)] = inventory; managers[typeof(BuildingManager)] = buildings;
                var potion = (PotionConfig)config.ItemDatabase.GetItemConfig("HealingPotion");
                foreach (var cost in potion.CraftCosts) progress.InventoryProgress.SavedResources[cost.Resource.Name] = cost.Amount * 2;
                inventory.InitManager();
                var service = go.GetComponent<CraftProductionService>();
                Check(service.TryStart(potion, 1), "Paid craft starts");
                Check(!service.TryStart(potion, 1) && inventory.GetItemAmount(potion.Name) == 0, "Second craft rejected; no early output");
                Check(potion.CraftCosts.All(c => inventory.GetItemAmount(c.Resource.Name) == c.Amount), "Exactly one batch of costs deducted");
                var json = JsonUtility.ToJson(progress.InventoryProgress.PendingCraft);
                var restored = JsonUtility.FromJson<CraftJob>(json);
                Check(restored.ItemId == potion.Name && restored.Amount == 1 && restored.ReadyUtcTicks == service.Job.ReadyUtcTicks, "Craft deadline survives serialization");
                Check(Mathf.Approximately(restored.Progress(restored.StartedUtcTicks), 0) && Mathf.Approximately(restored.Progress(restored.ReadyUtcTicks), 1), "Progress covers zero to one");
                void Tick() { typeof(CraftProductionService).GetField("_nextPoll", Private).SetValue(service, -1f); typeof(CraftProductionService).GetMethod("Update", Private).Invoke(service, null); }
                restored.ReadyUtcTicks = DateTime.UtcNow.AddSeconds(-1).Ticks;
                progress.InventoryProgress.PendingCraft = restored;
                Tick(); Tick();
                Check(service.Job == null && inventory.GetItemAmount(potion.Name) == 1, "Restored completed craft granted exactly once");
                var farm = config.BuildingsDatabase.AllBuildings.First(b => b.name == "FarmBuilding");
                progress.BuildingProgress.SavedBuildings[farm.BuildingName] = new BuildingSaveData(1, null);
                progress.InventoryProgress.FarmNextProductionUtcTicks = DateTime.UtcNow.AddSeconds(-1201).Ticks;
                Tick(); Tick();
                Check(inventory.GetItemAmount("Wheat") == 30 && inventory.GetItemAmount("SugarCane") == 20 && inventory.GetItemAmount("MelonSlice") == 10, "Offline farm capped at ten harvests without duplicate grant");
                var health = go.AddComponent<MineArena.Game.Health.Health>();
                typeof(MineArena.Game.Health.Health).GetField("_maxHealth", Private).SetValue(health, 100f);
                health.SetCurrentValue(100, false);
                var effects = go.AddComponent<PotionEffects>();
                Check(!effects.TryDrink(potion) && inventory.GetItemAmount(potion.Name) == 1, "Full health does not consume healing potion");
                health.SetCurrentValue(30, false);
                Check(effects.TryDrink(potion) && health.CurrentValue == 70 && inventory.GetItemAmount(potion.Name) == 0, "Healing restores forty and consumes one potion");
                Check(!effects.TryDrink(potion), "Repeated use cannot consume twice");
                var speed = (PotionConfig)config.ItemDatabase.GetItemConfig("SpeedPotion");
                inventory.AddItemById(speed.Name, 1);
                typeof(PotionEffects).GetField("_nextDrink", Private).SetValue(effects, -1f);
                Check(effects.TryDrink(speed) && Mathf.Approximately(effects.MovementMultiplier, 1.3f), "Speed potion applies thirty percent bonus");
                typeof(PotionEffects).GetField("_speedUntil", Private).SetValue(effects, -1f);
                Check(effects.MovementMultiplier == 1f, "Expired speed effect restores normal movement");
                var regen = (PotionConfig)config.ItemDatabase.GetItemConfig("RegenerationPotion");
                inventory.AddItemById(regen.Name, 1);
                typeof(PotionEffects).GetField("_nextDrink", Private).SetValue(effects, -1f);
                Check(effects.TryDrink(regen), "Regeneration potion consumes successfully");
                typeof(PotionEffects).GetField("_lastRegenTick", Private).SetValue(effects, Time.time - 10f);
                typeof(PotionEffects).GetField("_regenUntil", Private).SetValue(effects, Time.time);
                typeof(PotionEffects).GetMethod("Update", Private).Invoke(effects, null);
                Check(Mathf.Approximately(health.CurrentValue, 100f), "Ten seconds of regeneration restores thirty including the final tick");
                BalanceValidation.Validate();
                File.WriteAllLines("Documentation/production-unity-validation.txt", lines);
                Debug.Log("Production validation: " + lines.Count + " checks passed.");
            }
            finally { instance.SetValue(null, null); EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
