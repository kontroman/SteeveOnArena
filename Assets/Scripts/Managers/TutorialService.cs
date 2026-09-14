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
    // Preserve serialized values of existing checkpoints.
    public enum TutorialStep { Portal, Launch, Mine, Kill, Exit, Build, Craft, Gift, Complete, BuildSmith, CraftArmor, CraftSword, EquipArmor, EquipSword, Potion, Rewards, WorkshopButton, PlaytimeRewards, FortuneWheel }

    [Serializable]
    public sealed class TutorialProgress : BaseProgress
    {
        public bool Initialized;
        public TutorialStep Step;
        public bool Mined;
        public bool MiningStarted;
        public bool Collected;
        public bool SmithSuppliesGranted;
        public bool PotionGranted;
    }

    // Checkpoints live in the same save as inventory, buildings and paid crafting jobs.
    public sealed class TutorialService : MonoBehaviour
    {
        public static TutorialProgress Progress => GameRoot.PlayerProgress?.TutorialProgress;
        public static bool Active => Progress != null && Progress.Initialized && Progress.Step != TutorialStep.Complete;
        public static bool Expedition => Active && Progress.Step >= TutorialStep.Mine && Progress.Step <= TutorialStep.Exit;
        public static BuildingConfig Workshop => GameRoot.GameConfig?.BuildingsDatabase.AllBuildings.FirstOrDefault(b => b != null && b.name == "LumberjackBuilding");
        public static BuildingConfig Smith => GameRoot.GameConfig?.BuildingsDatabase.AllBuildings.FirstOrDefault(b => b != null && b.name == "SmithBuilding");
        public static bool CraftStep => Active && Progress.Step == TutorialStep.CraftSword;
        public static string CraftItemId => "StoneSword";
        public static BuildingConfig CraftBuilding => Smith;
        public static bool AllowHud(MineArena.UI.GameUiDestination destination) => !Active || destination == MineArena.UI.GameUiDestination.Settings ||
            destination == MineArena.UI.GameUiDestination.Crafting && (CraftStep || Progress.Step == TutorialStep.WorkshopButton) ||
            destination == MineArena.UI.GameUiDestination.Inventory && (Progress.Step == TutorialStep.EquipArmor || Progress.Step == TutorialStep.EquipSword || Progress.Step == TutorialStep.Potion) ||
            destination == MineArena.UI.GameUiDestination.Daily && Progress.Step == TutorialStep.Rewards ||
            destination == MineArena.UI.GameUiDestination.Playtime && Progress.Step == TutorialStep.PlaytimeRewards ||
            destination == MineArena.UI.GameUiDestination.Wheel && Progress.Step == TutorialStep.FortuneWheel ||
            destination == MineArena.UI.GameUiDestination.Levels && Progress.Step == TutorialStep.Launch;
        public static void HudOpened(MineArena.UI.GameUiDestination destination)
        {
            if (!Active || AwaitingConfirmation) return;
            if (_instance != null && ReviewWindowOpen()) _instance._hudVisited = true;
        }
        private static string ReviewWindowName => Progress?.Step == TutorialStep.Rewards ? "DailyGiftWIndow" :
            Progress?.Step == TutorialStep.PlaytimeRewards ? "PlaytimeGiftWindow" :
            Progress?.Step == TutorialStep.FortuneWheel ? "FortuneWheelWindow" :
            Progress?.Step == TutorialStep.WorkshopButton ? "CraftingWindow" : null;
        private static bool ReviewWindowOpen() => ReviewWindowName != null &&
            FindObjectsOfType<Devotion.SDK.Base.BaseWindow>().Any(w => w.GetType().Name == ReviewWindowName);
        private bool _hudVisited;
        private TutorialSpotlightGraphic _spotlight;
        private RectTransform _dragHand;
        private Image _dragItem;
        private MineArena.UI.ResourceIcon _blockIllustration;
        private static int StepNumber(TutorialStep step) => Array.IndexOf(Steps, step) + 1;
        public static readonly TutorialStep[] Steps = { TutorialStep.Portal, TutorialStep.Launch, TutorialStep.Mine,
            TutorialStep.Kill, TutorialStep.Exit, TutorialStep.BuildSmith, TutorialStep.CraftSword,
            TutorialStep.EquipSword, TutorialStep.Potion, TutorialStep.Rewards, TutorialStep.PlaytimeRewards, TutorialStep.FortuneWheel, TutorialStep.WorkshopButton, TutorialStep.Gift };
        private static TutorialStep CurrentCheckpoint(TutorialStep step)
        {
            if (step == TutorialStep.Build || step == TutorialStep.Craft || step == TutorialStep.CraftArmor)
                return TutorialStep.BuildSmith;
            if (step == TutorialStep.EquipArmor) return TutorialStep.EquipSword;
            return step;
        }
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
        public static bool WorldGuidanceVisible => !AwaitingConfirmation && (GameRoot.Instance == null || GameRoot.UIManager?.HasOpenDialog != true) && !FindObjectsOfType<Devotion.SDK.Base.BaseWindow>().Any(w =>
            w.GetType().Name != "PlayingWindow" && w.GetType().Name != "LevelProgressWindow");
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
            _hudVisited = false;
            if (!p.Initialized)
            {
                p.Initialized = true;
                // Existing settlements keep their progress and do not get onboarding restrictions.
                p.Step = GameRoot.PlayerProgress.LevelsProgress.HighestUnlockedLevelIndex > 0 ||
                    GameRoot.PlayerProgress.BuildingProgress.SavedBuildings.Count > 0
                    ? TutorialStep.Complete : TutorialStep.Portal;
                p.Save();
            }
            SetStep(CurrentCheckpoint(p.Step));
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
            if (Progress.Step == TutorialStep.BuildSmith && GameRoot.GetManager<BuildingManager>()?.GetBuildingLevel(Smith) > 0)
                SetStep(TutorialStep.CraftSword);
        }
        public static void SetStep(TutorialStep step)
        {
            step = CurrentCheckpoint(step);
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
            if (Active) { Progress.MiningStarted = false; SetStep(Progress.Collected ? TutorialStep.Kill : TutorialStep.Mine); }
        }
        public static void StartedMining()
        {
            if (!Expedition || Progress.Step != TutorialStep.Mine) return;
            Progress.MiningStarted = true;
            if (_instance != null && _instance._arrow != null) _instance._arrow.gameObject.SetActive(false);
        }
        public static bool AllowBuilding(BuildingConfig config) => !Active || config == Smith && Progress.Step == TutorialStep.BuildSmith;
        public static bool AllowCraft(ItemConfig item) => !Active || CraftStep && item != null && item.Name == CraftItemId;
        public static bool AllowWindow(Type type)
        {
            if (!Active) return true;
            switch (type.Name)
            {
                case "PlayingWindow": case "LoadingWindow": case "SettingsWindow": case "LevelProgressWindow": case "BlackWindow": return true;
                case "SelectLevelWindow": return Progress.Step == TutorialStep.Launch;
                case "LevelCompleteWindow": return Progress.Step == TutorialStep.Exit;
                case "BuildingWindow": return Progress.Step == TutorialStep.Build || Progress.Step == TutorialStep.BuildSmith;
                case "CraftingWindow": return CraftStep || Progress.Step == TutorialStep.WorkshopButton;
                case "InventoryWindow": return AllowHud(MineArena.UI.GameUiDestination.Inventory);
                case "DailyGiftWIndow": return Progress.Step == TutorialStep.Rewards;
                case "PlaytimeGiftWindow": return Progress.Step == TutorialStep.PlaytimeRewards;
                case "FortuneWheelWindow": return Progress.Step == TutorialStep.FortuneWheel;
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
        public static void Built(BuildingConfig config) {
            if (Active && Progress.Step == TutorialStep.BuildSmith && config == Smith) SetStep(TutorialStep.CraftSword);
        }
        // Output and checkpoint are saved together by the production service.
        public static void Crafted(string id) {
            if (CraftStep && id == CraftItemId) Progress.Step = TutorialStep.EquipSword;
        }
        private static void RefreshExtendedProgress()
        {
            var inventory = GameRoot.GetManager<InventoryManager>();
            if (inventory == null) return;
            SetStep(CurrentCheckpoint(Progress.Step));
            var saved = GameRoot.PlayerProgress.InventoryProgress;
            if (Progress.Step == TutorialStep.BuildSmith && !Progress.SmithSuppliesGranted && Smith != null)
            {
                var costs = new Dictionary<string, int>();
                foreach (var cost in Smith.GetLevelByNumber(1).RequiredResources.Concat(
                    GameRoot.GameConfig.ItemDatabase.GetItemConfig("StoneSword").CraftCosts))
                {
                    costs.TryGetValue(cost.Resource.Name, out int amount);
                    costs[cost.Resource.Name] = amount + cost.Amount;
                }
                foreach (var cost in costs) {
                    saved.SavedResources.TryGetValue(cost.Key, out int amount);
                    saved.SavedResources[cost.Key] = Math.Max(amount, cost.Value);
                }
                Progress.SmithSuppliesGranted = true;
                inventory.InitManager(); Progress.Save();
            }
            if (Progress.Step == TutorialStep.BuildSmith && GameRoot.GetManager<BuildingManager>()?.GetBuildingLevel(Smith) > 0) SetStep(TutorialStep.CraftSword);
            if (Progress.Step == TutorialStep.EquipSword && saved.GetQuickSlotItemId(saved.SelectedQuickSlotIndex) == "StoneSword") SetStep(TutorialStep.Potion);
            if (Progress.Step == TutorialStep.Potion && !Progress.PotionGranted) {
                saved.SavedResources.TryGetValue("HealingPotion", out int amount);
                saved.SavedResources["HealingPotion"] = Math.Max(amount, 1);
                Progress.PotionGranted = true; inventory.InitManager(); Progress.Save();
            }
            if (Progress.Step == TutorialStep.Potion && saved.QuickSlotItemIds.Contains("HealingPotion")) SetStep(TutorialStep.Rewards);
        }

        public static bool ClaimGift()
        {
            if (!Active || Progress.Step != TutorialStep.Gift) return false;
            var inventory = GameRoot.GetManager<InventoryManager>();
            if (inventory == null) return false;
            var resources = GameRoot.PlayerProgress.InventoryProgress.SavedResources;
            foreach (var gift in new Dictionary<string, int> { ["IronChestplate"] = 1 })
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
            var level = Smith?.GetLevelByNumber(1);
            var sword = GameRoot.GameConfig?.ItemDatabase.GetItemConfig(CraftItemId);
            if (level == null || sword == null) return;
            var required = new Dictionary<ItemConfig, int>();
            foreach (var cost in level.RequiredResources.Concat(sword.CraftCosts))
            {
                if (cost.Resource == null) continue;
                required.TryGetValue(cost.Resource, out int amount);
                required[cost.Resource] = amount + cost.Amount;
            }
            foreach (var cost in required)
            {
                rewards.TryGetValue(cost.Key, out int amount);
                rewards[cost.Key] = Mathf.Max(amount, cost.Value);
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
            RefreshExtendedProgress();
            if (!AwaitingConfirmation && ReviewWindowOpen()) _hudVisited = true;
            if (_hudVisited && !FindObjectsOfType<Devotion.SDK.Base.BaseWindow>().Any(w =>
                w.GetType().Name != "PlayingWindow" && w.GetType().Name != "LevelProgressWindow"))
            {
                _hudVisited = false;
                SetStep(Steps[Array.IndexOf(Steps, Progress.Step) + 1]);
            }
            _overlay.SetActive(true);
            if (_shown != Progress.Step)
            {
                ResumeTime();
                _shown = Progress.Step;
                _nextTargetSearch = 0;
                _title.text = $"ПЕРВЫЕ ШАГИ   •   {StepNumber(_shown)} / 14";
                _body.text = Instructions(_shown);
                _gift.gameObject.SetActive(false);
                _popupTitle.text = "ПЕРВЫЕ ШАГИ  •  " + StepNumber(_shown) + " / 14";
                _popupBody.text = Instructions(_shown);
                _confirmLabel.text = _shown == TutorialStep.Gift ? "Забрать подарок" : "Понятно!";
                UpdateIllustration(_shown);
                _popup.SetActive(false);
                if (!CraftStep) _deferredCraft = null;
            }
            bool busy = FindObjectsOfType<BuildingConstructionSequence>().Any(s => s.IsPlaying) ||
                FindObjectsOfType<Devotion.SDK.Base.BaseWindow>().Any(w => w.GetType().Name == "LoadingWindow" || w.GetType().Name == "SettingsWindow") ||
                Progress.Step == TutorialStep.BuildSmith && LevelController.Current != null;
            KeepInventoryLessonOpen();
            if ((_shown == TutorialStep.Kill || _shown == TutorialStep.Exit) && AwaitingConfirmation) _acknowledged = _shown;
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
            UpdateDragLesson(!busy && !_popup.activeSelf);
            UpdateSpotlight(!busy && !_popup.activeSelf && !_hudVisited ? _target as RectTransform : null);
            _arrow.gameObject.SetActive(_target != null && !(_target is RectTransform) && camera != null && !_popup.activeSelf && !busy && WorldGuidanceVisible &&
                !(Progress.Step == TutorialStep.Mine && (Progress.MiningStarted || Progress.Mined)));
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
            if (CraftStep)
            {
                if (craft != null) craft.Invoke();
                else MineArena.Windows.Crafting.CraftingWindow.Open(CraftBuilding);
            }
        }

        public static string Instructions(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.PlaytimeRewards: return "Нажми награды за время в игре.\nЗдесь подарки за проведённое время. Осмотри окно и закрой его.";
                case TutorialStep.FortuneWheel: return "Нажми колесо фортуны: здесь можно выиграть призы.\nОсмотри условия вращения и закрой окно.";
                case TutorialStep.BuildSmith: return "Подойди к участку кузницы и нажми «Построить».\nНаграды первой локации хватит на кузницу и меч.";
                case TutorialStep.CraftArmor: return "Создай железный нагрудник в кузнице.\nМатериалы уже у тебя. Дождись завершения крафта.";
                case TutorialStep.CraftSword: return "Теперь создай каменный меч и дождись результата.\nОн сильнее стартового деревянного меча.";
                case TutorialStep.EquipArmor: return "Открой инвентарь. Перетащи железный нагрудник\nв слот брони на груди — он уменьшает входящий урон.";
                case TutorialStep.EquipSword: return "Открой инвентарь и перетащи каменный меч в нижний слот.\nВыбери этот слот клавишей 1–5, чтобы взять меч в руки.";
                case TutorialStep.Potion: return "Перетащи зелье лечения из инвентаря в нижний слот.\nВыбери его клавишей 1–5 и используй для лечения.";
                case TutorialStep.Rewards: return "Нажми подсвеченную кнопку наград.\nЗдесь подарки за ежедневный вход. Осмотри окно и закрой его.";
                case TutorialStep.WorkshopButton: return "Кнопка «Мастерская» открывает рецепты зданий.\nНажми её, осмотри крафт и закрой окно. Горячая клавиша — B.";
                case TutorialStep.Portal: return "Добро пожаловать! Подойди к порталу.\nWASD — движение. Жёлтый указатель покажет путь.";
                case TutorialStep.Launch: return "Выбери первый уровень и нажми «Начать».\nУчебная экспедиция: добыча ресурсов и один зомби.";
                case TutorialStep.Mine: return "Подойди к блоку и нажми E, чтобы добыть его.\nЗатем подбери выпавшие ресурсы.";
                case TutorialStep.Kill: return "Теперь победи зомби.\nЛевая кнопка мыши — атака. Меч уже в быстром слоте.";
                case TutorialStep.Exit: return "Портал открыт! Можно продолжать бегать и добывать ресурсы.\nКогда будешь готов, войди в портал и вернись на базу.";
                case TutorialStep.Build: return "Подойди к участку мастерской и нажми «Построить».\nДерево и камень для постройки уже у тебя.";
                case TutorialStep.Craft: return "После «Понятно» откроется мастерская. Создай доски.\nДождись заполнения полосы: первый крафт занимает 7 секунд.";
                case TutorialStep.Gift: return "Обучение завершено! Твой подарок — железный нагрудник.\nНадень его в инвентаре, чтобы получать меньше урона.";
                default: return "";
            }
        }
        private Transform FindTarget()
        {
            IEnumerable<Transform> targets = Array.Empty<Transform>();
            switch (Progress.Step)
            {
                case TutorialStep.EquipArmor:
                case TutorialStep.EquipSword:
                case TutorialStep.Potion:
                    if (FindObjectOfType<MineArena.UI.InventoryWindow>() != null) return null;
                    return HudTarget(MineArena.UI.GameUiDestination.Inventory);
                case TutorialStep.Rewards: return _hudVisited ? null : HudTarget(MineArena.UI.GameUiDestination.Daily);
                case TutorialStep.PlaytimeRewards: return _hudVisited ? null : HudTarget(MineArena.UI.GameUiDestination.Playtime);
                case TutorialStep.FortuneWheel: return _hudVisited ? null : HudTarget(MineArena.UI.GameUiDestination.Wheel);
                case TutorialStep.WorkshopButton: return _hudVisited ? null : HudTarget(MineArena.UI.GameUiDestination.Crafting);
                case TutorialStep.Portal: targets = FindObjectsOfType<ArenaPortal>().Select(p => p.transform); break;
                case TutorialStep.Launch: return FindObjectOfType<MineArena.Windows.SelectLevel.LevelSelectionView>()?.TutorialTarget;
                case TutorialStep.Mine:
                    if (Progress.MiningStarted || Progress.Mined) return null;
                    var pickups = FindObjectsOfType<ItemInteractor>();
                    targets = Progress.Mined && pickups.Length > 0 ? pickups.Select(p => p.transform) : FindObjectsOfType<InteractableObject>().Where(p => p.IsMineable).Select(p => p.transform); break;
                case TutorialStep.Kill: targets = FindObjectsOfType<MobHealth>().Where(m => m.CurrentValue > 0).Select(m => m.transform); break;
                case TutorialStep.Exit:
                    var completion = FindObjectOfType<MineArena.Windows.LevelCompleteWindow>();
                    if (completion != null) return completion.TutorialTarget;
                    targets = FindObjectsOfType<LevelPortal>().Select(p => p.transform); break;
                case TutorialStep.Build:
                case TutorialStep.BuildSmith:
                    var buildingWindow = FindObjectOfType<MineArena.Windows.BuildingWindow>();
                    if (buildingWindow != null) return buildingWindow.TutorialTarget;
                    targets = FindObjectsOfType<BuildingZone>().Where(z => z.Config == (Progress.Step == TutorialStep.BuildSmith ? Smith : Workshop)).Select(z => z.transform); break;
                case TutorialStep.Craft:
                case TutorialStep.CraftArmor:
                case TutorialStep.CraftSword:
                    var crafting = FindObjectOfType<MineArena.Windows.Crafting.CraftingWindow>();
                    if (crafting != null) return crafting.TutorialTarget;
                    return HudTarget(MineArena.UI.GameUiDestination.Crafting);
            }
            return targets.OrderBy(t => (t.position - Player.Instance.transform.position).sqrMagnitude).FirstOrDefault();
        }
        private static Transform HudTarget(MineArena.UI.GameUiDestination destination) =>
            FindObjectsOfType<MineArena.UI.GameUiAction>().FirstOrDefault(a => a.Destination == destination)?.transform;

        private void UpdateSpotlight(RectTransform target)
        {
            if (_spotlight == null)
            {
                var go = new GameObject("Tutorial circular spotlight", typeof(RectTransform), typeof(TutorialSpotlightGraphic));
                go.transform.SetParent(_overlay.transform, false); go.transform.SetAsFirstSibling();
                _spotlight = go.GetComponent<TutorialSpotlightGraphic>();
                _spotlight.rectTransform.anchorMin = Vector2.zero; _spotlight.rectTransform.anchorMax = Vector2.one;
                _spotlight.rectTransform.sizeDelta = Vector2.zero;
            }
            _spotlight.gameObject.SetActive(target != null);
            if (target == null) return;
            var corners = new Vector3[4]; target.GetWorldCorners(corners);
            var min = OverlayPoint(target, corners[0]); var max = OverlayPoint(target, corners[2]);
            if (target.GetComponent<MineArena.UI.GameUiAction>() != null)
                _spotlight.Focus((min + max) * .5f, (max - min).magnitude * .5f + 10);
            else _spotlight.FocusRect(Rect.MinMaxRect(min.x - 10, min.y - 10, max.x + 10, max.y + 10));
        }
        private Vector2 OverlayPoint(Transform source, Vector3 point)
        {
            var canvas = source.GetComponentInParent<Canvas>();
            var cam = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)_overlay.transform,
                RectTransformUtility.WorldToScreenPoint(cam, point), null, out var local);
            return local;
        }
        private void KeepInventoryLessonOpen()
        {
            if (Progress.Step == TutorialStep.Potion && FindObjectOfType<MineArena.UI.InventoryWindow>() != null)
                _acknowledged = Progress.Step;
        }
        private void UpdateDragLesson(bool visible)
        {
            if (_dragHand == null)
            {
                var ghost = new GameObject("Drag item demonstration", typeof(RectTransform), typeof(Image));
                ghost.transform.SetParent(_overlay.transform, false);
                _dragItem = ghost.GetComponent<Image>(); _dragItem.raycastTarget = false; _dragItem.preserveAspect = true;
                _dragItem.rectTransform.sizeDelta = new Vector2(60, 60);
                var hand = new GameObject("Minecraft drag hand", typeof(RectTransform), typeof(TutorialHandGraphic));
                hand.transform.SetParent(_overlay.transform, false);
                _dragHand = (RectTransform)hand.transform; _dragHand.sizeDelta = new Vector2(84, 112);
                hand.GetComponent<TutorialHandGraphic>().raycastTarget = false;
            }
            bool lesson = Progress.Step == TutorialStep.EquipSword || Progress.Step == TutorialStep.Potion;
            var inventory = lesson ? FindObjectOfType<MineArena.UI.InventoryWindow>() : null;
            string id = Progress.Step == TutorialStep.Potion ? "HealingPotion" : "StoneSword";
            var cell = inventory != null ? inventory.GetComponentsInChildren<MineArena.UI.InventoryCellUI>().FirstOrDefault(c => c.Item?.Name == id) : null;
            var slots = FindObjectsOfType<Devotion.SDK.UI.PlayingInventorySlotUI>().OrderBy(s => s.Index).ToArray();
            var saved = GameRoot.PlayerProgress.InventoryProgress;
            var destination = slots.FirstOrDefault(s => string.IsNullOrEmpty(saved.GetQuickSlotItemId(s.Index))) ??
                slots.FirstOrDefault(s => saved.GetQuickSlotItemId(s.Index) != "StoneSword");
            bool show = visible && inventory != null && cell != null && destination != null && !Input.GetMouseButton(0);
            _dragHand.gameObject.SetActive(show); _dragItem.gameObject.SetActive(show);
            if (!show) return;
            var sourceRect = (RectTransform)cell.transform;
            var endRect = (RectTransform)destination.transform;
            Vector2 start = OverlayPoint(sourceRect, sourceRect.TransformPoint(sourceRect.rect.center));
            Vector2 end = OverlayPoint(endRect, endRect.TransformPoint(endRect.rect.center));
            float phase = Mathf.Repeat(Time.unscaledTime, 2.8f) / 2.8f;
            float t = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.18f, .8f, phase));
            var point = Vector2.Lerp(start, end, t) + Vector2.up * Mathf.Sin(t * Mathf.PI) * 35;
            _dragItem.sprite = cell.Item.Icon; _dragItem.color = new Color(1, 1, 1, .75f);
            _dragItem.rectTransform.anchoredPosition = point;
            _dragHand.anchoredPosition = point + new Vector2(26, -42);
            _dragHand.localScale = Vector3.one * (phase < .18f ? Mathf.Lerp(1, .88f, phase / .18f) : .88f);
        }
        private void UpdateIllustration(TutorialStep step)
        {
            _illustration.sprite = StepIllustration(step);
            var item = GameRoot.GameConfig?.ItemDatabase.GetItemConfig(step == TutorialStep.Mine ? "WoodOak" : step == TutorialStep.Craft ? "Planks" : "");
            bool block = item is StackableItemConfig resource && resource.BlockStyleIcon;
            if (block && _blockIllustration == null)
            {
                var prefab = _theme != null ? _theme.ResourceIcon : null;
                if (prefab != null) {
                    _blockIllustration = Instantiate(prefab, _illustration.transform);
                    var rect = (RectTransform)_blockIllustration.transform;
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                    foreach (var graphic in _blockIllustration.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
                }
            }
            _illustration.enabled = !block || _blockIllustration == null;
            if (_blockIllustration != null) {
                _blockIllustration.gameObject.SetActive(block);
                if (block) _blockIllustration.SetResource((StackableItemConfig)item);
            }
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
            _gift = button.GetComponent<Button>(); _gift.onClick.AddListener(() => { if (CraftStep) MineArena.Windows.Crafting.CraftingWindow.Open(CraftBuilding); });
            button.SetActive(false);
            Label(button.transform, "Label", 20, new Vector2(10, -4), new Vector2(205, 30)).text = "Забрать подарок";
            _arrow = Label(_overlay.transform, "Target arrow", 27, Vector2.zero, new Vector2(100, 100)); _arrow.alignment = TextAlignmentOptions.Center; _arrow.color = new Color(1, .78f, .18f); _arrow.outlineWidth = .2f; _arrow.outlineColor = Color.black;
            ((RectTransform)_arrow.transform).pivot = new Vector2(.5f, .5f);
            var pointer = new GameObject("Pointer", typeof(RectTransform), typeof(TutorialArrowGraphic)); pointer.transform.SetParent(_arrow.transform, false);
            var pointerRect = (RectTransform)pointer.transform; pointerRect.anchorMin = pointerRect.anchorMax = new Vector2(.5f, .5f); pointerRect.sizeDelta = new Vector2(76, 88); _pointer = pointerRect;
            pointer.GetComponent<TutorialArrowGraphic>().raycastTarget = false;
            _shade = new GameObject("Popup backdrop", typeof(RectTransform), typeof(Image)); _shade.transform.SetParent(_overlay.transform, false);
            var shadeRect = (RectTransform)_shade.transform; shadeRect.anchorMin = Vector2.zero; shadeRect.anchorMax = Vector2.one; shadeRect.sizeDelta = Vector2.zero;
            _shade.GetComponent<Image>().color = new Color(.025f, .04f, .09f, .65f);
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
            if (step == TutorialStep.Gift) return GameRoot.GameConfig?.ItemDatabase.GetItemConfig("IronChestplate")?.Icon;
            if (step == TutorialStep.Rewards || step == TutorialStep.PlaytimeRewards || step == TutorialStep.FortuneWheel || step == TutorialStep.WorkshopButton)
            {
                var destination = step == TutorialStep.Rewards ? MineArena.UI.GameUiDestination.Daily :
                    step == TutorialStep.PlaytimeRewards ? MineArena.UI.GameUiDestination.Playtime :
                    step == TutorialStep.FortuneWheel ? MineArena.UI.GameUiDestination.Wheel : MineArena.UI.GameUiDestination.Crafting;
                return FindObjectsOfType<MineArena.UI.GameUiAction>(true).FirstOrDefault(a => a.Destination == destination)?.transform.Find("Icon")?.GetComponent<Image>()?.sprite;
            }
            if (step == TutorialStep.BuildSmith) return Smith?.GetLevelByNumber(1)?.Preview;
            if (step == TutorialStep.CraftArmor || step == TutorialStep.EquipArmor) return GameRoot.GameConfig?.ItemDatabase.GetItemConfig("IronChestplate")?.Icon;
            if (step == TutorialStep.CraftSword || step == TutorialStep.EquipSword) return GameRoot.GameConfig?.ItemDatabase.GetItemConfig("StoneSword")?.Icon;
            if (step == TutorialStep.Potion) return GameRoot.GameConfig?.ItemDatabase.GetItemConfig("HealingPotion")?.Icon;
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

}
