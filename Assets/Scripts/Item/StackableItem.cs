using UnityEngine;

namespace Devotion.Items
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

        public bool AddToStack(int amount)
        {
            if (CurrentStack + amount > MaxStackSize) return false;

            CurrentStack += amount;
            return true;
        }
    }
}