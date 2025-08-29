using MineArena.Items;
using MineArena.UI.FortuneWheel.DistributionStrategy;
using UnityEngine;

namespace MineArena.UI.FortuneWheel
{
    [System.Serializable]
    public class ItemPrize : IPrize
    {
        [SerializeField] private ItemConfig _itemConfig;
        [SerializeField] private DistributionType distributionType;
        [SerializeField] private int _amountInStack;

        private Item _item;

        public string Name => _itemConfig != null ? _itemConfig.Name : "Empty";
        public Sprite Icon => _itemConfig != null ? _itemConfig.Icon : null;
        public int Amount => _amountInStack < 0 ? 1 : _amountInStack;
        public Item Item => _item;

        public void GiveTo()
        {
            IDistributionStrategy strategy = CreateStrategy(distributionType);
            strategy.Distribute(this);
        }

        public void Construct() // Start => public Construct
        {
            _item = new Item(_itemConfig.Name, _itemConfig.Prefab, _itemConfig.Icon);

            if (_itemConfig.Stackable)
                _item = new StackableItem(_itemConfig as StackableItemConfig, _amountInStack);
            else
                _item = new EquipmentItem(_itemConfig as EquipmentItemConfig);
        }

        private IDistributionStrategy CreateStrategy(DistributionType type)
        {
            return type switch
            {
                DistributionType.Inventory => new InventoryDistribution(),
                DistributionType.Instant => new InstantActivationDistribution(),
                _ => new InventoryDistribution()
            };
        }
    }
}