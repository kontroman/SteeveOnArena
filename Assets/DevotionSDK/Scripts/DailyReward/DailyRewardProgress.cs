using System;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class DailyRewardProgress : BaseProgress
    {
        [SerializeField] private bool initialized;
        [SerializeField] private long lastClaimedUtcDayNumber = -1;
        [SerializeField] private int nextRewardIndex;

        public bool Initialized => initialized;
        public long LastClaimedUtcDayNumber => lastClaimedUtcDayNumber;
        public int NextRewardIndex => Mathf.Max(0, nextRewardIndex);

        public bool RegisterFirstSession(long todayUtcDayNumber)
        {
            if (initialized)
                return false;

            initialized = true;
            lastClaimedUtcDayNumber = todayUtcDayNumber;
            nextRewardIndex = 0;
            Save();
            return true;
        }

        public bool IsRewardAvailable(long todayUtcDayNumber, int rewardsCount)
        {
            return initialized
                && rewardsCount > 0
                && todayUtcDayNumber > lastClaimedUtcDayNumber;
        }

        public int GetRewardIndexForClaim(long todayUtcDayNumber, int rewardsCount)
        {
            if (!IsRewardAvailable(todayUtcDayNumber, rewardsCount))
                return -1;

            if (lastClaimedUtcDayNumber >= 0 && todayUtcDayNumber - lastClaimedUtcDayNumber == 1)
                return NormalizeRewardIndex(nextRewardIndex, rewardsCount);

            return 0;
        }

        public void MarkRewardClaimed(long todayUtcDayNumber, int rewardsCount)
        {
            if (rewardsCount <= 0)
                return;

            var rewardIndex = GetRewardIndexForClaim(todayUtcDayNumber, rewardsCount);
            if (rewardIndex < 0)
                return;

            initialized = true;
            lastClaimedUtcDayNumber = todayUtcDayNumber;
            nextRewardIndex = NormalizeRewardIndex(rewardIndex + 1, rewardsCount);
            Save();
        }

        private static int NormalizeRewardIndex(int index, int rewardsCount)
        {
            if (rewardsCount <= 0)
                return 0;

            var normalized = index % rewardsCount;
            return normalized < 0 ? normalized + rewardsCount : normalized;
        }
    }
}
