namespace MineArena.Items
{
    public class Pickaxe : EquipmentItem
    {
        public float MiningDuration { get; }
        public int MiningLoops { get; }

        public Pickaxe(PickaxeConfig config) : base(config)
        {
            MiningDuration = config.MiningDuration;
            MiningLoops = config.MiningLoops;
        }
    }
}
