using UnityEngine;

namespace Devotion.Equipment
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Items/Create new sword", order = 51)]
    public class SwordConfig : EquipmentItemConfig
    {
        [SerializeField] private int _damage;

        public int Damage => _damage;
    }
}
