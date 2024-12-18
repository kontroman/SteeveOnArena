using UnityEngine;

namespace Devotion.Equipment
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Items/Create new Armor", order = 51)]
    public class ArmorConfig : EquipmentItemConfig
    {
        [SerializeField] private int _resist;

        private ItemTypes _slot = ItemTypes.Armor;

        public int Resist => _resist;
        public ItemTypes Slot => _slot;
    }
}
