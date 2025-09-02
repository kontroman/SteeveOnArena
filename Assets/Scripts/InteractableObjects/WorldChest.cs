using MineArena.UI.FortuneWheel;
using UnityEngine;

namespace MineArena.InteractableObjects
{
    public class WorldChest : MonoBehaviour
    {
        [SerializeField] private string _chestId;
        [SerializeField] private ItemPrize _prize;

        private bool _isOpened;

        public ItemPrize Prize { get {  return _prize; } }

        private void Awake()
        {
            Debug.LogError("[TODO]: save load chest progress");
        }
    }
}