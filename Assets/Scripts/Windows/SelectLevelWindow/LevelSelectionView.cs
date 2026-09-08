using System;
using System.Collections.Generic;
using MineArena.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows.SelectLevel
{
    public sealed class LevelSelectionView : MonoBehaviour
    {
        [SerializeField] private RectTransform cardsRoot;
        [SerializeField] private LevelSelectionCard cardPrefab;
        [SerializeField] private LevelResourceChip resourcePrefab;
        [SerializeField] private RectTransform resourcesRoot;
        [SerializeField] private RectTransform rewardsRoot;
        [SerializeField] private GameObject noResources;
        [SerializeField] private GameObject noRewards;
        [SerializeField] private GameObject details;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private TMP_Text levelTitle;
        [SerializeField] private TMP_Text description;
        [SerializeField] private TMP_Text difficulty;
        [SerializeField] private TMP_Text access;
        [SerializeField] private TMP_Text counter;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private Image levelIcon;
        [SerializeField] private Button startButton;
        public Transform TutorialTarget => startButton != null ? startButton.transform : null;
        [SerializeField] private Button closeButton;
        [SerializeField] private ScrollRect listScroll;
        [SerializeField] private ScrollRect detailScroll;
        private readonly Dictionary<int, LevelSelectionCard> _cards = new Dictionary<int, LevelSelectionCard>();
        private IReadOnlyList<LevelConfig> _levels;
        private int _highestUnlocked;
        private int _selected = -1;
        public event Action<int> StartRequested;
        public event Action CloseRequested;

        private void Awake()
        {
            startButton.onClick.AddListener(RequestStart);
            closeButton.onClick.AddListener(() => CloseRequested?.Invoke());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseRequested?.Invoke();
        }

        private void RequestStart()
        {
            if (_levels == null || _selected < 0 || _selected >= _levels.Count ||
                _selected > _highestUnlocked || _levels[_selected] == null ||
                _levels[_selected].LevelPrefab == null) return;
            StartRequested?.Invoke(_selected);
        }

        public void Refresh(IReadOnlyList<LevelConfig> levels, int highestUnlocked)
        {
            _levels = levels;
            _highestUnlocked = Mathf.Max(0, highestUnlocked);
            Clear(cardsRoot);
            _cards.Clear();
            int first = -1, unlockedCount = 0;
            if (levels != null)
            {
                for (int i = 0; i < levels.Count; i++)
                {
                    if (levels[i] == null) continue;
                    if (first < 0) first = i;
                    bool unlocked = i <= _highestUnlocked;
                    if (unlocked) unlockedCount++;
                    var card = Instantiate(cardPrefab, cardsRoot);
                    card.gameObject.SetActive(true);
                    card.Bind(levels[i], i, unlocked, Select);
                    _cards.Add(i, card);
                }
            }
            counter.text = $"Открыто {unlockedCount} из {_cards.Count}";
            details.SetActive(first >= 0);
            emptyState.SetActive(first < 0);
            startButton.interactable = false;
            startLabel.text = "Нет уровней";
            access.text = "Уровни пока не добавлены";
            if (first >= 0) Select(_cards.ContainsKey(_selected) ? _selected : first);
            else _selected = -1;
            Canvas.ForceUpdateCanvases();
            listScroll.StopMovement();
            float overflow = cardsRoot.rect.height - listScroll.viewport.rect.height;
            listScroll.verticalNormalizedPosition = overflow > 0 && _cards.TryGetValue(_selected, out var selectedCard)
                ? 1f - Mathf.Clamp01(-((RectTransform)selectedCard.transform).anchoredPosition.y / overflow)
                : 1f;
        }

        public void Select(int index)
        {
            if (_levels == null || index < 0 || index >= _levels.Count || _levels[index] == null) return;
            _selected = index;
            var config = _levels[index];
            foreach (var card in _cards) card.Value.SetSelected(card.Key == index);
            levelTitle.text = config.DisplayName;
            description.text = config.Description;
            difficulty.text = "Сложность: " + DifficultyLabel(config.Difficulty);
            levelIcon.sprite = config.LevelIcon;
            levelIcon.enabled = levelIcon.sprite != null;
            bool unlocked = index <= _highestUnlocked;
            levelIcon.color = unlocked ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);
            bool configured = config.LevelPrefab != null;
            startButton.interactable = unlocked && configured;
            startLabel.text = !unlocked ? "Уровень закрыт" : configured ? "Начать экспедицию" : "Скоро";
            access.text = unlocked ? (configured ? "Уровень открыт • можно отправляться" : "Этот уровень ещё готовится") :
                "Пройдите «" + (_levels[index - 1] != null ? _levels[index - 1].DisplayName : "Уровень " + index) + "»";
            Clear(resourcesRoot);
            Clear(rewardsRoot);
            int resources = 0, rewards = 0;
            if (config.AvailableResources != null)
                foreach (var item in config.AvailableResources)
                {
                    if (item == null) continue;
                    var chip = Instantiate(resourcePrefab, resourcesRoot);
                    chip.gameObject.SetActive(true);
                    chip.Bind(item, null);
                    resources++;
                }
            if (config.RewardResources != null)
                foreach (var reward in config.RewardResources)
                {
                    if (reward == null || reward.Item == null || reward.Amount <= 0) continue;
                    var chip = Instantiate(resourcePrefab, rewardsRoot);
                    chip.gameObject.SetActive(true);
                    chip.Bind(reward.Item, reward.Amount);
                    rewards++;
                }
            noResources.SetActive(resources == 0);
            noRewards.SetActive(rewards == 0);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)detailScroll.content);
            Canvas.ForceUpdateCanvases();
            detailScroll.StopMovement();
            detailScroll.verticalNormalizedPosition = 1;
        }

        public static string DifficultyLabel(LevelDifficulty value)
        {
            switch (value)
            {
                case LevelDifficulty.Easy: return "Лёгкая";
                case LevelDifficulty.Meduim: return "Средняя";
                case LevelDifficulty.Hard: return "Сложная";
                case LevelDifficulty.Insane: return "Экстремальная";
                default: return value.ToString();
            }
        }

        private static void Clear(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }
    }
}
