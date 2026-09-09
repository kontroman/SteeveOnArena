using System.Collections.Generic;
using System.Linq;
using Achievements;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Managers;
using MineArena.Items;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using MineArena.UI;
using MineArena.Windows.SelectLevel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Windows
{
    public class WindowAchievements : BaseWindow,
        IMessageSubscriber<AchievementMessages.AchievementTargetTaken>,
        IMessageSubscriber<AchievementMessages.AchievementCompleted>,
        IMessageSubscriber<GameMessages.WorldChestOpened>
    {
        [SerializeField] private Transform rowsRoot;
        [SerializeField] private QuestJournalRow rowPrefab;
        [SerializeField] private Button[] filters;
        [SerializeField] private TMP_Text[] filterLabels;
        [SerializeField] private TMP_Text summary, empty, detailTitle, description, progressText, claimStatus;
        [SerializeField] private GameObject details;
        [SerializeField] private Slider progress;
        [SerializeField] private LevelResourceChip reward, target;
        [SerializeField] private Button claim;
        [SerializeField] private Sprite normalTab, selectedTab;
        [SerializeField] private TMP_Text difficulty;
        [SerializeField] private Image detailIcon;
        private List<Achievement> _quests = new List<Achievement>();
        private Achievement _selected;
        private int _filter;
        private readonly List<QuestJournalRow> _rows = new List<QuestJournalRow>();

        private void Awake()
        {
            for (int i = 0; i < filters.Length; i++) { int index = i; filters[i].onClick.AddListener(() => SetFilter(index)); }
            claim.onClick.AddListener(Claim);
        }
        private void OnEnable()
        {
            MessageService.Subscribe(this);
            if (GameRoot.Instance != null) Bind(GameRoot.GetManager<AchievementManager>().GetQuests());
        }
        private void OnDisable() => MessageService.Unsubscribe(this);
        public override void CloseWindow() => GameRoot.UIManager.CloseWindow<WindowAchievements>();
        public void Close() => CloseWindow();
        public void OnMessage(AchievementMessages.AchievementTargetTaken message) => Refresh();
        public void OnMessage(AchievementMessages.AchievementCompleted message) => Refresh();
        public void OnMessage(GameMessages.WorldChestOpened message) => Refresh();
        public void Bind(IReadOnlyList<Achievement> quests)
        {
            _quests = quests?.ToList() ?? new List<Achievement>();
            _selected = null; _filter = _quests.Any(IsReady) ? 1 : 0; Refresh();
        }
        public void SetFilter(int filter) { _filter = Mathf.Clamp(filter, 0, 2); _selected = null; Refresh(); }
        private static bool IsReady(Achievement q) => q.CanTakePrize && !q.IsCompleted;
        private void Refresh()
        {
            int ready = _quests.Count(IsReady), done = _quests.Count(q => q.IsCompleted);
            summary.text = "Выполнено " + done + " / " + _quests.Count + "   •   Наград к получению: " + ready;
            filterLabels[0].text = "В работе  " + (_quests.Count - done);
            filterLabels[1].text = "Можно забрать  " + ready;
            filterLabels[2].text = "Завершено  " + done;
            for (int i = 0; i < filters.Length; i++) filters[i].GetComponent<Image>().sprite = i == _filter ? selectedTab : normalTab;
            var visible = _quests.Where(q => _filter == 2 ? q.IsCompleted : _filter == 1 ? IsReady(q) : !q.IsCompleted)
                .OrderByDescending(IsReady).ThenBy(q => q.Data.Difficulty).ThenByDescending(q => q.MaxValueProgress > 0 ? (float)q.CurrentValueProgress / q.MaxValueProgress : 0).ThenBy(q => q.ID).ToList();
            if (_selected == null || !visible.Contains(_selected)) _selected = visible.FirstOrDefault();
            while (_rows.Count < visible.Count) _rows.Add(Instantiate(rowPrefab, rowsRoot));
            for (int i = 0; i < _rows.Count; i++)
            {
                bool active = i < visible.Count; _rows[i].gameObject.SetActive(active);
                if (!active) continue;
                var q = visible[i]; _rows[i].Bind(q, q == _selected, () => { _selected = q; Refresh(); });
            }
            empty.gameObject.SetActive(visible.Count == 0);
            empty.text = _filter == 1 ? "Пока нет наград к получению.\nПродолжайте задания во вкладке «В работе»." : _filter == 2 ? "Здесь появятся выполненные задания." : "Все поручения выполнены!";
            details.SetActive(_selected != null);
            if (_selected == null) return;
            detailTitle.text = QuestJournalRow.Title(_selected); description.text = QuestJournalRow.Description(_selected);
            if (difficulty != null) difficulty.text = _selected.Data.DifficultyLabel + " · " + (_selected.Data.ItemTarget is ChestCollectionTarget ? Devotion.SDK.Services.Localization.LocalizationService.GetLocalizedText("quest.chests.category") : _selected.Data.ItemTarget is ItemConfig ? "Добыча ресурсов" : "Охота на врагов");
            if (detailIcon != null)
            {
                detailIcon.sprite = _selected.Data.QuestIcon;
                detailIcon.gameObject.SetActive(detailIcon.sprite != null);
                detailIcon.color = Color.white;
            }
            progress.value = _selected.MaxValueProgress > 0 ? (float)_selected.CurrentValueProgress / _selected.MaxValueProgress : 0;
            progressText.text = Mathf.Min(_selected.CurrentValueProgress, _selected.MaxValueProgress) + " / " + _selected.MaxValueProgress;
            var item = _selected.Data.ItemTarget as ItemConfig; target.gameObject.SetActive(item != null); if (item != null) target.Bind(item, null);
            var prize = _selected.Data.ItemPrize;
            reward.gameObject.SetActive(prize?.ItemConfig != null); if (prize?.ItemConfig != null) reward.Bind(prize.ItemConfig, Mathf.Max(1, prize.Amount));
            claim.interactable = IsReady(_selected);
            claimStatus.text = _selected.IsCompleted ? "Награда уже в инвентаре" : IsReady(_selected) ? "Задание выполнено — заберите награду" : "Награда откроется после выполнения";
        }
        public void Claim()
        {
            if (_selected == null || !IsReady(_selected)) return;
            var selected = _selected;
            selected.TransferPrize();
            GameRoot.PlayerProgress.AchievementProgress.SaveProgress(selected);
            GameRoot.PlayerProgress.Save();
            Refresh();
        }
    }
}
