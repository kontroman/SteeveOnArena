using DG.Tweening;
using Devotion.SDK.Controllers;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using UnityEngine;

namespace UI.UIAchievement
{
    public class AchievementButton : MonoBehaviour,
        IMessageSubscriber<AchievementMessages.PrizeTake>,
        IMessageSubscriber<AchievementMessages.AchievementCompleted>
    {
        private const float GrowScale = 1.15f;
        private const float ShrinkScale = 0.9f;
        private const float GrowDuration = 0.18f;
        private const float ShrinkDuration = 0.16f;
        private const float RestoreDuration = 0.14f;
        private const float LoopDelay = 1.5f;

        private readonly int _addValue = 1;
        private readonly int _subtractValue = -1;

        private int _valueAchievementWithPrize;
        private Vector3 _defaultScale;
        private Sequence _notificationSequence;
        private bool _isInitialized;

        private void Awake()
        {
            _defaultScale = transform.localScale;
        }

        private void Start()
        {
            _valueAchievementWithPrize = GetAvailablePrizeCount();
            _isInitialized = true;
            RefreshNotificationState();
        }

        public void OnMessage(AchievementMessages.PrizeTake message) =>
            ChangeAvailablePrizeCount(_addValue);

        public void OnMessage(AchievementMessages.AchievementCompleted message) =>
            ChangeAvailablePrizeCount(_subtractValue);

        private int GetAvailablePrizeCount()
        {
            int count = 0;

            foreach (var (key, data) in GameRoot.PlayerProgress.AchievementProgress.Achievements)
            {
                if (data.CanTakePrize && !data.IsCompleted)
                    count++;
            }

            return count;
        }

        private void ChangeAvailablePrizeCount(int value)
        {
            _valueAchievementWithPrize = Mathf.Max(0, _valueAchievementWithPrize + value);
            RefreshNotificationState();
        }

        private void RefreshNotificationState()
        {
            if (_valueAchievementWithPrize > 0)
                StartNotificationAnimation();
            else
                StopNotificationAnimation(true);
        }

        private void StartNotificationAnimation()
        {
            if (_notificationSequence != null && _notificationSequence.IsActive())
                return;

            _notificationSequence = DOTween.Sequence()
                .Append(transform.DOScale(_defaultScale * GrowScale, GrowDuration).SetEase(Ease.OutSine))
                .Append(transform.DOScale(_defaultScale * ShrinkScale, ShrinkDuration).SetEase(Ease.InOutSine))
                .Append(transform.DOScale(_defaultScale, RestoreDuration).SetEase(Ease.OutSine))
                .AppendInterval(LoopDelay)
                .SetLoops(-1, LoopType.Restart);
        }

        private void StopNotificationAnimation(bool resetScale)
        {
            if (_notificationSequence != null)
            {
                _notificationSequence.Kill();
                _notificationSequence = null;
            }

            if (resetScale)
                transform.localScale = _defaultScale;
        }

        private void OnEnable()
        {
            MessageService.Subscribe(this);

            if (_isInitialized)
            {
                _valueAchievementWithPrize = GetAvailablePrizeCount();
                RefreshNotificationState();
            }
        }

        private void OnDisable()
        {
            MessageService.Unsubscribe(this);
            StopNotificationAnimation(true);
        }
    }
}
