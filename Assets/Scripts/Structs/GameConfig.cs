using MineArena.Levels;
using System.Collections.Generic;
using MineArena.UI.FortuneWheel;
using UI.Quests;
using UnityEngine;
using UnityEngine.Serialization;

namespace MineArena.Structs
{
    [CreateAssetMenu(menuName = nameof(GameConfig))]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] private List<LevelConfig> levels;
        [SerializeField] private List<ItemPrize> _prizes;
        [SerializeField] private List<DataQuest> _dataQuests;

        public List<LevelConfig> Levels { get { return levels; } }
        public List<ItemPrize> Prizes { get { return _prizes; } }
        public List<DataQuest> DataQuests { get { return _dataQuests; } }
    }
}