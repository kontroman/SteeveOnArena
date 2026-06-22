using System;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class LevelsProgress : BaseProgress
    {
        [SerializeField] private int highestUnlockedLevelIndex;

        public int HighestUnlockedLevelIndex => Mathf.Max(0, highestUnlockedLevelIndex);

        public bool IsLevelUnlocked(int levelIndex)
        {
            return levelIndex >= 0 && levelIndex <= HighestUnlockedLevelIndex;
        }

        public void UnlockLevel(int levelIndex)
        {
            if (levelIndex <= highestUnlockedLevelIndex)
                return;

            highestUnlockedLevelIndex = Mathf.Max(0, levelIndex);
            Save();
        }

        public void UnlockNextLevel(int completedLevelIndex)
        {
            UnlockLevel(completedLevelIndex + 1);
        }
    }
}
