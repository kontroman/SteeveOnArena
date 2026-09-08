using System;
using System.Collections.Generic;
using MineArena.Items;
using UnityEngine;

namespace MineArena.UI
{
    [CreateAssetMenu(menuName = "MineArena/Playtime Rewards")]
    public sealed class PlaytimeRewardsConfig : ScriptableObject
    {
        public List<PlaytimeReward> Rewards = new List<PlaytimeReward>();
    }
    [Serializable]
    public sealed class PlaytimeReward
    {
        [Min(1)] public int Minutes = 5;
        public ItemConfig Item;
        [Min(1)] public int Amount = 1;
    }
}
