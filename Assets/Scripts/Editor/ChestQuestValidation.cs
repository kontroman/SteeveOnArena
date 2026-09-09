using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Achievements;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.InteractableObjects;
using MineArena.Items;
using MineArena.Structs;
using MineArena.Managers;
using MineArena.Messages.MessageService;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MineArena.Editor
{
    public static class ChestQuestValidation
    {
        [InitializeOnLoadMethod]
        static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/validate-chest-quest.request";
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(request)) return;
            File.Delete(request);
            try { Validate(); }
            catch (Exception e) { File.WriteAllText("Documentation/chest-quest-validation.txt", "FAIL " + e); Debug.LogException(e); }
        };

        [MenuItem("MineArena/Validation/Chest Quest")]
        public static void Validate()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var checks = new List<string>();
            void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks.Add("PASS " + message); }
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
            var definition = config.DataAchievements.Single(d => d.StableId == 2000);
            var target = (ChestCollectionTarget)definition.ItemTarget;
            var chests = new List<WorldChest>();
            foreach (string path in new[]{"LocationVillage", "DesertLocation", "LocationForest", "LocationMine"})
                chests.AddRange(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Arenas/" + path + ".prefab").GetComponentsInChildren<WorldChest>(true));
            Check(chests.Count == 3 && chests.Select(c => c.ChestId).Distinct().Count() == 3, "All three placed chests have unique IDs");
            Check(target.ChestIds.OrderBy(x => x).SequenceEqual(chests.Select(c => c.ChestId).OrderBy(x => x)) && definition.MaxValueOnTask == chests.Count, "Quest catalog exactly matches placed chests");
            Check(chests.All(c => c.Prize.ItemConfig != null && c.Prize.Amount > 0), "Every chest has valid loot");
            var weapon = definition.ItemPrize.ItemConfig as WeaponItemConfig;
            Check(weapon != null && weapon.AttackConfig.BaseDamage == 110 && weapon.Material != null && weapon.Icon != null && weapon.CraftCosts.Count == 0, "Exclusive sword has combat stats, material, icon and no crafting recipe");
            Check(config.ItemDatabase.GetItemConfig(weapon.Name) == weapon, "Reward registered in inventory database");
            var singleton = typeof(GameRoot).GetProperty("Instance");
            var previousRoot = GameRoot.Instance;
            var busField = typeof(MessageService).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            var previousBus = busField.GetValue(null);
            busField.SetValue(null, new MessageService());
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Chest validation"); go.SetActive(false);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var root = go.AddComponent<GameRoot>(); singleton.SetValue(null, root);
                var progress = new PlayerProgress("chest-validation-transient");
                typeof(GameRoot).GetField("gameConfig", flags).SetValue(root, config);
                typeof(GameRoot).GetField("playerProgress", flags).SetValue(root, progress);
                var inventory = go.AddComponent<InventoryManager>();
                var manager = go.AddComponent<global::Managers.AchievementManager>();
                var managers = (Dictionary<Type, BaseManager>)typeof(GameRoot).GetField("_managers", flags).GetValue(root);
                managers[typeof(InventoryManager)] = inventory;
                managers[typeof(global::Managers.AchievementManager)] = manager;
                MessageService.Subscribe(manager);
                var quest = manager.GetQuests().Single(q => q.ID == 2000);
                quest.TransferPrize();
                Check(!inventory.HasItem(weapon.Name), "Premature reward claim gives nothing");
                int index = 0;
                foreach (var source in chests)
                {
                    var chest = go.AddComponent<WorldChest>();
                    typeof(WorldChest).GetField("_chestId", flags).SetValue(chest, source.ChestId);
                    typeof(WorldChest).GetField("_prize", flags).SetValue(chest, source.Prize);
                    int before = inventory.GetItemAmount(source.Prize.Name);
                    Check(chest.TryBeginOpening() && !chest.TryBeginOpening(), "Opening is protected against reentry: " + source.ChestId);
                    chest.CancelOpening();
                    Check(chest.TryBeginOpening() && chest.TryCollect(), "Chest can retry after cancellation and grants loot: " + source.ChestId);
                    Check(inventory.GetItemAmount(source.Prize.Name) == before + Math.Max(1, source.Prize.Amount), "Loot reaches inventory: " + source.ChestId);
                    Check(!chest.TryCollect() && !chest.TryBeginOpening(), "Opened chest cannot grant loot twice: " + source.ChestId);
                    Check(quest.CurrentValueProgress == ++index && quest.CanTakePrize == (index == 3), "Only unique chests advance completion: " + index);
                }
                var equipment = go.AddComponent<MineArena.PlayerSystem.PlayerEquipment>();
                equipment.EquipSword(weapon.AttackConfig, false);
                Check(equipment.GetSwordAttackConfig() == weapon.AttackConfig, "Equipping reward uses its 110-damage attack configuration");
                var journalObject = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/WindowAchievements.prefab"), go.transform);
                var journal = journalObject.GetComponent<global::Windows.WindowAchievements>();
                journal.Bind(new[]{quest});
                Check(journalObject.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t => t.text == "3 / 3"), "Journal displays completed chest collection without target type errors");
                var serialized = JsonUtility.ToJson(progress);
                var loaded = JsonUtility.FromJson<PlayerProgress>(serialized);
                Check(target.CountFound(loaded.AchievementProgress) == 3, "Chest collection survives save serialization");
                quest.TransferPrize(); quest.TransferPrize();
                Check(quest.IsCompleted && inventory.GetItemAmount(weapon.Name) == 1, "Final sword is granted exactly once");
                Check(inventory.Items.Single(i => i != null && i.Name == weapon.Name) is EquipmentItem, "Sword is created as usable equipment");
                loaded = JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(progress));
                var restored = new Achievement(definition, 2000);
                restored.LoadData(loaded.AchievementProgress.Achievements[2000]); restored.TransferPrize();
                Check(restored.IsCompleted && inventory.GetItemAmount(weapon.Name) == 1, "Reload cannot duplicate the final reward");
                var legacy = JsonUtility.FromJson<AchievementProgress>("{}");
                Check(!legacy.HasOpenedChest("villageChest") && legacy.RegisterChest("villageChest"), "Old saves without chest data are supported");
                File.WriteAllLines("Documentation/chest-quest-validation.txt", checks);
                Debug.Log("[ChestQuest] " + checks.Count + " checks passed");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); singleton.SetValue(null, previousRoot); busField.SetValue(null, previousBus); }
        }
    }
}
