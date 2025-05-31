using MineArena.Items;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Levels
{
    [CreateAssetMenu(menuName = "Levels/" + nameof(LevelConfig))]
    public class LevelConfig : ScriptableObject
    {
        [SerializeField] private Sprite levelIcon;
        [SerializeField] private LevelDifficulty difficulty;
        [SerializeField] private LevelSettings settings;
        [SerializeField] private List<StackableItemConfig> availableResources;
        [SerializeField] private List<StackableItemConfig> rewardResources;
        [SerializeField] private GameObject levelPrefab;

        public Sprite LevelIcon {  get { return levelIcon; } }
        public LevelDifficulty Difficulty { get { return difficulty; } }
        public LevelSettings Settings { get { return settings; } }
        public List<StackableItemConfig> AvailableResources { get { return availableResources; } }
        public List<StackableItemConfig> RewardResources { get { return rewardResources; } }
        public GameObject LevelPrefab { get { return levelPrefab; } }
    }
}