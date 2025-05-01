using DG.Tweening;
using UnityEngine;

namespace MineArena.PlayerSystem
{
    public class RotationController : MonoBehaviour
    {
        private int _currentPriority;
        private Tweener _activeTween;

        public bool IsRotating(int priority = 0)
        {
            return _activeTween != null && _activeTween.IsActive()
                && _currentPriority >= priority;
        }

        //TODO: think about callback action
        public void RotateToDirection(Vector3 direction, int priority, float duration)
        {
            if (priority < _currentPriority) return;

            direction.y = 0;
            if (direction == Vector3.zero) return;

            if (_activeTween != null && _activeTween.IsActive())
            {
                _activeTween.Kill();
            }

            _currentPriority = priority;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            _activeTween = transform.DORotateQuaternion(targetRotation, duration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => _currentPriority = 0);
        }

        public void RotatePlayerToTarget(Transform target)
        {
            float duration = 0.5f;

            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Vector3 targetEulerAngles = new Vector3(0, targetRotation.eulerAngles.y, 0);

            transform.DORotate(targetEulerAngles, duration, RotateMode.FastBeyond360);
        }
    }
}