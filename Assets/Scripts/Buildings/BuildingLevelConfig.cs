using System.Collections.Generic;
using System;
using UnityEngine;
using MineArena.Items;

namespace MineArena.Buildings
{
    [Serializable]
    public class BuildingLevelConfig
    {
        [SerializeField] private int _level;
        [SerializeField] private List<ResourceRequired> _requiredResources;
        [SerializeField] private List<ItemConfig> _unlocks;
        [SerializeField] private GameObject _modelPrefab;
        [SerializeField] private Sprite _preview;
        [SerializeField, Min(0)] private int _craftOutputBonus;
        [SerializeField, Range(0, 50)] private int _expeditionRewardBonusPercent;
        [SerializeField] private List<MineArena.Levels.LevelRewards> _production = new();
        [SerializeField, Min(1)] private float _productionSeconds = 60f;

        public int Level => _level;
        public IReadOnlyList<ResourceRequired> RequiredResources => _requiredResources;
        public IReadOnlyList<ItemConfig> Unlocks => _unlocks;
        public GameObject ModelPrefab => _modelPrefab;
        public Sprite Preview => _preview;
        public int CraftOutputBonus => _craftOutputBonus;
        public int ExpeditionRewardBonusPercent => _expeditionRewardBonusPercent;
        public IReadOnlyList<MineArena.Levels.LevelRewards> Production => _production;
        public float ProductionSeconds => Mathf.Max(1f, _productionSeconds);
    }

    [Serializable]
    public struct ResourceRequired
    {
        public ResourceRequired(StackableItemConfig resource, int amount)
        {
            _resource = resource;
            _amount = amount;
        }

        [SerializeField] private StackableItemConfig _resource;
        [SerializeField] private int _amount;

        public StackableItemConfig Resource => _resource;
        public int Amount => _amount;
        public string ResourceCategory => _resource != null ? _resource.ResourceCategory : string.Empty;
    }
}
