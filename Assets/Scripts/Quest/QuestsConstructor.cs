using System.Collections.Generic;
using UI.Quest;
using UI.Sector;
using UnityEngine;

namespace Quest
{
    public class QuestsConstructor : MonoBehaviour
    {
        [SerializeField] private GameObject _questPrefab;
        [SerializeField] private Transform _content;

        private readonly List<QuestVisualizer> _quests = new();
        private ISectorBuilder _builder;

        public List<QuestVisualizer> CreateQuestVisualizers(List<Quest> quests)
        {
            _builder = new SectorBuilder();

            foreach (var quest in quests)
            {
                var questVisualizer = CreateQuestVisualizer(quest);
                _quests.Add(questVisualizer);
            }

            return _quests;
        }

        private QuestVisualizer CreateQuestVisualizer(Quest quest)
        {
            GameObject questSector = Instantiate(_questPrefab, _content);
            ConfigureQuestSector(questSector, quest.Data);

            QuestVisualizer questVisualizer = questSector.GetComponent<QuestVisualizer>();
            questVisualizer.Construct(quest);

            return questVisualizer;
        }

        private void ConfigureQuestSector(GameObject questSector, DataQuest data)
        {
            _builder.Build(questSector, data);
        }
    }
}