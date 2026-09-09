using Devotion.SDK.Controllers;
using MineArena.UI.FortuneWheel;
using UnityEngine;

namespace MineArena.InteractableObjects
{
    public class WorldChest : MonoBehaviour
    {
        [SerializeField] private string _chestId;
        [SerializeField] private ItemPrize _prize;
        private bool _opening;
        public string ChestId => _chestId;
        public ItemPrize Prize => _prize;
        public bool IsOpened => GameRoot.PlayerProgress?.AchievementProgress.HasOpenedChest(_chestId) == true;

        private void Start()
        {
            // Collected chests remain absent on subsequent expeditions.
            if (IsOpened) gameObject.SetActive(false);
        }

        public bool TryBeginOpening()
        {
            if (_opening || IsOpened || string.IsNullOrWhiteSpace(_chestId) ||
                _prize?.ItemConfig == null || GameRoot.PlayerProgress == null) return false;
            _opening = true;
            return true;
        }

        public void CancelOpening() => _opening = false;

        public bool TryCollect()
        {
            if (!_opening || IsOpened || _prize?.ItemConfig == null) return false;
            var inventory = GameRoot.GetManager<MineArena.Managers.InventoryManager>();
            if (inventory == null) return false;
            _prize.Construct();
            if (!GameRoot.PlayerProgress.AchievementProgress.RegisterChest(_chestId)) return false;
            _opening = false;
            _prize.GiveTo();
            GameRoot.GetManager<global::Managers.AchievementManager>()?.RefreshChestProgress();
            Devotion.SDK.Messages.Player.SavePlayerProgress.Publish();
            return true;
        }
    }
}
