using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UIButton = UnityEngine.UI.Button;

namespace MineArena.UI
{
    [AddComponentMenu("UI/Animated Button")]
    public class AnimatedButton : UIButton
    {
        private Tween _animation;
        private Vector3 _restScale;
        private bool _hasRestScale;

        protected override void OnEnable()
        {
            base.OnEnable();
            _restScale = transform.localScale;
            _hasRestScale = true;
        }

        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (eventData.button == PointerEventData.InputButton.Left && IsActive() && IsInteractable())
                Animate(0.94f, 0.055f);
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            if (eventData.button == PointerEventData.InputButton.Left && IsActive()) Animate(1f, 0.12f);
        }

        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            if (IsActive()) Animate(1f, 0.12f);
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (IsActive() && IsInteractable())
            {
                _animation?.Kill();
                transform.localScale = _restScale * 0.94f;
                Animate(1f, 0.16f);
            }
            base.OnSubmit(eventData);
        }

        private void Animate(float scale, float duration)
        {
            _animation?.Kill();
            _animation = transform.DOScale(_restScale * scale, duration).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        protected override void OnDisable()
        {
            _animation?.Kill();
            _animation = null;
            if (_hasRestScale) transform.localScale = _restScale;
            base.OnDisable();
        }
    }
}
