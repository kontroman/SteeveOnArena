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

        public BuildingLevelConfig GetLevelByNumber(int level)
        {
            return _levels.Find(l => l.Level == level);
        }

        public bool TryGetNextLevel(int currentLevel, out BuildingLevelConfig nextLevel)
        {
            nextLevel = null;

            if (_levels == null || _levels.Count == 0)
                return false;

            int index = _levels.FindIndex(l => l.Level == currentLevel);

            if (index < 0)
            {
                if (currentLevel >= 0 && currentLevel < _levels.Count)
                    index = currentLevel;
                else
                    return false;
            }

            if (index + 1 < _levels.Count)
            {
                nextLevel = _levels[index + 1];
                return true;
            }

            return false;
        }
    }
}
