using MineArena.UI.FortuneWheel.DistributionStrategy;
using UnityEngine;

namespace MineArena.UI.FortuneWheel
{
    public class InstantActivationDistribution : IDistributionStrategy
    {
        public void Distribute(IPrize prize)
        {
            Debug.Log($"Activation {prize.Item.Name}");
        }
    }
}