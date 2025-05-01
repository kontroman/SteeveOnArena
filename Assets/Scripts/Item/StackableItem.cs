using UnityEngine;

namespace MineArena.Items
{
    public class StackableItem : Item
    {
        public int MaxStackSize { get; }
        public int CurrentStack { get; private set; }

        public StackableItem(StackableItemConfig config, int initialStack) : base(config.Name, config.Prefab, config.Icon)
        {
            MaxStackSize = config.MaxStackSize;
            CurrentStack = Mathf.Clamp(initialStack, 0, MaxStackSize);
        }

        public void AddToStack(int amount)
        {
            CurrentStack += amount;
        }

        public bool CanStackWith(StackableItem other)
        {
            return other != null && other.Name == Name && CurrentStack < MaxStackSize;
        }
    }
}
