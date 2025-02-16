using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Divotion.Game.UI
{
    public class Bar<T> : MonoBehaviour where T : MonoBehaviour
    {
        [SerializeField] protected Image FillImage;
        [SerializeField] private float _time = 10f;

        protected T TargetSystem;
        private Coroutine _coroutine;

        private void Start()
        {
            if (FillImage == null)
                Debug.LogError("Не назначен Image");

            TargetSystem = GetComponentInParent<T>();

            Debug.Log(TargetSystem.gameObject.name);
            SubscribeToChange();
        }

        protected virtual void SubscribeToChange() { }

        public virtual void UpdateBar(float currentValue, float maxValue)
        {
            float targetFill = currentValue / maxValue;

            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _coroutine = StartCoroutine(SlowlyChangeValue(targetFill));
        }

        private IEnumerator SlowlyChangeValue(float targetValue)
        {
            while (!Mathf.Approximately(FillImage.fillAmount, targetValue))
            {
                FillImage.fillAmount = Mathf.Lerp(FillImage.fillAmount, targetValue, _time * Time.deltaTime);
                yield return null;
            }
        }
    }
}