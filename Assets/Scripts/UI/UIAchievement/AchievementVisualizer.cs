using System;
using Achievements;
using MineArena.Game.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIAchievement
{
    public class AchievementVisualizer : MonoBehaviour, IProgressBar
    {
        [SerializeField] private Button _button;
        [SerializeField] private ProgressQuestBar _progressBar;
        [SerializeField] private TextMeshProUGUI _completeText;
        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private TextMeshProUGUI _task;

        private const string QuestMessageComplete = "Complete";

        private Achievement _achievement;
        private bool _questCompleted;

        public event Action<float, float> OnValueChanged;

        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }
        public Achievement MyAchievement => _achievement;

        public void Construct(Achievement achievement)
        {
            _achievement = achievement;
            MaxValue = achievement.MaxValueProgress;
            _name.text = achievement.Data.NameQuest;
            _task.text = achievement.Data.TextTask;
            _button.gameObject.SetActive(false);
            _completeText.gameObject.SetActive(false);
            _questCompleted = false;
        }

        public void ChangeCurrentValue()
        {
            CurrentValue = _achievement.CurrentValueProgress;

            if (CurrentValue < MaxValue)
            {
                OnValueChanged?.Invoke(CurrentValue, MaxValue);
            }
            else if (_questCompleted == false)
            {
                _progressBar.gameObject.SetActive(false);
                _button.gameObject.SetActive(true);
                _questCompleted = true;
            }
        }

        public void ShowMessageCompleted()
        {
            _button.gameObject.SetActive(false);
            _achievement.TransferPrize();
            _completeText.gameObject.SetActive(true);
            _completeText.text = QuestMessageComplete;
        }
    }
}