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
            if (MineArena.UI.MobileGameInput.Blocked) return;
            float scroll = Input.mouseScrollDelta.y;
            if (MineArena.UI.MobileGameInput.Enabled && Input.touchCount > 0)
            {
                if (!MineArena.UI.MobileGameInput.Pinching) return;
                if (Input.touchCount != 2) return;
                var first = Input.GetTouch(0);
                var second = Input.GetTouch(1);
                if (MineArena.UI.MobileTouchControl.OwnsPointer(first.fingerId) || MineArena.UI.MobileTouchControl.OwnsPointer(second.fingerId)) return;
                if (first.phase == TouchPhase.Began || second.phase == TouchPhase.Began ||
                    first.phase == TouchPhase.Ended || second.phase == TouchPhase.Ended ||
                    first.phase == TouchPhase.Canceled || second.phase == TouchPhase.Canceled) return;
                float previous = Vector2.Distance(first.position - first.deltaPosition, second.position - second.deltaPosition);
                scroll = (Vector2.Distance(first.position, second.position) - previous) / Mathf.Max(1, Mathf.Min(Screen.width, Screen.height)) * 12f;
            }
            if (Mathf.Approximately(scroll, 0f))
                return;

            if (!MineArena.UI.MobileGameInput.Enabled && _ignoreWhenPointerOverUi && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            _targetDistance = Mathf.Clamp(_targetDistance - MineArena.UI.CameraSensitivity.Apply(scroll, _zoomStep), _minDistance, _maxDistance);
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

        public void SetFollowOffset(Vector3 followOffset)
        {
            if (!_isInitialized)
                Initialize();

            if (!_isInitialized || followOffset.sqrMagnitude <= Mathf.Epsilon)
                return;

            _zoomDirection = followOffset.normalized;
            _currentDistance = Mathf.Clamp(followOffset.magnitude, _minDistance, _maxDistance);
            _targetDistance = _currentDistance;
            _zoomVelocity = 0f;
            _transposer.m_FollowOffset = _zoomDirection * _currentDistance;
        }

        public void SetDistanceLimits(float minDistance, float maxDistance)
        {
            _minDistance = Mathf.Max(0.1f, minDistance);
            _maxDistance = Mathf.Max(_minDistance, maxDistance);

            if (!_isInitialized)
                Initialize();

            if (!_isInitialized)
                return;

            _currentDistance = Mathf.Clamp(_currentDistance, _minDistance, _maxDistance);
            _targetDistance = Mathf.Clamp(_targetDistance, _minDistance, _maxDistance);
            _zoomVelocity = 0f;
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
