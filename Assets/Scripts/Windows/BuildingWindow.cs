using MineArena.Buildings;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using MineArena.Windows.Elements;
using TMPro;
using UnityEngine;
using MineArena.Managers;

namespace MineArena.Windows
{
    public class BuildingWindow : BaseWindow
    {
        [SerializeField] private TextMeshProUGUI _buildingName;
        [SerializeField] private Transform _priceTransform;
        [SerializeField] private Transform _opensTransform;
        [SerializeField] private BuildingPriceElement _pricePrefab;

        private BuildingConfig _buildingConfig;

        public void InitializeBuilding(BuildingConfig config)
        {
            ClearSavedData();

            _buildingConfig = config;

            _buildingName.text = config.BuildingName;

            foreach (var item in config.GetCurrentLevel().RequiredResources)
            {
                BuildingPriceElement element = Instantiate(_pricePrefab, _priceTransform);
                element.Setup(item);
            }
            
            foreach (var item in config.GetCurrentLevel().Unlocks)
            {
                BuildingPriceElement element = Instantiate(_pricePrefab, _opensTransform);
                element.Setup(item);
            }
        }

        public void ClearSavedData()
        {
            foreach(Transform t in _priceTransform)
            {
                Destroy(t.gameObject);
            }            

            foreach(Transform t in _opensTransform)
            {
                Destroy(t.gameObject);
            }
        }

        public void OnTryBuildClick()
        {
            if (GameRoot.Instance.GetManager<BuildingManager>().TryBuilding(_buildingConfig))
            {
                OnCloseClick();
            }
        }
        
        public void OnCloseClick()
        {
            GameRoot.Instance.GetManager<UIManager>().CloseWindow<BuildingWindow>();
        }
    }
}