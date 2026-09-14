using Devotion.SDK.Controllers;
using MineArena.Items;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    // Pixel-stepped fill, black backing, green-to-red colour like the item durability HUD.
    public sealed class ItemDurabilityBar : MaskableGraphic
    {
        private ArmorConfig _item;
        private int _current = -1;
        private int? _storedDurability;
        public static void BindStored(GameObject slot, ArmorConfig item, int durability)
        {
            Bind(slot, item);
            var bar = slot.GetComponentInChildren<ItemDurabilityBar>(true);
            if (bar == null) return;
            bar._storedDurability = durability < 0 ? item?.MaxDurability ?? 0 : durability;
            bar.Refresh();
        }
        public static void Bind(GameObject slot, string itemId)
        {
            Bind(slot, GameRoot.GameConfig?.ItemDatabase?.GetItemConfig(itemId) as ArmorConfig);
        }
        public static void Bind(GameObject slot, ArmorConfig item)
        {
            var bar = slot.GetComponentInChildren<ItemDurabilityBar>(true);
            if (bar == null && item != null && item.MaxDurability > 0)
            {
                var go = new GameObject("Durability", typeof(RectTransform), typeof(CanvasRenderer), typeof(ItemDurabilityBar));
                go.layer = slot.layer;
                go.transform.SetParent(slot.transform, false);
                bar = go.GetComponent<ItemDurabilityBar>();
                bar.raycastTarget = false;
                var rect = bar.rectTransform;
                rect.anchorMin = new Vector2(.16f, 0);
                rect.anchorMax = new Vector2(.84f, 0);
                rect.pivot = new Vector2(.5f, 0);
                rect.offsetMin = new Vector2(0, 8);
                rect.offsetMax = new Vector2(0, 14);
            }
            if (bar == null) return;
            bar._item = item;
            bar._storedDurability = null;
            bar._current = -1;
            bar.gameObject.SetActive(item != null && item.MaxDurability > 0);
            bar.transform.SetAsLastSibling();
            bar.Refresh();
        }
        private void Update() => Refresh();
        private void Refresh()
        {
            int value = _storedDurability ?? (_item != null ? GameRoot.PlayerProgress?.InventoryProgress?.GetItemDurability(_item.Name, _item.MaxDurability) ?? 0 : 0);
            if (_current == value) return;
            _current = value;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_item == null || _current <= 0) return;
            var rect = GetPixelAdjustedRect();
            float fraction = Mathf.Clamp01((float)_current / _item.MaxDurability);
            float fill = Mathf.Max(1, Mathf.RoundToInt(fraction * 13)) / 13f;
            AddQuad(vh, rect, Color.black);
            AddQuad(vh, new Rect(rect.xMin, rect.yMin + 2, rect.width * fill, rect.height - 2),
                Color.HSVToRGB(fraction / 3f, 1, 1));
        }
        private static void AddQuad(VertexHelper vh, Rect r, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(new Vector3(r.xMin, r.yMin), tint, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMax), tint, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMax), tint, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMin), tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
