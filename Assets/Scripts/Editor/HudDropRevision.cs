using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using MineArena.Game.UI;
using MineArena.Items;
using MineArena.ObjectPools;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static class HudDropRevision
    {
        private const string Hud = "Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab";
        private const string Art = "Assets/Art/UI/Accents/";
        private const BindingFlags Fields = BindingFlags.NonPublic | BindingFlags.Instance;

        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/hud-drop-revision.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            bool validateOnly = File.ReadAllText(request).Trim() == "validate";
            File.Delete(request);
            try { if (validateOnly) ValidateAndRender(); else Rebuild(); }
            catch (Exception e) { File.WriteAllText("Documentation/hud-drop-validation.txt", "FAIL " + e); Debug.LogException(e); }
        };

        [MenuItem("MineArena/UI/Refresh Vitals And Mob Bars")]
        public static void Rebuild()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var pool = AssetDatabase.LoadAssetAtPath<ObjectPoolPreset>("Assets/Scripts/ObjectPool/Pools/MobsPoolsPeresets.asset");
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mobs/MobHealthCanvas.prefab");
            foreach (var prefab in pool.Preset)
            {
                if (prefab.GetComponentInChildren<EnemyHealthBar>(true) != null) continue;
                string path = AssetDatabase.GetAssetPath(prefab);
                var mob = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    PrefabUtility.InstantiatePrefab(template, mob.transform);
                    PrefabUtility.SaveAsPrefabAsset(mob, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(mob); }
            }
            var root = PrefabUtility.LoadPrefabContents(Hud);
            try
            {
                Skin(root.GetComponentInChildren<PlayerHealthBar>(true), false);
                Skin(root.GetComponentInChildren<PlayerArmorBar>(true), true);
                PrefabUtility.SaveAsPrefabAsset(root, Hud);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            ValidateAndRender();
        }

        private static void ValidateAndRender()
        {
            var pool = AssetDatabase.LoadAssetAtPath<ObjectPoolPreset>("Assets/Scripts/ObjectPool/Pools/MobsPoolsPeresets.asset");
            Validate(pool);
            GameUiBuilder.Render(AssetDatabase.LoadAssetAtPath<GameObject>(Hud), "Documentation/UI/PlayerVitals.png", preview =>
            {
                var hp = preview.GetComponentInChildren<PlayerHealthBar>(true);
                ((Image)hp.GetType().GetField("_fillImage", Fields).GetValue(hp)).fillAmount = 0.73f;
                ((TMP_Text)hp.GetType().GetField("_valueText", Fields).GetValue(hp)).text = "73 / 100";
                var armor = preview.GetComponentInChildren<PlayerArmorBar>(true);
                ((Image)armor.GetType().GetField("_fillImage", Fields).GetValue(armor)).fillAmount = 0;
                ((TMP_Text)armor.GetType().GetField("_valueText", Fields).GetValue(armor)).text = "0%";
            });
        }

        private static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var color); return color; }

        private static Image Image(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            Place(image.rectTransform, x, y, w, h);
            return image;
        }

        private static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            rect.localScale = Vector3.one;
        }

        private static Sprite Sprite(string name, bool armor, bool icon)
        {
            int width = icon ? 12 : 8, height = icon ? 12 : 8;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            string[] heart = { "............", "..##...##...", ".####.####..", ".#########..", ".#########..", "..#######...", "...#####....", "....###.....", ".....#......", "............", "............", "............" };
            string[] shield = { "............", "..########..", "..########..", "..########..", "..########..", "..########..", "...######...", "...######...", "....####....", ".....##.....", "............", "............" };
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                Color color;
                if (icon)
                {
                    color = (armor ? shield : heart)[height - 1 - y][x] == '.' ? Color.clear :
                        armor ? C(x < 6 ? "B4D4D0" : "709CA0") : C(y > 6 ? "E8A17B" : "C56851");
                }
                else color = y > 5 ? C(armor ? "84ADB0" : "D88C6C") : y < 2 ? C(armor ? "426B72" : "874937") : C(armor ? "608F96" : "B56950");
                texture.SetPixel(x, y, color);
            }
            texture.Apply();
            string path = Art + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void Skin(MonoBehaviour controller, bool armor)
        {
            var bar = (RectTransform)controller.transform;
            var oldText = bar.GetComponentInChildren<TMP_Text>(true);
            var font = oldText != null ? oldText.font : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset");
            foreach (Transform child in bar.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            var background = bar.GetComponent<Image>();
            background.sprite = null;
            background.type = UnityEngine.UI.Image.Type.Simple;
            background.color = C("4B3A2B");
            background.raycastTarget = false;
            Image("Rim", bar, 1, 1, 278, 28, C("BDA679"));
            Image("TopEdge", bar, 1, 1, 278, 1, C("F6E8C7"));
            Image("IconWell", bar, 3, 3, 28, 24, C("625444"));
            var icon = Image("StatIcon", bar, 5, 3, 24, 24, Color.white);
            icon.sprite = Sprite(armor ? "hud-shield" : "hud-heart", armor, true);
            var track = Image("Track", bar, 33, 4, 243, 22, C(armor ? "354E52" : "50372F"));
            var fill = Image("Fill", track.transform, 0, 0, 243, 22, Color.white);
            fill.sprite = Sprite(armor ? "hud-armor-fill" : "hud-health-fill", armor, false);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = armor ? 0 : 1;
            for (int i = 1; i < 5; i++) Image("Division" + i, track.transform, Mathf.Round(243 * i / 5f), 0, 1, 22, new Color(0.16f, 0.12f, 0.09f, 0.22f));
            var text = new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(bar, false);
            Place(text.rectTransform, 33, 3, 243, 24);
            text.font = font;
            text.fontSize = 18;
            text.color = C("FFF3D8");
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.text = armor ? "0%" : "100 / 100";
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.15f, 0.1f, 0.07f, 0.9f);
            shadow.effectDistance = new Vector2(1, -1);
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("_fillImage").objectReferenceValue = fill;
            serialized.FindProperty("_valueText").objectReferenceValue = text;
            if (!armor)
            {
                serialized.FindProperty("_animatedHeightOffset").floatValue = 0;
                serialized.FindProperty("_animatedTextScaleOffset").floatValue = 0;
                serialized.FindProperty("_animatedTextRect").objectReferenceValue = text.rectTransform;
                serialized.FindProperty("_smoothTextValue").boolValue = false;
                serialized.FindProperty("_format").stringValue = "{0} / {1}";
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Validate(ObjectPoolPreset pool)
        {
            var report = new List<string>();
            void Check(bool ok, string message) { if (!ok) throw new Exception(message); report.Add("PASS " + message); }
            foreach (var mob in pool.Preset)
                Check(mob.GetComponentsInChildren<EnemyHealthBar>(true).Length == 1, mob.name + ": exactly one shared enemy healthbar");
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>(Hud);
            foreach (var controller in new MonoBehaviour[] { hud.GetComponentInChildren<PlayerHealthBar>(true), hud.GetComponentInChildren<PlayerArmorBar>(true) })
            {
                var fill = (Image)controller.GetType().GetField("_fillImage", Fields).GetValue(controller);
                Check(fill != controller.GetComponent<Image>() && fill.type == UnityEngine.UI.Image.Type.Filled, controller.name + ": fill is separate from persistent frame");
                Check(controller.transform.Find("Track").GetComponent<Image>().color.a == 1, controller.name + ": empty track stays opaque");
            }
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            // DOTween skips Kill outside play mode until initialized. Drive only our tweens
            // manually, without creating a persistent runtime updater in the user's scene.
            var initialized = typeof(DOTween).GetField("initialized", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var wasInitialized = initialized.GetValue(null);
            try
            {
                initialized.SetValue(null, true);
                var floor = new GameObject("Drop test floor", typeof(BoxCollider));
                floor.transform.position = new Vector3(10000, 999, 10000);
                floor.GetComponent<BoxCollider>().size = new Vector3(5, 1, 5);
                var pickup = new GameObject("Drop test", typeof(BoxCollider), typeof(Rigidbody), typeof(AnimationIDLE), typeof(AnimationDrop));
                pickup.transform.position = new Vector3(10000, 1000, 10000);
                pickup.transform.rotation = Quaternion.Euler(0, 180, 0);
                var landedRotation = pickup.transform.rotation;
                var idle = pickup.GetComponent<AnimationIDLE>();
                typeof(AnimationIDLE).GetField("_step", Fields).SetValue(idle, Vector3.up * 0.27f);
                typeof(AnimationIDLE).GetField("_rotation", Fields).SetValue(idle, Vector3.up * 180);
                typeof(AnimationIDLE).GetField("_duration", Fields).SetValue(idle, 2.2f);
                typeof(AnimationIDLE).GetField("_repeats", Fields).SetValue(idle, -1);
                var drop = pickup.GetComponent<AnimationDrop>();
                typeof(AnimationDrop).GetMethod("Awake", Fields).Invoke(drop, null);
                typeof(AnimationDrop).GetField("_launchedAt", Fields).SetValue(drop, Time.time - 1);
                Physics.SyncTransforms();
                typeof(AnimationDrop).GetMethod("FixedUpdate", Fields).Invoke(drop, null);
                Check(pickup.GetComponent<Rigidbody>().isKinematic && pickup.GetComponent<Collider>().isTrigger, "Drop settles on a Default-layer floor, not only Ground");
                var move = (Tween)typeof(AnimationIDLE).GetField("_moveTween", Fields).GetValue(idle);
                var rotation = (Tween)typeof(AnimationIDLE).GetField("_rotationTween", Fields).GetValue(idle);
                move.SetUpdate(UpdateType.Manual); rotation.SetUpdate(UpdateType.Manual);
                move.Goto(1.1f); rotation.Goto(1.1f);
                Check(pickup.transform.position.y > 1000.1f && Quaternion.Angle(pickup.transform.rotation, landedRotation) > 80, "Landed pickup bobs and rotates even from a 180-degree starting pose");
                drop.StartAnimation();
                Check(!move.IsActive() && !rotation.IsActive() && !pickup.GetComponent<Rigidbody>().isKinematic && !pickup.GetComponent<Collider>().isTrigger,
                    "Relaunch stops idle tweens and restores physical flight");
                Object.DestroyImmediate(pickup);
                Object.DestroyImmediate(floor);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                initialized.SetValue(null, wasInitialized);
                SceneManager.SetActiveScene(previous);
            }
            File.WriteAllLines("Documentation/hud-drop-validation.txt", report);
        }
    }
}
