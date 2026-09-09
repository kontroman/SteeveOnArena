using System;
using System.IO;
using TMPro;
using UI.UIAchievement;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static partial class GameUiBuilder
    {
        [InitializeOnLoadMethod]
        private static void WatchAchievementPopupRequest()
        {
            EditorApplication.update += () =>
            {
                const string request = "Temp/rebuild-achievement-popup.request";
                if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(request)) return;
                File.Delete(request);
                try
                {
                    BuildAchievementPopup();
                    File.WriteAllText("Temp/achievement-popup-result.txt", "PASS: popup and HUD prefabs rebuilt; editor compilation succeeded.");
                }
                catch (Exception e) { File.WriteAllText("Temp/achievement-popup-result.txt", e.ToString()); Debug.LogException(e); }
            };
        }

        [MenuItem("MineArena/UI/Rebuild Achievement Popup")]
        public static void BuildAchievementPopup()
        {
            const string path = "Assets/Prefabs/Achievements/AchievementPopup.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                for (int i = root.transform.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1);
                rect.sizeDelta = new Vector2(600, 112);
                rect.anchoredPosition = new Vector2(0, 124);
                var layout = root.GetComponent<LayoutElement>();
                if (layout != null) Object.DestroyImmediate(layout);
                var background = root.GetComponent<Image>();
                background.sprite = S("panel"); background.type = UnityEngine.UI.Image.Type.Sliced; background.raycastTarget = false;
                Outline(rect); Brick(rect, 0.25f);
                var plate = Panel("AchievementIconPlate", rect, "selected"); Box(plate, 16, 22, 68, 68);
                var icon = Image("Icon", plate, Existing("UI/knowledge_book.png"));
                Box(icon.rectTransform, 8, 8, 52, 52); icon.preserveAspect = true;
                var title = Text("QuestName", rect, "Название достижения", 23, 100, 12, 480, 50, true);
                title.enableWordWrapping = true; title.enableAutoSizing = true; title.fontSizeMin = 18; title.fontSizeMax = 23;
                var track = Panel("ProgressPopupQuestBar", rect, "inset"); Box(track, 100, 70, 480, 26);
                var fill = Image("Fill", track, TextureSprite());
                Stretch(fill.rectTransform); fill.rectTransform.offsetMin = new Vector2(3, 3); fill.rectTransform.offsetMax = new Vector2(-3, -3);
                fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 0.4f; fill.color = C("8B9B54");
                var count = Text("ProgressText", track, "4 / 10", 18, 0, 0, 480, 26); count.alignment = TextAlignmentOptions.Center;
                var bar = track.gameObject.AddComponent<ProgressPopupQuestBar>(); Set(bar, "_fillImage", fill, "_textBar", count);
                var reward = Text("TakePrizeText", rect, "", 20, 100, 66, 480, 34); reward.color = C("587F79");
                reward.enableAutoSizing = true; reward.fontSizeMin = 14; reward.fontSizeMax = 20; reward.gameObject.SetActive(false);
                Set(root.GetComponent<AchievementPopup>(), "_nameQuest", title, "_messageTakePrize", reward, "_progressBarQuest", bar);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            var hud = PrefabUtility.LoadPrefabContents(UI + "PlayingWindow.prefab");
            try
            {
                var old = hud.transform.Find("AchievementPopup");
                bool active = old == null || old.gameObject.activeSelf;
                if (old != null) Object.DestroyImmediate(old.gameObject);
                var popup = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), hud.transform);
                popup.SetActive(active);
                PrefabUtility.SaveAsPrefabAsset(hud, UI + "PlayingWindow.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(hud); }
            var progress = PrefabUtility.LoadPrefabContents(UI + "LevelProgressWindow.prefab");
            try
            {
                ((RectTransform)progress.transform.Find("ProgressPanel")).anchoredPosition = Vector2.zero;
                PrefabUtility.SaveAsPrefabAsset(progress, UI + "LevelProgressWindow.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(progress); }
            Render(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "PlayingWindow.prefab"), "Documentation/UI/AchievementPopup-HUD.png", preview =>
            {
                var popup = preview.transform.Find("AchievementPopup"); popup.gameObject.SetActive(true);
                ((RectTransform)popup).anchoredPosition = Vector2.zero;
                var clearance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(UI + "LevelProgressWindow.prefab"), preview.transform);
                clearance.SetActive(true);
                ((RectTransform)clearance.transform.Find("ProgressPanel")).anchoredPosition = new Vector2(0, -124);
            });
        }
    }
}
