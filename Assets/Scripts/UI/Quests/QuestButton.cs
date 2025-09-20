using MineArena.Messages;
using MineArena.Messages.MessageService;
using TMPro;
using UnityEngine;

namespace UI.Quests
{
    public class QuestButton : MonoBehaviour,
        IMessageSubscriber<QuestMessages.PrizeTake>,
        IMessageSubscriber<QuestMessages.QuestCompleted>
    {
        [SerializeField] private TextMeshProUGUI _text;

        private int _valueQuestWithPrize;

        private void Start()
        {
            SetValue(0);
        }

        public void OnMessage(QuestMessages.PrizeTake message)
        {
            SetValue(1);
        }

        public void OnMessage(QuestMessages.QuestCompleted message)
        {
            SetValue(-1);
        }

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