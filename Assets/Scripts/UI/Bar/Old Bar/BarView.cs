using UnityEngine;

namespace Divotion.Game.UI
{
    public class BarView : MonoBehaviour
    {
        [SerializeField] protected Health Health;

        private void OnEnable()
            => Health.OnHealthChanged += DisplayAmount;

        private void OnDisable()
            => Health.OnHealthChanged -= DisplayAmount;

        public virtual void DisplayAmount(float currentHealth, float maxHealth) { }
    }
}