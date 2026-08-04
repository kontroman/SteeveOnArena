using System;
using System.Collections.Generic;
using MineArena.Items;
using UnityEngine;

namespace Devotion.SDK.DailyReward
{
    [CreateAssetMenu(fileName = "DailyRewardsConfig", menuName = "Devotion/Daily Rewards Config")]
    public class DailyRewardConfig : ScriptableObject
    {
        [SerializeField] private List<DailyRewardEntry> rewards = new();

        public IReadOnlyList<DailyRewardEntry> Rewards => rewards;
        public int RewardsCount => rewards?.Count ?? 0;

        public bool HasRewards => RewardsCount > 0;

        public DailyRewardEntry GetReward(int index)
        {
            if (rewards == null || index < 0 || index >= rewards.Count)
                return null;

            return rewards[index];
        }
    }

    [Serializable]
    public class DailyRewardEntry
    {
        [SerializeField] private ItemConfig itemConfig;
        [SerializeField, Min(1)] private int amount = 1;

        public ItemConfig ItemConfig => itemConfig;
        public int Amount => Mathf.Max(1, amount);
        public string ItemId => itemConfig != null ? itemConfig.Name : string.Empty;
        public string DisplayName => itemConfig != null ? itemConfig.Name : "Empty";
        public Sprite Icon => itemConfig != null ? itemConfig.Icon : null;
        public bool IsValid => itemConfig != null && !string.IsNullOrWhiteSpace(itemConfig.Name);
    }
}
