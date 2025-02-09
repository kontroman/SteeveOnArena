using UnityEngine.UI;
using UnityEngine;

public class HealthViewSlider : HealthView
{
    [SerializeField] private Slider _slider;

    public override void DisplayAmount(float value)
       => _slider.value = value / Health.MaxHealth;
}