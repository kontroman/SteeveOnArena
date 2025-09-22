using Devotion.SDK.Controllers;
using MineArena.Managers;
using MineArena.UI.FortuneWheel;
using MineArena.UI.FortuneWheel.DistributionStrategy;

public class InventoryDistribution : IDistributionStrategy
{
    public void Distribute(IPrize prize)
    {
        GameRoot.GetManager<InventoryManager>().AddItem(prize.Item);
    }
}