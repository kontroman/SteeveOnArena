namespace MineArena.Game.UI
{
    public class HealthBar : AbstractBar<Health.Health>
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