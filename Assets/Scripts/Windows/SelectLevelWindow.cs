using System;
using Devotion.SDK.Async;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using MineArena.Basics;
using MineArena.Controllers;
using MineArena.Levels;
using MineArena.Managers;
using UnityEngine;
using MineArena.Windows.SelectLevel;

namespace MineArena.Windows
{
    public class SelectLevelWindow : BaseWindow
    {
        private bool _isLoading;
        [SerializeField] private LevelSelectionView view;

        private void OnEnable()
        {
            if (!Application.isPlaying || view == null) return;
            view.StartRequested += OnSelectLevelButtonClicked;
            view.CloseRequested += OnCloseClick;
            view.Refresh(GameRoot.GameConfig != null ? GameRoot.GameConfig.Levels : null,
                TutorialService.Active ? 0 : GameRoot.PlayerProgress?.LevelsProgress?.HighestUnlockedLevelIndex ?? 0);
        }

        private void OnDisable()
        {
            if (view == null) return;
            view.StartRequested -= OnSelectLevelButtonClicked;
            view.CloseRequested -= OnCloseClick;
        }

        public void SelectLevel(int index)
        {
            if (view != null) view.Select(index);
        }

        public override void CloseWindow()
        {
            GameRoot.UIManager.CloseWindow<SelectLevelWindow>();
        }

        public void OnCloseClick()
        {
            CloseWindow();
        }

        public void OnSelectLevelButtonClicked(int levelIndex)
        {
            if (TutorialService.BlocksInput) return;
            if (!TutorialService.AllowLevel(levelIndex)) return;
            if (_isLoading || !TryGetLevelConfig(levelIndex, out var config))
                return;

            StartLevel(config);
        }

        private static bool TryGetLevelConfig(int levelIndex, out LevelConfig config)
        {
            config = null;

            var levels = GameRoot.GameConfig != null ? GameRoot.GameConfig.Levels : null;
            if (levels == null || levelIndex < 0 || levelIndex >= levels.Count)
                return false;

            if (!IsLevelUnlocked(levelIndex))
                return false;

            config = levels[levelIndex];
            return config != null && config.LevelPrefab != null;
        }

        private static bool IsLevelUnlocked(int levelIndex)
        {
            var progress = GameRoot.PlayerProgress != null ? GameRoot.PlayerProgress.LevelsProgress : null;
            return progress != null ? progress.IsLevelUnlocked(levelIndex) : levelIndex == 0;
        }

        private void StartLevel(LevelConfig config)
        {
            _isLoading = true;

            GameRoot.UIManager.CloseAllWindows();

            var loadingWindow = (LoadingWindow)GameRoot.UIManager.OpenWindow<LoadingWindow>();
            LevelController levelController = null;

            loadingWindow.SetProgressValue(0.3f)
                .Then(() => GameRoot.GetManager<UnitySceneLoader>().LoadSceneAsync(Constants.SceneNames.GameplayScene))
                .Then(() =>
                {
                    levelController = FindObjectOfType<LevelController>();
                    if (levelController == null)
                    {
                        throw new InvalidOperationException("LevelController not found in scene after loading gameplay.");
                    }

                    return levelController.InitLevel(config);
                })
                .Then(() => loadingWindow.SetProgressValue(0.8f))
                .Then(() => levelController.GenerateLevel())
                .Then(() => WeatherManager.Instance.ApplyLevelPreset(config.GetRandomWeatherPreset()))
                .Then(() => levelController.GenerateOres())
                .Then(() => loadingWindow.SetProgressValue(0.9f))
                .Then(() => loadingWindow.SetProgressValue(1f))
                .Finally(() =>
                {
                    GameRoot.UIManager.CloseWindow<LoadingWindow>();
                    _isLoading = false;
                });
        }
    }
}
