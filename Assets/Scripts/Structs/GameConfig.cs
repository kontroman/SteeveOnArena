using MineArena.Levels;
using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Structs
{
    [CreateAssetMenu(menuName = nameof(GameConfig))]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] private List<LevelConfig> levels;

        public List<LevelConfig> Levels { get { return levels; } }
    }
}