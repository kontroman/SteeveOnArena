using Devotion.SDK.Controllers;
using Devotion.SDK.Helpers;
using Devotion.SDK.Managers;
using MineArena.Windows;
using MineArena.Windows.Crafting;
using UnityEngine;

namespace MineArena.Buildings
{
    public class BuildingZone : MonoBehaviour
    {
        [SerializeField] private BuildingConfig config;
        [SerializeField] private GameObject signObject;
        [SerializeField] private Transform playerPositionOnBuild;
        [SerializeField] private bool overrideCinematicCamera;
        [SerializeField] private BuildingCinematicCameraSettings cinematicCameraSettings = new BuildingCinematicCameraSettings();

        public Transform PlayerPositionOnBuild => playerPositionOnBuild;
        public BuildingCinematicCameraSettings CinematicCameraSettings => overrideCinematicCamera ? cinematicCameraSettings : null;

        private void OnTriggerEnter(Collider other)
        {
            if (other.IsPlayer())
            {
                BuildingWindow window = (BuildingWindow)GameRoot.UIManager.OpenWindow<BuildingWindow>();
                if (window != null)
                {
                    window.InitializeBuilding(config, this.transform);
                }
            }           
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.IsPlayer())
            {
                GameRoot.UIManager.CloseWindow<BuildingWindow>();
            }
        }

        public void DestroySign()
        {
            Destroy(signObject);
        }
    }
}
