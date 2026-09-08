using Devotion.SDK.Controllers;
using MineArena.Managers;
using MineArena.UI.FortuneWheel;
using MineArena.UI.FortuneWheel.DistributionStrategy;

public class InventoryDistribution : IDistributionStrategy
{
    public void Distribute(IPrize prize)
    {
            int amount = prize is ItemPrize itemPrize ? UnityEngine.Mathf.Max(1, itemPrize.Amount) : 1;
            GameRoot.GetManager<InventoryManager>().AddItemById(prize.Item.Name, amount);
    }
}
