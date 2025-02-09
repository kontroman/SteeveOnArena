using UnityEngine;

public class HealthView : MonoBehaviour
{
    [SerializeField] protected Health Health;

    private void OnEnable()
        => Health.HealthChanged += DisplayAmount;

    private void OnDisable()
        => Health.HealthChanged -= DisplayAmount;

    public virtual void DisplayAmount(float value) { }
}