using Achievements;
using MineArena.UI.FortuneWheel;
using UnityEngine;

namespace Structs
{
    [System.Serializable]
    public class DataAchievement
    {
        [SerializeField] private ItemPrize _itemPrize;
        [SerializeField] private ScriptableObject  _itemTarget;
        [SerializeField] private string _nameQuest;
        [SerializeField] private string _textTask;
        [SerializeField] private int _maxValueOnTask;

        public ItemPrize ItemPrize => _itemPrize;
        public IAchievementTarget ItemTarget => _itemTarget as IAchievementTarget;
        public int Amount => _itemPrize.Amount;
        public string NameQuest => _nameQuest;
        public string TextTask => _textTask;
        public int MaxValueOnTask => _maxValueOnTask;
    }
}