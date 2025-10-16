using TMPro;
using UnityEngine;
using Devotion.SDK.Services.Localization;
using MineArena.Messages.MessageService;

namespace MineArena.SDK.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedTextMeshPro : MonoBehaviour,
        IMessageSubscriber<Messages.Game.LanguageChanged>
    {
        [SerializeField] private string _localizationKey;

        private TextMeshProUGUI _textMeshPro;

        private void Awake()
        {
            _textMeshPro = GetComponent<TextMeshProUGUI>();
        }

        private void Start()
        {
            UpdateLocalizedText();
        }

        private void OnEnable()
        {
            MessageService.Subscribe(this);
        }

        private void OnDisable()
        {
            MessageService.Unsubscribe(this);
        }

        private void UpdateLocalizedText()
        {
            _textMeshPro.text = LocalizationService.GetLocalizedText(_localizationKey);
        }

        public void OnMessage(Messages.Game.LanguageChanged message)
        {
            UpdateLocalizedText();
        }
    }
}
