using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Achievements;
using Devotion.SDK.Services.Localization;
using DG.Tweening;
using MineArena.Basics;
using MineArena.Game.UI;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using TMPro;
using UnityEngine;

namespace UI.UIAchievement
{
    public class AchievementPopup : MonoBehaviour,
        IProgressBar,
        IMessageSubscriber<AchievementMessages.AchievementBegun>,
        IMessageSubscriber<AchievementMessages.PrizeTake>
    {
        [SerializeField] private TextMeshProUGUI _nameQuest;
        [SerializeField] private TextMeshProUGUI _messageTakePrize;
        [SerializeField] private ProgressPopupQuestBar _progressBarQuest;

        private const string TextMessageTakePrizeKey = "[Achievement]TakePrize";

        private readonly Queue<Achievement> _messageQueue = new();

        private RectTransform _rectTransform;
        private Achievement _achievement;
        private bool _isAnimating;

        public event Action<float, float> OnValueChanged;

        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }

        private void Awake() =>
            _rectTransform = GetComponent<RectTransform>();

        public void OnMessage(AchievementMessages.PrizeTake message)
        {
            Activation(message.Model);
        }

        public void OnMessage(AchievementMessages.AchievementBegun message)
        {
            Activation(message.Model);
        }

        private void Activation(Achievement achievement)
        {
            _messageQueue.Enqueue(achievement);

            if (!_isAnimating)
                ProcessQueue();
        }

        private async void ProcessQueue()
        {
            _isAnimating = true;

            while (_messageQueue.Count != 0)
            {
                Achievement achievement = _messageQueue.Dequeue();

                if (!achievement.CanTakePrize)
                    ConstructProgress(achievement);
                else
                    ConstructCompletion(achievement);

                await ShowAnimation();
            }

            _isAnimating = false;
        }

        private void ConstructCompletion(Achievement achievement)
        {
            _progressBarQuest.gameObject.SetActive(false);
            _messageTakePrize.gameObject.SetActive(true);
            _nameQuest.text = LocalizationService.GetLocalizedText(achievement.Data.NameAchievementKey);
            _messageTakePrize.text = LocalizationService.GetLocalizedText(TextMessageTakePrizeKey);
        }

        private void ConstructProgress(Achievement achievement)
        {
            _messageTakePrize.gameObject.SetActive(false);
            _progressBarQuest.gameObject.SetActive(true);
            MaxValue = achievement.MaxValueProgress;
            CurrentValue = achievement.CurrentValueProgress;
            _nameQuest.text = LocalizationService.GetLocalizedText(achievement.Data.NameAchievementKey);
            OnValueChanged?.Invoke(CurrentValue, MaxValue);
        }

        private async Task ShowAnimation()
        {
            var sequence = DOTween.Sequence()
                .Append(transform
                    .DOMove(_rectTransform.position + new Vector3(0, -100, 0), Constants.QuestPopup.Duration)
                    .SetEase(Ease.Linear))
                .AppendInterval(Constants.QuestPopup.Timer)
                .Append(transform.DOMove(_rectTransform.position, Constants.QuestPopup.Duration).SetEase(Ease.Linear));

            await sequence.AsyncWaitForCompletion();
        }

        private void OnEnable() =>
            MessageService.Subscribe(this);

        private void OnDisable() =>
            MessageService.Unsubscribe(this);
    }
}