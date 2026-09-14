using UnityEngine;

namespace MineArena.Items
{
    public enum ArmorSlot
    {
        Helmet,
        Chest,
        Leggings,
        Boots,
        OffHand
    }

    public enum ArmorGrade
    {
        Leather,
        Iron,
        Gold,
        Diamond,
        Netherite
    }

    [CreateAssetMenu(fileName = "New Armor", menuName = "Items/Create New Armor", order = 51)]
    public class ArmorConfig : EquipmentItemConfig
    {
        [SerializeField] private int _resist;
        [SerializeField] private ArmorSlot _slot;
        [SerializeField] private ArmorGrade _grade;
        [SerializeField] private Material _material;

        [SerializeField, Min(1)] private int _maxDurability = 336;
        public int MaxDurability => _slot == ArmorSlot.OffHand ? Mathf.Max(1, _maxDurability) : 0;

        public int Resist => _resist;
        public ArmorSlot Slot => _slot;
        public ArmorGrade Grade => _grade;
        public Material Material => _material;
    }
}
