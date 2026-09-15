using System;
using System.IO;
using System.Reflection;
using MineArena.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MineArena.Editor
{
    public static class MobileControlsValidation
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/mobile-controls.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Validate(); }
            catch (Exception e) { File.WriteAllText("Documentation/mobile-controls-validation.txt", e.ToString()); }
        };

        [MenuItem("MineArena/Validation/Mobile controls")]
        public static void Validate()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = new GameObject("Mobile HUD validation", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                SceneManager.MoveGameObjectToScene(root, scene);
                var hud = root.AddComponent<MobileControlsHud>();
                typeof(MobileControlsHud).GetMethod("Build", Private).Invoke(hud, null);
                var rect = (RectTransform)root.transform;
                var safe = (RectTransform)root.transform.Find("MobileControls");
                var controls = root.GetComponentsInChildren<MobileTouchControl>();
                Check(controls.Length == 5, "Five touch controls");
                foreach (var size in new[] { new Vector2(1000, 650), new Vector2(2164, 1000), new Vector2(1000, 2164), new Vector2(1920, 1080) })
                {
                    rect.sizeDelta = size;
                    float scale = MobileControlsHud.LayoutScale(size);
                    foreach (Transform child in safe) child.localScale = Vector3.one * scale;
                    Canvas.ForceUpdateCanvases();
                    typeof(MobileControlsHud).GetMethod("Arrange", Private).Invoke(hud, new object[] { new Rect(0, 0, Screen.width, Screen.height) });
                    for (int i = 0; i < controls.Length; i++)
                    {
                        if (controls[i].Floating) continue; // Floating visuals have no fixed hit area.
                        Rect bounds = Bounds((RectTransform)controls[i].transform, rect);
                        Check(rect.rect.Contains(bounds.min) && rect.rect.Contains(bounds.max), "Safe bounds: " + controls[i].name + " " + size);
                        for (int j = i + 1; j < controls.Length; j++)
                        {
                            if (controls[j].Floating) continue;
                            Check(!bounds.Overlaps(Bounds((RectTransform)controls[j].transform, rect)), "No overlapping hit areas: " + controls[i].name + "/" + controls[j].name);
                        }
                    }
                }
                var move = Array.Find(controls, c => c.Action == MobileTouchControl.Control.Move);
                var aim = Array.Find(controls, c => c.Action == MobileTouchControl.Control.Aim);
                Check(move.Visibility.alpha == 0 && aim.Visibility.alpha == 0, "Sticks hidden before touch");
                Check(!move.GetComponent<Image>().raycastTarget && !aim.Visibility.blocksRaycasts, "Sticks do not intercept HUD clicks");
                var route = typeof(MobileControlsHud).GetMethod("RoutePointer", Private);
                var area = new Rect(0, 0, Screen.width, Screen.height);
                var left = new Vector2(Screen.width * .25f, Screen.height * .5f);
                var right = new Vector2(Screen.width * .75f, Screen.height * .5f);
                route.Invoke(hud, new object[] { 11, left, true, false, area, null });
                route.Invoke(hud, new object[] { 22, right, true, false, area, null });
                Check(move.HasPointer(11) && aim.HasPointer(22), "Starting half selects independent stick");
                Check(move.Visibility.alpha == 1 && aim.Visibility.alpha == 1, "Sticks visible while held");
                Check(Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null, move.transform.position), left) < .01f, "Movement appears at tap");
                Check(Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null, aim.transform.position), right) < .01f, "Aim appears at tap");
                route.Invoke(hud, new object[] { 33, left + Vector2.up * 10, true, false, area, null });
                Check(move.HasPointer(11), "Extra finger cannot replace owner");
                route.Invoke(hud, new object[] { 11, right, false, false, area, null });
                Check(move.HasPointer(11) && aim.HasPointer(22), "Crossing midpoint preserves ownership");
                Check(Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null, move.transform.position), left) < .01f, "Dragging preserves starting center");
                var moveRect = (RectTransform)move.transform;
                Vector2 outside = RectTransformUtility.WorldToScreenPoint(null, moveRect.TransformPoint(new Vector3(500, 0, 0)));
                move.OnDrag(new PointerEventData(null) { pointerId = 11, position = outside });
                Check(Vector2.Distance(MobileGameInput.Move, Vector2.right) < .001f, "Drag clamps to full deflection");
                Vector2 center = RectTransformUtility.WorldToScreenPoint(null, moveRect.TransformPoint(Vector3.zero));
                move.OnDrag(new PointerEventData(null) { pointerId = 22, position = center });
                Check(MobileGameInput.Move == Vector2.right, "Other finger cannot drag movement");
                move.OnDrag(new PointerEventData(null) { pointerId = 11, position = center });
                Check(MobileGameInput.Move == Vector2.zero, "Center dead zone");
                MobileGameInput.Move = Vector2.right;
                MobileGameInput.Aim = Vector2.up;
                MobileGameInput.AttackHeld = true;
                move.OnPointerUp(new PointerEventData(null) { pointerId = 22 });
                Check(MobileGameInput.Move == Vector2.right, "Other finger cannot release movement");
                move.OnPointerUp(new PointerEventData(null) { pointerId = 11 });
                Check(MobileGameInput.Move == Vector2.zero && MobileGameInput.AttackHeld, "Movement release preserves attack finger");
                Check(move.Visibility.alpha == 0 && aim.Visibility.alpha == 1, "Release hides only owning stick");
                route.Invoke(hud, new object[] { 44, left + Vector2.up * 20, true, false, area, null });
                Check(Vector2.Distance(RectTransformUtility.WorldToScreenPoint(null, move.transform.position), left + Vector2.up * 20) < .01f, "Next tap relocates stick");
                route.Invoke(hud, new object[] { 44, left, false, true, area, null });
                Check(!move.IsHeld && move.Visibility.alpha == 0, "Canceled pointer hides and releases stick");
                aim.gameObject.SetActive(false);
                // MonoBehaviour lifecycle callbacks do not run in an edit-mode preview scene.
                typeof(MobileTouchControl).GetMethod("OnDisable", Private).Invoke(aim, null);
                Check(!MobileGameInput.AttackHeld && MobileGameInput.Aim == Vector2.zero, "Disabled stick releases attack");
                aim.gameObject.SetActive(true);
                MobileGameInput.ShieldHeld = true;
                typeof(MobileControlsHud).GetMethod("OnApplicationFocus", Private).Invoke(hud, new object[] { false });
                Check(!MobileGameInput.ShieldHeld && MobileGameInput.Move == Vector2.zero, "Focus loss clears input");
                foreach (var graphic in root.GetComponentsInChildren<MobilePixelGraphic>())
                {
                    Check(graphic.GetComponent<CanvasRenderer>() != null, "CanvasRenderer required for rendering and raycasts: " + graphic.name);
                    Check(graphic.canvasRenderer != null, "Graphic renderer reference: " + graphic.name);
                    using var vertices = new VertexHelper();
                    typeof(MobilePixelGraphic).GetMethod("OnPopulateMesh", Private | BindingFlags.DeclaredOnly).Invoke(graphic, new object[] { vertices });
                    Check(vertices.currentVertCount > 0, "Pixel geometry: " + graphic.name);
                }
                RenderHud();
                File.WriteAllText("Documentation/mobile-controls-validation.txt", "PASS: five controls; landscape, wide landscape and portrait bounds; no overlapping controls; independent pointer release; disable/focus reset; CanvasRenderer dependencies; pixel meshes; full HUD preview.\nDevice multitouch and gameplay still require a phone smoke test.\n");
            }
            finally { MobileGameInput.Reset(); EditorSceneManager.ClosePreviewScene(scene); }
        }
        private static Rect Bounds(RectTransform item, Transform root)
        {
            var corners = new Vector3[4]; item.GetWorldCorners(corners);
            return Rect.MinMaxRect(root.InverseTransformPoint(corners[0]).x, root.InverseTransformPoint(corners[0]).y,
                root.InverseTransformPoint(corners[2]).x, root.InverseTransformPoint(corners[2]).y);
        }
        private static void RenderHud()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab");
            GameUiBuilder.Render(prefab, "Documentation/UI/MobileControls-HUD.png", root =>
            {
                var hud = root.GetComponent<MobileControlsHud>() ?? root.AddComponent<MobileControlsHud>();
                typeof(MobileControlsHud).GetMethod("Build", Private).Invoke(hud, null);
                Canvas.ForceUpdateCanvases();
                typeof(MobileControlsHud).GetMethod("Arrange", Private).Invoke(hud, new object[] { new Rect(0, 0, Screen.width, Screen.height) });
                var config = AssetDatabase.LoadAssetAtPath<MineArena.Structs.GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
                foreach (var control in root.GetComponentsInChildren<MobileTouchControl>())
                {
                    // Illustrate two held sticks; at rest both are invisible.
                    if (control.Floating)
                    {
                        control.Visibility.alpha = 1;
                        ((RectTransform)control.transform).anchoredPosition = control.Action == MobileTouchControl.Control.Move ? new Vector2(230, 310) : new Vector2(-270, 480);
                    }
                    Sprite icon = null;
                    foreach (var item in config.ItemDatabase.AllItems)
                    {
                        if (item == null || item.Icon == null) continue;
                        if ((control.Action == MobileTouchControl.Control.Aim && item is MineArena.Items.WeaponItemConfig weapon && weapon.Kind == MineArena.Items.WeaponItemKind.Sword) ||
                            (control.Action == MobileTouchControl.Control.Potion && item is MineArena.Items.PotionConfig) ||
                            (control.Action == MobileTouchControl.Control.Shield && item is MineArena.Items.ArmorConfig armor && armor.Slot == MineArena.Items.ArmorSlot.OffHand) ||
                            (control.Action == MobileTouchControl.Control.Interact && item.Name.IndexOf("pickaxe", StringComparison.OrdinalIgnoreCase) >= 0)) { icon = item.Icon; break; }
                    }
                    typeof(MobileControlsHud).GetMethod("SetIcon", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { control, icon });
                }
            });
        }
        private static void Check(bool condition, string name) { if (!condition) throw new Exception(name); }
    }
}
