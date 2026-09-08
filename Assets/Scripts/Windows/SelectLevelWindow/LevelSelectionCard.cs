using System;
using MineArena.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows.SelectLevel
{
    public sealed class LevelSelectionCard : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image frame;
        [SerializeField] private Image icon;
        [SerializeField] private GameObject lockedIcon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private TMP_Text number;
        [SerializeField] private Sprite normalFrame;
        [SerializeField] private Sprite selectedFrame;
        private bool _unlocked;

        public void Bind(LevelConfig config, int index, bool unlocked, Action<int> select)
        {
            title.text = config.DisplayName;
            _unlocked = unlocked;
            title.color = unlocked ? new Color32(81, 71, 55, 255) : new Color32(224, 219, 205, 255);
            subtitle.color = number.color = unlocked ? new Color32(133, 119, 94, 255) : new Color32(191, 187, 174, 255);
            number.text = (index + 1).ToString("00");
            subtitle.text = unlocked ? LevelSelectionView.DifficultyLabel(config.Difficulty) : "Закрыт";
            icon.sprite = config.LevelIcon;
            icon.enabled = icon.sprite != null;
            icon.color = unlocked ? Color.white : new Color(0.35f, 0.35f, 0.35f, 1f);
            lockedIcon.SetActive(!unlocked);
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => select(index));
        }

        public void SetSelected(bool selected)
        {
            frame.sprite = selected ? selectedFrame : normalFrame;
            frame.color = _unlocked ? Color.white : selected ? new Color(0.48f, 0.49f, 0.44f) : new Color(0.43f, 0.43f, 0.41f);
        }
    }
}
