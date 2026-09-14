using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.DailyReward;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Devotion.SDK.UI
{
    public class DailyGiftWIndow : BaseWindow
    {
        [System.Serializable]
        public sealed class RewardSlotView
        {
            [SerializeField] private GameObject root;
            [SerializeField] private Image background;
            [SerializeField] private Image icon;
            [SerializeField] private MineArena.UI.ResourceIcon blockIcon;
            [SerializeField] private TMP_Text dayText;
            [SerializeField] private TMP_Text amountText;
            [SerializeField] private TMP_Text statusText;
            [SerializeField] private TMP_Text itemText;
            private Image claimedShade;

            public void ResolveReferences()
            {
                if (root == null)
                    return;

                var rootTransform = root.transform;

                if (background == null)
                    background = root.GetComponent<Image>();

                if (icon == null)
                    icon = FindImage(rootTransform, "Icon");

                if (dayText == null)
                    dayText = FindText(rootTransform, "DayText") ?? FindText(rootTransform, "Day");

                if (amountText == null)
                    amountText = FindText(rootTransform, "AmountText") ?? FindText(rootTransform, "Amount");
            }

            public void ResolveFrom(Transform slotRoot)
            {
                if (slotRoot == null)
                    return;

                if (root == null)
                    root = slotRoot.gameObject;

                ResolveReferences();
            }

            public void Refresh(int index, DailyRewardConfig config, int currentRewardIndex, Color claimedColor, Color currentColor, Color futureColor, bool canClaim, bool claimed, string availableAt)
            {
                var active = config != null && index < config.RewardsCount;
                if (root != null)
                    root.SetActive(active);

                if (!active)
                    return;

                var reward = config.GetReward(index);

                if (background != null)
                    background.color = GetStateColor(index, currentRewardIndex, claimedColor, currentColor, futureColor);

                if (dayText != null)
                    dayText.text = $"День {index + 1}";
                if (itemText != null) itemText.text = reward?.DisplayName;
                if (statusText != null) statusText.text = claimed ? "ПОЛУЧЕНО" : index == currentRewardIndex && canClaim ? "СЕГОДНЯ" : availableAt;

                if (amountText != null)
                    amountText.text = reward != null ? $"x{reward.Amount}" : string.Empty;

                if (icon != null)
                {
                    icon.sprite = reward?.Icon;
                    icon.enabled = icon.sprite != null;
                }
                ShowBlock(icon, blockIcon, reward?.ItemConfig);
                if (claimedShade == null && root != null)
                {
                    var shade = new GameObject("ClaimedShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    shade.transform.SetParent(root.transform, false);
                    claimedShade = shade.GetComponent<Image>();
                    claimedShade.color = new Color(0f, 0f, 0f, 0.55f);
                    claimedShade.raycastTarget = false;
                    var rect = claimedShade.rectTransform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                }
                if (claimedShade != null) claimedShade.gameObject.SetActive(claimed);
                if (claimed && statusText != null) statusText.text = "ПОЛУЧЕНО";
            }

            private static Color GetStateColor(int index, int currentRewardIndex, Color claimedColor, Color currentColor, Color futureColor)
            {
                if (index < currentRewardIndex)
                    return claimedColor;

                if (index == currentRewardIndex)
                    return currentColor;

                return futureColor;
            }
        }

        [SerializeField] private Transform rewardsRoot;
        [SerializeField] private RewardSlotView[] rewardSlots;
        [SerializeField] private Color claimedSlotColor = new(0.25f, 0.62f, 0.33f, 1f);
        [SerializeField] private Color currentSlotColor = new(0.96f, 0.78f, 0.25f, 1f);
        [SerializeField] private Color futureSlotColor = new(0.18f, 0.2f, 0.24f, 1f);
        [SerializeField] private Image rewardIcon;
        [SerializeField] private MineArena.UI.ResourceIcon rewardBlockIcon;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text rewardText;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button closeButton;

        private DailyRewardManager manager;
        private DailyRewardConfig config;
        private int rewardIndex = -1;
        private bool referencesResolved;
        private bool claimButtonBound;
        private bool closeButtonBound;
        private float nextRefresh;

        private void Update()
        {
            if (Time.unscaledTime < nextRefresh) return;
            nextRefresh = Time.unscaledTime + 0.5f;
            Refresh();
        }

        public void Setup(DailyRewardManager rewardManager, DailyRewardConfig rewardsConfig, int currentRewardIndex)
        {
            manager = rewardManager;
            config = rewardsConfig;
            rewardIndex = currentRewardIndex;

            ResolveExistingReferences();
            BindButtons();
            Refresh();
        }

        public override void CloseWindow()
        {
            if (GameRoot.UIManager != null)
                GameRoot.UIManager.CloseWindow<DailyGiftWIndow>();
            else
                gameObject.SetActive(false);
        }

        private void Awake()
        {
            ResolveExistingReferences();
            BindButtons();
        }

        private void OnEnable()
        {
            ResolveExistingReferences();
            BindButtons();
            Refresh();
        }

        private void OnDestroy()
        {
            if (claimButton != null)
                claimButton.onClick.RemoveListener(HandleClaimClicked);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(CloseWindow);
        }

        private void HandleClaimClicked()
        {
            if (manager == null)
                return;

            if (manager.TryClaimCurrentReward())
                CloseWindow();
        }

        private void Refresh()
        {
            if (manager != null) rewardIndex = manager.CurrentRewardIndex;
            if (titleText != null)
                titleText.text = "Ежедневный подарок";

            if (config == null || rewardIndex < 0)
            {
                if (rewardText != null)
                    rewardText.text = string.Empty;

                if (rewardIcon != null)
                    rewardIcon.enabled = false;
                if (rewardBlockIcon != null) rewardBlockIcon.gameObject.SetActive(false);

                if (claimButton != null)
                    claimButton.interactable = false;

                RefreshRewardSlots();
                return;
            }

            var reward = config.GetReward(rewardIndex);
            if (rewardText != null)
                rewardText.text = reward != null ? $"{reward.DisplayName} x{reward.Amount}" : string.Empty;

            if (rewardIcon != null)
            {
                rewardIcon.sprite = reward?.Icon;
                rewardIcon.enabled = rewardIcon.sprite != null;
            }
            ShowBlock(rewardIcon, rewardBlockIcon, reward?.ItemConfig);

            if (claimButton != null)
                claimButton.interactable = reward != null && reward.IsValid && manager != null && manager.CanClaim;

            RefreshRewardSlots();
        }

        private static void ShowBlock(Image flat, MineArena.UI.ResourceIcon block, MineArena.Items.ItemConfig item)
        {
            bool cube = item != null && item.BlockStyleIcon && item is MineArena.Items.StackableItemConfig && block != null;
            if (block != null)
            {
                block.gameObject.SetActive(cube);
                if (cube) block.SetResource((MineArena.Items.StackableItemConfig)item);
            }
            if (flat != null) flat.gameObject.SetActive(!cube);
        }

        private void RefreshRewardSlots()
        {
            if (rewardSlots == null)
                return;

            for (int i = 0; i < rewardSlots.Length; i++)
            {
                var slot = rewardSlots[i];
                if (slot == null)
                    continue;

                string availableAt = manager != null
                    ? "Через " + PlaytimeGiftWindow.FormatRemaining((float)(manager.GetRewardAvailableAtLocal(i).ToUniversalTime() - System.DateTime.UtcNow).TotalSeconds)
                    : string.Empty;
                slot.Refresh(i, config, rewardIndex, claimedSlotColor, currentSlotColor, futureSlotColor, manager != null && manager.CanClaim, manager != null && manager.IsRewardClaimed(i), availableAt);
            }
        }

        private void ResolveExistingReferences()
        {
            if (referencesResolved)
                return;

            if (claimButton == null)
                claimButton = FindButton("ClaimButton") ?? GetComponentInChildren<Button>(true);

            if (closeButton == null)
                closeButton = FindButton("CloseButton");

            if (titleText == null)
                titleText = FindText(transform, "Title");

            if (rewardText == null)
                rewardText = FindText(transform, "RewardText");

            if (rewardIcon == null)
                rewardIcon = FindImage(transform, "RewardIcon") ?? FindImage(transform, "Reward");

            if (rewardsRoot == null)
                rewardsRoot = FindChild(transform, "Rewards") ?? FindChild(transform, "RewardsRoot");

            ResolveRewardSlots();
            referencesResolved = true;
        }

        private void ResolveRewardSlots()
        {
            if (rewardSlots != null && rewardSlots.Length > 0)
            {
                for (int i = 0; i < rewardSlots.Length; i++)
                    rewardSlots[i]?.ResolveReferences();

                return;
            }

            if (rewardsRoot == null || rewardsRoot.childCount == 0)
                return;

            rewardSlots = new RewardSlotView[rewardsRoot.childCount];
            for (int i = 0; i < rewardsRoot.childCount; i++)
            {
                var slot = new RewardSlotView();
                slot.ResolveFrom(rewardsRoot.GetChild(i));
                rewardSlots[i] = slot;
            }
        }

        private void BindButtons()
        {
            if (claimButton != null && !claimButtonBound)
            {
                claimButton.onClick.AddListener(HandleClaimClicked);
                claimButtonBound = true;
            }

            if (closeButton != null && !closeButtonBound)
            {
                closeButton.onClick.AddListener(CloseWindow);
                closeButtonBound = true;
            }
        }

        private static TMP_Text FindText(Transform root, string childName)
        {
            var child = FindChild(root, childName);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        private static Image FindImage(Transform root, string childName)
        {
            var child = FindChild(root, childName);
            return child != null ? child.GetComponent<Image>() : null;
        }

        private Button FindButton(string childName)
        {
            var child = FindChild(transform, childName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root == null)
                return null;

            foreach (Transform child in root)
            {
                if (child.name == childName)
                    return child;

                var nested = FindChild(child, childName);
                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
