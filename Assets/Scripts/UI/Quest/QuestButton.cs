using MineArena.Messages;
using MineArena.Messages.MessageService;
using TMPro;
using UnityEngine;

namespace UI.Quest
{
    public class QuestButton : MonoBehaviour,
        IMessageSubscriber<QuestMessages.PrizeTake>,
        IMessageSubscriber<QuestMessages.QuestCompleted>
    {
        [SerializeField] private TextMeshProUGUI _text;

        private readonly int _startValue = 0;
        private readonly int _addValue = 1;
        private readonly int _subtractValue = -1;
        
        private int _valueQuestWithPrize;

        private void Start() => 
            SetValue(_startValue);

        public void OnMessage(QuestMessages.PrizeTake message) =>
            SetValue(_addValue);

        public void OnMessage(QuestMessages.QuestCompleted message) =>
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