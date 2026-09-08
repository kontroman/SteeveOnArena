using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MineArena.Game.UI;
using MineArena.Items;
using MineArena.PlayerSystem;
using MineArena.Structs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Health = MineArena.Game.Health.Health;

namespace MineArena.Editor
{
    public static class InventoryArmorValidation
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/inventory-armor.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating
                || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Validate(); }
            catch (Exception e) { File.WriteAllText("Documentation/inventory-armor-validation.txt", e.ToString()); Debug.LogException(e); }
        };

        [MenuItem("MineArena/Validation/Inventory Icons And Armor")]
        public static void Validate()
        {
            var report = new List<string>();
            void Check(bool ok, string message)
            {
                if (!ok) throw new InvalidOperationException(message);
                report.Add("PASS " + message);
            }
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/ScriptableObjects/Configs/Game/ItemDatabase.asset");
            var items = AssetDatabase.FindAssets("t:ItemConfig").Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ItemConfig>).ToArray();
            foreach (var item in items)
                Check(item != null && item.Icon != null && item.Icon.rect.width > 0, "Icon: " + item.name);
            Check(database.AllItems.All(i => i != null && i.Icon != null), "Every registered inventory item has a Sprite");
            Check(items.All(i => database.AllItems.Contains(i)), "Every item config is registered");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Scenes/PlayerV2.0.prefab");
            Check(prefab.GetComponent<Health>() != null && prefab.GetComponent<PlayerEquipment>() != null,
                "Player health and defense provider share a GameObject");
            var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab");
            var barPrefab = hudPrefab.GetComponentInChildren<PlayerArmorBar>(true);
            Check(barPrefab != null, "HUD armor component attached");

            var scene = EditorSceneManager.NewPreviewScene();
            GameObject go = null, hud = null;
            try
            {
                go = new GameObject("ArmorValidation");
                go.SetActive(false);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var equipment = go.AddComponent<PlayerEquipment>();
                var health = go.AddComponent<Health>();
                typeof(Health).GetField("_maxHealth", Fields).SetValue(health, 100f);
                hud = UnityEngine.Object.Instantiate(barPrefab.gameObject);
                hud.SetActive(false);
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(hud, scene);
                var bar = hud.GetComponent<PlayerArmorBar>();
                var fill = (Image)typeof(PlayerArmorBar).GetField("_fillImage", Fields).GetValue(bar);
                var label = (TMP_Text)typeof(PlayerArmorBar).GetField("_valueText", Fields).GetValue(bar);
                Check(fill != null && label != null, "HUD fill and label references resolve");
                typeof(PlayerArmorBar).GetField("_equipment", Fields).SetValue(bar, equipment);
                var armor = items.OfType<ArmorConfig>().ToArray();
                foreach (var grade in new[] { ArmorGrade.Leather, ArmorGrade.Iron, ArmorGrade.Gold, ArmorGrade.Diamond, ArmorGrade.Netherite })
                {
                    var set = armor.Where(a => a.Grade == grade).ToArray();
                    foreach (ArmorSlot slot in Enum.GetValues(typeof(ArmorSlot)))
                    {
                        var field = new[] { "_helmet", "_chest", "_leggings", "_boots" }[(int)slot];
                        typeof(PlayerEquipment).GetField(field, Fields).SetValue(equipment, set.SingleOrDefault(a => a.Slot == slot));
                    }
                    float expected = set.Sum(a => a.Resist) / 100f;
                    Check(expected <= .5f, grade + " current set mitigates at most 50%");
                    health.SetCurrentValue(100, false);
                    health.TakeDamage(new DamageData(20, health));
                    Check(Mathf.Approximately(health.CurrentValue, 100 - 20 * (1 - expected)), grade + " actual Health.TakeDamage: " + health.CurrentValue);
                    typeof(PlayerArmorBar).GetMethod("RefreshValue", Fields).Invoke(bar, null);
                    Check(Mathf.Approximately(fill.fillAmount, expected) && label.text == Mathf.RoundToInt(expected * 100) + "%", grade + " HUD matches damage reduction");
                }
                foreach (var field in new[] { "_helmet", "_chest", "_leggings", "_boots" })
                    typeof(PlayerEquipment).GetField(field, Fields).SetValue(equipment, null);
                typeof(PlayerArmorBar).GetMethod("RefreshValue", Fields).Invoke(bar, null);
                Check(fill.fillAmount == 0 && label.text == "0%", "Removing all armor clears HUD");
                Check(equipment.ModifyIncomingDamage(-10) == 0, "Negative damage cannot heal through defense");
            }
            finally
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
                if (hud != null) UnityEngine.Object.DestroyImmediate(hud);
                EditorSceneManager.ClosePreviewScene(scene);
            }
            Directory.CreateDirectory("Documentation");
            File.WriteAllLines("Documentation/inventory-armor-validation.txt", report);
            Debug.Log("Inventory and armor validation passed: " + report.Count);
        }
    }
}
