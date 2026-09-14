using MineArena.PlayerSystem;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    // Pixel geometry uses the same crisp, code-authored icon style as the HUD portrait.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PlayerAttributeIcon : MaskableGraphic
    {
        public PlayerAttribute attribute;
        private static readonly string[][] Shapes = {
            new[] { "................", "..####....####..", ".#++++#..#++++#.", ".#+++++##+++++#.", ".#++**+++++++#..", ".#++**+++++++#..", "..#++++++++#...", "...#++++++#....", "....#++++#.....", ".....#++#......", "......##.......", "................" },
            new[] { "......####......", "......#++#......", "......#++#......", "......#++#......", "......#**#......", "......#**###....", "......#+++++#...", "..##..#++++++#..", ".#++#.#++++++#..", "..##..########..", "......#******#..", "......########.." },
            new[] { "..........####..", ".........#**+#..", "........#**+#...", ".......#**+#....", "......#**+#.....", ".....#**+#......", "..#.#**+#.......", ".#+#**+#........", "..#+++#.........", "..#++#++#.......", ".#++#.##........", "..##............" },
            new[] { "...###..###.....", "..#+++#++++#....", "..#+**#**++#....", "...#++#+++#.....", "....#####.......", "..##++#++##.....", ".#++**#**++#....", ".#++++#++++#....", "..####+####.....", "......#+#.......", ".......#+#......", "........##......" },
            new[] { "..############..", "..#++++**++++#..", "..#++++**++++#..", "..#++++**++++#..", "..#**********#..", "..#**********#..", "...#+++**+++#...", "...#+++**+++#...", "....#++**++#....", ".....#+**+#.....", "......#++#......", ".......##......." }
        };
        private static readonly Color32[] Tints = { new(206, 82, 73, 255), new(74, 163, 189, 255), new(222, 166, 69, 255), new(98, 161, 80, 255), new(145, 119, 182, 255) };
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int index = Mathf.Clamp((int)attribute, 0, 4);
            var pixels = Shapes[index];
            var r = rectTransform.rect;
            float unit = Mathf.Min(r.width / 16f, r.height / 14f);
            for (int y = 0; y < pixels.Length; y++)
            for (int x = 0; x < pixels[y].Length; x++)
            {
                char pixel = pixels[y][x];
                if (pixel == '.') continue;
                Color tint = pixel == '#' ? new Color32(75, 62, 46, 255) : pixel == '*' ? Color.Lerp(Tints[index], Color.white, .5f) : Tints[index];
                tint *= color;
                var p = r.center + new Vector2(x - 8, 5 - y) * unit;
                int start = vh.currentVertCount;
                vh.AddVert(p, tint, Vector2.zero); vh.AddVert(p + Vector2.right * unit, tint, Vector2.zero);
                vh.AddVert(p + Vector2.one * unit, tint, Vector2.zero); vh.AddVert(p + Vector2.up * unit, tint, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
