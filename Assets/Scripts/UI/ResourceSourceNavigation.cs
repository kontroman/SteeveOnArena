using System;
using System.Linq;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using MineArena.Items;
using MineArena.Managers;
using MineArena.Windows;
using MineArena.Windows.Crafting;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    public static class ResourceSourceNavigation
    {
        // Additional sources (shops, quests, etc.) can supply their own navigation.
        public static Func<ItemConfig, Func<bool>> CustomSourceResolver { get; set; }

        public static void Bind(GameObject row, ItemConfig item, BaseWindow owner)
        {
            var button = row.GetComponent<Button>() ?? row.AddComponent<Button>();
            var graphic = row.GetComponent<Image>() ?? row.AddComponent<Image>();
            if (graphic.sprite == null) graphic.color = Color.clear;
            graphic.raycastTarget = true;
            button.targetGraphic = graphic;
            button.onClick.RemoveAllListeners();
            button.interactable = Resolve(item, out _) != null;
            button.onClick.AddListener(() =>
            {
                if (TutorialService.Active) return;
                var open = Resolve(item, out var opensCrafting);
                if (open != null && open() && owner != null &&
                    !(owner is CraftingWindow && opensCrafting)) owner.CloseWindow();
            });
        }



        private static Func<bool> Resolve(ItemConfig item, out bool opensCrafting)
        {
            opensCrafting = false;
            if (item == null || GameRoot.Instance == null) return null;
            var custom = CustomSourceResolver?.Invoke(item);
            if (custom != null) return custom;
            var recipe = new ProjectCraftingAdapter().BuildCatalog()
                .SelectMany(category => category.Recipes).FirstOrDefault(entry => entry.Item == item);
            if (recipe != null)
            {
                opensCrafting = true;
                return () =>
                {
                    return CraftingWindow.OpenItem(item, recipe.SourceBuilding) != null;
                };
            }
            var levels = GameRoot.GameConfig != null ? GameRoot.GameConfig.Levels : null;
            if (levels == null) return null;
            for (int i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                if (level == null || level.LevelPrefab == null) continue;
                if (level.AvailableResources?.Contains(item) != true &&
                    level.RewardResources?.Any(reward => reward != null && reward.Item == item && reward.Amount > 0) != true) continue;
                int index = i;
                return () =>
                {
                    var window = GameRoot.UIManager.OpenWindow<SelectLevelWindow>() as SelectLevelWindow;
                    if (window == null) return false;
                    window.SelectLevel(index);
                    return true;
                };
            }
            return null;
        }
    }
}
