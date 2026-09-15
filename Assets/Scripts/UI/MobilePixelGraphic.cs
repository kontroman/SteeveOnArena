using UnityEngine;
using UnityEngine.UI;
namespace MineArena.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MobilePixelGraphic : MaskableGraphic
    {
        public bool Thumb, Icon, Stick, Stop;
        public MobileTouchControl.Control Action;
        private bool active;
        protected override void OnEnable()
        {
            if (GetComponent<CanvasRenderer>() == null) gameObject.AddComponent<CanvasRenderer>();
            base.OnEnable();
        }
        private void LateUpdate()
        {
            var control = GetComponentInParent<MobileTouchControl>();
            bool pressed = control != null && (control.IsHeld || (control.Action == MobileTouchControl.Control.Shield && MobileGameInput.ShieldHeld));
            if (active == pressed) return;
            active = pressed; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (Icon) { DrawIcon(vh, r); return; }
            if (Stick)
            {
                // A fine stepped ring leaves the terrain visible beneath the thumb.
                const int grid = 48;
                for (int y = 0; y < grid; y++)
                for (int x = 0; x < grid; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + .5f, y + .5f), Vector2.one * 24);
                    if (distance > 24) continue;
                    Color32 tint = distance > 23 ? new Color32(75, 66, 49, 130) : distance > 22 ? new Color32(242, 226, 189, 175) : new Color32(47, 54, 49, 28);
                    if (distance > 19 && distance < 21 && (x == 23 || x == 24 || y == 23 || y == 24)) tint = new Color32(242, 226, 189, 145);
                    if (active && distance > 22) tint = new Color32(111, 174, 165, 220);
                    Quad(vh, new Rect(r.x + x * r.width / grid, r.y + y * r.height / grid, r.width / grid, r.height / grid), tint);
                }
                return;
            }
            // Match the parchment/wood HUD with a two-pixel bevel, not a stone slab.
            StepRect(vh, new Rect(r.x + 2, r.y - 2, r.width, r.height), new Color32(38, 32, 23, 65));
            StepRect(vh, r, active ? new Color32(60, 122, 117, 245) : new Color32(108, 88, 59, 215));
            StepRect(vh, Inset(r, 2), active ? new Color32(188, 220, 206, 245) : new Color32(249, 236, 205, Thumb ? (byte)235 : (byte)220));
            StepRect(vh, Inset(r, 4), active ? new Color32(137, 187, 173, 235) : new Color32(230, 211, 173, Thumb ? (byte)235 : (byte)205));
            Quad(vh, new Rect(r.x + 6, r.yMax - 5, r.width - 12, 1), new Color32(255, 247, 224, 190));
        }
        private static Rect Inset(Rect r, float amount) => new Rect(r.x + amount, r.y + amount, r.width - amount * 2, r.height - amount * 2);
        private void StepRect(VertexHelper vh, Rect r, Color32 tint)
        {
            Quad(vh, new Rect(r.x + 2, r.y, r.width - 4, r.height), tint);
            Quad(vh, new Rect(r.x, r.y + 2, 2, r.height - 4), tint);
            Quad(vh, new Rect(r.xMax - 2, r.y + 2, 2, r.height - 4), tint);
        }
        private bool Pixel(int x, int y)
        {
            if (x < 0 || x > 15 || y < 0 || y > 15) return false;
            if (Stop) return x >= 4 && x <= 11 && y >= 4 && y <= 11;
            switch (Action)
            {
                case MobileTouchControl.Control.Move:
                    return ((x == 7 || x == 8) && y >= 2 && y <= 13) || ((y == 7 || y == 8) && x >= 2 && x <= 13) ||
                        (y >= 2 && y <= 4 && Mathf.Abs(x - 7.5f) <= y - 1) || (y >= 11 && y <= 13 && Mathf.Abs(x - 7.5f) <= 14 - y) ||
                        (x >= 2 && x <= 4 && Mathf.Abs(y - 7.5f) <= x - 1) || (x >= 11 && x <= 13 && Mathf.Abs(y - 7.5f) <= 14 - x);
                case MobileTouchControl.Control.Aim:
                    return (y >= 1 && y <= 10 && x + y >= 13 && x + y <= 16) || (x >= 3 && x <= 8 && y - x == 5) || (y >= 11 && x + y >= 14 && x + y <= 15);
                case MobileTouchControl.Control.Shield:
                    return y >= 2 && y <= 13 && x >= 3 + Mathf.Max(0, y - 9) && x <= 12 - Mathf.Max(0, y - 9);
                case MobileTouchControl.Control.Jump:
                    return (y >= 3 && y <= 7 && Mathf.Abs(x - 7.5f) <= y - 2) || (x >= 6 && x <= 9 && y >= 7 && y <= 11) || (x >= 3 && x <= 12 && y == 14);
                case MobileTouchControl.Control.Interact:
                    return (y >= 2 && y <= 4 && x >= 3 && x <= 12) || (x >= 11 && x <= 13 && y >= 4 && y <= 7) || (y >= 5 && x + y >= 12 && x + y <= 13);
                default:
                    return (x >= 6 && x <= 9 && y >= 1 && y <= 5) || (y >= 6 && y <= 13 && x >= 3 + Mathf.Abs(y - 10) / 3 && x <= 12 - Mathf.Abs(y - 10) / 3);
            }
        }
        private void DrawIcon(VertexHelper vh, Rect r)
        {
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                bool fill = Pixel(x, y);
                if (!fill && !Pixel(x - 1, y) && !Pixel(x + 1, y) && !Pixel(x, y - 1) && !Pixel(x, y + 1)) continue;
                Color32 tint = new Color32(76, 65, 47, 255);
                if (fill)
                {
                    tint = x < 8 ? new Color32(250, 248, 223, 255) : new Color32(133, 164, 157, 255);
                    if (Action == MobileTouchControl.Control.Shield) tint = x == 7 || x == 8 ? new Color32(230, 212, 159, 255) : new Color32(106, 151, 147, 255);
                    if (Action == MobileTouchControl.Control.Potion) tint = y < 5 ? new Color32(169, 125, 67, 255) : x < 7 ? new Color32(231, 153, 174, 255) : new Color32(171, 84, 125, 255);
                    if (Stop) tint = new Color32(184, 100, 72, 255);
                }
                Quad(vh, new Rect(r.x + x * r.width / 16, r.y + (15 - y) * r.height / 16, r.width / 16, r.height / 16), tint);
            }
        }
        private void Quad(VertexHelper vh, Rect r, Color tint)
        {
            int i = vh.currentVertCount; tint *= color;
            vh.AddVert(new Vector2(r.xMin, r.yMin), tint, Vector2.zero);
            vh.AddVert(new Vector2(r.xMax, r.yMin), tint, Vector2.zero);
            vh.AddVert(new Vector2(r.xMax, r.yMax), tint, Vector2.zero);
            vh.AddVert(new Vector2(r.xMin, r.yMax), tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
