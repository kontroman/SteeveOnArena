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
        [SerializeField] private GameObject _questPrefab;
        [SerializeField] private Transform _content;

        private readonly List<Quest> _quests = new();
        
        public List<Quest> CreateQuests()
        {
            foreach (DataQuest dataQuest in GameRoot.GameConfig.DataQuests)
            {
                GameObject questSector = Instantiate(_questPrefab, _content);

                SettingQuestSector(questSector, dataQuest);

                Quest quest = questSector.GetComponent<Quest>();
                quest.Construct(dataQuest.MaxValueOnTask, dataQuest.ItemPrize, dataQuest.ItemNeedToGet);
                _quests.Add(quest);
            }

            return _quests;
        }

        private void SettingQuestSector(GameObject questSector, DataQuest data)
        {
            Transform iconPrize = questSector.transform.Find(Constants.Quest.IconPrize);
            Transform taskQuest = questSector.transform.Find(Constants.Quest.TaskContent);
            Transform dropAmount = questSector.transform.Find(Constants.Quest.DropAmount);

            Image icon = iconPrize?.GetComponent<Image>();
            TMP_Text textContent = taskQuest?.GetComponent<TMP_Text>();
            TMP_Text amount = dropAmount?.GetComponent<TMP_Text>();

            if (icon) icon.sprite = data.ItemPrize.Icon;
            if (textContent) textContent.text = data.TextTask;
            if (amount) amount.text = data.Amount.ToString();
        }
    }
}