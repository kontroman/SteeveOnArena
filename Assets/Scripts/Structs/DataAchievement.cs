using Achievements;
using MineArena.UI.FortuneWheel;
using UnityEngine;

namespace Structs
{
    public enum QuestDifficulty { Easy, Normal, Hard }

    [System.Serializable]
    public class DataAchievement
    {
        [SerializeField] private ItemPrize _itemPrize;
        [SerializeField] private ScriptableObject _itemTarget;
        [SerializeField] private string _nameAchievementKey;
        [SerializeField] private string _textTaskKey;
        [SerializeField] private int _maxValueOnTask;
        [SerializeField] private int _stableId = -1;
        [SerializeField] private string _title;
        [SerializeField, TextArea] private string _description;
        [SerializeField] private Sprite _questIcon;
        [SerializeField] private QuestDifficulty _difficulty;

        public int StableId => _stableId;
        public string Title => _title;
        public string Description => _description;
        public Sprite QuestIcon => _questIcon;
        public QuestDifficulty Difficulty => _difficulty;
        public string DifficultyLabel => _difficulty == QuestDifficulty.Hard ? "Сложное" : _difficulty == QuestDifficulty.Normal ? "Среднее" : "Лёгкое";

        public ItemPrize ItemPrize => _itemPrize;
        public IAchievementTarget ItemTarget => _itemTarget as IAchievementTarget;
        public int Amount => _itemPrize.Amount;
        public string NameAchievementKey => _nameAchievementKey;
        public string TextTaskKey => _textTaskKey;
        public int MaxValueOnTask => _maxValueOnTask;
    }
}
