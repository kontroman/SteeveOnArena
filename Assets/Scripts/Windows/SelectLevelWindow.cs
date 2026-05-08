using System;
using System.Collections.Generic;
using Devotion.SDK.Async;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using DG.Tweening;
using MineArena.Basics;
using MineArena.Controllers;
using MineArena.Items;
using MineArena.Levels;
using MineArena.Managers;
using MineArena.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows
{
    public class SelectLevelWindow : BaseWindow
    {
        private const int MinimumVisibleSlots = 4;
        private const int UiLayer = 5;
        private const string ResourceIconPath = "Prefabs/Windows/ResourceIcon";

        private readonly List<LevelCardView> _cards = new List<LevelCardView>();

        private SpriteSet _sprites;
        private RectTransform _cardsRoot;
        private RectTransform _detailsPanel;
        private RectTransform _availableResourcesRoot;
        private RectTransform _rewardResourcesRoot;
        private TextMeshProUGUI _difficultyTitle;
        private TextMeshProUGUI _descriptionText;
        private TextMeshProUGUI _emptyStateText;
        private TextMeshProUGUI _startButtonLabel;
        private Button _startButton;
        private CanvasGroup _detailsCanvasGroup;
        private ResourceIcon _resourceIconPrefab;
        private LevelConfig _selectedConfig;
        private int _selectedIndex = -1;
        private bool _isBuilt;

        private void Awake()
        {
            BuildWindow();
        }

        private void OnEnable()
        {
            BuildWindow();
            RefreshLevels();
        }

        public override void CloseWindow()
        {
            GameRoot.UIManager.CloseWindow<SelectLevelWindow>();
        }

        private void BuildWindow()
        {
            if (_isBuilt)
                return;

            _isBuilt = true;
            _sprites = SpriteSet.Create();
            _resourceIconPrefab = Resources.Load<ResourceIcon>(ResourceIconPath);

            SetLayerRecursively(gameObject, UiLayer);
            ClearChildren((RectTransform)transform);

            var rect = (RectTransform)transform;
            Stretch(rect);

            var background = EnsureImage(gameObject);
            background.sprite = _sprites.Overlay;
            background.type = Image.Type.Simple;
            background.color = Color.white;
            background.raycastTarget = true;

            BuildLeftPanel(rect);
            BuildDetailsPanel(rect);
            BuildCloseButton(rect);
        }

        private void BuildLeftPanel(RectTransform root)
        {
            var panel = CreatePanel("LevelSelectTower", root, _sprites.Sidebar, out var panelImage);
            panel.anchorMin = new Vector2(0f, 0f);
            panel.anchorMax = new Vector2(0f, 1f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(386f, 0f);
            panelImage.raycastTarget = true;

            var titlePlate = CreatePanel("TitlePlate", panel, _sprites.TitlePlate, out _);
            titlePlate.anchorMin = new Vector2(0f, 1f);
            titlePlate.anchorMax = new Vector2(0f, 1f);
            titlePlate.pivot = new Vector2(0f, 1f);
            titlePlate.anchoredPosition = new Vector2(52f, -10f);
            titlePlate.sizeDelta = new Vector2(270f, 58f);

            var title = CreateText("Title", titlePlate, "ВЫБОР УРОВНЯ", 25f, FontStyles.Bold, TextAlignmentOptions.Center);
            title.color = new Color32(238, 235, 224, 255);
            title.enableAutoSizing = true;
            title.fontSizeMin = 14f;
            title.fontSizeMax = 25f;
            Stretch(title.rectTransform, 12f, 5f, 12f, 7f);

            var cardsScroll = CreateRect("LevelCardsScroll", panel);
            cardsScroll.anchorMin = new Vector2(0f, 0f);
            cardsScroll.anchorMax = new Vector2(1f, 1f);
            cardsScroll.offsetMin = new Vector2(70f, 64f);
            cardsScroll.offsetMax = new Vector2(-54f, -106f);

            var scrollRect = cardsScroll.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;

            var viewport = CreateImage("Viewport", cardsScroll, _sprites.Overlay);
            viewport.color = new Color(0f, 0f, 0f, 0f);
            viewport.raycastTarget = true;
            Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            _cardsRoot = CreateRect("Content", viewport.rectTransform);
            _cardsRoot.anchorMin = new Vector2(0f, 1f);
            _cardsRoot.anchorMax = new Vector2(1f, 1f);
            _cardsRoot.pivot = new Vector2(0.5f, 1f);
            _cardsRoot.anchoredPosition = Vector2.zero;
            _cardsRoot.sizeDelta = Vector2.zero;

            scrollRect.viewport = viewport.rectTransform;
            scrollRect.content = _cardsRoot;

            var layout = _cardsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = _cardsRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            BuildLantern(panel, new Vector2(32f, 28f));
            BuildLantern(panel, new Vector2(336f, 28f));
            BuildLeafDecoration(panel);
        }

        private void BuildDetailsPanel(RectTransform root)
        {
            _detailsPanel = CreatePanel("LevelDetailsPanel", root, _sprites.DetailPanel, out _);
            _detailsPanel.anchorMin = new Vector2(0f, 0f);
            _detailsPanel.anchorMax = new Vector2(1f, 1f);
            _detailsPanel.offsetMin = new Vector2(436f, 92f);
            _detailsPanel.offsetMax = new Vector2(-162f, -96f);
            _detailsCanvasGroup = _detailsPanel.gameObject.AddComponent<CanvasGroup>();

            _difficultyTitle = CreateText("DifficultyTitle", _detailsPanel, string.Empty, 42f, FontStyles.Bold, TextAlignmentOptions.Left);
            _difficultyTitle.color = new Color32(145, 224, 70, 255);
            _difficultyTitle.enableAutoSizing = true;
            _difficultyTitle.fontSizeMin = 24f;
            _difficultyTitle.fontSizeMax = 42f;
            _difficultyTitle.rectTransform.anchorMin = new Vector2(0f, 1f);
            _difficultyTitle.rectTransform.anchorMax = new Vector2(1f, 1f);
            _difficultyTitle.rectTransform.pivot = new Vector2(0f, 1f);
            _difficultyTitle.rectTransform.offsetMin = new Vector2(34f, -84f);
            _difficultyTitle.rectTransform.offsetMax = new Vector2(-34f, -28f);

            _descriptionText = CreateText("Description", _detailsPanel, string.Empty, 16f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            _descriptionText.color = new Color32(225, 224, 221, 255);
            _descriptionText.enableWordWrapping = true;
            _descriptionText.rectTransform.anchorMin = new Vector2(0f, 1f);
            _descriptionText.rectTransform.anchorMax = new Vector2(1f, 1f);
            _descriptionText.rectTransform.pivot = new Vector2(0f, 1f);
            _descriptionText.rectTransform.offsetMin = new Vector2(34f, -132f);
            _descriptionText.rectTransform.offsetMax = new Vector2(-34f, -86f);

            BuildSectionHeader(_detailsPanel, "AvailableHeader", _sprites.PickaxeIcon, "ДОБЫЧА:", new Vector2(34f, -194f));
            _availableResourcesRoot = BuildResourceRow("AvailableResources", _detailsPanel, new Vector2(34f, -285f));

            BuildSectionHeader(_detailsPanel, "RewardHeader", _sprites.ChestIcon, "НАГРАДА ЗА ПРОХОЖДЕНИЕ:", new Vector2(34f, -355f));
            _rewardResourcesRoot = BuildResourceRow("RewardResources", _detailsPanel, new Vector2(34f, -446f));

            _startButton = CreateButton("StartButton", _detailsPanel, _sprites.StartButton, out var startImage);
            _startButton.transition = Selectable.Transition.ColorTint;
            _startButton.targetGraphic = startImage;
            _startButton.onClick.AddListener(StartSelectedLevel);

            var startRect = (RectTransform)_startButton.transform;
            startRect.anchorMin = new Vector2(0.5f, 0f);
            startRect.anchorMax = new Vector2(0.5f, 0f);
            startRect.pivot = new Vector2(0.5f, 0f);
            startRect.anchoredPosition = new Vector2(0f, 28f);
            startRect.sizeDelta = new Vector2(320f, 70f);

            _startButtonLabel = CreateText("StartButtonLabel", startRect, "НАЧАТЬ", 31f, FontStyles.Bold, TextAlignmentOptions.Center);
            _startButtonLabel.color = new Color32(250, 247, 232, 255);
            _startButtonLabel.enableAutoSizing = true;
            _startButtonLabel.fontSizeMin = 18f;
            _startButtonLabel.fontSizeMax = 31f;
            Stretch(_startButtonLabel.rectTransform, 24f, 8f, 24f, 10f);

            _emptyStateText = CreateText("EmptyState", _detailsPanel, string.Empty, 18f, FontStyles.Bold, TextAlignmentOptions.Center);
            _emptyStateText.color = new Color32(190, 187, 174, 255);
            _emptyStateText.enableWordWrapping = true;
            Stretch(_emptyStateText.rectTransform, 44f);
            _emptyStateText.gameObject.SetActive(false);
        }

        private void BuildCloseButton(RectTransform root)
        {
            var closeButton = CreateButton("CloseButton", root, _sprites.CloseButton, out var closeImage);
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(CloseWindow);

            var rect = (RectTransform)closeButton.transform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-26f, -18f);
            rect.sizeDelta = new Vector2(64f, 64f);
        }

        private void RefreshLevels()
        {
            ClearCards();

            var levels = GameRoot.GameConfig != null ? GameRoot.GameConfig.Levels : null;
            var levelsCount = levels != null ? levels.Count : 0;
            var totalSlots = Mathf.Max(MinimumVisibleSlots, levelsCount);
            var firstUnlockedIndex = -1;

            for (var i = 0; i < totalSlots; i++)
            {
                var config = i < levelsCount ? levels[i] : null;
                var unlocked = config != null && IsLevelUnlocked(i);
                var card = CreateLevelCard(_cardsRoot);
                card.Setup(i, config, unlocked, false, _sprites, OnCardClicked);
                _cards.Add(card);

                if (unlocked && firstUnlockedIndex < 0)
                {
                    firstUnlockedIndex = i;
                }
            }

            if (_selectedIndex < 0 || _selectedIndex >= levelsCount || !IsLevelUnlocked(_selectedIndex))
            {
                _selectedIndex = firstUnlockedIndex;
            }

            if (_selectedIndex >= 0)
            {
                SelectLevel(_selectedIndex, false);
            }
            else
            {
                ShowEmptyState("Нет доступных уровней.");
            }
        }

        private void ClearCards()
        {
            _cards.Clear();
            ClearChildren(_cardsRoot);
        }

        private void OnCardClicked(int levelIndex)
        {
            if (!IsLevelUnlocked(levelIndex))
                return;

            SelectLevel(levelIndex, true);
        }

        private void SelectLevel(int levelIndex, bool animate)
        {
            var levels = GameRoot.GameConfig != null ? GameRoot.GameConfig.Levels : null;
            if (levels == null || levelIndex < 0 || levelIndex >= levels.Count || !IsLevelUnlocked(levelIndex))
                return;

            _selectedIndex = levelIndex;
            _selectedConfig = levels[levelIndex];
            if (_selectedConfig == null)
            {
                ShowEmptyState("Уровень недоступен.");
                return;
            }

            for (var i = 0; i < _cards.Count; i++)
            {
                _cards[i].SetSelected(_cards[i].Index == levelIndex, _sprites);
            }

            PopulateDetails(_selectedConfig);

            if (animate && _detailsCanvasGroup != null)
            {
                _detailsCanvasGroup.DOKill();
                _detailsPanel.DOKill();
                _detailsCanvasGroup.alpha = 0.78f;
                _detailsPanel.localScale = new Vector3(0.985f, 0.985f, 1f);
                _detailsCanvasGroup.DOFade(1f, 0.18f);
                _detailsPanel.DOScale(Vector3.one, 0.18f).SetEase(Ease.OutQuad);
            }
        }

        private void PopulateDetails(LevelConfig config)
        {
            if (config == null)
            {
                ShowEmptyState("Уровень недоступен.");
                return;
            }

            _emptyStateText.gameObject.SetActive(false);
            _difficultyTitle.gameObject.SetActive(true);
            _descriptionText.gameObject.SetActive(true);
            _availableResourcesRoot.gameObject.SetActive(true);
            _rewardResourcesRoot.gameObject.SetActive(true);
            _startButton.gameObject.SetActive(true);

            _difficultyTitle.text = GetDifficultyTitle(config.Difficulty);
            _descriptionText.text = GetDifficultyDescription(config.Difficulty);

            ClearChildren(_availableResourcesRoot);
            var availableResources = config.AvailableResources;
            if (availableResources != null)
            {
                foreach (var item in availableResources)
                {
                    if (item != null)
                    {
                        CreateResourceSlot(_availableResourcesRoot, item, 0);
                    }
                }
            }

            ClearChildren(_rewardResourcesRoot);
            var rewardResources = config.RewardResources;
            if (rewardResources != null)
            {
                foreach (var reward in rewardResources)
                {
                    if (reward != null && reward.Item != null)
                    {
                        CreateResourceSlot(_rewardResourcesRoot, reward.Item, reward.Amount);
                    }
                }
            }

            var canStart = config.LevelPrefab != null;
            _startButton.interactable = canStart;
            _startButtonLabel.text = canStart ? "НАЧАТЬ" : "НЕТ АРЕНЫ";
            _startButtonLabel.color = canStart
                ? new Color32(250, 247, 232, 255)
                : new Color32(190, 187, 174, 255);
        }

        private void ShowEmptyState(string message)
        {
            _selectedConfig = null;
            _difficultyTitle.gameObject.SetActive(false);
            _descriptionText.gameObject.SetActive(false);
            _availableResourcesRoot.gameObject.SetActive(false);
            _rewardResourcesRoot.gameObject.SetActive(false);
            _startButton.gameObject.SetActive(false);
            _emptyStateText.text = message;
            _emptyStateText.gameObject.SetActive(true);
        }

        private bool IsLevelUnlocked(int levelIndex)
        {
            if (levelIndex < 0)
                return false;

            var progress = GameRoot.PlayerProgress != null ? GameRoot.PlayerProgress.LevelsProgress : null;
            return progress != null ? progress.IsLevelUnlocked(levelIndex) : levelIndex == 0;
        }

        private void StartSelectedLevel()
        {
            if (_selectedConfig == null || _selectedConfig.LevelPrefab == null)
                return;

            _startButton.interactable = false;

            GameRoot.UIManager.CloseAllWindows();

            var loadingWindow = (LoadingWindow)GameRoot.UIManager.OpenWindow<LoadingWindow>();
            LevelController levelController = null;

            loadingWindow.SetProgressValue(0.3f)
                .Then(() => GameRoot.GetManager<UnitySceneLoader>().LoadSceneAsync(Constants.SceneNames.GameplayScene))
                .Then(() =>
                {
                    levelController = FindObjectOfType<LevelController>();
                    if (levelController == null)
                    {
                        throw new InvalidOperationException("LevelController not found in scene after loading gameplay.");
                    }

                    return levelController.InitLevel(_selectedConfig);
                })
                .Then(() => loadingWindow.SetProgressValue(0.8f))
                .Then(() => levelController.GenerateLevel())
                .Then(() => WeatherManager.Instance.ApplyLevelPreset(_selectedConfig.WeatherPreset))
                .Then(() => levelController.GenerateOres())
                .Then(() => loadingWindow.SetProgressValue(0.9f))
                .Then(() => loadingWindow.SetProgressValue(1f))
                .Finally(() =>
                {
                    GameRoot.UIManager.CloseWindow<LoadingWindow>();
                    _startButton.interactable = true;
                });
        }

        private static string GetDifficultyTitle(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy:
                    return "EASY";
                case LevelDifficulty.Meduim:
                    return "MEDIUM";
                case LevelDifficulty.Hard:
                    return "HARD";
                case LevelDifficulty.Insane:
                    return "INSANE";
                default:
                    return difficulty.ToString().ToUpperInvariant();
            }
        }

        private static string GetDifficultyDescription(LevelDifficulty difficulty)
        {
            switch (difficulty)
            {
                case LevelDifficulty.Easy:
                    return "Отличный уровень для новичков.\nРесурсы доступны, враги слабы.";
                case LevelDifficulty.Meduim:
                    return "Сбалансированный уровень для роста.\nДобычи больше, враги уже опаснее.";
                case LevelDifficulty.Hard:
                    return "Испытание для подготовленного игрока.\nРедкие ресурсы и сильные противники.";
                case LevelDifficulty.Insane:
                    return "Максимальный риск и лучшие награды.\nОшибки здесь почти не прощаются.";
                default:
                    return "Выберите уровень и подготовьтесь к забегу.";
            }
        }

        private RectTransform BuildResourceRow(string name, RectTransform parent, Vector2 anchoredPosition)
        {
            var row = CreateRect(name, parent);
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(1f, 1f);
            row.pivot = new Vector2(0f, 1f);
            row.offsetMin = new Vector2(anchoredPosition.x, anchoredPosition.y);
            row.offsetMax = new Vector2(-34f, anchoredPosition.y + 76f);

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return row;
        }

        private void BuildSectionHeader(RectTransform parent, string name, Sprite iconSprite, string labelText, Vector2 anchoredPosition)
        {
            var root = CreateRect(name, parent);
            root.anchorMin = new Vector2(0f, 1f);
            root.anchorMax = new Vector2(1f, 1f);
            root.pivot = new Vector2(0f, 1f);
            root.offsetMin = new Vector2(anchoredPosition.x, anchoredPosition.y);
            root.offsetMax = new Vector2(-34f, anchoredPosition.y + 34f);

            var icon = CreateImage("Icon", root, iconSprite);
            icon.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            icon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            icon.rectTransform.pivot = new Vector2(0f, 0.5f);
            icon.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            icon.rectTransform.sizeDelta = new Vector2(30f, 30f);

            var label = CreateText("Label", root, labelText, 18f, FontStyles.Bold, TextAlignmentOptions.Left);
            label.color = new Color32(176, 174, 166, 255);
            label.enableAutoSizing = true;
            label.fontSizeMin = 12f;
            label.fontSizeMax = 18f;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(40f, 0f);
            label.rectTransform.offsetMax = Vector2.zero;
        }

        private void CreateResourceSlot(RectTransform parent, ItemConfig item, int amount)
        {
            var slot = CreatePanel("ResourceSlot", parent, _sprites.ResourceSlot, out _);
            slot.sizeDelta = new Vector2(76f, 76f);

            var layout = slot.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 76f;
            layout.preferredHeight = 76f;

            if (item.BlockStyleIcon && item is StackableItemConfig stackable && _resourceIconPrefab != null)
            {
                var icon = Instantiate(_resourceIconPrefab, slot);
                icon.SetResource(stackable);

                if (icon.transform is RectTransform iconRect)
                {
                    Stretch(iconRect, 7f, 10f, 7f, 8f);
                }
            }
            else
            {
                var icon = CreateImage("Icon", slot, item.Icon != null ? item.Icon : _sprites.PlaceholderIcon);
                icon.preserveAspect = true;
                Stretch(icon.rectTransform, 13f, 15f, 13f, 12f);
            }

            if (amount > 0)
            {
                var amountText = CreateText("Amount", slot, amount.ToString(), 16f, FontStyles.Bold, TextAlignmentOptions.BottomRight);
                amountText.color = new Color32(246, 244, 231, 255);
                amountText.enableAutoSizing = true;
                amountText.fontSizeMin = 10f;
                amountText.fontSizeMax = 16f;
                Stretch(amountText.rectTransform, 4f, 2f, 7f, 4f);
            }
        }

        private LevelCardView CreateLevelCard(RectTransform parent)
        {
            var button = CreateButton("LevelCard", parent, _sprites.LevelCard, out var background);
            button.transition = Selectable.Transition.None;
            var rect = (RectTransform)button.transform;

            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 118f;
            layout.minHeight = 118f;
            layout.flexibleWidth = 1f;

            var selectionMarker = CreateImage("SelectionMarker", rect, _sprites.SelectorDiamond);
            selectionMarker.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            selectionMarker.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            selectionMarker.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            selectionMarker.rectTransform.anchoredPosition = new Vector2(-18f, 0f);
            selectionMarker.rectTransform.sizeDelta = new Vector2(27f, 31f);

            var icon = CreateImage("Preview", rect, _sprites.PlaceholderPreview);
            icon.rectTransform.anchorMin = Vector2.zero;
            icon.rectTransform.anchorMax = Vector2.one;
            icon.rectTransform.offsetMin = new Vector2(13f, 31f);
            icon.rectTransform.offsetMax = new Vector2(-13f, -12f);
            icon.preserveAspect = false;

            var captionBack = CreatePanel("CaptionBack", rect, _sprites.CaptionPlate, out _);
            captionBack.anchorMin = new Vector2(0.5f, 0f);
            captionBack.anchorMax = new Vector2(0.5f, 0f);
            captionBack.pivot = new Vector2(0.5f, 0f);
            captionBack.anchoredPosition = new Vector2(0f, 8f);
            captionBack.sizeDelta = new Vector2(126f, 28f);

            var caption = CreateText("Caption", captionBack, string.Empty, 17f, FontStyles.Bold, TextAlignmentOptions.Center);
            caption.color = new Color32(248, 247, 232, 255);
            caption.enableAutoSizing = true;
            caption.fontSizeMin = 10f;
            caption.fontSizeMax = 17f;
            Stretch(caption.rectTransform, 6f, 1f, 6f, 2f);

            var lockedRoot = CreateRect("Locked", rect);
            Stretch(lockedRoot);

            var lockIcon = CreateImage("LockIcon", lockedRoot, _sprites.LockIcon);
            lockIcon.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            lockIcon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            lockIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            lockIcon.rectTransform.anchoredPosition = new Vector2(0f, 14f);
            lockIcon.rectTransform.sizeDelta = new Vector2(42f, 48f);

            var lockedLabel = CreateText("LockedLabel", lockedRoot, "УРОВЕНЬ ЗАБЛОКИРОВАН", 10f, FontStyles.Bold, TextAlignmentOptions.Center);
            lockedLabel.color = new Color32(183, 180, 171, 255);
            lockedLabel.enableAutoSizing = true;
            lockedLabel.fontSizeMin = 7f;
            lockedLabel.fontSizeMax = 10f;
            lockedLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
            lockedLabel.rectTransform.anchorMax = new Vector2(1f, 0f);
            lockedLabel.rectTransform.pivot = new Vector2(0.5f, 0f);
            lockedLabel.rectTransform.offsetMin = new Vector2(12f, 11f);
            lockedLabel.rectTransform.offsetMax = new Vector2(-12f, 31f);

            return new LevelCardView(button, background, icon, captionBack.gameObject, caption, lockedRoot.gameObject, selectionMarker);
        }

        private void BuildLantern(RectTransform parent, Vector2 anchoredPosition)
        {
            var lantern = CreateImage("Lantern", parent, _sprites.Lantern);
            lantern.rectTransform.anchorMin = Vector2.zero;
            lantern.rectTransform.anchorMax = Vector2.zero;
            lantern.rectTransform.pivot = new Vector2(0.5f, 0f);
            lantern.rectTransform.anchoredPosition = anchoredPosition;
            lantern.rectTransform.sizeDelta = new Vector2(48f, 62f);
        }

        private void BuildLeafDecoration(RectTransform parent)
        {
            for (var i = 0; i < 12; i++)
            {
                var leaf = CreateImage("Leaf", parent, _sprites.Leaf);
                leaf.rectTransform.anchorMin = new Vector2(0f, 1f);
                leaf.rectTransform.anchorMax = new Vector2(0f, 1f);
                leaf.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                leaf.rectTransform.anchoredPosition = new Vector2(30f + (i % 4) * 18f, -58f - (i / 4) * 18f);
                leaf.rectTransform.sizeDelta = new Vector2(17f, 17f);
            }

            for (var i = 0; i < 10; i++)
            {
                var leaf = CreateImage("Leaf", parent, _sprites.Leaf);
                leaf.rectTransform.anchorMin = new Vector2(1f, 1f);
                leaf.rectTransform.anchorMax = new Vector2(1f, 1f);
                leaf.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                leaf.rectTransform.anchoredPosition = new Vector2(-34f - (i % 3) * 16f, -24f - (i / 3) * 18f);
                leaf.rectTransform.sizeDelta = new Vector2(16f, 16f);
            }
        }

        private static Image EnsureImage(GameObject target)
        {
            var image = target.GetComponent<Image>();
            if (image == null)
            {
                image = target.AddComponent<Image>();
            }

            return image;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Sprite sprite, out Image image)
        {
            var rect = CreateRect(name, parent);
            image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private static Button CreateButton(string name, Transform parent, Sprite sprite, out Image image)
        {
            var rect = CreatePanel(name, parent, sprite, out image);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            return button;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = new Color32(245, 238, 218, 255);
            label.raycastTarget = false;
            label.enableWordWrapping = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            SetLayerRecursively(gameObject, UiLayer);
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.localPosition = Vector3.zero;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0f);
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            Stretch(rect, padding, padding, padding, padding);
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null)
                return;

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                child.gameObject.SetActive(false);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;

            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private sealed class LevelCardView
        {
            private readonly Button _button;
            private readonly Image _background;
            private readonly Image _preview;
            private readonly GameObject _captionBack;
            private readonly TextMeshProUGUI _caption;
            private readonly GameObject _lockedRoot;
            private readonly Image _selectionMarker;
            private bool _unlocked;

            public int Index { get; private set; }

            public LevelCardView(Button button, Image background, Image preview, GameObject captionBack, TextMeshProUGUI caption, GameObject lockedRoot, Image selectionMarker)
            {
                _button = button;
                _background = background;
                _preview = preview;
                _captionBack = captionBack;
                _caption = caption;
                _lockedRoot = lockedRoot;
                _selectionMarker = selectionMarker;
                Index = -1;
            }

            public void Setup(int index, LevelConfig config, bool unlocked, bool selected, SpriteSet sprites, Action<int> clicked)
            {
                Index = index;
                _unlocked = unlocked;
                _button.onClick.RemoveAllListeners();

                if (_unlocked)
                {
                    _button.interactable = true;
                    _button.onClick.AddListener(() => clicked(index));
                    _preview.sprite = config.LevelIcon != null ? config.LevelIcon : sprites.PlaceholderPreview;
                    _preview.color = Color.white;
                    _caption.text = GetDifficultyTitle(config.Difficulty);
                    _captionBack.SetActive(true);
                    _lockedRoot.SetActive(false);
                }
                else
                {
                    _button.interactable = false;
                    _preview.sprite = sprites.PlaceholderPreview;
                    _preview.color = new Color32(38, 38, 38, 210);
                    _caption.text = string.Empty;
                    _captionBack.SetActive(false);
                    _lockedRoot.SetActive(true);
                }

                SetSelected(selected, sprites);
            }

            public void SetSelected(bool selected, SpriteSet sprites)
            {
                _selectionMarker.gameObject.SetActive(selected && _unlocked);
                _background.sprite = !_unlocked ? sprites.LevelCardLocked : selected ? sprites.LevelCardSelected : sprites.LevelCard;
            }
        }

        private sealed class SpriteSet
        {
            public Sprite Overlay { get; private set; }
            public Sprite Sidebar { get; private set; }
            public Sprite TitlePlate { get; private set; }
            public Sprite DetailPanel { get; private set; }
            public Sprite LevelCard { get; private set; }
            public Sprite LevelCardSelected { get; private set; }
            public Sprite LevelCardLocked { get; private set; }
            public Sprite CaptionPlate { get; private set; }
            public Sprite ResourceSlot { get; private set; }
            public Sprite StartButton { get; private set; }
            public Sprite CloseButton { get; private set; }
            public Sprite LockIcon { get; private set; }
            public Sprite SelectorDiamond { get; private set; }
            public Sprite Lantern { get; private set; }
            public Sprite Leaf { get; private set; }
            public Sprite PickaxeIcon { get; private set; }
            public Sprite ChestIcon { get; private set; }
            public Sprite PlaceholderPreview { get; private set; }
            public Sprite PlaceholderIcon { get; private set; }

            public static SpriteSet Create()
            {
                return new SpriteSet
                {
                    Overlay = CreateFlatSprite("level_select_overlay", new Color32(0, 0, 0, 128), 4, Vector4.zero),
                    Sidebar = CreateFrameSprite("level_select_sidebar", 64, 64, new Color32(54, 43, 30, 245), new Color32(43, 42, 39, 255), new Color32(127, 116, 86, 255), new Color32(20, 18, 16, 255), 8, new Vector4(16, 16, 16, 16), true),
                    TitlePlate = CreateFrameSprite("level_select_title_plate", 96, 32, new Color32(83, 62, 40, 255), new Color32(31, 22, 14, 255), new Color32(143, 106, 58, 255), new Color32(20, 14, 9, 255), 4, new Vector4(12, 10, 12, 10), true),
                    DetailPanel = CreateFrameSprite("level_select_detail_panel", 64, 64, new Color32(16, 22, 13, 172), new Color32(126, 114, 76, 130), new Color32(186, 168, 109, 118), new Color32(28, 21, 13, 170), 3, new Vector4(12, 12, 12, 12), false),
                    LevelCard = CreateFrameSprite("level_select_card", 96, 56, new Color32(52, 48, 39, 245), new Color32(42, 29, 18, 255), new Color32(184, 166, 70, 255), new Color32(20, 15, 11, 255), 4, new Vector4(14, 14, 14, 14), true),
                    LevelCardSelected = CreateFrameSprite("level_select_card_selected", 96, 56, new Color32(62, 70, 34, 248), new Color32(64, 94, 24, 255), new Color32(222, 236, 90, 255), new Color32(28, 45, 14, 255), 4, new Vector4(14, 14, 14, 14), true),
                    LevelCardLocked = CreateFrameSprite("level_select_card_locked", 96, 56, new Color32(28, 29, 28, 245), new Color32(38, 34, 30, 255), new Color32(81, 76, 66, 255), new Color32(12, 12, 11, 255), 4, new Vector4(14, 14, 14, 14), true),
                    CaptionPlate = CreateFrameSprite("level_select_caption", 64, 24, new Color32(91, 142, 46, 255), new Color32(28, 55, 18, 255), new Color32(155, 205, 72, 255), new Color32(42, 76, 24, 255), 3, new Vector4(8, 6, 8, 6), true),
                    ResourceSlot = CreateFrameSprite("level_select_resource_slot", 40, 40, new Color32(39, 42, 35, 205), new Color32(93, 87, 65, 190), new Color32(138, 127, 90, 170), new Color32(18, 16, 13, 210), 3, new Vector4(8, 8, 8, 8), false),
                    StartButton = CreateFrameSprite("level_select_start_button", 96, 32, new Color32(94, 141, 37, 255), new Color32(71, 45, 22, 255), new Color32(177, 201, 72, 255), new Color32(38, 77, 23, 255), 4, new Vector4(14, 10, 14, 10), true),
                    CloseButton = CreateCloseButtonSprite(),
                    LockIcon = CreateLockIcon(),
                    SelectorDiamond = CreateDiamondSprite("level_select_selector", new Color32(147, 226, 49, 255), new Color32(44, 85, 20, 255)),
                    Lantern = CreateLanternSprite(),
                    Leaf = CreateDiamondSprite("level_select_leaf", new Color32(74, 151, 29, 255), new Color32(24, 75, 14, 255)),
                    PickaxeIcon = CreatePickaxeIcon(),
                    ChestIcon = CreateChestIcon(),
                    PlaceholderPreview = CreatePlaceholderPreview(),
                    PlaceholderIcon = CreatePlaceholderIcon()
                };
            }

            private static Sprite CreateFlatSprite(string name, Color32 color, int size, Vector4 border)
            {
                var texture = CreateTexture(size, size, new Color32(0, 0, 0, 0));
                FillRect(texture, 0, 0, size, size, color);
                texture.Apply();
                return CreateSprite(name, texture, border);
            }

            private static Sprite CreateFrameSprite(string name, int width, int height, Color32 fill, Color32 border, Color32 highlight, Color32 shadow, int borderSize, Vector4 spriteBorder, bool noisyFill)
            {
                var texture = CreateTexture(width, height, new Color32(0, 0, 0, 0));

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var color = fill;
                        if (noisyFill)
                        {
                            color = AddNoise(fill, x, y, 10);
                        }

                        if (x < borderSize || x >= width - borderSize || y < borderSize || y >= height - borderSize)
                        {
                            color = border;
                        }
                        else if (x < borderSize * 2 || y >= height - borderSize * 2)
                        {
                            color = highlight;
                        }
                        else if (x >= width - borderSize * 2 || y < borderSize * 2)
                        {
                            color = shadow;
                        }

                        texture.SetPixel(x, y, color);
                    }
                }

                DrawCornerCaps(texture, width, height, borderSize, shadow);
                texture.Apply();
                return CreateSprite(name, texture, spriteBorder);
            }

            private static Sprite CreateCloseButtonSprite()
            {
                var texture = CreateTexture(40, 40, new Color32(0, 0, 0, 0));
                DrawFrame(texture, 40, 40, new Color32(92, 66, 45, 255), new Color32(36, 24, 16, 255), new Color32(151, 107, 66, 255), new Color32(22, 15, 10, 255), 4);

                for (var i = 0; i < 16; i++)
                {
                    DrawBlock(texture, 12 + i, 12 + i, 4, 4, new Color32(247, 76, 58, 255));
                    DrawBlock(texture, 27 - i, 12 + i, 4, 4, new Color32(247, 76, 58, 255));
                }

                texture.Apply();
                return CreateSprite("level_select_close_button", texture, new Vector4(10, 10, 10, 10));
            }

            private static Sprite CreateLockIcon()
            {
                var texture = CreateTexture(32, 32, new Color32(0, 0, 0, 0));
                DrawRect(texture, 9, 6, 14, 14, new Color32(162, 162, 166, 255));
                DrawRect(texture, 11, 8, 10, 10, new Color32(218, 218, 222, 255));
                DrawRect(texture, 10, 17, 12, 4, new Color32(218, 218, 222, 255));
                DrawRect(texture, 12, 20, 8, 3, new Color32(58, 58, 62, 255));
                DrawRect(texture, 13, 21, 6, 3, new Color32(0, 0, 0, 0));
                DrawRect(texture, 14, 11, 4, 5, new Color32(36, 36, 39, 255));
                DrawRect(texture, 15, 8, 2, 4, new Color32(36, 36, 39, 255));
                texture.Apply();
                return CreateSprite("level_select_lock", texture, Vector4.zero);
            }

            private static Sprite CreateDiamondSprite(string name, Color32 fill, Color32 outline)
            {
                const int size = 24;
                var texture = CreateTexture(size, size, new Color32(0, 0, 0, 0));
                var center = size / 2;

                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var distance = Mathf.Abs(x - center) + Mathf.Abs(y - center);
                        if (distance <= center - 1)
                        {
                            texture.SetPixel(x, y, distance >= center - 3 ? outline : fill);
                        }
                    }
                }

                texture.Apply();
                return CreateSprite(name, texture, Vector4.zero);
            }

            private static Sprite CreateLanternSprite()
            {
                var texture = CreateTexture(32, 40, new Color32(0, 0, 0, 0));
                DrawRect(texture, 10, 3, 12, 4, new Color32(72, 47, 25, 255));
                DrawRect(texture, 8, 7, 16, 21, new Color32(79, 52, 30, 255));
                DrawRect(texture, 11, 10, 10, 15, new Color32(255, 172, 44, 255));
                DrawRect(texture, 13, 12, 6, 10, new Color32(255, 226, 111, 255));
                DrawRect(texture, 6, 28, 20, 5, new Color32(58, 41, 26, 255));
                DrawRect(texture, 11, 33, 10, 3, new Color32(98, 70, 38, 255));
                DrawRect(texture, 15, 34, 2, 4, new Color32(98, 70, 38, 255));
                texture.Apply();
                return CreateSprite("level_select_lantern", texture, Vector4.zero);
            }

            private static Sprite CreatePickaxeIcon()
            {
                var texture = CreateTexture(32, 32, new Color32(0, 0, 0, 0));
                for (var i = 0; i < 18; i++)
                {
                    DrawBlock(texture, 8 + i, 6 + i, 2, 2, new Color32(119, 82, 45, 255));
                }

                DrawRect(texture, 7, 22, 17, 3, new Color32(188, 190, 184, 255));
                DrawRect(texture, 5, 20, 7, 3, new Color32(219, 221, 215, 255));
                DrawRect(texture, 22, 19, 4, 6, new Color32(144, 146, 141, 255));
                texture.Apply();
                return CreateSprite("level_select_pickaxe", texture, Vector4.zero);
            }

            private static Sprite CreateChestIcon()
            {
                var texture = CreateTexture(32, 32, new Color32(0, 0, 0, 0));
                DrawRect(texture, 5, 9, 22, 15, new Color32(142, 91, 38, 255));
                DrawRect(texture, 5, 20, 22, 4, new Color32(103, 64, 28, 255));
                DrawRect(texture, 4, 8, 24, 3, new Color32(58, 40, 25, 255));
                DrawRect(texture, 14, 12, 4, 8, new Color32(220, 152, 50, 255));
                DrawRect(texture, 13, 15, 6, 4, new Color32(63, 48, 33, 255));
                texture.Apply();
                return CreateSprite("level_select_chest", texture, Vector4.zero);
            }

            private static Sprite CreatePlaceholderPreview()
            {
                var texture = CreateTexture(64, 40, new Color32(99, 156, 204, 255));
                DrawRect(texture, 0, 0, 64, 12, new Color32(84, 122, 53, 255));
                DrawRect(texture, 0, 0, 64, 6, new Color32(112, 90, 48, 255));
                DrawRect(texture, 8, 10, 14, 16, new Color32(126, 78, 34, 255));
                DrawRect(texture, 10, 12, 10, 10, new Color32(188, 128, 51, 255));
                DrawRect(texture, 42, 11, 12, 20, new Color32(35, 34, 35, 255));
                DrawRect(texture, 45, 22, 6, 4, new Color32(121, 38, 168, 255));
                DrawRect(texture, 27, 9, 8, 11, new Color32(80, 149, 35, 255));
                DrawRect(texture, 30, 14, 2, 2, new Color32(26, 48, 20, 255));
                DrawRect(texture, 34, 14, 2, 2, new Color32(26, 48, 20, 255));
                DrawRect(texture, 32, 10, 2, 2, new Color32(26, 48, 20, 255));
                texture.Apply();
                return CreateSprite("level_select_placeholder_preview", texture, Vector4.zero);
            }

            private static Sprite CreatePlaceholderIcon()
            {
                var texture = CreateTexture(24, 24, new Color32(0, 0, 0, 0));
                DrawRect(texture, 5, 5, 14, 14, new Color32(111, 105, 86, 255));
                DrawRect(texture, 5, 17, 14, 2, new Color32(158, 147, 111, 255));
                DrawRect(texture, 17, 5, 2, 14, new Color32(56, 53, 45, 255));
                DrawRect(texture, 5, 5, 14, 2, new Color32(25, 23, 20, 255));
                texture.Apply();
                return CreateSprite("level_select_placeholder_icon", texture, Vector4.zero);
            }

            private static Texture2D CreateTexture(int width, int height, Color32 clear)
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.hideFlags = HideFlags.HideAndDontSave;

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        texture.SetPixel(x, y, clear);
                    }
                }

                return texture;
            }

            private static Sprite CreateSprite(string name, Texture2D texture, Vector4 border)
            {
                texture.name = name;
                var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                sprite.name = name;
                sprite.hideFlags = HideFlags.HideAndDontSave;
                return sprite;
            }

            private static void DrawFrame(Texture2D texture, int width, int height, Color32 fill, Color32 border, Color32 highlight, Color32 shadow, int borderSize)
            {
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        var color = fill;
                        if (x < borderSize || x >= width - borderSize || y < borderSize || y >= height - borderSize)
                        {
                            color = border;
                        }
                        else if (x < borderSize * 2 || y >= height - borderSize * 2)
                        {
                            color = highlight;
                        }
                        else if (x >= width - borderSize * 2 || y < borderSize * 2)
                        {
                            color = shadow;
                        }

                        texture.SetPixel(x, y, color);
                    }
                }
            }

            private static void DrawCornerCaps(Texture2D texture, int width, int height, int borderSize, Color32 color)
            {
                var cap = Mathf.Max(2, borderSize);
                DrawRect(texture, 0, 0, cap, cap, color);
                DrawRect(texture, width - cap, 0, cap, cap, color);
                DrawRect(texture, 0, height - cap, cap, cap, color);
                DrawRect(texture, width - cap, height - cap, cap, cap, color);
            }

            private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color32 color)
            {
                DrawRect(texture, x, y, width, height, color);
            }

            private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color32 color)
            {
                for (var py = y; py < y + height; py++)
                {
                    for (var px = x; px < x + width; px++)
                    {
                        if (px >= 0 && px < texture.width && py >= 0 && py < texture.height)
                        {
                            texture.SetPixel(px, py, color);
                        }
                    }
                }
            }

            private static void DrawBlock(Texture2D texture, int x, int y, int width, int height, Color32 color)
            {
                DrawRect(texture, x, y, width, height, color);
            }

            private static Color32 AddNoise(Color32 color, int x, int y, int amount)
            {
                var delta = ((x * 17 + y * 31) & 7) - 3;
                delta *= amount / 4;

                return new Color32(
                    (byte)Mathf.Clamp(color.r + delta, 0, 255),
                    (byte)Mathf.Clamp(color.g + delta, 0, 255),
                    (byte)Mathf.Clamp(color.b + delta, 0, 255),
                    color.a);
            }
        }
    }
}
