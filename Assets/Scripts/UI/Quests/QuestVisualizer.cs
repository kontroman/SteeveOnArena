using System;
using MineArena.Basics;
using MineArena.Game.UI;
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

        private Quest _quest;

        public event Action<float, float> OnValueChanged;

        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }
        public Quest MyQuest => _quest;

        public void Construct(Quest quest)
        {
            _quest = quest;
            MaxValue = quest.MaxValueProgress;
            _name.text = quest.Data.NameQuest;
            _task.text = quest.Data.TextTask;
            _button.gameObject.SetActive(false);
            _completeText.gameObject.SetActive(false);
        }

        public void ChangeCurrentValue()
        {
            CurrentValue = _quest.CurrentValueProgress;
            
            if (CurrentValue < MaxValue)
            {
                OnValueChanged?.Invoke(CurrentValue, MaxValue);
            }
            else
            {
                _progressBar.gameObject.SetActive(false);
                _button.gameObject.SetActive(true);
            }
        }

        public void ShowMessageCompleted()
        {
            _button.gameObject.SetActive(false);
            _quest.TransferPrize();
            _completeText.gameObject.SetActive(true);
            _completeText.text = Constants.Quest.QuestMessageComplete;
        }
    }
}