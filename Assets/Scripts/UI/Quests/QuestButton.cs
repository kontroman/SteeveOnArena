using Devotion.SDK.Controllers;
using Managers;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using TMPro;
using UnityEngine;

namespace UI.Quests
{
    public class QuestButton : MonoBehaviour,
        IMessageSubscriber<QuestMessages.PrizeTake>
    {
        [SerializeField] private TextMeshProUGUI _text;

        private int _valueQuestWithPrize;

        private void Start()
        {
            _valueQuestWithPrize = 0;
            _text.text = _valueQuestWithPrize.ToString();
        }

        public void OnMessage(QuestMessages.PrizeTake message)
        {
            Debug.Log(_valueQuestWithPrize);
            _valueQuestWithPrize = message.Model;
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