using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Basics;
using MineArena.Game.UI;
using System;

namespace MineArena.PlayerSystem
{
    public class PlayerExperience : IProgressBar
    {
        private PlayerDataProgress _progress;
        private int _currentExperience;
        private int _currentLevel;

        public PlayerExperience(PlayerDataProgress progress = null)
        {
            BindProgress(progress);
        }

        public event Action<float, float> OnExperienceChanged;
        public event Action<int> OnLevelChanged;
        public event Action<int, int> OnExperienceGained;

        public int CurrentExperience => _currentExperience;
        public int CurrentLevel => _currentLevel;
        public const int MaxLevel = 100;
        public int ExperiencePerLevel => RequiredExperience(_currentLevel);
        // Argument is the current level: 1 -> 2 costs 60, 2 -> 3 costs 120.
        public static int RequiredExperience(int level) => level <= 1 ? 60 : level == 2 ? 120 :
            (int)Math.Min(1000000000d, 250d * Math.Pow(2d, level - 3));
        public static int QuestReward(global::Structs.QuestDifficulty difficulty) => difficulty switch
        { global::Structs.QuestDifficulty.Easy => 80, global::Structs.QuestDifficulty.Normal => 200, _ => 500 };
        public static int MonsterReward(float health) => UnityEngine.Mathf.Clamp(15 + UnityEngine.Mathf.RoundToInt(health * .12f), 15, 150);
        public static int ArenaMonsterReward(int totalExperience, int totalMonsters, int spawnIndex)
        {
            if (totalExperience <= 0 || totalMonsters <= 0 || spawnIndex < 0 || spawnIndex >= totalMonsters) return 0;
            // Differences of cumulative integer shares preserve the exact full-clear budget.
            return (int)((long)totalExperience * (spawnIndex + 1) / totalMonsters - (long)totalExperience * spawnIndex / totalMonsters);
        }

        public float MaxValue => ExperiencePerLevel;
        public float CurrentValue => _currentExperience;

        public void BindProgress(PlayerDataProgress progress)
        {
            _progress = progress;
            RestoreData();
        }

        public void AddExperience(int amount = 50)
        {
            if (amount <= 0)
                return;

            if (_currentLevel >= MaxLevel) return;
            int previousLevel = _currentLevel;
            _currentExperience = (int)Math.Min(int.MaxValue, (long)_currentExperience + amount);
            bool levelChanged = ApplyLevelUps();

            SaveData();

            if (levelChanged)
                NotifyLevelChanged();
            NotifyExperienceChanged();
            OnExperienceGained?.Invoke(amount, _currentLevel - previousLevel);
        }

        public void RestoreData(int level, int experience)
        {
            _currentLevel = Math.Min(MaxLevel, Math.Max(1, level));
            _currentExperience = Math.Max(0, experience);

            ApplyLevelUps();

            if (_progress != null && (_progress.CurrentLevel != _currentLevel || _progress.CurrentExperience != _currentExperience)) SaveData();
            NotifyLevelChanged();
            NotifyExperienceChanged();
        }

        public void RestoreData()
        {
            if (_progress != null)
            {
                RestoreData(_progress.CurrentLevel, _progress.CurrentExperience);
                return;
            }

            _currentLevel = Math.Max(1, _currentLevel);
            NotifyLevelChanged();
            NotifyExperienceChanged();
        }

        private void SaveData()
        {
            _progress?.CacheExperience(_currentLevel, _currentExperience);
        }

        private bool ApplyLevelUps()
        {
            int oldLevel = _currentLevel;
            while (_currentLevel < MaxLevel && _currentExperience >= ExperiencePerLevel)
            {
                _currentExperience -= ExperiencePerLevel;
                _currentLevel++;
            }
            if (_currentLevel >= MaxLevel) _currentExperience = 0;
            return oldLevel != _currentLevel;
        }

        private void NotifyExperienceChanged()
        {
            OnExperienceChanged?.Invoke(_currentExperience, ExperiencePerLevel);
        }

        private void NotifyLevelChanged()
        {
            OnLevelChanged?.Invoke(_currentLevel);
        }
    }
}
