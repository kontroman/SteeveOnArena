using MineArena.Levels;
using System.Collections.Generic;
using MineArena.UI.FortuneWheel;
using Structs;
using UnityEngine;

using UnityEngine.Serialization;

using Devotion.SDK.Confgs;
using MineArena.Items;
using MineArena.Buildings;


namespace MineArena.Structs
{
    [CreateAssetMenu(menuName = nameof(GameConfig))]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] private List<LevelConfig> levels;
        [SerializeField] private List<ItemPrize> _prizes;
        [SerializeField] private List<DataQuest> _dataQuests;
        [SerializeField] private LocalizationConfig localizationConfig;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private BuildingsDatabase buildingsDatabase;

        public List<LevelConfig> Levels { get { return levels; } }
        public List<ItemPrize> Prizes { get { return _prizes; } }
        public List<DataQuest> DataQuests { get { return _dataQuests; } }
        public LocalizationConfig LocalizationConfig { get { return localizationConfig; } }
        public ItemDatabase ItemDatabase { get { return itemDatabase; } }
        public BuildingsDatabase BuildingsDatabase { get { return buildingsDatabase; } }

    }
}