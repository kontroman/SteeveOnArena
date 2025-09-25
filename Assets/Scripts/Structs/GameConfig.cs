using MineArena.Levels;
using System.Collections.Generic;
using MineArena.UI.FortuneWheel;
using UnityEngine;
using Devotion.SDK.Confgs;

namespace MineArena.Structs
{
    [CreateAssetMenu(menuName = nameof(GameConfig))]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] private List<LevelConfig> levels;
        [SerializeField] private List<ItemPrize> _prizes;
        [SerializeField] private LocalizationConfig localizationConfig;

        public List<LevelConfig> Levels { get { return levels; } }
        public List<ItemPrize> Prizes { get { return _prizes; } }
        public LocalizationConfig LocalizationConfig { get { return localizationConfig; } }
    }
}