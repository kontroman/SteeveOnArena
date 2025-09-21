using System.Collections.Generic;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using UI.Quests;
using UnityEngine;

namespace Managers
{
    public class QuestManager : BaseManager,
        IMessageSubscriber<QuestMessages.ItemTaken>,
        IMessageSubscriber<QuestMessages.QuestCompleted>
    {
        private readonly List<Quest> _quests = new();

        private int _valueTakePrizeQuests;

        private void Start() =>
            CreatQuests();

        public List<Quest> GiveQuests() =>
            _quests;

        public void OnMessage(QuestMessages.ItemTaken message)
        {
            foreach (var quest in _quests)
            {
                if (quest.Data.ItemTarget == message.Model.Item1)
                {
                    quest.ChangeCurrentValue(message.Model.Item2);

                    // if (Mathf.Approximately(quest.CurrentValueProgress, 1))
                    // {
                    //     QuestMessages.QuestBegun.Publish(quest);
                    // }
                }
            }
        }

        public void OnMessage(QuestMessages.QuestCompleted message)
        {
            _quests.Remove(message.Model);
        }

        private void CreatQuests()
        {
            foreach (DataQuest dataQuest in GameRoot.GameConfig.DataQuests)
                _quests.Add(new(dataQuest));
        }

        private void OnEnable() =>
            MessageService.Subscribe(this);

        private void OnDisable() =>
            MessageService.Unsubscribe(this);

        private void OnDestroy() =>
            MessageService.Unsubscribe(this);
    }
}