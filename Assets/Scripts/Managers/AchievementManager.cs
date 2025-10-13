using System.Collections.Generic;
using System.Linq;
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
            CreatQuests();

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

        private void CreatQuests()
        {
            for (var i = 0; i < GameRoot.GameConfig.DataAchievements.Count; i++)
                _achievements.Add(new Achievement(GameRoot.GameConfig.DataAchievements[i], i));

            if (GameRoot.PlayerProgress.AchievementProgress.AchievementsDataSave.Count == 0)
            {
                foreach (Achievement achievement in _achievements)
                    GameRoot.PlayerProgress.AchievementProgress.AddAchievement(achievement);
            }
            else
            {
                foreach (AchievementSaveData data in GameRoot.PlayerProgress.AchievementProgress.AchievementsDataSave)
                {
                    foreach (var achievement in
                             _achievements.Where(achievement => achievement.ID == data.AchievementId))
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