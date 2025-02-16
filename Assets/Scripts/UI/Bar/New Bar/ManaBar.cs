using UnityEngine;

namespace Divotion.Game.UI
{
    public class ManaBar : Bar<Mana>
    {
        protected override void SubscribeToChange()
        {
            if (TargetSystem != null)
            {
                Debug.Log(TargetSystem.name);
                TargetSystem.OnManaChanged += UpdateBar;
            }
        }

        private void OnDestroy()
        {
            if (TargetSystem != null)
                TargetSystem.OnManaChanged -= UpdateBar;
        }
    }
}
