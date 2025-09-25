using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using MineArena.Basics;
using MineArena.Game.UI;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using TMPro;
using UnityEngine;

namespace UI.Quest
{
    public class QuestPopup : MonoBehaviour,
        IProgressBar,
        IMessageSubscriber<QuestMessages.QuestBegun>
    {
        [SerializeField] private TextMeshProUGUI _nameQuest;
        [SerializeField] private ProgressPopupQuestBar _progressBarQuest;
        
        private readonly Queue<global::Quest.Quest> _messageQueue = new();

        private RectTransform _rectTransform;
        private global::Quest.Quest _quest;
        private bool _isAnimating;

        public event Action<float, float> OnValueChanged;

        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }

        private void Awake() =>
            _rectTransform = GetComponent<RectTransform>();

        public void OnMessage(QuestMessages.QuestBegun message)
        {
            _messageQueue.Enqueue(message.Model);

            if (!_isAnimating)
                ProcessQueue();
        }

        private async void ProcessQueue()
        {
            _isAnimating = true;

            while (_messageQueue.Count != 0)
            {
                var mes = _messageQueue.Dequeue();
                Construct(mes);
                await ShowAnimation();
            }

            _isAnimating = false;
        }

        private void Construct(global::Quest.Quest message)
        {
            MaxValue = message.MaxValueProgress;
            CurrentValue = message.CurrentValueProgress;
            _nameQuest.text = message.Data.NameQuest;
            OnValueChanged?.Invoke(CurrentValue, MaxValue);
        }

        private async Task ShowAnimation()
        {
            var sequence = DOTween.Sequence()
                .Append(transform.DOMove(_rectTransform.position + new Vector3(0, -100, 0), Constants.QuestPopup.Duration)
                    .SetEase(Ease.Linear))
                .AppendInterval(Constants.QuestPopup.Timer)
                .Append(transform.DOMove(_rectTransform.position, Constants.QuestPopup.Duration).SetEase(Ease.Linear));

            await sequence.AsyncWaitForCompletion();
        }

        private void OnEnable() =>
            MessageService.Subscribe(this);

        private void OnDisable() =>
            MessageService.Unsubscribe(this);

        private void OnDestroy() =>
            MessageService.Unsubscribe(this);
    }
}