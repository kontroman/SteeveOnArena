using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Devotion.SDK.Controllers;
using Devotion.SDK.DailyReward;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.SaveSystem;
using Devotion.SDK.Services.SaveSystem.Progress;
using Devotion.SDK.UI;
using MineArena.Managers;
using MineArena.Structs;
using MineArena.UI;
using MineArena.Windows.SelectLevel;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static partial class GameUiBuilder
    {
        private static readonly string[] GiftColors = { "CB6593", "657FC3", "CC963D", "55A596", "986EC1", "D27A51", "BD9052" };
        private static void PartyFrame(RectTransform frame, string color)
        {
            frame.GetComponent<Image>().sprite = AccentSprite("party-frame", "FFF5E8", "BD86C4");
            Ribbon(frame, color, IconArt("gift"));
            for (int i = 0; i < 22; i++)
            {
                var confetti = Image("Confetti" + i, frame, null);
                confetti.color = C(GiftColors[i % GiftColors.Length]); confetti.raycastTarget = false;
                float x = 22 + (i * 173) % (int)(frame.sizeDelta.x - 50);
                Box(confetti.rectTransform, x, i % 2 == 0 ? 104 : frame.sizeDelta.y - 24, i % 3 == 0 ? 10 : 6, 7);
                confetti.rectTransform.localRotation = Quaternion.Euler(0, 0, i * 29 % 90);
            }
        }

        private static void DecorateDay(RectTransform slot, int index)
        {
            slot.GetComponent<Image>().sprite = AccentSprite("party-day-" + index, "FFF8EB", GiftColors[index % GiftColors.Length]);
            var strip = Image("GiftRibbon", slot, AccentSprite("party-strip-" + index, GiftColors[index % GiftColors.Length], GiftColors[index % GiftColors.Length]));
            Box(strip.rectTransform, 3, 3, 144, 44); strip.transform.SetAsFirstSibling(); strip.raycastTarget = false;
            var gift = Image("GiftSeal", slot, IconArt("gift")); Box(gift.rectTransform, 64, 218, 22, 22); gift.raycastTarget = false;
        }

        private static void BuildPlaytimeCards(PlaytimeRewardsConfig config)
        {
            var root = Edit<PlaytimeGiftWindow>(UI + "PlaytimeGiftWindow.prefab");
            int count = config.Rewards.Count, columns = Mathf.Clamp(count, 1, 4), rows = Mathf.CeilToInt(count / (float)columns);
            float width = Mathf.Max(900, columns * 340 + 88), height = 380 + rows * 330;
            var frame = Window(root, "Подарки за время", width, height);
            PartyFrame(frame, "7770B6");
            Text("Intro", frame, "Больше времени в игре — больше подарков!", 25, 36, 130, width - 72, 40);
            var grid = Rect("AllRewards", frame); Box(grid, 44, 194, width - 88, rows * 330);
            var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2((width - 88 - (columns - 1) * 18) / columns, 308);
            layout.spacing = new Vector2(18, 22); layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount; layout.constraintCount = columns;
            var window = root.GetComponent<PlaytimeGiftWindow>();
            var data = new SerializedObject(window); var cards = data.FindProperty("cards"); cards.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                float w = layout.cellSize.x;
                var card = Panel("Reward" + i, grid, "card");
                card.GetComponent<Image>().sprite = AccentSprite("playtime-card-" + i, "FFF8EB", GiftColors[i % GiftColors.Length]);
                var band = Image("Ribbon", card, AccentSprite("playtime-band-" + i, GiftColors[i % GiftColors.Length], GiftColors[i % GiftColors.Length])); Box(band.rectTransform, 3, 3, w - 6, 48);
                var unlock = Text("UnlockAt", card, "", 21, 12, 11, w - 24, 34, true); unlock.alignment = TextAlignmentOptions.Center; unlock.color = Color.white;
                var chipRoot = Rect("RewardItem", card); Box(chipRoot, 12, 58, w - 24, 152);
                var chip = chipRoot.gameObject.AddComponent<LevelResourceChip>();
                var icon = Image("Icon", chipRoot, null); Box(icon.rectTransform, (w - 24 - 88) / 2, 0, 88, 88); icon.preserveAspect = true;
                AddBlock(chipRoot, Vector2.zero, 0.88f); var block = chipRoot.GetComponentInChildren<ResourceIcon>(true);
                var blockRect = (RectTransform)block.transform; blockRect.anchorMin = blockRect.anchorMax = new Vector2(0.5f, 1); blockRect.anchoredPosition = new Vector2(0, -44);
                var name = Text("Name", chipRoot, "", 20, 0, 90, w - 24, 30); name.alignment = TextAlignmentOptions.Center;
                var amount = Text("Amount", chipRoot, "", 25, 0, 120, w - 24, 32, true); amount.alignment = TextAlignmentOptions.Center;
                Set(chip, "icon", icon, "blockIcon", block, "label", name, "amount", amount);
                chip.Bind(config.Rewards[i].Item, config.Rewards[i].Amount);
                var countdown = Text("Countdown", card, "", 24, 10, 212, w - 20, 34, true); countdown.alignment = TextAlignmentOptions.Center;
                var track = Image("ProgressTrack", card, S("slot")); Box(track.rectTransform, 18, 253, w - 36, 12); track.color = C("E8DFEF");
                var fill = Image("Progress", track.transform, S("slot")); Stretch(fill.rectTransform); fill.color = C(GiftColors[i % GiftColors.Length]); fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillOrigin = 0;
                var status = Text("Status", card, "", 15, 8, 277, w - 16, 24, true); status.alignment = TextAlignmentOptions.Center;
                var entry = cards.GetArrayElementAtIndex(i);
                foreach (var pair in new Dictionary<string, Object> { ["Root"] = card.gameObject, ["Reward"] = chip, ["UnlockAt"] = unlock, ["Countdown"] = countdown, ["Status"] = status, ["Progress"] = fill, ["Background"] = card.GetComponent<Image>() }) entry.FindPropertyRelative(pair.Key).objectReferenceValue = pair.Value;
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            var timer = Text("Timer", frame, "", 23, 36, height - 171, width - 72, 36, true); timer.alignment = TextAlignmentOptions.Center;
            var summary = Text("Summary", frame, "", 19, 36, height - 132, width - 72, 30); summary.alignment = TextAlignmentOptions.Center;
            var claim = Button("Claim", frame, "Забрать подарок", width / 2 - 230, height - 88, 460, 60);
            claim.GetComponent<Image>().sprite = AccentSprite("party-claim", "D894B7", "A04E80");
            Set(window, "config", config, "reward", null, "timer", timer, "summary", summary, "claim", claim);
            Save(root, UI + "PlaytimeGiftWindow.prefab");
        }

        [InitializeOnLoadMethod]
        private static void WatchRewardUi() => EditorApplication.update += () =>
        {
            if (!File.Exists("Temp/reward-ui.request") || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete("Temp/reward-ui.request");
            try { RefreshRewardWindows(); }
            catch (Exception e) { File.WriteAllText("Documentation/reward-ui-validation.txt", "FAIL " + e); Debug.LogException(e); }
        };

        [MenuItem("MineArena/UI/Refresh Festive Rewards")]
        public static void RefreshRewardWindows()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || GameRoot.Instance != null || Object.FindObjectsOfType<SaveService>().Any(s => s.IsLoaded)) throw new InvalidOperationException("Run rewards validation in Edit Mode without a loaded save.");
            BuildDaily(); BuildPlaytime();
            var results = new List<string>();
            void Check(bool ok, string message) { if (!ok) throw new Exception(message); results.Add("PASS " + message); }
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Transient reward validation"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var root = go.AddComponent<GameRoot>(); typeof(GameRoot).GetProperty("Instance").SetValue(null, root);
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
                var progress = new PlayerProgress("reward-preview");
                typeof(GameRoot).GetField("gameConfig", flags).SetValue(root, config); typeof(GameRoot).GetField("playerProgress", flags).SetValue(root, progress);
                var inventory = go.AddComponent<InventoryManager>(); var daily = go.AddComponent<DailyRewardManager>();
                var managers = (Dictionary<Type, BaseManager>)typeof(GameRoot).GetField("_managers", flags).GetValue(root);
                managers[typeof(InventoryManager)] = inventory; managers[typeof(DailyRewardManager)] = daily; inventory.InitManager();
                progress.DailyRewardProgress.RegisterFirstSession((long)(DateTime.UtcNow.Date - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays - 1);
                var gifts = AssetDatabase.LoadAssetAtPath<PlaytimeRewardsConfig>(GiftsPath);
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "PlaytimeGiftWindow.prefab"), scene);
                var view = instance.GetComponent<PlaytimeGiftWindow>();
                void Refresh() => typeof(PlaytimeGiftWindow).GetMethod("Refresh", flags).Invoke(view, null);
                var cards = (PlaytimeGiftWindow.RewardCard[])typeof(PlaytimeGiftWindow).GetField("cards", flags).GetValue(view);
                progress.PlaytimeGiftProgress.SecondsPlayed = 299.5f; Refresh();
                Check(cards.Length == gifts.Rewards.Count && cards.All(c => c.Root.activeSelf), "All configured rewards are visible together");
                Check(cards[0].Countdown.text.Contains("00:01"), "Countdown rounds up before unlock");
                typeof(PlaytimeGiftWindow).GetMethod("Claim", flags).Invoke(view, null);
                Check(progress.PlaytimeGiftProgress.ClaimedRewards == 0, "Premature claim rejected");
                progress.PlaytimeGiftProgress.SecondsPlayed = 300; Refresh();
                Check(cards[0].Countdown.text == "Доступна сейчас" && cards[1].Countdown.text.Contains("10:00"), "Each reward has an independent remaining time");
                int before = inventory.GetItemAmount(gifts.Rewards[0].Item.Name);
                typeof(PlaytimeGiftWindow).GetMethod("Claim", flags).Invoke(view, null);
                typeof(PlaytimeGiftWindow).GetMethod("Claim", flags).Invoke(view, null);
                Check(progress.PlaytimeGiftProgress.ClaimedRewards == 1 && inventory.GetItemAmount(gifts.Rewards[0].Item.Name) == before + gifts.Rewards[0].Amount, "Ready reward is granted exactly once");
                Check(cards[0].Root.activeSelf && cards[0].Status.text == "ПОЛУЧЕНО", "Claimed rewards remain visible");
                Check(PlaytimeGiftWindow.FormatRemaining(3661) == "01:01:01", "Timers support rewards beyond one hour");
                Directory.CreateDirectory("Documentation/UI");
                Render(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "PlaytimeGiftWindow.prefab"), "Documentation/UI/Playtime-Festive.png", rendered => typeof(PlaytimeGiftWindow).GetMethod("Refresh", flags).Invoke(rendered.GetComponent<PlaytimeGiftWindow>(), null));
                Render(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "DailyGiftWindow.prefab"), "Documentation/UI/Daily-Festive.png", rendered =>
                {
                    var dailyConfig = AssetDatabase.LoadAssetAtPath<DailyRewardConfig>("Assets/ScriptableObjects/Configs/Game/DailyRewardsConfig.asset");
                    rendered.GetComponent<DailyGiftWIndow>().Setup(daily, dailyConfig, 0);
                    Canvas.ForceUpdateCanvases();
                    foreach (var rect in rendered.GetComponentsInChildren<RectTransform>()) LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                    var states = rendered.GetComponentsInChildren<TMP_Text>().Where(t => t.name == "Status").ToArray();
                    Check(states.Length == dailyConfig.RewardsCount && states[0].text == "СЕГОДНЯ", "Daily cards show all seven days and highlight today's reward");
                    Check(states.All(t => -t.rectTransform.anchoredPosition.y + t.rectTransform.rect.height <= ((RectTransform)t.transform.parent).rect.height), "Daily status labels fit inside their cards");
                });
                progress.PlaytimeGiftProgress.SecondsPlayed = gifts.Rewards.Max(g => g.Minutes) * 60;
                for (int i = 0; i < gifts.Rewards.Count; i++) typeof(PlaytimeGiftWindow).GetMethod("Claim", flags).Invoke(view, null);
                Refresh(); Check(cards.All(c => c.Root.activeSelf && c.Status.text == "ПОЛУЧЕНО"), "All rewards remain visible after completing the track");
                File.WriteAllLines("Documentation/reward-ui-validation.txt", results);
            }
            finally { typeof(GameRoot).GetProperty("Instance").SetValue(null, null); EditorSceneManager.ClosePreviewScene(scene); }
        }
    }
}
