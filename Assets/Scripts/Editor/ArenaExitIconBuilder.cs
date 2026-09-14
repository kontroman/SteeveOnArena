using System;
using System.IO;
using Devotion.SDK.UI;
using MineArena.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Editor
{
    public static class ArenaExitIconBuilder
    {
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string confirmationRequest = "Temp/arena-exit-style.request";
            if (File.Exists(confirmationRequest) && !EditorApplication.isCompiling && !EditorApplication.isUpdating
                && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete(confirmationRequest);
                try { BuildConfirmation(); }
                catch (Exception e) { File.WriteAllText("Temp/arena-exit-style-result.txt", "FAIL: " + e); Debug.LogException(e); }
            }
            const string request = "Temp/arena-exit-icon.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Build(); }
            catch (Exception e) { File.WriteAllText("Temp/arena-exit-icon-result.txt", "FAIL: " + e); Debug.LogException(e); }
        };

        [MenuItem("MineArena/UI/Style Arena Exit Confirmation")]
        public static void BuildConfirmation()
        {
            const string path = "Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                StyleConfirmation(root.transform.Find("ArenaExit/Confirmation"));
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            GameUiBuilder.Render(AssetDatabase.LoadAssetAtPath<GameObject>(path), "Documentation/UI/ArenaExit.png", preview =>
            {
                foreach (Transform child in preview.transform) child.gameObject.SetActive(child.name == "ArenaExit");
                preview.transform.Find("ArenaExit/Exit").gameObject.SetActive(false);
                preview.transform.Find("ArenaExit/Confirmation").gameObject.SetActive(true);
            });
            File.WriteAllText("Temp/arena-exit-style-result.txt", "PASS: confirmation prefab styled and preview rendered; existing action bindings retained.");
        }

        public static void StyleConfirmation(Transform confirmation)
        {
            Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Expedition/beige-" + name + ".png");
            void Surface(Image image, string sprite)
            { image.sprite = Sprite(sprite); image.type = Image.Type.Sliced; image.color = Color.white; }
            void Box(RectTransform rect, float x, float y, float w, float h)
            {
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
            }
            Image Decoration(Transform parent, string name, string sprite, float x, float y, float w, float h)
            {
                var existing = parent.Find(name);
                var go = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
                go.layer = 5; go.transform.SetParent(parent, false);
                var image = go.GetComponent<Image>(); Surface(image, sprite); image.raycastTarget = false;
                Box(image.rectTransform, x, y, w, h); return image;
            }
            confirmation.GetComponent<Image>().color = new Color(.15f, .13f, .09f, .78f);
            var panel = (RectTransform)confirmation.Find("Panel");
            panel.sizeDelta = new Vector2(840, 410);
            Surface(panel.GetComponent<Image>(), "panel");
            var outline = panel.GetComponent<Outline>() ?? panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(101, 85, 61, 255); outline.effectDistance = new Vector2(3, -3);
            var brick = Decoration(panel, "PaleBrickTexture", "brick", 6, 6, 828, 398);
            brick.type = Image.Type.Tiled; brick.color = new Color(1, 1, 1, .4f); brick.transform.SetAsFirstSibling();
            var header = Decoration(panel, "Header", "card", 6, 6, 828, 90);
            header.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Accents/ribbon-468D91.png");
            header.transform.SetSiblingIndex(1);
            var pixel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset");
            var title = panel.Find("Title").GetComponent<TMP_Text>();
            Box(title.rectTransform, 32, 20, 776, 60);
            title.font = pixel; title.fontSize = title.fontSizeMax = 34; title.fontSizeMin = 24;
            title.enableAutoSizing = true; title.color = new Color32(255, 250, 233, 255); title.alignment = TextAlignmentOptions.MidlineLeft;
            var warning = panel.Find("Warning").GetComponent<TMP_Text>();
            Box(warning.rectTransform, 36, 120, 768, 150);
            warning.font = pixel; warning.fontSize = warning.fontSizeMax = 25; warning.fontSizeMin = 18;
            warning.enableAutoSizing = true; warning.enableWordWrapping = true;
            warning.color = new Color32(81, 71, 55, 255); warning.alignment = TextAlignmentOptions.MidlineLeft;
            foreach (string name in new[] { "Cancel", "Confirm" })
            {
                bool stay = name == "Cancel";
                var button = panel.Find(name).GetComponent<Button>();
                Box((RectTransform)button.transform, stay ? 36 : 432, 306, 372, 68);
                Surface(button.GetComponent<Image>(), stay ? "button" : "card");
                var label = button.GetComponentInChildren<TMP_Text>(true);
                label.font = pixel; label.fontSize = label.fontSizeMax = 25; label.fontSizeMin = 17; label.enableAutoSizing = true;
                label.color = stay ? new Color32(255, 250, 233, 255) : new Color32(81, 71, 55, 255);
                label.raycastTarget = false;
            }
            title.raycastTarget = warning.raycastTarget = false;
            var fit = confirmation.GetComponent<MineArena.Windows.SelectLevel.LevelWindowFit>()
                ?? confirmation.gameObject.AddComponent<MineArena.Windows.SelectLevel.LevelWindowFit>();
            var data = new SerializedObject(fit);
            data.FindProperty("panel").objectReferenceValue = panel; data.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("MineArena/UI/Style Arena Exit Icon")]
        public static void Build()
        {
            const string path = "Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var button = root.transform.Find("ArenaExit/Exit").GetComponent<Button>();
                Style(button);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                typeof(ArenaRevision).GetMethod("ValidateBehavior", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(null, new object[] { saved });
                GameUiBuilder.Render(saved, "Documentation/UI/ArenaExitIcon.png", preview =>
                {
                    preview.transform.Find("ArenaExit/Exit").gameObject.SetActive(true);
                    preview.transform.Find("ArenaExit/Confirmation").gameObject.SetActive(false);
                    preview.transform.Find("GiftNavigation").gameObject.SetActive(false);
                });
                File.WriteAllText("Temp/arena-exit-icon-result.txt", "PASS: 76px framed exit icon, non-raycast door graphic, localized hover/keyboard hint; existing confirmation references retained.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void Style(Button button)
        {
            var rect = (RectTransform)button.transform;
            rect.anchorMin = rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-78, -250);
            rect.sizeDelta = new Vector2(76, 76);
            var background = button.GetComponent<Image>();
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Accents/nav-Inventory.png");
            background.type = Image.Type.Sliced;
            background.color = Color.white;
            if (rect.Find("Icon") == null)
            {
                var icon = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(ArenaExitGraphic));
                icon.transform.SetParent(rect, false);
                var iconRect = (RectTransform)icon.transform;
                iconRect.anchorMin = Vector2.zero; iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(10, 10); iconRect.offsetMax = new Vector2(-10, -10);
                icon.GetComponent<ArenaExitGraphic>().raycastTarget = false;
            }
            if (rect.Find("Tooltip") != null) return;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            var tooltip = new GameObject("Tooltip", typeof(RectTransform), typeof(Image));
            tooltip.transform.SetParent(rect, false);
            var hintRect = (RectTransform)tooltip.transform;
            hintRect.anchorMin = hintRect.anchorMax = new Vector2(0, 0.5f);
            hintRect.pivot = new Vector2(1, 0.5f);
            hintRect.anchoredPosition = new Vector2(-12, 0);
            hintRect.sizeDelta = new Vector2(232, 44);
            var hintImage = tooltip.GetComponent<Image>();
            hintImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/Accents/tooltip.png");
            hintImage.type = Image.Type.Sliced; hintImage.raycastTarget = false;
            label.transform.SetParent(hintRect, false);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(10, 0); label.rectTransform.offsetMax = new Vector2(-10, 0);
            label.fontSize = 20; label.fontSizeMax = 20; label.fontSizeMin = 14;
            label.color = new Color32(255, 241, 211, 255);
            label.raycastTarget = false;
            var hint = button.gameObject.AddComponent<IconButtonHint>();
            var data = new SerializedObject(hint);
            data.FindProperty("hint").objectReferenceValue = tooltip;
            data.ApplyModifiedPropertiesWithoutUndo();
            tooltip.SetActive(false);
        }
    }
}
