using Devotion.SDK.Base;
using Devotion.SDK.Controllers;

namespace Devotion.SDK.UI
{
    public class PlayingWindow : BaseWindow
    {
        public void OnAchievmentButtonClick() => GameRoot.UIManager.ShowWindow<AchievemntQuest>();
    }
}