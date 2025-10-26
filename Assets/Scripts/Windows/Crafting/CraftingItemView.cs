using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows.Crafting
{
    public class CraftingItemView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _icon;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private GameObject _lockedState;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GameObject _selectionHighlight;

        private Action _onClick;

        public void Setup(Sprite icon, string displayName, Action onClick)
        {
            if (_icon != null)
            {
                _icon.sprite = icon;
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
        }

        public void SetLocked(bool isLocked)
        {
            if (_button != null)
            {
                _button.interactable = !isLocked;
            }

            if (_lockedState != null)
            {
                _lockedState.SetActive(isLocked);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = isLocked ? 0.4f : 1f;
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectionHighlight != null)
            {
                _selectionHighlight.SetActive(isSelected);
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
