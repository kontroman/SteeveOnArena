using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows.Crafting
{
    public class CraftingTabButton : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _selectedVisual;

        private Action _onClick;

        public Button Button => _button;

        public void Setup(string title, Action onClick)
        {
            if (_label != null)
            {
                _label.text = title;
                ConfigureLabel(_label, (RectTransform)transform);
            }

            _onClick = onClick;

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(HandleClick);
            }

            SetSelected(false);
        }

        public static void ConfigureLabel(TextMeshProUGUI label, RectTransform buttonRect)
        {
            const float horizontalPadding = 12f;
            label.enableAutoSizing = false;
            label.fontSize = 16f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(horizontalPadding, 8f);
            label.rectTransform.offsetMax = new Vector2(-horizontalPadding, -8f);
            buttonRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
                Mathf.Max(156f, Mathf.Ceil(label.GetPreferredValues(label.text).x) + horizontalPadding * 2f));
        }

        public void SetSelected(bool selected)
        {
            if (_selectedVisual != null)
            {
                _selectedVisual.SetActive(selected);
            }
        }

        private void HandleClick()
        {
            _onClick?.Invoke();
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClick);
            }
        }
    }
}
