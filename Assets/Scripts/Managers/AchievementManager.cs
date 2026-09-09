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

        public List<Achievement> GetQuests()
        {
            if (_achievements.Count == 0 && GameRoot.GameConfig != null) CreateQuests();
            return _achievements;
        }

        public void OnMessage(AchievementMessages.AchievementTargetTaken message)
        {
            foreach (var achievement in GetQuests())
            {
                if (achievement.Data.ItemTarget != null && achievement.Data.ItemTarget == message.Model.Item1 && !achievement.CanTakePrize && !achievement.IsCompleted)
                {
                    achievement.ChangeCurrentValue(message.Model.Item2);
                    GameRoot.PlayerProgress.AchievementProgress.SaveProgress(achievement);
                }
            }
        }

        public void OnMessage(AchievementMessages.AchievementCompleted message) =>
            GameRoot.PlayerProgress.AchievementProgress.SaveProgress(message.Model);

        public void RefreshChestProgress()
        {
            foreach (var quest in GetQuests())
                if (quest.Data.ItemTarget is ChestCollectionTarget target)
                {
                    quest.ChangeCurrentValue(target.CountFound(GameRoot.PlayerProgress.AchievementProgress) - quest.CurrentValueProgress);
                    GameRoot.PlayerProgress.AchievementProgress.SaveProgress(quest);
                }
        }

        private void CreateQuests()
        {
            if (_achievements.Count > 0) return;
            for (var i = 0; i < GameRoot.GameConfig.DataAchievements.Count; i++)
            {
                var definition = GameRoot.GameConfig.DataAchievements[i];
                Achievement achievement = new Achievement(definition, definition.StableId >= 0 ? definition.StableId : i);
                _achievements.Add(achievement);

                if (GameRoot.PlayerProgress.AchievementProgress.Achievements.TryGetValue(achievement.ID, out var data))
                {
                    achievement.LoadData(data);
                    GameRoot.PlayerProgress.AchievementProgress.SaveProgress(achievement);
                }
                else GameRoot.PlayerProgress.AchievementProgress.AddAchievement(achievement);
            }
            RefreshChestProgress();
        }

        private void OnEnable() =>
            MessageService.Subscribe(this);

        private void OnDisable() =>
            MessageService.Unsubscribe(this);
    }
}
