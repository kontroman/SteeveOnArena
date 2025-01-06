using UnityEngine;

namespace Devotion.Items
{
    public class SwordConfig : EquipmentItemConfig
    {
        [SerializeField] private int _damage;

        public int Damage => _damage;
    }
}