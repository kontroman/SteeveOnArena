using Devotion.SDK.Async;
using Devotion.SDK.Controllers;
using MineArena.Basics;
using MineArena.Buildings;
using MineArena.Controllers;
using MineArena.Levels;
using MineArena.Managers;
using MineArena.Windows.Elements;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows.SelectLevel
{
    public class LevelDescription : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI difficultyText;

        [SerializeField] private Button startButton;

        [SerializeField] private Transform availableTransform;
        [SerializeField] private Transform rewardTransform;

        [SerializeField] private GameObject resourcePrefab;

        private LevelConfig _config;

        public void Initialize(LevelConfig config)
        {
            this._config = config;

            SetupUI();
        }

        private void SetupUI()
        {
            startButton.onClick.RemoveListener(StartLevel);
            ClearResources(availableTransform);
            ClearResources(rewardTransform);
            startButton.interactable = CanStart();
            if (_config == null) return;
            difficultyText.text = LevelSelectionView.DifficultyLabel(_config.Difficulty);

            if (_config.AvailableResources != null)
            {
                foreach (var item in _config.AvailableResources)
                {
                    if (item == null) continue;
                    var resource = Instantiate(resourcePrefab, availableTransform).GetComponent<BuildingPriceElement>();
                    resource.Setup(item);
                }
            }

            if (_config.RewardResources != null)
            {
                foreach (var item in _config.RewardResources)
                {
                    if (item == null || item.Item == null || item.Amount <= 0) continue;
                    var resource = Instantiate(resourcePrefab, rewardTransform).GetComponent<BuildingPriceElement>();
                    resource.Setup(item.Item, item.Amount);
                }
            }

            startButton.onClick.AddListener(StartLevel);
        }

        private void StartLevel()
        {
            if (!MineArena.Managers.TutorialService.AllowLevel(GameRoot.GameConfig.Levels.IndexOf(_config))) return;
            if (!CanStart()) return;
            startButton.interactable = false;

            GameRoot.UIManager.CloseAllWindows();

            LoadingWindow loadingWindow = (LoadingWindow)GameRoot.UIManager.OpenWindow<LoadingWindow>();
            LevelController levelController = null;

            loadingWindow.SetProgressValue(0.3f)
                .Then(() => GameRoot.GetManager<UnitySceneLoader>().LoadSceneAsync(Constants.SceneNames.GameplayScene))
                //.Then(() => GameRoot.GameConfig.);
                .Then(() =>
                {
                    levelController = FindObjectOfType<LevelController>();
                    if (levelController == null)
                    {
                        throw new InvalidOperationException("LevelController not found in scene after loading gameplay.");
                    }

                    return levelController.InitLevel(_config);
                })
                .Then(() => loadingWindow.SetProgressValue(0.8f))
                .Then(() => levelController.GenerateLevel())
                .Then(() => WeatherManager.Instance.ApplyLevelPreset(_config.GetRandomWeatherPreset()))
                .Then(() => levelController.GenerateOres())
                .Then(() => loadingWindow.SetProgressValue(0.9f))
                //.Then(() => GameRoot.LevelController)
                .Then(() => loadingWindow.SetProgressValue(1f))
                .Finally(() =>
                {
                    GameRoot.UIManager.CloseWindow<LoadingWindow>();
                    startButton.interactable = true;
                });
        }

        private bool CanStart()
        {
            var levels = GameRoot.GameConfig != null ? GameRoot.GameConfig.Levels : null;
            int index = levels != null ? levels.IndexOf(_config) : -1;
            return index >= 0 && _config != null && _config.LevelPrefab != null &&
                (GameRoot.PlayerProgress?.LevelsProgress?.IsLevelUnlocked(index) ?? index == 0);
        }

        private static void ClearResources(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                root.GetChild(i).gameObject.SetActive(false);
                Destroy(root.GetChild(i).gameObject);
            }
        }

    }
}
