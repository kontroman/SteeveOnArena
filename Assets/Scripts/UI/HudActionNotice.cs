using System;
using System.Linq;
using Devotion.SDK.Controllers;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    public enum HudNoticeKind { Development, Daily, Playtime, Wheel }

    public sealed class HudActionNotice : MonoBehaviour
    {
        [SerializeField] private HudNoticeKind kind;
        [SerializeField] private RectTransform badge;
        [SerializeField] private PlaytimeRewardsConfig playtimeRewards;
        private float _nextCheck;
        private Button _button;
        public HudNoticeKind Kind => kind;
        public bool IsVisible => badge != null && badge.gameObject.activeSelf;

        public static void Install(Transform hud)
        {
            var panel = hud.Find("PlayerPanel");
            if (panel != null) Attach(panel, HudNoticeKind.Development);
            foreach (var action in hud.GetComponentsInChildren<GameUiAction>(true))
            {
                switch (action.Destination)
                {
                    case GameUiDestination.Daily: Attach(action.transform, HudNoticeKind.Daily); break;
                    case GameUiDestination.Playtime: Attach(action.transform, HudNoticeKind.Playtime); break;
                    case GameUiDestination.Wheel: Attach(action.transform, HudNoticeKind.Wheel); break;
                }
            }
        }
        private static void Attach(Transform target, HudNoticeKind kind)
        {
            var notice = target.GetComponent<HudActionNotice>() ?? target.gameObject.AddComponent<HudActionNotice>();
            notice.kind = kind;
            notice.EnsureBadge();
        }
        public void EnsureBadge()
        {
            _button = GetComponent<Button>();
            if (badge == null)
            {
                var existing = transform.Find("ActionNotice");
                if (existing != null) badge = existing as RectTransform;
                else
                {
                    var go = new GameObject("ActionNotice", typeof(RectTransform), typeof(HudNoticeGraphic));
                    go.layer = gameObject.layer;
                    go.transform.SetParent(transform, false);
                    badge = (RectTransform)go.transform;
                }
            }
            badge.anchorMin = badge.anchorMax = new Vector2(1, 1);
            badge.pivot = new Vector2(.5f, .5f);
            badge.sizeDelta = new Vector2(30, 30);
            badge.anchoredPosition = new Vector2(-17, -17);
            badge.SetAsLastSibling();
            badge.GetComponent<Graphic>().raycastTarget = false;
            if (playtimeRewards == null) playtimeRewards = Resources.Load<PlaytimeRewardsConfig>("UI/PlaytimeRewards");
        }
        private void Awake() => EnsureBadge();
        private void OnEnable() { EnsureBadge(); Refresh(); }
        private void OnDisable() { if (badge != null) badge.localScale = Vector3.one; }
        private void Update()
        {
            if (Time.unscaledTime >= _nextCheck) { _nextCheck = Time.unscaledTime + .25f; Refresh(); }
            if (IsVisible) badge.localScale = Vector3.one * (1f + .10f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * 3f)));
        }
        public void Refresh()
        {
            if (badge == null) return;
            var progress = GameRoot.PlayerProgress;
            bool allowed = kind == HudNoticeKind.Development ? !TutorialService.Active : TutorialService.AllowHud(
                kind == HudNoticeKind.Daily ? GameUiDestination.Daily : kind == HudNoticeKind.Playtime ? GameUiDestination.Playtime : GameUiDestination.Wheel);
            bool ready = allowed && (_button == null || _button.interactable) && IsAvailable(kind, progress,
                GameRoot.GameConfig?.DailyRewardConfig?.RewardsCount ?? 0, playtimeRewards, DateTime.UtcNow);
            SetVisible(ready);
        }
        public void SetVisible(bool visible)
        {
            if (badge == null) EnsureBadge();
            if (badge.gameObject.activeSelf != visible) badge.gameObject.SetActive(visible);
            if (!visible) badge.localScale = Vector3.one;
        }
        // Read-only: showing the HUD must never claim a gift or grant a spin.
        public static bool IsAvailable(HudNoticeKind kind, PlayerProgress progress, int dailyCount, PlaytimeRewardsConfig playtime, DateTime utcNow)
        {
            if (progress == null) return false;
            switch (kind)
            {
                case HudNoticeKind.Development:
                    return progress.PlayerDataProgress.CurrentLevel - 1 > progress.PlayerDataProgress.CopyDevelopment().Sum();
                case HudNoticeKind.Daily:
                    return progress.DailyRewardProgress.IsRewardAvailable(utcNow.Date.Ticks / TimeSpan.TicksPerDay, dailyCount);
                case HudNoticeKind.Playtime:
                    int index = progress.PlaytimeGiftProgress.ClaimedRewards;
                    if (playtime == null || index < 0 || index >= playtime.Rewards.Count) return false;
                    var reward = playtime.Rewards[index];
                    return reward != null && reward.Item != null && reward.Amount > 0 && progress.PlaytimeGiftProgress.SecondsPlayed >= reward.Minutes * 60f;
                case HudNoticeKind.Wheel:
                    var wheel = progress.LuckyWheelProgress;
                    return !wheel.FortuneWheelInitialized || wheel.FortuneSpins > 0 ||
                        wheel.NextFreeSpinUtcTicks > 0 && utcNow.Ticks >= wheel.NextFreeSpinUtcTicks;
                default: return false;
            }
        }
    }
}
