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

                if (config == null)
                {
                    Debug.LogError($"[InventoryManager] item config not found id: {itemId}");
                    continue;
                }

                var newItem = new StackableItem(config, amount);

                _items.Add(newItem);
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

        public void RemoveItem(Item item)
        {
            if (_items.Remove(item))
            {
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
    }
}
