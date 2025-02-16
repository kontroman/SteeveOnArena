using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Divotion.Game.UI
{
    public class BarViewSmoothImage : BarView
    {
        [SerializeField] private Image _image;
        [SerializeField] private float _time = 10f;

        private Coroutine _coroutine;

        private void Start()
        {
            _coroutine = StartCoroutine(SlowlyChangeValue(Health.CurrentHealth, Health.MaxHealth));
        }

        public override void DisplayAmount(float value, float maxValue)
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _coroutine = StartCoroutine(SlowlyChangeValue(value, maxValue));
        }

        private IEnumerator SlowlyChangeValue(float currentValue, float maxValue)
        {
            currentValue /= maxValue;

            while (_image.fillAmount != currentValue)
            {
                _image.fillAmount = Mathf.Lerp(_image.fillAmount, currentValue, _time * Time.deltaTime);
                yield return null;
            }
        }
    }
}