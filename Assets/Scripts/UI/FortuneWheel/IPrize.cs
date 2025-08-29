using MineArena.Items;

namespace MineArena.UI.FortuneWheel
{
    public interface IPrize
    {
        Item Item { get; }
        void GiveTo();
        void Construct();
    }
}