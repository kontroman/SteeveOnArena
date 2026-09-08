using System;
using Devotion.SDK.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows
{
    public class DeathWindow : BaseWindow
    {
        [SerializeField] private Button _reviveButton;
        [SerializeField] private Button _villageButton;
        [SerializeField] private TextMeshProUGUI _statusText;
        private Action _decline;
        public Button ReviveButton => _reviveButton;
        public Button VillageButton => _villageButton;
        public TextMeshProUGUI StatusText => _statusText;

        public void Setup(Action revive, Action decline)
        {
            _decline = decline;
            _reviveButton.onClick.RemoveAllListeners();
            _villageButton.onClick.RemoveAllListeners();
            _reviveButton.onClick.AddListener(() => revive());
            _villageButton.onClick.AddListener(() => decline());
        }

        public override void CloseWindow() => _decline?.Invoke();
    }
}
