using System;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class PlayerDataProgress : BaseProgress
    {
        [SerializeField] private int currentExperience;
        [SerializeField] private int currentLevel;
        [SerializeField] private string fallbackName;
        [SerializeField] private int[] development = new int[5];

        public string FallbackName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(fallbackName))
                {
                    fallbackName = "Player" + UnityEngine.Random.Range(100, 1000);
                    if (!Application.isPlaying || Devotion.SDK.Services.SaveSystem.SaveService.Instance.IsLoaded) Save();
                }
                return fallbackName;
            }
        }

        public int[] CopyDevelopment()
        {
            var result = new int[5];
            int remaining = Math.Max(0, CurrentLevel - 1);
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = development != null && i < development.Length ? Mathf.Clamp(development[i], 0, Math.Min(50, remaining)) : 0;
                remaining -= result[i];
            }
            return result;
        }

        public bool TrySaveDevelopment(int[] values)
        {
            if (values == null || values.Length != 5) return false;
            int total = 0;
            foreach (int value in values)
            {
                if (value < 0 || value > 50) return false;
                total += value;
            }
            if (total > Math.Max(0, CurrentLevel - 1)) return false;
            development = (int[])values.Clone();
            Save();
            return true;
        }

        public int CurrentExperience => currentExperience;
        public int CurrentLevel => Math.Max(1, currentLevel);

        public void CacheExperience(int level, int experience)
        {
            currentLevel = Mathf.Max(0, level);
            currentExperience = Mathf.Max(0, experience);

            Save();
        }
    }
}
