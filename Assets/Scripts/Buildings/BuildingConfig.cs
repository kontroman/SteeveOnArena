using System.Collections.Generic;
using UnityEngine;

namespace MineArena.Buildings
{
    [CreateAssetMenu(fileName = "BuildingConfig", menuName = "Configs/BuildingConfig")]
    public class BuildingConfig : ScriptableObject
    {
        [SerializeField] private string _buildingName;
        [SerializeField] private List<BuildingLevelConfig> _levels;
        [SerializeField] private int _currentLevel;
        [SerializeField] private Quaternion _buildingRotation;

        public string BuildingName => _buildingName;
        public IReadOnlyList<BuildingLevelConfig> Levels => _levels;
        public int CurrentLevel => _currentLevel;
        public Quaternion BuildingRotation => _buildingRotation;

        public BuildingLevelConfig GetCurrentLevel() => _levels[_currentLevel];
    }
}