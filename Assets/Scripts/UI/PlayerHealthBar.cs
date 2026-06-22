using MineArena.Basics;
using MineArena.Controllers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlayerHealth = MineArena.Game.Health.Health;

namespace MineArena.Game.UI
{
    public class PlayerHealthBar : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private bool _smoothFill = true;
        [SerializeField] private RectTransform _animatedRect;
        [SerializeField] private float _animatedHeightOffset = 4f;
        [SerializeField] private float _heightAnimationSpeed = 16f;
        [SerializeField] private bool _smoothTextValue = true;
        [SerializeField] private RectTransform _animatedTextRect;
        [SerializeField] private float _animatedTextScaleOffset = 0.08f;
        [SerializeField] private float _textAnimationSpeed = 16f;
        [SerializeField] private float _textValueAnimationSpeed = 10f;
        [SerializeField] private string _format = "{0} / {1}";

        private PlayerHealth _health;
        private Coroutine _fillCoroutine;
        private Coroutine _bindCoroutine;
        private float _baseHeight;
        private bool _hasBaseHeight;
        private Vector3 _baseTextScale;
        private bool _hasBaseTextScale;
        private float _displayedCurrentValue;
        private float _displayedMaxValue;
        private bool _hasDisplayedHealth;

        private void Awake()
        {
            if (_valueText == null)
                _valueText = GetComponentInChildren<TMP_Text>();

            ResolveAnimatedRect();
            ResolveAnimatedTextRect();
        }

        private void OnEnable()
        {
            if (!TryBindHealth())
                _bindCoroutine = StartCoroutine(BindWhenReady());
        }

        private void OnDisable()
        {
            if (_bindCoroutine != null)
            {
                StopCoroutine(_bindCoroutine);
                _bindCoroutine = null;
            }

            if (_health != null)
                _health.OnHealthChanged -= UpdateHealth;

            if (_fillCoroutine != null)
            {
                StopCoroutine(_fillCoroutine);
                _fillCoroutine = null;
            }

            ResetAnimatedHeight();
            ResetAnimatedTextScale();
        }

        private System.Collections.IEnumerator BindWhenReady()
        {
            while (!TryBindHealth())
                yield return null;

            _bindCoroutine = null;
        }

        private void UpdateHealth(float currentValue, float maxValue)
        {
            if (!_hasDisplayedHealth)
            {
                _displayedCurrentValue = currentValue;
                _displayedMaxValue = maxValue;
                _hasDisplayedHealth = true;
                SetFill(currentValue, maxValue);
                UpdateText(currentValue, maxValue);
                return;
            }

            if (_fillCoroutine != null)
                StopCoroutine(_fillCoroutine);

            _fillCoroutine = StartCoroutine(ChangeHealth(currentValue, maxValue));
        }

        private void SetFill(float currentValue, float maxValue)
        {
            if (_fillImage == null)
                return;

            _fillImage.fillAmount = GetFillAmount(currentValue, maxValue);
        }

        private void UpdateText(float currentValue, float maxValue)
        {
            if (_valueText != null)
                _valueText.text = string.Format(
                    _format,
                    Mathf.RoundToInt(currentValue),
                    Mathf.RoundToInt(maxValue));
        }

        private System.Collections.IEnumerator ChangeHealth(float targetCurrentValue, float targetMaxValue)
        {
            CacheBaseHeight();
            CacheBaseTextScale();

            float targetFill = GetFillAmount(targetCurrentValue, targetMaxValue);

            while (!IsFillAtTarget(targetFill) || !IsTextValueAtTarget(targetCurrentValue, targetMaxValue))
            {
                if (_fillImage != null)
                {
                    _fillImage.fillAmount = _smoothFill
                        ? Mathf.Lerp(_fillImage.fillAmount, targetFill, Constants.UISettings.SpeedFillProgressBar * Time.deltaTime)
                        : targetFill;
                }

                if (_smoothTextValue)
                {
                    _displayedCurrentValue = Mathf.Lerp(
                        _displayedCurrentValue,
                        targetCurrentValue,
                        _textValueAnimationSpeed * Time.deltaTime);

                    _displayedMaxValue = Mathf.Lerp(
                        _displayedMaxValue,
                        targetMaxValue,
                        _textValueAnimationSpeed * Time.deltaTime);
                }
                else
                {
                    _displayedCurrentValue = targetCurrentValue;
                    _displayedMaxValue = targetMaxValue;
                }

                UpdateText(_displayedCurrentValue, _displayedMaxValue);

                SetAnimatedHeight(Mathf.Lerp(
                    GetAnimatedHeight(),
                    _baseHeight + _animatedHeightOffset,
                    _heightAnimationSpeed * Time.deltaTime));

                SetAnimatedTextScale(Vector3.Lerp(
                    GetAnimatedTextScale(),
                    GetIncreasedTextScale(),
                    _textAnimationSpeed * Time.deltaTime));

                yield return null;
            }

            if (_fillImage != null)
                _fillImage.fillAmount = targetFill;

            _displayedCurrentValue = targetCurrentValue;
            _displayedMaxValue = targetMaxValue;
            UpdateText(_displayedCurrentValue, _displayedMaxValue);

            while (!IsAnimatedHeightAtBase() || !IsAnimatedTextScaleAtBase())
            {
                SetAnimatedHeight(Mathf.Lerp(
                    GetAnimatedHeight(),
                    _baseHeight,
                    _heightAnimationSpeed * Time.deltaTime));

                SetAnimatedTextScale(Vector3.Lerp(
                    GetAnimatedTextScale(),
                    _baseTextScale,
                    _textAnimationSpeed * Time.deltaTime));

                yield return null;
            }

            SetAnimatedHeight(_baseHeight);
            SetAnimatedTextScale(_baseTextScale);
            _fillCoroutine = null;
        }

