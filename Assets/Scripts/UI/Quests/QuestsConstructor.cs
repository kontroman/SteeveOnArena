using System.Collections.Generic;
using Devotion.SDK.Controllers;
using MineArena.Basics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Quests
{
    public class QuestsConstructor : MonoBehaviour
    {
        [SerializeField] private RectTransform _content;
        [SerializeField] private GameObject _questPrefab;

        private readonly List<DataQuest> _quests = new();

        private void Start()
        {
            CreatListPrizeItems();
            ConstructQuest();
        }

        private void ConstructQuest()
        {
            foreach (DataQuest quest in _quests)
            {
                GameObject questItem = Instantiate(_questPrefab, _content);
                SettingSector(questItem, quest);
            }
        }

        private void SettingSector(GameObject questItem, DataQuest data)
        {
            Transform iconPrize = questItem.transform.Find(Constants.Quest.IconPrize);
            Transform taskQuest = questItem.transform.Find(Constants.Quest.TaskContent);
            Transform dropAmount = questItem.transform.Find(Constants.Quest.DropAmount);

            Image icon = iconPrize?.GetComponent<Image>();
            TMP_Text textContent = taskQuest?.GetComponent<TMP_Text>();
            TMP_Text amount = dropAmount?.GetComponent<TMP_Text>();

            if (icon) icon.sprite = data.ItemConfig.Icon;
            if (textContent) textContent.text = data.TextTask;
            if (amount) amount.text = data.Amount.ToString();

            Quest quest = questItem.GetComponent<Quest>();
            quest.Construct(data.MaxValueOnTask);
        }

        private void CreatListPrizeItems()
        {
            foreach (var t in GameRoot.GameConfig.DataQuests)
                _quests.Add(t);
        }
    }
}