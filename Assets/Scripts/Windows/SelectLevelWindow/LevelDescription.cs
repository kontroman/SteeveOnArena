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
            difficultyText.text = _config.Difficulty.ToString();

            foreach (var item in _config.AvailableResources)
            {
                var resource = Instantiate(resourcePrefab, availableTransform).GetComponent<BuildingPriceElement>();
                resource.Setup(item);
            }

            foreach (var item in _config.RewardResources)
            {
                var resource = Instantiate(resourcePrefab, rewardTransform).GetComponent<BuildingPriceElement>();
                resource.Setup(item.Item, item.Amount);
            }

            startButton.onClick.AddListener(StartLevel);
        }

        private void StartLevel()
        {
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
                .Then(() => WeatherManager.Instance.ApplyLevelPreset(_config.WeatherPreset))
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

    }
}
