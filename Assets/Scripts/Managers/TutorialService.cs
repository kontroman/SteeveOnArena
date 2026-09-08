using System;
using System.Collections.Generic;
using System.Linq;
using Devotion.SDK.Controllers;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.AI;
using MineArena.Buildings;
using MineArena.Buildings.Portal;
using MineArena.Controllers;
using MineArena.Items;
using MineArena.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MineArena.Managers
{
    public enum TutorialStep { Portal, Launch, Mine, Kill, Exit, Build, Craft, Gift, Complete }

    [Serializable]
    public sealed class TutorialProgress : BaseProgress
    {
        public bool Initialized;
        public TutorialStep Step;
        public bool Mined;
        public bool Collected;
    }

    // Checkpoints live in the same save as inventory, buildings and paid crafting jobs.
    public sealed class TutorialService : MonoBehaviour
    {
        public static TutorialProgress Progress => GameRoot.PlayerProgress?.TutorialProgress;
        public static bool Active => Progress != null && Progress.Initialized && Progress.Step != TutorialStep.Complete;
        public static bool Expedition => Active && Progress.Step >= TutorialStep.Mine && Progress.Step <= TutorialStep.Exit;
        public static BuildingConfig Workshop => GameRoot.GameConfig?.BuildingsDatabase.AllBuildings.FirstOrDefault(b => b != null && b.name == "LumberjackBuilding");
        private GameObject _overlay;
        private TMP_Text _title, _body, _arrow;
        private Button _gift;
        private Transform _target;
        private float _nextTargetSearch;
        private TutorialStep _shown = (TutorialStep)(-1);
        private TutorialProgress _boundProgress;
        private static TMP_FontAsset _font;
        private static MineArena.UI.TutorialTheme _theme;
        private static TutorialService _instance;
        private TutorialStep _acknowledged = (TutorialStep)(-1);
        private GameObject _popup, _shade;
        private TMP_Text _popupTitle, _popupBody, _confirmLabel;
        private Image _illustration;
        private RectTransform _pointer;
        private float _oldTimeScale = 1, _popupOpenedAt;
        private bool _paused;
        private float _inputResumeAt;
        private Action _deferredCraft;
        public static bool AwaitingConfirmation => Active && _instance != null && _instance._acknowledged != Progress.Step;
        public static bool BlocksInput => AwaitingConfirmation || (_instance != null && (_instance._paused || Time.unscaledTime < _instance._inputResumeAt));
        public static bool DeferCraft(Action open)
        {
            if (!AwaitingConfirmation) return false;
            _instance._deferredCraft = open;
            return true;
        }

        public void Initialize()
        {
            _instance = this;
            if (Application.isPlaying && !Devotion.SDK.Services.SaveSystem.SaveService.Instance.IsLoaded) return;
            var p = Progress;
            if (p == null) return;
            _boundProgress = p;
            if (!p.Initialized)
            {
                p.Initialized = true;
                // Existing settlements keep their progress and do not get onboarding restrictions.
                p.Step = GameRoot.PlayerProgress.LevelsProgress.HighestUnlockedLevelIndex > 0 ||
                    GameRoot.PlayerProgress.BuildingProgress.SavedBuildings.Count > 0
                    ? TutorialStep.Complete : TutorialStep.Portal;
                p.Save();
            }
            ResumeInLobby();
        }

        private void OnEnable() => SceneManager.sceneLoaded += SceneLoaded;
        private void OnDisable() { SceneManager.sceneLoaded -= SceneLoaded; ResumeTime(); if (_overlay != null) _overlay.SetActive(false); }
        private void OnDestroy() { if (_instance == this) _instance = null; ResumeTime(); if (_overlay != null) { if (Application.isPlaying) Destroy(_overlay); else DestroyImmediate(_overlay); } }
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == MineArena.Basics.Constants.SceneNames.PlayerBaseScene) ResumeInLobby();
        }
        private static void ResumeInLobby()
        {
            if (!Active || LevelController.Current != null) return;
            if (Progress.Step >= TutorialStep.Launch && Progress.Step <= TutorialStep.Exit) SetStep(TutorialStep.Portal);
            if (Progress.Step == TutorialStep.Build && GameRoot.GetManager<BuildingManager>()?.GetBuildingLevel(Workshop) > 0)
                SetStep(TutorialStep.Craft);
        }
        public static void SetStep(TutorialStep step)
        {
            if (!Active || Progress.Step == step) return;
            Progress.Step = step;
            Progress.Save();
        }
        public static bool EnterPortal()
        {
            if (!Active) return true;
            if (Progress.Step == TutorialStep.Portal) SetStep(TutorialStep.Launch);
            return Progress.Step == TutorialStep.Launch;
        }
        public static bool AllowLevel(int index) => !Active || index == 0 && Progress.Step == TutorialStep.Launch;
        public static void BeginLevel()
        {
            if (Active) SetStep(Progress.Collected ? TutorialStep.Kill : TutorialStep.Mine);
        }
        public static bool AllowBuilding(BuildingConfig config) => !Active || config == Workshop && Progress.Step == TutorialStep.Build;
        public static bool AllowCraft(ItemConfig item) => !Active || Progress.Step == TutorialStep.Craft && item != null && item.Name == "Planks";
        public static bool AllowWindow(Type type)
        {
            if (!Active) return true;
            switch (type.Name)
            {
                case "PlayingWindow": case "LoadingWindow": case "SettingsWindow": case "LevelProgressWindow": case "BlackWindow": return true;
                case "SelectLevelWindow": return Progress.Step == TutorialStep.Launch;
                case "LevelCompleteWindow": return Progress.Step == TutorialStep.Exit;
                case "BuildingWindow": return Progress.Step == TutorialStep.Build;
                case "CraftingWindow": return Progress.Step == TutorialStep.Craft;
                default: return false;
            }
        }
        public static void MinedBlock()
        {
            if (!Expedition || Progress.Step != TutorialStep.Mine) return;
            Progress.Mined = true;
            Progress.Save();
        }
        public static void CollectedResource(ItemConfig item)
        {
            if (!Expedition || Progress.Step != TutorialStep.Mine || !Progress.Mined || item == null || !item.Stackable) return;
            Progress.Collected = true;
            SetStep(TutorialStep.Kill);
        }
        public static void EnemyKilled() { if (Expedition && Progress.Step == TutorialStep.Kill) SetStep(TutorialStep.Exit); }
        public static void Built(BuildingConfig config) { if (Active && Progress.Step == TutorialStep.Build && config == Workshop) SetStep(TutorialStep.Craft); }
        // Called before the craft output publishes its inventory save.
        public static void Crafted(string id) { if (Active && Progress.Step == TutorialStep.Craft && id == "Planks") Progress.Step = TutorialStep.Gift; }

        public static bool ClaimGift()
        {
            if (!Active || Progress.Step != TutorialStep.Gift) return false;
            var inventory = GameRoot.GetManager<InventoryManager>();
            if (inventory == null) return false;
            var resources = GameRoot.PlayerProgress.InventoryProgress.SavedResources;
            foreach (var gift in new Dictionary<string, int> { ["WoodOak"] = 16, ["Stone"] = 12, ["HealingPotion"] = 2 })
            {
                resources.TryGetValue(gift.Key, out int owned);
                resources[gift.Key] = (int)Math.Min(int.MaxValue, (long)owned + gift.Value);
            }
            Progress.Step = TutorialStep.Complete;
            inventory.InitManager();
            Progress.Save();
            return true;
        }

        // Guaranteed completion rewards, independent of random mining drops and ad multipliers.
        public static void EnsureFirstBuildingReward(Dictionary<ItemConfig, int> rewards)
        {
            var level = Workshop?.GetLevelByNumber(1);
            if (level == null) return;
            foreach (var cost in level.RequiredResources)
            {
                if (cost.Resource == null) continue;
                rewards.TryGetValue(cost.Resource, out int amount);
                rewards[cost.Resource] = Mathf.Max(amount, cost.Amount);
            }
            var planks = GameRoot.GameConfig.ItemDatabase.GetItemConfig("Planks");
            if (planks == null) return;
            foreach (var cost in planks.CraftCosts)
            {
                if (cost.Resource == null) continue;
                int construction = level.RequiredResources.Where(c => c.Resource == cost.Resource).Sum(c => c.Amount);
                rewards.TryGetValue(cost.Resource, out int amount);
                rewards[cost.Resource] = Mathf.Max(amount, construction + cost.Amount);
            }
        }

        private void Update()
        {
            if (GameRoot.Instance != null && Progress != _boundProgress) Initialize();
            if (!Active || Player.Instance == null)
            {
                ResumeTime();
                if (_overlay != null) _overlay.SetActive(false);
                return;
            }
            if (_overlay == null) CreateOverlay();
            _overlay.SetActive(true);
            if (_shown != Progress.Step)
            {
                ResumeTime();
                _shown = Progress.Step;
                _nextTargetSearch = 0;
                _title.text = $"ПЕРВЫЕ ШАГИ   •   {(int)_shown + 1} / 8";
                _body.text = Instructions(_shown);
                _gift.gameObject.SetActive(_shown == TutorialStep.Craft);
                _gift.GetComponentInChildren<TMP_Text>().text = "Мастерская [B]";
                _popupTitle.text = "ПЕРВЫЕ ШАГИ  •  " + ((int)_shown + 1) + " / 8";
                _popupBody.text = Instructions(_shown);
                _confirmLabel.text = _shown == TutorialStep.Gift ? "Забрать подарок" : "Понятно!";
                _illustration.sprite = StepIllustration(_shown);
                _popup.SetActive(false);
                if (_shown != TutorialStep.Craft) _deferredCraft = null;
            }
            bool busy = FindObjectsOfType<BuildingConstructionSequence>().Any(s => s.IsPlaying) ||
                FindObjectsOfType<Devotion.SDK.Base.BaseWindow>().Any(w => w.GetType().Name == "LoadingWindow" || w.GetType().Name == "SettingsWindow") ||
                Progress.Step == TutorialStep.Build && LevelController.Current != null;
            if (AwaitingConfirmation && !busy && !_popup.activeSelf)
            {
                // Scene-local EventSystem is destroyed when leaving the base; restore input BEFORE pausing.
                var input = GameRoot.UIManager?.EnsureInputSystem();
                if (input == null) return;
                GameRoot.UIManager?.CloseTutorialDialogs();
                _oldTimeScale = Time.timeScale; _paused = true; Time.timeScale = 0;
                _popupOpenedAt = Time.unscaledTime;
                _popup.SetActive(true);
                input.SetSelectedGameObject(_popup.transform.Find("Confirm").gameObject);
            }
            _shade.SetActive(_popup.activeSelf);
            _overlay.transform.Find("Coach").gameObject.SetActive(!_popup.activeSelf && !busy);
            if (_popup.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) ConfirmPopup();
                float t = Mathf.Clamp01((Time.unscaledTime - _popupOpenedAt) / .22f);
                _popup.transform.localScale = Vector3.one * Mathf.Lerp(.88f, 1, 1 - Mathf.Pow(1 - t, 3));
            }
            if (Time.unscaledTime >= _nextTargetSearch)
            {
                _nextTargetSearch = Time.unscaledTime + .4f;
                _target = FindTarget();
            }
            var camera = Camera.main;
            _arrow.gameObject.SetActive(_target != null && camera != null && !_popup.activeSelf && !busy);
            if (_arrow.gameObject.activeSelf)
            {
                bool ui = _target is RectTransform;
                Vector3 point;
                float angle = 0;
                if (ui)
                {
                    var canvas = _target.GetComponentInParent<Canvas>();
                    var targetRect = (RectTransform)_target;
                    point = RectTransformUtility.WorldToScreenPoint(canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                        targetRect.TransformPoint(targetRect.rect.center));
                    point.y += (58 + Mathf.Sin(Time.unscaledTime * 5) * 8) * _overlay.GetComponent<Canvas>().scaleFactor;
                }
                else
                {
                    // A large moving compass arrow stays beside the character, even when the destination is off screen.
                    Vector3 origin = camera.WorldToScreenPoint(Player.Instance.transform.position + Vector3.up);
                    Vector3 destination = camera.WorldToScreenPoint(_target.position);
                    Vector2 direction = (Vector2)(destination - origin);
                    if (destination.z < 0) direction = -direction;
                    if (direction.sqrMagnitude < 1) direction = Vector2.up;
                    direction.Normalize();
                    point = origin + (Vector3)direction * (125 + Mathf.Sin(Time.unscaledTime * 4) * 24);
                    angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90;
                }
                if (!ui)
                {
                    point.x = Mathf.Clamp(point.x, 85, Screen.width - 85);
                    point.y = Mathf.Clamp(point.y, 100, Screen.height - 100);
                }
                _arrow.transform.position = point;
                _pointer.localRotation = Quaternion.Euler(0, 0, angle);
                _pointer.localScale = Vector3.one * (1 + Mathf.Sin(Time.unscaledTime * 4) * .08f);
                _arrow.text = ui ? "" : "\n" + Mathf.CeilToInt(Vector3.Distance(Player.Instance.transform.position, _target.position)) + " м";
            }
        }

        private void ResumeTime()
        {
            if (!_paused) return;
            Time.timeScale = _oldTimeScale; _paused = false;
        }
        public void ConfirmPopup()
        {
            if (_shown != Progress.Step || !_popup.activeSelf || Time.unscaledTime - _popupOpenedAt < .2f) return;
            _acknowledged = _shown;
            _inputResumeAt = Time.unscaledTime + .15f;
            _popup.SetActive(false); _shade.SetActive(false); ResumeTime();
            if (_shown == TutorialStep.Gift) ClaimGift();
            var craft = _deferredCraft; _deferredCraft = null;
            if (Active && Progress.Step == TutorialStep.Craft)
            {
                if (craft != null) craft.Invoke();
                else MineArena.Windows.Crafting.CraftingWindow.Open(Workshop);
            }
        }

        public static string Instructions(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.Portal: return "Добро пожаловать! Подойди к порталу.\nWASD — движение. Жёлтый указатель покажет путь.";
                case TutorialStep.Launch: return "Выбери первый уровень и нажми «Начать».\nУчебная экспедиция: добыча ресурсов и один зомби.";
                case TutorialStep.Mine: return "Подойди к блоку и нажми E, чтобы добыть его.\nЗатем подбери выпавшие ресурсы.";
                case TutorialStep.Kill: return "Теперь победи зомби.\nЛевая кнопка мыши — атака. Меч уже в быстром слоте.";
                case TutorialStep.Exit: return "Отлично! Войди в появившийся портал.\nЗабери награду и вернись на базу — хватит на мастерскую!";
                case TutorialStep.Build: return "Подойди к участку мастерской и нажми «Построить».\nДерево и камень для постройки уже у тебя.";
                case TutorialStep.Craft: return "После «Понятно» откроется мастерская. Создай доски.\nДождись заполнения полосы: первый крафт занимает 7 секунд.";
                case TutorialStep.Gift: return "Обучение завершено! Твой подарок:\n16 дуба, 12 камня и 2 зелья лечения. Дальше — твои приключения!";
                default: return "";
            }
        }
        private Transform FindTarget()
        {
            IEnumerable<Transform> targets = Array.Empty<Transform>();
            switch (Progress.Step)
            {
                case TutorialStep.Portal: targets = FindObjectsOfType<ArenaPortal>().Select(p => p.transform); break;
                case TutorialStep.Launch: return FindObjectOfType<MineArena.Windows.SelectLevel.LevelSelectionView>()?.TutorialTarget;
                case TutorialStep.Mine:
                    var pickups = FindObjectsOfType<ItemInteractor>();
                    targets = Progress.Mined && pickups.Length > 0 ? pickups.Select(p => p.transform) : FindObjectsOfType<InteractableObject>().Where(p => p.IsMineable).Select(p => p.transform); break;
                case TutorialStep.Kill: targets = FindObjectsOfType<MobHealth>().Where(m => m.CurrentValue > 0).Select(m => m.transform); break;
                case TutorialStep.Exit: targets = FindObjectsOfType<LevelPortal>().Select(p => p.transform); break;
                case TutorialStep.Build:
                    var buildingWindow = FindObjectOfType<MineArena.Windows.BuildingWindow>();
                    if (buildingWindow != null) return buildingWindow.TutorialTarget;
                    targets = FindObjectsOfType<BuildingZone>().Where(z => z.Config == Workshop).Select(z => z.transform); break;
                case TutorialStep.Craft:
                    var crafting = FindObjectOfType<MineArena.Windows.Crafting.CraftingWindow>();
                    if (crafting != null) return crafting.TutorialTarget;
                    return _gift != null ? _gift.transform : null;
            }
            return targets.OrderBy(t => (t.position - Player.Instance.transform.position).sqrMagnitude).FirstOrDefault();
        }
        private void CreateOverlay()
        {
            _theme = Resources.Load<MineArena.UI.TutorialTheme>("UI/TutorialTheme");
            if (_font == null)
                _font = Resources.Load<GameObject>("Prefabs/Windows/Crafting/CraftingWindow")?.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t.font != null && t.font.name.StartsWith("Rubik"))?.font ?? TMP_Settings.defaultFontAsset;
            if (_theme != null) _font = _theme.BodyFont;
            _overlay = new GameObject("Tutorial guidance", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (Application.isPlaying) DontDestroyOnLoad(_overlay);
            var canvas = _overlay.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = _overlay.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var panel = new GameObject("Coach", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(_overlay.transform, false);
            var rect = (RectTransform)panel.transform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -8); rect.sizeDelta = new Vector2(840, 110);
            panel.GetComponent<Image>().color = new Color(.10f, .15f, .25f, .96f);
            panel.GetComponent<Image>().sprite = Skin("craft_panel"); panel.GetComponent<Image>().type = Image.Type.Sliced;
            _title = Label(panel.transform, "Title", 20, new Vector2(24, -10), new Vector2(792, 28)); _title.color = new Color(1, .8f, .32f);
            _body = Label(panel.transform, "Instructions", 21, new Vector2(24, -40), new Vector2(792, 64));
            var button = new GameObject("Claim tutorial gift", typeof(RectTransform), typeof(Image), typeof(Button)); button.transform.SetParent(panel.transform, false);
            var br = (RectTransform)button.transform; br.anchorMin = br.anchorMax = new Vector2(1, 1); br.pivot = new Vector2(1, 1); br.anchoredPosition = new Vector2(-18, -12); br.sizeDelta = new Vector2(220, 38);
            button.GetComponent<Image>().color = new Color(.24f, .58f, .39f);
            _gift = button.GetComponent<Button>(); _gift.onClick.AddListener(() => { if (Active && Progress.Step == TutorialStep.Craft) MineArena.Windows.Crafting.CraftingWindow.Open(Workshop); });
            Label(button.transform, "Label", 20, new Vector2(10, -4), new Vector2(205, 30)).text = "Забрать подарок";
            _arrow = Label(_overlay.transform, "Target arrow", 27, Vector2.zero, new Vector2(100, 100)); _arrow.alignment = TextAlignmentOptions.Center; _arrow.color = new Color(1, .78f, .18f); _arrow.outlineWidth = .2f; _arrow.outlineColor = Color.black;
            ((RectTransform)_arrow.transform).pivot = new Vector2(.5f, .5f);
            var pointer = new GameObject("Pointer", typeof(RectTransform), typeof(TutorialArrowGraphic)); pointer.transform.SetParent(_arrow.transform, false);
            var pointerRect = (RectTransform)pointer.transform; pointerRect.anchorMin = pointerRect.anchorMax = new Vector2(.5f, .5f); pointerRect.sizeDelta = new Vector2(76, 88); _pointer = pointerRect;
            pointer.GetComponent<TutorialArrowGraphic>().raycastTarget = false;
            _shade = new GameObject("Popup backdrop", typeof(RectTransform), typeof(Image)); _shade.transform.SetParent(_overlay.transform, false);
            var shadeRect = (RectTransform)_shade.transform; shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = Vector2.one; shadeRect.sizeDelta = Vector2.zero;
            _shade.GetComponent<Image>().color = new Color(.025f, .04f, .09f, .78f);
            _popup = new GameObject("Tutorial popup", typeof(RectTransform), typeof(Image)); _popup.transform.SetParent(_overlay.transform, false);
            var popupRect = (RectTransform)_popup.transform; popupRect.anchorMin = popupRect.anchorMax = new Vector2(.5f, .5f); popupRect.sizeDelta = new Vector2(820, 480);
            var frame = _popup.GetComponent<Image>(); frame.sprite = Skin("craft_panel"); frame.type = Image.Type.Sliced; frame.color = Color.white;
            var ribbon = new GameObject("Golden ribbon", typeof(RectTransform), typeof(Image)); ribbon.transform.SetParent(_popup.transform, false);
            var ribbonRect = (RectTransform)ribbon.transform; ribbonRect.anchorMin = new Vector2(0, 1); ribbonRect.anchorMax = Vector2.one; ribbonRect.pivot = new Vector2(.5f, 1); ribbonRect.sizeDelta = new Vector2(-36, 72); ribbonRect.anchoredPosition = new Vector2(0, -18);
            ribbon.GetComponent<Image>().sprite = Skin("craft_button_selected"); ribbon.GetComponent<Image>().type = Image.Type.Sliced;
            _popupTitle = Label(_popup.transform, "Popup title", 29, new Vector2(40, -35), new Vector2(740, 45)); _popupTitle.alignment = TextAlignmentOptions.Center; _popupTitle.color = new Color(1, .95f, .78f);
            var icon = new GameObject("Step illustration", typeof(RectTransform), typeof(Image)); icon.transform.SetParent(_popup.transform, false);
            var ir = (RectTransform)icon.transform; ir.anchorMin = ir.anchorMax = new Vector2(.5f, 1); ir.pivot = new Vector2(.5f, 1); ir.anchoredPosition = new Vector2(0, -106); ir.sizeDelta = new Vector2(112, 112);
            _illustration = icon.GetComponent<Image>(); _illustration.preserveAspect = true; _illustration.raycastTarget = false;
            _popupBody = Label(_popup.transform, "Popup instructions", 27, new Vector2(48, -236), new Vector2(724, 132)); _popupBody.alignment = TextAlignmentOptions.Center; _popupBody.color = new Color(1, .97f, .88f);
            var confirm = new GameObject("Confirm", typeof(RectTransform), typeof(Image), typeof(Button)); confirm.transform.SetParent(_popup.transform, false);
            var cr = (RectTransform)confirm.transform; cr.anchorMin = cr.anchorMax = new Vector2(.5f, 0); cr.pivot = new Vector2(.5f, 0); cr.anchoredPosition = new Vector2(0, 28); cr.sizeDelta = new Vector2(300, 64);
            confirm.GetComponent<Image>().sprite = Skin("craft_button_green"); confirm.GetComponent<Image>().type = Image.Type.Sliced; confirm.GetComponent<Button>().onClick.AddListener(ConfirmPopup);
            _confirmLabel = Label(confirm.transform, "Label", 27, new Vector2(0, -8), new Vector2(300, 48)); _confirmLabel.alignment = TextAlignmentOptions.Center;
            _popup.SetActive(false); _shade.SetActive(false);
            ApplyTheme(panel, ribbon, confirm);
        }
        private void ApplyTheme(GameObject coach, GameObject ribbon, GameObject confirm)
        {
            if (_theme == null) return;
            foreach (var frame in new[] { coach.GetComponent<Image>(), _popup.GetComponent<Image>() })
            { frame.sprite = _theme.Panel; frame.type = Image.Type.Sliced; frame.color = Color.white; }
            ribbon.GetComponent<Image>().sprite = _theme.Ribbon;
            confirm.GetComponent<Image>().sprite = _theme.Button;
            _gift.GetComponent<Image>().sprite = _theme.Button;
            var quickRect = (RectTransform)_gift.transform; quickRect.sizeDelta = new Vector2(230, 28); quickRect.anchoredPosition = new Vector2(-12, -7);
            var quickLabel = _gift.GetComponentInChildren<TMP_Text>(); quickLabel.font = _theme.HeadingFont; quickLabel.fontSize = 18;
            quickLabel.rectTransform.anchoredPosition = new Vector2(8, -2); quickLabel.rectTransform.sizeDelta = new Vector2(214, 24);
            _body.color = _popupBody.color = new Color(.25f, .20f, .16f);
            _title.font = _popupTitle.font = _confirmLabel.font = _theme.HeadingFont;
            _title.color = _popupTitle.color = new Color(1, .96f, .85f);
            var topRibbon = new GameObject("Coach ribbon", typeof(RectTransform), typeof(Image)); topRibbon.transform.SetParent(coach.transform, false); topRibbon.transform.SetAsFirstSibling();
            var rect = (RectTransform)topRibbon.transform; rect.anchorMin = new Vector2(0, 1); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -5); rect.sizeDelta = new Vector2(-10, 32);
            var image = topRibbon.GetComponent<Image>(); image.sprite = _theme.Ribbon; image.type = Image.Type.Sliced; image.raycastTarget = false;
            _arrow.font = _theme.HeadingFont;
            _arrow.fontSize = 40;
            _arrow.color = Color.yellow;
            _arrow.fontStyle = FontStyles.Bold;
            _arrow.outlineWidth = .2f;
            _arrow.rectTransform.sizeDelta = new Vector2(180, 140);
            _arrow.alignment = TextAlignmentOptions.Bottom;
        }
        private static Sprite Skin(string name) => Resources.Load<Sprite>("Prefabs/Windows/Crafting/Textures/" + name);
        public static Sprite StepIllustration(TutorialStep step)
        {
            if (step == TutorialStep.Build) return Workshop?.GetLevelByNumber(1)?.Preview;
            if (step == TutorialStep.Portal || step == TutorialStep.Launch || step == TutorialStep.Exit)
                return GameRoot.GameConfig?.Levels.FirstOrDefault()?.LevelIcon;
            if (step == TutorialStep.Gift)
            {
                var gift = FindObjectsOfType<MineArena.UI.GameUiAction>().FirstOrDefault(a => a.Destination == MineArena.UI.GameUiDestination.Daily);
                var sprite = gift?.transform.Find("Icon")?.GetComponent<Image>()?.sprite;
                if (sprite != null) return sprite;
            }
            return GameRoot.GameConfig?.ItemDatabase.GetItemConfig(step == TutorialStep.Kill ? "WoodSword" : step == TutorialStep.Craft ? "Planks" : step == TutorialStep.Gift ? "HealingPotion" : "WoodOak")?.Icon;
        }
        private static TMP_Text Label(Transform parent, string name, int size, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = dimensions;
            var text = go.GetComponent<TextMeshProUGUI>(); text.font = _font; text.fontSize = size; text.color = Color.white; text.raycastTarget = false; return text;
        }
    }

    // Mesh arrow stays sharp at any resolution and does not depend on font glyph coverage.
    public sealed class TutorialArrowGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            // Authored on a pixel grid: dark outline, cream highlight and amber bevel.
            string[] pixels = {
                ".....#####.....",
                ".....#hhh#.....",
                ".....#hyg#.....",
                ".....#hyg#.....",
                ".....#hyg#.....",
                ".....#hyg#.....",
                "######hyg######",
                "#hhhhhhyyyyggg#",
                ".#hyyyyyyyygg#.",
                "..#hyyyyyygg#..",
                "...#hyyyygg#...",
                "....#hyygg#....",
                ".....#hgg#.....",
                "......#g#......",
                ".......#......."
            };
            float unit = Mathf.Min(r.width, r.height) / 15f;
            for (int y = 0; y < pixels.Length; y++)
            for (int x = 0; x < pixels[y].Length; x++)
            {
                char pixel = pixels[y][x];
                if (pixel == '.') continue;
                Color32 tint = pixel == '#' ? new Color32(48, 32, 28, 255) :
                    pixel == 'h' ? new Color32(255, 249, 194, 255) :
                    pixel == 'g' ? new Color32(215, 133, 30, 255) : new Color32(255, 211, 64, 255);
                var p = r.center + new Vector2(x - 7.5f, 6.5f - y) * unit;
                int start = vh.currentVertCount;
                vh.AddVert(p, tint, Vector2.zero);
                vh.AddVert(p + Vector2.right * unit, tint, Vector2.zero);
                vh.AddVert(p + Vector2.one * unit, tint, Vector2.zero);
                vh.AddVert(p + Vector2.up * unit, tint, Vector2.zero);
                vh.AddTriangle(start, start + 1, start + 2);
                vh.AddTriangle(start, start + 2, start + 3);
            }
        }
    }
}
