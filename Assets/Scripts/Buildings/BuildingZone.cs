using Devotion.SDK.Controllers;
using Devotion.SDK.Helpers;
using Devotion.SDK.Managers;
using MineArena.Windows;
using UnityEngine;

namespace MineArena.Buildings
{
    public class BuildingZone : MonoBehaviour
    {
        [SerializeField] private BuildingConfig config;

        private void OnTriggerEnter(Collider other)
        {
            if (other.IsPlayer())
            {
                BuildingWindow window = (BuildingWindow)GameRoot.UIManager.OpenWindow<BuildingWindow>();

                window.InitializeBuilding(config, this.transform);
            }           
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.IsPlayer())
            {
                GameRoot.UIManager.CloseWindow<BuildingWindow>();
            }
        }
    }
}