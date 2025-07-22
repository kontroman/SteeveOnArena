using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using DG.Tweening;
using MineArena.Windows.SelectLevel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows
{
    public class SelectLevelWindow : BaseWindow
    {
        [SerializeField] private List<Button> buttons = new List<Button>();
        [SerializeField] private GameObject LevelInfoPrefab;

        private int currentSelectedLevel = -1;
        private GameObject currentLevelInfoPrefab;

        public void OnSelectLevelButtonClicked(int level) => ShowLevelDetails(level);

        private void Start()
        {
            Debug.Log("Это ненужная хуйня");

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
            Debug.Log("Это нужная хуйня");

            if (currentSelectedLevel == level) return;

            currentSelectedLevel = level;

            if (currentLevelInfoPrefab)
            {
                var cachedPrefab = currentLevelInfoPrefab;
                cachedPrefab.GetComponent<RectTransform>().DOAnchorPosY(-1500, 0.5f)
                    .OnComplete(() => Destroy(cachedPrefab));
            }

            currentLevelInfoPrefab = Instantiate(LevelInfoPrefab, transform);
            RectTransform rectTransform = currentLevelInfoPrefab.GetComponent<RectTransform>();

            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, 1000);

            rectTransform.DOAnchorPosY(0, 0.5f);

            currentLevelInfoPrefab.GetComponent<LevelDescription>().Initialize(GameRoot.GameConfig.Levels[currentSelectedLevel]);
        }
    }
}