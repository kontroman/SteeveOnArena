using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    // Pixel-aligned geometry follows the HUD's block-art style at every UI scale.
    public sealed class ArenaExitGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = GetPixelAdjustedRect();
            float unit = Mathf.Min(rect.width, rect.height) / 24f;
            var origin = rect.center - Vector2.one * (12 * unit);
            void Box(float x, float y, float w, float h, Color32 tint)
            {
                int start = mesh.currentVertCount;
                mesh.AddVert(origin + new Vector2(x, y) * unit, tint, Vector2.zero);
                mesh.AddVert(origin + new Vector2(x, y + h) * unit, tint, Vector2.zero);
                mesh.AddVert(origin + new Vector2(x + w, y + h) * unit, tint, Vector2.zero);
                mesh.AddVert(origin + new Vector2(x + w, y) * unit, tint, Vector2.zero);
                mesh.AddTriangle(start, start + 1, start + 2);
                mesh.AddTriangle(start, start + 2, start + 3);
            }
            Color32 ink = new Color32(98, 69, 51, 255);
            Box(3, 2, 12, 20, ink);
            Box(5, 4, 8, 16, new Color32(224, 173, 94, 255));
            Box(7, 4, 6, 14, new Color32(72, 89, 91, 255));
            Box(1, 1, 16, 2, ink);
            Box(10, 9, 10, 6, ink);
            Box(17, 6, 3, 12, ink);
            Box(20, 9, 3, 6, ink);
            Box(11, 11, 10, 2, new Color32(255, 241, 211, 255));
            Box(18, 8, 2, 8, new Color32(255, 241, 211, 255));
        }
    }
}
