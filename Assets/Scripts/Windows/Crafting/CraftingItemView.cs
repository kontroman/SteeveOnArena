using System;
using MineArena.Items;
using MineArena.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows.Crafting
{
    public class CraftingItemView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private ResourceIcon _blockIcon;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _meta;
        [SerializeField] private GameObject _lockedState;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _selectionHighlight;

        private Action _onClick;

        public Button Button => _button;
        public TextMeshProUGUI NameLabel => _name;
        public TextMeshProUGUI MetaLabel => _meta;

        public void Setup(Sprite icon, string displayName, Action onClick)
        {
            if (_icon != null)
            {
                _icon.sprite = icon;
                _icon.gameObject.SetActive(true);
            }

            if (_blockIcon != null)
            {
                _blockIcon.gameObject.SetActive(false);
            }

            if (_name != null)
            {
                _name.text = displayName;
            }

            _onClick = onClick;

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(HandleClick);
            }

            SetSelected(false);
            SetUnavailable(false);
        }

        public void Setup(ItemConfig item, Sprite fallbackIcon, string displayName, Action onClick)
        {
            SetupIcon(item, fallbackIcon);

            if (_name != null)
            {
                _name.text = displayName;
            }

            _onClick = onClick;

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(HandleClick);
            }

            SetSelected(false);
            SetUnavailable(false);
        }

        public void SetLocked(bool isLocked)
        {
            if (_button != null)
            {
                _button.interactable = !isLocked;
            }

            SetUnavailable(isLocked);
        }

        public void SetUnavailable(bool isUnavailable)
        {
            if (_lockedState != null)
            {
                _lockedState.SetActive(isUnavailable);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = isUnavailable ? 0.48f : 1f;
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectionHighlight != null)
            {
                _selectionHighlight.SetActive(isSelected);
            }
        }

        public void SetNameColor(Color color)
        {
            if (_name != null)
            {
                _name.color = color;
            }
        }

        public void SetMeta(string text, Color color)
        {
            if (_meta != null)
            {
                _meta.text = text;
                _meta.color = color;
                _meta.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
            }
        }

        private void SetupIcon(ItemConfig item, Sprite fallbackIcon)
        {
            if (item != null && item.BlockStyleIcon && item is StackableItemConfig stackableItem && _blockIcon != null)
            {
                _blockIcon.gameObject.SetActive(true);
                _blockIcon.SetResource(stackableItem);

                if (_icon != null)
                {
                    _icon.sprite = null;
                    _icon.gameObject.SetActive(false);
                }

                return;
            }

            if (_blockIcon != null)
            {
                _blockIcon.gameObject.SetActive(false);
            }

            if (_icon != null)
            {
                _icon.sprite = item != null && item.Icon != null ? item.Icon : fallbackIcon;
                _icon.gameObject.SetActive(true);
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
