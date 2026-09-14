using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Managers
{
    // Mesh arrow stays sharp at any resolution and does not depend on font glyph coverage.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialArrowGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            // Authored on a pixel grid: dark outline, cream highlight and amber bevel.
            string[] pixels = {
                ".....#####.....",
                ".....#hhh#.....",
                ".....#hyg#.....",
                ".....#hyg#.....",
                ".....#hyg#.....",
                ".....#hyg#.....",
                "######hyg######",
                "#hhhhhhyyyyggg#",
                ".#hyyyyyyyygg#.",
                "..#hyyyyyygg#..",
                "...#hyyyygg#...",
                "....#hyygg#....",
                ".....#hgg#.....",
                "......#g#......",
                ".......#......."
            };
            float unit = Mathf.Min(r.width, r.height) / 15f;
            for (int y = 0; y < pixels.Length; y++)
            for (int x = 0; x < pixels[y].Length; x++)
            {
                char pixel = pixels[y][x];
                if (pixel == '.') continue;
                Color32 tint = pixel == '#' ? new Color32(48, 32, 28, 255) :
                    pixel == 'h' ? new Color32(255, 249, 194, 255) :
                    pixel == 'g' ? new Color32(215, 133, 30, 255) : new Color32(255, 211, 64, 255);
                var p = r.center + new Vector2(x - 7.5f, 6.5f - y) * unit;
                int start = vh.currentVertCount;
                vh.AddVert(p, tint, Vector2.zero);
                vh.AddVert(p + Vector2.right * unit, tint, Vector2.zero);
                vh.AddVert(p + Vector2.one * unit, tint, Vector2.zero);
                vh.AddVert(p + Vector2.up * unit, tint, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
