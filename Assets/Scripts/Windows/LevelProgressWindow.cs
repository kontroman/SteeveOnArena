using Devotion.SDK.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UI.UIAchievement;

namespace MineArena.Windows
{
    public class LevelProgressWindow : BaseWindow
    {
        [SerializeField] private GameObject _progressPanel;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private RectTransform _portalArrow;
        [SerializeField] private Camera _targetCamera;

        private Transform _portalTarget;
        private Transform _playerTarget;
        private Tween _popupShift;
        private int _killedMobs, _totalMobs;
        private SystemLanguage _textLanguage;

        private void Awake()
        {
            SetProgress(0, 0);
            SetPortalTarget(null, null);
        }

        private void Update()
        {
            RefreshProgressVisibility();
            UpdatePortalArrow();
            if (_textLanguage != Devotion.SDK.Services.Localization.LocalizationService.CurrentLanguage)
                RefreshProgressText();
        }

        private void OnEnable()
        {
            RefreshProgressVisibility();
            AchievementPopup.OccupiedHeightChanged += ShiftForPopup;
            if (_progressPanel != null)
                ((RectTransform)_progressPanel.transform).anchoredPosition = new Vector2(0, -AchievementPopup.OccupiedHeight);
        }

        private void ShiftForPopup(float height)
        {
            _popupShift?.Kill();
            if (_progressPanel == null) return;
            _popupShift = ((RectTransform)_progressPanel.transform)
                .DOAnchorPosY(-height, MineArena.Basics.Constants.QuestPopup.Duration)
                .SetEase(Ease.OutCubic).SetUpdate(true);
        }

        private void OnDisable()
        {
            AchievementPopup.OccupiedHeightChanged -= ShiftForPopup;
            _popupShift?.Kill();
            _popupShift = null;
        }

        private void RefreshProgressVisibility()
        {
            if (_progressPanel == null) return;
            bool visible = !MineArena.Managers.TutorialService.Active;
            if (_progressPanel.activeSelf != visible) _progressPanel.SetActive(visible);
        }

        public void SetProgress(int killedMobs, int totalMobs)
        {
            _killedMobs = killedMobs;
            _totalMobs = totalMobs;
            float progress = totalMobs > 0 ? Mathf.Clamp01((float)killedMobs / totalMobs) : 0f;
            if (_progressBar != null) _progressBar.value = progress;
            RefreshProgressText();
        }

        private void RefreshProgressText()
        {
            _textLanguage = Devotion.SDK.Services.Localization.LocalizationService.CurrentLanguage;
            if (_progressText != null)
                _progressText.text = _totalMobs > 0 && _killedMobs >= _totalMobs
                    ? Devotion.SDK.Services.Localization.LocalizationService.GetLocalizedText("Arena.GoToPortal")
                    : $"{_killedMobs}/{_totalMobs}";
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
