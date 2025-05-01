using UnityEngine;

namespace MineArena.Items
{
    public abstract class EquipmentItemConfig : ItemConfig
    {
        [SerializeField] private int _price;
        public int Price => _price;
    }
}