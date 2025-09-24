using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using UI.Quest;

namespace Devotion.SDK.UI
{
    public class PlayingWindow : BaseWindow
    {
        public void OnAchievmentButtonClick() => GameRoot.UIManager.ShowWindow<WindowQuests>();
    }
}