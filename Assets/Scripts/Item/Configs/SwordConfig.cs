using UnityEngine;
using MineArena.PlayerSystem;

namespace MineArena.Items
{
    public class SwordConfig : EquipmentItemConfig
    {
        [SerializeField] private int _damage;
        [SerializeField] private AttackConfig _attackProfile;

        public int Damage => _damage;
        public AttackConfig AttackProfile => _attackProfile;
    }
}
