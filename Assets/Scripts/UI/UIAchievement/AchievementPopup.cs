using System;
using System.Collections.Generic;
using System.Collections;
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

        private readonly Queue<Achievement> _messageQueue = new();

        private RectTransform _rectTransform;
        private bool _isAnimating;
        private Sequence _sequence;
        private Coroutine _queueRoutine;
        private const float TopInset = 0f;
        private const float PanelGap = 12f;
        public static float OccupiedHeight { get; private set; }
        public static event Action<float> OccupiedHeightChanged;

        public event Action<float, float> OnValueChanged;

        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _rectTransform.anchoredPosition = new Vector2(0, _rectTransform.rect.height + PanelGap);
        }

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
                _queueRoutine = StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            _isAnimating = true;
            SetOccupiedHeight(TopInset + _rectTransform.rect.height + PanelGap);

            while (_messageQueue.Count != 0)
            {
                Achievement achievement = _messageQueue.Dequeue();

                if (!achievement.CanTakePrize)
                    ConstructProgress(achievement);
                else
                    ConstructCompletion(achievement);

                _sequence = DOTween.Sequence().SetUpdate(true)
                    .Append(_rectTransform.DOAnchorPosY(-TopInset, Constants.QuestPopup.Duration).SetEase(Ease.OutCubic))
                    .AppendInterval(Constants.QuestPopup.Timer)
                    .Append(_rectTransform.DOAnchorPosY(_rectTransform.rect.height + PanelGap, Constants.QuestPopup.Duration).SetEase(Ease.InCubic));
                yield return _sequence.WaitForCompletion();
            }

            _sequence = null;
            _queueRoutine = null;
            _isAnimating = false;
            SetOccupiedHeight(0);
        }

        private void ConstructCompletion(Achievement achievement)
        {
            _progressBarQuest.gameObject.SetActive(false);
            _messageTakePrize.gameObject.SetActive(true);
            _nameQuest.text = MineArena.UI.QuestJournalRow.Title(achievement);
            _messageTakePrize.text = LocalizationService.GetLocalizedText(Constants.AchievementKey.TextMessageTakePrizeKey);
        }

        private void ConstructProgress(Achievement achievement)
        {
            _messageTakePrize.gameObject.SetActive(false);
            _progressBarQuest.gameObject.SetActive(true);
            MaxValue = achievement.MaxValueProgress;
            CurrentValue = achievement.CurrentValueProgress;
            _nameQuest.text = MineArena.UI.QuestJournalRow.Title(achievement);
            OnValueChanged?.Invoke(CurrentValue, MaxValue);
        }

        private static void SetOccupiedHeight(float height)
        {
            OccupiedHeight = height;
            OccupiedHeightChanged?.Invoke(height);
        }

        private void OnEnable() =>
            MessageService.Subscribe(this);

        private void OnDisable()
        {
            MessageService.Unsubscribe(this);
            if (_queueRoutine != null) StopCoroutine(_queueRoutine);
            _queueRoutine = null;
            _sequence?.Kill();
            _sequence = null;
            _messageQueue.Clear();
            _isAnimating = false;
            if (_rectTransform != null)
                _rectTransform.anchoredPosition = new Vector2(0, _rectTransform.rect.height + PanelGap);
            SetOccupiedHeight(0);
        }
    }
}
