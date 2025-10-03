using MineArena.Items;
using System;
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
        [SerializeField] private List<ItemConfig> availableResources;
        [SerializeField] private List<LevelRewards> rewardResources;
        [SerializeField] private GameObject levelPrefab;
        [SerializeField] private Quaternion levelPrefabRotation;
        [SerializeField] private WeatherPreset weatherPreset;

        public Sprite LevelIcon {  get { return levelIcon; } }
        public LevelDifficulty Difficulty { get { return difficulty; } }
        public LevelSettings Settings { get { return settings; } }
        public List<ItemConfig> AvailableResources { get { return availableResources; } }
        public List<LevelRewards> RewardResources { get { return rewardResources; } }
        public GameObject LevelPrefab { get { return levelPrefab; } }
        public Quaternion LevelPrefabRotation { get { return levelPrefabRotation; } }
        public WeatherPreset WeatherPreset { get { return weatherPreset; } }
    }

    [Serializable]
    public class LevelRewards
    {
        public ItemConfig Item;
        public int Amount;
    }
}