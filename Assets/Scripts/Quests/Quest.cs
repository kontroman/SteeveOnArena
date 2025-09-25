using MineArena.Items;
using MineArena.Messages;
using MineArena.UI.FortuneWheel;
using Structs;

namespace Quests
{
    public class Quest
    {
        private readonly int _initialValue = 0;
        private readonly int _maxValueProgress;
        private readonly ItemPrize _itemPrize;

        private int _currentValueProgress;
        private bool _isCompleted;
        private ItemConfig _itemTarget;
        private bool _canTakePrize;

        public int CurrentValueProgress => _currentValueProgress;
        public int MaxValueProgress => _maxValueProgress;
        public bool IsCompleted => _isCompleted;
        public bool CanTakePrize => _canTakePrize;
        public DataQuest Data { get; private set; }

        public Quest(DataQuest data)
        {
            Data = data;
            _currentValueProgress = _initialValue;
            _maxValueProgress = data.MaxValueOnTask;
            _itemPrize = data.ItemPrize;
            _itemPrize.Construct();
            _isCompleted = false;
            _canTakePrize = false;
        }

        public void ChangeCurrentValue(int value)
        {
            _currentValueProgress += value;

            if (_currentValueProgress == 1)
                QuestMessages.QuestBegun.Publish(this);

            if (_currentValueProgress == _maxValueProgress)
            {
                _canTakePrize = true;
                QuestMessages.PrizeTake.Publish(this);
            }
        }

        public void TransferPrize()
        {
            _isCompleted = true;
            QuestMessages.QuestCompleted.Publish(this);
            _itemPrize.GiveTo();
        }
    }
}