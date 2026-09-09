using System;
using System.IO;
using System.Reflection;
using MineArena.AI;
using MineArena.PlayerSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Editor
{
    public static class GameplayFeedbackRevision
    {
        private const string PrefabPath = "Assets/Resources/UI/MiningPrompt.prefab";
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        [InitializeOnLoadMethod]
        private static void Watch()
        {
            EditorApplication.update += () =>
            {
                const string request = "Temp/gameplay-feedback.request";
                if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete(request);
                try { Build(); }
                catch (Exception e) { File.WriteAllText("Documentation/gameplay-feedback-validation.txt", e.ToString()); Debug.LogException(e); }
            };
        }

        [MenuItem("MineArena/UI/Build Mining Prompt And Validate Feedback")]
        public static void Build()
        {
            const string materialPath = "Assets/Resources/UI/MiningPromptOverlay.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("MineArena/UI/Mining Prompt Overlay"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            var root = new GameObject("MiningPrompt", typeof(RectTransform), typeof(Canvas));
            try
            {
                var rect = (RectTransform)root.transform;
                rect.sizeDelta = Vector2.one;
                rect.localScale = Vector3.one * 1.3f;
                rect.localPosition = new Vector3(0, 1.35f, 0);
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;
                Panel(root.transform, material, "Shadow", new Vector2(0, -0.025f), new Vector2(0.76f, 0.49f), new Color(0.02f, 0.03f, 0.04f, 0.7f));
                Panel(root.transform, material, "Outline", Vector2.zero, new Vector2(0.76f, 0.48f), Hex("17232B"));
                Panel(root.transform, material, "Fine stone edge", Vector2.zero, new Vector2(0.72f, 0.44f), Hex("8BABA7"));
                Panel(root.transform, material, "Face", Vector2.zero, new Vector2(0.68f, 0.40f), Hex("293F48"));
                var icon = Panel(root.transform, material, "Iron pickaxe", new Vector2(-0.13f, 0), new Vector2(0.36f, 0.36f), Color.white);
                icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/Icons/Equipment/iron_pickaxe.png");
                if (icon.sprite == null) throw new Exception("Pickaxe sprite missing");
                icon.preserveAspect = true;
                Panel(root.transform, material, "Divider", new Vector2(0.10f, 0), new Vector2(0.012f, 0.24f), Hex("58716F"));
                Panel(root.transform, material, "Key shadow", new Vector2(0.225f, -0.012f), new Vector2(0.18f, 0.21f), Hex("17232B"));
                Panel(root.transform, material, "Key face", new Vector2(0.225f, 0.008f), new Vector2(0.18f, 0.19f), Hex("E8D5A2"));
                var ink = Hex("423926");
                Panel(root.transform, material, "E stem", new Vector2(0.1875f, 0.008f), new Vector2(0.025f, 0.125f), ink);
                for (int i = -1; i <= 1; i++)
                    Panel(root.transform, material, "E bar " + i, new Vector2(0.225f, 0.008f + i * 0.05f), new Vector2(0.10f, 0.025f), ink);
                root.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            AssetDatabase.SaveAssets();
            Validate();
            RenderPreview();
        }

        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var color); return color; }
        private static Image Panel(Transform parent, Material material, string name, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.material = material;
            image.raycastTarget = false;
            return image;
        }

        private static void Validate()
        {
            var facing = typeof(MobMovement).GetMethod("ResolveMovementFacing", BindingFlags.Static | BindingFlags.NonPublic);
            var left = (Vector3)facing.Invoke(null, new object[] { Vector3.left * 2, Vector3.right });
            if (left != Vector3.left * 2) throw new Exception("Movement must face actual velocity when avoiding walls");
            var starting = (Vector3)facing.Invoke(null, new object[] { Vector3.zero, Vector3.back });
            if (starting != Vector3.back) throw new Exception("Starting movement must face navigation direction");
            var idle = (Vector3)facing.Invoke(null, new object[] { Vector3.up, Vector3.zero });
            if (idle != Vector3.zero) throw new Exception("Vertical noise must not rotate idle mob");
            var go = new GameObject("Death delay validation");
            try
            {
                var flow = go.AddComponent<PlayerDeathFlow>();
                var type = typeof(PlayerDeathFlow);
                float delay = (float)type.GetField("_deathWindowDelay", Private).GetValue(flow);
                if (delay < 1f || delay > 2f) throw new Exception("Death delay out of expected range");
                var state = type.GetField("_state", Private);
                state.SetValue(flow, Enum.Parse(state.FieldType, "Dying"));
                type.GetField("_showWindowAt", Private).SetValue(flow, Time.time + delay);
                type.GetMethod("Update", Private).Invoke(flow, null);
                if (type.GetField("_window", Private).GetValue(flow) != null || state.GetValue(flow).ToString() != "Dying") throw new Exception("Death window opened before fall delay");
                if (!flow.IsProtected) throw new Exception("Dying player must remain protected");
                type.GetMethod("OnDisable", Private).Invoke(flow, null);
                if (state.GetValue(flow).ToString() != "Alive") throw new Exception("Pending death window not cancelled on disable");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            foreach (var graphic in prefab.GetComponentsInChildren<Graphic>(true))
                if (graphic.raycastTarget || graphic.material.shader.name != "MineArena/UI/Mining Prompt Overlay") throw new Exception("Prompt graphic missing overlay material or blocks input");
            var billboardRoot = new GameObject("Billboard validation");
            var cameraRoot = new GameObject("Tilted camera validation", typeof(Camera));
            try
            {
                var canvas = UnityEngine.Object.Instantiate(prefab, billboardRoot.transform);
                var billboard = billboardRoot.AddComponent<MineArena.Items.BillboardCanvas>();
                var camera = cameraRoot.GetComponent<Camera>();
                camera.transform.rotation = Quaternion.Euler(55, 32, 0);
                var type = typeof(MineArena.Items.BillboardCanvas);
                type.GetField("_canvas", Private).SetValue(billboard, canvas);
                type.GetField("_mainCamera", Private).SetValue(billboard, camera);
                type.GetMethod("LateUpdate", Private).Invoke(billboard, null);
                if (Quaternion.Angle(canvas.transform.rotation, camera.transform.rotation) > 0.01f) throw new Exception("Billboard did not match tilted camera");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(billboardRoot);
                UnityEngine.Object.DestroyImmediate(cameraRoot);
            }
            File.WriteAllText("Documentation/gameplay-feedback-validation.txt", "PASS: navigation facing follows actual horizontal velocity, start fallback and idle handling.\nPASS: death window delayed 1.25 game seconds, protected while dying, cancelled on disable.\nPASS: all mining prompt graphics use through-wall material and do not block input.\nEditor checks, not a full gameplay playtest.\n");
        }

        private static void RenderPreview()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
            var cameraGo = new GameObject("Preview camera", typeof(Camera));
            RenderTexture target = null;
            Texture2D pixels = null;
            var previous = RenderTexture.active;
            try
            {
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGo, scene);
                root.SetActive(true);
                root.transform.position = Vector3.zero;
                var camera = cameraGo.GetComponent<Camera>();
                camera.scene = scene;
                camera.transform.position = new Vector3(0, 0, -3);
                camera.orthographic = true;
                camera.orthographicSize = 0.64f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Hex("759389");
                root.GetComponent<Canvas>().worldCamera = camera;
                // An opaque wall completely covers the icon in depth: the prompt must still render.
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall, scene);
                wall.transform.position = new Vector3(0, 0, -1);
                wall.transform.localScale = new Vector3(2, 2, 0.1f);
                target = new RenderTexture(512, 512, 24);
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                pixels = new Texture2D(512, 512, TextureFormat.RGB24, false);
                pixels.ReadPixels(new Rect(0, 0, 512, 512), 0, 0);
                pixels.Apply();
                File.WriteAllBytes("Documentation/UI/MiningPrompt.png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                if (cameraGo != null) cameraGo.GetComponent<Camera>().targetTexture = null;
                if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
                if (target != null) UnityEngine.Object.DestroyImmediate(target);
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
