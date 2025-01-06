using UnityEngine;

namespace Devotion.Items
{
    public abstract class EquipmentItemConfig : ItemConfig
    {
        [SerializeField] private int _price;
        public int Price => _price;
    }
}