using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using MineArena.Basics;
using MineArena.Game.UI;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using Quests;
using TMPro;
using UnityEngine;


namespace UI.UIQuest
{
    public class QuestPopup : MonoBehaviour,
        IProgressBar,
        IMessageSubscriber<QuestMessages.QuestBegun>,
        IMessageSubscriber<QuestMessages.PrizeTake>
    {
        [SerializeField] private TextMeshProUGUI _nameQuest;
        [SerializeField] private TextMeshProUGUI _messageTakePrize;
        [SerializeField] private ProgressPopupQuestBar _progressBarQuest;

        private const string TextMessageTakePrize = "The quest is complete. Collect your reward.";

        private readonly Queue<Quest> _messageQueue = new();

        private RectTransform _rectTransform;
        private Quest _quest;
        private bool _isAnimating;

        public event Action<float, float> OnValueChanged;

        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }

        private void Awake() =>
            _rectTransform = GetComponent<RectTransform>();

        public void OnMessage(QuestMessages.PrizeTake message)
        {
            Activation(message.Model);
        }

        public void OnMessage(QuestMessages.QuestBegun message)
        {
            Activation(message.Model);
        }

        private void Activation(Quest quest)
        {
            _messageQueue.Enqueue(quest);

            if (!_isAnimating)
                ProcessQueue();
        }

        private async void ProcessQueue()
        {
            _isAnimating = true;

            while (_messageQueue.Count != 0)
            {
                Quest quest = _messageQueue.Dequeue();
                
                if (!quest.CanTakePrize)
                    ConstructProgress(quest);
                else
                    ConstructCompletion(quest);

                await ShowAnimation();
            }

            _isAnimating = false;
        }

        private void ConstructCompletion(Quest quest)
        {
            _progressBarQuest.gameObject.SetActive(false);
            _messageTakePrize.gameObject.SetActive(true);
            _nameQuest.text = quest.Data.NameQuest;
            _messageTakePrize.text = TextMessageTakePrize;
        }

        private void ConstructProgress(Quest quest)
        {
            _messageTakePrize.gameObject.SetActive(false);
            _progressBarQuest.gameObject.SetActive(true);
            MaxValue = quest.MaxValueProgress;
            CurrentValue = quest.CurrentValueProgress;
            _nameQuest.text = quest.Data.NameQuest;
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