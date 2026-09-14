using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MineArena.UI
{
    // Geometry keeps the padlock sharp at every UI scale and needs no texture or font glyph.
    public sealed class StorageLockedSlotGraphic : MaskableGraphic, IPointerClickHandler
    {
        public static void SetLocked(InventoryCellUI slot, bool locked)
        {
            var overlay = slot.GetComponentInChildren<StorageLockedSlotGraphic>(true);
            if (overlay == null && locked)
            {
                var go = new GameObject("LockedSlot", typeof(RectTransform), typeof(CanvasRenderer), typeof(StorageLockedSlotGraphic));
                go.layer = slot.gameObject.layer;
                go.transform.SetParent(slot.transform, false);
                overlay = go.GetComponent<StorageLockedSlotGraphic>();
                var rect = overlay.rectTransform;
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(2, 2); rect.offsetMax = new Vector2(-2, -2);
                overlay.raycastTarget = true;
            }
            if (overlay == null) return;
            overlay.gameObject.SetActive(locked);
            if (locked) overlay.transform.SetAsLastSibling();
        }

        public void OnPointerClick(PointerEventData eventData) =>
            GetComponentInParent<MineArena.Windows.StorageWindow>()?.ShowLockedHint();

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            // A warm recessed tint lets the existing cream tile and bevel show through.
            Quad(vh, r, new Color(.38f, .30f, .19f, .18f));
            float unit = Mathf.Min(r.width, r.height) / 32f;
            Vector2 center = r.center;
            void Block(float x, float y, float width, float height, Color tint) =>
                Quad(vh, new Rect(center.x + x * unit, center.y + y * unit, width * unit, height * unit), tint);
            var outline = new Color(.51f, .42f, .29f);
            var face = new Color(.88f, .81f, .65f);
            var highlight = new Color(1f, .95f, .81f);
            // Small stepped shackle, inset body and a restrained keyhole.
            Block(-3, 1, 1, 4, outline);
            Block(2, 1, 1, 4, outline);
            Block(-2, 5, 4, 1, outline);
            Block(-2, 2, .6f, 3, highlight);
            Block(-4, -5, 8, 7, new Color(.44f, .35f, .23f, .18f));
            Block(-4, -4, 8, 6, outline);
            Block(-3, -3, 6, 4, face);
            Block(-3, 0, 6, 1, highlight);
            Block(-3, -3, .6f, 3, highlight);
            Block(-.7f, -1.4f, 1.4f, 1.4f, outline);
            Block(-.35f, -2.3f, .7f, 1.2f, outline);
        }

        private static void Quad(VertexHelper vh, Rect rect, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(rect.xMin, rect.yMin), tint, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMin, rect.yMax), tint, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMax), tint, Vector2.zero);
            vh.AddVert(new Vector3(rect.xMax, rect.yMin), tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
