using System;
using System.Collections.Generic;
using System.Linq;
using Achievements;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class AchievementProgress : BaseProgress
    {
        public List<AchievementSaveData> AchievementsDataSave = new();

        public void AddAchievement(Achievement achievement)
        {
            AchievementsDataSave.Add(new AchievementSaveData(achievement.ID, achievement.CurrentValueProgress,
                achievement.IsCompleted, achievement.CanTakePrize));
        }

        public void SaveProgress(Achievement achievement)
        {
            foreach (var dataSave in AchievementsDataSave.Where(dataSave => dataSave.AchievementId == achievement.ID))
            {
                dataSave.CurrentValue = achievement.CurrentValueProgress;
                dataSave.CanTakePrize = achievement.CanTakePrize;
                dataSave.IsCompleted = achievement.IsCompleted;
            }
        }
    }
}

[Serializable]
public class AchievementSaveData
{
    public int AchievementId;
    public int CurrentValue;
    public bool IsCompleted;
    public bool CanTakePrize;

    public AchievementSaveData(int id, int value, bool completed, bool canTakePrize)
    {
        AchievementId = id;
        CurrentValue = value;
        IsCompleted = completed;
        CanTakePrize = canTakePrize;
    }
}