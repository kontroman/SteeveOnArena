using System.Collections.Generic;
using System.Linq;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using MineArena.Items;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using UI.Quests;

public class QuestManager : BaseManager,
    IMessageSubscriber<QuestMessages.ItemTaken>,
    IMessageSubscriber<QuestMessages.OpenWindowQuests>,
    IMessageSubscriber<QuestMessages.CloseWindowQuests>
{
    private readonly Dictionary<ItemConfig, int> _dictionary = new();
    private List<Quest> _quests = new();
    private List<QuestVisualizer> _questVisualizers = new();

    private readonly int _initialValue = 0;
    private bool _windowQuestActive;

    private void Start()
    {
        CreatQuests();
    }

    public void OnMessage(QuestMessages.ItemTaken message)
    {
        foreach (var quest in _quests.Where(quest => quest.Data.ItemTarget == message.Model.Item1))
            quest.ChangeCurrentValue(message.Model.Item2);
    }

    public void OnMessage(QuestMessages.OpenWindowQuests listQuestVisualizers)
    {
        _windowQuestActive =  true;
        
        for (int i = 0; i < _quests.Count && i < listQuestVisualizers.Model.Count; i++)
        {
            foreach (var quest in _quests.Where(quest => quest == listQuestVisualizers.Model[i].MyQuest))
                listQuestVisualizers.Model[i].MyQuest.ChangeCurrentValue(quest.CurrentValueProgress);
        }
    }

    public void OnMessage(QuestMessages.CloseWindowQuests listQuests)
    {
        _windowQuestActive = false;
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