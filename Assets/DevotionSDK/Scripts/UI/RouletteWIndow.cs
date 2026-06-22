using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using UnityEngine;

namespace Devotion.SDK.UI
{
    public class RouletteWIndow : BaseWindow
    {
        public override void CloseWindow()
        {
            GameRoot.UIManager.CloseWindow<RouletteWIndow>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseWindow();
        }
    }
}
