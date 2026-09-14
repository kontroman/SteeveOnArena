using System;
using System.IO;
using System.Reflection;
using Devotion.SDK.UI;
using MineArena.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Editor
{
    public static class InventoryBackdropValidation
    {
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/inventory-backdrop.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Validate(); }
            catch (Exception e) { File.WriteAllText("Temp/inventory-backdrop-result.txt", "FAIL: " + e); Debug.LogException(e); }
        };

        [MenuItem("MineArena/Validation/Inventory Backdrop")]
        public static void Validate()
        {
            var canvasObject = new GameObject("Inventory validation", typeof(RectTransform), typeof(Canvas));
            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var hud = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<PlayingWindow>(
                    "Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab"), canvas.transform);
                var inventory = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<InventoryWindow>(
                    "Assets/DevotionSDK/Prefabs/UI/InventoryWindow.prefab"), canvas.transform);
                void Invoke(string method) => typeof(InventoryWindow).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(inventory, null);
                Invoke("Awake");
                inventory.gameObject.AddComponent<InventoryDisableProbe>();
                var panel = hud.QuickAccessPanel;
                if (panel == null) throw new Exception("Missing quick access panel.");
                var parent = panel.parent;
                int sibling = panel.GetSiblingIndex();
                var before = new Vector3[4];
                var after = new Vector3[4];
                foreach (var resolution in new[] { new Vector2(1920, 1080), new Vector2(2560, 1080), new Vector2(1280, 720), new Vector2(1080, 1920) })
                {
                    ((RectTransform)canvas.transform).sizeDelta = resolution;
                    Canvas.ForceUpdateCanvases();
                    panel.GetWorldCorners(before);
                    inventory.gameObject.SetActive(true);
                    Invoke("RaiseQuickAccessPanel");
                    var fit = inventory.GetComponent<MineArena.Windows.SelectLevel.LevelWindowFit>();
                    // Run the real per-frame layout that used to reinstate the bottom inset.
                    typeof(MineArena.Windows.SelectLevel.LevelWindowFit).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(fit, null);
                    Canvas.ForceUpdateCanvases();
                    panel.GetWorldCorners(after);
                    for (int i = 0; i < 4; i++)
                        if (Vector3.Distance(before[i], after[i]) > 0.01f) throw new Exception("Quick slots moved at " + resolution);
                    var shade = (RectTransform)inventory.transform.Find("InventoryBackdrop");
                    if (shade.rect.size != ((RectTransform)canvas.transform).rect.size) throw new Exception("Incomplete backdrop coverage.");
                    var slotCanvas = panel.GetComponent<Canvas>();
                    if (panel.parent != parent || panel.GetSiblingIndex() != sibling) throw new Exception("Opening inventory changed HUD hierarchy.");
                    if (!slotCanvas.overrideSorting || slotCanvas.sortingOrder <= canvas.sortingOrder) throw new Exception("Quick slots below backdrop.");
                    if (!panel.GetComponent<GraphicRaycaster>().isActiveAndEnabled) throw new Exception("Quick slots cannot receive drops.");
                    if (shade.GetComponent<Image>().raycastTarget) throw new Exception("Backdrop intercepts drag/drop.");
                    foreach (var handler in new[] { typeof(InventoryCellDragHandler), typeof(PlayingInventorySlotUI), typeof(ArmorEquipmentSlotUI) })
                    {
                        var drag = new GameObject("Drag validation", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                        drag.transform.SetParent(canvas.transform, false);
                        try
                        {
                            handler.GetMethod("PrepareDraggedVisual", BindingFlags.Static | BindingFlags.NonPublic)
                                .Invoke(null, new object[] { panel, (RectTransform)drag.transform });
                            var dragCanvas = drag.GetComponent<Canvas>();
                            if (!dragCanvas.overrideSorting || dragCanvas.sortingLayerID != slotCanvas.sortingLayerID || dragCanvas.sortingOrder <= slotCanvas.sortingOrder)
                                throw new Exception(handler.Name + " drag is below the quick slots.");
                            if (drag.GetComponent<Image>().raycastTarget || drag.GetComponent<CanvasGroup>().blocksRaycasts || drag.GetComponent<GraphicRaycaster>() != null)
                                throw new Exception(handler.Name + " drag intercepts drops.");
                        }
                        finally { UnityEngine.Object.DestroyImmediate(drag); }
                    }
                    inventory.gameObject.SetActive(false);
                    if (slotCanvas.overrideSorting || !panel.gameObject.activeInHierarchy) throw new Exception("Closing inventory did not restore visible HUD slots.");
                    if (panel.parent != parent || panel.GetSiblingIndex() != sibling) throw new Exception("HUD hierarchy not restored.");
                }
                // Reproduce an instance stranded by the old implementation, then run HUD recovery.
                panel.SetParent(inventory.transform, false);
                if (panel.gameObject.activeInHierarchy) throw new Exception("Invalid stranded-panel fixture.");
                typeof(PlayingWindow).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
                if (panel.parent != hud.transform || !panel.gameObject.activeInHierarchy) throw new Exception("Stranded quick slots were not recovered.");
                inventory.gameObject.SetActive(true);
                var oldShade = (RectTransform)inventory.transform.Find("InventoryBackdrop");
                oldShade.offsetMin = new Vector2(0, 160);
                Invoke("EnsureBackdrop");
                if (oldShade.offsetMin != Vector2.zero) throw new Exception("Existing backdrop inset not repaired.");
                File.WriteAllText("Temp/inventory-backdrop-result.txt", "PASS: full-screen shade after actual LevelWindowFit update at four aspect ratios; quick-slot canvas and raycaster; inventory, quick-slot and armor drag visuals render above the panel without intercepting drops; no hierarchy changes during parent deactivation; HUD remains visible after closing; stranded-panel recovery; existing 160px inset repaired.");
            }
            finally { UnityEngine.Object.DestroyImmediate(canvasObject); }
        }
    }

    // Exercise the real close handler inside Unity's SetActive deactivation traversal in Edit Mode.
    [ExecuteAlways]
    public sealed class InventoryDisableProbe : MonoBehaviour
    {
        private void OnDisable()
        {
            var inventory = GetComponent<InventoryWindow>();
            if (inventory != null)
                typeof(InventoryWindow).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(inventory, null);
        }
    }
}
