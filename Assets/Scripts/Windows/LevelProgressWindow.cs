using Devotion.SDK.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows
{
    public class LevelProgressWindow : BaseWindow
    {
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private RectTransform _portalArrow;
        [SerializeField] private Camera _targetCamera;

        private Transform _portalTarget;
        private Transform _playerTarget;

        private void Awake()
        {
            SetProgress(0, 0);
            SetPortalTarget(null, null);
        }

        private void Update()
        {
            UpdatePortalArrow();
        }

        public void SetProgress(int killedMobs, int totalMobs)
        {
            if (_progressBar == null)
                return;

            float progress = totalMobs > 0 ? Mathf.Clamp01((float)killedMobs / totalMobs) : 0f;
            _progressBar.value = progress;

            if (_progressText != null)
                _progressText.text = $"{killedMobs}/{totalMobs}";
        }

        public void SetPortalTarget(Transform portal, Transform player)
        {
            _portalTarget = portal;
            _playerTarget = player;

            if (_portalArrow != null)
                _portalArrow.gameObject.SetActive(_portalTarget != null && _playerTarget != null);
        }

        private void UpdatePortalArrow()
        {
            if (_portalArrow == null || _portalTarget == null || _playerTarget == null)
                return;

            Camera camera = _targetCamera != null ? _targetCamera : Camera.main;
            if (camera == null)
                return;

            Vector3 toPortal = _portalTarget.position - _playerTarget.position;
            Vector3 cameraForward = camera.transform.forward;
            Vector3 cameraRight = camera.transform.right;
            toPortal.y = 0f;
            cameraForward.y = 0f;
            cameraRight.y = 0f;

            if (toPortal.sqrMagnitude <= 0.0001f || cameraForward.sqrMagnitude <= 0.0001f || cameraRight.sqrMagnitude <= 0.0001f)
                return;

            float angle = Mathf.Atan2(Vector3.Dot(toPortal.normalized, cameraRight.normalized), Vector3.Dot(toPortal.normalized, cameraForward.normalized)) * Mathf.Rad2Deg;
            _portalArrow.localRotation = Quaternion.Euler(0f, 0f, -angle + 180f);
        }
    }
}
