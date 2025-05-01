using UnityEngine;

namespace MineArena.Items
{
    public enum ArmorSlot
    {
        Helmet,
        Chest,
        Leggings,
        Boots
    }

    [CreateAssetMenu(fileName = "New Armor", menuName = "Items/Create New Armor", order = 51)]
    public class ArmorConfig : EquipmentItemConfig
    {
        [SerializeField] private int _resist;
        [SerializeField] private ArmorSlot _slot;

        public int Resist => _resist;
        public ArmorSlot Slot => _slot;
    }
}