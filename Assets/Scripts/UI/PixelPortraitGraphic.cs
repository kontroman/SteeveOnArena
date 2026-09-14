using UnityEngine;
using UnityEngine.UI;

namespace Devotion.SDK.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PixelPortraitGraphic : MaskableGraphic
    {
        private static readonly string[] Pixels = {
            "....########....",
            "..############..",
            ".##HHHHHHHHHH##.",
            ".#HHHHHHHHHHHH#.",
            ".#HHHSSSSSHHHH#.",
            ".#HHSSSSSSSSHH#.",
            ".#HSSSSSSSSSSH#.",
            ".#SWWEESSWWEES#.",
            ".#SWWEESSWWEES#.",
            ".#SSSSSNNSSSSS#.",
            "..#SSSSNNSSSS#..",
            "..#SSMMMMMMSS#..",
            "...#SSSLLSSS#...",
            "....#SSSSSS#....",
            "..###TTTTTT###..",
            ".##TTTTTTTTTT##."
        };

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float unit = Mathf.Min(r.width, r.height) / 16f;
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < Pixels[y].Length; x++)
            {
                char pixel = Pixels[y][x];
                if (pixel == '.') continue;
                Color32 tint = pixel switch {
                    '#' => new Color32(39, 35, 42, 255),
                    'H' => new Color32(91, 56, 38, 255),
                    'S' => new Color32(226, 167, 116, 255),
                    'W' => new Color32(255, 245, 218, 255),
                    'E' => new Color32(43, 91, 105, 255),
                    'N' => new Color32(190, 127, 83, 255),
                    'M' => new Color32(106, 63, 47, 255),
                    'L' => new Color32(246, 195, 145, 255),
                    _ => new Color32(56, 141, 149, 255)
                };
                var p = r.center + new Vector2(x - 8, 7 - y) * unit;
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
