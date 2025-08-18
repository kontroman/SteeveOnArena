using MineArena.Levels;
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
                var resource = Instantiate(resourcePrefab, availableTransform).GetComponent<Image>();
                resource.sprite = item.Icon;
            }

            foreach (var item in _config.RewardResources)
            {
                var resource = Instantiate(resourcePrefab, rewardTransform).GetComponent<Image>();
                resource.sprite = item.Icon;
            }

            startButton.onClick.AddListener(StartLevel);
        }

        private void StartLevel()
        {

        }
    }
}