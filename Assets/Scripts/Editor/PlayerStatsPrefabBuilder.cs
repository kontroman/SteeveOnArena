using System;
using System.IO;
using System.Linq;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.PlayerSystem;
using MineArena.UI;
using MineArena.Windows;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static partial class GameUiBuilder
    {
        private const string StatsPath = UI + "PlayerStatsWindow.prefab";
        [InitializeOnLoadMethod]
        private static void WatchStatsBuild() => EditorApplication.update += () =>
        {
            const string request = "Temp/player-stats.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { BuildPlayerStats(); ValidatePlayerStats(); PlayerStatsFlowValidation.Validate(); File.WriteAllText("Temp/player-stats-result.txt", "PASS"); }
            catch (Exception error) { File.WriteAllText("Temp/player-stats-result.txt", error.ToString()); Debug.LogException(error); }
        };

        [MenuItem("MineArena/UI/Rebuild Player Stats Window")]
        public static void BuildPlayerStats()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var root = Edit<PlayerStatsWindow>(StatsPath);
            var window = root.GetComponent<PlayerStatsWindow>();
            var frame = Window(root, "Характеристики", 1160, 944);
            Ribbon(frame, "468D91", null);
            var summary = Panel("PlayerSummary", frame, "inset"); Box(summary, 32, 119, 1096, 130);
            var badge = Panel("PortraitFrame", summary, "card"); Box(badge, 18, 17, 94, 94);
            var face = Rect("PixelFace", badge).gameObject.AddComponent<Devotion.SDK.UI.PixelPortraitGraphic>();
            Box(face.rectTransform, 10, 10, 74, 74); face.raycastTarget = false;
            var name = Text("PlayerName", summary, "Player247", 27, 134, 16, 538, 36, true); name.richText = false;
            var level = Text("Level", summary, "УРОВЕНЬ 1", 22, 744, 17, 330, 34, true); level.alignment = TextAlignmentOptions.MidlineRight;
            var xp = Text("Experience", summary, "Опыт: 0 / 60", 19, 136, 58, 580, 30);
            var bar = Slider(summary, "ExperienceBar", 136, 95, 936, 13, false); bar.value = 0f;
            bar.fillRect.GetComponent<Image>().color = C("55AAAC");
            var points = Text("AvailablePoints", frame, "Очки развития: 0", 25, 38, 267, 520, 42, true); points.color = C("367D80");
            var note = Text("LevelRule", frame, "+1 очко за каждый новый уровень", 19, 556, 267, 570, 42); note.alignment = TextAlignmentOptions.MidlineRight;

            var rows = new PlayerStatsWindow.AttributeRow[5];
            Directory.CreateDirectory("Assets/Prefabs/UI/PlayerAttributes");
            for (int i = 0; i < rows.Length; i++)
            {
                var row = Panel("Attribute_" + (PlayerAttribute)i, frame, "card"); Box(row, 32, 326 + i * 91, 1096, 81);
                var iconBack = Panel("IconFrame", row, "inset"); Box(iconBack, 12, 9, 64, 64);
                var icon = Rect("Icon", iconBack).gameObject.AddComponent<PlayerAttributeIcon>();
                Box(icon.rectTransform, 4, 4, 56, 56); icon.attribute = (PlayerAttribute)i; icon.raycastTarget = false;
                PrefabUtility.SaveAsPrefabAsset(icon.gameObject, "Assets/Prefabs/UI/PlayerAttributes/" + (PlayerAttribute)i + ".prefab");
                Text("Title", row, PlayerStatsWindow.Titles[i], 24, 92, 9, 492, 30, true);
                Text("Description", row, PlayerStatsWindow.Descriptions[i], 17, 92, 42, 590, 31);
                var effect = Text("Effect", row, PlayerStatsWindow.Effect(i, 0), 19, 682, 18, 202, 46, true); effect.color = C("397F83");
                var minus = Button("Minus", row, "−", 886, 17, 46, 46, false);
                var rank = Text("Rank", row, "0 / 50", 20, 938, 18, 88, 46); rank.alignment = TextAlignmentOptions.Center;
                var plus = Button("Plus", row, "+", 1036, 17, 46, 46);
                rows[i] = new PlayerStatsWindow.AttributeRow { rank = rank, effect = effect, minus = minus, plus = plus };
            }
            var feedback = Text("Feedback", frame, "Распределите очки и нажмите «Сохранить».", 20, 38, 795, 1080, 35);
            Text("RespecHint", frame, "«−» возвращает очко. Закрытие отменяет изменения.", 17, 38, 858, 640, 50).enableWordWrapping = true;
            var cancel = Button("Cancel", frame, "Отмена", 704, 854, 174, 54, false);
            var save = Button("Save", frame, "Сохранить", 898, 854, 230, 54);
            Set(window, "playerName", name, "level", level, "experience", xp, "experienceBar", bar,
                "points", points, "feedback", feedback, "save", save, "cancel", cancel);
            var so = new SerializedObject(window); var array = so.FindProperty("rows"); array.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                var entry = array.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("rank").objectReferenceValue = rows[i].rank;
                entry.FindPropertyRelative("effect").objectReferenceValue = rows[i].effect;
                entry.FindPropertyRelative("minus").objectReferenceValue = rows[i].minus;
                entry.FindPropertyRelative("plus").objectReferenceValue = rows[i].plus;
            }
            so.ApplyModifiedPropertiesWithoutUndo(); Save(root, StatsPath);
            RegisterStatsWindow();
            var hud = PrefabUtility.LoadPrefabContents(UI + "PlayingWindow.prefab");
            try
            {
                var panel = hud.transform.Find("PlayerPanel");
                if (panel.GetComponent<PlayerPanelUI>() == null) panel.gameObject.AddComponent<PlayerPanelUI>();
                var hudName = Find(panel, "PlayerName").GetComponent<TMP_Text>();
                hudName.text = "Player247"; hudName.richText = false;
                hudName.rectTransform.sizeDelta = new Vector2(248, hudName.rectTransform.sizeDelta.y);
                var hudLevel = panel.GetComponentInChildren<MineArena.Game.UI.PlayerLevelText>(true);
                if (hudLevel != null) hudLevel.GetComponent<TMP_Text>().text = "1";
                var hudXp = panel.GetComponentInChildren<MineArena.Game.UI.PlayerExperienceBar>(true);
                if (hudXp != null) hudXp.GetComponent<Image>().fillAmount = 0;
                var portrait = Find(panel, "PlayerIcon");
                if (portrait != null && portrait.Find("PixelFace") == null)
                {
                    var hudFace = Rect("PixelFace", portrait).gameObject.AddComponent<Devotion.SDK.UI.PixelPortraitGraphic>();
                    hudFace.raycastTarget = false;
                    hudFace.rectTransform.anchorMin = new Vector2(.1f, .1f);
                    hudFace.rectTransform.anchorMax = new Vector2(.9f, .9f);
                    hudFace.rectTransform.offsetMin = hudFace.rectTransform.offsetMax = Vector2.zero;
                }
                var panelRect = (RectTransform)panel;
                panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, Mathf.Max(198, panelRect.sizeDelta.y));
                if (panel.Find("DevelopmentHint") == null)
                {
                    var hint = Text("DevelopmentHint", panel, "Характеристики  •  Нажмите", 16, 18, 168, panelRect.sizeDelta.x - 36, 28);
                    hint.color = C("367D80"); hint.alignment = TextAlignmentOptions.Center;
                }
                HudActionNotice.Install(hud.transform);
                foreach (var notice in hud.GetComponentsInChildren<HudActionNotice>(true)) notice.SetVisible(false);
                PrefabUtility.SaveAsPrefabAsset(hud, UI + "PlayingWindow.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(hud); }
            AssetDatabase.SaveAssets();
            Render(AssetDatabase.LoadAssetAtPath<GameObject>(StatsPath), "Documentation/UI/PlayerStatsWindow.png");
            Render(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "PlayingWindow.prefab"), "Documentation/UI/PlayerProgression-HUD.png");
        }

        private static void RegisterStatsWindow()
        {
            const string path = "Assets/DevotionSDK/Prefabs/Managers/UIManager.prefab";
            var manager = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var so = new SerializedObject(manager.GetComponent<Devotion.SDK.Managers.UIManager>());
                var array = so.FindProperty("_windows");
                if (!Enumerable.Range(0, array.arraySize).Any(i => array.GetArrayElementAtIndex(i).objectReferenceValue is PlayerStatsWindow))
                {
                    int index = array.arraySize;
                    array.arraySize++;
                    array.GetArrayElementAtIndex(index).objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerStatsWindow>(StatsPath);
                }
                so.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(manager, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(manager); }
        }

        [MenuItem("MineArena/Validation/Player Progression")]
        public static void ValidatePlayerStats()
        {
            void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
            var progress = new PlayerDataProgress();
            var xp = new PlayerExperience(progress);
            Check(xp.CurrentLevel == 1 && xp.ExperiencePerLevel == 60, "New player starts at level 1 / 60 XP");
            xp.AddExperience(59); Check(xp.CurrentLevel == 1, "No premature level");
            xp.AddExperience(1); Check(xp.CurrentLevel == 2 && xp.CurrentExperience == 0, "Exact threshold");
            xp.AddExperience(120 + 250 + 7); Check(xp.CurrentLevel == 4 && xp.CurrentExperience == 7, "Multiple thresholds and remainder");
            Check(progress.TrySaveDevelopment(new[] { 1, 1, 1, 0, 0 }), "One point per earned level");
            Check(!progress.TrySaveDevelopment(new[] { 2, 1, 1, 0, 0 }), "Overspending rejected");
            Check(!progress.TrySaveDevelopment(new[] { -1, 1, 1, 0, 0 }), "Negative ranks rejected");
            var draft = progress.CopyDevelopment(); draft[0] = 0;
            Check(progress.CopyDevelopment()[0] == 1, "Draft isolated until Save");
            Check(progress.TrySaveDevelopment(draft), "Refund committed");
            var loaded = JsonUtility.FromJson<PlayerDataProgress>(JsonUtility.ToJson(progress));
            var restored = new PlayerExperience(loaded);
            Check(restored.CurrentLevel == 4 && restored.CurrentExperience == 7 && loaded.CopyDevelopment()[1] == 1, "Save round trip");
            Check(progress.FallbackName == JsonUtility.FromJson<PlayerDataProgress>(JsonUtility.ToJson(progress)).FallbackName, "Fallback name persists");
            int[] thresholds = { 60, 120, 250, 500, 1000, 2000, 4000 };
            for (int i = 0; i < thresholds.Length; i++) Check(PlayerExperience.RequiredExperience(i + 1) == thresholds[i], "Requested level threshold " + (i + 2));
            for (int i = 2; i < PlayerExperience.MaxLevel; i++)
                Check(PlayerExperience.RequiredExperience(i) > PlayerExperience.RequiredExperience(i - 1) || PlayerExperience.RequiredExperience(i) == 1000000000, "Increasing XP curve up to safety cap");
            Check(PlayerExperience.RequiredExperience(30) > 100000, "Steep late game");
            xp.RestoreData(99, 0); xp.AddExperience(int.MaxValue); Check(xp.CurrentLevel == 100 && xp.CurrentExperience == 0, "Maximum and overflow");
            var prefab = AssetDatabase.LoadAssetAtPath<PlayerStatsWindow>(StatsPath);
            Check(prefab != null && prefab.GetComponentsInChildren<PlayerAttributeIcon>(true).Length == 5, "Five distinct stat icons");
            Check(prefab.GetComponentsInChildren<Button>(true).Length == 13, "Five +/- pairs, close, cancel, save");
            File.WriteAllText("Documentation/player-progression-validation.txt", "PASS: thresholds, overflow, multi-level reward, one point per level, draft isolation, refunds, invalid allocations, JSON roundtrip, stable name, curve, prefab controls.\n");
            Debug.Log("[PlayerProgression] Validation passed.");
        }
    }
}
