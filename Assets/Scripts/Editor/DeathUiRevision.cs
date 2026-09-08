using System;
using System.IO;
using System.Reflection;
using MineArena.Controllers;
using MineArena.Game.Health;
using MineArena.Game.UI;
using MineArena.Structs;
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
        [InitializeOnLoadMethod]
        private static void WatchDeathUi()
        {
            EditorApplication.update += () =>
            {
                if (!File.Exists("Temp/death-ui.request") || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete("Temp/death-ui.request");
                try { BuildDeathWindow(); ValidatePlayerHealthBar(); }
                catch (Exception ex) { File.WriteAllText("Documentation/death-ui-validation.txt", "FAIL " + ex); Debug.LogException(ex); }
            };
        }

        [MenuItem("MineArena/UI/Rebuild Death Window")]
        public static void BuildDeathWindow()
        {
            const string path = "Assets/Resources/UI/DeathWindow.prefab";
            var root = Edit<DeathWindow>(path);
            var frame = Window(root, "ВЫ ПОГИБЛИ", 880, 670);
            Ribbon(frame, "985D50", null);
            var card = Panel("ReviveCard", frame, "inset"); Box(card, 36, 132, 808, 212);
            Text("CardTitle", card, "ЕЩЁ ОДИН ШАНС", 28, 28, 22, 752, 45, true);
            var description = Text("Description", card, "Посмотрите рекламу, чтобы продолжить бой\nс полным здоровьем на месте гибели.", 25, 28, 78, 752, 80);
            description.enableWordWrapping = true;
            Text("Protection", frame, "После возрождения — 3 секунды защиты", 23, 40, 360, 800, 38);
            var status = Text("Status", frame, "Продолжите сражение или вернитесь в деревню.", 22, 40, 408, 800, 66);
            status.enableWordWrapping = true;
            var revive = Button("Revive", frame, "ВОЗРОДИТЬСЯ ЗА РЕКЛАМУ", 36, 490, 808, 64);
            revive.GetComponent<Image>().sprite = AccentSprite("button-teal", "3D8990", "28555C");
            var village = Button("Village", frame, "ВЕРНУТЬСЯ В ДЕРЕВНЮ", 36, 570, 808, 60, false);
            Set(root.GetComponent<DeathWindow>(), "_reviveButton", revive, "_villageButton", village, "_statusText", status);
            Save(root, path);
            AssetDatabase.SaveAssets();
            Render(AssetDatabase.LoadAssetAtPath<GameObject>(path), "Documentation/UI/DeathWindow.png");
        }

        [MenuItem("MineArena/Validation/Player Health Bar")]
        public static void ValidatePlayerHealthBar()
        {
            if (Application.isPlaying) throw new Exception("Run health bar validation in Edit Mode.");
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var singleton = typeof(Player).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            var previous = singleton.GetValue(null);
            var playerObject = new GameObject("Health validation player"); playerObject.SetActive(false);
            var uiObject = new GameObject("Health validation UI", typeof(RectTransform)); uiObject.SetActive(false);
            var results = new System.Collections.Generic.List<string>();
            void Check(bool ok, string message) { if (!ok) throw new Exception(message); results.Add("PASS " + message); }
            void Field(object obj, string name, object value) => obj.GetType().GetField(name, flags).SetValue(obj, value);
            void Call(object obj, string name) => obj.GetType().GetMethod(name, flags).Invoke(obj, null);
            PlayerHealthBar bar = null;
            try
            {
                var player = playerObject.AddComponent<Player>();
                var health = playerObject.AddComponent<Health>();
                typeof(Health).GetField("_maxHealth", flags).SetValue(health, 100f);
                health.SetCurrentValue(100f, false);
                singleton.SetValue(null, player);
                var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image)).GetComponent<Image>(); fill.transform.SetParent(uiObject.transform);
                fill.type = UnityEngine.UI.Image.Type.Filled; fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
                var label = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>(); label.transform.SetParent(uiObject.transform);
                bar = uiObject.AddComponent<PlayerHealthBar>();
                Field(bar, "_fillImage", fill); Field(bar, "_valueText", label);
                Field(bar, "_smoothFill", false); Field(bar, "_smoothTextValue", false);
                Call(bar, "Awake"); Call(bar, "OnEnable");
                Check(fill.fillAmount == 1f && label.text == "100 / 100", "Initial health is synchronized");
                // Avoid editor godmode/tutorial global state while exercising the actual damage method.
                singleton.SetValue(null, null);
                health.TakeDamage(new DamageData(25f, health));
                Check(health.CurrentValue == 75f && fill.fillAmount == .75f && label.text == "75 / 100", "Damage updates fill and numeric HP");
                singleton.SetValue(null, player);
                Call(bar, "OnDisable");
                health.SetCurrentValue(50f, false);
                Check(fill.fillAmount == .75f, "Hidden HUD is unsubscribed");
                Call(bar, "OnEnable");
                Check(fill.fillAmount == .5f && label.text == "50 / 100", "Reopened HUD immediately reads current health");
                health.ChangeValue(-10f);
                Check(fill.fillAmount == .4f, "Reopened HUD receives subsequent damage");
                for (int i = 0; i < 3; ++i) { Call(bar, "OnDisable"); Call(bar, "OnEnable"); }
                health.ChangeValue(-10f);
                Check(fill.fillAmount == .3f && label.text == "30 / 100", "Repeated HUD reopening preserves subscription");
                Field(bar, "_smoothFill", true); Field(bar, "_smoothTextValue", true);
                health.SetCurrentValue(0f, false);
                Check(fill.fillAmount == 0f && label.text == "0 / 100", "Death immediately shows zero even with smoothing enabled");
                Call(bar, "OnDisable"); health.RestoreFullHealth(); Call(bar, "OnEnable");
                Check(fill.fillAmount == 1f && label.text == "100 / 100", "Revival synchronizes full HP on HUD reopening");
                var hud = AssetDatabase.LoadAssetAtPath<GameObject>(UI + "PlayingWindow.prefab").GetComponentInChildren<PlayerHealthBar>(true);
                var image = (Image)typeof(PlayerHealthBar).GetField("_fillImage", flags).GetValue(hud);
                Check(image != null && image.type == UnityEngine.UI.Image.Type.Filled && image.fillMethod == UnityEngine.UI.Image.FillMethod.Horizontal && image.fillOrigin == 0, "Actual HUD prefab uses horizontal left-to-right filled Image");
                Check(typeof(PlayerHealthBar).GetField("_valueText", flags).GetValue(hud) != null, "Actual HUD prefab has numeric HP reference");
                File.WriteAllLines("Documentation/death-ui-validation.txt", results);
                Debug.Log("[DeathUI] " + results.Count + " health bar checks passed.");
            }
            finally
            {
                if (bar != null) Call(bar, "OnDisable");
                singleton.SetValue(null, previous);
                Object.DestroyImmediate(uiObject); Object.DestroyImmediate(playerObject);
            }
        }
    }
}
