using System;
using Achievements;
using Devotion.SDK.Services.Localization;
using MineArena.Game.UI;
using MineArena.Items;
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
        [SerializeField] private TextMeshProUGUI _buttonText;

        private const string AchievementMessageComplete = "completed";
        private const string ButtonTextKey = "achievementButtonGet";

        private Achievement _achievement;

        public event Action<float, float> OnValueChanged;

        public float MaxValue { get; private set; }
        public float CurrentValue { get; private set; }
        public Achievement MyAchievement => _achievement;

        public void Construct(Achievement achievement)
        {
            _achievement = achievement;
            MaxValue = achievement.MaxValueProgress;
            _name.text = string.Format(LocalizationService.GetLocalizedText(achievement.Data.NameAchievementKey));
            _task.text = string.Format(LocalizationService.GetLocalizedText(achievement.Data.TextTaskKey),
                achievement.Data.MaxValueOnTask, achievement.Data.ItemTarget.Name);
            _button.gameObject.SetActive(false);
            _completeText.gameObject.SetActive(false);
            _completeText.text = LocalizationService.GetLocalizedText(AchievementMessageComplete);
            _buttonText.text = LocalizationService.GetLocalizedText(ButtonTextKey);
        }

        public void ChangeCurrentValue()
        {
            CurrentValue = _achievement.CurrentValueProgress;

            if (CurrentValue < MaxValue)
                OnValueChanged?.Invoke(CurrentValue, MaxValue);

            if (_achievement.CanTakePrize && !_achievement.IsCompleted)
                ShowButtonGetPrize();

            if (_achievement.CanTakePrize && _achievement.IsCompleted)
                ShowTextCompleted();
        }

        public void ShowMessageCompleted()
        {
            _button.gameObject.SetActive(false);
            _achievement.TransferPrize();
            _completeText.gameObject.SetActive(true);
        }

        private void ShowTextCompleted()
        {
            _button.gameObject.SetActive(false);
            _progressBar.gameObject.SetActive(false);
            _completeText.gameObject.SetActive(true);
        }

        private void ShowButtonGetPrize()
        {
            _progressBar.gameObject.SetActive(false);
            _button.gameObject.SetActive(true);
        }
    }
}