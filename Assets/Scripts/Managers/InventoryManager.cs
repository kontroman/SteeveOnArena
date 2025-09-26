using MineArena.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using Devotion.SDK.Managers;
using Devotion.SDK.Controllers;
using UnityEngine;

namespace MineArena.Managers
{
    public class InventoryManager: BaseManager
    {
        private readonly List<Item> _items = new List<Item>();

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
                    UnityEngine.Debug.LogWarning($"[InventoryManager] Не найден конфиг для предмета с id: {itemId}");
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

                    UnityEngine.Debug.Log($"Item added to inventory: {item.Name}");

                    return;
                }
            }

            _items.Add(item);

            GameRoot.PlayerProgress.InventoryProgress.AddResource(item.Name, amount);

            UnityEngine.Debug.Log($"Item added to inventory: {item.Name}");

            InventoryUpdated?.Invoke();
        }

        public void RemoveItem(Item item)
        {
            if (_items.Remove(item))
            {
                UnityEngine.Debug.Log($"Item removed from inventory: {item.Name}");
                InventoryUpdated?.Invoke();
            }
        }

        //TODO: use BuildingConfig price to see if we have enough resources
        public bool CanAfford()
        {
            return true;
        }
    }
}
