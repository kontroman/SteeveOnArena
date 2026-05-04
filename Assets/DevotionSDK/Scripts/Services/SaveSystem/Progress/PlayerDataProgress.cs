using System;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class PlayerDataProgress : BaseProgress
    {
        [SerializeField] private int currentExperience;
        [SerializeField] private int currentLevel;

        public int CurrentExperience => currentExperience;
        public int CurrentLevel => currentLevel;

        public void CacheExperience(int level, int experience)
        {
            currentLevel = Mathf.Max(0, level);
            currentExperience = Mathf.Max(0, experience);

            Save();
        }
    }
}
