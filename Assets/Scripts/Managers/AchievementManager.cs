using System.Collections.Generic;
using Achievements;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using MineArena.Messages;
using MineArena.Messages.MessageService;

namespace Managers
{
    public class AchievementManager : BaseManager,
        IMessageSubscriber<AchievementMessages.AchievementTargetTaken>,
        IMessageSubscriber<AchievementMessages.AchievementCompleted>
    {
        private List<Achievement> _achievements = new();

        private void Start() =>
            CreateQuests();

        public List<Achievement> GetQuests() =>
            _achievements;

        public void OnMessage(AchievementMessages.AchievementTargetTaken message)
        {
            foreach (var achievement in _achievements)
            {
                if (achievement.Data.ItemTarget == message.Model.Item1 && !achievement.CanTakePrize)
                {
                    achievement.ChangeCurrentValue(message.Model.Item2);
                    GameRoot.PlayerProgress.AchievementProgress.SaveProgress(achievement);
                }
            }
        }

        public void OnMessage(AchievementMessages.AchievementCompleted message) =>
            GameRoot.PlayerProgress.AchievementProgress.SaveProgress(message.Model);

        private void CreateQuests()
        {
            for (var i = 0; i < GameRoot.GameConfig.DataAchievements.Count; i++)
            {
                Achievement achievement = new Achievement(GameRoot.GameConfig.DataAchievements[i], i);
                _achievements.Add(achievement);

                if (GameRoot.PlayerProgress.AchievementProgress.Achievements.Count < GameRoot.GameConfig.DataAchievements.Count)
                {
                    GameRoot.PlayerProgress.AchievementProgress.AddAchievement(achievement);
                }
                else if (GameRoot.PlayerProgress.AchievementProgress.Achievements.TryGetValue(achievement.ID, out var data))
                {
                    achievement.LoadData(data);
                }
            }
        }

        private void OnEnable() =>
            MessageService.Subscribe(this);

        private void OnDisable() =>
            MessageService.Unsubscribe(this);
    }
}