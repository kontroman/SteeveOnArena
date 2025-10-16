using Achievements;
using MineArena.UI.FortuneWheel;
using UnityEngine;

namespace Structs
{
    [System.Serializable]
    public class DataAchievement
    {
        [SerializeField] private ItemPrize _itemPrize;
        [SerializeField] private ScriptableObject _itemTarget;
        [SerializeField] private string _nameAchievementKey;
        [SerializeField] private string _textTaskKey;
        [SerializeField] private int _maxValueOnTask;

        public ItemPrize ItemPrize => _itemPrize;
        public IAchievementTarget ItemTarget => _itemTarget as IAchievementTarget;
        public int Amount => _itemPrize.Amount;
        public string NameAchievementKey => _nameAchievementKey;
        public string TextTaskKey => _textTaskKey;
        public int MaxValueOnTask => _maxValueOnTask;
    }
}