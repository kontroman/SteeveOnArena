using MineArena.PlayerSystem;
using UnityEngine;

namespace MineArena.Items
{
    public enum WeaponItemKind
    {
        Sword,
        Bow
    }

    [CreateAssetMenu(fileName = "New Weapon", menuName = "Items/Create New Weapon", order = 53)]
    public class WeaponItemConfig : EquipmentItemConfig
    {
        [SerializeField] private WeaponItemKind _kind;
        [SerializeField] private AttackConfig _attackConfig;
        [SerializeField] private Material _material;

        public WeaponItemKind Kind => _kind;
        public AttackConfig AttackConfig => _attackConfig;
        public Material Material => _material;
    }
}
