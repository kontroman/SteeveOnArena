using System;
using System.Collections;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.SaveSystem;
using Devotion.SDK.Services.SaveSystem.Progress;
using Devotion.SDK.UI;
using MineArena.Managers;
using MineArena.Messages.MessageService;
using UnityEngine;

namespace Devotion.SDK.DailyReward
{
    public class DailyRewardManager : BaseManager,
        IMessageSubscriber<Devotion.SDK.Messages.Player.PlayerProgressLoaded>
    {
        [SerializeField] private bool openWindowAutomatically = true;

        private bool startupRewardProcessed;
        private Coroutine waitForProgressCoroutine;

        public override void InitManager()
        {
            base.InitManager();

            if (SaveService.Instance.IsLoaded)
                TryProcessStartupReward(nameof(InitManager));
            else
                StartWaitingForProgress();
        }

        private void OnEnable()
        {
            MessageService.Subscribe(this);
        }

        private void Start()
        {
            if (!startupRewardProcessed)
                StartWaitingForProgress();
        }

        private void OnDisable()
        {
            MessageService.Unsubscribe(this);

            if (waitForProgressCoroutine != null)
            {
                StopCoroutine(waitForProgressCoroutine);
                waitForProgressCoroutine = null;
            }
        }

        public void OnMessage(Devotion.SDK.Messages.Player.PlayerProgressLoaded message)
        {
            TryProcessStartupReward(nameof(Devotion.SDK.Messages.Player.PlayerProgressLoaded));
        }

        public bool TryClaimCurrentReward()
        {
            var config = GetConfig();
            var progress = GetProgress();
            if (config == null || progress == null || !config.HasRewards)
                return false;

            var todayUtcDayNumber = GetCurrentUtcDayNumber();
            var rewardIndex = progress.GetRewardIndexForClaim(todayUtcDayNumber, config.RewardsCount);
            var reward = config.GetReward(rewardIndex);
            if (reward == null || !reward.IsValid)
                return false;

            var inventory = GameRoot.GetManager<InventoryManager>();
            if (inventory == null)
                return false;

            inventory.AddItemById(reward.ItemId, reward.Amount);
            progress.MarkRewardClaimed(todayUtcDayNumber, config.RewardsCount);
            if (rewardIndex == config.RewardsCount - 1)
                MineArena.Cosmetics.SkinService.GrantReward("daily_cycle_complete");

            Debug.Log($"[DailyRewardManager] Claimed daily reward: {reward.ItemId} x{reward.Amount}.");
            return true;
        }

        public bool CanClaim => GetConfig() != null && GetProgress() != null &&
            GetProgress().IsRewardAvailable(GetCurrentUtcDayNumber(), GetConfig().RewardsCount);

        public int CurrentRewardIndex
        {
            get
            {
                var config = GetConfig();
                var progress = GetProgress();
                if (config == null || progress == null || !config.HasRewards) return -1;
                int index = progress.GetRewardIndexForClaim(GetCurrentUtcDayNumber(), config.RewardsCount);
                return index >= 0 ? index : progress.NextRewardIndex;
            }
        }

        public DateTime GetRewardAvailableAtLocal(int index)
        {
            var now = DateTime.UtcNow;
            var progress = GetProgress();
            long today = now.Date.Ticks / TimeSpan.TicksPerDay;
            long firstDay = CanClaim ? today : Math.Max(today + 1, (progress?.LastClaimedUtcDayNumber ?? today) + 1);
            int daysAhead = Mathf.Max(0, index - CurrentRewardIndex);
            return new DateTime((firstDay + daysAhead) * TimeSpan.TicksPerDay, DateTimeKind.Utc).ToLocalTime();
        }

        public bool IsRewardClaimed(int index)
        {
            var config = GetConfig();
            var progress = GetProgress();
            if (config == null || progress == null) return false;
            if (CanClaim)
                return index < progress.GetRewardIndexForClaim(GetCurrentUtcDayNumber(), config.RewardsCount);
            return index < (progress.CompletedCycle ? config.RewardsCount : progress.NextRewardIndex);
        }

        public void OpenRewards()
        {
            var config = GetConfig(); var progress = GetProgress();
            if (config == null || progress == null || !config.HasRewards) return;
            int index = progress.GetRewardIndexForClaim(GetCurrentUtcDayNumber(), config.RewardsCount);
            var window = GameRoot.UIManager.OpenWindow<DailyGiftWIndow>() as DailyGiftWIndow;
            window?.Setup(this, config, index >= 0 ? index : progress.NextRewardIndex);
        }

        private void StartWaitingForProgress()
        {
            if (waitForProgressCoroutine == null && isActiveAndEnabled)
                waitForProgressCoroutine = StartCoroutine(WaitForProgressAndProcess());
        }

        private IEnumerator WaitForProgressAndProcess()
        {
            const float timeoutSeconds = 5f;
            var startedAt = Time.realtimeSinceStartup;

            while (!startupRewardProcessed && !SaveService.Instance.IsLoaded)
            {
                if (Time.realtimeSinceStartup - startedAt >= timeoutSeconds)
                {
                    Debug.LogWarning("[DailyRewardManager] SaveService is not loaded yet. Daily reward startup check is waiting for PlayerProgressLoaded.");
                    waitForProgressCoroutine = null;
                    yield break;
                }

                yield return null;
            }

            waitForProgressCoroutine = null;
            TryProcessStartupReward(nameof(WaitForProgressAndProcess));
        }

        private void TryProcessStartupReward(string source)
        {
            if (startupRewardProcessed)
                return;

            var config = GetConfig();
            var progress = GetProgress();
            if (config == null)
            {
                Debug.LogWarning($"[DailyRewardManager] DailyRewardConfig is missing. Source: {source}.");
                return;
            }

            if (progress == null)
            {
                Debug.LogWarning($"[DailyRewardManager] PlayerProgress is missing. Source: {source}.");
                return;
            }

            if (!config.HasRewards)
            {
                Debug.LogWarning($"[DailyRewardManager] DailyRewardConfig has no rewards. Source: {source}.");
                return;
            }

            var todayUtcDayNumber = GetCurrentUtcDayNumber();
            if (progress.RegisterFirstSession(todayUtcDayNumber))
            {
                startupRewardProcessed = true;
                Debug.Log("[DailyRewardManager] First player session registered. Daily reward window skipped.");
                return;
            }

            if (!progress.IsRewardAvailable(todayUtcDayNumber, config.RewardsCount))
            {
                startupRewardProcessed = true;
                Debug.Log("[DailyRewardManager] Daily reward is not available today.");
                return;
            }

            startupRewardProcessed = true;

            if (!openWindowAutomatically)
                return;

            ShowDailyRewardWindow(config, progress, todayUtcDayNumber);
        }

        private void ShowDailyRewardWindow(DailyRewardConfig config, DailyRewardProgress progress, long todayUtcDayNumber)
        {
            var rewardIndex = progress.GetRewardIndexForClaim(todayUtcDayNumber, config.RewardsCount);
            if (rewardIndex < 0)
                return;

            var window = GameRoot.UIManager?.ShowWindow<DailyGiftWIndow>() as DailyGiftWIndow;
            if (window == null)
            {
                Debug.LogWarning("[DailyRewardManager] Daily reward window prefab is not registered in UIManager.");
                return;
            }

            window.Setup(this, config, rewardIndex);
        }

        private static DailyRewardConfig GetConfig()
        {
            return GameRoot.GameConfig != null ? GameRoot.GameConfig.DailyRewardConfig : null;
        }

        private static DailyRewardProgress GetProgress()
        {
            return GameRoot.PlayerProgress != null ? GameRoot.PlayerProgress.DailyRewardProgress : null;
        }

        private static long GetCurrentUtcDayNumber()
        {
            return DateTime.UtcNow.Date.Ticks / TimeSpan.TicksPerDay;
        }
    }
}
