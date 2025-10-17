﻿using System.Collections.Generic;
using Achievements;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.Services.Localization;
using Managers;
using MineArena.Basics;
using MineArena.Messages;
using MineArena.Messages.MessageService;
using TMPro;
using UI.UIAchievement;
using UnityEngine;

namespace Windows
{
    public class WindowAchievements : BaseWindow,
        IMessageSubscriber<AchievementMessages.AchievementTargetTaken>
    {
        [SerializeField] private AchievementsConstructor _achievementsConstructor;
        [SerializeField] private TextMeshProUGUI _name;
        

        private List<Achievement> _activeAchievements = new();
        private List<AchievementVisualizer> _achievementVisualizers = new();

        private void Awake()
        {
            _activeAchievements = GameRoot.GetManager<AchievementManager>().GetQuests();
            _achievementVisualizers = _achievementsConstructor.CreateQuestVisualizers(_activeAchievements);
            _name.text = LocalizationService.GetLocalizedText(Constants.AchievementKey.WindowNameKey);
        }

        public void Close() =>
            GameRoot.UIManager.CloseWindow<WindowAchievements>();

        public void OnMessage(AchievementMessages.AchievementTargetTaken message) =>
            UpdateProgressValue();

        private void UpdateProgressValue()
        {
            _activeAchievements = GameRoot.GetManager<AchievementManager>().GetQuests();

            foreach (var visualizer in _achievementVisualizers)
            {
                foreach (var Achievement in _activeAchievements)
                    if (Achievement == visualizer.MyAchievement)
                        visualizer.ChangeCurrentValue();
            }
        }

        private void OnEnable()
        {
            UpdateProgressValue();
            MessageService.Subscribe(this);
        }

        private void OnDisable() =>
            MessageService.Unsubscribe(this);
    }
}