using MineArena.Items;
using MineArena.UI.FortuneWheel;
using UnityEngine;

namespace Structs
{
    [System.Serializable]
    public class DataQuest
    {
        [SerializeField] private ItemPrize _itemPrize;
        [SerializeField] private ItemConfig _itemTarget;
        [SerializeField] private string _nameQuest;
        [SerializeField] private string _textTask;
        [SerializeField] private int _maxValueOnTask;

        public ItemPrize ItemPrize => _itemPrize;
        public ItemConfig ItemTarget => _itemTarget;
        public int Amount => _itemPrize.Amount;
        public string NameQuest => _nameQuest;
        public string TextTask => _textTask;
        public int MaxValueOnTask => _maxValueOnTask;
    }
}