using UnityEngine;
using UnityEngine.EventSystems;
namespace MineArena.UI
{
    public sealed class MobileTouchControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public enum Control { Move, Aim, Shield, Jump, Interact, Potion }
        public Control Action;
        public RectTransform Thumb;
        public bool Floating;
        public CanvasGroup Visibility;
        private static readonly System.Collections.Generic.HashSet<int> ActivePointers = new System.Collections.Generic.HashSet<int>();
        public static bool OwnsPointer(int id) => ActivePointers.Contains(id);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPointers() => ActivePointers.Clear();
        private int pointer = int.MinValue;
        public bool IsHeld => pointer != int.MinValue;
        public bool HasPointer(int id) => pointer == id && IsHeld;
        // The HUD checks blocking UI and the starting half before capturing a finger.
        public void BeginFloating(int id, Vector2 position, Camera camera)
        {
            if (!Floating || IsHeld) return;
            var parent = (RectTransform)transform.parent;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, position, camera, out var local)) return;
            ((RectTransform)transform).anchoredPosition = local;
            pointer = id;
            SelectAttackSword();
            ActivePointers.Add(id);
            if (Visibility != null) Visibility.alpha = 1;
            UpdateFloating(id, position, camera);
        }
        public void UpdateFloating(int id, Vector2 position, Camera camera)
        {
            if (!HasPointer(id)) return;
            SetDrag(position, camera);
        }
        public void OnPointerDown(PointerEventData e)
        {
            if (Floating) return;
            if (pointer != int.MinValue || MobileGameInput.Blocked) return;
            pointer = e.pointerId;
            SelectAttackSword();
            ActivePointers.Add(pointer);
            if (Action == Control.Shield) MobileGameInput.ShieldHeld = !MobileGameInput.ShieldHeld;
            if (Action == Control.Jump) MobileGameInput.JumpPressed();
            if (Action == Control.Interact) MobileGameInput.InteractPressed();
            if (Action == Control.Potion) MineArena.PlayerSystem.PotionEffects.TryDrinkSelected();
            OnDrag(e);
        }
        private void SelectAttackSword()
        {
            if (Action == Control.Aim)
                GetComponentInParent<Devotion.SDK.UI.PlayingWindow>()?.SelectSwordSlot();
        }
        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId != pointer || (Action != Control.Move && Action != Control.Aim)) return;
            SetDrag(e.position, e.pressEventCamera);
        }
        private void SetDrag(Vector2 position, Camera camera)
        {
            var rect = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, position, camera, out var point)) return;
            float radius = rect.rect.width * .25f;
            var value = Vector2.ClampMagnitude((point - rect.rect.center) / radius, 1);
            if (Thumb != null) Thumb.anchoredPosition = value * radius;
            if (value.magnitude < .18f) value = Vector2.zero;
            else value = value.normalized * Mathf.InverseLerp(.18f, 1, value.magnitude);
            if (Action == Control.Move) MobileGameInput.Move = value;
            else { MobileGameInput.Aim = value; MobileGameInput.AttackHeld = value.sqrMagnitude > .04f; }
        }
        public void OnPointerUp(PointerEventData e) { if (e.pointerId == pointer) Release(); }
        public void Release()
        {
            ActivePointers.Remove(pointer);
            pointer = int.MinValue;
            if (Floating && Visibility != null) Visibility.alpha = 0;
            if (Thumb != null) Thumb.anchoredPosition = Vector2.zero;
            if (Action == Control.Move) MobileGameInput.Move = Vector2.zero;
            if (Action == Control.Aim) { MobileGameInput.Aim = Vector2.zero; MobileGameInput.AttackHeld = false; }
        }
        private void OnDisable() => Release();
    }
}
