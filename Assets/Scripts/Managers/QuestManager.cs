using System.Collections.Generic;
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

    private readonly int _initialValue = 0;
    private bool _windowQuestActive;

    private void Start() =>
        FillDictionary();

    public void OnMessage(QuestMessages.OpenWindowQuests listQuests)
    {
        _windowQuestActive = true;
        _quests = listQuests.Model;

        UpdateQuestData();
    }

    public void OnMessage(QuestMessages.CloseWindowQuests listQuests)
    {
        _quests = listQuests.Model;
        _windowQuestActive = false;
    }

    public void OnMessage(QuestMessages.ItemTaken message)
    {
        AddCurrentValue(message);
    }

    private void AddCurrentValue(QuestMessages.ItemTaken itemConfig)
    {
        if (_dictionary.ContainsKey(itemConfig.Model.Item1))
            _dictionary[itemConfig.Model.Item1] += itemConfig.Model.Item2;

        if (_windowQuestActive)
            UpdateQuestData();
    }

    private void UpdateQuestData()
    {
        foreach (var quest in _quests)
        {
            if (_dictionary.ContainsKey(quest.ItemPrize.ItemConfig))
                quest.ChangeCurrentValue(_dictionary[quest.ItemTarget]);
        }
    }

    private void FillDictionary()
    {
        foreach (var dataQuest in GameRoot.GameConfig.DataQuests)
            _dictionary.TryAdd(dataQuest.ItemNeedToGet, _initialValue);
    }

    private void OnEnable() =>
        MessageService.Subscribe(this);

    private void OnDisable() =>
        MessageService.Unsubscribe(this);

    private void OnDestroy() =>
        MessageService.Unsubscribe(this);
}