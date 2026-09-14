using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HudNoticeGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            float unit = Mathf.Min(rect.width, rect.height) / 16f;
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                if ((x < 2 || x > 13) && (y < 2 || y > 13)) continue;
                bool edge = x == 0 || x == 15 || y == 0 || y == 15 ||
                    ((x < 2 || x > 13) && (y == 2 || y == 13)) || ((y < 2 || y > 13) && (x == 2 || x == 13));
                bool mark = x >= 7 && x <= 9 && (y >= 4 && y <= 8 || y >= 11 && y <= 12);
                Color tint = edge ? new Color32(89, 52, 34, 255) : mark ? new Color32(255, 248, 211, 255) :
                    x <= 2 || y <= 2 ? new Color32(255, 203, 100, 255) : new Color32(214, 102, 48, 255);
                var p = rect.center + new Vector2(x - 8, 7 - y) * unit;
                int i = vh.currentVertCount;
                vh.AddVert(p, tint * color, Vector2.zero); vh.AddVert(p + Vector2.right * unit, tint * color, Vector2.zero);
                vh.AddVert(p + Vector2.one * unit, tint * color, Vector2.zero); vh.AddVert(p + Vector2.up * unit, tint * color, Vector2.zero);
                vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
            }
        }
    }
}
