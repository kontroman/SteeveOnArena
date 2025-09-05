using System.Collections.Generic;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using UnityEngine;

namespace UI.Quests
{
    public class WindowQuests : BaseWindow,
        IMessageSubscriber<QuestMessages.QuestCompleted>
    {
        [SerializeField] private QuestsConstructor _questsConstructor;

        private List<Quest> _listActiveQuests = new();

        public void Awake() => 
            _listActiveQuests = _questsConstructor.CreateQuests();

        public void OnMessage(QuestMessages.QuestCompleted message) => 
            _listActiveQuests.Remove(message.Model);

        public void Close() => 
            GameRoot.UIManager.CloseWindow<WindowQuests>();

        private void OnEnable()
        {
            QuestMessages.OpenWindowQuests.Publish(_listActiveQuests);
            MessageService.Subscribe(this);
        }

        private void OnDisable()
        {
            QuestMessages.CloseWindowQuests.Publish(_listActiveQuests);
            MessageService.Unsubscribe(this);
        }

        private void OnDestroy() =>
            MessageService.Unsubscribe(this);
    }
}