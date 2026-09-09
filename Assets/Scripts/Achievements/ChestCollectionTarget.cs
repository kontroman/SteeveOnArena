using UnityEngine;
namespace Achievements
{
    public sealed class ChestCollectionTarget : ScriptableObject, IAchievementTarget
    {
        [SerializeField] private string[] _chestIds;
        public string Name => "WorldChests";
        public System.Collections.Generic.IReadOnlyList<string> ChestIds => _chestIds;
        public int CountFound(Devotion.SDK.Services.SaveSystem.Progress.AchievementProgress progress)
        {
            int count = 0;
            foreach (var id in _chestIds) if (progress.HasOpenedChest(id)) count++;
            return count;
        }
    }
}
