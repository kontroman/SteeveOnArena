namespace Divotion.Game.UI
{
    public class HealthBar : AbstractBar<Divotion.Game.Health.Health>
    {
        protected override void SubscribeToChange()
        {
            if (TargetSystem != null)
                TargetSystem.OnHealthChanged += UpdateBar;
        }

        private void OnDestroy()
        {
            if (TargetSystem != null)
                TargetSystem.OnHealthChanged -= UpdateBar;
        }
    }
}