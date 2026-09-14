using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Items;
using UnityEditor;
using UnityEngine;

namespace MineArena.Editor
{
    public static class StorageValidation
    {
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            if (File.Exists("Temp/storage-layout.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete("Temp/storage-layout.request");
                try { UpgradePrefab(); }
                catch (Exception e) { File.WriteAllText("Temp/storage-layout-result.txt", e.ToString()); Debug.LogException(e); }
            }
            const string request = "Temp/storage-validation.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Validate(); }
            catch (Exception e) { File.WriteAllText("Temp/storage-validation-result.txt", e.ToString()); Debug.LogException(e); }
        };

        [MenuItem("MineArena/UI/Update Storage Capacity Layout")]
        public static void UpgradePrefab()
        {
            const string path = "Assets/DevotionSDK/Prefabs/UI/StorageWindow.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var window = root.GetComponent<MineArena.Windows.StorageWindow>();
                var state = new SerializedObject(window);
                state.FindProperty("_storageBuilding").objectReferenceValue = AssetDatabase.LoadAssetAtPath<MineArena.Buildings.BuildingConfig>("Assets/ScriptableObjects/Configs/Buildings/StorageBuilding.asset");
                state.ApplyModifiedPropertiesWithoutUndo();
                window.SetStorageContents(null);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            File.WriteAllText("Temp/storage-layout-result.txt", "PASS storage prefab expanded with locked slots");
        }

        [MenuItem("MineArena/Validation/Storage")]
        public static void Validate()
        {
            var report = new List<string>();
            void Check(bool condition, string text)
            {
                if (!condition) throw new Exception(text);
                report.Add("PASS " + text);
            }
            var database = AssetDatabase.LoadAssetAtPath<MineArena.Structs.GameConfig>("Assets/ScriptableObjects/GameConfig.asset").ItemDatabase;
            var resource = database.GetItemConfig("IronIngot");
            var shield = database.GetItemConfig("Shield") as ArmorConfig;
            if (shield == null)
                shield = AssetDatabase.FindAssets("t:ArmorConfig").Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<ArmorConfig>).First(a => a.MaxDurability > 0);
            Check(resource != null && shield != null, "Real resource and durable equipment configs resolve");
            var player = new PlayerProgress("storage-test");
            var progress = player.InventoryProgress;
            progress.AddResource(resource.Name, 50);
            progress.SetQuickSlotItemId(1, resource.Name);
            Check(!progress.TryDeposit(resource, 20, 30) && progress.SavedResources[resource.Name] == 50 && progress.StoredItems.Count == 0,
                "Quick-slot items cannot be deposited, including partial stacks");
            progress.SetQuickSlotItemId(1, "");
            for (int slot = 0; slot < 5; slot++)
            {
                progress.SetQuickSlotItemId(slot, resource.Name);
                Check(progress.IsStorageTransferProtected(resource.Name) && !progress.TryDeposit(resource, 1, 30),
                    "Quick slot " + slot + " protects its item regardless of selected slot");
                progress.SetQuickSlotItemId(slot, "");
            }
            foreach (var slot in new[] { "Helmet", "Chest", "Leggings", "Boots", "OffHand" })
            {
                progress.SetEquippedArmorItemId(slot, shield.Name);
                Check(progress.IsStorageTransferProtected(shield.Name), "Equipped slot " + slot + " protects its item");
                progress.SetEquippedArmorItemId(slot, "");
            }
            Check(progress.TryDeposit(resource, 20, 30) && progress.SavedResources[resource.Name] == 30 && progress.StoredItems[0].Amount == 20,
                "Partial deposit conserves quantities");
            Check(progress.TryDeposit(resource, 30, 30) && !progress.SavedResources.ContainsKey(resource.Name) && progress.StoredItems.Count == 1 && progress.StoredItems[0].Amount == 50,
                "Full deposit merges storage stack and removes carried resource");
            Check(progress.GetQuickSlotItemId(1) == "", "Empty carried stack clears quick slot");
            Check(!progress.TryDeposit(resource, 1, 30) && !progress.TryDeposit(resource, -1, 30), "Missing or invalid quantities cannot duplicate items");
            var entry = progress.StoredItems[0];
            Check(progress.TryWithdraw(entry, resource, 1) && entry.Amount == 49 && progress.SavedResources[resource.Name] == 1, "Withdraw one item");
            Check(!progress.TryWithdraw(new StoredInventoryItem { ItemId = resource.Name, Amount = 100 }, resource, 1), "Forged or stale storage entry is rejected");
            Check(!progress.TryWithdraw(entry, shield, 1), "Mismatched config is rejected");
            Check(progress.TryWithdraw(entry, resource, 49) && progress.StoredItems.Count == 0 && progress.SavedResources[resource.Name] == 50,
                "Withdraw whole stack restores original total");
            Check(!progress.TryWithdraw(entry, resource, 1), "Repeated withdrawal cannot duplicate the removed stack");
            progress.TryDeposit(resource, 1, 30);
            progress.SavedResources[resource.Name] = int.MaxValue;
            entry = progress.StoredItems[0];
            Check(!progress.TryWithdraw(entry, resource, 1) && entry.Amount == 1, "Overflow leaves both containers unchanged");
            progress.SavedResources[resource.Name] = 1;
            entry.Amount = int.MaxValue;
            Check(!progress.TryDeposit(resource, 1, 30) && progress.SavedResources[resource.Name] == 1, "Deposit overflow is atomic");
            entry.Amount = 1;

