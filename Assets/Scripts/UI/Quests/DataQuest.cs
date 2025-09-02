using MineArena.Items;
using UnityEngine;

namespace UI.Quests
{
    [System.Serializable]
    public class DataQuest
    {
        [SerializeField] private ItemConfig _itemConfig;
        [SerializeField] private int _amountInStack;
        [SerializeField] private string _textTask;
        [SerializeField] private int _maxValueOnTask;

        public ItemConfig ItemConfig => _itemConfig;
        public int Amount => _amountInStack < 0 ? 1 : _amountInStack;
        public string TextTask => _textTask;
        public int MaxValueOnTask => _maxValueOnTask;
    }
}