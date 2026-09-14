using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.PlayerSystem;
using MineArena.UI;
using UnityEditor;
using UnityEngine;

namespace MineArena.Editor
{
    public static class HudActionNoticeValidation
    {
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/hud-notices.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Validate(); }
            catch (Exception error) { File.WriteAllText("Documentation/hud-notices-validation.txt", "FAIL " + error); Debug.LogException(error); }
        };
        [MenuItem("MineArena/Validation/HUD Notices And Small Stat Bonuses")]
        public static void Validate()
        {
            var log = new System.Text.StringBuilder();
            void Check(bool condition, string message) { if (!condition) throw new Exception(message); log.AppendLine("PASS " + message); }
            var progress = new PlayerProgress("notice-validation");
            var config = Resources.Load<PlaytimeRewardsConfig>("UI/PlaytimeRewards");
            var now = new DateTime(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
            bool Ready(HudNoticeKind kind) => HudActionNotice.IsAvailable(kind, progress, 7, config, now);
            Check(!Ready(HudNoticeKind.Development), "No stat notice with no points");
            progress.PlayerDataProgress.CacheExperience(2, 0);
            Check(Ready(HudNoticeKind.Development), "Level up enables stat notice");
            progress.PlayerDataProgress.TrySaveDevelopment(new[] { 1, 0, 0, 0, 0 });
            Check(!Ready(HudNoticeKind.Development), "Spending all saved points clears notice");
            var draft = progress.PlayerDataProgress.CopyDevelopment(); draft[0] = 0;
            Check(!Ready(HudNoticeKind.Development), "Uncommitted draft cannot change HUD availability");
            progress.PlayerDataProgress.TrySaveDevelopment(draft);
            Check(Ready(HudNoticeKind.Development), "Saving a refund enables notice");
            long day = now.Date.Ticks / TimeSpan.TicksPerDay;
            progress.DailyRewardProgress.RegisterFirstSession(day - 1);
            Check(Ready(HudNoticeKind.Daily), "Next day's daily reward enables notice");
            progress.DailyRewardProgress.MarkRewardClaimed(day, 7);
            Check(!Ready(HudNoticeKind.Daily), "Claiming daily reward clears notice");
            Check(HudActionNotice.IsAvailable(HudNoticeKind.Daily, progress, 7, config, now.AddDays(1)), "Daily notice returns at next UTC day");
            Check(config != null && config.Rewards.Count > 0, "Playtime reward config exists");
            progress.PlaytimeGiftProgress.SecondsPlayed = config.Rewards[0].Minutes * 60 - 1;
            Check(!Ready(HudNoticeKind.Playtime), "Playtime reward stays hidden before threshold");
            progress.PlaytimeGiftProgress.SecondsPlayed++;
            Check(Ready(HudNoticeKind.Playtime), "Playtime notice activates at exact threshold");
            progress.PlaytimeGiftProgress.ClaimedRewards = config.Rewards.Count;
            Check(!Ready(HudNoticeKind.Playtime), "No notice after all time rewards claimed");
            Check(Ready(HudNoticeKind.Wheel), "First free spin is advertised before opening wheel");
            var wheel = progress.LuckyWheelProgress;
            Check(!wheel.FortuneWheelInitialized && wheel.FortuneSpins == 0, "Notice query does not grant or initialize spins");
            wheel.InitializeFortuneWheel(now, 1); wheel.TryConsumeFortuneSpin(); wheel.ScheduleNextFreeSpin(now.AddMinutes(30));
            Check(!Ready(HudNoticeKind.Wheel), "No spin notice during cooldown");
            Check(HudActionNotice.IsAvailable(HudNoticeKind.Wheel, progress, 7, config, now.AddMinutes(30)), "Notice appears when free-spin timer expires with wheel closed");
            wheel.AddFortuneSpins(1); Check(Ready(HudNoticeKind.Wheel), "Owned spins enable notice");

            var fixture = new GameObject("Small stat bonuses validation");
            try
            {
                var stats = fixture.AddComponent<PlayerDevelopment>();
                // Check each attribute's individual upper bound, regardless of how the player distributes points.
                typeof(PlayerDevelopment).GetField("_ranks", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(stats, new[] { 50, 50, 50, 50, 50 });
                Check(Mathf.Approximately(stats.MovementMultiplier, 1.25f), "50 movement points add 25% speed");
                Check(Mathf.Approximately(stats.AttackMultiplier, 1.50f), "50 attack points add 50% damage");
                Check(Mathf.Approximately(stats.CriticalChance, .25f), "50 luck points give 25% critical chance");
                Check(Mathf.Abs(stats.ModifyIncomingDamage(100) - 100f / 1.5f) < .001f, "50 defense points reduce damage by one third, not immunity");
                Check(PlayerDevelopment.HealthPerPoint * 50 == 100, "50 health points add 100 HP");
            }
            finally { UnityEngine.Object.DestroyImmediate(fixture); }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab");
            var notices = prefab.GetComponentsInChildren<HudActionNotice>(true);
            Check(notices.Length == 4 && notices.Select(n => n.Kind).Distinct().Count() == 4, "Four distinct HUD targets contain serialized notices");
            Check(notices.All(n => !n.IsVisible), "Saved prefab has no false active notices");
            Check(prefab.GetComponentsInChildren<HudNoticeGraphic>(true).All(g => !g.raycastTarget && g.GetComponent<CanvasRenderer>() != null), "Notices render and never intercept clicks");
            GameUiBuilder.Render(prefab, "Documentation/UI/HUD-AvailableActions.png", preview =>
            {
                preview.transform.Find("ArenaExit")?.gameObject.SetActive(false);
                preview.transform.Find("GiftNavigation")?.gameObject.SetActive(true);
                foreach (var notice in preview.GetComponentsInChildren<HudActionNotice>(true)) notice.SetVisible(true);
            });
            File.WriteAllText("Documentation/hud-notices-validation.txt", log.ToString());
            Debug.Log("[HudNotices] Availability and stat balance validation passed.");
        }
    }
}
