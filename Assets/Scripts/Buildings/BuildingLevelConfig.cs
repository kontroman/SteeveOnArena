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

        public int Level => _level;
        public IReadOnlyList<ResourceRequired> RequiredResources => _requiredResources;
        public IReadOnlyList<ItemConfig> Unlocks => _unlocks;
        public GameObject ModelPrefab => _modelPrefab;
    }

    [Serializable]
    public struct ResourceRequired
    {
        [SerializeField] private StackableItemConfig _resource;
        [SerializeField] private int _amount;
        [SerializeField] private bool _blockStyleIcon;

        public StackableItemConfig Resource => _resource;
        public int Amount => _amount;
        public bool BlockStyleIcon => _blockStyleIcon;
        public string ResourceCategory => _resource != null ? _resource.ResourceCategory : string.Empty;
    }
}
