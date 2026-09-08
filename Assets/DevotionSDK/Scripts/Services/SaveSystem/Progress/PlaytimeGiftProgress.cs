using System;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public sealed class PlaytimeGiftProgress : BaseProgress
    {
        public float SecondsPlayed;
        public int ClaimedRewards;
    }
}
