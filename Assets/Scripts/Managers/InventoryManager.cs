using MineArena.Buildings;
using MineArena.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using Devotion.SDK.Managers;
using Devotion.SDK.Controllers;
using UnityEngine;

namespace MineArena.Managers
{
    public class InventoryManager : BaseManager
    {
        private readonly List<Item> _items = new();

        public event Action InventoryUpdated;

        private InventoryManager() { }

        public IReadOnlyList<Item> Items => _items;

        public override void InitManager()
        {
            _items.Clear();

            var progress = GameRoot.PlayerProgress.InventoryProgress;
            var savedResources = progress.SavedResources;

            foreach (var itemId in progress.InventoryItemOrder)
            {
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    _items.Add(null);
                    continue;
                }

                AddSavedItem(itemId, savedResources);
            }

            InventoryUpdated?.Invoke();
        }

        public void AddItem(Item item, int amount = 1)
        {
            if (item is StackableItem stackableItem)
            {
                var existingItem = _items
                    .OfType<StackableItem>()
                    .FirstOrDefault(i => i.CanStackWith(stackableItem));

                if (existingItem != null)
                {
                    existingItem.AddToStack(amount);

                    GameRoot.PlayerProgress.InventoryProgress.AddResource(item.Name, amount);

                    InventoryUpdated?.Invoke();

                    Debug.Log($"Item added to inventory: {item.Name}");

                    return;
                }
            }

            var emptySlotIndex = _items.IndexOf(null);
            if (emptySlotIndex >= 0)
                _items[emptySlotIndex] = item;
            else
                _items.Add(item);

            GameRoot.PlayerProgress.InventoryProgress.AddResource(item.Name, amount);
            SaveInventoryOrder();

            Debug.Log($"Item added to inventory: {item.Name}");

            InventoryUpdated?.Invoke();
        }

        public bool HasItem(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            if (_items.Any(item => item != null && item.Name == itemId))
                return true;

            return GameRoot.PlayerProgress.InventoryProgress.SavedResources.ContainsKey(itemId);
        }

        public void AddItemById(string itemId, int amount = 1)
        {
            if (string.IsNullOrWhiteSpace(itemId))
                return;

            amount = Mathf.Max(1, amount);
            var config = GameRoot.GameConfig.ItemDatabase.GetItemConfig(itemId);

            if (config == null)
            {
                AddItem(new Item(itemId, null, null), amount);
                return;
            }

            AddItem(CreateItemFromConfig(config, amount), amount);
        }

        public void ClearInventory(bool clearQuickSlots = true)
        {
            _items.Clear();
            GameRoot.PlayerProgress?.InventoryProgress?.ClearInventory(clearQuickSlots);
            InventoryUpdated?.Invoke();
        }

        public void RemoveItem(Item item, int amount = 1)
        {
            if (item == null)
                return;

            if (item is StackableItem stackable)
            {
                if (amount <= 0)
                    amount = 1;

                var toRemove = Mathf.Min(amount, stackable.CurrentStack);

                if (toRemove <= 0)
                    return;

                stackable.RemoveFromStack(toRemove);
                GameRoot.PlayerProgress.InventoryProgress.RemoveResource(stackable.Name, toRemove);

                if (stackable.CurrentStack <= 0)
                {
                    ClearItemSlot(stackable);
                    SaveInventoryOrder();
                    Debug.Log($"Item removed from inventory: {item.Name}");
                }
                else
                {
                    Debug.Log($"Reduced stack for item: {item.Name} by {toRemove}");
                }

                InventoryUpdated?.Invoke();
                return;
            }

            int itemIndex = _items.IndexOf(item);
            if (itemIndex >= 0)
            {
                _items[itemIndex] = null;
                TrimTrailingEmptySlots();
                GameRoot.PlayerProgress.InventoryProgress.RemoveResource(item.Name, amount);
                SaveInventoryOrder();
                Debug.Log($"Item removed from inventory: {item.Name}");
                InventoryUpdated?.Invoke();
            }
        }

        public void MoveItem(Item itemToMove, Item targetItem)
        {
            if (itemToMove == null || targetItem == null || itemToMove == targetItem)
                return;

            int fromIndex = _items.IndexOf(itemToMove);
            int toIndex = _items.IndexOf(targetItem);

            if (fromIndex < 0 || toIndex < 0 || fromIndex == toIndex)
                return;

            _items[fromIndex] = targetItem;
            _items[toIndex] = itemToMove;
            SaveInventoryOrder();
            InventoryUpdated?.Invoke();
        }

        public void MoveItemToEnd(Item itemToMove)
        {
            MoveItemToIndex(itemToMove, _items.Count - 1);
        }

        public void MoveItemToIndex(Item itemToMove, int toIndex)
        {
            if (itemToMove == null || _items.Count == 0)
                return;

            int fromIndex = _items.IndexOf(itemToMove);
            toIndex = Mathf.Clamp(toIndex, 0, _items.Count - 1);

            if (fromIndex < 0 || fromIndex == toIndex)
                return;

            _items.RemoveAt(fromIndex);
            _items.Insert(toIndex, itemToMove);
            SaveInventoryOrder();
            InventoryUpdated?.Invoke();
        }

