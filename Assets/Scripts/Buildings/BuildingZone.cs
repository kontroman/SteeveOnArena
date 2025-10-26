using Devotion.SDK.Controllers;
using Devotion.SDK.Helpers;
using Devotion.SDK.Managers;
using MineArena.Windows.Crafting;
using UnityEngine;

namespace MineArena.Buildings
{
    public class BuildingZone : MonoBehaviour
    {
        [SerializeField] private BuildingConfig config;
        [SerializeField] private GameObject signObject;

        private void OnTriggerEnter(Collider other)
        {
            if (other.IsPlayer())
            {
                var window = (CraftingWindow)GameRoot.UIManager.OpenWindow<CraftingWindow>();
                if (window != null)
                {
                    window.Initialize(config);
                }
            }           
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.IsPlayer())
            {
                GameRoot.UIManager.CloseWindow<CraftingWindow>();
            }
        }

        public void DestroySign()
        {
            Destroy(signObject);
        }
    }
}
