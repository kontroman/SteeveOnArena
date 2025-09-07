using MineArena.Items;
using MineArena.UI.FortuneWheel;

namespace UI.Quests
{
    public class Quest
    {
        private readonly float _initialValue = 0;

        private float _currentValueProgress;
        private float _maxValueProgress;

        private ItemConfig _itemTarget;
        private ItemPrize _itemPrize;
        private DataQuest _data;

        public float InitialValue => _initialValue;
        public float CurrentValueProgress => _currentValueProgress;
        public float MaxValueProgress => _maxValueProgress;
        public ItemPrize ItemPrize => _itemPrize;
        public DataQuest Data => _data;

        public Quest(DataQuest data)
        {
            _data = data;
            _currentValueProgress = _initialValue;
            _maxValueProgress = data.MaxValueOnTask;
            _itemPrize = data.ItemPrize;
        }

        public void ChangeCurrentValue(float value)
        {
            _currentValueProgress += value;
        }

        public void TransferPrize()
        {
            _itemPrize.GiveTo();
        }
    }
}