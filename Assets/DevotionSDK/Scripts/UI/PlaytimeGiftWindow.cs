using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using MineArena.Managers;
using MineArena.UI;
using MineArena.Windows.SelectLevel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Devotion.SDK.UI
{
    public class PlaytimeGiftWindow : BaseWindow
    {
        [System.Serializable]
        public sealed class RewardCard
        {
            public GameObject Root;
            public LevelResourceChip Reward;
            public TMP_Text UnlockAt;
            public TMP_Text Countdown;
            public TMP_Text Status;
            public Image Progress;
            public Image Background;

            public void Refresh(PlaytimeReward entry, int index, int claimed, float seconds)
            {
                Root.SetActive(true);
                UnlockAt.text = $"{entry.Minutes} МИНУТ ИГРЫ";
                bool valid = entry.Item != null && entry.Amount > 0;
                Reward.gameObject.SetActive(valid);
                if (valid) Reward.Bind(entry.Item, entry.Amount);
                bool taken = index < claimed;
                float remaining = Mathf.Max(0, entry.Minutes * 60f - seconds);
                Countdown.text = taken ? "Спасибо за игру!" : remaining > 0 ? "Через " + FormatRemaining(remaining) : "Доступна сейчас";
                Status.text = taken ? "ПОЛУЧЕНО" : !valid ? "НЕДОСТУПНО" : remaining <= 0 ? "МОЖНО ЗАБРАТЬ" : "СКОРО ОТКРОЕТСЯ";
                Status.color = taken || remaining <= 0 ? new Color32(35, 112, 89, 255) : new Color32(104, 74, 146, 255);
                Progress.fillAmount = Mathf.Clamp01(seconds / Mathf.Max(1, entry.Minutes * 60f));
                Background.color = taken ? new Color32(212, 243, 227, 255) : Color.white;
            }
        }

        [SerializeField] private RewardCard[] cards;
        [SerializeField] private PlaytimeRewardsConfig config;
        [SerializeField] private LevelResourceChip reward;
        [SerializeField] private TMP_Text timer;
        [SerializeField] private TMP_Text summary;
        [SerializeField] private Button claim;
        private float _nextRefresh;
        private void Awake() { if (claim != null) claim.onClick.AddListener(Claim); }
        private void OnEnable() => Refresh();
        private void Update() { if (Time.unscaledTime >= _nextRefresh) { _nextRefresh = Time.unscaledTime + 0.5f; Refresh(); } }
        public override void CloseWindow() => GameRoot.UIManager.CloseWindow<PlaytimeGiftWindow>();
        private void Refresh()
        {
            var progress = GameRoot.PlayerProgress?.PlaytimeGiftProgress;
            claim.interactable = false;
            if (progress == null || config == null) return;
            int index = Mathf.Max(0, progress.ClaimedRewards);
            summary.text = "Получено подарков: " + index + " / " + config.Rewards.Count;
            if (reward != null) reward.gameObject.SetActive(false);
            if (cards != null)
                for (int i = 0; i < cards.Length; i++)
                {
                    if (i < config.Rewards.Count) cards[i].Refresh(config.Rewards[i], i, index, progress.SecondsPlayed);
                    else cards[i].Root.SetActive(false);
                }
            if (index >= config.Rewards.Count) { timer.text = "Все подарки получены!"; return; }
            var next = config.Rewards[index];
            if (next.Item == null || next.Amount <= 0) { timer.text = "Подарок пока недоступен"; return; }
            float remaining = Mathf.Max(0, next.Minutes * 60f - progress.SecondsPlayed);
            timer.text = remaining <= 0 ? "Подарок готов — забирайте!" : "Следующий подарок через " + FormatRemaining(remaining);
            claim.interactable = remaining <= 0;
        }
        public static string FormatRemaining(float seconds)
        {
            int total = Mathf.CeilToInt(Mathf.Max(0, seconds));
            return total >= 3600 ? $"{total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}" : $"{total / 60:00}:{total % 60:00}";
        }
        private void Claim()
        {
            var progress = GameRoot.PlayerProgress?.PlaytimeGiftProgress;
            var inventory = GameRoot.GetManager<InventoryManager>();
            if (progress == null || inventory == null || config == null || progress.ClaimedRewards < 0 || progress.ClaimedRewards >= config.Rewards.Count) return;
            var next = config.Rewards[progress.ClaimedRewards];
            if (next.Item == null || next.Amount <= 0 || progress.SecondsPlayed < next.Minutes * 60f) return;
            if (GameRoot.GameConfig.ItemDatabase.GetItemConfig(next.Item.Name) != next.Item) return;
            progress.ClaimedRewards++;
            inventory.AddItemById(next.Item.Name, next.Amount);
            progress.Save();
            Refresh();
        }
    }
}
