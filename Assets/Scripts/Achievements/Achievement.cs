using MineArena.Messages;
using MineArena.UI.FortuneWheel;
using Structs;

namespace Achievements
{
    public class Achievement
    {
        private readonly int _initialValue = 0;
        private readonly int _maxValueProgress;
        private readonly ItemPrize _itemPrize;

        private int _currentValueProgress;
        private bool _isCompleted;
        private IAchievementTarget _itemTarget;
        private bool _canTakePrize;
        private readonly int _id;

        public int ID => _id;
        public int CurrentValueProgress => _currentValueProgress;
        public int MaxValueProgress => _maxValueProgress;
        public bool IsCompleted => _isCompleted;
        public bool CanTakePrize => _canTakePrize;
        public DataAchievement Data { get; private set; }

        public Achievement(DataAchievement data, int id)
        {
            Data = data;
            _id = id;
            _currentValueProgress = _initialValue;
            _maxValueProgress = data.MaxValueOnTask;
            _itemPrize = data.ItemPrize;
            _itemPrize.Construct();
            _isCompleted = false;
            _canTakePrize = false;
        }

        public void ChangeCurrentValue(int value)
        {
            if (value <= 0 || _isCompleted || _canTakePrize) return;
            bool firstProgress = _currentValueProgress == 0;
            _currentValueProgress = (int)System.Math.Min((long)_currentValueProgress + value, _maxValueProgress);

            if (firstProgress && Data.Difficulty == QuestDifficulty.Easy)
                AchievementMessages.AchievementBegun.Publish(this);

            if (_currentValueProgress >= _maxValueProgress)
            {
                _canTakePrize = true;
                AchievementMessages.PrizeTake.Publish(this);
            }
        }

        public void TransferPrize()
        {
            if (_isCompleted || !_canTakePrize) return;
            _isCompleted = true;
            AchievementMessages.AchievementCompleted.Publish(this);
            _itemPrize.GiveTo();
            MineArena.Cosmetics.SkinService.GrantQuestRewards(_id);
            MineArena.Controllers.Player.Instance?.Experience?.AddExperience(MineArena.PlayerSystem.PlayerExperience.QuestReward(Data.Difficulty));
            if (Data.ItemTarget is ChestCollectionTarget)
                Devotion.SDK.Messages.Player.SavePlayerProgress.Publish();
        }

        public void LoadData(AchievementSaveData data)
        {
            _currentValueProgress = data.CurrentValue;
            _isCompleted = data.IsCompleted;
            _canTakePrize = !_isCompleted && _currentValueProgress >= _maxValueProgress;
        }
    }
}
