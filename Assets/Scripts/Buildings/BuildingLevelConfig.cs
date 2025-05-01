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
        [SerializeField] private List<EquipmentItemConfig> _unlocks;
        [SerializeField] private GameObject _modelPrefab;

        public int Level => _level;
        public IReadOnlyList<ResourceRequired> RequiredResources => _requiredResources;
        public IReadOnlyList<EquipmentItemConfig> Unlocks => _unlocks;
        public GameObject ModelPrefab => _modelPrefab;
    }

    [Serializable]
    public struct ResourceRequired
    {
        [SerializeField] private StackableItemConfig _resource;
        [SerializeField] private int _amount;

        public StackableItemConfig Resource => _resource;
        public int Amount => _amount;
    }
}