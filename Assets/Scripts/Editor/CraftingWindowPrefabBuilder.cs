using System.Collections.Generic;
using System.IO;
using MineArena.Windows.Crafting;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Editor
{
    public static class CraftingWindowPrefabBuilder
    {
        private const string PrefabPath = "Assets/Resources/Prefabs/Windows/Crafting/CraftingWindow.prefab";
        private const string TextureDirectory = "Assets/Resources/Prefabs/Windows/Crafting/Textures";

        [InitializeOnLoadMethod]
        private static void BuildMissingPrefabOnReload()
        {
            EditorApplication.delayCall += () =>
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                    return;

                Build();
            };
        }

        [MenuItem("MineArena/UI/Rebuild Crafting Window Prefab")]
        public static void Build()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/Prefabs");
            EnsureFolder("Assets/Resources/Prefabs/Windows");
            EnsureFolder("Assets/Resources/Prefabs/Windows/Crafting");
            EnsureFolder(TextureDirectory);

            var sprites = CreateSprites();
            var root = CreateWindowObject(sprites);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CraftingWindowPrefabBuilder] Prefab rebuilt: {PrefabPath}");
        }

        private static Dictionary<string, Sprite> CreateSprites()
        {
            return new Dictionary<string, Sprite>
            {
                ["Overlay"] = CreateFlatSprite("craft_overlay", new Color32(0, 0, 0, 170), 4f),
                ["Panel"] = CreateFrameSprite("craft_panel", new Color32(55, 52, 48, 255), new Color32(18, 16, 14, 255), new Color32(116, 108, 94, 255), new Color32(29, 27, 25, 255)),
                ["PanelInset"] = CreateFrameSprite("craft_panel_inset", new Color32(36, 35, 33, 255), new Color32(13, 12, 11, 255), new Color32(79, 74, 66, 255), new Color32(21, 20, 19, 255)),
                ["Slot"] = CreateFrameSprite("craft_slot", new Color32(80, 76, 68, 255), new Color32(20, 18, 16, 255), new Color32(142, 132, 113, 255), new Color32(42, 39, 35, 255)),
                ["SlotSelected"] = CreateFrameSprite("craft_slot_selected", new Color32(98, 90, 72, 255), new Color32(248, 219, 106, 255), new Color32(255, 241, 154, 255), new Color32(122, 91, 32, 255)),
                ["Button"] = CreateFrameSprite("craft_button", new Color32(96, 74, 51, 255), new Color32(30, 21, 14, 255), new Color32(148, 114, 74, 255), new Color32(55, 39, 28, 255)),
                ["ButtonSelected"] = CreateFrameSprite("craft_button_selected", new Color32(113, 92, 55, 255), new Color32(242, 207, 99, 255), new Color32(255, 230, 132, 255), new Color32(83, 55, 23, 255)),
                ["ButtonDisabled"] = CreateFrameSprite("craft_button_disabled", new Color32(61, 59, 57, 255), new Color32(24, 22, 21, 255), new Color32(84, 80, 75, 255), new Color32(38, 36, 34, 255)),
                ["CraftButton"] = CreateFrameSprite("craft_button_green", new Color32(67, 128, 62, 255), new Color32(18, 45, 24, 255), new Color32(110, 181, 96, 255), new Color32(35, 77, 39, 255)),
                ["Placeholder"] = CreatePlaceholderSprite()
            };
        }

        private static GameObject CreateWindowObject(IReadOnlyDictionary<string, Sprite> sprites)
        {
            var root = new GameObject("CraftingWindow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(CraftingWindow));
            root.SetActive(false);
            SetLayerRecursively(root, 5);

            var rootRect = (RectTransform)root.transform;
            Stretch(rootRect);

            var rootImage = root.GetComponent<Image>();
            rootImage.sprite = sprites["Overlay"];
            rootImage.type = Image.Type.Simple;
            rootImage.raycastTarget = true;

            var canvasGroup = root.GetComponent<CanvasGroup>();

            var windowPanel = CreatePanel("PixelCraftPanel", rootRect, sprites["Panel"], out var windowPanelImage);
            windowPanel.anchorMin = new Vector2(0.08f, 0.08f);
            windowPanel.anchorMax = new Vector2(0.92f, 0.92f);
            windowPanel.offsetMin = Vector2.zero;
            windowPanel.offsetMax = Vector2.zero;

            BuildHeader(windowPanel, sprites, out var tabsPanelImage, out var tabsRoot);
            BuildBody(windowPanel, sprites, out var leftPanelImage, out var rightPanelImage, out var listViewportImage, out var itemsRoot, out var detailIconSlotImage, out var detailIcon, out var detailName, out var detailDescription, out var detailRequirement, out var costsRoot, out var emptyState, out var resultText, out var craftButton, out var craftButtonImage, out var craftButtonLabel);

            var window = root.GetComponent<CraftingWindow>();
            var serializedObject = new SerializedObject(window);
            SetObject(serializedObject, "_rootImage", rootImage);
            SetObject(serializedObject, "_windowPanelImage", windowPanelImage);
            SetObject(serializedObject, "_tabsPanelImage", tabsPanelImage);
            SetObject(serializedObject, "_leftPanelImage", leftPanelImage);
            SetObject(serializedObject, "_rightPanelImage", rightPanelImage);
            SetObject(serializedObject, "_listViewportImage", listViewportImage);
            SetObject(serializedObject, "_detailIconSlotImage", detailIconSlotImage);
            SetObject(serializedObject, "_canvasGroup", canvasGroup);
            SetObject(serializedObject, "_windowPanel", windowPanel);
            SetObject(serializedObject, "_tabsRoot", tabsRoot);
            SetObject(serializedObject, "_itemsRoot", itemsRoot);
            SetObject(serializedObject, "_costsRoot", costsRoot);
            SetObject(serializedObject, "_detailIcon", detailIcon);
            SetObject(serializedObject, "_detailName", detailName);
            SetObject(serializedObject, "_detailDescription", detailDescription);
            SetObject(serializedObject, "_detailRequirement", detailRequirement);
            SetObject(serializedObject, "_emptyState", emptyState);
            SetObject(serializedObject, "_resultText", resultText);
            SetObject(serializedObject, "_craftButton", craftButton);
            SetObject(serializedObject, "_craftButtonImage", craftButtonImage);
            SetObject(serializedObject, "_craftButtonLabel", craftButtonLabel);
            SetObject(serializedObject, "_overlaySprite", sprites["Overlay"]);
            SetObject(serializedObject, "_panelSprite", sprites["Panel"]);
            SetObject(serializedObject, "_panelInsetSprite", sprites["PanelInset"]);
            SetObject(serializedObject, "_slotSprite", sprites["Slot"]);
            SetObject(serializedObject, "_slotSelectedSprite", sprites["SlotSelected"]);
            SetObject(serializedObject, "_buttonSprite", sprites["Button"]);
            SetObject(serializedObject, "_buttonSelectedSprite", sprites["ButtonSelected"]);
            SetObject(serializedObject, "_buttonDisabledSprite", sprites["ButtonDisabled"]);
            SetObject(serializedObject, "_craftButtonSprite", sprites["CraftButton"]);
            SetObject(serializedObject, "_placeholderIconSprite", sprites["Placeholder"]);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static void BuildHeader(RectTransform parent, IReadOnlyDictionary<string, Sprite> sprites, out Image tabsPanelImage, out RectTransform tabsRoot)
        {
            var header = CreateRect("Header", parent);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(22f, -86f);
            header.offsetMax = new Vector2(-22f, -18f);

            var title = CreateText("Title", header, "Craft / Shop", 28f, FontStyles.Bold, TextAlignmentOptions.Left);
            title.rectTransform.anchorMin = new Vector2(0f, 0f);
            title.rectTransform.anchorMax = new Vector2(0f, 1f);
            title.rectTransform.sizeDelta = new Vector2(190f, 0f);

            var tabsPanel = CreatePanel("TabsPanel", header, sprites["PanelInset"], out tabsPanelImage);
            tabsPanel.anchorMin = Vector2.zero;
            tabsPanel.anchorMax = Vector2.one;
            tabsPanel.offsetMin = new Vector2(205f, 0f);
            tabsPanel.offsetMax = Vector2.zero;

            tabsRoot = CreateRect("Tabs", tabsPanel);
            Stretch(tabsRoot, 10f, 8f, 10f, 8f);

            var tabsLayout = tabsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            tabsLayout.spacing = 8f;
            tabsLayout.childAlignment = TextAnchor.MiddleLeft;
            tabsLayout.childControlWidth = false;
            tabsLayout.childControlHeight = true;
            tabsLayout.childForceExpandWidth = false;
            tabsLayout.childForceExpandHeight = true;
        }

        private static void BuildBody(RectTransform parent, IReadOnlyDictionary<string, Sprite> sprites, out Image leftPanelImage, out Image rightPanelImage, out Image listViewportImage, out RectTransform itemsRoot, out Image detailIconSlotImage, out Image detailIcon, out TextMeshProUGUI detailName, out TextMeshProUGUI detailDescription, out TextMeshProUGUI detailRequirement, out RectTransform costsRoot, out TextMeshProUGUI emptyState, out TextMeshProUGUI resultText, out Button craftButton, out Image craftButtonImage, out TextMeshProUGUI craftButtonLabel)
        {
            var body = CreateRect("Body", parent);
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(22f, 22f);
            body.offsetMax = new Vector2(-22f, -98f);

            var bodyLayout = body.gameObject.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 14f;
            bodyLayout.childAlignment = TextAnchor.MiddleCenter;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = true;
            bodyLayout.childForceExpandHeight = true;

            var leftPanel = CreatePanel("RecipeListPanel", body, sprites["PanelInset"], out leftPanelImage);
            var leftLayout = leftPanel.gameObject.AddComponent<LayoutElement>();
            leftLayout.flexibleWidth = 0.42f;
            leftLayout.minWidth = 310f;
            BuildListPanel(leftPanel, sprites, out listViewportImage, out itemsRoot);

            var rightPanel = CreatePanel("DetailsPanel", body, sprites["PanelInset"], out rightPanelImage);
            var rightLayout = rightPanel.gameObject.AddComponent<LayoutElement>();
            rightLayout.flexibleWidth = 0.58f;
            rightLayout.minWidth = 420f;
            BuildDetailsPanel(rightPanel, sprites, out detailIconSlotImage, out detailIcon, out detailName, out detailDescription, out detailRequirement, out costsRoot, out emptyState, out resultText, out craftButton, out craftButtonImage, out craftButtonLabel);
        }

        private static void BuildListPanel(RectTransform parent, IReadOnlyDictionary<string, Sprite> sprites, out Image listViewportImage, out RectTransform itemsRoot)
        {
            var title = CreateText("ListTitle", parent, "Crafts", 22f, FontStyles.Bold, TextAlignmentOptions.Left);
            title.rectTransform.anchorMin = new Vector2(0f, 1f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.pivot = new Vector2(0.5f, 1f);
            title.rectTransform.offsetMin = new Vector2(18f, -54f);
            title.rectTransform.offsetMax = new Vector2(-18f, -14f);

            var scrollRoot = CreateRect("ListScroll", parent);
            scrollRoot.anchorMin = Vector2.zero;
            scrollRoot.anchorMax = Vector2.one;
            scrollRoot.offsetMin = new Vector2(14f, 14f);
            scrollRoot.offsetMax = new Vector2(-14f, -62f);

            var scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = CreatePanel("Viewport", scrollRoot, sprites["Overlay"], out listViewportImage);
            Stretch(viewport);
            listViewportImage.type = Image.Type.Simple;
            listViewportImage.color = new Color(0f, 0f, 0f, 0.08f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            itemsRoot = CreateRect("Content", viewport);
            itemsRoot.anchorMin = new Vector2(0f, 1f);
            itemsRoot.anchorMax = new Vector2(1f, 1f);
            itemsRoot.pivot = new Vector2(0.5f, 1f);
            itemsRoot.anchoredPosition = Vector2.zero;
            itemsRoot.sizeDelta = Vector2.zero;

            var listLayout = itemsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 8f;
            listLayout.padding = new RectOffset(4, 4, 4, 4);
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            var contentSize = itemsRoot.gameObject.AddComponent<ContentSizeFitter>();
            contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = itemsRoot;
        }

        private static void BuildDetailsPanel(RectTransform parent, IReadOnlyDictionary<string, Sprite> sprites, out Image detailIconSlotImage, out Image detailIcon, out TextMeshProUGUI detailName, out TextMeshProUGUI detailDescription, out TextMeshProUGUI detailRequirement, out RectTransform costsRoot, out TextMeshProUGUI emptyState, out TextMeshProUGUI resultText, out Button craftButton, out Image craftButtonImage, out TextMeshProUGUI craftButtonLabel)
        {
            var top = CreateRect("DetailsTop", parent);
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = new Vector2(1f, 1f);
            top.pivot = new Vector2(0.5f, 1f);
            top.offsetMin = new Vector2(18f, -154f);
            top.offsetMax = new Vector2(-18f, -18f);

            var iconSlot = CreatePanel("DetailIconSlot", top, sprites["Slot"], out detailIconSlotImage);
            iconSlot.anchorMin = new Vector2(0f, 1f);
            iconSlot.anchorMax = new Vector2(0f, 1f);
            iconSlot.pivot = new Vector2(0f, 1f);
            iconSlot.anchoredPosition = Vector2.zero;
            iconSlot.sizeDelta = new Vector2(112f, 112f);

            detailIcon = CreateImage("DetailIcon", iconSlot, sprites["Placeholder"]);
            Stretch(detailIcon.rectTransform, 14f);
            detailIcon.preserveAspect = true;

            detailName = CreateText("DetailName", top, string.Empty, 26f, FontStyles.Bold, TextAlignmentOptions.Left);
            detailName.rectTransform.anchorMin = new Vector2(0f, 1f);
            detailName.rectTransform.anchorMax = new Vector2(1f, 1f);
            detailName.rectTransform.pivot = new Vector2(0f, 1f);
            detailName.rectTransform.offsetMin = new Vector2(132f, -48f);
            detailName.rectTransform.offsetMax = new Vector2(-6f, 0f);

            detailRequirement = CreateText("Requirement", top, string.Empty, 16f, FontStyles.Bold, TextAlignmentOptions.Left);
            detailRequirement.color = new Color32(163, 155, 135, 255);
            detailRequirement.rectTransform.anchorMin = new Vector2(0f, 1f);
            detailRequirement.rectTransform.anchorMax = new Vector2(1f, 1f);
            detailRequirement.rectTransform.pivot = new Vector2(0f, 1f);
            detailRequirement.rectTransform.offsetMin = new Vector2(132f, -86f);
            detailRequirement.rectTransform.offsetMax = new Vector2(-6f, -50f);

            detailDescription = CreateText("Description", parent, string.Empty, 17f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            detailDescription.enableWordWrapping = true;
            detailDescription.rectTransform.anchorMin = new Vector2(0f, 0.42f);
            detailDescription.rectTransform.anchorMax = new Vector2(1f, 1f);
            detailDescription.rectTransform.offsetMin = new Vector2(20f, 0f);
            detailDescription.rectTransform.offsetMax = new Vector2(-20f, -170f);

            var costsTitle = CreateText("CostsTitle", parent, "Cost", 20f, FontStyles.Bold, TextAlignmentOptions.Left);
            costsTitle.rectTransform.anchorMin = new Vector2(0f, 0.42f);
            costsTitle.rectTransform.anchorMax = new Vector2(1f, 0.42f);
            costsTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            costsTitle.rectTransform.offsetMin = new Vector2(20f, -36f);
            costsTitle.rectTransform.offsetMax = new Vector2(-20f, 0f);

            costsRoot = CreateRect("Costs", parent);
            costsRoot.anchorMin = new Vector2(0f, 0f);
            costsRoot.anchorMax = new Vector2(1f, 0.42f);
            costsRoot.offsetMin = new Vector2(20f, 92f);
            costsRoot.offsetMax = new Vector2(-20f, -42f);

            var costsLayout = costsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            costsLayout.spacing = 6f;
            costsLayout.childControlWidth = true;
            costsLayout.childControlHeight = false;
            costsLayout.childForceExpandWidth = true;
            costsLayout.childForceExpandHeight = false;

            resultText = CreateText("Result", parent, string.Empty, 15f, FontStyles.Bold, TextAlignmentOptions.Left);
            resultText.color = new Color32(238, 104, 76, 255);
            resultText.rectTransform.anchorMin = new Vector2(0f, 0f);
            resultText.rectTransform.anchorMax = new Vector2(1f, 0f);
            resultText.rectTransform.offsetMin = new Vector2(22f, 58f);
            resultText.rectTransform.offsetMax = new Vector2(-22f, 88f);

            craftButton = CreateButton("CraftButton", parent, sprites["CraftButton"], out craftButtonImage);
            var craftButtonRect = (RectTransform)craftButton.transform;
            craftButtonRect.anchorMin = new Vector2(1f, 0f);
            craftButtonRect.anchorMax = new Vector2(1f, 0f);
            craftButtonRect.pivot = new Vector2(1f, 0f);
            craftButtonRect.anchoredPosition = new Vector2(-20f, 18f);
            craftButtonRect.sizeDelta = new Vector2(210f, 52f);

            craftButtonLabel = CreateText("CraftButtonLabel", craftButtonRect, "Craft", 21f, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(craftButtonLabel.rectTransform);

            emptyState = CreateText("EmptyState", parent, string.Empty, 22f, FontStyles.Bold, TextAlignmentOptions.Center);
            emptyState.color = new Color32(163, 155, 135, 255);
            Stretch(emptyState.rectTransform, 24f);
            emptyState.gameObject.SetActive(false);
        }

        private static Sprite CreateFlatSprite(string fileName, Color32 color, float pixelsPerUnit)
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color32[16];

            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return SaveSprite(fileName, texture, pixelsPerUnit, Vector4.zero);
        }

        private static Sprite CreateFrameSprite(string fileName, Color32 fill, Color32 border, Color32 highlight, Color32 shadow)
        {
            const int size = 16;
            const int borderSize = 2;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var isBorder = x < borderSize || y < borderSize || x >= size - borderSize || y >= size - borderSize;
                    var color = isBorder ? border : fill;

                    if ((x == borderSize || y == size - borderSize - 1) && x >= borderSize && y >= borderSize)
                    {
                        color = shadow;
                    }

                    if ((x == size - borderSize - 1 || y == borderSize) && x >= borderSize && y >= borderSize)
                    {
                        color = highlight;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            return SaveSprite(fileName, texture, size, new Vector4(3f, 3f, 3f, 3f));
        }

        private static Sprite CreatePlaceholderSprite()
        {
            const int size = 16;

            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var clear = new Color32(0, 0, 0, 0);
            var top = new Color32(116, 107, 88, 255);
            var left = new Color32(84, 78, 66, 255);
            var right = new Color32(52, 49, 43, 255);
            var outline = new Color32(20, 18, 16, 255);

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            for (var y = 3; y < 13; y++)
            {
                for (var x = 3; x < 13; x++)
                {
                    var color = y > 9 ? top : x < 8 ? left : right;

                    if (x == 3 || x == 12 || y == 3 || y == 12)
                    {
                        color = outline;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();

            return SaveSprite("craft_placeholder_icon", texture, size, Vector4.zero);
        }

        private static Sprite SaveSprite(string fileName, Texture2D texture, float pixelsPerUnit, Vector4 border)
        {
            var path = $"{TextureDirectory}/{fileName}.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.spriteBorder = border;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static RectTransform CreatePanel(string name, Transform parent, Sprite sprite, out Image image)
        {
            var rect = CreateRect(name, parent);
            image = rect.gameObject.AddComponent<Image>();
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

        private static Button CreateButton(string name, Transform parent, Sprite sprite, out Image image)
        {
            var rect = CreatePanel(name, parent, sprite, out image);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            return button;
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
