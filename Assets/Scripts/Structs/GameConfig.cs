using MineArena.Levels;
using System.Collections.Generic;
using System;
using MineArena.UI.FortuneWheel;
using Structs;
using UnityEngine;

using UnityEngine.Serialization;

using Devotion.SDK.Confgs;
using MineArena.Items;
using MineArena.Buildings;
using Sirenix.OdinInspector;


namespace MineArena.Structs
{
    [CreateAssetMenu(menuName = nameof(GameConfig))]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] private List<LevelConfig> levels;
        [SerializeField] private List<ItemPrize> _prizes;
        [SerializeField] private List<DataAchievement> _dataAchievements;
        [SerializeField] private LocalizationConfig localizationConfig;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private BuildingsDatabase buildingsDatabase;
        [SerializeField, Min(1)] private int freeFortuneSpinCooldownMinutes = 30;

        public List<LevelConfig> Levels { get { return levels; } }
        public List<ItemPrize> Prizes { get { return _prizes; } }
        public List<DataAchievement> DataAchievements { get { return _dataAchievements; } }
        public LocalizationConfig LocalizationConfig { get { return localizationConfig; } }
        public ItemDatabase ItemDatabase { get { return itemDatabase; } }
        public BuildingsDatabase BuildingsDatabase { get { return buildingsDatabase; } }
        public int FreeFortuneSpinCooldownMinutes => freeFortuneSpinCooldownMinutes <= 0 ? 30 : freeFortuneSpinCooldownMinutes;

#region GODMODE
        [Space(10)]
        [SerializeField] private bool godMode;
        private bool godModeInvulnerability;
        private bool godModeOneHitKill;

        private event Action<bool> godModeChanged;
        private event Action<bool> invulnerabilityChanged;
        private event Action<bool> oneHitKillChanged;

        public event Action<bool> GodModeChanged
        {
            add => godModeChanged += value;
            remove => godModeChanged -= value;
        }

        public event Action<bool> InvulnerabilityChanged
        {
            add => invulnerabilityChanged += value;
            remove => invulnerabilityChanged -= value;
        }

        public event Action<bool> OneHitKillChanged
        {
            add => oneHitKillChanged += value;
            remove => oneHitKillChanged -= value;
        }
        public bool GodMode
        {
            get { return godMode; }
            set
            {
                if (godMode == value)
                    return;

                godMode = value;
                godModeChanged?.Invoke(godMode);

                if (!godMode)
                {
                    GodModeInvulnerability = false;
                    GodModeOneHitKill = false;
                }
            }
        }

        public bool GodModeInvulnerability
        {
            get { return godModeInvulnerability; }
            set
            {
                if (godModeInvulnerability == value)
                    return;

                godModeInvulnerability = value;
                invulnerabilityChanged?.Invoke(godModeInvulnerability);
            }
        }

        public bool GodModeOneHitKill
        {
            get { return godModeOneHitKill; }
            set
            {
                if (godModeOneHitKill == value)
                    return;

                godModeOneHitKill = value;
                oneHitKillChanged?.Invoke(godModeOneHitKill);
            }
        }        
#endregion
    }
}
