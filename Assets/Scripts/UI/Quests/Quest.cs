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
    public class Quest : MonoBehaviour, IProgressBar
    {
        [SerializeField] private Button _button;
        [SerializeField] private ProgressQuestBar _progressBar;
        [SerializeField] private TextMeshProUGUI _completeText;
        [SerializeField] private TextMeshProUGUI _name;

        private readonly float _initialValue = 0;
        private ItemPrize _itemPrize;
        private ItemConfig _itemTarget;

        public event Action<float, float> OnValueChanged;

        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }
        public ItemPrize ItemPrize => _itemPrize;
        public ItemConfig ItemTarget => _itemTarget;

        public void Construct(int dataMaxValueOnTask, ItemPrize itemPrize, ItemConfig itemTarget)
        {
            MaxValue = dataMaxValueOnTask;
            UpdateProgressBar(_initialValue);
            _itemTarget = itemTarget;
            _itemPrize = itemPrize;
            _itemPrize.Construct();
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
            }
        }

        public void TransferPrize()
        {
            _itemPrize.GiveTo();
            _button.gameObject.SetActive(false);
            ShowMessageCompleted();
        }

        private void UpdateProgressBar(float value)
        {
            CurrentValue = value;
            OnValueChanged?.Invoke(CurrentValue, MaxValue);
        }

        private void ShowMessageCompleted()
        {
            _completeText.gameObject.SetActive(true);
            _completeText.text = Constants.Quest.QuestMessageComplete;
            QuestMessages.QuestCompleted.Publish(this);
        }
    }
}