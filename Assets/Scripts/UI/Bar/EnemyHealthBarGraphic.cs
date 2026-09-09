using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Game.UI
{
    // Flat quads keep the small, square-edged HUD crisp without extra textures or materials.
    public sealed class EnemyHealthBarGraphic : MaskableGraphic
    {
        private float _health = 1;
        private float _trail = 1;

        public void SetHealth(float health, float trail)
        {
            if (Mathf.Approximately(_health, health) && Mathf.Approximately(_trail, trail)) return;
            _health = health;
            _trail = trail;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect r = GetPixelAdjustedRect();
            float x = r.xMin, y = r.yMin, w = r.width, h = r.height;
            Quad(mesh, x + 2, y - 2, w, h, new Color32(26, 21, 17, 130));
            Quad(mesh, x, y, w, h, new Color32(73, 55, 40, 255));
            Quad(mesh, x + 1, y + 1, w - 2, h - 2, new Color32(193, 165, 113, 255));
            Quad(mesh, x + 1, y + h - 2, w - 2, 1, new Color32(255, 240, 204, 255));
            Quad(mesh, x + 3, y + 3, w - 6, h - 6, new Color32(61, 43, 36, 255));
            float inner = w - 6;
            Quad(mesh, x + 3, y + 3, inner * _trail, h - 6, new Color32(228, 175, 82, 255));
            Color32 fill = _health <= 0.25f ? new Color32(191, 66, 51, 255) : new Color32(172, 98, 73, 255);
            Quad(mesh, x + 3, y + 3, inner * _health, h - 6, fill);
            Quad(mesh, x + 3, y + h - 5, inner * _health, 2, new Color32(230, 153, 110, 255));
            for (int i = 1; i < 5; i++)
                Quad(mesh, x + 3 + inner * i / 5f, y + 3, 1, h - 6, new Color32(61, 43, 36, 100));
        }

        private static void Quad(VertexHelper mesh, float x, float y, float w, float h, Color32 tint)
        {
            if (w <= 0 || h <= 0) return;
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector3(x, y), tint, Vector2.zero);
            mesh.AddVert(new Vector3(x, y + h), tint, Vector2.zero);
            mesh.AddVert(new Vector3(x + w, y + h), tint, Vector2.zero);
            mesh.AddVert(new Vector3(x + w, y), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
