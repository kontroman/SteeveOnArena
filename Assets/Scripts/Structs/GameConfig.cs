using MineArena.Levels;
using System.Collections.Generic;
using System;
using MineArena.UI.FortuneWheel;
using UnityEngine;
using Devotion.SDK.Confgs;
using MineArena.Items;
using MineArena.Buildings;
using UnityEngine.Events;
using Sirenix.OdinInspector;

namespace MineArena.Structs
{
    [CreateAssetMenu(menuName = nameof(GameConfig))]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] private List<LevelConfig> levels;
        [SerializeField] private List<ItemPrize> _prizes;
        [SerializeField] private LocalizationConfig localizationConfig;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private BuildingsDatabase buildingsDatabase;

        public List<LevelConfig> Levels { get { return levels; } }
        public List<ItemPrize> Prizes { get { return _prizes; } }
        public LocalizationConfig LocalizationConfig { get { return localizationConfig; } }
        public ItemDatabase ItemDatabase { get { return itemDatabase; } }
        public BuildingsDatabase BuildingsDatabase { get { return buildingsDatabase; } }

#region GODMODE
        [Space(10)]
        [SerializeField] private bool godMode;
        private bool godModeInvulnerability;
        private bool godModeOneHitKill;

        private UnityEvent<bool> godModeChanged;
        private UnityEvent<bool> invulnerabilityChanged;
        private UnityEvent<bool> oneHitKillChanged;

        public event Action<bool> GodModeChanged
        {
            add => godModeChanged.AddListener(value.Invoke);
            remove => godModeChanged.RemoveListener(value.Invoke);
        }

        public event Action<bool> InvulnerabilityChanged
        {
            add => invulnerabilityChanged.AddListener(value.Invoke);
            remove => invulnerabilityChanged.RemoveListener(value.Invoke);
        }

        public event Action<bool> OneHitKillChanged
        {
            add => oneHitKillChanged.AddListener(value.Invoke);
            remove => oneHitKillChanged.RemoveListener(value.Invoke);
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
