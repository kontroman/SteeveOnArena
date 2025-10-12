using System.Collections.Generic;
using Structs;
using UI.Sector;
using UI.UIAchievement;
using UnityEngine;

namespace Achievements
{
    public class AchievementsConstructor : MonoBehaviour
    {
        [SerializeField] private GameObject _questPrefab;
        [SerializeField] private Transform _content;

        private readonly List<AchievementVisualizer> _achievements = new();
        private ISectorBuilder _builder;

        public List<AchievementVisualizer> CreateQuestVisualizers(List<Achievement> quests)
        {
            _builder = new SectorBuilder();

            foreach (var quest in quests)
            {
                var questVisualizer = CreateQuestVisualizer(quest);
                _achievements.Add(questVisualizer);
            }

            return _achievements;
        }

        private AchievementVisualizer CreateQuestVisualizer(Achievement achievement)
        {
            GameObject questSector = Instantiate(_questPrefab, _content);
            ConfigureQuestSector(questSector, achievement.Data);

            AchievementVisualizer achievementVisualizer = questSector.GetComponent<AchievementVisualizer>();
            achievementVisualizer.Construct(achievement);

            return achievementVisualizer;
        }

        private void ConfigureQuestSector(GameObject questSector, DataAchievement data)
        {
            _builder.Build(questSector, data);
        }
    }
}