using Devotion.SDK.Services.SaveSystem.Progress;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class PlayerProgress : BaseProgress
    {
        [SerializeField] private string _id;

        [SerializeField] private InventoryProgress inventoryProgress;
        [SerializeField] private BuildingProgress buildingsProgress;
        [SerializeField] private LevelsProgress levelsProgress;
        [SerializeField] private PurchasesProgress purchasesProgress;
        [SerializeField] private AdsProgress adsProgress;
        [SerializeField] private LuckyWheelProgress luckyWheelProgress;
        [SerializeField] private PlayerDataProgress playerDataProgress;
        [SerializeField] private AchievementProgress achievementProgress;

        public InventoryProgress InventoryProgress => inventoryProgress;
        public BuildingProgress BuildingProgress => buildingsProgress;
        public LevelsProgress LevelsProgress => levelsProgress;
        public PurchasesProgress PurchasesProgress => purchasesProgress;
        public AdsProgress AdsProgress => adsProgress;
        public LuckyWheelProgress LuckyWheelProgress => luckyWheelProgress;
        public PlayerDataProgress PlayerDataProgress => playerDataProgress;
        public AchievementProgress AchievementProgress => achievementProgress;

        public PlayerProgress(string saveId)
        {
            _id = saveId;

            inventoryProgress = new InventoryProgress();
            buildingsProgress = new BuildingProgress();
            levelsProgress = new LevelsProgress();
            purchasesProgress = new PurchasesProgress();
            adsProgress = new AdsProgress();
            achievementProgress = new AchievementProgress();
            playerDataProgress = new PlayerDataProgress();
            achievementProgress = new AchievementProgress();
        }
    }
}