using UnityEngine;

namespace Devotion.Equipment
{
    public class Armor : EquipmentItem
    {
        [SerializeField] private int _resist;

        private ItemTypes _slot = ItemTypes.ArmorSlots;

        public Armor(int level, ItemTypes type) : base(level, type)
        {
        }

        public int Resist => _resist;
        public ItemTypes Slot => _slot;
    }
}
