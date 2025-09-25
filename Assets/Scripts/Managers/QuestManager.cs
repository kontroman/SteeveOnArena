using System.Collections.Generic;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using Quest;

namespace Managers
{
    public class QuestManager : BaseManager,
        IMessageSubscriber<QuestMessages.ItemTaken>
    {
        private List<Quest.Quest> _quests = new();

        private void Start() =>
            CreatQuests();

        public List<Quest.Quest> GetQuests() =>
            _quests;

        public void OnMessage(QuestMessages.ItemTaken message)
        {
            foreach (var quest in _quests)
            {
                if (quest.Data.ItemTarget == message.Model.Item1 && !quest.CanTakePrize)
                    quest.ChangeCurrentValue(message.Model.Item2);
            }
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
    }
}