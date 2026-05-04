using MineArena.Basics;
using MineArena.Controllers;
using MineArena.PlayerSystem;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Game.UI
{
    [RequireComponent(typeof(Image))]
    public class PlayerExperienceBar : MonoBehaviour
    {
        [SerializeField] private Image _fillImage;
        [SerializeField] private bool _smoothFill = true;
        [SerializeField] private RectTransform _animatedRect;
        [SerializeField] private float _animatedHeightOffset = 4f;
        [SerializeField] private float _heightAnimationSpeed = 16f;

        private PlayerExperience _experience;
        private Coroutine _fillCoroutine;
        private float _baseHeight;
        private bool _hasBaseHeight;

        private void Awake()
        {
            if (_fillImage == null)
                _fillImage = GetComponent<Image>();

            ResolveAnimatedRect();
        }

        private void OnEnable()
        {
            Player.ExperienceInitialized += SetExperience;
            SetExperience(Player.Instance?.Experience);
        }

        private void OnDisable()
        {
            Player.ExperienceInitialized -= SetExperience;
            SetExperience(null);

            if (_fillCoroutine != null)
            {
                StopCoroutine(_fillCoroutine);
                _fillCoroutine = null;
            }

            ResetAnimatedHeight();
        }

        private void UpdateBar(float currentValue, float maxValue)
        {
            if (_fillImage == null)
                return;

            float targetFill = maxValue > 0f ? Mathf.Clamp01(currentValue / maxValue) : 0f;

            if (!_smoothFill)
            {
                _fillImage.fillAmount = targetFill;
                return;
            }

            if (_fillCoroutine != null)
                StopCoroutine(_fillCoroutine);

            _fillCoroutine = StartCoroutine(ChangeFill(targetFill));
        }

        private System.Collections.IEnumerator ChangeFill(float targetFill)
        {
            CacheBaseHeight();

            while (!Mathf.Approximately(_fillImage.fillAmount, targetFill))
            {
                _fillImage.fillAmount = Mathf.Lerp(
                    _fillImage.fillAmount,
                    targetFill,
                    Constants.UISettings.SpeedFillProgressBar * Time.deltaTime);

                SetAnimatedHeight(Mathf.Lerp(
                    GetAnimatedHeight(),
                    _baseHeight + _animatedHeightOffset,
                    _heightAnimationSpeed * Time.deltaTime));

                yield return null;
            }

            _fillImage.fillAmount = targetFill;

            while (!Mathf.Approximately(GetAnimatedHeight(), _baseHeight))
            {
                SetAnimatedHeight(Mathf.Lerp(
                    GetAnimatedHeight(),
                    _baseHeight,
                    _heightAnimationSpeed * Time.deltaTime));

                yield return null;
            }

            SetAnimatedHeight(_baseHeight);
            _fillCoroutine = null;
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

        private void SetExperience(PlayerExperience experience)
        {
            if (_experience == experience)
            {
                if (_experience != null)
                    UpdateBar(_experience.CurrentExperience, _experience.ExperiencePerLevel);

                return;
            }

            if (_experience != null)
                _experience.OnExperienceChanged -= UpdateBar;

            _experience = experience;

            if (_experience == null)
                return;

            _experience.OnExperienceChanged += UpdateBar;
            UpdateBar(_experience.CurrentExperience, _experience.ExperiencePerLevel);
        }
    }
}
