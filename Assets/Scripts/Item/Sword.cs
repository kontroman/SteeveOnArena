namespace Devotion.Items
{
    public class Sword : EquipmentItem
    {
        public int Damage { get; }

        public Sword(SwordConfig config) : base(config)
        {
            Damage = config.Damage;
        }
    }
}