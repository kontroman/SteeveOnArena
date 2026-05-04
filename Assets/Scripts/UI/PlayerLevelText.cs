using MineArena.Controllers;
using MineArena.PlayerSystem;
using TMPro;
using UnityEngine;

namespace MineArena.Game.UI
{
    [RequireComponent(typeof(TMP_Text))]
    public class PlayerLevelText : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private string _format = "{0}";

        private PlayerExperience _experience;

        private void Awake()
        {
            if (_levelText == null)
                _levelText = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            Player.ExperienceInitialized += SetExperience;
            SetExperience(Player.Instance?.Experience);
        }

        private void OnDisable()
        {
            Player.ExperienceInitialized -= SetExperience;
            SetExperience(null);
        }

        private void UpdateLevel(int level)
        {
            if (_levelText != null)
                _levelText.text = string.Format(_format, level);
        }

        private void SetExperience(PlayerExperience experience)
        {
            if (_experience == experience)
            {
                if (_experience != null)
                    UpdateLevel(_experience.CurrentLevel);

                return;
            }

            if (_experience != null)
                _experience.OnLevelChanged -= UpdateLevel;

            _experience = experience;

            if (_experience == null)
                return;

            _experience.OnLevelChanged += UpdateLevel;
            UpdateLevel(_experience.CurrentLevel);
        }
    }
}
