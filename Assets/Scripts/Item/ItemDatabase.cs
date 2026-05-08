using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Items
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "MineArena/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemConfig> allItems;

        public IReadOnlyList<ItemConfig> AllItems => allItems;

        private Dictionary<string, ItemConfig> _itemsById;

        public void Initialize()
        {
            _itemsById = new Dictionary<string, ItemConfig>();

            if (allItems == null)
                return;

            foreach (var item in allItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Name))
                    continue;

                if (_itemsById.ContainsKey(item.Name))
                    continue;

                _itemsById.Add(item.Name, item);
            }

            Debug.Log($"[ItemDatabase] loaded items: {_itemsById.Count}");
        }

        public ItemConfig GetItemConfig(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            if (_itemsById == null)
                Initialize();

            if (_itemsById.TryGetValue(id, out var config))
                return config;

            return allItems.Find(x => x != null && x.Name == id);
        }

        public StackableItemConfig GetStackableItemConfig(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || allItems == null)
                return null;

            return allItems.Find(x => x != null && x.Name == id) as StackableItemConfig;
        }
    }
}
