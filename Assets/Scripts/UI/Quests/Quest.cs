using MineArena.Items;
using MineArena.Messages;
using MineArena.UI.FortuneWheel;
using UnityEngine;

namespace UI.Quests
{
    public class Quest
    {
        private readonly float _initialValue = 0;

        private readonly float _maxValueProgress;
        private float _currentValueProgress;
        private bool _isCompleted;
        private bool _canTakePrize;

        private readonly ItemPrize _itemPrize;
        private ItemConfig _itemTarget;

        public float CurrentValueProgress => _currentValueProgress;
        public float MaxValueProgress => _maxValueProgress;
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
            _canTakePrize = false;
            _isCompleted = false;
        }

        public void ChangeCurrentValue(float value)
        {
            _currentValueProgress += value;

            if (Mathf.Approximately(_currentValueProgress, 1))
            {
                //Debug.Log(1);
                QuestMessages.QuestBegun.Publish(this);
            }

            if (Mathf.Approximately(_currentValueProgress, _maxValueProgress))
            {
                _canTakePrize = true;
                QuestMessages.PrizeTake.Publish();
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