using System;
using Devotion.SDK.Controllers;
using MineArena.Managers;

namespace MineArena.Cosmetics
{
    public static class SkinService
    {
        public static event Action Changed;
        public static CosmeticsProgress Progress => GameRoot.PlayerProgress?.CosmeticsProgress;
        public static bool Owns(string id) => Progress?.Owns(id) == true;
        public static void NotifyChanged() => Changed?.Invoke();

        public static bool Buy(string id)
        {
            var skin = SkinCatalog.Load()?.Find(id);
            var player = GameRoot.PlayerProgress;
            var inventory = GameRoot.GetManager<InventoryManager>();
            if (skin == null || skin.Source != SkinSource.Currency || skin.Price <= 0 || skin.Currency == null ||
                player == null || inventory == null || Owns(id) ||
                GameRoot.GameConfig.ItemDatabase.GetItemConfig(skin.Currency.Name) != skin.Currency ||
                inventory.GetItemAmount(skin.Currency.Name) < skin.Price) return false;
            // Ownership is recorded before TrySpendExact publishes the save event.
            player.CosmeticsProgress.Unlock(id);
            inventory.TrySpendExact(skin.Currency, skin.Price);
            Changed?.Invoke();
            return true;
        }

        public static bool Equip(string id)
        {
            if (SkinCatalog.Load()?.Find(id) == null || Progress?.Equip(id) != true) return false;
            Progress.Save(); Changed?.Invoke();
            MineArena.Networking.NetworkClientManager.Instance?.SendCustomization();
            return true;
        }

        public static bool ClaimQuest(string id)
        {
            var skin = SkinCatalog.Load()?.Find(id);
            if (skin == null || skin.Source != SkinSource.Quest || Progress == null ||
                GameRoot.PlayerProgress.AchievementProgress == null ||
                !GameRoot.PlayerProgress.AchievementProgress.Achievements.TryGetValue(skin.QuestId, out var quest) || !quest.IsCompleted) return false;
            return Grant(id);
        }

        public static void GrantQuestRewards(int questId)
        {
            var catalog = SkinCatalog.Load();
            if (catalog == null) return;
            foreach (var skin in catalog.Skins)
                if (skin.Source == SkinSource.Quest && skin.QuestId == questId) ClaimQuest(skin.Id);
        }

        // Call only from the successful reward callback, never from the shop button.
        public static bool GrantReward(string rewardId)
        {
            var catalog = SkinCatalog.Load(); bool changed = false;
            if (catalog == null || string.IsNullOrWhiteSpace(rewardId)) return false;
            foreach (var skin in catalog.Skins)
                if (skin.Source == SkinSource.Reward && skin.RewardId == rewardId) changed |= Grant(skin.Id);
            return changed;
        }

        private static bool Grant(string id)
        {
            if (Progress?.Unlock(id) != true) return false;
            Progress.Save(); Changed?.Invoke(); return true;
        }
    }
}
