using System;
using System.Collections.Generic;
using DG.Tweening;
using MineArena.Game.UI;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using TMPro;
using System.Threading.Tasks;
using UnityEngine;

namespace UI.Quests
{
    public class QuestPopup : MonoBehaviour,
        IProgressBar,
        IMessageSubscriber<QuestMessages.ItemTaken>,
        IMessageSubscriber<QuestMessages.QuestBegun>
    {
        [SerializeField] private TextMeshProUGUI _nameQuest;
        [SerializeField] private ProgressQuestBar _progressBarQuest;
        [SerializeField] private float _duration = 0.5f;
        [SerializeField] private float _timer = 0.7f;

        private readonly Queue<QuestMessages.QuestBegun> _messageQueue = new();

        // private bool _isProcessing = false;
        private Coroutine _processingCoroutine;
        private RectTransform _rectTransform;
        private Quest _quest;

        private bool _isAnimating;
        //private readonly Queue<Quest> _queueQuests = new();

        public event Action<float, float> OnValueChanged;

        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        public void OnMessage(QuestMessages.QuestBegun message)
        {
            _messageQueue.Enqueue(message);
    
            if (!_isAnimating)
            {
                ProcessQueue();
            }
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


        private void Construct(QuestMessages.QuestBegun message)
        {
            MaxValue = message.Model.MaxValueProgress;
            Debug.Log(MaxValue);
            Debug.Log(CurrentValue);
            CurrentValue = message.Model.CurrentValueProgress;
            _nameQuest.text = message.Model.Data.NameQuest;
            OnValueChanged?.Invoke(CurrentValue, MaxValue);
        }

        private async Task ShowAnimation()
        {
            var sequence = DOTween.Sequence()
                .Append(transform.DOMove(_rectTransform.position + new Vector3(0, -100, 0), _duration)
                    .SetEase(Ease.Linear))
                .AppendInterval(_timer)
                .Append(transform.DOMove(_rectTransform.position, _duration).SetEase(Ease.Linear));

            await sequence.AsyncWaitForCompletion();
        }

        private void OnEnable() =>
            MessageService.Subscribe(this);

        private void OnDisable() =>
            MessageService.Unsubscribe(this);

        private void OnDestroy() =>
            MessageService.Unsubscribe(this);

        public void OnMessage(QuestMessages.ItemTaken message)
        {
            // Debug.LogError("Пришло сообщение в попап");
            // _quests = GameRoot.GetManager<QuestManager>().GiveQuests();
            //
            // foreach (Quest quest in _quests)
            // {
            //     Debug.LogError("ЗАшли в цикл обработки квестов");
            //     if (quest.CurrentValueProgress != 0)
            //     {
            //         MaxValue = quest.MaxValueProgress;
            //         CurrentValue = quest.CurrentValueProgress;
            //         _nameQuest.text = quest.Data.NameQuest;
            //         _spot = 0;
            //         _coroutine = StartCoroutine(ShowInformation());
            //     }
            // }
        }
    }
}