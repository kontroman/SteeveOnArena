using System.Collections.Generic;
using UnityEngine;

namespace Devotion.Buildings
{
    [CreateAssetMenu(fileName = "BuildingConfig", menuName = "Configs/BuildingConfig")]
    public class BuildingConfig : ScriptableObject
    {
        [SerializeField] private string _buildingName;
        [SerializeField] private List<BuildingLevelConfig> _levels;
        [SerializeField] private int _currentLevel;
        [SerializeField] private Transform _buildingPlace;

        public string BuildingName => _buildingName;
        public IReadOnlyList<BuildingLevelConfig> Levels => _levels;
        public int CurrentLevel => _currentLevel;
        public Transform BuildingPlace => _buildingPlace;

        public BuildingLevelConfig GetCurrentLevel() => _levels[_currentLevel];
    }
}