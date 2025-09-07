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

        private readonly List<Quest> _listActiveQuests = new();
        private List<QuestVisualizer> _listQuestVisualizers = new();

        public void Start()
        {
            _listQuestVisualizers = _questsConstructor.QuestVisualizers(_listActiveQuests);
        }

        public void OnMessage(QuestMessages.QuestCompleted message) =>
            _listActiveQuests.Remove(message.Model);

        public void Close() =>
            GameRoot.UIManager.CloseWindow<WindowQuests>();

        private void OnEnable()
        {
            
            QuestMessages.OpenWindowQuests.Publish(_listQuestVisualizers);
            MessageService.Subscribe(this);
        }

        private void OnDisable()
        {
            QuestMessages.CloseWindowQuests.Publish();
            MessageService.Unsubscribe(this);
        }

        private void OnDestroy() =>
            MessageService.Unsubscribe(this);
    }
}