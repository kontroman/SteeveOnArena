using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using UnityEngine;

namespace Devotion.SDK.UI
{
    public class FortuneWheelWindow : BaseWindow
    {
        public override void CloseWindow()
        {
            GameRoot.UIManager.CloseWindow<FortuneWheelWindow>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseWindow();
        }
    }
}
