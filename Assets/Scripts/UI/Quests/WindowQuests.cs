using System.Collections.Generic;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Managers;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using UnityEngine;

namespace UI.Quests
{
    public class WindowQuests : BaseWindow,
        IMessageSubscriber<QuestMessages.ItemTaken>
    {
        [SerializeField] private QuestsConstructor _questsConstructor;

        private List<Quest> _activeQuests = new();
        private List<QuestVisualizer> _questVisualizers = new();

        private void Awake()
        {
            _activeQuests = GameRoot.GetManager<QuestManager>().GiveQuests();
            _questVisualizers = _questsConstructor.QuestVisualizers(_activeQuests);
        }

        public void Close() =>
            GameRoot.UIManager.CloseWindow<WindowQuests>();

        public void OnMessage(QuestMessages.ItemTaken message)
        {
            UpdateProgressValue();
        }

        private void UpdateProgressValue()
        {
            _activeQuests = GameRoot.GetManager<QuestManager>().GiveQuests();

            foreach (var visualizer in _questVisualizers)
            {
                foreach (var quest in _activeQuests)
                {
                    if (quest == visualizer.MyQuest)
                        visualizer.ChangeCurrentValue();
                }
            }
        }

        private void OnEnable()
        {
            UpdateProgressValue();
            MessageService.Subscribe(this);
        }

        private void OnDisable() =>
            MessageService.Unsubscribe(this);

        private void OnDestroy() =>
            MessageService.Unsubscribe(this);
    }
}