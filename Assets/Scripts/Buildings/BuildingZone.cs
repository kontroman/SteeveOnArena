using Devotion.SDK.Controllers;
using Devotion.SDK.Helpers;
using Devotion.SDK.Managers;
using MineArena.Windows;
using MineArena.Windows.Crafting;
using UnityEngine;
using System.Linq;
using MineArena.Managers;

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
        public BuildingConfig Config => config;
        public BuildingCinematicCameraSettings CinematicCameraSettings => overrideCinematicCamera ? cinematicCameraSettings : null;

        private void OnTriggerEnter(Collider other)
        {
            if (other.IsPlayer())
            {
                if (config == null) return;
                var manager = GameRoot.GetManager<BuildingManager>();
                if (manager != null && manager.GetBuildingLevel(config) > 0 &&
                    MineArena.Cosmetics.SkinCatalog.Load()?.Building == config)
                {
                    GameRoot.UIManager.OpenWindow<MineArena.Cosmetics.SkinShopWindow>();
                    return;
                }
                if (manager != null && manager.GetBuildingLevel(config) > 0 &&
                    new ProjectCraftingAdapter().BuildCatalog().Any(category =>
                        category.Recipes.Any(recipe => recipe.SourceBuilding == config)))
                {
                    CraftingWindow.Open(config);
                    return;
                }
                if (!TutorialService.AllowBuilding(config)) return;
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
