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

        private readonly int _startValue = 0;
        private readonly int _addValue = 1;
        private readonly int _subtractValue = -1;
        
        private int _valueQuestWithPrize;

        private void Start() => 
            SetValue(_startValue);

        public void OnMessage(AchievementMessages.PrizeTake message) =>
            SetValue(_addValue);

        public void OnMessage(AchievementMessages.AchievementCompleted message) =>
            SetValue(_subtractValue);

        private void SetValue(int value)
        {
            _valueQuestWithPrize += value;
            _text.text = _valueQuestWithPrize.ToString();
        }

        private void OnEnable() =>
            MessageService.Subscribe(this);

        private void OnDisable() =>
            MessageService.Unsubscribe(this);

        private void OnDestroy() =>
            MessageService.Unsubscribe(this);
    }
}