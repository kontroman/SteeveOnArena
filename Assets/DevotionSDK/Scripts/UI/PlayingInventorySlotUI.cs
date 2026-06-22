using MineArena.Items;
using MineArena.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Devotion.SDK.UI
{
    [RequireComponent(typeof(Image))]
    public class PlayingInventorySlotUI : MonoBehaviour, IPointerClickHandler, IInventoryItemDropTarget,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly Color SelectedColor = new Color(1f, 0.78f, 0.16f, 1f);

        private PlayingWindow _owner;
        private int _index;
        private Image _background;
        private Image _flatIcon;
        private ResourceIcon _resourceIcon;
        private Canvas _rootCanvas;
        private RectTransform _rootCanvasRect;
        private RectTransform _draggedVisual;

        public Image Background => _background;
        public ResourceIcon ResourceIcon => _resourceIcon;

        public void Initialize(PlayingWindow owner, int index, Image background, Image flatIcon, ResourceIcon resourceIcon)
        {
            _owner = owner;
            _index = index;
            _background = background;
            _flatIcon = flatIcon;
            _resourceIcon = resourceIcon;

            if (_background != null)
            {
                _background.enabled = true;
                _background.raycastTarget = true;
                _background.gameObject.SetActive(true);
            }

            ClearItem();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _owner?.SelectInventorySlot(_index);
        }

        public bool TryDropInventoryItem(Item item)
        {
            return _owner != null && _owner.TrySetInventorySlotItem(_index, item);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            var sourceRect = GetActiveItemRectTransform();
            if (sourceRect == null)
                return;

            ResolveCanvasReferences();
            if (_rootCanvasRect == null)
                return;

            _draggedVisual = Instantiate(sourceRect.gameObject, _rootCanvasRect).GetComponent<RectTransform>();
            if (_draggedVisual == null)
                return;

            PrepareDraggedVisual(sourceRect, _draggedVisual);
            UpdateDraggedVisualPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_draggedVisual == null)
                return;

            UpdateDraggedVisualPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_draggedVisual == null)
                return;

            if (IsOverInventory(eventData))
                _owner?.TryReturnInventorySlotItem(_index);

            Destroy(_draggedVisual.gameObject);
            _draggedVisual = null;
        }

        public void SetItem(Item item, Sprite fallbackSprite)
        {
            if (item == null)
            {
                ClearItem();
                return;
            }

            if (ShouldUseResourceIcon(item, out var stackableItem))
            {
                SetFlatIcon(null);

                if (_resourceIcon == null)
                    return;

                _resourceIcon.gameObject.SetActive(true);
                _resourceIcon.SetResource(stackableItem.Config);
                return;
            }

            if (_resourceIcon != null)
                _resourceIcon.gameObject.SetActive(false);

            SetFlatIcon(item.Icon != null ? item.Icon : fallbackSprite);
        }

        public void ClearItem()
        {
            SetFlatIcon(null);

            if (_resourceIcon != null)
            {
                _resourceIcon.SetSprite(null);
                _resourceIcon.gameObject.SetActive(false);
            }
        }

        public void SetSelected(bool selected, Sprite activeSprite, Sprite inactiveSprite)
        {
            if (_background == null)
                return;

            var sprite = selected ? activeSprite : inactiveSprite;
            if (sprite != null)
                _background.sprite = sprite;

            _background.enabled = true;
            _background.raycastTarget = true;
            _background.color = selected ? SelectedColor : Color.white;
        }

        private void ResolveCanvasReferences()
        {
            if (_rootCanvas != null)
                return;

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                return;

            _rootCanvas = canvas.rootCanvas;
            _rootCanvasRect = _rootCanvas != null ? _rootCanvas.transform as RectTransform : null;
        }

        private bool IsOverInventory(PointerEventData eventData)
        {
            if (EventSystem.current == null)
                return false;

            var raycastResults = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, raycastResults);

            foreach (var result in raycastResults)
            {
                if (result.gameObject == null)
                    continue;

                if (result.gameObject.GetComponentInParent<InventoryWindow>() != null)
                    return true;
            }

            return false;
        }

        private void UpdateDraggedVisualPosition(PointerEventData eventData)
        {
            if (_rootCanvas == null || _rootCanvasRect == null || _draggedVisual == null)
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

            foreach (var canvasGroup in visual.GetComponentsInChildren<CanvasGroup>(true))
                canvasGroup.blocksRaycasts = false;

            foreach (var graphic in visual.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }

        private void SetFlatIcon(Sprite sprite)
        {
            if (_flatIcon == null)
                return;

            _flatIcon.sprite = sprite;
            _flatIcon.enabled = sprite != null;
            _flatIcon.gameObject.SetActive(sprite != null);
        }

        private static bool ShouldUseResourceIcon(Item item, out StackableItem stackableItem)
        {
            stackableItem = item as StackableItem;
            return stackableItem != null && stackableItem.Config != null && stackableItem.Config.BlockStyleIcon;
        }

        private RectTransform GetActiveItemRectTransform()
        {
            if (_resourceIcon != null && _resourceIcon.gameObject.activeInHierarchy)
                return _resourceIcon.transform as RectTransform;

            if (_flatIcon != null && _flatIcon.gameObject.activeInHierarchy)
                return _flatIcon.rectTransform;

            return null;
        }
    }
}
