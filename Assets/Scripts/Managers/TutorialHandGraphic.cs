using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Managers
{
    // Blocky skin-colored pointing hand with a teal sleeve, drawn on a pixel grid.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialHandGraphic : MaskableGraphic
    {
        private static readonly string[] Pixels = {
            "..####........", "..#hh#........", "..#hs#........", "..#hs#........",
            "..#hs#####....", "..#hhhhhhs#...", ".##hhhhhhs##..", "#hhhhhhhsss#..",
            "#hhhhhhhsss#..", ".#hhhhhsss#...", "..#hhhhss#....", "..########....",
            "..#tttttd#....", "..#tttttd#....", "..#tttttd#....", "..########...."
        };
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var rect = rectTransform.rect;
            float unit = Mathf.Min(rect.width / 14, rect.height / 16);
            for (int y = 0; y < Pixels.Length; y++)
            for (int x = 0; x < Pixels[y].Length; x++)
            {
                char pixel = Pixels[y][x]; if (pixel == '.') continue;
                Color tint = pixel == '#' ? new Color32(57, 36, 29, 255) : pixel == 'h' ? new Color32(222, 171, 125, 255) :
                    pixel == 's' ? new Color32(170, 113, 77, 255) : pixel == 't' ? new Color32(39, 186, 181, 255) : new Color32(23, 112, 122, 255);
                Vector2 p = rect.center + new Vector2(x - 7, 7 - y) * unit;
                TutorialSpotlightGraphic.Quad(vh, p, p + Vector2.right * unit, p + Vector2.one * unit, p + Vector2.up * unit, tint);
            }
        }
    }
}
