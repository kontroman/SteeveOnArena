using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Items
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "MineArena/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemConfig> allItems;

        private Dictionary<string, ItemConfig> _itemsById;

        public void Initialize()
        {
            _itemsById = new Dictionary<string, ItemConfig>();

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
            if (_itemsById == null)
                return null;

            if (_itemsById.TryGetValue(id, out var config))
                return config;

            return null;
        }

        public StackableItemConfig GetStackableItemConfig(string id)
        {
            return GetItemConfig(id) as StackableItemConfig;
        }
    }
}
