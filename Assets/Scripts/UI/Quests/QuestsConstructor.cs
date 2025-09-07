using System.Collections.Generic;
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

        private readonly List<QuestVisualizer> _quests = new();

        public List<QuestVisualizer> QuestVisualizers(List<Quest> quests)
        {
            foreach (var quest in quests)
            {
                GameObject questSector = Instantiate(_questPrefab, _content);
                
                SettingQuestSector(questSector, quest.Data);
                
                QuestVisualizer questVisualizer = questSector.GetComponent<QuestVisualizer>();
                questVisualizer.Construct(quest);
                _quests.Add(questVisualizer);
            }

            return _quests;
        }

        private void SettingQuestSector(GameObject questSector, DataQuest data)
        {
            Transform iconPrize = questSector.transform.Find(Constants.Quest.IconPrize);
            Transform taskQuest = questSector.transform.Find(Constants.Quest.TaskContent);
            Transform dropAmount = questSector.transform.Find(Constants.Quest.DropAmount);
            Transform componentNameQuest = questSector.transform.Find(Constants.Quest.QuestName);

            Image icon = iconPrize?.GetComponent<Image>();
            TMP_Text textContent = taskQuest?.GetComponent<TMP_Text>();
            TMP_Text nameQuest = componentNameQuest?.GetComponent<TMP_Text>();
            TMP_Text amount = dropAmount?.GetComponent<TMP_Text>();

            if (icon) icon.sprite = data.ItemPrize.Icon;
            if (textContent) textContent.text = data.TextTask;
            if (nameQuest) nameQuest.text = data.NameQuest;
            if (amount) amount.text = data.Amount.ToString();
        }
    }
}