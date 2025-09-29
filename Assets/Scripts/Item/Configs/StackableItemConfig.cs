using Quests;
using UnityEngine;

namespace MineArena.Items
{
    [CreateAssetMenu(fileName = "New Stackable Item", menuName = "Items/Create New Stackable Item", order = 51)]
    public class StackableItemConfig : ItemConfig
    {
        [SerializeField] private int _maxStackSize;

        public int MaxStackSize => _maxStackSize;
    }
}