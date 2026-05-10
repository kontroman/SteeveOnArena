using MineArena.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MineArena.UI
{
    [RequireComponent(typeof(Image))]
    public class ArmorEquipmentSlotUI : MonoBehaviour, IInventoryItemDropTarget,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private const float IconPadding = 10f;

        private InventoryWindow _owner;
        private ArmorSlot _slot;
        private Image _background;
        private Image _icon;
        private CanvasGroup _iconCanvasGroup;
        private ArmorConfig _armor;
        private Canvas _rootCanvas;
        private RectTransform _rootCanvasRect;
        private RectTransform _draggedVisual;
        private float _originalIconAlpha = 1f;

        public ArmorSlot Slot => _slot;

        public void Initialize(InventoryWindow owner, ArmorSlot slot, Image background)
        {
            _owner = owner;
            _slot = slot;
            _background = background != null ? background : GetComponent<Image>();

            if (_background != null)
                _background.raycastTarget = true;

            EnsureIcon();
            SetArmor(null);
        }

        public bool TryDropInventoryItem(Item item)
        {
            return _owner != null && _owner.TryEquipArmorItem(_slot, item);
        }

        public void SetArmor(ArmorConfig armor)
        {
            _armor = armor;
            EnsureIcon();

            if (_icon == null)
                return;

            _icon.sprite = armor != null ? armor.Icon : null;
            _icon.enabled = armor != null && armor.Icon != null;
            _icon.gameObject.SetActive(_icon.enabled);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_armor == null || _icon == null || !_icon.gameObject.activeInHierarchy)
                return;

            ResolveCanvasReferences();
            if (_rootCanvasRect == null)
                return;

            _draggedVisual = Instantiate(_icon.gameObject, _rootCanvasRect).GetComponent<RectTransform>();
            if (_draggedVisual == null)
                return;

            PrepareDraggedVisual(_icon.rectTransform, _draggedVisual);
            UpdateDraggedVisualPosition(eventData);

            if (_iconCanvasGroup != null)
            {
                _originalIconAlpha = _iconCanvasGroup.alpha;
                _iconCanvasGroup.alpha = 0f;
            }
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
            {
                RestoreIcon();
                return;
            }

            if (_owner != null && _owner.IsInventoryDropArea(eventData, this))
                _owner.TryUnequipArmor(_slot);

            Destroy(_draggedVisual.gameObject);
            _draggedVisual = null;
            RestoreIcon();
        }

        private void EnsureIcon()
        {
            if (_icon != null)
                return;

            var iconTransform = transform.Find("EquippedIcon");
            _icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

            if (_icon == null)
            {
                var iconObject = new GameObject("EquippedIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconObject.transform.SetParent(transform, false);
                _icon = iconObject.GetComponent<Image>();
            }

            var rectTransform = _icon.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(IconPadding, IconPadding);
            rectTransform.offsetMax = new Vector2(-IconPadding, -IconPadding);
            rectTransform.localScale = Vector3.one;

            _icon.preserveAspect = true;
            _icon.raycastTarget = false;
            _icon.enabled = false;
            _icon.gameObject.SetActive(false);

            _iconCanvasGroup = _icon.GetComponent<CanvasGroup>();
            if (_iconCanvasGroup == null)
                _iconCanvasGroup = _icon.gameObject.AddComponent<CanvasGroup>();

            _iconCanvasGroup.blocksRaycasts = false;
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

        private void UpdateDraggedVisualPosition(PointerEventData eventData)
        {
            if (_rootCanvas == null || _rootCanvasRect == null || _draggedVisual == null)
                return;

            var eventCamera = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_rootCanvasRect, eventData.position, eventCamera, out var localPoint))
                _draggedVisual.anchoredPosition = localPoint;
        }

        private void RestoreIcon()
        {
            if (_iconCanvasGroup != null)
                _iconCanvasGroup.alpha = _originalIconAlpha;
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
    }
}
