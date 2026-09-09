using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Devotion.SDK.Confgs;
using Devotion.SDK.Controllers;
using Devotion.SDK.DailyReward;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.Localization;
using Devotion.SDK.Services.SaveSystem;
using Devotion.SDK.Services.SaveSystem.Progress;
using Devotion.SDK.UI;
using MineArena.Managers;
using MineArena.SDK.UI;
using MineArena.Structs;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static class LocalizationRewardValidation
    {
        const string UI = "Assets/DevotionSDK/Prefabs/UI/";
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly SystemLanguage[] Languages = { SystemLanguage.Russian, SystemLanguage.English, SystemLanguage.German,
            SystemLanguage.Spanish, SystemLanguage.Italian, SystemLanguage.French, SystemLanguage.Portuguese,
            SystemLanguage.Turkish, SystemLanguage.Indonesian };

        [InitializeOnLoadMethod]
        static void Watch() => EditorApplication.update += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var action in new[] { "prepare", "validate" })
            {
                string request = "Temp/" + action + "-localization-rewards.request";
                if (!File.Exists(request)) continue;
                File.Delete(request);
                try { if (action == "prepare") Prepare(); else Validate(); }
                catch (Exception e) { File.WriteAllText("Documentation/" + action + "-localization-rewards.txt", "FAIL " + e); Debug.LogException(e); }
            }
        };

        [MenuItem("MineArena/Localization/Prepare UI")]
        public static void Prepare()
        {
            var config = AssetDatabase.LoadAssetAtPath<LocalizationConfig>("Assets/DevotionSDK/Configs/LocalizationConfig.asset");
            config.defaultLanguage = SystemLanguage.Russian;
            config.supportedLanguages = Languages;
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssetIfDirty(config);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset");
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/Rubik-VariableFont_wght SDF.asset");
            font.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
            if (!font.fallbackFontAssetTable.Contains(fallback)) font.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(font); AssetDatabase.SaveAssetIfDirty(font);
            var settings = PrefabUtility.LoadPrefabContents(UI + "SettingsWindow.prefab");
            try
            {
                var frame = (RectTransform)settings.transform.Find("BeigeWindow");
                frame.sizeDelta = new Vector2(frame.sizeDelta.x, 920);
                var existing = frame.Find("Language");
                var button = existing != null ? existing.GetComponent<Button>() :
                    Object.Instantiate(frame.Find("Close").GetComponent<Button>(), frame);
                button.name = "Language"; button.onClick = new Button.ButtonClickedEvent();
                var rect = (RectTransform)button.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
                rect.anchoredPosition = new Vector2(38, -802); rect.sizeDelta = new Vector2(964, 64);
                var label = button.GetComponentInChildren<TMP_Text>();
                label.text = "Язык: Русский  ›"; label.fontSize = 26;
                label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(12, 4); label.rectTransform.offsetMax = new Vector2(-12, -4);
                var serialized = new SerializedObject(settings.GetComponent<MineArena.Windows.SettingsWindow>());
                serialized.FindProperty("languageButton").objectReferenceValue = button;
                serialized.FindProperty("languageLabel").objectReferenceValue = label;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(settings, UI + "SettingsWindow.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(settings); }
            int count = 0;
            foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/DevotionSDK/Prefabs", "Assets/Prefabs", "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset.GetComponent<LocalizedTextScope>() != null || asset.GetComponentInChildren<TMP_Text>(true) == null) continue;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string assetGuid, out long rootId);
                string yaml = File.ReadAllText(path);
                long componentId = 7900000000000000000;
                while (yaml.Contains(componentId.ToString())) componentId++;
                string header = "--- !u!1 &" + rootId + "\n";
                int start = yaml.IndexOf(header, StringComparison.Ordinal);
                if (start < 0) throw new Exception("Missing prefab root: " + path);
                int componentList = yaml.IndexOf("  m_Component:\n", start, StringComparison.Ordinal) + "  m_Component:\n".Length;
                yaml = yaml.Insert(componentList, "  - component: {fileID: " + componentId + "}\n");
                var scriptGuid = AssetDatabase.AssetPathToGUID("Assets/DevotionSDK/Scripts/UI/LocalizedTextScope.cs");
                yaml += "\n--- !u!114 &" + componentId + "\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: " + rootId + "}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: " + scriptGuid + ", type: 3}\n  m_Name: \n  m_EditorClassIdentifier: \n";
                File.WriteAllText(path, yaml);
                AssetDatabase.ImportAsset(path); count++;
            }
            File.WriteAllText("Documentation/prepare-localization-rewards.txt", "PASS nine languages, settings selector, localized prefab scopes: " + count);
        }

        [MenuItem("MineArena/Validation/Localization and Rewards")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || GameRoot.Instance != null || Object.FindObjectsOfType<SaveService>().Any(s => s.IsLoaded))
                throw new InvalidOperationException("Validation needs Edit Mode without a loaded save.");
            var results = new List<string>();
            void Check(bool condition, string message) { if (!condition) throw new Exception(message); results.Add("PASS " + message); }
            var russian = Read(SystemLanguage.Russian);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Main/4197-font SDF.asset");
            foreach (var language in Languages)
            {
                var data = Read(language);
                Check(russian.Keys.OrderBy(x => x).SequenceEqual(data.Keys.OrderBy(x => x)), language + " has all " + russian.Count + " keys");
                foreach (var entry in russian)
                {
                    string Tokens(string s) => string.Join("|", Regex.Matches(s, @"\{\d+[^{}]*\}|</?[^>]+>").Cast<Match>().Select(m => m.Value).OrderBy(x => x));
                    if (string.IsNullOrWhiteSpace(data[entry.Key]) || Tokens(entry.Value) != Tokens(data[entry.Key]))
                        throw new Exception(language + " invalid structure: " + entry.Key);
                }
                results.Add("PASS " + language + " placeholders and rich-text tags");
                var letters = new string(string.Concat(data.Values).Where(char.IsLetter).Distinct().ToArray());
                Check(font.HasCharacters(letters, out uint[] missing, true, true), language + " font coverage: " + string.Join(",", missing ?? Array.Empty<uint>()));
                LocalizedTextRenderer.Rebuild(russian, data);
                Check(LocalizedTextRenderer.Translate("Крутить! (3)") == data["Крутить! ({0})"].Replace("{0}", "3"), language + " dynamic spin count");
                Check(LocalizedTextRenderer.Translate("День 17") == data["День {0}"].Replace("{0}", "17"), language + " dynamic daily label");
                Check(LocalizedTextRenderer.Translate("Занято ячеек: 12") == data["Занято ячеек: "] + "12", language + " concatenated counter");
                if (language != SystemLanguage.Russian)
                {
                    var untranslated = new List<string>();
                    foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/DevotionSDK/Prefabs", "Assets/Prefabs", "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath))
                    {
                        foreach (var label in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<TMP_Text>(true))
                            if (Regex.IsMatch(LocalizedTextRenderer.Translate(label.text) ?? "", "[А-Яа-яЁё]")) untranslated.Add(path + "/" + label.name + ": " + label.text);
                        foreach (var label in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Text>(true))
                            if (Regex.IsMatch(LocalizedTextRenderer.Translate(label.text) ?? "", "[А-Яа-яЁё]")) untranslated.Add(path + "/" + label.name + ": " + label.text);
                    }
                    Check(untranslated.Count == 0, language + " serialized UI coverage" + (untranslated.Count == 0 ? "" : "\n" + string.Join("\n", untranslated)));
                }
            }
            LocalizedTextRenderer.Rebuild(russian, russian);
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Transient localization reward validation");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var root = go.AddComponent<GameRoot>(); typeof(GameRoot).GetProperty("Instance").SetValue(null, root);
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
                var progress = new PlayerProgress("localization-reward-validation");
                typeof(GameRoot).GetField("gameConfig", Private).SetValue(root, config);
                typeof(GameRoot).GetField("playerProgress", Private).SetValue(root, progress);
                var inventory = go.AddComponent<InventoryManager>(); var daily = go.AddComponent<DailyRewardManager>();
                var managers = (Dictionary<Type, BaseManager>)typeof(GameRoot).GetField("_managers", Private).GetValue(root);
                managers[typeof(InventoryManager)] = inventory; managers[typeof(DailyRewardManager)] = daily; inventory.InitManager();
                long today = DateTime.UtcNow.Date.Ticks / TimeSpan.TicksPerDay;
                progress.DailyRewardProgress.RegisterFirstSession(today - 1);
                Check(daily.CanClaim && !daily.IsRewardClaimed(0), "First daily reward available and bright");
                Check(daily.TryClaimCurrentReward() && !daily.TryClaimCurrentReward(), "Daily claim granted exactly once");
                Check(daily.IsRewardClaimed(0) && !daily.IsRewardClaimed(1), "Claimed daily reward shaded");
                var dailyPrefab = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "DailyGiftWindow.prefab"), scene);
                dailyPrefab.GetComponent<DailyGiftWIndow>().Setup(daily, config.DailyRewardConfig, 1);
                Check(dailyPrefab.GetComponentsInChildren<Image>().Count(i => i.name == "ClaimedShade") == 1, "Only claimed card has visible shade");
                GameUiBuilder.Render(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "DailyGiftWindow.prefab"), "Documentation/UI/Daily-Claimed.png", rendered =>
                {
                    rendered.GetComponent<DailyGiftWIndow>().Setup(daily, config.DailyRewardConfig, 1);
                    foreach (var label in rendered.GetComponentsInChildren<TMP_Text>(true)) LocalizedTextRenderer.Bind(label);
                });
                int count = config.DailyRewardConfig.RewardsCount;
                typeof(DailyRewardProgress).GetField("nextRewardIndex", Private).SetValue(progress.DailyRewardProgress, count - 1);
                typeof(DailyRewardProgress).GetField("lastClaimedUtcDayNumber", Private).SetValue(progress.DailyRewardProgress, today - 1);
                Check(daily.TryClaimCurrentReward() && Enumerable.Range(0, count).All(daily.IsRewardClaimed), "Final day shades entire completed cycle");
                typeof(DailyRewardProgress).GetField("lastClaimedUtcDayNumber", Private).SetValue(progress.DailyRewardProgress, today - 1);
                Check(daily.CanClaim && !Enumerable.Range(0, count).Any(daily.IsRewardClaimed), "Next cycle resets shading");
                var wheelObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "FortuneWheelWindow.prefab"), scene);
                var wheel = wheelObject.GetComponent<FortuneWheelWindow>();
                Invoke(wheel, "EnsureRewards");
                var rewards = (List<FortuneWheelReward>)typeof(FortuneWheelWindow).GetField("_rewards", Private).GetValue(wheel);
                Check(wheelObject.GetComponentInChildren<MineArena.UI.FortuneWheel.WheelPointerGraphic>() != null, "Wheel pointer uses a font-independent mesh");
                var audio = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/ManagersPrefabs/AudioManager.prefab").GetComponent<AudioManager>();
                var sounds = new SerializedObject(audio).FindProperty("_music").objectReferenceValue as MineArena.MusicResourses.MusicResourses;
                Check(sounds != null && sounds.GetEffect(MineArena.Basics.Constants.AudioNames.UIClick) != null, "Wheel tick sound is assigned in the effects mixer path");
                foreach (var reward in rewards)
                {
                    Check(config.ItemDatabase.GetItemConfig(reward.Id) != null, "Wheel reward exists: " + reward.Id);
                    Check(reward.FallbackGroup.All(id => config.ItemDatabase.GetItemConfig(id) != null), "Wheel replacement items exist: " + reward.Id);
                    int before = inventory.GetItemAmount(reward.Id);
                    typeof(FortuneWheelWindow).GetField("_pendingResolvedReward", Private).SetValue(wheel, reward);
                    typeof(FortuneWheelWindow).GetField("_isSpinning", Private).SetValue(wheel, true);
                    Invoke(wheel, "CompleteSpin"); Invoke(wheel, "CompleteSpin");
                    Check(inventory.GetItemAmount(reward.Id) == before + reward.Amount, "Wheel credits once: " + reward.Id);
                    Check(progress.InventoryProgress.SavedResources[reward.Id] == inventory.GetItemAmount(reward.Id), "Wheel reward included in saved progress: " + reward.Id);
                }
                var unique = rewards.First(r => r.IsUnique);
                foreach (string id in unique.FallbackGroup) inventory.AddItemById(id);
                var replacement = (FortuneWheelReward)typeof(FortuneWheelWindow).GetMethod("ResolveRewardDuplicate", Private).Invoke(wheel, new object[] { unique });
                Check(replacement.Id == "DiamondOre" && replacement.Amount == 25, "Owned unique set yields 25 diamonds");
                int diamonds = inventory.GetItemAmount("DiamondOre");
                typeof(FortuneWheelWindow).GetField("_pendingResolvedReward", Private).SetValue(wheel, replacement);
                typeof(FortuneWheelWindow).GetField("_isSpinning", Private).SetValue(wheel, true);
                Invoke(wheel, "OnDisable"); Invoke(wheel, "OnDisable");
                Check(inventory.GetItemAmount("DiamondOre") == diamonds + 25, "Interrupted spin credits once");
                LocalizedTextRenderer.Rebuild(russian, Read(SystemLanguage.German));
                GameUiBuilder.Render(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "FortuneWheelWindow.prefab"), "Documentation/UI/Wheel-German.png", rendered =>
                {
                    foreach (var label in rendered.GetComponentsInChildren<TMP_Text>(true)) LocalizedTextRenderer.Bind(label);
                });
                LocalizedTextRenderer.Rebuild(russian, Read(SystemLanguage.Turkish));
                GameUiBuilder.Render(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "SettingsWindow.prefab"), "Documentation/UI/Settings-Turkish.png", rendered =>
                {
                    rendered.transform.Find("BeigeWindow/Language").GetComponentInChildren<TMP_Text>().text = "Язык: Türkçe  ›";
                    foreach (var label in rendered.GetComponentsInChildren<TMP_Text>(true)) LocalizedTextRenderer.Bind(label);
                });
            }
            finally
            {
                typeof(GameRoot).GetProperty("Instance").SetValue(null, null);
                EditorSceneManager.ClosePreviewScene(scene);
                LocalizedTextRenderer.Rebuild(russian, russian);
            }
            File.WriteAllText("Documentation/validate-localization-rewards.txt", string.Join("\n", results));
            Debug.Log("Localization/rewards validation passed: " + results.Count + " checks");
        }
        static Dictionary<string, string> Read(SystemLanguage language) => JsonUtility.FromJson<LocalizationData>(File.ReadAllText("Assets/Resources/Localization/" + language + ".json")).ToDictionary();
        static void Invoke(object target, string name) => target.GetType().GetMethod(name, Private).Invoke(target, null);
    }
}
