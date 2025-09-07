using MineArena.Messages;
using MineArena.Messages.MessageService;
using TMPro;
using UnityEngine;

namespace UI.Quests
{
    public class QuestButton : MonoBehaviour,
        IMessageSubscriber<QuestMessages.QuestCompleted>,
        IMessageSubscriber<QuestMessages.PrizeTake>
    {
        [SerializeField] private TextMeshProUGUI _text;

        private int _valueQuestWithPrize;

        // private void Start()
        // {
        //     _valueQuestWithPrize = 0;
        // }

        public void OnMessage(QuestMessages.QuestCompleted message)
        {
            Debug.Log("Квест завершен. Награда ждет");
            _valueQuestWithPrize -= 1;
            _text.text = _valueQuestWithPrize.ToString();
        }


        public void OnMessage(QuestMessages.PrizeTake message)
        {
            _valueQuestWithPrize += 1;
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