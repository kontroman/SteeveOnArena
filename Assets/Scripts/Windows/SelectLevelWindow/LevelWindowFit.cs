using UnityEngine;

namespace MineArena.Windows.SelectLevel
{
    [ExecuteAlways]
    public sealed class LevelWindowFit : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        private readonly Vector3[] _corners = new Vector3[4];
        private Devotion.SDK.UI.PlayingWindow _hud;

        private void LateUpdate()
        {
            if (panel == null || !(transform is RectTransform root) || root.rect.width <= 0 || root.rect.height <= 0) return;
            float bottomInset = 16f;
            if (GetComponent<MineArena.UI.InventoryWindow>() != null)
            {
                if (_hud == null) _hud = FindObjectOfType<Devotion.SDK.UI.PlayingWindow>();
                bottomInset = 160f;
                if (_hud != null && _hud.QuickAccessPanel != null && _hud.QuickAccessPanel.gameObject.activeInHierarchy)
                {
                    _hud.QuickAccessPanel.GetWorldCorners(_corners);
                    bottomInset = 16f;
                    foreach (var corner in _corners)
                        bottomInset = Mathf.Max(bottomInset, root.InverseTransformPoint(corner).y - root.rect.yMin + 24f);
                }
                panel.anchoredPosition = new Vector2(0, (bottomInset - 16f) * 0.5f);
                // Reserve space only for the content; the backdrop covers the entire screen.
            }
            float scale = Mathf.Min(1f, (root.rect.width - 32f) / panel.sizeDelta.x,
                (root.rect.height - bottomInset - 16f) / panel.sizeDelta.y);
            panel.localScale = Vector3.one * Mathf.Max(0.05f, scale);
        }
    }
}
