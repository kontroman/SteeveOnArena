using UnityEngine;

namespace MineArena.Items
{
    [CreateAssetMenu(fileName = "New Stackable Item", menuName = "Items/Create New Stackable Item", order = 51)]
    public class StackableItemConfig : ItemConfig
    {
        [SerializeField] private int _maxStackSize;
        [SerializeField] private string _resourceCategory;
        [SerializeField] private Sprite _topIcon;
        [SerializeField] private Sprite _sideIcon;
        public Sprite TopIcon => _topIcon != null ? _topIcon : Icon;
        public Sprite SideIcon => _sideIcon != null ? _sideIcon : Icon;

        public int MaxStackSize => _maxStackSize;
        public string ResourceCategory => string.IsNullOrWhiteSpace(_resourceCategory) ? Name : _resourceCategory;
    }
}