            progress.AddResource(shield.Name, 2);
            progress.DamageDurableItem(shield.Name, 21, shield.MaxDurability);
            progress.SetEquippedArmorItemId(shield.Slot.ToString(), shield.Name);
            Check(!progress.TryDeposit(shield, 1, 30) && progress.SavedResources[shield.Name] == 2 &&
                progress.GetItemDurability(shield.Name, shield.MaxDurability) == shield.MaxDurability - 21,
                "Equipped shield cannot be deposited even with a reserve; wear remains unchanged");
            progress.SetEquippedArmorItemId(shield.Slot.ToString(), "");
            progress.SetQuickSlotItemId(2, shield.Name);
            Check(!progress.TryDeposit(shield, 1, 30), "Unequipped quick-slot equipment is still protected");
            progress.SetQuickSlotItemId(2, "");
            Check(progress.TryDeposit(shield, 1, 30), "Unequipped item can be deposited after removing quick-slot assignment");
            var firstShield = progress.StoredItems.Last();
            Check(firstShield.Durability == shield.MaxDurability - 21 && progress.GetItemDurability(shield.Name, shield.MaxDurability) == shield.MaxDurability,
                "Deposit preserves worn copy and exposes pristine reserve");
            Check(progress.GetEquippedArmorItemId(shield.Slot.ToString()) == "" && progress.GetQuickSlotItemId(2) == "", "Depositing equipment clears armor and quick slot even with a reserve");
            progress.DamageDurableItem(shield.Name, 42, shield.MaxDurability);
            Check(progress.TryWithdraw(firstShield, shield, 1), "Two independently worn copies can coexist in the inventory");
            player = JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(player));
            progress = player.InventoryProgress;
            Check(progress.GetItemDurability(shield.Name, shield.MaxDurability) == shield.MaxDurability - 42 && progress.StoredItems[0].Amount == 1,
                "Player save round trip preserves current wear and storage");
            Check(progress.TryDeposit(shield, 1, 30) && progress.StoredItems.Last().Durability == shield.MaxDurability - 42 &&
                progress.GetItemDurability(shield.Name, shield.MaxDurability) == shield.MaxDurability - 21,
                "Save round trip preserves reserve wear and deposit promotes it");
            Check(progress.TryDeposit(shield, 1, 30) && !progress.SavedResources.ContainsKey(shield.Name), "Each equipment copy gets its own storage entry");
            var storedShields = progress.StoredItems.Where(item => item.ItemId == shield.Name).ToArray();
            Check(storedShields.Length == 2, "Two worn shields remain separate");
            progress.TryWithdraw(storedShields[0], shield, 1);
            progress.TryWithdraw(storedShields[1], shield, 1);
            Check(progress.DamageDurableItem(shield.Name, shield.MaxDurability, shield.MaxDurability) &&
                progress.GetItemDurability(shield.Name, shield.MaxDurability) == shield.MaxDurability - 21,
                "Breaking the active copy retains reserve wear");
            progress.TryDeposit(shield, 1, 30);
            progress.ClearInventory();
            Check(progress.StoredItems.Count == 2 && progress.StoredItems.Last().Durability == shield.MaxDurability - 21,
                "Clearing carried inventory does not erase storage");
            var legacy = JsonUtility.FromJson<InventoryProgress>("{}");
            Check(legacy.StoredItems.Count == 0, "Old saves initialize an empty storage");
            var pickaxe = database.GetItemConfig("WoodenPickaxe");
            progress.AddResource("WoodenPickaxe", 1);
            Check(!progress.TryDeposit(pickaxe, 1, 30) && progress.SavedResources["WoodenPickaxe"] == 1, "Starter pickaxe cannot be stranded in storage");
            var limited = new InventoryProgress();
            limited.AddResource(resource.Name, 5);
            limited.AddResource(shield.Name, 60);
            Check(!limited.TryDeposit(resource, 1, 0), "Unbuilt storage rejects deposits");
            Check(limited.TryDeposit(resource, 1, 18), "First stack occupies one slot");
            for (int i = 0; i < 17; i++) Check(limited.TryDeposit(shield, 1, 18), "Level-one equipment slot " + (i + 2));
            var before = JsonUtility.ToJson(limited);
            Check(!limited.TryDeposit(shield, 1, 18) && before == JsonUtility.ToJson(limited), "Full storage rejects new item atomically");
            Check(limited.TryDeposit(resource, 4, 18) && limited.UsedStorageSlots == 18, "Existing stack can grow in full storage");
            Check(limited.TryDeposit(shield, 1, 24) && limited.UsedStorageSlots == 19, "Upgrade immediately permits a new slot");
            var restoredLimit = JsonUtility.FromJson<InventoryProgress>(JsonUtility.ToJson(limited));
            Check(restoredLimit.UsedStorageSlots == 19 && !restoredLimit.TryDeposit(shield, 1, 18), "Old over-capacity saves retain all items and reject new slots");
            Check(restoredLimit.TryWithdraw(restoredLimit.StoredItems.Last(), shield, 1), "Over-capacity items can always be withdrawn");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/UI/StorageWindow.prefab");
            var window = prefab.GetComponent<MineArena.Windows.StorageWindow>();
            var serialized = new SerializedObject(window);
            foreach (var field in new[] { "_inventoryContent", "_storageContent", "_cellTemplate", "_inventoryCount", "_storageCount", "_storageEmpty", "_storageBuilding" })
                Check(serialized.FindProperty(field).objectReferenceValue != null, "Storage prefab binding " + field);
            ValidateWindow(prefab, resource, shield, Check);
            File.WriteAllLines("Temp/storage-validation-result.txt", report);
            Debug.Log("Storage validation passed: " + report.Count);
        }

        private static void ValidateWindow(GameObject prefab, ItemConfig resource, ArmorConfig shield, Action<bool, string> check)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var singleton = typeof(Devotion.SDK.Controllers.GameRoot).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            if (singleton.GetValue(null) != null) throw new Exception("Run storage validation outside Play Mode");
            void Set(object target, string field, object value) => target.GetType().GetField(field, flags).SetValue(target, value);
            void Call(object target, string method) => target.GetType().GetMethod(method, flags).Invoke(target, null);
            try
            {
                GameUiBuilder.Render(prefab, "Temp/storage-functional-preview.png", root =>
                {
                    var fixture = new GameObject("Storage test services");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fixture, root.scene);
                    var gameRoot = fixture.AddComponent<Devotion.SDK.Controllers.GameRoot>();
                    var player = new PlayerProgress("storage-ui-test");
                    var progress = player.InventoryProgress;
                    var inventory = fixture.AddComponent<MineArena.Managers.InventoryManager>();
                    var buildings = fixture.AddComponent<MineArena.Managers.BuildingManager>();
                    var config = AssetDatabase.LoadAssetAtPath<MineArena.Buildings.BuildingConfig>("Assets/ScriptableObjects/Configs/Buildings/StorageBuilding.asset");
                    Set(gameRoot, "playerProgress", player);
                    Set(gameRoot, "gameConfig", AssetDatabase.LoadAssetAtPath<MineArena.Structs.GameConfig>("Assets/ScriptableObjects/GameConfig.asset"));
                    Set(gameRoot, "_managers", new Dictionary<Type, Devotion.SDK.Managers.BaseManager>
                    {
                        [typeof(MineArena.Managers.InventoryManager)] = inventory,
                        [typeof(MineArena.Managers.BuildingManager)] = buildings
                    });
                    singleton.SetValue(null, gameRoot);
                    player.BuildingProgress.SavedBuildings[config.BuildingName] = new MineArena.Structs.BuildingSaveData(1, fixture.transform);
                    progress.AddResource(resource.Name, 25);
                    progress.AddResource(shield.Name);
                    progress.DamageDurableItem(shield.Name, 50, shield.MaxDurability);
                    inventory.InitManager();
                    var window = root.GetComponent<MineArena.Windows.StorageWindow>();
                    Set(window, "_inventory", inventory); Set(window, "_building", config); Set(window, "_buildingPlace", fixture.transform);
                    inventory.InventoryUpdated += (Action)Delegate.CreateDelegate(typeof(Action), window, "RefreshAll");
                    Call(window, "ConfigureControls"); Call(window, "RefreshAll");
                    var state = new SerializedObject(window);
                    var left = (Transform)state.FindProperty("_inventoryContent").objectReferenceValue;
                    var right = (Transform)state.FindProperty("_storageContent").objectReferenceValue;
                    MineArena.UI.InventoryCellUI Find(Transform side, string id) => side.GetComponentsInChildren<MineArena.UI.InventoryCellUI>().First(c => c.Item?.Name == id);
                    var source = Find(left, resource.Name);
                    progress.SetQuickSlotItemId(4, resource.Name);
                    check(!window.CanStartTransfer(source) && !window.Transfer(source, true),
                        "Protected item cannot start a drag or transfer by click");
                    window.TryDrop(source, source.Item, new List<UnityEngine.EventSystems.RaycastResult> { new() { gameObject = right.gameObject } });
                    check(progress.SavedResources[resource.Name] == 25 && progress.StoredItems.Count == 0,
                        "Drop rechecks protection and cannot bypass it");
                    progress.SetQuickSlotItemId(4, "");
                    check(window.CanStartTransfer(source), "Removing quick-slot assignment enables dragging immediately");
                    progress.SetQuickSlotItemId(4, resource.Name);
                    progress.SetEquippedArmorItemId(shield.Slot.ToString(), shield.Name);
                    Call(window, "RefreshAll");
                    check(!left.GetComponentsInChildren<MineArena.UI.InventoryCellUI>().Any(c => c.HasItem), "Equipped and quick-slot items are omitted from transfer panel");
                    progress.SetQuickSlotItemId(4, "");
                    progress.SetEquippedArmorItemId(shield.Slot.ToString(), "");
                    Call(window, "RefreshAll");
                    source = Find(left, resource.Name);
                    check(Find(left, shield.Name) != null, "Unequipped items return to transfer panel");
                    check(window.Capacity == 18 && right.GetComponentsInChildren<MineArena.UI.InventoryCellUI>().Length == 30 &&
                        right.GetComponentsInChildren<MineArena.UI.StorageLockedSlotGraphic>().Length == 12,
                        "Level one shows 18 open slots and 12 locked slots");
                    var lockedCell = right.GetComponentsInChildren<MineArena.UI.StorageLockedSlotGraphic>().First();
                    window.TryDrop(source, source.Item, new List<UnityEngine.EventSystems.RaycastResult> { new() { gameObject = lockedCell.gameObject } });
                    check(progress.StoredItems.Count == 0, "Dropping on locked slot cannot transfer even with free slots");
                    check(source.GetComponent<MineArena.UI.StorageCellInteraction>() != null && source.GetComponent<MineArena.UI.InventoryCellDragHandler>() != null,
                        "Visible cells have click and drag controls");
                    check(window.Transfer(source, true) && progress.SavedResources[resource.Name] == 24 && Find(right, resource.Name) != null,
                        "Window transfer refreshes both panels through InventoryUpdated");
                    source = Find(left, resource.Name);
                    window.TryDrop(source, source.Item, new List<UnityEngine.EventSystems.RaycastResult> { new() { gameObject = right.gameObject } });
                    check(!progress.SavedResources.ContainsKey(resource.Name) && progress.StoredItems.First().Amount == 25, "Drag across panels deposits whole stack");
                    source = Find(right, resource.Name);
                    window.TryDrop(source, source.Item, new List<UnityEngine.EventSystems.RaycastResult> { new() { gameObject = right.gameObject } });
                    check(!progress.SavedResources.ContainsKey(resource.Name), "Drag within same panel does not transfer");
                    window.TryDrop(source, source.Item, new List<UnityEngine.EventSystems.RaycastResult> { new() { gameObject = left.parent.gameObject } });
                    check(progress.SavedResources[resource.Name] == 25, "Drag onto empty opposite viewport withdraws items");
                    window.Transfer(Find(left, shield.Name));
                    var storedShield = Find(right, shield.Name);
                    var bar = storedShield.GetComponentInChildren<MineArena.UI.ItemDurabilityBar>();
                    check(bar != null && (int)bar.GetType().GetField("_current", flags).GetValue(bar) == shield.MaxDurability - 50,
                        "Storage durability bar reads stored copy rather than inventory");
                    var upgrade = root.transform.GetComponentsInChildren<UnityEngine.UI.Button>(true).First(b => b.name == "UpgradeStorage");
                    check(upgrade.gameObject.activeSelf, "Built storage retains upgrade button");
                    check(config.Levels.All(level => level.ExpeditionRewardBonusPercent == 0) && buildings.ExpeditionRewardBonusPercent == 0,
                        "Storage no longer grants expedition rewards");
                    player.BuildingProgress.SavedBuildings[config.BuildingName].Level = 2;
                    Call(window, "RefreshAll");
                    check(window.Capacity == 24 && right.GetComponentsInChildren<MineArena.UI.StorageLockedSlotGraphic>().Length == 6,
                        "Level two opens six more slots");
                    player.BuildingProgress.SavedBuildings[config.BuildingName].Level = config.Levels.Max(l => l.Level);
                    Call(window, "RefreshUpgrade");
                    Call(window, "RefreshAll");
                    check(window.Capacity == 30 && right.GetComponentsInChildren<MineArena.UI.StorageLockedSlotGraphic>().Length == 0,
                        "Level three unlocks all 30 slots");
                    check(!upgrade.gameObject.activeSelf, "Maximum level hides upgrade button");
                    player.BuildingProgress.SavedBuildings[config.BuildingName].Level = 0;
                    check(!window.Transfer(storedShield), "Unbuilt storage cannot transfer items");
                    player.BuildingProgress.SavedBuildings[config.BuildingName].Level = 1;
                    Call(window, "RefreshUpgrade");
                    window.Transfer(Find(left, resource.Name), true);
                });
            }
            finally { singleton.SetValue(null, null); }
        }
    }
}
