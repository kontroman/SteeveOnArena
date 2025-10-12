using System;
using System.Collections.Generic;
using Achievements;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class AchievementProgress : BaseProgress
    {
        public List<Achievement> AchievementsDataSave= new();

        public AchievementProgress () { }
        
        // public List<Achievement> GetAchievements() =>
        //     AchievementsDataSave;

        public void AddAchievement(Achievement achievement)
        {
            
            AchievementsDataSave.Add(achievement);
            Debug.LogError(AchievementsDataSave.Count); 
        }

        public void SaveProgressQuests(Achievement achievementSave)
        {
            for (var i = 0; i < AchievementsDataSave.Count; i++)
            {
                if (AchievementsDataSave[i].ID == achievementSave.ID)
                {
                    AchievementsDataSave.Add(achievementSave);
                    AchievementsDataSave.Remove(AchievementsDataSave[i]);
                }
            }
        }
    }

    // [Serializable]
    // public class QuestSaveData
    // {
    //     public string questId;
    //     public int currentValue;
    //     public bool isCompleted;
    //     public bool canTakePrize;
    //
    //     public QuestSaveData(string id, int value, bool completed, bool canTakePrize)
    //     {
    //         questId = id;
    //         currentValue = value;
    //         isCompleted = completed;
    //         this.canTakePrize = canTakePrize;
    //     }
    // }
}