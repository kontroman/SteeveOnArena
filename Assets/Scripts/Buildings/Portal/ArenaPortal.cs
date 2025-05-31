using Devotion.SDK.Controllers;
using Devotion.SDK.Helpers;
using MineArena.Windows;
using UnityEngine;

namespace MineArena.Buildings.Portal
{
    public class ArenaPortal : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.IsPlayer())
            {
                GameRoot.UIManager.OpenWindow<SelectLevelWindow>();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.IsPlayer())
            {
                GameRoot.UIManager.CloseWindow<SelectLevelWindow>();
            }
        }
    }
}