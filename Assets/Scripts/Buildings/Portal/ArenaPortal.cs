using Devotion.SDK.Controllers;
using Devotion.SDK.Helpers;
using MineArena.Controllers;
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
                if (LevelController.Current != null && LevelController.Current.TryEnterSpawnedPortal(transform, other))
                    return;

                if (LevelController.Current == null && MineArena.Managers.TutorialService.EnterPortal())
                    GameRoot.UIManager.OpenWindow<SelectLevelWindow>();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.IsPlayer())
            {
                LevelController.Current?.ExitSpawnedPortal(transform, other);
                GameRoot.UIManager.CloseWindow<SelectLevelWindow>();
            }
        }
    }
}
