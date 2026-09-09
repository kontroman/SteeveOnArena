using MineArena.AI;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Game.UI
{
    // The owner stays on the pooled mob; only its presentation lives in screen space.
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private float _headPadding = 0.35f;
        [SerializeField] private Vector2 _size = new Vector2(86, 14);
        private static Canvas _overlay;
        private static int _viewCount;
        private MobHealth _health;
        private Renderer[] _renderers;
        private Collider[] _colliders;
        private Camera _camera;
        private EnemyHealthBarGraphic _view;
        private float _trail;
        private float _lastValue;
        private float _damageTime;
        private bool _reset = true;

        private void Awake()
        {
            _health = GetComponentInParent<MobHealth>();
            if (_health == null) { enabled = false; return; }
            _renderers = _health.GetComponentsInChildren<Renderer>(true);
            _colliders = _health.GetComponentsInChildren<Collider>(true);
        }

        private void OnEnable() => _reset = true;

        private void LateUpdate()
        {
            if (_health == null) return;
            if (_camera == null || !_camera.isActiveAndEnabled) _camera = Camera.main;
            if (_view == null) CreateView();
            float value = _health.MaxValue > 0 ? Mathf.Clamp01(_health.CurrentValue / _health.MaxValue) : 0;
            if (_reset || value > _lastValue) { _trail = value; _reset = false; }
            if (value < _lastValue) _damageTime = Time.time;
            _lastValue = value;
            if (Time.time - _damageTime > 0.2f) _trail = Mathf.MoveTowards(_trail, value, Time.deltaTime * 1.8f);
            if (_camera == null || value <= 0) { _view.gameObject.SetActive(false); return; }

            Vector3 head = _health.transform.position;
            float top = head.y;
            bool found = false;
            foreach (var body in _renderers)
            {
                if (body == null || !body.enabled || !body.gameObject.activeInHierarchy ||
                    !(body is MeshRenderer || body is SkinnedMeshRenderer)) continue;
                top = Mathf.Max(top, body.bounds.max.y);
                found = true;
            }
            if (!found)
                foreach (var body in _colliders)
                    if (body != null && body.enabled && !body.isTrigger && body.gameObject.activeInHierarchy)
                    { top = Mathf.Max(top, body.bounds.max.y); found = true; }
            head.y = (found ? top : head.y + 2f) + _headPadding;
            Vector3 point = _camera.WorldToScreenPoint(head);
            Rect viewport = _camera.pixelRect;
            bool visible = point.z > _camera.nearClipPlane && point.z < _camera.farClipPlane &&
                           viewport.Contains(new Vector2(point.x, point.y));
            _view.gameObject.SetActive(visible);
            if (!visible) return;
            _overlay.targetDisplay = _camera.targetDisplay;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_overlay.transform,
                point, null, out Vector2 local);
            _view.rectTransform.anchoredPosition = local;
            _view.SetHealth(value, _trail);
        }

        private void CreateView()
        {
            if (_overlay == null)
            {
                var root = new GameObject("Enemy health bars", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
                _overlay = root.GetComponent<Canvas>();
                _overlay.renderMode = RenderMode.ScreenSpaceOverlay;
                _overlay.sortingOrder = -100; // Above world geometry, below windows and the HUD.
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                _viewCount = 0;
            }
            var view = new GameObject("Enemy health", typeof(RectTransform), typeof(CanvasRenderer), typeof(EnemyHealthBarGraphic));
            view.transform.SetParent(_overlay.transform, false);
            _view = view.GetComponent<EnemyHealthBarGraphic>();
            _view.raycastTarget = false;
            _view.rectTransform.sizeDelta = _size;
            _viewCount++;
        }

        private void OnDisable()
        {
            if (_view != null) _view.gameObject.SetActive(false);
            _reset = true;
        }

        private void OnDestroy()
        {
            if (_view == null) return;
            Destroy(_view.gameObject);
            if (--_viewCount == 0 && _overlay != null)
            {
                Destroy(_overlay.gameObject);
                _overlay = null;
            }
        }
    }
}
