using MineArena.Levels;
using System.Collections.Generic;
using UI.FortuneWheel;
using UnityEngine;

namespace MineArena.Structs
{
    [CreateAssetMenu(menuName = nameof(GameConfig))]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] private List<LevelConfig> levels;
        [SerializeField] private List<WheelPrize> _prizes;

        public List<LevelConfig> Levels { get { return levels; } }
        public List<WheelPrize> Prizes { get { return _prizes; } }
    }
}