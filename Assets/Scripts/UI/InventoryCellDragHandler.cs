using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace MineArena.UI
{
    public class InventoryCellDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Canvas _canvas;

        private InventoryCellUI _cellUI;
        private Canvas _rootCanvas;
        private RectTransform _rootCanvasRect;
        private RectTransform _draggedVisual;
        private CanvasGroup _originalIconCanvasGroup;
        private float _originalIconAlpha = 1f;
        private bool _originalBlocksRaycasts = true;

        private void Awake()
        {
            _cellUI = GetComponent<InventoryCellUI>();
            ResolveCanvasReferences();
        }

        private void ResolveCanvasReferences()
        {
            if (_rootCanvas != null)
                return;

            var canvas = _canvas != null ? _canvas : GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            _rootCanvas = canvas.rootCanvas;
            _rootCanvasRect = _rootCanvas != null ? _rootCanvas.transform as RectTransform : null;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_cellUI == null || !_cellUI.HasItem)
                return;

            ResolveCanvasReferences();
            if (_rootCanvasRect == null)
                return;

            var sourceRect = _cellUI.ActiveIconRectTransform ?? _cellUI.Icon?.rectTransform;
            if (sourceRect == null)
                return;

            _draggedVisual = Instantiate(sourceRect.gameObject, _rootCanvasRect).GetComponent<RectTransform>();
            if (_draggedVisual == null)
                return;

            PrepareDraggedVisual(sourceRect, _draggedVisual);
            UpdateDraggedVisualPosition(eventData);

            _originalIconCanvasGroup = _cellUI.ActiveIconCanvasGroup;
            if (_originalIconCanvasGroup != null)
            {
                _originalIconAlpha = _originalIconCanvasGroup.alpha;
                _originalBlocksRaycasts = _originalIconCanvasGroup.blocksRaycasts;
                _originalIconCanvasGroup.alpha = 0f;
                _originalIconCanvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_draggedVisual == null || _rootCanvasRect == null)
                return;

            UpdateDraggedVisualPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_draggedVisual == null)
            {
                RestoreOriginalIconVisual();
                return;
            }

            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            InventoryCellUI targetCell = null;
            foreach (var result in raycastResults)
            {
                var cell = result.gameObject.GetComponent<InventoryCellUI>();
                if (cell != null && cell != _cellUI)
                {
                    targetCell = cell;
                    break;
                }
            }

            if (targetCell != null)
            {
                var inventoryUI = GetComponentInParent<InventoryWindow>();
                if (inventoryUI != null)
                {
                    int fromIndex = _cellUI.transform.GetSiblingIndex();
                    int toIndex = targetCell.transform.GetSiblingIndex();
                    inventoryUI.MoveItem(fromIndex, toIndex);
                }
            }

            CleanupDraggedVisual();
            RestoreOriginalIconVisual();
        }

        private void UpdateDraggedVisualPosition(PointerEventData eventData)
        {
            if (_rootCanvas == null || _draggedVisual == null)
                return;

            Camera eventCamera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootCanvasRect, eventData.position, eventCamera, out var localPoint))
            {
                _draggedVisual.anchoredPosition = localPoint;
            }
        }

        private static void PrepareDraggedVisual(RectTransform sourceRect, RectTransform visual)
        {
            visual.anchorMin = visual.anchorMax = new Vector2(0.5f, 0.5f);
            visual.pivot = new Vector2(0.5f, 0.5f);
            visual.localScale = Vector3.one;
            visual.sizeDelta = sourceRect.rect.size;
            visual.SetAsLastSibling();

            foreach (var group in visual.GetComponentsInChildren<CanvasGroup>(true))
            {
                group.blocksRaycasts = false;
            }

            foreach (var graphic in visual.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        private void CleanupDraggedVisual()
        {
            if (_draggedVisual != null)
            {
                Destroy(_draggedVisual.gameObject);
                _draggedVisual = null;
            }
        }

        private void RestoreOriginalIconVisual()
        {
            if (_originalIconCanvasGroup != null)
            {
                _originalIconCanvasGroup.alpha = _originalIconAlpha;
                _originalIconCanvasGroup.blocksRaycasts = _originalBlocksRaycasts;
                _originalIconCanvasGroup = null;
            }
        }
    }
}
