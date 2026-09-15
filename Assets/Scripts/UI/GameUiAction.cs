using Devotion.SDK.Controllers;
using Devotion.SDK.DailyReward;
using Devotion.SDK.UI;
using MineArena.Windows;
using MineArena.Windows.Crafting;
using UnityEngine;
using UnityEngine.UI;
using Windows;

namespace MineArena.UI
{
    public enum GameUiDestination { Inventory, Crafting, Levels, Shop, Daily, Playtime, Achievements, Wheel, Settings }
    [RequireComponent(typeof(MineArena.UI.AnimatedButton))]
    public sealed class GameUiAction : MonoBehaviour
    {
        [SerializeField] private GameUiDestination destination;
        public GameUiDestination Destination => destination;
        private void Update()
        {
            GetComponent<Button>().interactable = MineArena.Managers.TutorialService.AllowHud(destination);
        }
        private void Awake() => GetComponent<Button>().onClick.AddListener(Open);
        public void Open()
        {
            if (GameRoot.UIManager == null || !MineArena.Managers.TutorialService.AllowHud(destination)) return;
            switch (destination)
            {
                case GameUiDestination.Inventory: GameRoot.UIManager.OpenWindow<InventoryWindow>(); break;
                case GameUiDestination.Crafting: CraftingWindow.Open(); break;
                case GameUiDestination.Levels: GameRoot.UIManager.OpenWindow<SelectLevelWindow>(); break;
                case GameUiDestination.Shop: GameRoot.UIManager.OpenWindow<ShopWindow>(); break;
                case GameUiDestination.Daily: GameRoot.GetManager<DailyRewardManager>()?.OpenRewards(); break;
                case GameUiDestination.Playtime: GameRoot.UIManager.OpenWindow<PlaytimeGiftWindow>(); break;
                case GameUiDestination.Achievements: GameRoot.UIManager.OpenWindow<WindowAchievements>(); break;
                case GameUiDestination.Wheel: GameRoot.UIManager.OpenWindow<FortuneWheelWindow>(); break;
                case GameUiDestination.Settings: GameRoot.UIManager.OpenWindow<SettingsWindow>(); break;
            }
            MineArena.Managers.TutorialService.HudOpened(destination);
        }
    }
}
