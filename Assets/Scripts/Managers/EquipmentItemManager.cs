using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;
using Devotion.Equipment;

namespace Devotion.Managers
{
    public class EquipmentItemManager : BaseManager
    {
        [SerializeField] private EquipmentItemsConfig _itemsConfig;

        private EquipmentItemConfig _item;

        public EquipmentItem GetItem(int levelItem, ItemTypes itemTypes)
        {
            return new EquipmentItem(levelItem, itemTypes);
        }

        public void CreatEquipmentItem(Vector3 position, ItemTypes type, int level)
        {
            _item = FindObject(GetEquipmentList(type), level);
            var obj = Instantiate(_item.Prefab);
            obj.transform.position = position;
        }

        private List<EquipmentItemConfig> GetEquipmentList(ItemTypes type)
        {
            switch (type)
            {
                case ItemTypes.Sword:
                    return _itemsConfig.Swords;

                case ItemTypes.Armor:
                    return _itemsConfig.Armor;

                default:
                    throw new ArgumentException(nameof(type));
            }
        }

        private EquipmentItemConfig FindObject(List<EquipmentItemConfig> items, int level)
        {
            var item = items.Where(item => item.Level == level).Select(item => item).ToList();

            if (item.Count == 1)           
                return item[0];
            else            
                throw new ArgumentException(nameof(item));            
        }
    }
}
