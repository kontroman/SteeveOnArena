using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MineArena.AI;
using MineArena.Levels;
using Unity.AI.Navigation;
using UnityEngine.AI;
using UnityEditor.SceneManagement;
using System.Reflection;
using Devotion.SDK.Controllers;
using Devotion.SDK.Managers;
using Devotion.SDK.Services.SaveSystem.Progress;
using MineArena.Controllers;
using MineArena.Managers;
using MineArena.PlayerSystem;
using MineArena.Structs;
using Devotion.SDK.UI;
using Cinemachine;

namespace MineArena.Editor
{
    public static class ExpeditionFlowValidation
    {
        [InitializeOnLoadMethod]
        static void Watch() => EditorApplication.update += () =>
        {
            if (File.Exists("Temp/validate-expedition.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                File.Delete("Temp/validate-expedition.request");
                try { Validate(); }
                catch (Exception ex) { File.WriteAllText("Documentation/expedition-validation.txt", "FAIL " + ex); Debug.LogException(ex); }
            }
            if (File.Exists("Temp/repair-village-spawns.request") && !EditorApplication.isCompiling && !EditorApplication.isUpdating)
            {
                File.Delete("Temp/repair-village-spawns.request");
                RepairVillageSpawns();
            }
            if (!File.Exists("Temp/inspect-expedition.request") || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete("Temp/inspect-expedition.request");
            var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Arenas/LocationVillage.prefab");
            var lines = new List<string>();
            lines.Add("Arena " + root.transform.position + " scale=" + root.transform.localScale);
            foreach (var s in root.GetComponentsInChildren<SpawnPoint>(true)) lines.Add("SPAWN " + s.name + " world=" + s.transform.position + " local=" + s.transform.localPosition + " parent=" + s.transform.parent.name);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                if (r.bounds.size.x > 15 || r.bounds.size.z > 15) lines.Add("GROUND " + r.name + " center=" + r.bounds.center + " size=" + r.bounds.size);
            var arena = root.GetComponent<Arena>(); lines.Add("Player=" + arena.PlayerSpawnPosition.position + " Portal=" + arena.PortalSpawnPoint.position);
            var hud = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab");
            foreach (Transform child in hud.transform) lines.Add("HUD " + child.name);
            if (!Application.isPlaying)
            {
                var sceneSurfaces = UnityEngine.Object.FindObjectsOfType<NavMeshSurface>();
                var scene = EditorSceneManager.NewPreviewScene();
                try
                {
                    foreach (var surface in sceneSurfaces) surface.RemoveData();
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(root, scene);
                    foreach (var surface in instance.GetComponentsInChildren<NavMeshSurface>()) surface.AddData();
                    foreach (var point in instance.GetComponentsInChildren<SpawnPoint>())
                        lines.Add("NAV " + point.name + " valid=" + NavMesh.SamplePosition(point.transform.position, out var hit, 2f, NavMesh.AllAreas) + " target=" + hit.position);
                }
                finally { EditorSceneManager.ClosePreviewScene(scene); foreach (var surface in sceneSurfaces) if (surface != null) surface.AddData(); }
            }
            File.WriteAllLines("Documentation/expedition-inspection.txt", lines);
        };

        [MenuItem("MineArena/Validation/Expedition Return And HUD")]
        public static void Validate()
        {
            if (Application.isPlaying || GameRoot.Instance != null || Player.Instance != null) throw new Exception("Run in Edit Mode without a player.");
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var scene = EditorSceneManager.NewPreviewScene();
            var cameraScene = EditorSceneManager.NewPreviewScene();
            var checks = new List<string>();
            void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks.Add("PASS " + message); }
            try
            {
                var go = new GameObject("Return fixture"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                var root = go.AddComponent<GameRoot>(); typeof(GameRoot).GetProperty("Instance").SetValue(null, root);
                var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/ScriptableObjects/GameConfig.asset");
                var progress = new PlayerProgress("return-fixture"); progress.TutorialProgress.Initialized = true;
                typeof(GameRoot).GetField("gameConfig", flags).SetValue(root, config); typeof(GameRoot).GetField("playerProgress", flags).SetValue(root, progress);
                var inventory = go.AddComponent<InventoryManager>();
                var managers = (Dictionary<Type, BaseManager>)typeof(GameRoot).GetField("_managers", flags).GetValue(root); managers[typeof(InventoryManager)] = inventory;
                inventory.InitManager();
                var uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/Managers/UIManager.prefab");
                var windows = new SerializedObject(uiPrefab.GetComponent<UIManager>()).FindProperty("_windows");
                Check(Enumerable.Range(0, windows.arraySize).Any(i => windows.GetArrayElementAtIndex(i).objectReferenceValue is MineArena.Windows.BlackWindow), "Construction fade window is registered in UIManager");
                var pricePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Windows/BuildingWindow/PriceElement.prefab");
                var priceObject = (GameObject)PrefabUtility.InstantiatePrefab(pricePrefab, scene);
                var price = priceObject.GetComponent<MineArena.Windows.Elements.BuildingPriceElement>();
                var cost = config.BuildingsDatabase.AllBuildings.First(b => b.name == "LumberjackBuilding").GetLevelByNumber(1).RequiredResources[0];
                price.Setup(cost);
                var amount = (TMPro.TMP_Text)typeof(MineArena.Windows.Elements.BuildingPriceElement).GetField("_amountText", flags).GetValue(price);
                Check(amount.text.Contains("0") && amount.text.Contains(cost.Amount.ToString()), "Building cost shows owned and required amounts");
                inventory.AddItemById(cost.Resource.Name, cost.Amount + 3); price.RefreshCost();
                Check(amount.text.Contains((cost.Amount + 3).ToString()), "Building resource display refreshes after inventory changes");
                var block = GameObject.CreatePrimitive(PrimitiveType.Cube); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(block, scene);
                block.transform.position = new Vector3(500, 0, 500); block.transform.localScale = new Vector3(10, 6, 10);
                var outside = MineArena.Buildings.BuildingConstructionSequence.ResolveOutsidePosition(block, block.transform.position);
                Check(Mathf.Abs(outside.x - 500) > 6 || Mathf.Abs(outside.z - 500) > 6, "Construction moves an interior marker beyond the building footprint");
                var playerObject = new GameObject("Persistent player", typeof(CharacterController), typeof(PlayerMovement), typeof(PlayerAttack), typeof(Player));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(playerObject, scene);
                var player = playerObject.GetComponent<Player>();
                typeof(Player).GetProperty("Instance").SetValue(null, player);
                typeof(Player).GetField("_components", flags).SetValue(player, playerObject.GetComponents<Component>().ToList());
                typeof(Player).GetProperty("Experience").SetValue(player, new PlayerExperience(progress.PlayerDataProgress));
                var movement = playerObject.GetComponent<PlayerMovement>(); var attack = playerObject.GetComponent<PlayerAttack>();
                playerObject.transform.position = new Vector3(150, -200, 300); movement.SetMovement(false); attack.SetComponentEnable(false);
                var cameraObject = new GameObject("Base camera", typeof(CinemachineVirtualCamera)); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.GetComponent<CinemachineVirtualCamera>();
                var destination = new Vector3(4, 2, 8); var rotation = Quaternion.Euler(0, 70, 0);
                player.RestoreAt(destination, rotation);
                Check(player.transform.position == destination && Quaternion.Angle(player.transform.rotation, rotation) < .01f, "Returning player is teleported to the base spawn");
                Check((bool)typeof(PlayerMovement).GetField("_canMove", flags).GetValue(movement) && (bool)typeof(PlayerAttack).GetField("_isEnabled", flags).GetValue(attack), "Movement and attacks are restored after extraction");
                Check(playerObject.GetComponent<CharacterController>().enabled && !PlayerMovement.IsPlayerDead, "Character controller and alive state are restored");
                Check(camera.Follow == player.transform, "Base camera follows the persistent player instead of a destroyed scene duplicate");
                player.RestoreAt(destination, rotation);
                Check(camera.Follow == player.transform && player.transform.position == destination, "Repeated returns remain stable");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, cameraScene);
                var outputCamera = cameraObject.AddComponent<Camera>();
                typeof(Player).GetField("_sceneCamera", flags).SetValue(player, outputCamera);
                var body = camera.AddCinemachineComponent<CinemachineTransposer>();
                body.m_FollowOffset = new Vector3(0, 10, -10);
                body.m_XDamping = body.m_YDamping = body.m_ZDamping = 0;
                cameraObject.AddComponent<CameraZoomController>();
                var staleTarget = new GameObject("Stale camera target"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(staleTarget, cameraScene);
                camera.Follow = staleTarget.transform; camera.LookAt = staleTarget.transform;
                player.RestoreAt(destination, rotation);
                Check(camera.Follow == player.transform && camera.LookAt == player.transform, "Persistent camera rig outside the base scene is rebound to the player");
                camera.InternalUpdateCameraState(Vector3.up, -1f);
                var before = camera.State.FinalPosition;
                player.transform.position += Vector3.right * 5;
                camera.InternalUpdateCameraState(Vector3.up, 1f);
                Check(Vector3.Distance(camera.State.FinalPosition - before, Vector3.right * 5) < .01f, "Cinemachine actually moves with the restored player");
                var hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/UI/PlayingWindow.prefab");
                var hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, scene); var window = hud.GetComponent<PlayingWindow>();
                window.RefreshTutorialVisibility();
                Check(hud.transform.Find("InventoryPanel").gameObject.activeSelf && !hud.transform.Find("PlayerPanel").gameObject.activeSelf && !hud.transform.Find("IconNavigation").gameObject.activeSelf, "Tutorial shows quick slots but hides player panel and navigation");
                progress.TutorialProgress.Step = TutorialStep.Complete; window.RefreshTutorialVisibility();
                Check(new[] { "PlayerPanel", "IconNavigation", "GiftNavigation", "CurrencyPouch", "Levels" }.All(n => hud.transform.Find(n).gameObject.activeSelf), "Finishing tutorial reveals all main HUD groups");
                progress.TutorialProgress.Step = TutorialStep.EquipSword; window.RefreshTutorialVisibility();
                Check(hud.transform.Find("InventoryPanel").gameObject.activeSelf && hud.transform.Find("IconNavigation").gameObject.activeSelf, "Equipment checkpoint reveals inventory navigation and retains quick slots");
                File.WriteAllLines("Documentation/expedition-validation.txt", checks);
                Debug.Log("Expedition return validation: " + checks.Count + " checks passed.");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                EditorSceneManager.ClosePreviewScene(cameraScene);
                typeof(Player).GetProperty("Instance").SetValue(null, null); typeof(GameRoot).GetProperty("Instance").SetValue(null, null);
            }
        }

        [MenuItem("MineArena/Balance/Fix Village Spawn Points")]
        public static void RepairVillageSpawns()
        {
            if (Application.isPlaying) throw new InvalidOperationException("Run in Edit Mode.");
            var surfaces = UnityEngine.Object.FindObjectsOfType<NavMeshSurface>();
            var root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Arenas/LocationVillage.prefab");
            try
            {
                foreach (var surface in surfaces) surface.RemoveData();
                foreach (var surface in root.GetComponentsInChildren<NavMeshSurface>()) surface.AddData();
                var points = new[] { new Vector2(-36,-38), new Vector2(-30,-40), new Vector2(-22,-40), new Vector2(-16,-30), new Vector2(-12,-22), new Vector2(-22,-18), new Vector2(-28,-10), new Vector2(-18,-4), new Vector2(-4,-8), new Vector2(2,-28), new Vector2(4,-40), new Vector2(-34,-26), new Vector2(-8,-38) };
                var spawn = root.GetComponent<Arena>().PlayerSpawnPosition.position;
                if (!NavMesh.SamplePosition(spawn, out var playerHit, 3, NavMesh.AllAreas)) throw new Exception("Village player spawn is off the NavMesh.");
                var valid = new List<Vector3>();
                foreach (var point in points)
                {
                    if (!NavMesh.SamplePosition(new Vector3(point.x, 2.4f, point.y), out var hit, 2.5f, NavMesh.AllAreas)) continue;
                    var path = new NavMeshPath();
                    if (NavMesh.CalculatePath(hit.position, playerHit.position, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete) valid.Add(hit.position);
                }
                if (valid.Count < 4) throw new Exception("Not enough connected interior village spawn points: " + valid.Count);
                var markers = root.GetComponentsInChildren<SpawnPoint>(true);
                for (int i = 0; i < markers.Length; i++)
                {
                    markers[i].enabled = i < valid.Count;
                    if (i < valid.Count) { markers[i].transform.position = valid[i]; markers[i].name = "VillageSpawn" + (i + 1); }
                }
                var data = new SerializedObject(root.GetComponentInChildren<WaveSpawner>(true));
                var list = data.FindProperty("_spawnPoints"); list.arraySize = valid.Count;
                for (int i = 0; i < valid.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = markers[i];
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, "Assets/Prefabs/Arenas/LocationVillage.prefab");
                File.WriteAllLines("Documentation/village-spawn-repair.txt", valid.Select(p => p.ToString()));
            }
            finally { PrefabUtility.UnloadPrefabContents(root); foreach (var surface in surfaces) if (surface != null) surface.AddData(); }
        }
    }
}
