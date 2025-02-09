using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class HealthViewSmoothSlider : HealthView
{
    [SerializeField] private Slider _slider;
    [SerializeField] private float _time = 10f;

    private Coroutine _coroutine;

    private void Start()
    {
        _coroutine = StartCoroutine(SlowlyChangeValue(Health.CurrentHealth));
    }

    public override void DisplayAmount(float value)
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);

        _coroutine = StartCoroutine(SlowlyChangeValue(value));
    }

    private IEnumerator SlowlyChangeValue(float targetValue)
    {
        targetValue /= Health.MaxHealth;

        while (_slider.value != targetValue)
        {
            _slider.value = Mathf.Lerp(_slider.value, targetValue, _time * Time.deltaTime);
            yield return null;
        }
    }
}