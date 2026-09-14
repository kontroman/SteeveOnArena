using UnityEngine;

namespace MineArena.UI
{
    public static class InventoryDragLayer
    {
        public static void Raise(RectTransform visual)
        {
            var root = visual.GetComponentInParent<Canvas>().rootCanvas;
            var canvas = visual.GetComponent<Canvas>();
            if (canvas == null) canvas = visual.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingLayerID = root.sortingLayerID;
            // Inventory quick slots use root + 1. Drag visuals must also pass over them.
            canvas.sortingOrder = root.sortingOrder + 2;
            // No GraphicRaycaster: the visual must never intercept the destination slot.
        }
    }
}
