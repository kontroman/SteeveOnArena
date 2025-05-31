using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows
{
    public class SelectLevelWindow : BaseWindow
    {
        [SerializeField] private List<Button> buttons = new List<Button>();

        public void OnSelectLevelButtonClicked(int level) => ShowLevelDetails(level);

        private void Start()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (i < GameRoot.GameConfig.Levels.Count)
                {
                    buttons[i].image.sprite = GameRoot.GameConfig.Levels[i].LevelIcon;

                    //buttons[i].interactable = GameRoot.GameConfig.Levels[i].IsUnlocked;
                }
            }
        }

        private void ShowLevelDetails(int level)
        {

        }

    }
}