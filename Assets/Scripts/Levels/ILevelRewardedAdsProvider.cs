using System;

namespace MineArena.Levels
{
    // Adapter point for the project's real rewarded ads service.
    public interface ILevelRewardedAdsProvider
    {
        void ShowLevelDoubleRewardsAd(Action<bool> onFinished);
    }
}
