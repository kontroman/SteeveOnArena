using UnityEngine;

namespace MineArena.Items
{
    public class StackableItem : Item
    {
        private readonly StackableItemConfig _config;

        public int MaxStackSize { get; }
        public int CurrentStack { get; private set; }
        public StackableItemConfig Config => _config;
        public string ResourceCategory => _config.ResourceCategory;

        public StackableItem(StackableItemConfig config, int initialStack) : base(config.Name, config.Prefab, config.Icon)
        {
            _config = config;
            MaxStackSize = config.MaxStackSize;
            CurrentStack = Mathf.Clamp(initialStack, 0, MaxStackSize);
        }

        public void AddToStack(int amount)
        {
            if (amount <= 0)
                return;

            CurrentStack += amount;
        }

        public void RemoveFromStack(int amount)
        {
            if (amount <= 0)
                return;

            CurrentStack = Mathf.Max(0, CurrentStack - amount);
        }

        public bool CanStackWith(StackableItem other)
        {
            return other != null && other.Name == Name && CurrentStack < MaxStackSize;
        }
    }
}
