using System.Collections.Generic;
using Achievements;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using UnityEngine;

namespace Managers
{
    public class AchievementManager : BaseManager,
        IMessageSubscriber<AchievementMessages.AchievementTargetTaken>
    {
        private void Start() =>
            CreatQuests();

        public List<Achievement> GetQuests() =>
            GameRoot.PlayerProgress.AchievementProgress.AchievementsDataSave;

        public void OnMessage(AchievementMessages.AchievementTargetTaken message)
        {
            foreach (var achievement in GameRoot.PlayerProgress.AchievementProgress.AchievementsDataSave)
            {
                if (achievement.Data.ItemTarget == message.Model.Item1 && !achievement.CanTakePrize)
                    achievement.ChangeCurrentValue(message.Model.Item2);
            }
        }

        private void CreatQuests()
        {
            if (GameRoot.PlayerProgress.AchievementProgress.AchievementsDataSave.Count == 0)
            {
                for (var i = 0; i < GameRoot.GameConfig.DataAchievements.Count; i++)
                    GameRoot.PlayerProgress.AchievementProgress.AddAchievement(
                        new Achievement(GameRoot.GameConfig.DataAchievements[i], i));
            }
            else
            {
                Debug.LogError(GameRoot.PlayerProgress.AchievementProgress.AchievementsDataSave.Count);
            }
        }

        private void OnEnable() =>
            MessageService.Subscribe(this);

        private void OnDisable() =>
            MessageService.Unsubscribe(this);
    }
}