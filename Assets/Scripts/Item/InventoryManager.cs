using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Devotion.Items
{
    public class InventoryManager
    {
        public static InventoryManager Instance { get; } = new InventoryManager();

        private readonly List<Item> _items = new List<Item>();

        public event Action InventoryUpdated;

        private InventoryManager() { }

        public IReadOnlyList<Item> Items => _items;

        public void AddItem(Item item)
        {
            if (item is StackableItem stackableItem)
            {
                var existingItem = _items
                    .OfType<StackableItem>()
                    .FirstOrDefault(i => i.CanStackWith(stackableItem));

                if (existingItem != null)
                {
                    existingItem.AddToStack(1);

                    InventoryUpdated?.Invoke();
                    UnityEngine.Debug.Log($"Item added to inventory: {item.Name}");

                    return;
                }
            }

            _items.Add(item);
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
