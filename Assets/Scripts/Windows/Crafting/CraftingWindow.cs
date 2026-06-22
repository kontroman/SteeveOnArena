using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Devotion.SDK.Base;
using MineArena.Basics;
using MineArena.Buildings;
using MineArena.Items;
using MineArena.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MineArena.Windows.Crafting
{
    public class CraftingWindow : BaseWindow
    {
        private const string PrefabResourcePath = "Prefabs/Windows/Crafting/CraftingWindow";
        private const string TabPrefabResourcePath = "Prefabs/Windows/Crafting/CraftingTabButton";
        private const string ItemPrefabResourcePath = "Prefabs/Windows/Crafting/CraftingItemView";
        private const string ResourceIconPrefabResourcePath = "Prefabs/Windows/ResourceIcon";
        private const float OpenAnimationDuration = 0.12f;
        private const float ClosedScale = 0.92f;
        private const float OpenScale = 1f;

        private static readonly Vector2 AnchorStretchMin = Vector2.zero;
        private static readonly Vector2 AnchorStretchMax = Vector2.one;

        private readonly ProjectCraftingAdapter _adapter = new ProjectCraftingAdapter();
        private readonly List<TabView> _tabs = new List<TabView>();
        private readonly List<ItemView> _items = new List<ItemView>();

        [Header("Prefab Layout")]
        [SerializeField] private Image _rootImage;
        [SerializeField] private Image _windowPanelImage;
        [SerializeField] private Image _tabsPanelImage;
        [SerializeField] private Image _leftPanelImage;
        [SerializeField] private Image _rightPanelImage;
        [SerializeField] private Image _listViewportImage;
        [SerializeField] private Image _detailIconSlotImage;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _windowPanel;
        [SerializeField] private RectTransform _tabsRoot;
        [SerializeField] private RectTransform _itemsRoot;
        [SerializeField] private RectTransform _costsRoot;
        [SerializeField] private Image _detailIcon;
        [SerializeField] private TextMeshProUGUI _detailName;
        [SerializeField] private TextMeshProUGUI _detailDescription;
        [SerializeField] private TextMeshProUGUI _detailRequirement;
        [SerializeField] private TextMeshProUGUI _emptyState;
        [SerializeField] private TextMeshProUGUI _resultText;
        [SerializeField] private Button _craftButton;
        [SerializeField] private Image _craftButtonImage;
        [SerializeField] private TextMeshProUGUI _craftButtonLabel;

        [Header("Element Prefabs")]
        [SerializeField] private CraftingTabButton _tabPrefab;
        [SerializeField] private CraftingItemView _itemPrefab;
        [SerializeField] private ResourceIcon _resourceIconPrefab;
        [SerializeField] private ResourceIcon _detailResourceIcon;

        [Header("Skin Sprites")]
        [SerializeField] private Sprite _overlaySprite;
        [SerializeField] private Sprite _panelSprite;
        [SerializeField] private Sprite _panelInsetSprite;
        [SerializeField] private Sprite _slotSprite;
        [SerializeField] private Sprite _slotSelectedSprite;
        [SerializeField] private Sprite _buttonSprite;
        [SerializeField] private Sprite _buttonSelectedSprite;
        [SerializeField] private Sprite _buttonDisabledSprite;
        [SerializeField] private Sprite _craftButtonSprite;
        [SerializeField] private Sprite _placeholderIconSprite;

        private CraftingPixelStyle _style;
        private IReadOnlyList<CraftingCategory> _categories = Array.Empty<CraftingCategory>();
        private CraftingCategory _selectedCategory;
        private CraftingRecipeEntry _selectedRecipe;
        private BuildingConfig _pendingInitialBuilding;
        private Coroutine _openAnimation;
        private bool _layoutReady;
        private bool _usesPrefabLayout;

        public static CraftingWindow Open(BuildingConfig initialBuilding = null)
        {
            var window = FindExistingWindow();

            if (window == null)
            {
                window = CreateRuntimeWindow();
            }

            if (window == null)
                return null;

            var wasActive = window.gameObject.activeInHierarchy;
            window._pendingInitialBuilding = initialBuilding;

            if (!wasActive)
            {
                window.gameObject.SetActive(true);
            }
            else
            {
                window.Initialize(initialBuilding);
            }

            return window;
        }

        public static void Toggle()
        {
            var window = FindExistingWindow();

            if (window != null && window.gameObject.activeInHierarchy)
            {
                window.CloseWindow();
                return;
            }

            Open();
        }

        private void OnEnable()
        {
            _adapter.Connect();
            _adapter.InventoryChanged += HandleInventoryChanged;

            EnsureLayout();
            Initialize(_pendingInitialBuilding);
            PlayOpenAnimation();
        }

        private void OnDisable()
        {
            _adapter.InventoryChanged -= HandleInventoryChanged;
            _adapter.Disconnect();

            if (_openAnimation != null)
            {
                StopCoroutine(_openAnimation);
                _openAnimation = null;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseWindow();
            }
        }

        public void Initialize(BuildingConfig initialBuilding)
        {
            _pendingInitialBuilding = initialBuilding;

            if (!isActiveAndEnabled)
                return;

            EnsureLayout();

            _categories = _adapter.BuildCatalog() ?? Array.Empty<CraftingCategory>();
            _selectedCategory = null;
            _selectedRecipe = null;
            _resultText.text = string.Empty;

            RebuildTabs();

            if (_categories.Count == 0)
            {
                ShowEmptyState("Нет доступных рецептов");
                return;
            }

            var category = ResolveInitialCategory(initialBuilding) ?? _categories[0];
            SelectCategory(category, true);
            _pendingInitialBuilding = null;
        }

        public override void CloseWindow()
        {
            gameObject.SetActive(false);
        }

        private void EnsureLayout()
        {
            if (_layoutReady)
                return;

            _style = CraftingPixelStyle.Create(
                _overlaySprite,
                _panelSprite,
                _panelInsetSprite,
                _slotSprite,
                _slotSelectedSprite,
                _buttonSprite,
                _buttonSelectedSprite,
                _buttonDisabledSprite,
                _craftButtonSprite,
                _placeholderIconSprite);

            _canvasGroup = _canvasGroup != null ? _canvasGroup : GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            _rootImage = _rootImage != null ? _rootImage : GetComponent<Image>();
            _usesPrefabLayout = HasPrefabLayout();
            EnsureElementPrefabs();

            if (_usesPrefabLayout)
            {
                _craftButton.onClick.RemoveListener(CraftSelected);
                _craftButton.onClick.AddListener(CraftSelected);
                _layoutReady = true;
                return;
            }

            _rootImage = _rootImage != null ? _rootImage : gameObject.AddComponent<Image>();
            _rootImage.sprite = _style.Overlay;
            _rootImage.type = Image.Type.Simple;
            _rootImage.color = Color.white;
            _rootImage.raycastTarget = true;

            var rootRect = (RectTransform)transform;
            Stretch(rootRect);

            ClearChildren(transform);

            _windowPanel = CreatePanel("PixelCraftPanel", transform, _style.Panel);
            _windowPanelImage = _windowPanel.GetComponent<Image>();
            _windowPanel.anchorMin = new Vector2(0.08f, 0.08f);
            _windowPanel.anchorMax = new Vector2(0.92f, 0.92f);
            _windowPanel.offsetMin = Vector2.zero;
            _windowPanel.offsetMax = Vector2.zero;

            BuildHeader(_windowPanel);
            BuildBody(_windowPanel);

            _layoutReady = true;
        }

        private bool HasPrefabLayout()
        {
            return _windowPanel != null &&
                   _tabsRoot != null &&
                   _itemsRoot != null &&
                   _costsRoot != null &&
                   _detailIcon != null &&
                   _detailName != null &&
                   _detailDescription != null &&
                   _detailRequirement != null &&
                   _emptyState != null &&
                   _resultText != null &&
                   _craftButton != null &&
                   _craftButtonLabel != null;
        }

        private void ApplyPrefabSkin()
        {
            SetSlicedSprite(_windowPanelImage, _style.Panel);
            SetSlicedSprite(_tabsPanelImage, _style.PanelInset);
            SetSlicedSprite(_leftPanelImage, _style.PanelInset);
            SetSlicedSprite(_rightPanelImage, _style.PanelInset);
            SetSlicedSprite(_detailIconSlotImage, _style.Slot);

            if (_listViewportImage != null)
            {
                _listViewportImage.sprite = _style.Overlay;
                _listViewportImage.type = Image.Type.Simple;
            }

            if (_detailIcon.sprite == null)
            {
                _detailIcon.sprite = _style.PlaceholderIcon;
            }

            if (_craftButtonImage == null && _craftButton.targetGraphic is Image targetImage)
            {
                _craftButtonImage = targetImage;
            }

            SetSlicedSprite(_craftButtonImage, _style.CraftButton);
        }

        private static void SetSlicedSprite(Image image, Sprite sprite)
        {
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        private void BuildHeader(RectTransform parent)
        {
            var header = CreateRect("Header", parent);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(22f, -86f);
            header.offsetMax = new Vector2(-22f, -18f);

            var title = CreateText("Title", header, "Craft / Shop", 28, FontStyles.Bold, TextAlignmentOptions.Left);
            title.rectTransform.anchorMin = new Vector2(0f, 0f);
            title.rectTransform.anchorMax = new Vector2(0f, 1f);
            title.rectTransform.sizeDelta = new Vector2(190f, 0f);
            title.rectTransform.anchoredPosition = Vector2.zero;

            var tabsViewport = CreatePanel("TabsPanel", header, _style.PanelInset);
            _tabsPanelImage = tabsViewport.GetComponent<Image>();
            tabsViewport.anchorMin = new Vector2(0f, 0f);
            tabsViewport.anchorMax = new Vector2(1f, 1f);
            tabsViewport.offsetMin = new Vector2(205f, 0f);
            tabsViewport.offsetMax = Vector2.zero;

            _tabsRoot = CreateRect("Tabs", tabsViewport);
            Stretch(_tabsRoot, 10f, 8f, 10f, 8f);

            var layout = _tabsRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        private void BuildBody(RectTransform parent)
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

            var leftPanel = CreatePanel("RecipeListPanel", body, _style.PanelInset);
            _leftPanelImage = leftPanel.GetComponent<Image>();
            var leftLayout = leftPanel.gameObject.AddComponent<LayoutElement>();
            leftLayout.flexibleWidth = 0.42f;
            leftLayout.minWidth = 310f;

            BuildListPanel(leftPanel);

            var rightPanel = CreatePanel("DetailsPanel", body, _style.PanelInset);
            _rightPanelImage = rightPanel.GetComponent<Image>();
            var rightLayout = rightPanel.gameObject.AddComponent<LayoutElement>();
            rightLayout.flexibleWidth = 0.58f;
            rightLayout.minWidth = 420f;

            BuildDetailsPanel(rightPanel);
        }

        private void BuildListPanel(RectTransform parent)
        {
            var title = CreateText("ListTitle", parent, "Crafts", 22, FontStyles.Bold, TextAlignmentOptions.Left);
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

            var viewport = CreatePanel("Viewport", scrollRoot, _style.Overlay);
            Stretch(viewport);
            var viewportImage = viewport.GetComponent<Image>();
            _listViewportImage = viewportImage;
            viewportImage.color = new Color(0f, 0f, 0f, 0.08f);
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            _itemsRoot = CreateRect("Content", viewport);
            _itemsRoot.anchorMin = new Vector2(0f, 1f);
            _itemsRoot.anchorMax = new Vector2(1f, 1f);
            _itemsRoot.pivot = new Vector2(0.5f, 1f);
            _itemsRoot.anchoredPosition = Vector2.zero;
            _itemsRoot.sizeDelta = Vector2.zero;

            var listLayout = _itemsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 8f;
            listLayout.padding = new RectOffset(4, 4, 4, 4);
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;

            var contentSize = _itemsRoot.gameObject.AddComponent<ContentSizeFitter>();
            contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport;
            scrollRect.content = _itemsRoot;
        }

        private void BuildDetailsPanel(RectTransform parent)
        {
            var top = CreateRect("DetailsTop", parent);
            top.anchorMin = new Vector2(0f, 1f);
            top.anchorMax = new Vector2(1f, 1f);
            top.pivot = new Vector2(0.5f, 1f);
            top.offsetMin = new Vector2(18f, -154f);
            top.offsetMax = new Vector2(-18f, -18f);

            var iconSlot = CreatePanel("DetailIconSlot", top, _style.Slot);
            _detailIconSlotImage = iconSlot.GetComponent<Image>();
            iconSlot.anchorMin = new Vector2(0f, 1f);
            iconSlot.anchorMax = new Vector2(0f, 1f);
            iconSlot.pivot = new Vector2(0f, 1f);
            iconSlot.anchoredPosition = Vector2.zero;
            iconSlot.sizeDelta = new Vector2(112f, 112f);

            _detailIcon = CreateImage("DetailIcon", iconSlot, _style.PlaceholderIcon);
            Stretch(_detailIcon.rectTransform, 14f);
            _detailIcon.preserveAspect = true;

            _detailName = CreateText("DetailName", top, string.Empty, 26, FontStyles.Bold, TextAlignmentOptions.Left);
            _detailName.rectTransform.anchorMin = new Vector2(0f, 1f);
            _detailName.rectTransform.anchorMax = new Vector2(1f, 1f);
            _detailName.rectTransform.pivot = new Vector2(0f, 1f);
            _detailName.rectTransform.offsetMin = new Vector2(132f, -48f);
            _detailName.rectTransform.offsetMax = new Vector2(-6f, 0f);

            _detailRequirement = CreateText("Requirement", top, string.Empty, 16, FontStyles.Bold, TextAlignmentOptions.Left);
            _detailRequirement.color = _style.MutedText;
            _detailRequirement.rectTransform.anchorMin = new Vector2(0f, 1f);
            _detailRequirement.rectTransform.anchorMax = new Vector2(1f, 1f);
            _detailRequirement.rectTransform.pivot = new Vector2(0f, 1f);
            _detailRequirement.rectTransform.offsetMin = new Vector2(132f, -86f);
            _detailRequirement.rectTransform.offsetMax = new Vector2(-6f, -50f);

            _detailDescription = CreateText("Description", parent, string.Empty, 17, FontStyles.Normal, TextAlignmentOptions.TopLeft);
            _detailDescription.color = _style.Text;
            _detailDescription.enableWordWrapping = true;
            _detailDescription.rectTransform.anchorMin = new Vector2(0f, 0.42f);
            _detailDescription.rectTransform.anchorMax = new Vector2(1f, 1f);
            _detailDescription.rectTransform.offsetMin = new Vector2(20f, 0f);
            _detailDescription.rectTransform.offsetMax = new Vector2(-20f, -170f);

            var costsTitle = CreateText("CostsTitle", parent, "Стоимость", 20, FontStyles.Bold, TextAlignmentOptions.Left);
            costsTitle.rectTransform.anchorMin = new Vector2(0f, 0.42f);
            costsTitle.rectTransform.anchorMax = new Vector2(1f, 0.42f);
            costsTitle.rectTransform.pivot = new Vector2(0.5f, 1f);
            costsTitle.rectTransform.offsetMin = new Vector2(20f, -36f);
            costsTitle.rectTransform.offsetMax = new Vector2(-20f, 0f);

            _costsRoot = CreateRect("Costs", parent);
            _costsRoot.anchorMin = new Vector2(0f, 0f);
            _costsRoot.anchorMax = new Vector2(1f, 0.42f);
            _costsRoot.offsetMin = new Vector2(20f, 92f);
            _costsRoot.offsetMax = new Vector2(-20f, -42f);

            var costsLayout = _costsRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            costsLayout.spacing = 6f;
            costsLayout.childControlWidth = true;
            costsLayout.childControlHeight = false;
            costsLayout.childForceExpandWidth = true;
            costsLayout.childForceExpandHeight = false;

            _resultText = CreateText("Result", parent, string.Empty, 15, FontStyles.Bold, TextAlignmentOptions.Left);
            _resultText.color = _style.WarningText;
            _resultText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _resultText.rectTransform.anchorMax = new Vector2(1f, 0f);
            _resultText.rectTransform.offsetMin = new Vector2(22f, 58f);
            _resultText.rectTransform.offsetMax = new Vector2(-22f, 88f);

            _craftButton = CreateButton("CraftButton", parent, _style.CraftButton, out _craftButtonImage);
            _craftButton.transform.SetAsLastSibling();
            _craftButton.onClick.AddListener(CraftSelected);
            var craftButtonRect = (RectTransform)_craftButton.transform;
            craftButtonRect.anchorMin = new Vector2(1f, 0f);
            craftButtonRect.anchorMax = new Vector2(1f, 0f);
            craftButtonRect.pivot = new Vector2(1f, 0f);
            craftButtonRect.anchoredPosition = new Vector2(-20f, 18f);
            craftButtonRect.sizeDelta = new Vector2(210f, 52f);

            _craftButtonLabel = CreateText("CraftButtonLabel", craftButtonRect, "Craft", 21, FontStyles.Bold, TextAlignmentOptions.Center);
            _craftButtonLabel.color = _style.Text;
            Stretch(_craftButtonLabel.rectTransform);

            _emptyState = CreateText("EmptyState", parent, string.Empty, 22, FontStyles.Bold, TextAlignmentOptions.Center);
            _emptyState.color = _style.MutedText;
            Stretch(_emptyState.rectTransform, 24f);
            _emptyState.gameObject.SetActive(false);
        }

        private void RebuildTabs()
        {
            ClearTabs();

            if (_categories == null || _categories.Count == 0)
                return;

            foreach (var category in _categories)
            {
                if (_tabPrefab != null)
                {
                    var tabButton = Instantiate(_tabPrefab, _tabsRoot);
                    tabButton.name = $"Tab_{category.DisplayName}";
                    tabButton.Setup(category.DisplayName, () => SelectCategory(category, false));
                    _tabs.Add(new TabView(category, tabButton));
                    continue;
                }

                var button = CreateButton($"Tab_{category.DisplayName}", _tabsRoot, _style.Button, out var image);
                var rect = (RectTransform)button.transform;
                rect.sizeDelta = new Vector2(156f, 0f);

                var label = CreateText("Label", rect, category.DisplayName, 16, FontStyles.Bold, TextAlignmentOptions.Center);
                label.enableAutoSizing = true;
                label.fontSizeMin = 10f;
                label.fontSizeMax = 16f;
                Stretch(label.rectTransform, 8f);

                var tab = new TabView(category, button, image, label);
                button.onClick.AddListener(() => SelectCategory(category, false));
                _tabs.Add(tab);
            }
        }

        private void RebuildItems()
        {
            ClearItems();

            if (_selectedCategory == null || _selectedCategory.Recipes.Count == 0)
            {
                ShowEmptyState("В этой категории нет рецептов");
                return;
            }

            _emptyState.gameObject.SetActive(false);

            foreach (var recipe in _selectedCategory.Recipes)
            {
                var view = CreateItemView(recipe);
                _items.Add(view);
            }
        }

        private ItemView CreateItemView(CraftingRecipeEntry recipe)
        {
            if (_itemPrefab != null)
            {
                var itemView = Instantiate(_itemPrefab, _itemsRoot);
                itemView.name = $"Item_{recipe.DisplayName}";
                itemView.Setup(recipe.Item, recipe.Icon != null ? recipe.Icon : _style.PlaceholderIcon, recipe.DisplayName, () => SelectRecipe(recipe));
                return new ItemView(recipe, itemView);
            }

            var button = CreateButton($"Item_{recipe.DisplayName}", _itemsRoot, _style.Slot, out var background);
            var rect = (RectTransform)button.transform;
            rect.sizeDelta = new Vector2(0f, 74f);
            button.onClick.AddListener(() => SelectRecipe(recipe));

            var canvasGroup = button.gameObject.AddComponent<CanvasGroup>();

            var layout = button.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 12, 9, 9);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var iconSlot = CreatePanel("IconSlot", rect, _style.PanelInset);
            iconSlot.sizeDelta = new Vector2(54f, 54f);
            iconSlot.gameObject.AddComponent<LayoutElement>().preferredWidth = 54f;

            var icon = CreateImage("Icon", iconSlot, recipe.Icon != null ? recipe.Icon : _style.PlaceholderIcon);
            Stretch(icon.rectTransform, 8f);
            icon.preserveAspect = true;
            var resourceIcon = CreateResourceIcon(iconSlot, "BlockIcon");
            SetItemIcon(icon, resourceIcon, recipe.Item, _style.PlaceholderIcon);

            var textRoot = CreateRect("Text", rect);
            var textLayout = textRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 1f;
            textLayout.childAlignment = TextAnchor.MiddleLeft;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textRoot.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var name = CreateText("Name", textRoot, recipe.DisplayName, 17, FontStyles.Bold, TextAlignmentOptions.Left);
            name.enableAutoSizing = true;
            name.fontSizeMin = 10f;
            name.fontSizeMax = 17f;

            var meta = CreateText("Meta", textRoot, string.Empty, 13, FontStyles.Normal, TextAlignmentOptions.Left);
            meta.color = _style.MutedText;

            return new ItemView(recipe, button, background, canvasGroup, name, meta);
        }

        private void SelectCategory(CraftingCategory category, bool selectFirstRecipe)
        {
            _selectedCategory = category;
            _resultText.text = string.Empty;

            foreach (var tab in _tabs)
            {
                var selected = tab.Category == category;

                if (tab.View != null)
                {
                    tab.View.SetSelected(selected);
                    continue;
                }

                if (tab.Background != null)
                {
                    tab.Background.sprite = selected ? _style.ButtonSelected : _style.Button;
                }

                if (tab.Label != null)
                {
                    tab.Label.color = selected ? _style.Text : _style.MutedText;
                }
            }

            RebuildItems();

            var recipe = selectFirstRecipe ? category?.Recipes.FirstOrDefault() : _selectedRecipe;

            if (recipe == null || recipe.Category != category)
            {
                recipe = category?.Recipes.FirstOrDefault();
            }

            SelectRecipe(recipe);
        }

        private void SelectRecipe(CraftingRecipeEntry recipe)
        {
            _selectedRecipe = recipe;
            _resultText.text = string.Empty;
            RefreshItemStates();
            RefreshDetails();
        }

        private void RefreshItemStates()
        {
            foreach (var view in _items)
            {
                var selected = view.Recipe == _selectedRecipe;
                var unlocked = _adapter.IsUnlocked(view.Recipe);

                if (view.Component != null)
                {
                    view.Component.SetSelected(selected);
                    view.Component.SetUnavailable(!unlocked);
                    view.Component.SetNameColor(unlocked ? _style.Text : _style.MutedText);
                }
                else
                {
                    view.Background.sprite = selected ? _style.SlotSelected : _style.Slot;
                    view.CanvasGroup.alpha = unlocked ? 1f : 0.48f;
                    view.Name.color = unlocked ? _style.Text : _style.MutedText;
                }

                if (view.Recipe.HasBuildingRequirement && !unlocked)
                {
                    view.Meta.text = $"Требуется {view.Recipe.SourceBuilding.BuildingName} Lv {view.Recipe.RequiredBuildingLevel}";
                    view.Meta.color = _style.WarningText;
                }
                else if (view.Recipe.HasBuildingRequirement)
                {
                    view.Meta.text = $"{view.Recipe.SourceBuilding.BuildingName} Lv {view.Recipe.RequiredBuildingLevel}";
                    view.Meta.color = _style.MutedText;
                }
                else
                {
                    view.Meta.text = view.Recipe.Category.DisplayName;
                    view.Meta.color = _style.MutedText;
                }
            }
        }

        private void RefreshDetails()
        {
            ClearCosts();

            if (_selectedRecipe == null)
            {
                ShowEmptyState("Выберите рецепт");
                return;
            }

            _emptyState.gameObject.SetActive(false);

            SetItemIcon(_detailIcon, ResolveDetailResourceIcon(), _selectedRecipe.Item, _style.PlaceholderIcon);
            _detailName.text = _selectedRecipe.DisplayName;
            _detailDescription.text = _adapter.GetDescription(_selectedRecipe);

            var unlocked = _adapter.IsUnlocked(_selectedRecipe);
            var canCraft = _adapter.CanCraft(_selectedRecipe);

            if (_selectedRecipe.HasBuildingRequirement)
            {
                var currentLevel = _adapter.GetBuildingLevel(_selectedRecipe.SourceBuilding);
                _detailRequirement.text = $"{_selectedRecipe.SourceBuilding.BuildingName}: {currentLevel}/{_selectedRecipe.RequiredBuildingLevel}";
                _detailRequirement.color = unlocked ? _style.SuccessText : _style.WarningText;
            }
            else
            {
                _detailRequirement.text = _selectedRecipe.Category.DisplayName;
                _detailRequirement.color = _style.MutedText;
            }

            RebuildCosts(_selectedRecipe.Costs);

            _craftButton.interactable = canCraft;
            if (!_usesPrefabLayout && _craftButtonImage != null)
            {
                _craftButtonImage.sprite = canCraft ? _style.CraftButton : _style.ButtonDisabled;
            }
            _craftButtonLabel.color = canCraft ? _style.Text : _style.MutedText;
        }

        private void RebuildCosts(IReadOnlyList<ResourceRequired> costs)
        {
            var hasAnyCost = false;

            if (costs != null)
            {
                foreach (var cost in costs)
                {
                    if (cost.Resource == null || cost.Amount <= 0)
                        continue;

                    hasAnyCost = true;
                    CreateCostRow(cost);
                }
            }

            if (!hasAnyCost)
            {
                var text = CreateText("NoCost", _costsRoot, "Нет стоимости", 16, FontStyles.Bold, TextAlignmentOptions.Left);
                text.color = _style.MutedText;
                text.rectTransform.sizeDelta = new Vector2(0f, 30f);
            }
        }

        private void CreateCostRow(ResourceRequired cost)
        {
            var row = CreatePanel($"Cost_{cost.Resource.Name}", _costsRoot, _style.Slot);
            row.sizeDelta = new Vector2(0f, 42f);

            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 10, 6, 6);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;

            var iconSlot = CreateRect("IconSlot", row);
            iconSlot.sizeDelta = new Vector2(30f, 30f);
            iconSlot.gameObject.AddComponent<LayoutElement>().preferredWidth = 30f;

            var icon = CreateImage("Icon", iconSlot, cost.Resource.Icon != null ? cost.Resource.Icon : _style.PlaceholderIcon);
            Stretch(icon.rectTransform);
            icon.preserveAspect = true;
            var resourceIcon = CreateResourceIcon(iconSlot, "BlockIcon");
            SetResourceIcon(icon, resourceIcon, cost.Resource, _style.PlaceholderIcon);

            var name = CreateText("Name", row, cost.Resource.Name, 15, FontStyles.Bold, TextAlignmentOptions.Left);
            name.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var available = _adapter.GetAvailable(cost);
            var amount = CreateText("Amount", row, $"{available}/{cost.Amount}", 15, FontStyles.Bold, TextAlignmentOptions.Right);
            amount.color = available >= cost.Amount ? _style.SuccessText : _style.WarningText;
            amount.rectTransform.sizeDelta = new Vector2(90f, 0f);
            amount.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        }

        private void CraftSelected()
        {
            var result = _adapter.TryCraft(_selectedRecipe);

            if (result.Success)
            {
                _resultText.color = _style.SuccessText;
                _resultText.text = "Создано";
            }
            else
            {
                _resultText.color = _style.WarningText;
                _resultText.text = ResolveResultMessage(result);
            }

            RefreshItemStates();
            RefreshDetails();
        }

        private void HandleInventoryChanged()
        {
            RefreshItemStates();
            RefreshDetails();
        }

        private CraftingCategory ResolveInitialCategory(BuildingConfig initialBuilding)
        {
            if (initialBuilding == null || _categories == null)
                return null;

            return _categories.FirstOrDefault(c =>
                c.Recipes.Any(r => r.SourceBuilding == initialBuilding));
        }

        private void ShowEmptyState(string message)
        {
            ClearItems();
            ClearCosts();

            _selectedRecipe = null;
            _emptyState.gameObject.SetActive(true);
            _emptyState.text = message;
            SetItemIcon(_detailIcon, ResolveDetailResourceIcon(), null, _style.PlaceholderIcon);
            _detailName.text = string.Empty;
            _detailDescription.text = string.Empty;
            _detailRequirement.text = string.Empty;
            _craftButton.interactable = false;
            if (!_usesPrefabLayout && _craftButtonImage != null)
            {
                _craftButtonImage.sprite = _style.ButtonDisabled;
            }
            _craftButtonLabel.color = _style.MutedText;
        }

        private void PlayOpenAnimation()
        {
            if (_openAnimation != null)
            {
                StopCoroutine(_openAnimation);
            }

            _openAnimation = StartCoroutine(OpenAnimationRoutine());
        }

        private IEnumerator OpenAnimationRoutine()
        {
            _canvasGroup.alpha = 0f;
            _windowPanel.localScale = Vector3.one * ClosedScale;

            var elapsed = 0f;

            while (elapsed < OpenAnimationDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / OpenAnimationDuration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);

                _canvasGroup.alpha = eased;
                _windowPanel.localScale = Vector3.one * Mathf.Lerp(ClosedScale, OpenScale, eased);

                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _windowPanel.localScale = Vector3.one;
            _openAnimation = null;
        }

        private void ClearTabs()
        {
            foreach (var tab in _tabs)
            {
                if (tab.Button != null)
                {
                    tab.Button.onClick.RemoveAllListeners();
                    Destroy(tab.Button.gameObject);
                }
            }

            _tabs.Clear();
        }

        private void ClearItems()
        {
            foreach (var item in _items)
            {
                if (item.Button != null)
                {
                    item.Button.onClick.RemoveAllListeners();
                    Destroy(item.Button.gameObject);
                }
            }

            _items.Clear();
        }

        private void ClearCosts()
        {
            if (_costsRoot == null)
                return;

            ClearChildren(_costsRoot);
        }

        private static string ResolveResultMessage(CraftingResult result)
        {
            switch (result.Status)
            {
                case CraftingResultStatus.Locked:
                    return "Рецепт закрыт";
                case CraftingResultStatus.NotEnoughResources:
                    return "Недостаточно ресурсов";
                case CraftingResultStatus.InventoryUnavailable:
                    return "Инвентарь недоступен";
                case CraftingResultStatus.NoRecipe:
                    return "Рецепт не найден";
                default:
                    return string.IsNullOrWhiteSpace(result.Message) ? "Не удалось создать" : result.Message;
            }
        }

        private void EnsureElementPrefabs()
        {
            if (_tabPrefab == null)
            {
                _tabPrefab = Resources.Load<CraftingTabButton>(TabPrefabResourcePath);
            }

            if (_itemPrefab == null)
            {
                _itemPrefab = Resources.Load<CraftingItemView>(ItemPrefabResourcePath);
            }

            if (_resourceIconPrefab == null)
            {
                _resourceIconPrefab = Resources.Load<ResourceIcon>(ResourceIconPrefabResourcePath);
            }
        }

        private ResourceIcon ResolveDetailResourceIcon()
        {
            if (_detailResourceIcon != null)
                return _detailResourceIcon;

            if (_detailIcon == null)
                return null;

            var parent = _detailIcon.transform.parent;

            if (parent == null)
                return null;

            _detailResourceIcon = parent.GetComponentInChildren<ResourceIcon>(true);

            if (_detailResourceIcon == null && _resourceIconPrefab != null)
            {
                _detailResourceIcon = Instantiate(_resourceIconPrefab, parent);
                _detailResourceIcon.name = "DetailBlockIcon";

                if (_detailResourceIcon.transform is RectTransform rect)
                {
                    CopyRect(_detailIcon.rectTransform, rect);
                    _detailResourceIcon.transform.SetSiblingIndex(_detailIcon.transform.GetSiblingIndex() + 1);
                }
            }

            if (_detailResourceIcon != null)
            {
                _detailResourceIcon.gameObject.SetActive(false);
            }

            return _detailResourceIcon;
        }

        private ResourceIcon CreateResourceIcon(Transform parent, string name)
        {
            if (_resourceIconPrefab == null || parent == null)
                return null;

            var icon = Instantiate(_resourceIconPrefab, parent);
            icon.name = name;
            icon.gameObject.SetActive(false);

            if (icon.transform is RectTransform rect)
            {
                Stretch(rect);
            }

            return icon;
        }

        private static void SetItemIcon(Image flatIcon, ResourceIcon blockIcon, ItemConfig item, Sprite fallback)
        {
            if (item != null && item.BlockStyleIcon && item is StackableItemConfig stackableItem && blockIcon != null)
            {
                blockIcon.gameObject.SetActive(true);
                blockIcon.SetResource(stackableItem);

                if (flatIcon != null)
                {
                    flatIcon.sprite = null;
                    flatIcon.gameObject.SetActive(false);
                }

                return;
            }

            if (blockIcon != null)
            {
                blockIcon.gameObject.SetActive(false);
            }

            if (flatIcon != null)
            {
                flatIcon.sprite = item != null && item.Icon != null ? item.Icon : fallback;
                flatIcon.color = Color.white;
                flatIcon.gameObject.SetActive(true);
            }
        }

        private static void SetResourceIcon(Image flatIcon, ResourceIcon blockIcon, StackableItemConfig resource, Sprite fallback)
        {
            if (resource != null && resource.BlockStyleIcon && blockIcon != null)
            {
                blockIcon.gameObject.SetActive(true);
                blockIcon.SetResource(resource);

                if (flatIcon != null)
                {
                    flatIcon.sprite = null;
                    flatIcon.gameObject.SetActive(false);
                }

                return;
            }

            if (blockIcon != null)
            {
                blockIcon.gameObject.SetActive(false);
            }

            if (flatIcon != null)
            {
                flatIcon.sprite = resource != null && resource.Icon != null ? resource.Icon : fallback;
                flatIcon.color = Color.white;
                flatIcon.gameObject.SetActive(true);
            }
        }

        private static void CopyRect(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.offsetMin = source.offsetMin;
            target.offsetMax = source.offsetMax;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.pivot = source.pivot;
            target.localScale = source.localScale;
            target.localRotation = source.localRotation;
            target.localPosition = source.localPosition;
        }

        private static CraftingWindow FindExistingWindow()
        {
            var windows = FindObjectsOfType<CraftingWindow>(true);
            CraftingWindow inactiveReadyWindow = null;

            foreach (var window in windows)
            {
                if (window == null || !window.gameObject.scene.IsValid())
                    continue;

                if (window.gameObject.activeInHierarchy)
                {
                    return window;
                }

                if (inactiveReadyWindow == null && (window._layoutReady || window.HasPrefabLayout()))
                {
                    inactiveReadyWindow = window;
                }
            }

            return inactiveReadyWindow;
        }

        private static CraftingWindow CreateRuntimeWindow()
        {
            var canvas = ResolveCanvas();

            if (canvas == null)
                return null;

            var prefab = Resources.Load<CraftingWindow>(PrefabResourcePath);

            if (prefab != null)
            {
                var prefabInstance = Instantiate(prefab, canvas.transform, false);
                prefabInstance.name = "CraftingWindow";
                prefabInstance.gameObject.SetActive(false);
                return prefabInstance;
            }

            var windowObject = new GameObject(
                "CraftingWindow",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(CraftingWindow));

            var rect = (RectTransform)windowObject.transform;
            rect.SetParent(canvas.transform, false);
            Stretch(rect);
            windowObject.SetActive(false);

            return windowObject.GetComponent<CraftingWindow>();
        }

        private static Canvas ResolveCanvas()
        {
            GameObject taggedCanvas = null;

            try
            {
                taggedCanvas = GameObject.FindGameObjectWithTag(Constants.GameTags.MainCanvas);
            }
            catch (UnityException)
            {
                taggedCanvas = null;
            }

            if (taggedCanvas != null && taggedCanvas.TryGetComponent(out Canvas canvas))
            {
                return canvas;
            }

            var existingCanvas = FindObjectOfType<Canvas>();

            if (existingCanvas != null)
                return existingCanvas;

            var canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var createdCanvas = canvasObject.GetComponent<Canvas>();
            createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            createdCanvas.pixelPerfect = true;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            try
            {
                canvasObject.tag = Constants.GameTags.MainCanvas;
            }
            catch (UnityException)
            {
                // Project tags are editor data; the runtime canvas still works without the tag.
            }

            return createdCanvas;
        }

        private Button CreateButton(string name, Transform parent, Sprite sprite, out Image image)
        {
            var rect = CreatePanel(name, parent, sprite);
            image = rect.GetComponent<Image>();

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = CreateButtonColors();

            return button;
        }

        private static ColorBlock CreateButtonColors()
        {
            var colors = ColorBlock.defaultColorBlock;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.68f, 0.68f, 0.68f, 0.9f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;

            return colors;
        }

        private RectTransform CreatePanel(string name, Transform parent, Sprite sprite)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            image.raycastTarget = true;

            return rect;
        }

        private Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = false;

            return image;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, FontStyles style, TextAlignmentOptions alignment)
        {
            var rect = CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = _style.Text;
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
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
            rect.anchorMin = AnchorStretchMin;
            rect.anchorMax = AnchorStretchMax;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private sealed class TabView
        {
            public TabView(CraftingCategory category, CraftingTabButton view)
            {
                Category = category;
                View = view;
                Button = view != null ? view.Button : null;
            }

            public TabView(CraftingCategory category, Button button, Image background, TextMeshProUGUI label)
            {
                Category = category;
                Button = button;
                Background = background;
                Label = label;
            }

            public CraftingCategory Category { get; }
            public CraftingTabButton View { get; }
            public Button Button { get; }
            public Image Background { get; }
            public TextMeshProUGUI Label { get; }
        }

        private sealed class ItemView
        {
            public ItemView(CraftingRecipeEntry recipe, CraftingItemView component)
            {
                Recipe = recipe;
                Component = component;
                Button = component != null ? component.Button : null;
                Meta = component != null ? component.MetaLabel : null;
            }

            public ItemView(
                CraftingRecipeEntry recipe,
                Button button,
                Image background,
                CanvasGroup canvasGroup,
                TextMeshProUGUI name,
                TextMeshProUGUI meta)
            {
                Recipe = recipe;
                Button = button;
                Background = background;
                CanvasGroup = canvasGroup;
                Name = name;
                Meta = meta;
            }

            public CraftingRecipeEntry Recipe { get; }
            public CraftingItemView Component { get; }
            public Button Button { get; }
            public Image Background { get; }
            public CanvasGroup CanvasGroup { get; }
            public TextMeshProUGUI Name { get; }
            public TextMeshProUGUI Meta { get; }
        }
    }
}
