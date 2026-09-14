using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Controllers;
using MineArena.PlayerSystem;
using MineArena.UI;
using MineArena.Windows;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static class PlayerStatsFlowValidation
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Invoke(object target, string method, params object[] args) => target.GetType().GetMethod(method, Fields).Invoke(target, args);
        [MenuItem("MineArena/Validation/Player Stats Flow")]
        public static void Validate()
        {
            var previousRoot = GameRoot.Instance;
            var previousPlayer = Player.Instance;
            var scene = EditorSceneManager.NewPreviewScene();
            void Check(bool condition, string message) { if (!condition) throw new Exception(message); File.AppendAllText("Documentation/player-progression-validation.txt", "PASS: " + message + "\n"); }
            try
            {
                var fixture = new GameObject("Progression test fixture");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fixture, scene);
                var root = fixture.AddComponent<GameRoot>();
                typeof(GameRoot).GetProperty("Instance").SetValue(null, root);
                var progress = new PlayerProgress("progression-fixture");
                typeof(GameRoot).GetField("playerProgress", Fields).SetValue(root, progress);
                progress.PlayerDataProgress.CacheExperience(11, 120);
                var playerObject = new GameObject("Progression player"); playerObject.transform.SetParent(fixture.transform);
                var player = playerObject.AddComponent<Player>();
                typeof(Player).GetProperty("Instance").SetValue(null, player);
                var xp = new PlayerExperience(progress.PlayerDataProgress);
                typeof(Player).GetProperty("Experience").SetValue(player, xp);
                var health = playerObject.AddComponent<MineArena.Game.Health.Health>();
                health.SetMaximumHealth(100); health.SetCurrentValue(70, false);
                var development = playerObject.AddComponent<PlayerDevelopment>(); Invoke(development, "Awake");

                var canvas = new GameObject("Stats test canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
                canvas.transform.SetParent(fixture.transform); canvas.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                ((RectTransform)canvas.transform).sizeDelta = new Vector2(1920, 1080);
                var managerObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/Managers/UIManager.prefab"), fixture.transform);
                var manager = managerObject.GetComponent<UIManager>();
                typeof(UIManager).GetField("_mainCanvas", Fields).SetValue(manager, canvas.GetComponent<Canvas>());
                ((Dictionary<Type, BaseManager>)typeof(GameRoot).GetField("_managers", Fields).GetValue(root))[typeof(UIManager)] = manager;
                var hud = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab"), canvas.transform);
                var panel = hud.transform.Find("PlayerPanel").GetComponent<PlayerPanelUI>();
                Invoke(panel, "Awake");
                panel.GetComponent<Button>().onClick.Invoke();
                var window = canvas.GetComponentInChildren<PlayerStatsWindow>();
                Check(window != null && window.gameObject.activeSelf && manager.HasOpenDialog, "Whole HUD panel opens registered stats dialog");
                Invoke(window, "Awake"); Invoke(window, "Bind", xp);
                var rows = (PlayerStatsWindow.AttributeRow[])typeof(PlayerStatsWindow).GetField("rows", Fields).GetValue(window);
                rows[0].plus.onClick.Invoke(); rows[1].plus.onClick.Invoke(); rows[2].plus.onClick.Invoke(); rows[3].plus.onClick.Invoke(); rows[4].plus.onClick.Invoke();
                Check(progress.PlayerDataProgress.CopyDevelopment().Sum() == 0 && health.MaxValue == 100, "Plus buttons leave save and live stats unchanged");
                rows[2].minus.onClick.Invoke(); Check(rows[2].rank.text == "0 / 50", "Minus undoes tentative increase");
                window.SaveChanges();
                Check(progress.PlayerDataProgress.CopyDevelopment().Sum() == 4 && health.MaxValue == 102f && health.CurrentValue == 70, "Save applies points and HP without free healing");
                Check(Mathf.Approximately(development.MovementMultiplier, 1.005f) && Mathf.Approximately(development.CriticalChance, .005f), "Speed and luck use saved ranks");
                Check(Mathf.Abs(development.ModifyIncomingDamage(101f) - 100) < .01f, "Defense changes real incoming damage");
                rows[2].plus.onClick.Invoke(); window.CloseWindow();
                Check(!window.gameObject.activeSelf && !manager.HasOpenDialog && progress.PlayerDataProgress.CopyDevelopment()[2] == 0, "Close discards uncommitted points");
                panel.Open(); Invoke(window, "Bind", xp);
                Check(rows[2].rank.text == "0 / 50", "Reopening starts from saved allocation");
                rows[2].plus.onClick.Invoke(); window.SaveChanges();
                Check(Mathf.Approximately(development.AttackMultiplier, 1.01f), "Attack upgrade applies to damage multiplier");
                rows[0].minus.onClick.Invoke(); rows[2].plus.onClick.Invoke(); window.SaveChanges();
                Check(health.MaxValue == 102 && progress.PlayerDataProgress.CopyDevelopment()[0] == 1 && progress.PlayerDataProgress.CopyDevelopment()[2] == 2, "Saved points cannot be refunded after reopening");
                Check(rows.All(row => !row.minus.interactable), "Save disables all minus buttons");
                window.Adjust(0, -1);
                Check(rows[0].rank.text == "1 / 50", "Direct adjustment cannot refund a saved point");
                rows[0].plus.onClick.Invoke();
                Check(rows[0].minus.interactable, "New tentative increase enables minus");
                rows[0].minus.onClick.Invoke();
                Check(rows[0].rank.text == "1 / 50" && !rows[0].minus.interactable, "Minus only undoes the unsaved increase");
                var savedBeforeXp = progress.PlayerDataProgress.CopyDevelopment();
                rows[4].plus.onClick.Invoke(); xp.AddExperience(xp.ExperiencePerLevel);
                Check(rows[4].rank.text == "2 / 50" && progress.PlayerDataProgress.CopyDevelopment().SequenceEqual(savedBeforeXp), "Level up preserves unsaved draft and grants another point");
                var title = window.GetComponentsInChildren<TMP_Text>().First(t => t.name == "PlayerName");
                Check(title.text.StartsWith("Player") && !title.richText, "Fallback name appears safely");
                foreach (var icon in window.GetComponentsInChildren<PlayerAttributeIcon>()) Check(icon.GetComponent<CanvasRenderer>() != null, "Icon renderer exists: " + icon.attribute);
                GameUiBuilder.Render(window.gameObject, "Documentation/UI/PlayerStatsWindow-populated.png");
                window.CloseWindow(); Invoke(window, "OnDisable");
            }
            finally
            {
                typeof(Player).GetProperty("Instance").SetValue(null, previousPlayer);
                typeof(GameRoot).GetProperty("Instance").SetValue(null, previousRoot);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
