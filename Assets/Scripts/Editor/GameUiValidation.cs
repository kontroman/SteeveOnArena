using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Devotion.SDK.Base;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.SaveSystem;
using Devotion.SDK.Services.SaveSystem.Progress;
using Devotion.SDK.UI;
using MineArena.Items;
using MineArena.Managers;
using MineArena.Structs;
using MineArena.UI;
using MineArena.Windows;
using MineArena.Windows.Crafting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static class GameUiValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly List<string> Results = new List<string>();
        private static void Check(bool condition, string message)
        { if (!condition) throw new Exception(message); Results.Add("PASS " + message); }

        [MenuItem("MineArena/UI/Validate All Beige Windows")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || GameRoot.Instance != null ||
                Object.FindObjectsOfType<SaveService>().Any(s => s.IsLoaded))
                throw new InvalidOperationException("Run validation in Edit Mode without an initialized game/save service.");
            Results.Clear();
            var scene = EditorSceneManager.NewPreviewScene();
            var instance = typeof(GameRoot).GetProperty("Instance");
            try
            {
                var catalog = AssetDatabase.LoadAssetAtPath<ShopCatalog>("Assets/Resources/UI/ShopCatalog.asset");
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
                Check(catalog != null && catalog.Currency != null && catalog.Offers.Count >= 9, "Shop catalog and currency exist");
                Check(catalog.Offers.All(o => o.IsValid && config.ItemDatabase.GetItemConfig(o.Item.Name) == o.Item), "Every offer resolves to a real item in the game database");
                var managerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/Managers/UIManager.prefab");
                var refs = new SerializedObject(managerPrefab.GetComponent<UIManager>()).FindProperty("_windows");
                var types = new List<Type>();
                for (int i = 0; i < refs.arraySize; i++)
                { var window = refs.GetArrayElementAtIndex(i).objectReferenceValue as BaseWindow; Check(window != null, "Registered window " + i); types.Add(window.GetType()); }
                Check(types.Distinct().Count() == types.Count, "Window registry has no duplicate types");
                Check(types.Contains(typeof(ShopWindow)) && types.Contains(typeof(SettingsWindow)) && types.Contains(typeof(PlaytimeGiftWindow)) && types.Contains(typeof(LevelCompleteWindow)), "New windows are registered");
                var hud = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab");
                Check(hud.GetComponentsInChildren<GameUiAction>(true).Length == 9, "All nine HUD destinations are wired");
                foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/DevotionSDK/Prefabs/UI", "Assets/Resources/UI", "Assets/Resources/Prefabs/Windows/Crafting" }).Select(AssetDatabase.GUIDToAssetPath))
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Check(prefab.GetComponentsInChildren<Transform>(true).All(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0), "No missing scripts: " + prefab.name);
                }

                var go = new GameObject("Transient validation root");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var root = go.AddComponent<GameRoot>();
                instance.SetValue(null, root);
                var progress = new PlayerProgress("ui-validation-transient");
                typeof(GameRoot).GetField("gameConfig", Private).SetValue(root, config);
                typeof(GameRoot).GetField("playerProgress", Private).SetValue(root, progress);
                var inventory = go.AddComponent<InventoryManager>();
                var managers = (Dictionary<Type, BaseManager>)typeof(GameRoot).GetField("_managers", Private).GetValue(root);
                managers[typeof(InventoryManager)] = inventory;
                var buildingConfigs = AssetDatabase.FindAssets("t:BuildingConfig").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<MineArena.Buildings.BuildingConfig>).ToArray();
                Check(buildingConfigs.Length > 0 && buildingConfigs.All(c => c.Levels.All(l => l.ModelPrefab == null || l.Preview != null)), "Every building model level has a baked preview");
                var buildingWindow = Spawn<BuildingWindow>("Assets/DevotionSDK/Prefabs/UI/BuildingWindow.prefab", scene);
                foreach (var buildingConfig in buildingConfigs)
                {
                    buildingWindow.InitializeBuilding(buildingConfig, null);
                    var buildingSerialized = new SerializedObject(buildingWindow);
                    var preview = (Image)buildingSerialized.FindProperty("_preview").objectReferenceValue;
                    Check(preview.sprite == buildingConfig.GetCurrentLevel().Preview && preview.enabled, "Building preview follows selection: " + buildingConfig.name);
                    foreach (string field in new[] { "_priceTransform", "_opensTransform" })
                    {
                        var parent = (Transform)buildingSerialized.FindProperty(field).objectReferenceValue;
                        for (int i = parent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(parent.GetChild(i).gameObject);
                    }
                }
                var questManager = go.AddComponent<global::Managers.AchievementManager>();
                managers[typeof(global::Managers.AchievementManager)] = questManager;
                var quests = questManager.GetQuests();
                Check(quests.Count == config.DataAchievements.Count && questManager.GetQuests().Count == quests.Count, "Quest initialization includes all definitions without duplicates");
                var quest = quests[0];
                string rewardId = quest.Data.ItemPrize.ItemConfig.Name;
                int rewardBefore = inventory.GetItemAmount(rewardId);
                quest.TransferPrize(); quest.ChangeCurrentValue(-1);
                Check(!quest.IsCompleted && quest.CurrentValueProgress == 0 && inventory.GetItemAmount(rewardId) == rewardBefore, "Quest rejects premature claims and negative progress");
                quest.ChangeCurrentValue(int.MaxValue);
                Check(quest.CanTakePrize && quest.CurrentValueProgress == quest.MaxValueProgress, "Quest overshoot unlocks reward and clamps progress");
                var journal = Spawn<global::Windows.WindowAchievements>("Assets/Prefabs/Windows/WindowAchievements.prefab", scene);
                journal.Bind(quests); journal.SetFilter(1);
                var claim = (Button)typeof(global::Windows.WindowAchievements).GetField("claim", Private).GetValue(journal);
                Check(journal.GetComponentsInChildren<QuestJournalRow>().Length == 1 && claim.interactable, "Ready filter shows eligible quest and enables claim");
                journal.Claim(); journal.Claim();
                Check(quest.IsCompleted && inventory.GetItemAmount(rewardId) == rewardBefore + quest.Data.ItemPrize.Amount, "Journal grants full reward amount exactly once");
                Check(journal.GetComponentsInChildren<QuestJournalRow>().Length == 0, "Claimed quest leaves ready list");
                journal.SetFilter(2);
                Check(journal.GetComponentsInChildren<QuestJournalRow>().Length == 1 && !claim.interactable, "Completed filter shows claimed quest without enabling another claim");
                journal.SetFilter(0);
                Check(journal.GetComponentsInChildren<QuestJournalRow>().Length == quests.Count - 1, "Active filter excludes completed quests");
                Check(hud.GetComponentsInChildren<IconButtonHint>(true).Length == 9, "All HUD destinations use icons with tooltips");
                bool hadSensitivity = PlayerPrefs.HasKey(CameraSensitivity.PreferenceKey);
                float oldSensitivity = PlayerPrefs.GetFloat(CameraSensitivity.PreferenceKey, 1);
                try
                {
                    var settings = Spawn<SettingsWindow>("Assets/DevotionSDK/Prefabs/UI/SettingsWindow.prefab", scene);
                    Call(settings, "Awake");
                    var slider = (Slider)typeof(SettingsWindow).GetField("sensitivity", Private).GetValue(settings);
                    Check(slider.minValue == CameraSensitivity.Minimum && slider.maxValue == CameraSensitivity.Maximum, "Sensitivity slider has intended range");
                    slider.value = 2;
                    Check(Mathf.Approximately(CameraSensitivity.Apply(1, 3), 6), "Sensitivity slider updates saved preference and camera zoom scaling");
                }
                finally
                {
                    if (hadSensitivity) PlayerPrefs.SetFloat(CameraSensitivity.PreferenceKey, oldSensitivity);
                    else PlayerPrefs.DeleteKey(CameraSensitivity.PreferenceKey);
                }
                var money = catalog.Currency.Name;
                var offer = catalog.Offers.First(o => o.Item is StackableItemConfig);
                progress.InventoryProgress.SavedResources[money] = offer.Price - 1;
                inventory.InitManager();
                Check(!inventory.TryExchange(catalog.Currency, offer.Price, offer.Item, offer.Amount) && inventory.GetItemAmount(money) == offer.Price - 1, "Insufficient funds: no debit and no reward");
                progress.InventoryProgress.SavedResources[money] = offer.Price;
                Check(inventory.TryExchange(catalog.Currency, offer.Price, offer.Item, offer.Amount) && inventory.GetItemAmount(money) == 0 && inventory.GetItemAmount(offer.Item.Name) == offer.Amount, "Exact funds: debit and item granted together");
                Check(!inventory.TryExchange(catalog.Currency, 0, offer.Item, 1) && !inventory.TryExchange(catalog.Currency, -1, offer.Item, 1), "Invalid prices rejected");
                var equipment = catalog.Offers.First(o => !(o.Item is StackableItemConfig));
                progress.InventoryProgress.SavedResources[money] = equipment.Price * 2;
                Check(inventory.TryExchange(catalog.Currency, equipment.Price, equipment.Item, 1), "Equipment purchase succeeds");
                Check(!inventory.TryExchange(catalog.Currency, equipment.Price, equipment.Item, 1) && inventory.GetItemAmount(money) == equipment.Price, "Duplicate equipment does not charge again");
                progress.InventoryProgress.SavedResources[offer.Item.Name] = int.MaxValue;
                Check(!inventory.TryExchange(catalog.Currency, 1, offer.Item, 1), "Resource overflow rejected");
                progress.InventoryProgress.SavedResources[offer.Item.Name] = 0;
                var unknown = ScriptableObject.CreateInstance<StackableItemConfig>();
                Check(!inventory.TryExchange(catalog.Currency, 1, unknown, 1), "Unknown item rejected"); Object.DestroyImmediate(unknown);

                var wheel = Spawn<FortuneWheelWindow>("Assets/DevotionSDK/Prefabs/UI/FortuneWheelWindow.prefab", scene);
                progress.InventoryProgress.SavedResources[money] = 15;
                Call(wheel, "PurchaseFortuneSpins", 3);
                Check(inventory.GetItemAmount(money) == 0 && progress.LuckyWheelProgress.FortuneSpins == 3, "Wheel charges 15 ore for three spins");
                Call(wheel, "PurchaseFortuneSpins", 3);
                Check(progress.LuckyWheelProgress.FortuneSpins == 3, "Wheel cannot buy without funds");
                Call(wheel, "RequestRewardedSpin");
                Check(progress.LuckyWheelProgress.FortuneSpins == 3, "No ad provider: no unverified spin granted");

                var gift = Spawn<PlaytimeGiftWindow>("Assets/DevotionSDK/Prefabs/UI/PlaytimeGiftWindow.prefab", scene);
                var gifts = AssetDatabase.LoadAssetAtPath<PlaytimeRewardsConfig>("Assets/Resources/UI/PlaytimeRewards.asset");
                var first = gifts.Rewards[0];
                progress.PlaytimeGiftProgress.SecondsPlayed = first.Minutes * 60 - 1;
                Call(gift, "Claim"); Check(progress.PlaytimeGiftProgress.ClaimedRewards == 0, "Playtime reward rejects early claim");
                int before = inventory.GetItemAmount(first.Item.Name);
                progress.PlaytimeGiftProgress.SecondsPlayed++;
                Call(gift, "Claim"); Call(gift, "Claim");
                Check(progress.PlaytimeGiftProgress.ClaimedRewards == 1 && inventory.GetItemAmount(first.Item.Name) == before + first.Amount, "Playtime threshold grants once, repeated click does not duplicate");
                var restored = JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(progress));
                Check(restored.PlaytimeGiftProgress.ClaimedRewards == 1 && restored.PlaytimeGiftProgress.SecondsPlayed == first.Minutes * 60, "Playtime survives serialization");
                var daily = new DailyRewardProgress(); daily.RegisterFirstSession(100);
                Check(!daily.IsRewardAvailable(100, 7) && daily.IsRewardAvailable(101, 7), "Daily eligibility respects new UTC day");
                daily.MarkRewardClaimed(101, 7); daily.MarkRewardClaimed(101, 7);
                Check(daily.NextRewardIndex == 1 && !daily.IsRewardAvailable(101, 7), "Daily reward cannot repeat on the same day");

                var crafting = Spawn<CraftingWindow>("Assets/Resources/Prefabs/Windows/Crafting/CraftingWindow.prefab", scene);
                Check((bool)Call(crafting, "HasPrefabLayout"), "Crafting has all required layout references");
                Call(crafting, "EnsureLayout");
                Check(crafting.GetComponentInChildren<Button>(true) != null, "Crafting initializes its rebuilt prefab");
                crafting.Initialize(null);
                Check(crafting.GetComponentsInChildren<CraftingTabButton>(true).Length > 0 && crafting.GetComponentsInChildren<CraftingItemView>(true).Length > 0, "Crafting populates real categories and recipes");
                GameUiBuilder.Render(crafting.gameObject, "Documentation/UI/CraftingWindow-populated.png");
                var complete = Spawn<LevelCompleteWindow>("Assets/DevotionSDK/Prefabs/UI/LevelCompleteWindow.prefab", scene);
                complete.Setup(catalog.Offers.Take(5).ToDictionary(o => o.Item, o => o.Amount), () => { }, () => { }, false);
                Check(complete.GetComponentsInChildren<MineArena.Windows.SelectLevel.LevelResourceChip>(true).Length == 5, "Level completion creates five resource rewards");
                GameUiBuilder.Render(complete.gameObject, "Documentation/UI/LevelCompleteWindow-populated.png");
                Results.Add("Complete. Transient progress only; no player save provider initialized. Full gameplay/ads not simulated.");
                Debug.Log("[GameUI] Validation passed: " + Results.Count + " checks/notes.");
            }
            catch (Exception e) { Results.Add("FAIL " + e); Debug.LogException(e); }
            finally
            {
                instance.SetValue(null, null);
                EditorSceneManager.ClosePreviewScene(scene);
                Directory.CreateDirectory("Documentation/UI"); File.WriteAllLines("Documentation/UI/validation.txt", Results);
            }
        }
        private static T Spawn<T>(string path, UnityEngine.SceneManagement.Scene scene) where T : Component
        { return ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene)).GetComponent<T>(); }
        private static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    }
}
