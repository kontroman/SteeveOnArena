using Devotion.Managers;
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
            switch (type)
            {
                case ItemTypes.Sword:
                    _item = FindObject(_itemsConfig.Swords, level);
                    break;

                case ItemTypes.Armor:
                    _item = FindObject(_itemsConfig.Armor, level);
                    break;

                default:
                    throw new ArgumentException(nameof(type));
            }

            var obj = Instantiate(_item.Prefab);
            obj.transform.position = position;
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
