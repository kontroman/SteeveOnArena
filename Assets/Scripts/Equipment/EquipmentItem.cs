using System;
using UnityEngine;
using UnityEngine.UI;

namespace Devotion.Equipment
{
    public class EquipmentItem : MonoBehaviour
    {
        [SerializeField] private int _lvl;
        [SerializeField] private Image _icon;
        [SerializeField] private int _price;
        [SerializeField] private ItemTypes _type;

        public EquipmentItem(int level, ItemTypes type)
        {
            if (level < 0) 
                throw new ArgumentOutOfRangeException(nameof(level));

            _lvl = level;
            _type = type;
        }

        public int LvlItem => _lvl;
        public Image Icon => _icon;
        public int Price => _price;
        public ItemTypes Type => _type;
    }
}
