using UnityEngine;

namespace Devotion.Equipment
{
    public class Sword : EquipmentItem
    {
        [SerializeField] private int _damage;

        public Sword(int level, ItemTypes type) : base(level, type)
        {
        }

        public int Damage => _damage;
    }
}