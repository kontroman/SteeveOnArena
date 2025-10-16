using System;
using Devotion.SDK.Controllers;
using Devotion.SDK.Services.Localization;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using TMPro;
using UnityEngine;

namespace UI.UIAchievement
{
    public class AchievementButton : MonoBehaviour,
        IMessageSubscriber<AchievementMessages.PrizeTake>,
        IMessageSubscriber<AchievementMessages.AchievementCompleted>
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private string _nameKye;

        private readonly int _addValue = 1;
        private readonly int _subtractValue = -1;

        private int _startValue;
        private int _valueAchievementWithPrize;

        private void Awake()
        {
            _name.text = LocalizationService.GetLocalizedText(_nameKye);
        }

        private void Start()
        {
            _startValue = 0;
            LoadData();
            SetValue(_startValue);
        }

        public void OnMessage(AchievementMessages.PrizeTake message) =>
            SetValue(_addValue);

        public void OnMessage(AchievementMessages.AchievementCompleted message) =>
            SetValue(_subtractValue);

        private void LoadData()
        {
            foreach (var (key, data) in GameRoot.PlayerProgress.AchievementProgress.Achievements)
            {
                if (data.CanTakePrize && !data.IsCompleted)
                    _startValue++;
            }
        }

        private void SetValue(int value)
        {
            _valueAchievementWithPrize += value;
            _text.text = _valueAchievementWithPrize.ToString();
        }

        private void OnEnable() =>
            MessageService.Subscribe(this);

        private void OnDisable() =>
            MessageService.Unsubscribe(this);
    }
}