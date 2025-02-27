using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Devotion.Basics;

namespace Divotion.Game.UI
{
    public abstract class AbstractBar<T> : MonoBehaviour where T : IProgressBar
    {
        [SerializeField] private Image _fillImage;

        protected T TargetSystem;
        private Coroutine _coroutine;

        private void Awake()
        {
            if (_fillImage == null)
            {
                Debug.LogError("Not assigned Image");
                return;
            }

            TargetSystem = GetComponentInParent<T>();

            if (TargetSystem == null)
            {
                Debug.LogError("TargetSystem not found");
                return;
            }

            SubscribeToChange();
        }

        protected virtual void SubscribeToChange() { }

        public virtual void UpdateBar(float currentValue, float maxValue)
        {
            var targetFill = currentValue / maxValue;

            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _coroutine = StartCoroutine(SlowlyChangeValue(targetFill));
        }

        private IEnumerator SlowlyChangeValue(float targetValue)
        {
            while (!Mathf.Approximately(_fillImage.fillAmount, targetValue))
            {
                _fillImage.fillAmount = Mathf.Lerp(_fillImage.fillAmount, targetValue, Constants.UISettings.SpeedFillProgressBar * Time.deltaTime);
                yield return null;
            }
        }
    }
}