using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Devotion.UI
{
    public class InventoryCellDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Canvas _canvas;

        private RectTransform _iconRectTransform;
        private CanvasGroup _iconCanvasGroup;
        private Vector2 _originalIconPosition;

        private InventoryCellUI _cellUI;

        private void Awake()
        {
            _cellUI = GetComponent<InventoryCellUI>();
            _iconRectTransform = _cellUI.Icon.GetComponent<RectTransform>();
            _iconCanvasGroup = _cellUI.Icon.GetComponent<CanvasGroup>();
            _canvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_cellUI.HasItem) return;

            _originalIconPosition = _iconRectTransform.anchoredPosition;
            _iconCanvasGroup.blocksRaycasts = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_cellUI.HasItem) return;

            _iconRectTransform.anchoredPosition += eventData.delta / _canvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_cellUI.HasItem) return;

            _iconCanvasGroup.blocksRaycasts = true;

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
                var inventoryUI = GetComponentInParent<InventoryUI>();
                if (inventoryUI != null)
                {
                    int fromIndex = _cellUI.transform.GetSiblingIndex();
                    int toIndex = targetCell.transform.GetSiblingIndex();
                    inventoryUI.MoveItem(fromIndex, toIndex);
                }
            }

            _iconRectTransform.anchoredPosition = _originalIconPosition;
        }
    }
}
