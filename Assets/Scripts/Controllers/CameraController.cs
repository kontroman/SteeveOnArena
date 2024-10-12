using UnityEngine;
using DG.Tweening;
using Devotion.Managers;

namespace Devotion.Controllers
{
    public class CameraController : BaseManager
    {
        [SerializeField] private Transform _player;
        [SerializeField] private float _followSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 5f;
        [SerializeField] private float _smoothMoveTime = 1f;

        private Camera _mainCamera;
        private bool _isFollowing = true;
        private bool _isMovingToTarget = false;

        public new void InitManager()
        {
            _mainCamera = Camera.main;
            _player = Player.Instance.gameObject.transform;
        }

        private void LateUpdate()
        {
            if (_isFollowing && !_isMovingToTarget)
            {
                FollowPlayer();
            }
        }

        private void FollowPlayer()
        {
            Vector3 targetPosition = _player.position;
            _mainCamera.transform.position = Vector3.Lerp(_mainCamera.transform.position, targetPosition, _followSpeed * Time.deltaTime);
            Quaternion targetRotation = Quaternion.LookRotation(_player.position - _mainCamera.transform.position);
            _mainCamera.transform.rotation = Quaternion.Slerp(_mainCamera.transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
        }

        public void SetFollowing(bool follow)
        {
            _isFollowing = follow;
        }

        public void MoveCameraToObject(Transform targetObject)
        {
            SetFollowing(false);
            _mainCamera.transform.position = targetObject.position;
            _mainCamera.transform.rotation = Quaternion.LookRotation(targetObject.position - _mainCamera.transform.position);
        }

        public void SetCameraAroundObject(Transform targetObject, Vector3 offset, float angle)
        {
            SetFollowing(false);
            Vector3 direction = Quaternion.Euler(0, angle, 0) * offset;
            _mainCamera.transform.position = targetObject.position + direction;
            _mainCamera.transform.LookAt(targetObject);
        }

        public void SmoothMoveToObject(Transform targetObject, float moveTime)
        {
            SetFollowing(false);
            _isMovingToTarget = true;

            _mainCamera.transform.DOMove(targetObject.position, moveTime)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() => _isMovingToTarget = false);

            _mainCamera.transform.DORotateQuaternion(Quaternion.LookRotation(targetObject.position - _mainCamera.transform.position), moveTime)
                .SetEase(Ease.InOutQuad);
        }

        public void SmoothMoveToObjectAndBack(Transform targetObject, float moveTime)
        {
            SmoothMoveToObject(targetObject, moveTime);

            DOVirtual.DelayedCall(moveTime + 1f, () => {
                _mainCamera.transform.DOMove(_player.position, moveTime)
                    .SetEase(Ease.InOutQuad)
                    .OnComplete(() => SetFollowing(true));

                _mainCamera.transform.DORotateQuaternion(Quaternion.LookRotation(_player.position - _mainCamera.transform.position), moveTime)
                    .SetEase(Ease.InOutQuad);
            });
        }
    }
}
