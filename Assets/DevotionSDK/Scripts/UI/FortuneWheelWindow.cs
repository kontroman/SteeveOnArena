using System;
using System.Collections.Generic;
using System.Linq;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.Services.SaveSystem.Progress;
using DG.Tweening;
using MineArena.Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Devotion.SDK.UI
{
    public class FortuneWheelWindow : BaseWindow
    {
        private const int DefaultStartSpins = 1;
        private const int CompensationDiamonds = 25;
        private const string CompensationItemId = "DiamondOre";

        [Header("UI")]
        [SerializeField] private Transform _wheelTransform;
        [SerializeField] private Button _spinButton;
        [SerializeField] private TMP_Text _spinButtonText;
        [SerializeField] private TMP_Text _freeSpinTimerText;
        [SerializeField] private List<Button> _purchaseButtons = new();

        [Header("Ad Mode")]
        [SerializeField] private Image _spinButtonModeIcon;
        [SerializeField] private Sprite _spinIcon;
        [SerializeField] private Sprite _adIcon;

        [Header("Rewards")]
        [SerializeField] private List<FortuneWheelReward> _rewards = new();
        [SerializeField, Range(0f, 0.45f)] private float _nearMissOffsetPercent = 0.3f;

        [Header("Spin Animation")]
        [SerializeField] private float _spinDuration = 4f;
        [SerializeField] private int _minFullRotations = 4;
        [SerializeField] private int _maxFullRotations = 6;
        [SerializeField] private Ease _spinEase = Ease.OutQuart;

        [Header("Pointer Animation")]
        [SerializeField] private Transform _pointerTransform;
        [SerializeField] private float _pointerMaxAngle = 18f;
        [SerializeField] private float _pointerSnapDuration = 0.06f;
        [SerializeField] private float _pointerReturnDuration = 0.08f;

        private Tween _spinTween;
        private Tween _pointerTween;
        private FortuneWheelReward _pendingSectorReward;
        private FortuneWheelReward _pendingResolvedReward;
        private bool _isSpinning;
        private bool _isRewardAdLoading;
        private bool _buttonsBound;
        private int _lastPointerSectorStep;
        private float _nextTimerRefreshTime;

        public override void CloseWindow()
        {
            if (_isSpinning)
                return;

            GameRoot.UIManager.CloseWindow<FortuneWheelWindow>();
        }

        private void Awake()
        {
            BindReferences();
            BindButtons();
            EnsureRewards();
        }

        private void OnEnable()
        {
            RefreshFreeSpinState();
            RefreshUI();
        }

        private void OnDisable()
        {
            _spinTween?.Kill();
            _pointerTween?.Kill();
            _isSpinning = false;
            _isRewardAdLoading = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                CloseWindow();

            if (Time.unscaledTime < _nextTimerRefreshTime)
                return;

            _nextTimerRefreshTime = Time.unscaledTime + 0.25f;
            RefreshFreeSpinState(false);
            RefreshUI();
        }

        public void AddFortuneSpins(int amount)
        {
            if (GameRoot.PlayerProgress == null)
                return;

            GameRoot.PlayerProgress.LuckyWheelProgress.AddFortuneSpins(amount);
            RefreshFreeSpinState(false);
            RefreshUI();
        }

        public void BuyFortuneSpin1() => PurchaseFortuneSpins(1);
        public void BuyFortuneSpin3() => PurchaseFortuneSpins(3);
        public void BuyFortuneSpin5() => PurchaseFortuneSpins(5);
        public void BuyFortuneSpin10() => PurchaseFortuneSpins(10);

        private void BindReferences()
        {
            if (_wheelTransform == null)
                _wheelTransform = FindChild("BackgroundWheel") ?? FindChild("WheelContainer");

            if (_pointerTransform == null)
                _pointerTransform = FindChild("Pointer");

            if (_spinButton == null)
                _spinButton = GetComponentsInChildren<Button>(true).FirstOrDefault(button => button.name == "Start");

            if (_spinButtonText == null && _spinButton != null)
                _spinButtonText = _spinButton.GetComponentInChildren<TMP_Text>(true);

            if (_freeSpinTimerText == null)
                _freeSpinTimerText = FindChild("FreeSpinText")?.GetComponent<TMP_Text>();

            if (_purchaseButtons == null)
                _purchaseButtons = new List<Button>();

            _purchaseButtons.RemoveAll(button => button == null);

            if (_purchaseButtons.Count == 0)
            {
                _purchaseButtons = GetComponentsInChildren<Button>(true)
                    .Where(button => button != null && button != _spinButton)
                    .ToList();
            }
        }

        private void BindButtons()
        {
            if (_buttonsBound)
                return;

            _buttonsBound = true;

            if (_spinButton != null)
                _spinButton.onClick.AddListener(OnSpinButtonClicked);

            var fallbackAmounts = new[] { 1, 3, 5, 10 };
            var fallbackIndex = 0;

            foreach (var button in _purchaseButtons)
            {
                if (button == null)
                    continue;

                var amount = ParseFirstNumber(button.GetComponentInChildren<TMP_Text>(true)?.text);
                if (amount <= 0 && fallbackIndex < fallbackAmounts.Length)
                    amount = fallbackAmounts[fallbackIndex++];

                if (amount <= 0)
                    continue;

                button.onClick.AddListener(() => PurchaseFortuneSpins(amount));
            }
        }

        private void EnsureRewards()
        {
            _rewards ??= new List<FortuneWheelReward>();
            _rewards.RemoveAll(reward => reward == null || !reward.IsValid);

            if (_rewards.Count > 0)
                return;

            _rewards = new List<FortuneWheelReward>
            {
                new("NetheriteHelmet", "Незеритовый шлем", FortuneWheelRewardType.UniqueItem, 2, 1, FortuneWheelRewardRarity.Legendary,
                    new[] { "NetheriteHelmet", "NetheriteChestplate", "NetheriteLeggings", "NetheriteBoots", "NetheritheSword", "NetherithePickaxe" }),
                new("IronOre", "Железо x15", FortuneWheelRewardType.StackableItem, 65, 15, FortuneWheelRewardRarity.Common),
                new("DiamondSword", "Алмазный меч", FortuneWheelRewardType.UniqueItem, 4, 1, FortuneWheelRewardRarity.Epic,
                    new[] { "DiamondSword", "DiamondPickaxe" }),
                new("GoldOre", "Золото x10", FortuneWheelRewardType.StackableItem, 15, 10, FortuneWheelRewardRarity.Common),
                new("Diamond Armor", "Алмазная броня", FortuneWheelRewardType.UniqueItem, 4, 1, FortuneWheelRewardRarity.Epic,
                    new[] { "Diamond Helmet", "Diamond Armor", "Diamond Leggings", "Diamond Boots" }),
                new("DiamondOre", "Алмазы x5", FortuneWheelRewardType.StackableItem, 10, 5, FortuneWheelRewardRarity.Uncommon)
            };
        }

        private void RefreshFreeSpinState(bool saveSchedule = true)
        {
            if (GameRoot.PlayerProgress == null)
                return;

            var progress = GameRoot.PlayerProgress.LuckyWheelProgress;
            var nowUtc = DateTime.UtcNow;

            progress.InitializeFortuneWheel(nowUtc, DefaultStartSpins);

            if (progress.FortuneSpins > 0)
                return;

            if (progress.NextFreeSpinUtcTicks <= 0)
            {
                if (saveSchedule)
                    progress.ScheduleNextFreeSpin(nowUtc.AddMinutes(GetFreeSpinCooldownMinutes()));

                return;
            }

            if (nowUtc.Ticks >= progress.NextFreeSpinUtcTicks)
                progress.GrantFreeSpin(nowUtc);
        }

        private void RefreshUI()
        {
            var progress = GameRoot.PlayerProgress?.LuckyWheelProgress;
            var spins = progress?.FortuneSpins ?? 0;
            var hasSpins = spins > 0;
            var controlsInteractable = !_isSpinning && !_isRewardAdLoading;

            if (_freeSpinTimerText != null)
                _freeSpinTimerText.text = BuildFreeSpinTimerText(progress, hasSpins);

            if (_spinButtonText != null)
                _spinButtonText.text = hasSpins ? "Крутить!" : "Смотреть рекламу\n+1 спин";

            if (_spinButtonModeIcon != null)
            {
                _spinButtonModeIcon.sprite = hasSpins ? _spinIcon : _adIcon;
                _spinButtonModeIcon.enabled = _spinButtonModeIcon.sprite != null;
            }

            if (_spinButton != null)
                _spinButton.interactable = controlsInteractable;

            foreach (var button in _purchaseButtons)
            {
                if (button != null)
                    button.interactable = controlsInteractable;
            }
        }

        private string BuildFreeSpinTimerText(LuckyWheelProgress progress, bool hasSpins)
        {
            if (hasSpins)
                return "Бесплатный спин доступен";

            if (progress == null || progress.NextFreeSpinUtcTicks <= 0)
                return string.Empty;

            var nextFreeSpinUtc = new DateTime(progress.NextFreeSpinUtcTicks, DateTimeKind.Utc);
            var remaining = nextFreeSpinUtc - DateTime.UtcNow;

            if (remaining <= TimeSpan.Zero)
                return "Бесплатный спин доступен";

            var hours = (int)remaining.TotalHours;
            var time = hours > 0
                ? $"{hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00}"
                : $"{remaining.Minutes:00}:{remaining.Seconds:00}";

            return $"До бесплатного спина: {time}";
        }

        private void OnSpinButtonClicked()
        {
            if (_isSpinning || _isRewardAdLoading)
                return;

            RefreshFreeSpinState();

            if (GameRoot.PlayerProgress.LuckyWheelProgress.FortuneSpins <= 0)
            {
                RequestRewardedSpin();
                return;
            }

            StartSpin();
        }

        private void PurchaseFortuneSpins(int amount)
        {
            if (_isSpinning || _isRewardAdLoading)
                return;

            // TODO: connect project IAP/currency price validation here when prices are defined.
            AddFortuneSpins(amount);
        }

        private void RequestRewardedSpin()
        {
            _isRewardAdLoading = true;
            RefreshUI();

            // TODO: assign _adIcon in the prefab and replace this placeholder with real rewarded ads callback.
            OnRewardedSpinAdFinished(true);
        }

        private void OnRewardedSpinAdFinished(bool rewarded)
        {
            _isRewardAdLoading = false;

            if (rewarded)
                AddFortuneSpins(1);
            else
                RefreshUI();
        }

        private void StartSpin()
        {
            EnsureRewards();

            if (_rewards.Count == 0)
            {
                Debug.LogError("[FortuneWheelWindow] Rewards list is empty.");
                return;
            }

            var progress = GameRoot.PlayerProgress.LuckyWheelProgress;
            if (!progress.TryConsumeFortuneSpin())
            {
                RefreshUI();
                return;
            }

            if (progress.FortuneSpins <= 0)
                progress.ScheduleNextFreeSpin(DateTime.UtcNow.AddMinutes(GetFreeSpinCooldownMinutes()));

            var blockedRewardId = progress.FortuneRewardStreak >= 2 ? progress.LastFortuneRewardId : null;
            var rewardIndex = RollRewardIndex(blockedRewardId);
            _pendingSectorReward = _rewards[rewardIndex];
            _pendingResolvedReward = ResolveRewardDuplicate(_pendingSectorReward);
            progress.RegisterFortuneRewardRoll(_pendingSectorReward.Id);
            _isSpinning = true;
            RefreshUI();

            if (_wheelTransform == null)
            {
                CompleteSpin();
                return;
            }

            StartSpinAnimation(rewardIndex);
        }

        private int RollRewardIndex(string blockedRewardId = null)
        {
            var availableRewards = _rewards
                .Select((reward, index) => new { Reward = reward, Index = index })
                .Where(entry => entry.Reward != null
                    && entry.Reward.Weight > 0
                    && entry.Reward.Id != blockedRewardId)
                .ToList();

            if (availableRewards.Count == 0)
            {
                availableRewards = _rewards
                    .Select((reward, index) => new { Reward = reward, Index = index })
                    .Where(entry => entry.Reward != null && entry.Reward.Weight > 0)
                    .ToList();
            }

            var totalWeight = availableRewards.Sum(entry => Mathf.Max(0, entry.Reward.Weight));
            var randomWeight = UnityEngine.Random.Range(0, totalWeight);
            var accumulated = 0;

            foreach (var entry in availableRewards)
            {
                accumulated += Mathf.Max(0, entry.Reward.Weight);

                if (randomWeight < accumulated)
                    return entry.Index;
            }

            return _rewards.Count - 1;
        }

        private FortuneWheelReward ResolveRewardDuplicate(FortuneWheelReward reward)
        {
            if (reward == null || !reward.IsUnique)
                return reward;

            var inventory = GameRoot.GetManager<InventoryManager>();
            if (inventory == null || !inventory.HasItem(reward.Id))
                return reward;

            foreach (var replacementId in reward.FallbackGroup)
            {
                if (string.IsNullOrWhiteSpace(replacementId) || inventory.HasItem(replacementId))
                    continue;

                return reward.CreateReplacement(replacementId);
            }

            return FortuneWheelReward.CreateCompensation(CompensationItemId, CompensationDiamonds);
        }

        private void StartSpinAnimation(int rewardIndex)
        {
            _spinTween?.Kill();
            _pointerTween?.Kill();
            _lastPointerSectorStep = -1;

            var sectorAngle = 360f / _rewards.Count;
            var targetAngle = rewardIndex * sectorAngle + CalculateNearMissOffset(rewardIndex, sectorAngle);
            var startAngle = _wheelTransform.localEulerAngles.z;
            var rotationCount = UnityEngine.Random.Range(
                Mathf.Min(_minFullRotations, _maxFullRotations),
                Mathf.Max(_minFullRotations, _maxFullRotations) + 1);
            var targetDelta = Mathf.Repeat(targetAngle - startAngle, 360f);
            var endAngle = startAngle + rotationCount * 360f + targetDelta;

            _spinTween = _wheelTransform
                .DOLocalRotate(new Vector3(0f, 0f, endAngle), _spinDuration, RotateMode.FastBeyond360)
                .SetEase(_spinEase)
                .SetUpdate(true)
                .OnUpdate(() => UpdatePointer(_spinTween.ElapsedPercentage()))
                .OnComplete(CompleteSpin);
        }

        private float CalculateNearMissOffset(int rewardIndex, float sectorAngle)
        {
            var reward = _rewards[rewardIndex];
            var previousIndex = (rewardIndex - 1 + _rewards.Count) % _rewards.Count;
            var nextIndex = (rewardIndex + 1) % _rewards.Count;
            var safeOffset = sectorAngle * Mathf.Clamp(_nearMissOffsetPercent, 0f, 0.45f);

            if (_rewards[nextIndex].Rarity > reward.Rarity)
                return safeOffset;

            if (_rewards[previousIndex].Rarity > reward.Rarity)
                return -safeOffset;

            return 0f;
        }

        private void UpdatePointer(float spinProgress)
        {
            if (_pointerTransform == null || _rewards.Count == 0)
                return;

            var sectorAngle = 360f / _rewards.Count;
            var sectorStep = Mathf.FloorToInt(_wheelTransform.localEulerAngles.z / sectorAngle);

            if (sectorStep == _lastPointerSectorStep)
                return;

            _lastPointerSectorStep = sectorStep;
            var amplitude = Mathf.Lerp(_pointerMaxAngle, _pointerMaxAngle * 0.25f, Mathf.Clamp01(spinProgress));
            var returnDuration = Mathf.Lerp(_pointerReturnDuration * 0.6f, _pointerReturnDuration * 1.4f, Mathf.Clamp01(spinProgress));

            _pointerTween?.Kill();
            _pointerTween = DOTween.Sequence()
                .SetUpdate(true)
                .Append(_pointerTransform.DOLocalRotate(new Vector3(0f, 0f, -amplitude), _pointerSnapDuration).SetEase(Ease.OutQuad))
                .Append(_pointerTransform.DOLocalRotate(Vector3.zero, returnDuration).SetEase(Ease.OutQuad));
        }

        private void CompleteSpin()
        {
            LogSpinResult(_pendingSectorReward, _pendingResolvedReward);
            GiveReward(_pendingResolvedReward);
            _pendingSectorReward = null;
            _pendingResolvedReward = null;
            _isSpinning = false;

            if (_pointerTransform != null)
                _pointerTransform.DOLocalRotate(Vector3.zero, _pointerReturnDuration).SetUpdate(true);

            RefreshFreeSpinState(false);
            RefreshUI();
        }

        private void GiveReward(FortuneWheelReward reward)
        {
            if (reward == null)
                return;

            var inventory = GameRoot.GetManager<InventoryManager>();
            if (inventory == null)
                return;

            inventory.AddItemById(reward.Id, reward.Amount);
            GameRoot.PlayerProgress.Save();
            Debug.Log($"[FortuneWheelWindow] Reward issued: {reward.DisplayName} x{reward.Amount}");
        }

        private void LogSpinResult(FortuneWheelReward sectorReward, FortuneWheelReward issuedReward)
        {
            if (sectorReward == null)
                return;

            if (issuedReward != null && issuedReward.Id != sectorReward.Id)
            {
                Debug.Log($"[FortuneWheelWindow] Dropped sector: {sectorReward.DisplayName} ({sectorReward.Id}). Duplicate resolved to: {issuedReward.DisplayName} x{issuedReward.Amount} ({issuedReward.Id}).");
                return;
            }

            Debug.Log($"[FortuneWheelWindow] Dropped sector: {sectorReward.DisplayName} x{sectorReward.Amount} ({sectorReward.Id}).");
        }

        private int GetFreeSpinCooldownMinutes()
        {
            return GameRoot.GameConfig != null ? GameRoot.GameConfig.FreeFortuneSpinCooldownMinutes : 30;
        }

        private Transform FindChild(string childName)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }

        private static int ParseFirstNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            var number = 0;
            var found = false;

            foreach (var character in value)
            {
                if (char.IsDigit(character))
                {
                    found = true;
                    number = number * 10 + character - '0';
                    continue;
                }

                if (found)
                    break;
            }

            return number;
        }
    }

    public enum FortuneWheelRewardType
    {
        StackableItem,
        UniqueItem
    }

    public enum FortuneWheelRewardRarity
    {
        Common,
        Uncommon,
        Epic,
        Legendary
    }

    [Serializable]
    public sealed class FortuneWheelReward
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private FortuneWheelRewardType type;
        [SerializeField, Min(1)] private int weight = 1;
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private Sprite icon;
        [SerializeField] private Transform sector;
        [SerializeField] private FortuneWheelRewardRarity rarity;
        [SerializeField] private List<string> fallbackGroup = new();

        public string Id => id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? id : displayName;
        public FortuneWheelRewardType Type => type;
        public int Weight => weight;
        public int Amount => Mathf.Max(1, amount);
        public Sprite Icon => icon;
        public Transform Sector => sector;
        public FortuneWheelRewardRarity Rarity => rarity;
        public IReadOnlyList<string> FallbackGroup => fallbackGroup;
        public bool IsUnique => type == FortuneWheelRewardType.UniqueItem;
        public bool IsValid => !string.IsNullOrWhiteSpace(id) && weight > 0;

        public FortuneWheelReward(string id, string displayName, FortuneWheelRewardType type, int weight, int amount, FortuneWheelRewardRarity rarity, IEnumerable<string> fallbackGroup = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.type = type;
            this.weight = weight;
            this.amount = amount;
            this.rarity = rarity;
            this.fallbackGroup = fallbackGroup?.Where(itemId => !string.IsNullOrWhiteSpace(itemId)).ToList() ?? new List<string>();
        }

        public FortuneWheelReward CreateReplacement(string replacementId)
        {
            return new FortuneWheelReward(replacementId, replacementId, type, weight, 1, rarity, fallbackGroup);
        }

        public static FortuneWheelReward CreateCompensation(string itemId, int amount)
        {
            return new FortuneWheelReward(itemId, $"Алмазы x{amount}", FortuneWheelRewardType.StackableItem, 1, amount, FortuneWheelRewardRarity.Uncommon);
        }
    }
}
