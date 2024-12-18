using Devotion.Managers;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Devotion.Equipment
{
    public class EquipmentItemManager : BaseManager
    {
        [SerializeField] private EquipmentItemsConfig _itemsConfig;

        public EquipmentItem GetItem(int levelItem, ItemTypes itemTypes)
        {
            return new EquipmentItem(levelItem, itemTypes);
        }

        public void CreatObject(Vector3 position, ItemTypes type, int level)
        {
            List<EquipmentItemConfig> listItems = null;

            foreach (var list in _itemsConfig.Items)
            {
                if (list.Value == type)
                {
                    listItems = list.Key.Where(item => item.Level == level).Select(item => item).ToList();
                    break;
                }
            }

            if (listItems.Count == 1)
            {
                var obj = Instantiate(listItems[0].Prefab);
                obj.transform.position = position;
            }
            else
            {
                throw new ArgumentNullException(nameof(listItems));
            }
        }
    }
}
