using MineArena.UI.FortuneWheel;
using Quests;
using UnityEngine;

namespace Structs
{
    [System.Serializable]
    public class DataQuest
    {
        [SerializeField] private ItemPrize _itemPrize;
        [SerializeField] private ScriptableObject  _itemTarget;
        [SerializeField] private string _nameQuest;
        [SerializeField] private string _textTask;
        [SerializeField] private int _maxValueOnTask;

        public ItemPrize ItemPrize => _itemPrize;
        public IQuestTarget ItemTarget => _itemTarget as IQuestTarget;
        public int Amount => _itemPrize.Amount;
        public string NameQuest => _nameQuest;
        public string TextTask => _textTask;
        public int MaxValueOnTask => _maxValueOnTask;
    }
}