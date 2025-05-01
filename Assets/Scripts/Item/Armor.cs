namespace MineArena.Items
{
    public class Armor : EquipmentItem
    {
        public int Resist { get; }
        public ArmorSlot Slot { get; }

        public Armor(ArmorConfig config) : base(config)
        {
            Resist = config.Resist;
            Slot = config.Slot;
        }
    }
}