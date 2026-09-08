using MineArena.Items;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.UI
{
    public class ResourceIcon : MonoBehaviour
    {
        [SerializeField] private List<Image> resourceImages;
        private Vector3[] _positions, _scales;
        private Bounds _bounds;
        private bool _fitting;

        private void OnEnable() => Fit();
        private void OnRectTransformDimensionsChange() => Fit();
        private void Fit()
        {
            if (_fitting || resourceImages == null || resourceImages.Count == 0) return;
            _fitting = true;
            try
            {
                var root = (RectTransform)transform;
                if (_positions == null)
                {
                    _positions = new Vector3[resourceImages.Count];
                    _scales = new Vector3[resourceImages.Count];
                    bool first = true;
                    var corners = new Vector3[4];
                    for (int i = 0; i < resourceImages.Count; i++)
                    {
                        if (resourceImages[i] == null) continue;
                        var rect = resourceImages[i].rectTransform;
                        _positions[i] = rect.localPosition; _scales[i] = rect.localScale;
                        rect.GetWorldCorners(corners);
                        foreach (var corner in corners)
                        {
                            var point = root.InverseTransformPoint(corner);
                            if (first) { _bounds = new Bounds(point, Vector3.zero); first = false; }
                            else _bounds.Encapsulate(point);
                        }
                    }
                }
                float scale = Mathf.Min(root.rect.width / Mathf.Max(1, _bounds.size.x), root.rect.height / Mathf.Max(1, _bounds.size.y)) * 0.88f;
                for (int i = 0; i < resourceImages.Count; i++)
                {
                    if (resourceImages[i] == null) continue;
                    var rect = resourceImages[i].rectTransform;
                    rect.localScale = _scales[i] * scale;
                    rect.localPosition = new Vector3(root.rect.center.x, root.rect.center.y, 0) + (_positions[i] - _bounds.center) * scale;
                }
            }
            finally { _fitting = false; }
        }

        public void SetResource(StackableItemConfig resource)
        {
            Fit();
            if (resourceImages == null) return;
            foreach (var face in resourceImages)
            {
                if (face == null) continue;
                var sprite = resource == null ? null : face.gameObject.name == "Top" ? resource.TopIcon : resource.SideIcon;
                face.sprite = sprite;
                face.enabled = sprite != null;
                face.raycastTarget = false;
            }
        }

        public void SetSprite(Sprite sprite)
        {
            if (resourceImages == null)
                return;

            foreach (var item in resourceImages)
            {
                if (item == null)
                    continue;

                item.sprite = sprite;
                item.enabled = sprite != null;
                item.raycastTarget = false;
            }
        }
    }
}
