using MineArena.Levels;
using System.Collections.Generic;
using MineArena.UI.FortuneWheel;
using UnityEngine;

namespace MineArena.Structs
{
    [CreateAssetMenu(menuName = nameof(GameConfig))]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] private List<LevelConfig> levels;
        [SerializeField] private List<ItemPrize> _prizes;

        public List<LevelConfig> Levels { get { return levels; } }
        public List<ItemPrize> Prizes { get { return _prizes; } }
    }
}