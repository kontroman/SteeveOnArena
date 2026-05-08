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

            var savedResources = GameRoot.PlayerProgress.InventoryProgress.SavedResources;

            foreach (var kvp in savedResources)
            {
                var itemId = kvp.Key;
                var amount = kvp.Value;

                var config = GameRoot.GameConfig.ItemDatabase.GetStackableItemConfig(itemId);

                if (config != null)
                {
                    var newItem = new StackableItem(config, amount);

                    _items.Add(newItem);
                    continue;
                }

                var itemConfig = GameRoot.GameConfig.ItemDatabase.GetItemConfig(itemId);
                if (itemConfig != null)
                {
                    _items.Add(CreateItemFromConfig(itemConfig));
                    continue;
                }

                _items.Add(new Item(itemId, null, null));
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

            _items.Add(item);

            GameRoot.PlayerProgress.InventoryProgress.AddResource(item.Name, amount);

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
                    _items.Remove(stackable);
                    Debug.Log($"Item removed from inventory: {item.Name}");
                }
                else
                {
                    Debug.Log($"Reduced stack for item: {item.Name} by {toRemove}");
                }

                InventoryUpdated?.Invoke();
                return;
            }

            if (_items.Remove(item))
            {
                GameRoot.PlayerProgress.InventoryProgress.RemoveResource(item.Name, amount);
                Debug.Log($"Item removed from inventory: {item.Name}");
                InventoryUpdated?.Invoke();
            }
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
                    _items.Remove(stackable);
            }

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
    }
}
