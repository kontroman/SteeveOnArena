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

        public int CurrentExperience => _currentExperience;
        public int CurrentLevel => _currentLevel;
        public int ExperiencePerLevel => Constants.GameSetting.ExperiencePerLevel;

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

            _currentExperience += amount;
            bool levelChanged = ApplyLevelUps();

            if (levelChanged)
                NotifyLevelChanged();
            NotifyExperienceChanged();

            SaveData();
        }

        public void RestoreData(int level, int experience)
        {
            _currentLevel = Math.Max(0, level);
            _currentExperience = Math.Max(0, experience);

            ApplyLevelUps();

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

            NotifyLevelChanged();
            NotifyExperienceChanged();
        }

        private void SaveData()
        {
            _progress?.CacheExperience(_currentLevel, _currentExperience);
        }

        private bool ApplyLevelUps()
        {
            if (ExperiencePerLevel <= 0)
                return false;

            int levelsToAdd = _currentExperience / ExperiencePerLevel;

            if (levelsToAdd <= 0)
                return false;

            _currentLevel += levelsToAdd;
            _currentExperience %= ExperiencePerLevel;

            return true;
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
