using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MineArena.Levels;
using MineArena.Structs;
using MineArena.UI;
using MineArena.Windows;
using MineArena.Windows.SelectLevel;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static class LevelSelectionPrefabBuilder
    {
        public const string WindowPath = "Assets/DevotionSDK/Prefabs/UI/SelectLevelWindow.prefab";
        private const string Art = "Assets/Art/UI/Expedition/";
        private const string Parts = "Assets/Prefabs/Windows/SelectLevelWindow/";
        private static readonly Color Ink = Hex("514737"), Muted = Hex("85775E");
        private static TMP_FontAsset _font, _pixel;
        private static Dictionary<string, Sprite> _sprites;

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindowPath);
                if (prefab != null && prefab.GetComponent<LevelSelectionView>() == null) Build();
            };
        }

        [MenuItem("MineArena/UI/Rebuild Beige Level Selection")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Directory.CreateDirectory(Art);
            Directory.CreateDirectory(Parts);
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset");
            _pixel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset");
            _sprites = new Dictionary<string, Sprite>
            {
                ["panel"] = Frame("panel", "F3E7CF", "887457", "FFF7E7", "C6B18B"),
                ["inset"] = Frame("inset", "E5D5B8", "BAA580", "D0BA96", "F9EDD6"),
                ["card"] = Frame("card", "FAF1DE", "BBA780", "FFFCF2", "D6C29E"),
                ["selected"] = Frame("selected", "E5E8CB", "73814B", "F8F8DF", "A5AF7A"),
                ["button"] = Frame("button", "819454", "4F6136", "AFBF7B", "62753E"),
                ["slot"] = Frame("slot", "FFF5DF", "CEBC99", "FFFCF1", "E0CFAC"),
                ["lock"] = LockIcon(),
                ["brick"] = BrickTexture()
            };
            var landscape = ImportSprite(Art + "expedition-landscape-v2.png", false);
            var card = BuildCard();
            var chip = BuildChip();
            // Loading the existing asset keeps its root component fileID and UIManager references intact.
            var root = PrefabUtility.LoadPrefabContents(WindowPath);
            try
            {
                root.SetActive(false);
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                foreach (var old in root.GetComponents<LevelSelectionView>()) Object.DestroyImmediate(old);
                foreach (var old in root.GetComponents<LevelWindowFit>()) Object.DestroyImmediate(old);
                var rootRect = (RectTransform)root.transform;
                Stretch(rootRect);
                var dim = root.GetComponent<Image>() ?? root.AddComponent<Image>();
                dim.color = new Color(0.15f, 0.13f, 0.09f, 0.78f);
                dim.raycastTarget = true;
                var view = root.AddComponent<LevelSelectionView>();
                var panel = Panel("ExpeditionPanel", rootRect, _sprites["panel"]);
                panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
                panel.sizeDelta = new Vector2(1460, 940);
                panel.anchoredPosition = Vector2.zero;
                var outline = panel.gameObject.AddComponent<Outline>();
                outline.effectColor = Hex("65553D");
                outline.effectDistance = new Vector2(3, -3);
                outline.useGraphicAlpha = true;
                var shadow = panel.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.08f, 0.06f, 0.035f, 0.48f);
                shadow.effectDistance = new Vector2(8, -10);
                shadow.useGraphicAlpha = true;
                AddBrickTexture(panel, 0.65f);
                Set(root.AddComponent<LevelWindowFit>(), "panel", panel);
                Label("Eyebrow", panel, "MINE ARENA / ЭКСПЕДИЦИИ", 17, Muted, 38, 27, 1050, 25);
                Label("Title", panel, "Выбор уровня", 40, Ink, 36, 61, 1100, 53, true);
                var close = Button("Close", panel, _sprites["card"], 1362, 35, 60, 60);
                FillLabel(close.transform, "×", 38, Ink);
                var line = Panel("Divider", panel, null);
                line.GetComponent<Image>().color = Hex("D6C29E");
                Box(line, 36, 131, 1388, 2);

                var left = Panel("LevelListPanel", panel, _sprites["inset"]);
                Box(left, 34, 154, 428, 720);
                AddBrickTexture(left, 0.25f);
                Label("ListHeading", left, "КАРТА МИРА", 22, Ink, 22, 18, 360, 33, true);
                var counter = Label("Counter", left, "", 19, Muted, 22, 58, 360, 28);
                var list = Scroll("LevelScroll", left, 14, 100, 400, 599, out var cards);
                var vertical = cards.gameObject.AddComponent<VerticalLayoutGroup>();
                vertical.spacing = 12;
                vertical.padding = new RectOffset(2, 8, 2, 8);
                vertical.childControlWidth = true;
                vertical.childControlHeight = true;
                vertical.childForceExpandHeight = false;
                vertical.childForceExpandWidth = true;

                var right = Panel("DetailsPanel", panel, _sprites["card"]);
                Box(right, 482, 154, 942, 720);
                AddBrickTexture(right, 0.25f);
                var detail = Scroll("DetailScroll", right, 20, 18, 902, 586, out var content);
                var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(2, 18, 0, 8);
                layout.spacing = 8;
                layout.childControlHeight = true;
                layout.childControlWidth = true;
                layout.childForceExpandHeight = false;

                var hero = Panel("Landscape", content, _sprites["inset"]);
                Height(hero, 172);
                hero.gameObject.AddComponent<RectMask2D>();
                var art = Panel("VoxelPanorama", hero, landscape);
                Stretch(art);
                art.gameObject.AddComponent<AspectRatioFitter>().aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                art.GetComponent<AspectRatioFitter>().aspectRatio = landscape.rect.width / landscape.rect.height;
                var iconPlate = Panel("LevelPreviewPlate", hero, _sprites["card"]);
                Box(iconPlate, 681, 10, 160, 150);
                var icon = Panel("LevelPreview", iconPlate, null).GetComponent<Image>();
                Box((RectTransform)icon.transform, 8, 8, 144, 134);
                icon.preserveAspect = true;
                var title = FlowText("LevelTitle", content, "", 32, Ink, 45, true);
                var description = FlowText("Description", content, "", 20, Muted, 44);
                description.enableWordWrapping = true;
                var difficulty = FlowText("Difficulty", content, "", 19, Hex("66763F"), 30);
                FlowText("ResourcesHeading", content, "МОЖНО ДОБЫТЬ", 19, Muted, 29, true);
                var resources = Grid("Resources", content);
                var noResources = FlowText("NoResources", content, "На этом уровне нет добываемых ресурсов", 18, Muted, 30);
                FlowText("RewardsHeading", content, "НАГРАДА ЗА ПРОХОЖДЕНИЕ", 19, Muted, 29, true);
                var rewards = Grid("Rewards", content);
                var noRewards = FlowText("NoRewards", content, "Дополнительная награда не предусмотрена", 18, Muted, 30);

                var empty = Label("EmptyState", right, "Новые экспедиции появятся здесь", 28, Ink, 45, 170, 840, 180, true);
                empty.alignment = TextAlignmentOptions.Center;
                var access = Label("AccessHint", right, "", 18, Muted, 26, 617, 880, 29);
                var start = Button("StartExpedition", right, _sprites["button"], 24, 656, 894, 48);
                var startLabel = FillLabel(start.transform, "Начать экспедицию", 25, Hex("FFFAE9"), true);
                Label("Footer", panel, "Выберите маршрут. Добывайте ресурсы. Открывайте новые земли.", 19, Muted, 38, 889, 1380, 28);

                Set(view, "cardsRoot", cards, "cardPrefab", card, "resourcePrefab", chip,
                    "resourcesRoot", resources, "rewardsRoot", rewards,
                    "noResources", noResources.gameObject, "noRewards", noRewards.gameObject,
                    "details", detail.gameObject, "emptyState", empty.gameObject,
                    "levelTitle", title, "description", description, "difficulty", difficulty,
                    "access", access, "counter", counter, "startLabel", startLabel,
                    "levelIcon", icon, "startButton", start, "closeButton", close,
                    "listScroll", list, "detailScroll", detail);
                Set(root.GetComponent<SelectLevelWindow>(), "view", view);
                var gameConfig = AssetDatabase.FindAssets("t:GameConfig").Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<GameConfig>).FirstOrDefault(c => c.Levels != null && c.Levels.Count > 0);
                view.Refresh(gameConfig != null ? gameConfig.Levels : null, 0);
                root.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(root, WindowPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            RenderPreview();
            Debug.Log("[LevelSelection] Beige prefab built and preview saved to Documentation/level-selection.png");
        }

        [MenuItem("MineArena/UI/Install Pixel Level Icons")]
        public static void InstallPixelLevelIcons()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            string[] icons = { "village", "mine", "quarry", "forest" };
            string[] levels = { "Level1_Village", "Level3_Mine", "Level2_Sand", "Level4_Forest" };
            for (int i = 0; i < icons.Length; i++)
            {
                string path = Art + "Levels/level-" + icons[i] + ".png";
                ImportSprite(path, false);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.filterMode = FilterMode.Point;
                importer.maxTextureSize = 256;
                importer.SaveAndReimport();
                var level = AssetDatabase.LoadAssetAtPath<LevelConfig>("Assets/ScriptableObjects/Levels/" + levels[i] + ".asset");
                Set(level, "levelIcon", AssetDatabase.LoadAssetAtPath<Sprite>(path));
                AssetDatabase.SaveAssetIfDirty(level);
            }
            Build();
            Debug.Log("[LevelSelection] Four location icons installed.");
        }

        private static LevelSelectionCard BuildCard()
        {
            var root = Panel("LevelCard", null, _sprites["card"]);
            root.sizeDelta = new Vector2(378, 128);
            Height(root, 128);
            var button = root.gameObject.AddComponent<Button>();
            Style(button);
            var number = Label("Number", root, "01", 17, Muted, 16, 10, 40, 24, true);
            var icon = Panel("Icon", root, null).GetComponent<Image>();
            Box((RectTransform)icon.transform, 13, 36, 88, 78);
            icon.preserveAspect = true;
            var title = Label("Name", root, "Поселение", 23, Ink, 116, 19, 215, 58, true);
            title.enableWordWrapping = true;
            title.enableAutoSizing = true;
            title.fontSizeMin = 18;
            title.fontSizeMax = 23;
            var subtitle = Label("State", root, "Лёгкая", 18, Muted, 116, 83, 205, 28);
            var locked = Panel("Lock", root, _sprites["lock"]);
            Box(locked, 335, 90, 23, 26);
            locked.GetComponent<Image>().preserveAspect = true;
            var card = root.gameObject.AddComponent<LevelSelectionCard>();
            Set(card, "button", button, "frame", root.GetComponent<Image>(), "icon", icon,
                "lockedIcon", locked.gameObject, "title", title, "subtitle", subtitle,
                "number", number, "normalFrame", _sprites["card"], "selectedFrame", _sprites["selected"]);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, Parts + "LevelCard.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<LevelSelectionCard>();
        }

        private static LevelResourceChip BuildChip()
        {
            var root = Panel("ResourceChip", null, _sprites["slot"]);
            root.sizeDelta = new Vector2(205, 68);
            var icon = Panel("Icon", root, null).GetComponent<Image>();
            Box((RectTransform)icon.transform, 8, 10, 45, 45);
            icon.preserveAspect = true;
            var name = Label("Name", root, "Ресурс", 17, Ink, 59, 8, 137, 25);
            name.enableAutoSizing = true;
            name.fontSizeMin = 12;
            name.fontSizeMax = 17;
            var amount = Label("Amount", root, "+10", 20, Hex("657540"), 59, 34, 132, 26, true);
            var chip = root.gameObject.AddComponent<LevelResourceChip>();
            var blockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/ResourceIcon.prefab");
            var block = (GameObject)PrefabUtility.InstantiatePrefab(blockPrefab, root);
            block.name = "BlockIcon";
            var blockRect = (RectTransform)block.transform;
            blockRect.anchorMin = blockRect.anchorMax = new Vector2(0, 1);
            blockRect.pivot = new Vector2(0.5f, 0.5f);
            blockRect.anchoredPosition = new Vector2(31, -35);
            blockRect.localScale = Vector3.one * 0.62f;
            foreach (var face in block.GetComponentsInChildren<Image>()) face.raycastTarget = false;
            block.SetActive(false);
            Set(chip, "icon", icon, "blockIcon", block.GetComponent<ResourceIcon>(), "label", name, "amount", amount);
            chip.FitContents();
            var prefab = PrefabUtility.SaveAsPrefabAsset(root.gameObject, Parts + "ResourceChip.prefab");
            Object.DestroyImmediate(root.gameObject);
            return prefab.GetComponent<LevelResourceChip>();
        }

        private static RectTransform Grid(string name, Transform parent)
        {
            var root = Rect(name, parent);
            var grid = root.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(205, 68);
            grid.spacing = new Vector2(10, 10);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            return root;
        }

        private static ScrollRect Scroll(string name, Transform parent, float x, float y, float w, float h, out RectTransform content)
        {
            var root = Rect(name, parent);
            Box(root, x, y, w, h);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            var viewport = Panel("Viewport", root, null);
            Stretch(viewport);
            viewport.offsetMax = new Vector2(-16, 0);
            viewport.GetComponent<Image>().color = Color.clear;
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 38;
            var track = Panel("Scrollbar", root, _sprites["inset"]);
            Box(track, w - 10, 0, 10, h);
            var handle = Panel("Handle", track, _sprites["button"]);
            Stretch(handle);
            track.GetComponent<Image>().raycastTarget = true;
            handle.GetComponent<Image>().raycastTarget = true;
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle;
            bar.targetGraphic = handle.GetComponent<Image>();
            bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return scroll;
        }

        private static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }
        private static RectTransform Panel(string name, Transform parent, Sprite sprite)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;
            return rect;
        }
        private static TMP_Text Label(string name, Transform parent, string text, int size, Color color,
            float x, float y, float w, float h, bool pixel = false)
        {
            var rect = Rect(name, parent);
            Box(rect, x, y, w, h);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = pixel ? _pixel : _font;
            label.fontSize = size;
            label.color = color;
            label.text = text;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            return label;
        }
        private static TMP_Text FlowText(string name, Transform parent, string text, int size, Color color, float height, bool pixel = false)
        {
            var label = Label(name, parent, text, size, color, 0, 0, 800, height, pixel);
            Height((RectTransform)label.transform, height);
            return label;
        }
        private static TMP_Text FillLabel(Transform parent, string text, int size, Color color, bool pixel = false)
        {
            var label = Label("Label", parent, text, size, color, 0, 0, 0, 0, pixel);
            Stretch((RectTransform)label.transform);
            label.alignment = TextAlignmentOptions.Center;
            return label;
        }
        private static Button Button(string name, Transform parent, Sprite sprite, float x, float y, float w, float h)
        {
            var rect = Panel(name, parent, sprite);
            Box(rect, x, y, w, h);
            var button = rect.gameObject.AddComponent<Button>();
            Style(button);
            return button;
        }
        private static void Style(Button button)
        {
            button.targetGraphic = button.GetComponent<Image>();
            button.targetGraphic.raycastTarget = true;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.04f, 1.04f, 1.04f);
            colors.pressedColor = new Color(0.83f, 0.83f, 0.78f);
            colors.disabledColor = new Color(0.73f, 0.70f, 0.62f, 0.7f);
            button.colors = colors;
        }
        private static void Height(RectTransform rect, float height)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = height;
        }
        private static void Box(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out var c); return c; }
        private static void Set(Object target, params object[] values)
        {
            var so = new SerializedObject(target);
            for (int i = 0; i < values.Length; i += 2)
                so.FindProperty((string)values[i]).objectReferenceValue = (Object)values[i + 1];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static Sprite Frame(string name, string fill, string border, string light, string shadow)
        {
            var texture = new Texture2D(24, 24, TextureFormat.RGBA32, false);
            for (int y = 0; y < 24; y++)
                for (int x = 0; x < 24; x++)
                {
                    int edge = Mathf.Min(x, y, 23 - x, 23 - y);
                    var color = edge < 2 ? Hex(border) : edge < 4 ? (x < 4 || y > 19 ? Hex(light) : Hex(shadow)) : Hex(fill);
                    if ((x < 2 || x > 21) && (y < 2 || y > 21)) color = Color.clear;
                    texture.SetPixel(x, y, color);
                }
            texture.Apply();
            var path = Art + "beige-" + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            return ImportSprite(path, true);
        }
        private static Sprite ImportSprite(string path, bool sliced)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.spriteBorder = sliced ? Vector4.one * 6 : Vector4.zero;
            importer.filterMode = sliced ? FilterMode.Point : FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite LockIcon()
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    bool body = x >= 2 && x <= 13 && y >= 1 && y <= 9;
                    bool shackle = x >= 4 && x <= 11 && y >= 9 && y <= 14 &&
                        (x <= 5 || x >= 10 || y >= 13);
                    bool keyhole = x >= 7 && x <= 8 && y >= 4 && y <= 6;
                    texture.SetPixel(x, y, keyhole || shackle ? Hex("68583F") :
                        body ? (y == 9 ? Hex("E8D6A9") : Hex("A88F5D")) : Color.clear);
                }
            texture.Apply();
            string path = Art + "beige-lock.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            var sprite = ImportSprite(path, false);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Sprite BrickTexture()
        {
            // Seamless, low-contrast pixel masonry, authored alongside the UI frame sprites.
            var texture = new Texture2D(128, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 128; x++)
                {
                    int row = y / 32;
                    int bx = (x + row * 32) % 64;
                    int by = y % 32;
                    Color color = Hex("F1E7D3");
                    if (by < 2 || bx < 2) color = Hex("DFD3BB");
                    else if (by == 2 || bx == 2) color = Hex("FBF4E5");
                    else if (by == 31 || bx == 63) color = Hex("E9DFC9");
                    else if ((x / 4 * 17 + y / 4 * 31) % 23 == 0) color = Hex("EDE2CD");
                    texture.SetPixel(x, y, color);
                }
            texture.Apply();
            string path = Art + "beige-brick.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            ImportSprite(path, false);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void AddBrickTexture(RectTransform parent, float opacity)
        {
            var rect = Panel("PaleBrickTexture", parent, _sprites["brick"]);
            Stretch(rect);
            rect.offsetMin = Vector2.one * 6;
            rect.offsetMax = Vector2.one * -6;
            var image = rect.GetComponent<Image>();
            image.type = Image.Type.Tiled;
            image.color = new Color(1, 1, 1, opacity);
        }

        [MenuItem("MineArena/UI/Render Level Selection Preview")]
        public static void RenderPreview()
        {
            RenderPreviewAt(0, 1920, 1080, "level-selection.png");
            RenderPreviewAt(1, 1920, 1080, "level-selection-locked.png");
            RenderPreviewAt(0, 1280, 720, "level-selection-1280.png");
            RenderPreviewAt(0, 1920, 1080, "level-selection-icons.png", int.MaxValue);
        }

        private static void RenderPreviewAt(int selected, int width, int height, string fileName, int previewUnlocked = 0)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var previous = RenderTexture.active;
            RenderTexture target = null;
            Texture2D capture = null;
            try
            {
                var cameraObject = new GameObject("Preview Camera", typeof(Camera));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Hex("BCB396");
                camera.orthographic = true;
                camera.orthographicSize = height / 2f;
                camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                camera.transform.position = new Vector3(0, 0, -100);
                var canvasObject = new GameObject("Preview Canvas", typeof(Canvas), typeof(CanvasScaler));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvasObject, scene);
                var canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = camera;
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(width, height);
                var window = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WindowPath), scene);
                window.transform.SetParent(canvas.transform, false);
                var gameConfig = AssetDatabase.FindAssets("t:GameConfig").Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<GameConfig>).FirstOrDefault(c => c.Levels != null && c.Levels.Count > 0);
                window.GetComponent<LevelSelectionView>().Refresh(gameConfig != null ? gameConfig.Levels : null, previewUnlocked);
                window.GetComponent<LevelSelectionView>().Select(selected);
                window.GetComponent<LevelWindowFit>().SendMessage("LateUpdate");
                target = new RenderTexture(width, height, 24);
                camera.targetTexture = target;
                Canvas.ForceUpdateCanvases();
                foreach (var rect in window.GetComponentsInChildren<RectTransform>()) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                capture = new Texture2D(width, height, TextureFormat.RGB24, false);
                capture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                capture.Apply();
                Directory.CreateDirectory("Documentation");
                File.WriteAllBytes("Documentation/" + fileName, capture.EncodeToPNG());
                camera.targetTexture = null;
            }
            finally
            {
                RenderTexture.active = previous;
                if (target != null) Object.DestroyImmediate(target);
                if (capture != null) Object.DestroyImmediate(capture);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
