using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Achievements;
using MineArena.Structs;
using MineArena.UI;
using MineArena.Windows.SelectLevel;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static partial class GameUiBuilder
    {
        [InitializeOnLoadMethod]
        private static void WatchResourceChipFit()
        {
            EditorApplication.update += () =>
            {
                if (!File.Exists("Temp/chip-fit.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete("Temp/chip-fit.request");
                try { FixAndValidateResourceChips(); }
                catch (Exception ex) { File.WriteAllText("Documentation/chip-fit-validation.txt", "FAIL " + ex); Debug.LogException(ex); }
            };
        }

        [MenuItem("MineArena/UI/Fit And Validate Resource Chips")]
        public static void FixAndValidateResourceChips()
        {
            const string path = "Assets/Prefabs/Windows/SelectLevelWindow/ResourceChip.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try { root.GetComponent<LevelResourceChip>().FitContents(); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
            var scene = EditorSceneManager.NewPreviewScene();
            var results = new List<string>();
            try
            {
                var chip = ((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene)).GetComponent<LevelResourceChip>();
                var rect = (RectTransform)chip.transform;
                foreach (var size in new[]{ new Vector2(400,52), new Vector2(338,68), new Vector2(205,68) })
                foreach (float scale in new[]{1f,.55f})
                foreach (string id in new[]{"GoldOre","HealingPotion"})
                foreach (bool quantity in new[]{false,true})
                {
                    rect.sizeDelta = size; rect.localScale = Vector3.one * scale;
                    chip.Bind(config.ItemDatabase.GetItemConfig(id), quantity ? 6 : (int?)null);
                    Canvas.ForceUpdateCanvases();
                    var so = new SerializedObject(chip);
                    var flat = (Image)so.FindProperty("icon").objectReferenceValue;
                    var block = (ResourceIcon)so.FindProperty("blockIcon").objectReferenceValue;
                    var images = block.gameObject.activeSelf ? block.GetComponentsInChildren<Image>() : new[]{flat};
                    var corners = new Vector3[4];
                    var label = (TMPro.TMP_Text)so.FindProperty("label").objectReferenceValue;
                    float textLeft = rect.InverseTransformPoint(label.rectTransform.TransformPoint(new Vector3(label.rectTransform.rect.xMin,0,0))).x;
                    foreach (var image in images)
                    {
                        image.rectTransform.GetWorldCorners(corners);
                        foreach (var corner in corners)
                        {
                            var p = rect.InverseTransformPoint(corner);
                            if (p.x < rect.rect.xMin + 7.9f || p.x > rect.rect.xMax - 7.9f || p.y < rect.rect.yMin + 7.9f || p.y > rect.rect.yMax - 7.9f || p.x > textLeft - 7.9f)
                                throw new Exception($"Icon escapes padding or overlaps text: {id}, {size}, {scale}, quantity={quantity}, {image.name}: {p}");
                        }
                    }
                    results.Add($"PASS {id}: panel {size}, scale {scale}, quantity {quantity}; all icon corners inside padding and clear of text");
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            var quest = config.DataAchievements.Single(q => q.StableId == 1014);
            Render(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/WindowAchievements.prefab"), "Documentation/UI/Quests-IconFit.png", instance => instance.GetComponent<global::Windows.WindowAchievements>().Bind(new[]{new Achievement(quest, quest.StableId)}));
            File.WriteAllLines("Documentation/chip-fit-validation.txt", results);
            Debug.Log("[ResourceChip] " + results.Count + " layout checks passed.");
        }
    }
}
