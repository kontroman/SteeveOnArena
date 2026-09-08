using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MineArena.Levels;
using MineArena.Items;
using MineArena.UI;
using MineArena.Structs;
using MineArena.Windows.SelectLevel;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static class LevelSelectionValidation
    {
        [MenuItem("MineArena/UI/Validate Level Selection")]
        public static void Run()
        {
            var root = PrefabUtility.LoadPrefabContents(LevelSelectionPrefabBuilder.WindowPath);
            var emptyConfig = ScriptableObject.CreateInstance<LevelConfig>();
            try
            {
                var view = root.GetComponent<LevelSelectionView>();
                var fields = new SerializedObject(view);
                var iterator = fields.GetIterator();
                while (iterator.NextVisible(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                        Require(iterator.objectReferenceValue != null, "Missing reference: " + iterator.propertyPath);
                var config = AssetDatabase.FindAssets("t:GameConfig").Select(AssetDatabase.GUIDToAssetPath)
                    .Select(AssetDatabase.LoadAssetAtPath<GameConfig>).First(c => c.Levels.Count > 0);
                var available = config.Levels.First(c => c != null && c.LevelPrefab != null);
                Require(config.Levels.All(c => c != null && c.LevelPrefab != null), "All configured levels have gameplay prefabs");
                var cards = (RectTransform)fields.FindProperty("cardsRoot").objectReferenceValue;
                var resources = (RectTransform)fields.FindProperty("resourcesRoot").objectReferenceValue;
                var start = (Button)fields.FindProperty("startButton").objectReferenceValue;
                var list = (ScrollRect)fields.FindProperty("listScroll").objectReferenceValue;
                typeof(LevelSelectionView).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(view, null);
                int starts = 0;
                view.StartRequested += _ => starts++;
                var levels = new List<LevelConfig> { available, available };
                view.Refresh(levels, 0);
                Require(cards.childCount == 2, "Initial cards");
                Require(start.interactable, "Unlocked start enabled");
                start.onClick.Invoke();
                Require(starts == 1, "Unlocked start event");
                view.Select(1);
                Require(!start.interactable, "Locked start disabled");
                start.onClick.Invoke();
                Require(starts == 1, "Locked start rejected even when invoked directly");
                view.Refresh(levels, 1);
                Require(start.interactable, "Progress refresh unlocks selected level");
                Require(cards.childCount == 2, "Reopening does not duplicate cards");
                int expected = available.AvailableResources.Count(r => r != null);
                view.Select(0);
                view.Select(1);
                Require(resources.childCount == expected, "Switching does not duplicate resources");
                var chip = resources.GetComponentInChildren<LevelResourceChip>(true);
                var blockItem = available.AvailableResources.First(i => i.BlockStyleIcon && i is StackableItemConfig);
                var flatItem = AssetDatabase.LoadAssetAtPath<ItemConfig>("Assets/ScriptableObjects/Configs/Drops/CoalItem.asset");
                chip.Bind(blockItem, 5);
                Require(chip.GetComponentInChildren<ResourceIcon>() != null, "Block resources use existing cube prefab");
                chip.Bind(flatItem, 5);
                Require(chip.GetComponentInChildren<ResourceIcon>() == null, "Non-block resources use flat icon");
                chip.Bind(blockItem, null);
                Require(chip.GetComponentsInChildren<ResourceIcon>().Length == 1, "Rebinding reuses cube without duplicates");
                var many = Enumerable.Repeat(available, 40).ToList();
                view.Refresh(many, 0);
                LayoutRebuilder.ForceRebuildLayoutImmediate(cards);
                Require(cards.childCount == 40, "Forty levels generated");
                Require(cards.rect.height > list.viewport.rect.height, "Long list scrolls");
                view.Refresh(new List<LevelConfig> { null, emptyConfig }, 1);
                Require(cards.childCount == 1 && !start.interactable, "Null entry and missing level prefab");
                Require(resources.childCount == 0, "Missing resource list");
                view.Refresh(null, 0);
                Require(cards.childCount == 0 && !start.interactable, "Empty level list");
                start.onClick.Invoke();
                Require(starts == 1, "Empty list cannot start");
                var legacyId = AssetDatabase.LoadAssetAtPath<GameObject>(LevelSelectionPrefabBuilder.WindowPath)
                    .GetComponent<MineArena.Windows.SelectLevelWindow>();
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(legacyId, out string guid, out long id);
                Require(guid == "52a0b70e8219959428f317ad27a12e9c" && id == 4206247176245032299,
                    "Existing UIManager reference preserved");
                Directory.CreateDirectory("Documentation");
                File.WriteAllText("Documentation/level-selection-validation.txt",
                    "PASS: configured level prefabs; cube/flat/rebound resource icons; serialized references; unlocked launch; locked launch guard; progress refresh; repeated refresh; resource cleanup; 40-level scroll; null entries; missing prefab/resources; empty list; original UIManager GUID/fileID.\n" +
                    "Unity " + Application.unityVersion + "\n" + DateTime.UtcNow.ToString("u") + "\n" +
                    "Editor validation; full gameplay scene transition and portal completion require a Play Mode playthrough.\n");
                Debug.Log("[LevelSelection] Validation PASS (including cube resource icons)");
            }
            finally
            {
                Object.DestroyImmediate(emptyConfig);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Level selection validation: " + message);
        }
    }
}
