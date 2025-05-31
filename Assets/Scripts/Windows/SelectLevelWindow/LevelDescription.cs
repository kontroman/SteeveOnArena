using MineArena.Levels;
using TMPro;
using UnityEngine;

namespace MineArena.Windows.SelectLevel
{
    public class LevelDescription : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI difficultyText;

        [SerializeField] private Transform availableTransform;
        [SerializeField] private Transform rewardTransform;

        private LevelConfig config;

        public void Initialize(LevelConfig config)
        {
            this.config = config;
        }

        private void SetupUI()
        {

        }
    }
}