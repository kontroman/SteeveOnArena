using UnityEngine;

namespace MineArena.Items
{
    public class SwordConfig : EquipmentItemConfig
    {
        [SerializeField] private int _damage;

        public int Damage => _damage;
    }
}