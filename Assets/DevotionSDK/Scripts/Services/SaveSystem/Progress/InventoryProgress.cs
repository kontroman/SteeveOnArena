using System;
using System.Collections.Generic;
using UnityEngine;
using static Devotion.SDK.Helpers.ContainersHelper;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class InventoryProgress : BaseProgress
    {
        private const int QuickSlotCount = 5;
        private const string StarterSwordItemId = "WoodSword";

        [SerializeField] private SerializableDictionary<string, int> savedResources = new();
        [SerializeField] private List<string> inventoryItemOrder = new();
        [SerializeField] private SerializableDictionary<string, string> equippedArmorItemIds = new();
        [SerializeField] private List<string> quickSlotItemIds = new();
        [SerializeField] private int selectedQuickSlotIndex;
        [SerializeField] private MineArena.Managers.CraftJob pendingCraft;
        [SerializeField] private long farmNextProductionUtcTicks;
        public MineArena.Managers.CraftJob PendingCraft
        {
            get => pendingCraft != null && !string.IsNullOrWhiteSpace(pendingCraft.ItemId) && pendingCraft.Amount > 0 ? pendingCraft : null;
            set => pendingCraft = value;
        }
        public long FarmNextProductionUtcTicks { get => farmNextProductionUtcTicks; set => farmNextProductionUtcTicks = value; }

        public SerializableDictionary<string, int> SavedResources
        {
            get
            {
                savedResources ??= new SerializableDictionary<string, int>();
                return savedResources;
            }
        }

        public IReadOnlyList<string> QuickSlotItemIds
        {
            get
            {
                EnsureQuickSlots();
                return quickSlotItemIds;
            }
        }

        public IReadOnlyList<string> InventoryItemOrder
        {
            get
            {
                EnsureInventoryItemOrder();
                return inventoryItemOrder;
            }
        }

        public int SelectedQuickSlotIndex
        {
            get
            {
                EnsureQuickSlots();
                return Mathf.Clamp(selectedQuickSlotIndex, 0, QuickSlotCount - 1);
            }
        }

        public InventoryProgress() { }

        // Preserve resources earned with the earlier, non-Minecraft material catalog.
        public void MigrateLegacyMaterials()
        {
            var replacements = new Dictionary<string, string>
            {
                ["Fiber"] = "String", ["Rope"] = "String", ["Fabric"] = "Leather",
                ["SteelIngot"] = "IronIngot", ["ReinforcedPlanks"] = "Planks", ["Resin"] = "CoalItem"
            };
            foreach (var entry in replacements)
            {
                if (!SavedResources.TryGetValue(entry.Key, out int amount)) continue;
                savedResources.TryGetValue(entry.Value, out int existing);
                savedResources[entry.Value] = (int)Math.Min(int.MaxValue, (long)Math.Max(0, existing) + Math.Max(0, amount));
                savedResources.Remove(entry.Key);
                inventoryItemOrder?.RemoveAll(id => id == entry.Key);
                if (quickSlotItemIds != null)
                    for (int i = 0; i < quickSlotItemIds.Count; i++)
                        if (quickSlotItemIds[i] == entry.Key) quickSlotItemIds[i] = entry.Value;
            }
            EnsureInventoryItemOrder();
        }

        public bool TrySpendExact(string itemId, int amount)
        {
            if (string.IsNullOrWhiteSpace(itemId) || amount <= 0 ||
                !SavedResources.TryGetValue(itemId, out int owned) || owned < amount) return false;
            EnsureInventoryItemOrder();
            if (owned == amount) { savedResources.Remove(itemId); inventoryItemOrder.Remove(itemId); }
            else savedResources[itemId] = owned - amount;
            Save();
            return true;
        }

        // Changes both sides before notifying the save system: no partially saved purchases.
        public bool TryExchange(string currencyId, int price, string itemId, int amount, bool unique)
        {
            if (string.IsNullOrWhiteSpace(currencyId) || string.IsNullOrWhiteSpace(itemId) ||
                currencyId == itemId || price <= 0 || amount <= 0 || (unique && amount != 1)) return false;
            SavedResources.TryGetValue(currencyId, out int funds);
            SavedResources.TryGetValue(itemId, out int owned);
            if (funds < price || (unique && owned > 0) || (long)owned + amount > int.MaxValue) return false;
            EnsureInventoryItemOrder();
            if (funds == price) { savedResources.Remove(currencyId); inventoryItemOrder.Remove(currencyId); }
            else savedResources[currencyId] = funds - price;
            savedResources[itemId] = owned + amount;
            AddInventoryOrderItem(itemId);
            Save();
            return true;
        }

        public void InitializeNewPlayerInventory()
        {
            if (!SavedResources.ContainsKey(StarterSwordItemId))
                SavedResources[StarterSwordItemId] = 1;

            AddInventoryOrderItem(StarterSwordItemId);

            EnsureQuickSlots();
            quickSlotItemIds[0] = StarterSwordItemId;
            selectedQuickSlotIndex = 0;
        }

        public void AddResource(string id, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            bool isNewItem = !SavedResources.ContainsKey(id);

            if (savedResources.TryGetValue(id, out int currentAmount))
                savedResources[id] = currentAmount + amount;
            else
                savedResources[id] = amount;

            if (isNewItem)
                AddInventoryOrderItem(id);

            Save();
        }

        public void RemoveResource(string id, int amount = 1)
        {
            if (!savedResources.TryGetValue(id, out int currentAmount))
                return;

            int newAmount = Mathf.Max(0, currentAmount - amount);

            if (newAmount <= 0)
            {
                savedResources.Remove(id);
                inventoryItemOrder?.Remove(id);
            }
            else
                savedResources[id] = newAmount;

            Debug.LogError("[TODO]: remove autosave");
            Save();
        }

        public void ClearInventory(bool clearQuickSlots = true)
        {
            SavedResources.Clear();
            inventoryItemOrder?.Clear();
            EquippedArmorItemIds.Clear();

            if (clearQuickSlots)
            {
                EnsureQuickSlots();

                for (int i = 0; i < quickSlotItemIds.Count; i++)
                    quickSlotItemIds[i] = string.Empty;
            }

            Save();
        }

        public string GetEquippedArmorItemId(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot))
                return string.Empty;

            return EquippedArmorItemIds.TryGetValue(slot, out var itemId) ? itemId : string.Empty;
        }

        public void SetEquippedArmorItemId(string slot, string itemId)
        {
            if (string.IsNullOrWhiteSpace(slot))
                return;

            itemId ??= string.Empty;

            if (string.IsNullOrWhiteSpace(itemId))
            {
                if (!EquippedArmorItemIds.Remove(slot))
                    return;
            }
            else
            {
                if (EquippedArmorItemIds.TryGetValue(slot, out var currentItemId) && currentItemId == itemId)
                    return;

                EquippedArmorItemIds[slot] = itemId;
            }

            Save();
        }

        public void SetInventoryItemOrder(IReadOnlyList<string> itemIds)
        {
            inventoryItemOrder ??= new List<string>();
            inventoryItemOrder.Clear();

            if (itemIds != null)
            {
                foreach (var itemId in itemIds)
                {
                    if (string.IsNullOrWhiteSpace(itemId))
                    {
                        inventoryItemOrder.Add(string.Empty);
                        continue;
                    }

                    if (!SavedResources.ContainsKey(itemId) || inventoryItemOrder.Contains(itemId))
                        continue;

                    inventoryItemOrder.Add(itemId);
                }
            }

            foreach (var itemId in SavedResources.Keys)
                AddInventoryOrderItem(itemId);

            Save();
        }

        public string GetQuickSlotItemId(int index)
        {
            EnsureQuickSlots();

            if (index < 0 || index >= quickSlotItemIds.Count)
                return string.Empty;

            return quickSlotItemIds[index];
        }

        public void SetQuickSlotItemId(int index, string itemId)
        {
            EnsureQuickSlots();

            if (index < 0 || index >= quickSlotItemIds.Count)
                return;

            itemId ??= string.Empty;
            if (quickSlotItemIds[index] == itemId)
                return;

            if (!string.IsNullOrWhiteSpace(itemId))
            {
                for (int i = 0; i < quickSlotItemIds.Count; i++)
                {
                    if (i != index && quickSlotItemIds[i] == itemId)
                        quickSlotItemIds[i] = string.Empty;
                }
            }

            quickSlotItemIds[index] = itemId;
            Save();
        }

        public void SetSelectedQuickSlotIndex(int index)
        {
            EnsureQuickSlots();
            index = Mathf.Clamp(index, 0, QuickSlotCount - 1);
            if (selectedQuickSlotIndex == index)
                return;

            selectedQuickSlotIndex = index;
            Save();
        }

        private void EnsureQuickSlots()
        {
            quickSlotItemIds ??= new List<string>(QuickSlotCount);

            while (quickSlotItemIds.Count < QuickSlotCount)
                quickSlotItemIds.Add(string.Empty);

            while (quickSlotItemIds.Count > QuickSlotCount)
                quickSlotItemIds.RemoveAt(quickSlotItemIds.Count - 1);

            selectedQuickSlotIndex = Mathf.Clamp(selectedQuickSlotIndex, 0, QuickSlotCount - 1);
        }

        private SerializableDictionary<string, string> EquippedArmorItemIds
        {
            get
            {
                equippedArmorItemIds ??= new SerializableDictionary<string, string>();
                return equippedArmorItemIds;
            }
        }

        private void EnsureInventoryItemOrder()
        {
            inventoryItemOrder ??= new List<string>();

            for (int i = inventoryItemOrder.Count - 1; i >= 0; i--)
            {
                if (!string.IsNullOrWhiteSpace(inventoryItemOrder[i]) && !SavedResources.ContainsKey(inventoryItemOrder[i]))
                    inventoryItemOrder.RemoveAt(i);
            }

            foreach (var itemId in SavedResources.Keys)
                AddInventoryOrderItem(itemId);
        }

        private void AddInventoryOrderItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            inventoryItemOrder ??= new List<string>();

            if (!inventoryItemOrder.Contains(itemId))
                inventoryItemOrder.Add(itemId);
        }
    }
}