        private float GetFillAmount(float currentValue, float maxValue)
        {
            return maxValue > 0f ? Mathf.Clamp01(currentValue / maxValue) : 0f;
        }

        private bool IsFillAtTarget(float targetFill)
        {
            return _fillImage == null || Mathf.Approximately(_fillImage.fillAmount, targetFill);
        }

        private bool IsTextValueAtTarget(float currentValue, float maxValue)
        {
            return Mathf.Approximately(_displayedCurrentValue, currentValue)
                   && Mathf.Approximately(_displayedMaxValue, maxValue);
        }

        private void ResolveAnimatedRect()
        {
            if (_animatedRect == null)
                _animatedRect = transform as RectTransform;

            CacheBaseHeight();
        }

        private void CacheBaseHeight()
        {
            if (_hasBaseHeight || _animatedRect == null)
                return;

            _baseHeight = _animatedRect.sizeDelta.y;
            _hasBaseHeight = true;
        }

        private float GetAnimatedHeight()
        {
            return _animatedRect != null ? _animatedRect.sizeDelta.y : _baseHeight;
        }

        private void SetAnimatedHeight(float height)
        {
            if (_animatedRect == null || !_hasBaseHeight)
                return;

            var size = _animatedRect.sizeDelta;
            size.y = height;
            _animatedRect.sizeDelta = size;
        }

        private void ResetAnimatedHeight()
        {
            if (_animatedRect == null || !_hasBaseHeight)
                return;

            SetAnimatedHeight(_baseHeight);
        }

        private void ResolveAnimatedTextRect()
        {
            if (_animatedTextRect == null && _valueText != null)
                _animatedTextRect = _valueText.rectTransform;

            CacheBaseTextScale();
        }

        private void CacheBaseTextScale()
        {
            if (_hasBaseTextScale || _animatedTextRect == null)
                return;

            _baseTextScale = _animatedTextRect.localScale;
            _hasBaseTextScale = true;
        }

        private Vector3 GetAnimatedTextScale()
        {
            return _animatedTextRect != null ? _animatedTextRect.localScale : _baseTextScale;
        }

        private Vector3 GetIncreasedTextScale()
        {
            return _baseTextScale * (1f + _animatedTextScaleOffset);
        }

        private void SetAnimatedTextScale(Vector3 scale)
        {
            if (_animatedTextRect == null || !_hasBaseTextScale)
                return;

            _animatedTextRect.localScale = scale;
        }

        private bool IsAnimatedHeightAtBase()
        {
            return _animatedRect == null || !_hasBaseHeight || Mathf.Approximately(GetAnimatedHeight(), _baseHeight);
        }

        private bool IsAnimatedTextScaleAtBase()
        {
            return _animatedTextRect == null
                   || !_hasBaseTextScale
                   || (GetAnimatedTextScale() - _baseTextScale).sqrMagnitude <= 0.0001f;
        }

        private void ResetAnimatedTextScale()
        {
            if (_animatedTextRect == null || !_hasBaseTextScale)
                return;

            SetAnimatedTextScale(_baseTextScale);
        }

        private bool TryBindHealth()
        {
            if (_health != null)
                return true;

            var player = Player.Instance;
            if (player == null)
                return false;

            _health = player.GetComponentFromList<PlayerHealth>();

            if (_health == null)
                _health = player.GetComponentInChildren<PlayerHealth>();

            if (_health == null)
                return false;

            _health.OnHealthChanged += UpdateHealth;
            UpdateHealth(_health.CurrentValue, _health.MaxValue);

            return true;
        }
    }
}
