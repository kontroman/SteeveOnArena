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
        [SerializeField] private MineArena.Cosmetics.CosmeticsProgress cosmeticsProgress;
        public MineArena.Cosmetics.CosmeticsProgress CosmeticsProgress => cosmeticsProgress ??= new MineArena.Cosmetics.CosmeticsProgress();
        [SerializeField] private MineArena.Managers.TutorialProgress tutorialProgress;
        public MineArena.Managers.TutorialProgress TutorialProgress => tutorialProgress ??= new MineArena.Managers.TutorialProgress();

        [SerializeField] private InventoryProgress inventoryProgress;
        [SerializeField] private BuildingProgress buildingsProgress;
        [SerializeField] private LevelsProgress levelsProgress;
        [SerializeField] private PurchasesProgress purchasesProgress;
        [SerializeField] private AdsProgress adsProgress;
        [SerializeField] private LuckyWheelProgress luckyWheelProgress;
        [SerializeField] private DailyRewardProgress dailyRewardProgress;
        [SerializeField] private PlayerDataProgress playerDataProgress;
        [SerializeField] private AchievementProgress achievementProgress;
        [SerializeField] private PlaytimeGiftProgress playtimeGiftProgress;
        public PlaytimeGiftProgress PlaytimeGiftProgress => playtimeGiftProgress ??= new PlaytimeGiftProgress();

        public InventoryProgress InventoryProgress
        {
            get
            {
                if (inventoryProgress == null)
                    inventoryProgress = new InventoryProgress();

                return inventoryProgress;
            }
        }
        public BuildingProgress BuildingProgress => buildingsProgress;
        public LevelsProgress LevelsProgress
        {
            get
            {
                if (levelsProgress == null)
                    levelsProgress = new LevelsProgress();

                return levelsProgress;
            }
        }
        public PurchasesProgress PurchasesProgress => purchasesProgress ??= new PurchasesProgress();
        public AdsProgress AdsProgress => adsProgress;
        public LuckyWheelProgress LuckyWheelProgress
        {
            get
            {
                if (luckyWheelProgress == null)
                    luckyWheelProgress = new LuckyWheelProgress();

                return luckyWheelProgress;
            }
        }
        public DailyRewardProgress DailyRewardProgress
        {
            get
            {
                if (dailyRewardProgress == null)
                    dailyRewardProgress = new DailyRewardProgress();

                return dailyRewardProgress;
            }
        }
        public PlayerDataProgress PlayerDataProgress
        {
            get
            {
                if (playerDataProgress == null)
                    playerDataProgress = new PlayerDataProgress();

                return playerDataProgress;
            }
        }
        public AchievementProgress AchievementProgress => achievementProgress;

        public PlayerProgress(string saveId)
        {
            _id = saveId;

            inventoryProgress = new InventoryProgress();
            buildingsProgress = new BuildingProgress();
            levelsProgress = new LevelsProgress();
            purchasesProgress = new PurchasesProgress();
            adsProgress = new AdsProgress();
            luckyWheelProgress = new LuckyWheelProgress();
            dailyRewardProgress = new DailyRewardProgress();
            achievementProgress = new AchievementProgress();
            playerDataProgress = new PlayerDataProgress();
            achievementProgress = new AchievementProgress();
        }
    }
}
