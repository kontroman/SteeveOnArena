using System.IO;
using System.Linq;
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
        private const string StorageWindowPath = UI + "StorageWindow.prefab";

        [InitializeOnLoadMethod]
        private static void BuildMissingStorageWindow()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(StorageWindowPath)) BuildStorageWindow();
            };
        }

        [MenuItem("MineArena/UI/Rebuild Storage Window")]
        public static void BuildStorageWindow()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var inventoryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UI + "InventoryWindow.prefab");
            var sourceCell = inventoryPrefab.GetComponentsInChildren<InventoryCellUI>(true).First(c => c.GetComponent<Image>() != null);
            var root = Edit<StorageWindow>(StorageWindowPath);
            var window = root.GetComponent<StorageWindow>();
            var storageConfig = AssetDatabase.LoadAssetAtPath<MineArena.Buildings.BuildingConfig>("Assets/ScriptableObjects/Configs/Buildings/StorageBuilding.asset");
            var frame = Window(root, "Хранилище", 1380, 850);
            Ribbon(frame, "587F79", null);
            Text("Description", frame, "Вещи игрока и запасы хранилища", 21, 36, 112, 1290, 36);
            var left = Panel("PlayerInventoryPanel", frame, "card"); Box(left, 32, 162, 644, 576);
            var right = Panel("StoragePanel", frame, "card"); Box(right, 704, 162, 644, 576);
            Text("InventoryTitle", left, "ИНВЕНТАРЬ ИГРОКА", 24, 20, 18, 600, 38, true);
            Text("StorageTitle", right, "СКЛАД", 24, 20, 18, 600, 38, true);
            var inventoryCount = Text("InventoryCount", left, "Занято ячеек: 0", 19, 20, 62, 600, 28);
            var storageCount = Text("StorageCount", right, "Занято ячеек: 0", 19, 20, 62, 600, 28);
            var inventoryScroll = Scroll(left, "InventoryScroll", 20, 106, 604, 444);
            var storageScroll = Scroll(right, "StorageScroll", 20, 106, 604, 444);
            foreach (var scroll in new[] { inventoryScroll, storageScroll })
            {
                Grid(scroll.content, 6, new Vector2(90, 78));
                scroll.content.GetComponent<GridLayoutGroup>().spacing = new Vector2(10, 10);
                for (int i = 0; i < (scroll == storageScroll ? storageConfig.Levels.Max(level => level.StorageSlots) : 30); i++)
                {
                    var cell = Object.Instantiate(sourceCell, scroll.content);
                    cell.name = "Slot_" + (i + 1).ToString("00");
                    foreach (var drag in cell.GetComponentsInChildren<InventoryCellDragHandler>(true)) Object.DestroyImmediate(drag);
                    cell.Clear();
                    cell.gameObject.SetActive(true);
                    if (scroll == storageScroll) StorageLockedSlotGraphic.SetLocked(cell, i >= storageConfig.GetLevelByNumber(1).StorageSlots);
                }
            }
            var empty = Text("StorageEmpty", frame, "Склад пока пуст", 21, 724, 750, 600, 30);
            Text("InventoryHint", frame, "Блоки, материалы и снаряжение", 20, 52, 750, 604, 30);
            Set(window, "_inventoryContent", inventoryScroll.content, "_storageContent", storageScroll.content,
                "_cellTemplate", inventoryScroll.content.GetChild(0).GetComponent<InventoryCellUI>(),
                "_inventoryCount", inventoryCount, "_storageCount", storageCount, "_storageEmpty", empty.gameObject,
                "_storageBuilding", storageConfig);
            Save(root, StorageWindowPath);
            RegisterStorageWindow();
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Documentation/UI");
            Render(AssetDatabase.LoadAssetAtPath<GameObject>(StorageWindowPath), "Documentation/UI/StorageWindow.png");
            RenderStorageItemsPreview();
            Debug.Log("[StorageWindow] Prefab built and registered.");
        }

        [MenuItem("MineArena/UI/Preview Storage Items")]
        public static void RenderStorageItemsPreview()
        {
            Render(AssetDatabase.LoadAssetAtPath<GameObject>(StorageWindowPath), "Documentation/UI/StorageWindow-items.png", root =>
            {
                var inventory = root.transform.Find("BeigeWindow/PlayerInventoryPanel/InventoryScroll/Viewport/Content");
                var names = new[] { "Stone", "WoodOak", "IronIngot", "Wheat", "HealingPotion", "GlassBottle", "DiamondOre" };
                var samples = names.Select((name, i) => new MineArena.Items.StackableItem(
                    AssetDatabase.LoadAssetAtPath<MineArena.Items.StackableItemConfig>("Assets/ScriptableObjects/Configs/Drops/" + name + ".asset"), 8 + i * 7)).ToArray();
                for (int i = 0; i < samples.Length; i++) inventory.GetChild(i).GetComponent<InventoryCellUI>().Setup(samples[i]);
                var sword = AssetDatabase.LoadAssetAtPath<MineArena.Items.ItemConfig>("Assets/ScriptableObjects/Configs/Equipment/Swords/IronSwordItem.asset");
                inventory.GetChild(samples.Length).GetComponent<InventoryCellUI>().Setup(new MineArena.Items.Item(sword.Name, sword.Prefab, sword.Icon));
                root.GetComponent<StorageWindow>().SetStorageContents(samples.Take(4));
                Find(root.transform, "InventoryCount").GetComponent<TMP_Text>().text = "Занято ячеек: 8";
            });
        }

        private static void RegisterStorageWindow()
        {
            const string path = "Assets/DevotionSDK/Prefabs/Managers/UIManager.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var manager = root.GetComponent<Devotion.SDK.Managers.UIManager>();
                var so = new SerializedObject(manager);
                var windows = so.FindProperty("_windows");
                if (windows == null) throw new System.InvalidOperationException("Window registry not found");
                var prefab = AssetDatabase.LoadAssetAtPath<StorageWindow>(StorageWindowPath);
                bool exists = false;
                for (int i = 0; i < windows.arraySize; i++)
                    if (windows.GetArrayElementAtIndex(i).objectReferenceValue is StorageWindow) exists = true;
                if (!exists) { int index = windows.arraySize++; windows.GetArrayElementAtIndex(index).objectReferenceValue = prefab; }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
