using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Managers
{
    // A true circular hole: raycasts pass through to the unchanged HUD button.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialSpotlightGraphic : MaskableGraphic, ICanvasRaycastFilter
    {
        private Vector2 _center;
        private float _radius;
        private bool _rectangle;
        private Rect _hole;
        public void Focus(Vector2 center, float radius) { _rectangle = false; _center = center; _radius = radius; SetVerticesDirty(); }
        public void FocusRect(Rect hole) { _rectangle = true; _hole = hole; SetVerticesDirty(); }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var point);
            return _rectangle ? !_hole.Contains(point) : (point - _center).sqrMagnitude > _radius * _radius;
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_rectangle)
            {
                var r = rectTransform.rect;
                var tint = new Color(.025f, .04f, .09f, .65f);
                Fill(vh, Rect.MinMaxRect(r.xMin, r.yMin, _hole.xMin, r.yMax), tint);
                Fill(vh, Rect.MinMaxRect(_hole.xMax, r.yMin, r.xMax, r.yMax), tint);
                Fill(vh, Rect.MinMaxRect(_hole.xMin, r.yMin, _hole.xMax, _hole.yMin), tint);
                Fill(vh, Rect.MinMaxRect(_hole.xMin, _hole.yMax, _hole.xMax, r.yMax), tint);
                return;
            }
            if (_radius <= 0) return;
            var bounds = rectTransform.rect;
            const int count = 128;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2 / count, b = (i + 1) * Mathf.PI * 2 / count;
                Vector2 d1 = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), d2 = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                Vector2 inner1 = _center + d1 * _radius, inner2 = _center + d2 * _radius;
                Quad(vh, inner1, inner2, Edge(bounds, d2), Edge(bounds, d1), new Color(.025f, .04f, .09f, .65f));
                float width = 4 + Mathf.Sin(Time.unscaledTime * 4) * 1.2f;
                Quad(vh, inner1, inner2, _center + d2 * (_radius + width), _center + d1 * (_radius + width), new Color(1, .84f, .25f, 1));
            }
        }
        private static void Fill(VertexHelper vh, Rect r, Color tint) => Quad(vh, r.min, new Vector2(r.xMax, r.yMin), r.max, new Vector2(r.xMin, r.yMax), tint);
        private Vector2 Edge(Rect rect, Vector2 direction)
        {
            // Extend beyond all screen corners; the viewport clips the outer edge.
            return _center + direction * Mathf.Max(_radius, rect.size.magnitude + Vector2.Distance(_center, rect.center));
        }
        internal static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int start = vh.currentVertCount;
            vh.AddVert(a, tint, Vector2.zero); vh.AddVert(b, tint, Vector2.zero);
            vh.AddVert(c, tint, Vector2.zero); vh.AddVert(d, tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
    }

}
