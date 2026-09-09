using System;
using Achievements;
using static Devotion.SDK.Helpers.ContainersHelper;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class AchievementProgress : BaseProgress
    {
        public SerializableDictionary<int, AchievementSaveData> Achievements = new();
        public System.Collections.Generic.List<string> OpenedChestIds = new();
        public bool HasOpenedChest(string id) => OpenedChestIds != null && OpenedChestIds.Contains(id);
        public bool RegisterChest(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || HasOpenedChest(id)) return false;
            (OpenedChestIds ??= new System.Collections.Generic.List<string>()).Add(id);
            return true;
        }

        public void AddAchievement(Achievement achievement)
        {
            Achievements.Add(achievement.ID, new AchievementSaveData(achievement.ID, achievement.CurrentValueProgress,
                achievement.IsCompleted, achievement.CanTakePrize));
        }

        public void SaveProgress(Achievement achievement)
        {
            if (Achievements.ContainsKey(achievement.ID))
            {
                Achievements[achievement.ID].CurrentValue = achievement.CurrentValueProgress;
                Achievements[achievement.ID].CanTakePrize = achievement.CanTakePrize;
                Achievements[achievement.ID].IsCompleted = achievement.IsCompleted;
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