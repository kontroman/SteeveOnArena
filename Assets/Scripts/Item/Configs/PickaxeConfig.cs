using UnityEngine;

namespace MineArena.Items
{
    [CreateAssetMenu(fileName = "New Pickaxe", menuName = "Items/Create New Pickaxe", order = 52)]
    public class PickaxeConfig : EquipmentItemConfig
    {
        [SerializeField] private float _miningDuration = 3.33f;
        [SerializeField] private int _miningLoops = 2;

        public float MiningDuration => _miningDuration;
        public int MiningLoops => _miningLoops;
    }
}