        public void MoveItemToSlot(Item itemToMove, int toIndex)
        {
            if (itemToMove == null || toIndex < 0)
                return;

            int fromIndex = _items.IndexOf(itemToMove);
            if (fromIndex < 0)
                fromIndex = FindItemIndexByName(itemToMove.Name);

            if (fromIndex < 0)
                return;

            while (_items.Count <= toIndex)
                _items.Add(null);

            if (fromIndex == toIndex)
                return;

            var targetItem = _items[toIndex];
            _items[toIndex] = _items[fromIndex];
            _items[fromIndex] = targetItem;

            TrimTrailingEmptySlots();
            SaveInventoryOrder();
            InventoryUpdated?.Invoke();
        }

        public bool HasResources(IReadOnlyList<ResourceRequired> requiredResources)
        {
            if (requiredResources == null || requiredResources.Count == 0)
                return true;

            foreach (var requirement in requiredResources)
            {
                if (requirement.Amount <= 0)
                    continue;

                var resourceConfig = requirement.Resource;
                if (resourceConfig == null)
                    continue;

                var available = GetTotalForCategory(resourceConfig.ResourceCategory);
                if (available < requirement.Amount)
                    return false;
            }

            return true;
        }

        public bool TryConsumeResources(IReadOnlyList<ResourceRequired> requiredResources)
        {
            if (requiredResources == null || requiredResources.Count == 0)
                return true;

            var pendingRemoval = new Dictionary<StackableItem, int>();

            foreach (var requirement in requiredResources)
            {
                if (requirement.Amount <= 0)
                    continue;

                var resourceConfig = requirement.Resource;
                if (resourceConfig == null)
                    continue;

                var category = resourceConfig.ResourceCategory;
                if (string.IsNullOrWhiteSpace(category))
                    return false;

                var amountLeft = requirement.Amount;

                foreach (var stackable in _items.OfType<StackableItem>())
                {
                    if (!CategoryEquals(stackable.ResourceCategory, category))
                        continue;

                    var alreadyQueued = pendingRemoval.TryGetValue(stackable, out var queuedAmount) ? queuedAmount : 0;
                    var available = stackable.CurrentStack - alreadyQueued;

                    if (available <= 0)
                        continue;

                    var toTake = Mathf.Min(available, amountLeft);

                    if (toTake <= 0)
                        continue;

                    pendingRemoval[stackable] = alreadyQueued + toTake;
                    amountLeft -= toTake;

                    if (amountLeft <= 0)
                        break;
                }

                if (amountLeft > 0)
                    return false;
            }

            if (pendingRemoval.Count == 0)
                return true;

            foreach (var entry in pendingRemoval)
            {
                var stackable = entry.Key;
                var amount = entry.Value;

                if (amount <= 0)
                    continue;

                stackable.RemoveFromStack(amount);
                GameRoot.PlayerProgress.InventoryProgress.RemoveResource(stackable.Name, amount);

                if (stackable.CurrentStack <= 0)
                    ClearItemSlot(stackable);
            }

            SaveInventoryOrder();
            InventoryUpdated?.Invoke();
            return true;
        }

        public bool CanAfford(IReadOnlyList<ResourceRequired> requiredResources)
        {
            return HasResources(requiredResources);
        }

        private int GetTotalForCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return 0;

            var total = 0;

            foreach (var stackable in _items.OfType<StackableItem>())
            {
                if (CategoryEquals(stackable.ResourceCategory, category))
                    total += stackable.CurrentStack;
            }

            return total;
        }

        private static bool CategoryEquals(string left, string right)
        {
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static Item CreateItemFromConfig(ItemConfig config, int amount = 1)
        {
            if (config == null)
                return null;

            if (config.Stackable && config is StackableItemConfig stackableConfig)
                return new StackableItem(stackableConfig, amount);

            if (config is PickaxeConfig pickaxeConfig)
                return new Pickaxe(pickaxeConfig);

            if (config is ArmorConfig armorConfig)
                return new Armor(armorConfig);

            if (config is EquipmentItemConfig equipmentConfig)
                return new EquipmentItem(equipmentConfig);

            return new Item(config.Name, config.Prefab, config.Icon);
        }

        private void AddSavedItem(string itemId, IReadOnlyDictionary<string, int> savedResources)
        {
            if (string.IsNullOrWhiteSpace(itemId) || savedResources == null || !savedResources.TryGetValue(itemId, out var amount))
                return;

            var config = GameRoot.GameConfig.ItemDatabase.GetStackableItemConfig(itemId);

            if (config != null)
            {
                _items.Add(new StackableItem(config, amount));
                return;
            }

            var itemConfig = GameRoot.GameConfig.ItemDatabase.GetItemConfig(itemId);
            if (itemConfig != null)
            {
                _items.Add(CreateItemFromConfig(itemConfig));
                return;
            }

            _items.Add(new Item(itemId, null, null));
        }

        private void SaveInventoryOrder()
        {
            var itemIds = new List<string>();

            foreach (var item in _items)
            {
                if (item == null)
                {
                    itemIds.Add(string.Empty);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.Name) || itemIds.Contains(item.Name))
                    continue;

                itemIds.Add(item.Name);
            }

            GameRoot.PlayerProgress?.InventoryProgress?.SetInventoryItemOrder(itemIds);
        }

        private int FindItemIndexByName(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return -1;

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] != null && string.Equals(_items[i].Name, itemName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private void TrimTrailingEmptySlots()
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i] != null)
                    break;

                _items.RemoveAt(i);
            }
        }

        private void ClearItemSlot(Item item)
        {
            int index = _items.IndexOf(item);
            if (index >= 0)
                _items[index] = null;

            TrimTrailingEmptySlots();
        }
    }
}
