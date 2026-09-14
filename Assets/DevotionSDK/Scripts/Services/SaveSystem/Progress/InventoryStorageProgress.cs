using System;
using System.Collections.Generic;
using MineArena.Items;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public sealed class StoredInventoryItem
    {
        public string ItemId;
        public int Amount;
        // -1 means pristine or an item without durability.
        public int Durability = -1;
    }

    public partial class InventoryProgress
    {
        [SerializeField] private List<StoredInventoryItem> storedItems = new();
        [SerializeField] private List<StoredInventoryItem> reserveWear = new();
        public IReadOnlyList<StoredInventoryItem> StoredItems => storedItems ??= new();
        public int UsedStorageSlots
        {
            get
            {
                int count = 0;
                foreach (var entry in StoredItems)
                    if (entry != null && entry.Amount > 0) count++;
                return count;
            }
        }

        public bool IsStorageTransferProtected(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            foreach (var equipped in EquippedArmorItemIds.Values)
                if (equipped == id) return true;
            foreach (var quickSlot in QuickSlotItemIds)
                if (quickSlot == id) return true;
            return false;
        }

        private void PromoteReserveDurability(string id)
        {
            itemDurability ??= new();
            itemDurability.Remove(id);
            reserveWear ??= new();
            int index = reserveWear.FindIndex(entry => entry.ItemId == id);
            if (index < 0) return;
            if (SavedResources.TryGetValue(id, out int owned) && owned > 0)
            {
                itemDurability[id] = reserveWear[index].Durability;
                reserveWear.RemoveAt(index);
            }
            else reserveWear.RemoveAll(entry => entry.ItemId == id);
        }

        private void ClearTransferredEquipment(string id)
        {
            foreach (var slot in new List<string>(EquippedArmorItemIds.Keys))
                if (EquippedArmorItemIds[slot] == id) EquippedArmorItemIds.Remove(slot);
            EnsureQuickSlots();
            for (int i = 0; i < quickSlotItemIds.Count; i++)
                if (quickSlotItemIds[i] == id) quickSlotItemIds[i] = string.Empty;
        }

        // Mutate both containers and equipment before publishing a single save.
        public bool TryDeposit(ItemConfig config, int amount, int capacity)
        {
            if (capacity <= 0 || config == null || config.Name == "WoodenPickaxe" || IsStorageTransferProtected(config.Name) || amount <= 0 ||
                !SavedResources.TryGetValue(config.Name, out int owned) || owned < amount) return false;
            bool stackable = config is StackableItemConfig;
            if (!stackable && amount != 1) return false;
            storedItems ??= new();
            var existing = stackable ? storedItems.Find(entry => entry != null && entry.Amount > 0 && entry.ItemId == config.Name) : null;
            if (existing == null && UsedStorageSlots >= capacity) return false;
            if (existing != null && (long)existing.Amount + amount > int.MaxValue) return false;
            int durability = config is ArmorConfig armor && armor.MaxDurability > 0
                ? GetItemDurability(config.Name, armor.MaxDurability) : -1;
            if (existing != null) existing.Amount += amount;
            else storedItems.Add(new StoredInventoryItem { ItemId = config.Name, Amount = amount, Durability = durability });
            if (owned == amount)
            {
                SavedResources.Remove(config.Name);
                inventoryItemOrder?.RemoveAll(id => id == config.Name);
            }
            else SavedResources[config.Name] = owned - amount;
            if (!stackable) PromoteReserveDurability(config.Name);
            if (!stackable || owned == amount) ClearTransferredEquipment(config.Name);
            Save();
            return true;
        }

        public bool TryWithdraw(StoredInventoryItem entry, ItemConfig config, int amount)
        {
            if (entry == null || config == null || entry.ItemId != config.Name ||
                storedItems == null || !storedItems.Contains(entry) || amount <= 0 || entry.Amount < amount) return false;
            bool stackable = config is StackableItemConfig;
            if (!stackable && amount != 1) return false;
            SavedResources.TryGetValue(config.Name, out int owned);
            if (owned < 0 || (long)owned + amount > int.MaxValue) return false;
            itemDurability ??= new();
            reserveWear ??= new();
            if (owned == 0)
            {
                itemDurability.Remove(config.Name);
                reserveWear.RemoveAll(item => item.ItemId == config.Name);
            }
            if (config is ArmorConfig armor && armor.MaxDurability > 0 && entry.Durability >= 0)
            {
                int wear = Mathf.Clamp(entry.Durability, 0, armor.MaxDurability);
                if (owned == 0) itemDurability[config.Name] = wear;
                else if (wear < armor.MaxDurability)
                    reserveWear.Add(new StoredInventoryItem { ItemId = config.Name, Amount = 1, Durability = wear });
            }
            SavedResources[config.Name] = owned + amount;
            AddInventoryOrderItem(config.Name);
            entry.Amount -= amount;
            if (entry.Amount == 0) storedItems.Remove(entry);
            Save();
            return true;
        }
    }
}
