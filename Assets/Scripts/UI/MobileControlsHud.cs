using Devotion.SDK.Controllers;
using MineArena.Controllers;
using MineArena.Items;
using MineArena.Managers;
using MineArena.PlayerSystem;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace MineArena.UI
{
    [DefaultExecutionOrder(-200)]
    public sealed class MobileControlsHud : MonoBehaviour
    {
        private RectTransform safe;
        private GameObject controls;
        private MobileTouchControl shield, interact, aim, move;
        private readonly System.Collections.Generic.List<RaycastResult> pointerHits = new System.Collections.Generic.List<RaycastResult>();
        private Rect previousArea;
        private Vector2 previousScreen;
        private bool wasBlocked;
        private RectTransform quickbar;
        private Vector3 quickbarScale;
        private Vector2 quickbarPosition;
        private bool adjustedQuickbar;
        private Sprite pickaxeIcon;
        [SerializeField, Tooltip("Show touch controls in the Editor Game view.")] private bool preview;
        public static void Install(Transform parent)
        {
            if (parent.GetComponent<MobileControlsHud>() == null) parent.gameObject.AddComponent<MobileControlsHud>();
        }
        public static float LayoutScale(Vector2 size) => Mathf.Min(1f, size.x / 1150f, size.y / 650f);
        private void Update()
        {
            bool enabled = Application.isMobilePlatform || (!Application.isEditor && Input.touchSupported) || preview || MobileGameInput.PreviewInEditor;
            MobileGameInput.Enabled = enabled;
            if (enabled && controls == null) Build();
            bool blocked = MobileGameInput.Blocked;
            if (blocked || !enabled)
            {
                MobileGameInput.Reset();
                if (!wasBlocked && controls != null) ResetTouches();
            }
            wasBlocked = blocked || !enabled;
            if (controls == null) return;
            if (!enabled) RestoreQuickbar();
            controls.SetActive(enabled && !blocked);
            if (!enabled || blocked) return;
            Rect area = Screen.safeArea;
            var screen = new Vector2(Screen.width, Screen.height);
            if (area != previousArea || screen != previousScreen) ResetTouches();
            previousArea = area; previousScreen = screen;
            safe.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            safe.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            Arrange(area);
            HandleFloatingInput(area);
            var equipment = Player.Instance != null ? Player.Instance.GetComponent<PlayerEquipment>() : null;
            bool canBlock = equipment != null && equipment.Shield != null && equipment.LastActiveHandItem != HandItemType.Bow;
            shield.gameObject.SetActive(canBlock);
            if (!canBlock) MobileGameInput.ShieldHeld = false;
            SetIcon(shield, canBlock ? equipment.Shield.Icon : null);
            var manager = GameRoot.GetManager<InteractionManager>();
            interact.gameObject.SetActive(manager != null && manager.HasMobileInteraction);
            bool mining = manager != null && manager.IsMining;
            var glyph = interact.GetComponentInChildren<MobilePixelGraphic>();
            glyph.Stop = mining;
            SetIcon(interact, mining ? null : pickaxeIcon);
            var progress = GameRoot.PlayerProgress?.InventoryProgress;
            var item = progress != null ? GameRoot.GameConfig?.ItemDatabase?.GetItemConfig(progress.GetQuickSlotItemId(progress.SelectedQuickSlotIndex)) : null;
            SetIcon(aim, item is WeaponItemConfig ? item.Icon : null);
        }
        private void HandleFloatingInput(Rect area)
        {
            var canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
            // Reserve two fingers in the upper play area for zoom until both lift.
            if (Input.touchCount == 0) MobileGameInput.Pinching = false;
            if (!MobileGameInput.Pinching && Input.touchCount == 2)
            {
                var first = Input.GetTouch(0);
                var second = Input.GetTouch(1);
                if (first.position.y > area.yMin + area.height * .45f && second.position.y > area.yMin + area.height * .45f &&
                    !IsOverHud(first.fingerId, first.position) && !IsOverHud(second.fingerId, second.position))
                {
                    move.Release();
                    aim.Release();
                    MobileGameInput.Pinching = true;
                }
            }
            if (MobileGameInput.Pinching) return;
            if (Input.touchCount > 0)
            {
                if (move.HasPointer(-1)) move.Release();
                if (aim.HasPointer(-1)) aim.Release();
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var touch = Input.GetTouch(i);
                    RoutePointer(touch.fingerId, touch.position, touch.phase == TouchPhase.Began,
                        touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled, area, camera);
                }
                ReleaseMissingTouch(move);
                ReleaseMissingTouch(aim);
            }
            else
            {
                RoutePointer(-1, Input.mousePosition, Input.GetMouseButtonDown(0), Input.GetMouseButtonUp(0), area, camera);
                // Recover missing/canceled touches and mouse releases outside the Game view.
                if (move.IsHeld && (!move.HasPointer(-1) || !Input.GetMouseButton(0))) move.Release();
                if (aim.IsHeld && (!aim.HasPointer(-1) || !Input.GetMouseButton(0))) aim.Release();
            }
        }
        private static void ReleaseMissingTouch(MobileTouchControl stick)
        {
            if (!stick.IsHeld) return;
            for (int i = 0; i < Input.touchCount; i++)
                if (stick.HasPointer(Input.GetTouch(i).fingerId)) return;
            stick.Release();
        }
        private void RoutePointer(int id, Vector2 position, bool began, bool ended, Rect area, Camera camera)
        {
            var captured = move.HasPointer(id) ? move : aim.HasPointer(id) ? aim : null;
            if (captured != null)
            {
                if (ended) captured.Release();
                else captured.UpdateFloating(id, position, camera);
                return;
            }
            if (!began || !area.Contains(position) || IsOverHud(id, position)) return;
            var target = position.x < Screen.width * .5f ? move : aim;
            target.BeginFloating(id, position, camera);
        }
        private bool IsOverHud(int id, Vector2 position)
        {
            if (EventSystem.current == null) return false;
            pointerHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { pointerId = id, position = position }, pointerHits);
            foreach (var hit in pointerHits)
                if (hit.module is GraphicRaycaster) return true;
            return false;
        }
        private void Arrange(Rect area)
        {
            float scale = LayoutScale(safe.rect.size);
            foreach (Transform child in safe) child.localScale = Vector3.one * scale;
            if (quickbar == null) quickbar = GetComponent<Devotion.SDK.UI.PlayingWindow>()?.QuickAccessPanel;
            if (quickbar != null)
            {
                if (!adjustedQuickbar) { quickbarScale = quickbar.localScale; quickbarPosition = quickbar.anchoredPosition; adjustedQuickbar = true; }
                // Keep the existing HUD size unless a narrow screen needs more thumb room.
                float available = Mathf.Max(240f * scale, safe.rect.width - 800f * scale);
                float barScale = Mathf.Min(quickbarScale.x, scale, available / Mathf.Max(1, quickbar.rect.width));
                quickbar.localScale = Vector3.one * barScale;
                var parent = (RectTransform)transform;
                quickbar.anchoredPosition = new Vector2((area.center.x / Screen.width - .5f) * parent.rect.width,
                    area.yMin / Screen.height * parent.rect.height + 24f * scale);
            }
        }
        private void Build()
        {
            controls = new GameObject("MobileControls", typeof(RectTransform));
            controls.transform.SetParent(transform, false);
            safe = (RectTransform)controls.transform;
            safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            move = Create("Move", MobileTouchControl.Control.Move, Vector2.zero, new Vector2(142, 154), true);
            aim = Create("Aim", MobileTouchControl.Control.Aim, Vector2.right, new Vector2(-220, 154), true);
            shield = Create("Shield", MobileTouchControl.Control.Shield, Vector2.right, new Vector2(-376, 154));
            Create("Jump", MobileTouchControl.Control.Jump, Vector2.right, new Vector2(-84, 244));
            interact = Create("Interact", MobileTouchControl.Control.Interact, Vector2.right, new Vector2(-232, 324));
            var items = GameRoot.GameConfig?.ItemDatabase?.AllItems;
            if (items != null) foreach (var item in items)
                if (item != null && item.Name != null && item.Name.IndexOf("pickaxe", System.StringComparison.OrdinalIgnoreCase) >= 0 && item.Icon != null) { pickaxeIcon = item.Icon; break; }
        }
        private MobileTouchControl Create(string name, MobileTouchControl.Control action, Vector2 anchor, Vector2 position, bool stick = false)
        {
            var group = new GameObject(name + "Anchor", typeof(RectTransform));
            group.transform.SetParent(safe, false);
            var container = (RectTransform)group.transform;
            container.anchorMin = container.anchorMax = anchor;
            container.anchoredPosition = Vector2.zero; container.sizeDelta = Vector2.zero;
            // Transparent padding keeps touch targets larger than the visible buttons.
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(MobileTouchControl));
            go.transform.SetParent(container, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = Vector2.one * (stick ? 264 : 144); rect.anchoredPosition = position;
            go.GetComponent<Image>().color = Color.clear;
            go.GetComponent<Image>().raycastTarget = !stick;
            var touch = go.GetComponent<MobileTouchControl>(); touch.Action = action;
            if (stick)
            {
                touch.Floating = true;
                touch.Visibility = go.AddComponent<CanvasGroup>();
                touch.Visibility.alpha = 0;
                touch.Visibility.blocksRaycasts = false;
            }
            var face = new GameObject("Face", typeof(RectTransform), typeof(MobilePixelGraphic));
            face.transform.SetParent(rect, false);
            ((RectTransform)face.transform).sizeDelta = Vector2.one * (stick ? 238 : 104);
            var background = face.GetComponent<MobilePixelGraphic>();
            background.Stick = stick; background.Action = action; background.raycastTarget = false;
            Transform iconParent = rect;
            if (stick)
            {
                var thumb = new GameObject("Thumb", typeof(RectTransform), typeof(MobilePixelGraphic));
                thumb.transform.SetParent(rect, false);
                touch.Thumb = (RectTransform)thumb.transform;
                touch.Thumb.sizeDelta = Vector2.one * 72;
                var graphic = thumb.GetComponent<MobilePixelGraphic>(); graphic.Thumb = true; graphic.raycastTarget = false;
                iconParent = thumb.transform;
            }
            var icon = new GameObject("PixelIcon", typeof(RectTransform), typeof(MobilePixelGraphic));
            icon.transform.SetParent(iconParent, false);
            ((RectTransform)icon.transform).sizeDelta = Vector2.one * (stick ? 40 : 58);
            var fallback = icon.GetComponent<MobilePixelGraphic>(); fallback.Icon = true; fallback.Action = action; fallback.raycastTarget = false;
            var sprite = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
            sprite.transform.SetParent(iconParent, false);
            ((RectTransform)sprite.transform).sizeDelta = Vector2.one * (stick ? 44 : 64);
            var image = sprite.GetComponent<Image>(); image.preserveAspect = true; image.raycastTarget = false; image.enabled = false;
            return touch;
        }
        private static void SetIcon(MobileTouchControl control, Sprite sprite)
        {
            var parent = control.Thumb != null ? control.Thumb : control.transform;
            var image = parent.Find("ItemIcon").GetComponent<Image>();
            image.sprite = sprite; image.enabled = sprite != null;
            var fallback = parent.Find("PixelIcon").GetComponent<MobilePixelGraphic>();
            fallback.enabled = sprite == null;
            bool stop = control.Action == MobileTouchControl.Control.Interact && control.GetComponentInChildren<MobilePixelGraphic>().Stop;
            if (fallback.Stop != stop) { fallback.Stop = stop; fallback.SetVerticesDirty(); }
        }
        private void OnDisable() { ResetTouches(); RestoreQuickbar(); MobileGameInput.Enabled = false; }
        private void RestoreQuickbar()
        {
            if (adjustedQuickbar && quickbar != null) { quickbar.localScale = quickbarScale; quickbar.anchoredPosition = quickbarPosition; }
            adjustedQuickbar = false;
        }
        private void OnApplicationFocus(bool focus) { if (!focus) ResetTouches(); }
        private void OnApplicationPause(bool pause) { if (pause) ResetTouches(); }
        private void ResetTouches()
        {
            MobileGameInput.Reset();
            if (controls != null) foreach (var control in controls.GetComponentsInChildren<MobileTouchControl>(true)) control.Release();
        }
    }
}
