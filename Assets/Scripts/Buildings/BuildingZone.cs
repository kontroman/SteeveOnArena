using Devotion.SDK.Controllers;
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
            if (other.CompareTag("Player"))
            {
                BuildingWindow window = (BuildingWindow)GameRoot.Instance.GetManager<UIManager>().OpenWindow<BuildingWindow>();

                window.InitializeBuilding(config);
            }           
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                GameRoot.Instance.GetManager<UIManager>().CloseWindow<BuildingWindow>();
            }
        }
    }
}