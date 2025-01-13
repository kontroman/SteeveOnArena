namespace Devotion.Items
{
    public class EquipmentItem : Item
    {
        public int Price { get; } 

        public EquipmentItem(EquipmentItemConfig config) : base(config.Name, config.Prefab, config.Icon)
        {
            Price = config.Price;
        }
    }
}