using Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MineArena.Controllers
{
    [RequireComponent(typeof(CinemachineVirtualCamera))]
    public class CameraZoomController : MonoBehaviour
    {
        [SerializeField] private float _minDistance = 8f;
        [SerializeField] private float _maxDistance = 28f;
        [SerializeField] private float _zoomStep = 2f;
        [SerializeField] private float _zoomSmoothTime = 0.08f;
        [SerializeField] private bool _ignoreWhenPointerOverUi = true;

        private CinemachineVirtualCamera _virtualCamera;
        private CinemachineTransposer _transposer;
        private Vector3 _zoomDirection;
        private float _targetDistance;
        private float _currentDistance;
        private float _zoomVelocity;
        private bool _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        private void LateUpdate()
        {
            if (!_isInitialized)
                Initialize();

            if (!_isInitialized)
                return;

            HandleZoomInput();
            ApplyZoom();
        }

        private void Initialize()
        {
            _virtualCamera = GetComponent<CinemachineVirtualCamera>();
            _transposer = _virtualCamera.GetCinemachineComponent<CinemachineTransposer>();

            if (_transposer == null)
                return;

            Vector3 offset = _transposer.m_FollowOffset;
            if (offset.sqrMagnitude <= Mathf.Epsilon)
                offset = Vector3.back * Mathf.Max(_minDistance, 1f);

            _zoomDirection = offset.normalized;
            _currentDistance = Mathf.Clamp(offset.magnitude, _minDistance, _maxDistance);
            _targetDistance = _currentDistance;
            _transposer.m_FollowOffset = _zoomDirection * _currentDistance;
            _isInitialized = true;
        }

        private void HandleZoomInput()
        {
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(scroll, 0f))
                return;

            if (_ignoreWhenPointerOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            _targetDistance = Mathf.Clamp(_targetDistance - scroll * _zoomStep, _minDistance, _maxDistance);
        }

        private void ApplyZoom()
        {
            if (Mathf.Approximately(_currentDistance, _targetDistance))
                return;

            if (_zoomSmoothTime <= 0f)
            {
                _currentDistance = _targetDistance;
            }
            else
            {
                _currentDistance = Mathf.SmoothDamp(
                    _currentDistance,
                    _targetDistance,
                    ref _zoomVelocity,
                    _zoomSmoothTime);
            }

            _transposer.m_FollowOffset = _zoomDirection * _currentDistance;
        }

        private void OnValidate()
        {
            _minDistance = Mathf.Max(0.1f, _minDistance);
            _maxDistance = Mathf.Max(_minDistance, _maxDistance);
            _zoomStep = Mathf.Max(0.1f, _zoomStep);
            _zoomSmoothTime = Mathf.Max(0f, _zoomSmoothTime);
        }
    }
}
