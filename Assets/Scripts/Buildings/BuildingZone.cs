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
        private BoxCollider constructionTrigger;
        private Vector3 originalTriggerCenter;
        private Vector3 originalTriggerSize;

        public Transform PlayerPositionOnBuild => playerPositionOnBuild;
        public BuildingConfig Config => config;
        public BuildingCinematicCameraSettings CinematicCameraSettings => overrideCinematicCamera ? cinematicCameraSettings : null;

        private void Awake()
        {
            FitConstructionTrigger();
            if (signObject != null && signObject.GetComponent<BuildingSignOutline>() == null)
                signObject.AddComponent<BuildingSignOutline>();
        }

        private void FitConstructionTrigger()
        {
            if (signObject == null) return;
            constructionTrigger = GetComponents<BoxCollider>().FirstOrDefault(collider => collider.isTrigger);
            if (constructionTrigger == null) return;

            originalTriggerCenter = constructionTrigger.center;
            originalTriggerSize = constructionTrigger.size;
            var scale = transform.lossyScale;
            var width = BuildingSignOutline.Radius * 2f;
            // Building zones are aligned with world axes. Convert world dimensions
            // to local units because the existing village zones are scaled.
            constructionTrigger.center = transform.InverseTransformPoint(
                BuildingSignOutline.GetGroundCenter(signObject) + Vector3.up);
            constructionTrigger.size = new Vector3(
                width / Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
                2f / Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
                width / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.IsPlayer())
            {
                if (config == null) return;
                var manager = GameRoot.GetManager<BuildingManager>();
                if (manager != null && manager.GetBuildingLevel(config) > 0 && StorageWindow.IsStorage(config))
                {
                    StorageWindow.Open(config, transform);
                    return;
                }
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
                if (StorageWindow.IsStorage(config)) GameRoot.UIManager.CloseWindow<StorageWindow>();
            }
        }

        public void DestroySign()
        {
            // Built buildings retain their original interaction area.
            if (constructionTrigger != null)
            {
                constructionTrigger.center = originalTriggerCenter;
                constructionTrigger.size = originalTriggerSize;
                constructionTrigger = null;
            }
            Destroy(signObject);
        }
    }
}
