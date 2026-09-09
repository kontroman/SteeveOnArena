using DG.Tweening;
using UnityEngine;

namespace MineArena.Items
{
    public class AnimationIDLE : MonoBehaviour
    {
        [SerializeField] private Vector3 _step;
        [SerializeField] private Vector3 _rotation;
        [SerializeField] private float _duration;
        [SerializeField] private int _repeats;
        private Tween _moveTween, _rotationTween;

        public void StartAnimation()
        {
            StopAnimation();
            float duration = Mathf.Max(0.1f, _duration);
            _moveTween = transform.DOMove(transform.position + _step, duration).SetLoops(_repeats, LoopType.Yoyo).SetEase(Ease.InOutSine);
            _rotationTween = transform.DORotate(_rotation, duration, RotateMode.FastBeyond360).SetRelative()
                .SetLoops(_repeats, LoopType.Incremental).SetEase(Ease.Linear);
        }

        public void StopAnimation()
        {
            _moveTween?.Kill();
            _rotationTween?.Kill();
            _moveTween = _rotationTween = null;
        }

        private void OnDisable() => StopAnimation();
    }
}
