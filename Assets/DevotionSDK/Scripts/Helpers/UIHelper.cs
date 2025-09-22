using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using UnityEngine;

namespace Devotion.SDK.Helpers
{
    public static class UIHelper
    {
        public static void CloseWindow<T>(this T window) where T : BaseWindow
        {
            GameRoot.UIManager.CloseWindow<T>();
        }
    }
}