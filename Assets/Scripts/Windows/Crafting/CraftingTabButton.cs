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

        public void Setup(string title, Action onClick)
        {
            if (_label != null)
            {
                _label.text = title;
            }

            _onClick = onClick;

            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(HandleClick);
            }

            SetSelected(false);
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
