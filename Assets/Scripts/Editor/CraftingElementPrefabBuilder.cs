using System.IO;
using MineArena.UI;
using MineArena.Windows.Crafting;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Editor
{
    public static class CraftingElementPrefabBuilder
    {
        private const string PrefabDirectory = "Assets/Resources/Prefabs/Windows/Crafting";
        private const string TabPrefabPath = PrefabDirectory + "/CraftingTabButton.prefab";
        private const string ItemPrefabPath = PrefabDirectory + "/CraftingItemView.prefab";
        private const string ResourceIconPath = "Assets/Resources/Prefabs/Windows/ResourceIcon.prefab";
        private const string TextureDirectory = PrefabDirectory + "/Textures";

        [InitializeOnLoadMethod]
        private static void BuildMissingPrefabsOnReload()
        {
            EditorApplication.delayCall += BuildMissing;
        }

        [MenuItem("MineArena/UI/Build Missing Crafting Element Prefabs")]
        public static void BuildMissing()
        {
            EnsureFolder(PrefabDirectory);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(TabPrefabPath) == null)
            {
                BuildTabPrefab();
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath) == null)
            {
                BuildItemPrefab();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildTabPrefab()
        {
            var root = new GameObject("CraftingTabButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CraftingTabButton));
            SetLayerRecursively(root, 5);

            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(156f, 42f);

            var background = root.GetComponent<Image>();
            background.sprite = LoadSprite("craft_button");
            background.type = Image.Type.Sliced;
            background.raycastTarget = true;

            var button = root.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;

            var selected = CreatePanel("Selected", rect, LoadSprite("craft_button_selected"));
            Stretch(selected);
            selected.GetComponent<Image>().raycastTarget = false;
            selected.gameObject.SetActive(false);

            var label = CreateText("Label", rect, "Tab", 16f, FontStyles.Bold, TextAlignmentOptions.Center);
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 16f;
            Stretch(label.rectTransform, 8f);

            var component = root.GetComponent<CraftingTabButton>();
            var serializedObject = new SerializedObject(component);
            SetObject(serializedObject, "_label", label);
            SetObject(serializedObject, "_button", button);
            SetObject(serializedObject, "_selectedVisual", selected.gameObject);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, TabPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildItemPrefab()
        {
            var root = new GameObject("CraftingItemView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(LayoutElement), typeof(CraftingItemView));
            SetLayerRecursively(root, 5);

            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(0f, 74f);

            var background = root.GetComponent<Image>();
            background.sprite = LoadSprite("craft_slot");
            background.type = Image.Type.Sliced;
            background.raycastTarget = true;

            var button = root.GetComponent<Button>();
            button.targetGraphic = background;
            button.transition = Selectable.Transition.ColorTint;

            var layoutElement = root.GetComponent<LayoutElement>();
            layoutElement.minHeight = 74f;
            layoutElement.preferredHeight = 74f;
            layoutElement.flexibleWidth = 1f;

            var selected = CreatePanel("SelectionHighlight", rect, LoadSprite("craft_slot_selected"));
            Stretch(selected);
            selected.GetComponent<Image>().raycastTarget = false;
            selected.gameObject.SetActive(false);

            var content = CreateRect("Content", rect);
            Stretch(content, 10f, 9f, 12f, 9f);

            var contentLayout = content.gameObject.AddComponent<HorizontalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.childAlignment = TextAnchor.MiddleLeft;
            contentLayout.childControlWidth = false;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = false;
            contentLayout.childForceExpandHeight = true;

            var iconSlot = CreatePanel("IconSlot", content, LoadSprite("craft_panel_inset"));
            iconSlot.sizeDelta = new Vector2(54f, 54f);
            var iconSlotLayout = iconSlot.gameObject.AddComponent<LayoutElement>();
            iconSlotLayout.preferredWidth = 54f;
            iconSlotLayout.preferredHeight = 54f;

            var flatIcon = CreateImage("Icon", iconSlot, LoadSprite("craft_placeholder_icon"));
            flatIcon.preserveAspect = true;
            Stretch(flatIcon.rectTransform, 8f);

            var blockIcon = CreateResourceIcon(iconSlot);

            var textRoot = CreateRect("Text", content);
            var textLayout = textRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 1f;
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textRoot.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var name = CreateText("Name", textRoot, "Item", 17f, FontStyles.Bold, TextAlignmentOptions.Left);
            name.enableAutoSizing = true;
            name.fontSizeMin = 10f;
            name.fontSizeMax = 17f;

            var meta = CreateText("Meta", textRoot, "Category", 13f, FontStyles.Normal, TextAlignmentOptions.Left);
            meta.color = new Color32(163, 155, 135, 255);

            var lockedState = CreateText("LockedState", rect, "Locked", 16f, FontStyles.Bold, TextAlignmentOptions.Right);
            lockedState.color = new Color32(238, 104, 76, 255);
            Stretch(lockedState.rectTransform, 10f);
            lockedState.gameObject.SetActive(false);

            var component = root.GetComponent<CraftingItemView>();
            var serializedObject = new SerializedObject(component);
            SetObject(serializedObject, "_button", button);
            SetObject(serializedObject, "_icon", flatIcon);
            SetObject(serializedObject, "_blockIcon", blockIcon);
            SetObject(serializedObject, "_name", name);
            SetObject(serializedObject, "_meta", meta);
            SetObject(serializedObject, "_lockedState", lockedState.gameObject);
            SetObject(serializedObject, "_canvasGroup", root.GetComponent<CanvasGroup>());
            SetObject(serializedObject, "_selectionHighlight", selected.gameObject);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ItemPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static ResourceIcon CreateResourceIcon(RectTransform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<ResourceIcon>(ResourceIconPath);

            if (prefab == null)
                return null;

            var instance = (ResourceIcon)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = "BlockIcon";
            instance.gameObject.SetActive(false);

            if (instance.transform is RectTransform rect)
            {
                Stretch(rect);
            }

            return instance;
        }

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{TextureDirectory}/{name}.png");
        }

        private static RectTransform CreatePanel(string name, Transform parent, Sprite sprite)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = true;
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = new Color32(245, 238, 218, 255);
            label.raycastTarget = false;
            label.enableWordWrapping = false;

            if (TMP_Settings.defaultFontAsset != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
            }

            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            SetLayerRecursively(gameObject, 5);
            var rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.localPosition = Vector3.zero;
            return rect;
        }

        private static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0f);
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            Stretch(rect, padding, padding, padding, padding);
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = Path.GetFileName(path);

            if (!string.IsNullOrWhiteSpace(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folder);
        }

        private static void SetLayerRecursively(GameObject gameObject, int layer)
        {
            gameObject.layer = layer;

            foreach (Transform child in gameObject.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
