using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using MineArena.Items;
using MineArena.Managers;
using UnityEngine.UI;
using MineArena.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MineArena.Editor
{
    public static class WorkshopMiningRevisionValidation
    {
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/workshop-mining.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Probe(); }
            catch (Exception e) { File.WriteAllText("Documentation/workshop-mining-validation.txt", e.ToString()); }
        };

        private static void Probe()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Arenas/LocationMine.prefab"));
                SceneManager.MoveGameObjectToScene(root, scene);
                root.transform.position = new Vector3(0, -0.59f, 0);
                var arena = root.GetComponent<Arena>();
                var point = arena.PortalSpawnPoint;
                var portalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Buildings/Portal/ForestPortal.prefab");
                var portal = UnityEngine.Object.Instantiate(portalPrefab);
                SceneManager.MoveGameObjectToScene(portal, scene);
                portal.transform.SetPositionAndRotation(point.position, point.rotation);
                var log = new StringBuilder();
                log.AppendLine("Spawn " + point.position + " scale " + point.lossyScale + " rotation " + point.rotation.eulerAngles);
                foreach (var r in portal.GetComponentsInChildren<Renderer>()) log.AppendLine("Portal " + r.name + " bounds " + r.bounds);
                foreach (var collider in root.GetComponentsInChildren<Collider>())
                    if (collider.Raycast(new Ray(point.position + Vector3.up * 50, Vector3.down), out var hit, 100))
                        log.AppendLine("Ground " + collider.name + " " + hit.point + " normal " + hit.normal);
                ValidateInventoryAndPrompt(scene, log);
                var frameBounds = portal.GetComponent<Renderer>().bounds;
                if (frameBounds.size.y < 5.9f) throw new Exception("Portal is not upright");
                bool grounded = false;
                foreach (var collider in root.GetComponentsInChildren<Collider>())
                    if (collider.Raycast(new Ray(point.position + Vector3.up, Vector3.down), out var hit, 5))
                        grounded |= Mathf.Abs(frameBounds.min.y - hit.point.y) < 0.03f;
                if (!grounded) throw new Exception("Portal does not rest on the mine floor");
                log.AppendLine("PASS: portal upright and grounded within 3 cm.");
                log.AppendLine("Unity editor checks; full gameplay playtest still required.");
                Render(scene, point.position, "Documentation/UI/MinePortal.png");
                File.WriteAllText("Documentation/workshop-mining-validation.txt", log.ToString());
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static void ValidateInventoryAndPrompt(Scene scene, StringBuilder log)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
            var wood = AssetDatabase.LoadAssetAtPath<PickaxeConfig>("Assets/ScriptableObjects/Configs/Equipment/Pickaxe/WoodenPickaxe.asset");
            if (wood.CraftCosts.Count != 0) throw new Exception("Wooden pickaxe still has a recipe");
            var workshop = AssetDatabase.LoadAssetAtPath<MineArena.Buildings.BuildingConfig>("Assets/ScriptableObjects/Configs/Buildings/LumberjackBuilding.asset");
            if (workshop.Levels.Any(level => level.Unlocks.Contains(wood))) throw new Exception("Workshop still unlocks wooden pickaxe");
            var go = new GameObject("Inventory validation");
            SceneManager.MoveGameObjectToScene(go, scene);
            var inventory = go.AddComponent<InventoryManager>();
            var item = new Item("WoodenPickaxe", null, null);
            var items = (System.Collections.Generic.List<Item>)typeof(InventoryManager).GetField("_items", flags).GetValue(inventory);
            items.Add(item);
            inventory.RemoveItem(item, 99);
            if (!items.Contains(item) || InventoryManager.CanDiscard(item)) throw new Exception("Starter pickaxe can be removed");
            if (!InventoryManager.CanDiscard(new Item("IronPickaxe", null, null))) throw new Exception("Other tools cannot be removed");
            log.AppendLine("PASS: starter pickaxe absent from recipes/unlocks, removal blocked; other tools remain discardable.");

            var stone = AssetDatabase.LoadAssetAtPath<WeaponItemConfig>("Assets/ScriptableObjects/Configs/Equipment/Swords/StoneSwordItem.asset");
            var iron = AssetDatabase.LoadAssetAtPath<WeaponItemConfig>("Assets/ScriptableObjects/Configs/Equipment/Swords/IronSwordItem.asset");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Textures/Materials/Equipment/Sword/Stone_sword.mat");
            if (stone.Icon == null || stone.Icon == iron.Icon || material.GetTexture("_BaseMap") != stone.Icon.texture)
                throw new Exception("Stone sword texture/icon assignment invalid");
            log.AppendLine("PASS: stone sword has its own icon and matching material texture.");

            var prompt = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/MiningPrompt.prefab"));
            SceneManager.MoveGameObjectToScene(prompt, scene);
            var billboard = go.AddComponent<BillboardCanvas>();
            var icon = prompt.transform.Find("Iron pickaxe").GetComponent<Image>();
            var originalSprite = icon.sprite;
            var originalSize = icon.rectTransform.sizeDelta;
            typeof(BillboardCanvas).GetField("_miningIcon", flags).SetValue(billboard, icon);
            typeof(BillboardCanvas).GetField("_pickaxeSprite", flags).SetValue(billboard, originalSprite);
            typeof(BillboardCanvas).GetField("_pickaxeSize", flags).SetValue(billboard, originalSize);
            billboard.SetMining(true);
            var cross = icon.transform.Find("Cancel cross");
            if (icon.enabled || cross == null || !cross.gameObject.activeSelf || cross.childCount != 2) throw new Exception("Cancel cross not shown");
            billboard.SetMining(false);
            if (!icon.enabled || cross.gameObject.activeSelf || icon.sprite != originalSprite || icon.rectTransform.sizeDelta != originalSize) throw new Exception("Mining icon not restored");
            var manager = go.AddComponent<InteractionManager>();
            int cancelled = 0;
            manager.BeginMining(null, () => { cancelled++; manager.EndMining(); });
            typeof(InteractionManager).GetMethod("OnDisable", flags).Invoke(manager, null);
            typeof(InteractionManager).GetMethod("OnDisable", flags).Invoke(manager, null);
            if (cancelled != 1) throw new Exception("Cancellation cleanup is not idempotent");
            log.AppendLine("PASS: stop icon switches/restores and disabling interaction cancels exactly once.");
            UnityEngine.Object.DestroyImmediate(prompt);
        }

        private static void Render(Scene scene, Vector3 focus, string path)
        {
            var go = new GameObject("Preview camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(go, scene);
            var lightGo = new GameObject("Preview light", typeof(Light));
            SceneManager.MoveGameObjectToScene(lightGo, scene);
            lightGo.GetComponent<Light>().type = LightType.Directional;
            lightGo.GetComponent<Light>().intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);
            var camera = go.GetComponent<Camera>();
            camera.scene = scene;
            camera.transform.position = focus + new Vector3(-12, 16, -14);
            camera.transform.LookAt(focus);
            camera.orthographic = true;
            camera.orthographicSize = 12;
            var target = new RenderTexture(1000, 800, 24);
            var previous = RenderTexture.active;
            var pixels = new Texture2D(1000, 800, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1000, 800), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
