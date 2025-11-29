using UnityEngine;
using UnityEngine.UI;

namespace UI.UIAchievement
{
    /// <summary>
    /// Adjusts a ScrollRect so that a required number of achievement items fit vertically without scaling.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class AchievementScrollRectSizer : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _visibleItems = 3;
        [SerializeField] private float _extraViewportPadding;
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;

        private RectTransform _scrollTransform;
        private RectTransform _viewport;
        private VerticalLayoutGroup _layoutGroup;

        public int VisibleItems
        {
            get => _visibleItems;
            set
            {
                int clamped = Mathf.Max(1, value);
                if (_visibleItems == clamped)
                    return;

                _visibleItems = clamped;
                Refresh();
            }
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnEnable()
        {
            Refresh();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _visibleItems = Mathf.Max(1, _visibleItems);
            CacheReferences();
            Refresh();
        }
#endif

        public void Refresh()
        {
            RefreshInternal(null);
        }

        public void RefreshWithVisibleCount(int visibleCount)
        {
            _visibleItems = Mathf.Max(1, visibleCount);
            RefreshInternal(null);
        }

        public void RefreshWithVisibleCount(int visibleCount, int? totalItemsOverride)
        {
            _visibleItems = Mathf.Max(1, visibleCount);
            RefreshInternal(totalItemsOverride);
        }

        public void RefreshForItems(int totalItems)
        {
            RefreshInternal(totalItems);
        }

        private void RefreshInternal(int? totalItemsOverride)
        {
            CacheReferences();

            if (_scrollRect == null || _viewport == null || _content == null || _layoutGroup == null)
                return;

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_content);

            float itemHeight = GetItemHeight();
            if (itemHeight <= 0f)
                return;

            int activeItems = totalItemsOverride ?? GetActiveChildrenCount();
            float spacing = _layoutGroup.spacing;
            float padding = _layoutGroup.padding.top + _layoutGroup.padding.bottom;
            int visibleCount = Mathf.Clamp(_visibleItems, 1, activeItems > 0 ? activeItems : int.MaxValue);

            float desiredHeight = CalculateHeight(itemHeight, visibleCount, spacing, padding) + _extraViewportPadding;
            float contentHeight = CalculateHeight(itemHeight, activeItems, spacing, padding);

            ApplyContentHeight(Mathf.Max(desiredHeight, contentHeight, GetViewportHeight()));
        }

        private static float CalculateHeight(float itemHeight, int items, float spacing, float padding)
        {
            if (items <= 0)
                return padding;

            return itemHeight * items + spacing * Mathf.Max(0, items - 1) + padding;
        }

        private void ApplyContentHeight(float height)
        {
            if (_content == null)
                return;

            ResizeKeepingTop(_content, Mathf.Max(0f, height));
        }

        private float GetViewportHeight()
        {
            if (_viewport != null)
                return _viewport.rect.height;

            return _scrollTransform != null ? _scrollTransform.rect.height : 0f;
        }

        private static void ResizeKeepingTop(RectTransform rect, float height)
        {
            if (rect == null)
                return;

            float previousHeight = rect.rect.height;
            float pivotFactor = 1f - rect.pivot.y;
            float top = rect.anchoredPosition.y + pivotFactor * previousHeight;

            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, top - pivotFactor * height);
        }

        private float GetItemHeight()
        {
            for (int i = 0; i < _content.childCount; i++)
            {
                if (_content.GetChild(i) is not RectTransform child || !child.gameObject.activeSelf)
                    continue;

                float preferred = LayoutUtility.GetPreferredHeight(child);
                if (preferred <= 0f)
                    preferred = child.rect.height;

                if (preferred > 0f)
                    return preferred;
            }

            return 0f;
        }

        private int GetActiveChildrenCount()
        {
            int count = 0;

            for (int i = 0; i < _content.childCount; i++)
            {
                if (_content.GetChild(i).gameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private void CacheReferences()
        {
            if (_scrollRect == null)
                _scrollRect = GetComponent<ScrollRect>();

            if (_scrollRect != null)
            {
                _scrollTransform = _scrollRect.transform as RectTransform;

                if (_viewport == null)
                    _viewport = _scrollRect.viewport != null ? _scrollRect.viewport : _scrollTransform;

                if (_content == null)
                    _content = _scrollRect.content;
            }

            if (_content != null && _layoutGroup == null)
                _layoutGroup = _content.GetComponent<VerticalLayoutGroup>();
        }
    }
}
