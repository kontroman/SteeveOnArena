using System;
using System.Collections.Generic;
using Devotion.SDK.Base;
using MineArena.Items;
using MineArena.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows
{
    public class LevelCompleteWindow : BaseWindow
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private Transform _resourcesRoot;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _doubleRewardsButton;

        private Action _continueClicked;
        private Action _doubleRewardsClicked;
        private bool _clicked;

        private void Awake()
        {
            EnsureRuntimeLayout();
            _continueButton.onClick.AddListener(HandleContinueClicked);
            _doubleRewardsButton.onClick.AddListener(HandleDoubleRewardsClicked);
        }

        public void Setup(IReadOnlyDictionary<ItemConfig, int> rewards, Action continueClicked, Action doubleRewardsClicked, bool canDoubleRewards)
        {
            EnsureRuntimeLayout();
            _clicked = false;
            _continueClicked = continueClicked;
            _doubleRewardsClicked = doubleRewardsClicked;

            _continueButton.interactable = true;
            _doubleRewardsButton.interactable = canDoubleRewards;

            RefreshRewards(rewards);
        }

        private void HandleContinueClicked()
        {
            if (_clicked)
                return;

            _clicked = true;
            SetButtonsInteractable(false);
            _continueClicked?.Invoke();
        }

        private void HandleDoubleRewardsClicked()
        {
            if (_clicked)
                return;

            _clicked = true;
            SetButtonsInteractable(false);
            _doubleRewardsClicked?.Invoke();
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_continueButton != null)
                _continueButton.interactable = interactable;

            if (_doubleRewardsButton != null)
                _doubleRewardsButton.interactable = interactable;
        }

        private void RefreshRewards(IReadOnlyDictionary<ItemConfig, int> rewards)
        {
            for (int i = _resourcesRoot.childCount - 1; i >= 0; i--)
                Destroy(_resourcesRoot.GetChild(i).gameObject);

            if (rewards == null || rewards.Count == 0)
            {
                CreateRewardRow("No resources", 0, null);
                return;
            }

            foreach (var reward in rewards)
            {
                if (reward.Key == null || reward.Value <= 0)
                    continue;

                CreateRewardRow(reward.Key.Name, reward.Value, reward.Key.Icon);
            }
        }

        private void CreateRewardRow(string itemName, int amount, Sprite icon)
        {
            var row = new GameObject("RewardRow").AddComponent<RectTransform>();
            row.SetParent(_resourcesRoot, false);
            row.sizeDelta = new Vector2(0f, 42f);

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = false;

            var iconRect = new GameObject("Icon").AddComponent<RectTransform>();
            iconRect.SetParent(row, false);
            iconRect.sizeDelta = new Vector2(36f, 36f);
            var iconImage = iconRect.gameObject.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.2f);

            var label = new GameObject("Label").AddComponent<TextMeshProUGUI>();
            label.transform.SetParent(row, false);
            label.text = amount > 0 ? $"{itemName} x{amount}" : itemName;
            label.fontSize = 22;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private void EnsureRuntimeLayout()
        {
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                rectTransform = gameObject.AddComponent<RectTransform>();

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            if (GetComponent<Image>() == null)
            {
                var image = gameObject.AddComponent<Image>();
                image.color = new Color(0f, 0f, 0f, 0.7f);
            }

            if (_titleText == null)
                _titleText = CreateText(rectTransform, "Level Complete", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(420f, 48f), 34);

            if (_resourcesRoot == null)
            {
                var resourcesRect = CreateRect("Resources", rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(420f, 260f));
                var layout = resourcesRect.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.childControlHeight = false;
                layout.childControlWidth = true;
                _resourcesRoot = resourcesRect;
            }

            if (_continueButton == null)
                _continueButton = CreateButton(rectTransform, "Continue", new Vector2(-110f, -90f));

            if (_doubleRewardsButton == null)
                _doubleRewardsButton = CreateButton(rectTransform, "Double Rewards", new Vector2(110f, -90f));
        }

        private static Button CreateButton(RectTransform parent, string label, Vector2 anchoredPosition)
        {
            var rect = CreateRect(label, parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), anchoredPosition, new Vector2(190f, 48f));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = new Color(0.18f, 0.44f, 0.9f, 1f);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var text = CreateText(rect, label, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 20);
            text.alignment = TextAlignmentOptions.Center;
            return button;
        }

        private static TextMeshProUGUI CreateText(RectTransform parent, string value, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, int fontSize)
        {
            var rect = CreateRect("Text", parent, anchorMin, anchorMax, anchoredPosition, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return text;
        }

        private static RectTransform CreateRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = new GameObject(name).AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
