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

        public List<AchievementVisualizer> CreateQuestVisualizers(List<Achievement> achievements)
        {
            _builder = new SectorBuilder();

            foreach (var achievement in achievements)
            {
                var achievementVisualizer = CreateQuestVisualizer(achievement);
                _achievements.Add(achievementVisualizer);
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