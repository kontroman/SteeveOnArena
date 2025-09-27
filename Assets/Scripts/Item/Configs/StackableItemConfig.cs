using UnityEngine;

namespace MineArena.Items
{
    [CreateAssetMenu(fileName = "New Stackable Item", menuName = "Items/Create New Stackable Item", order = 51)]
    public class StackableItemConfig : ItemConfig
    {
        [SerializeField] private int _maxStackSize;
        [SerializeField] private string _resourceCategory;

        public int MaxStackSize => _maxStackSize;
        public string ResourceCategory => string.IsNullOrWhiteSpace(_resourceCategory) ? Name : _resourceCategory;
    }
}
