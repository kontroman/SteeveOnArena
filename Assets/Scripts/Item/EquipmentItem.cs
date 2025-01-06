namespace Devotion.Items
{
    public abstract class EquipmentItem : Item
    {
        public int Price { get; }

        protected EquipmentItem(EquipmentItemConfig config) : base(config.Name, config.Prefab, config.Icon)
        {
            Price = config.Price;
        }
    }
}