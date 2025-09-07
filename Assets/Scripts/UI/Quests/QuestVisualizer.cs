using System;
using MineArena.Basics;
using MineArena.Game.UI;
using MineArena.Items;
using MineArena.Messages;
using MineArena.UI.FortuneWheel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Quests
{
    public class QuestVisualizer : MonoBehaviour, IProgressBar
    {
        [SerializeField] private Button _button;
        [SerializeField] private ProgressQuestBar _progressBar;
        [SerializeField] private TextMeshProUGUI _completeText;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _task;

        private ItemPrize _itemPrize;
        private ItemConfig _itemTarget;
        private Quest _quest;

        public event Action<float, float> OnValueChanged;

        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }
        public ItemConfig ItemTarget => _itemTarget;
        public Quest MyQuest => _quest;

        public void Construct(Quest quest)
        {
            _quest = quest;
            MaxValue = quest.MaxValueProgress;
            UpdateProgressBar(quest.InitialValue);
            _itemPrize = quest.ItemPrize;
            _itemPrize.Construct();
            _itemTarget = quest.Data.ItemTarget;
            _name.text = quest.Data.NameQuest;
            _task.text = quest.Data.TextTask;
            _button.gameObject.SetActive(false);
            _completeText.gameObject.SetActive(false);
        }

        public void ChangeCurrentValue(float value)
        {
            UpdateProgressBar(value);

            if (CurrentValue >= MaxValue)
            {
                _progressBar.gameObject.SetActive(false);
                _button.gameObject.SetActive(true);
                QuestMessages.PrizeTake.Publish();
            }
        }

        public void ShowMessageCompleted()
        {
            _button.gameObject.SetActive(false);
            _completeText.gameObject.SetActive(true);
            _completeText.text = Constants.Quest.QuestMessageComplete;
        }

        private void UpdateProgressBar(float value)
        {
            CurrentValue = value;
            OnValueChanged?.Invoke(CurrentValue, MaxValue);
        }
    }
}