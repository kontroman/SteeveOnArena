using DG.Tweening;
using UnityEngine;

namespace MineArena.UI
{
    [DisallowMultipleComponent]
    public sealed class WindowAnimation : MonoBehaviour
    {
        private CanvasGroup _group;
        private RectTransform _panel;
        private Vector2 _restPosition;
        private float _alpha;
        private bool _interactable;
        private Sequence _animation;
        public bool IsClosing { get; private set; }

        public static WindowAnimation For(GameObject window)
        {
            var animation = window.GetComponent<WindowAnimation>();
            if (animation == null) animation = window.AddComponent<WindowAnimation>();
            // Awake is deferred when the window is inactive.
            animation.EnsureInitialized();
            return animation;
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_group != null) return;
            // Unity's missing-component wrappers require its overloaded null check.
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _alpha = _group.alpha;
            _interactable = _group.interactable;
            // Slide only the dialog; the fullscreen dimmer stays stationary.
            _panel = transform.Find("PixelCraftPanel") as RectTransform;
            if (_panel == null) _panel = transform.Find("WindowPanel") as RectTransform;
            if (_panel == null) _panel = transform.Find("Panel") as RectTransform;
            if (_panel == null) _panel = transform.Find("BeigeWindow") as RectTransform;
            if (_panel != null) _restPosition = _panel.anchoredPosition;
        }

        public void Show()
        {
            EnsureInitialized();
            bool reversing = IsClosing;
            _animation?.Kill();
            IsClosing = false;
            gameObject.SetActive(true);
            _group.interactable = _interactable;
            if (!reversing)
            {
                _group.alpha = 0f;
                if (_panel != null) _panel.anchoredPosition = _restPosition + Vector2.down * 56f;
            }
            _animation = DOTween.Sequence().SetUpdate(true);
            _animation.Join(_group.DOFade(_alpha, 0.10f).SetEase(Ease.OutCubic));
            if (_panel != null) _animation.Join(_panel.DOAnchorPos(_restPosition, 0.22f).SetEase(Ease.OutQuart));
        }

        public void Hide()
        {
            if (IsClosing || !gameObject.activeInHierarchy) return;
            EnsureInitialized();
            _animation?.Kill();
            IsClosing = true;
            _group.interactable = false;
            _animation = DOTween.Sequence().SetUpdate(true);
            _animation.Join(_group.DOFade(0f, 0.12f).SetEase(Ease.InCubic));
            if (_panel != null) _animation.Join(_panel.DOAnchorPos(_restPosition + Vector2.down * 32f, 0.12f).SetEase(Ease.InCubic));
            _animation.OnComplete(() => gameObject.SetActive(false));
        }

        private void OnDisable()
        {
            _animation?.Kill();
            _animation = null;
            IsClosing = false;
            if (_group != null) { _group.alpha = _alpha; _group.interactable = _interactable; }
            if (_panel != null) _panel.anchoredPosition = _restPosition;
        }
    }
}
