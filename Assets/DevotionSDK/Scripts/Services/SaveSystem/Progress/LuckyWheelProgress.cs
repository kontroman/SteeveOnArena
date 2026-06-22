using System;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class LuckyWheelProgress : BaseProgress
    {
        [SerializeField] private bool fortuneWheelInitialized;
        [SerializeField] private int fortuneSpins;
        [SerializeField] private long lastFreeSpinUtcTicks;
        [SerializeField] private long nextFreeSpinUtcTicks;
        [SerializeField] private string lastFortuneRewardId;
        [SerializeField] private int fortuneRewardStreak;

        public bool FortuneWheelInitialized => fortuneWheelInitialized;
        public int FortuneSpins => fortuneSpins;
        public long LastFreeSpinUtcTicks => lastFreeSpinUtcTicks;
        public long NextFreeSpinUtcTicks => nextFreeSpinUtcTicks;
        public string LastFortuneRewardId => lastFortuneRewardId;
        public int FortuneRewardStreak => fortuneRewardStreak;

        public void InitializeFortuneWheel(DateTime nowUtc, int startSpins)
        {
            if (fortuneWheelInitialized)
                return;

            fortuneWheelInitialized = true;
            fortuneSpins = Mathf.Max(fortuneSpins, startSpins);
            lastFreeSpinUtcTicks = nowUtc.Ticks;
            nextFreeSpinUtcTicks = 0;
            Save();
        }

        public void AddFortuneSpins(int amount)
        {
            if (amount <= 0)
                return;

            fortuneSpins += amount;
            nextFreeSpinUtcTicks = 0;
            Save();
        }

        public bool TryConsumeFortuneSpin()
        {
            if (fortuneSpins <= 0)
                return false;

            fortuneSpins--;
            Save();
            return true;
        }

        public void ScheduleNextFreeSpin(DateTime nextFreeSpinUtc)
        {
            nextFreeSpinUtcTicks = nextFreeSpinUtc.Ticks;
            Save();
        }

        public void GrantFreeSpin(DateTime nowUtc)
        {
            fortuneSpins++;
            lastFreeSpinUtcTicks = nowUtc.Ticks;
            nextFreeSpinUtcTicks = 0;
            Save();
        }

        public void RegisterFortuneRewardRoll(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
                return;

            if (rewardId == lastFortuneRewardId)
                fortuneRewardStreak++;
            else
            {
                lastFortuneRewardId = rewardId;
                fortuneRewardStreak = 1;
            }

            Save();
        }
    }
}
