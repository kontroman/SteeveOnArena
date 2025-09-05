using MineArena.Items;
using MineArena.UI.FortuneWheel;
using UnityEngine;

namespace UI.Quests
{
    [System.Serializable]
    public class DataQuest
    {
        [SerializeField] private ItemPrize _itemPrize;
        [SerializeField] private ItemConfig _itemNeedToGet;
        [SerializeField] private string _textTask;
        [SerializeField] private int _maxValueOnTask;

        public ItemPrize ItemPrize => _itemPrize;
        public ItemConfig ItemNeedToGet => _itemNeedToGet;
        public int Amount => _itemPrize.Amount;
        public string TextTask => _textTask;
        public int MaxValueOnTask => _maxValueOnTask;
    }
}