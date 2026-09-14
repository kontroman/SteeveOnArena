using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using MineArena.Items;
using MineArena.PlayerSystem;
using MineArena.Structs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Health = MineArena.Game.Health.Health;

namespace MineArena.Editor
{
    public sealed class ShieldValidationProjectile : Projectile
    {
        public bool Released;
        protected override void Release() { Released = true; }
    }

    public static class ShieldValidation
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static void Set(object target, string field, object value, Type type = null) => (type ?? target.GetType()).GetField(field, Flags).SetValue(target, value);
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/shield-validation.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            try { Validate(); }
            catch (Exception e) { File.WriteAllText("Temp/shield-validation-result.txt", e.ToString()); Debug.LogException(e); }
        };
        private static void ValidateDurability(GameObject target, PlayerEquipment equipment, PlayerShield shield,
            Health health, ArmorConfig config, Action<bool, string> check)
        {
            var instance = typeof(Devotion.SDK.Controllers.GameRoot).GetField("<Instance>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);
            if (instance.GetValue(null) != null) throw new Exception("Run durability validation outside Play Mode with no active GameRoot");
            var fixture = new GameObject("Durability save fixture");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fixture, target.scene);
            try
            {
                var root = fixture.AddComponent<Devotion.SDK.Controllers.GameRoot>();
                var playerProgress = new Devotion.SDK.Services.SaveSystem.Progress.PlayerProgress("shield-validation");
                Set(root, "playerProgress", playerProgress);
                Set(root, "gameConfig", AssetDatabase.LoadAssetAtPath<MineArena.Structs.GameConfig>("Assets/ScriptableObjects/GameConfig.asset"));
                var manager = fixture.AddComponent<MineArena.Managers.InventoryManager>();
                var audio = fixture.AddComponent<MineArena.Managers.AudioManager>();
                Set(root, "_managers", new Dictionary<Type, Devotion.SDK.Managers.BaseManager>
                {
                    [typeof(MineArena.Managers.InventoryManager)] = manager,
                    [typeof(MineArena.Managers.AudioManager)] = audio
                });
                instance.SetValue(null, root);
                var progress = playerProgress.InventoryProgress;
                progress.AddResource(config.Name);
                progress.SetEquippedArmorItemId("OffHand", config.Name);
                progress.SetQuickSlotItemId(1, config.Name);
                manager.InitManager();
                check(progress.GetItemDurability(config.Name, config.MaxDurability) == 336, "Existing/new shields start at full durability");
                Set(equipment, "_shield", config); Set(shield, "_blockHeld", true); Set(shield, "_raise", 1f);
                health.SetCurrentValue(100, false);
                health.TakeDamage(new DamageData(20, health, Vector3.forward));
                check(health.CurrentValue == 100 && progress.GetItemDurability(config.Name, 336) == 315, "Successful block costs 1 + damage durability exactly once");
                health.TakeDamage(new DamageData(10, health, Vector3.back));
                check(progress.GetItemDurability(config.Name, 336) == 315, "Unblocked rear damage does not wear shield");
                equipment.UnequipArmor(ArmorSlot.OffHand); equipment.EquipArmor(config);
                check(progress.GetItemDurability(config.Name, 336) == 315, "Unequip/re-equip does not repair shield");
                var restored = JsonUtility.FromJson<Devotion.SDK.Services.SaveSystem.Progress.InventoryProgress>(JsonUtility.ToJson(progress));
                check(restored.GetItemDurability(config.Name, 336) == 315, "Durability survives save round trip");
                var slot = new GameObject("Durability slot", typeof(RectTransform));
                slot.transform.SetParent(fixture.transform);
                ((RectTransform)slot.transform).sizeDelta = new Vector2(82, 82);
                MineArena.UI.ItemDurabilityBar.Bind(slot, config);
                var bar = slot.GetComponentInChildren<MineArena.UI.ItemDurabilityBar>();
                var populate = typeof(MineArena.UI.ItemDurabilityBar).GetMethod("OnPopulateMesh", Flags, null, new[] { typeof(UnityEngine.UI.VertexHelper) }, null);
                var refresh = typeof(MineArena.UI.ItemDurabilityBar).GetMethod("Refresh", Flags);
                using var vertices = new UnityEngine.UI.VertexHelper();
                populate.Invoke(bar, new object[] { vertices });
                var vertex = new UnityEngine.UIVertex();
                vertices.PopulateUIVertex(ref vertex, 4);
                check(vertices.currentVertCount == 8 && vertex.color.g > vertex.color.r && !bar.raycastTarget, "Durability bar draws green fill and black backing without intercepting input");
                progress.DamageDurableItem(config.Name, 314, 336);
                refresh.Invoke(bar, null); populate.Invoke(bar, new object[] { vertices });
                vertices.PopulateUIVertex(ref vertex, 4);
                check(vertex.color.r > vertex.color.g, "Worn shield bar becomes red");
                Set(shield, "_blockHeld", true); Set(shield, "_raise", 1f);
                health.SetCurrentValue(100, false);
                health.TakeDamage(new DamageData(20, health, Vector3.forward));
                check(health.CurrentValue == 100, "Breaking shield still blocks the final hit");
                refresh.Invoke(bar, null); populate.Invoke(bar, new object[] { vertices });
                check(vertices.currentVertCount == 0, "Broken shield has no durability bar");
                MineArena.UI.ItemDurabilityBar.Bind(slot, (ArmorConfig)null);
                check(!bar.gameObject.activeSelf, "Clearing inventory slot hides durability bar");
                check(equipment.Shield == null && !shield.IsBlocking, "Break immediately removes equipment and block state");
                check(!progress.SavedResources.ContainsKey(config.Name) && !progress.InventoryItemOrder.Contains(config.Name)
                    && !manager.Items.Any(item => item != null && item.Name == config.Name), "Broken shield removed from saved and live inventory");
                check(string.IsNullOrEmpty(progress.GetEquippedArmorItemId("OffHand")) && string.IsNullOrEmpty(progress.GetQuickSlotItemId(1)), "Break clears saved equipment and quick slots");
                health.TakeDamage(new DamageData(20, health, Vector3.forward));
                check(health.CurrentValue == 80, "Next hit after break deals damage");
                progress.AddResource(config.Name, 2);
                progress.SetEquippedArmorItemId("OffHand", config.Name);
                progress.DamageDurableItem(config.Name, 336, 336);
                check(progress.SavedResources[config.Name] == 1 && progress.GetItemDurability(config.Name, 336) == 336
                    && string.IsNullOrEmpty(progress.GetEquippedArmorItemId("OffHand")), "Reserve shield remains pristine and is not auto-equipped");
                check(Resources.Load<AudioClip>("Audio/ShieldBlock") != null && Resources.Load<AudioClip>("Audio/ShieldBreak") != null, "Both shield sounds available in player resources");
            }
            finally
            {
                instance.SetValue(null, null);
                UnityEngine.Object.DestroyImmediate(fixture);
            }
        }

        [MenuItem("MineArena/Validation/Shield")]
        public static void Validate()
        {
            var report = new List<string>();
            void Check(bool condition, string name) { if (!condition) throw new Exception(name); report.Add("PASS " + name); }
            Check(PlayerShield.TryResolveBlockDirection(new Ray(new Vector3(3, 10, 0), Vector3.down), Vector3.zero, out var aim) && Vector3.Dot(aim, Vector3.right) > .999f, "Shield aims toward mouse ground position");
            Check(PlayerShield.TryResolveBlockDirection(new Ray(new Vector3(0, 10, -3), Vector3.down), Vector3.zero, out aim) && Vector3.Dot(aim, Vector3.back) > .999f, "Moving mouse updates shield aim behind character");
            Check(!PlayerShield.TryResolveBlockDirection(new Ray(Vector3.up, Vector3.forward), Vector3.zero, out aim), "Parallel mouse ray keeps current facing");
            Check(!PlayerShield.TryResolveBlockDirection(new Ray(Vector3.up, Vector3.down), Vector3.zero, out aim), "Mouse at character position keeps current facing");
            var scene = EditorSceneManager.NewPreviewScene();
            var go = new GameObject("Shield target");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
            try
            {
                var rotation = go.AddComponent<RotationController>();
                rotation.FaceDirection(Vector3.right, 2);
                Check(Vector3.Dot(go.transform.forward, Vector3.right) > .999f, "Block facing is applied immediately");
                rotation.FaceDirection(Vector3.forward, 2);
                var equipment = go.AddComponent<PlayerEquipment>();
                var health = go.AddComponent<Health>();
                var shield = go.AddComponent<PlayerShield>();
                var collider = go.AddComponent<BoxCollider>();
                var config = AssetDatabase.LoadAssetAtPath<ArmorConfig>("Assets/Art/Shield/ShieldItem.asset");
                Set(equipment, "_shield", config); Set(shield, "_equipment", equipment);
                Set(shield, "_blockHeld", true); Set(shield, "_raise", 1f);
                Set(health, "_maxHealth", 100f); health.SetCurrentValue(100, false);
                Check(shield.IsBlocking, "Raised equipped shield can block");
                health.TakeDamage(new DamageData(20, health, Vector3.forward * 2));
                Check(health.CurrentValue == 100, "Front melee damage blocked through Health.TakeDamage");
                health.TakeDamage(new DamageData(20, health, Vector3.back * 2));
                Check(health.CurrentValue == 80, "Rear hit deals damage");
                health.TakeDamage(new DamageData(10, health));
                Check(health.CurrentValue == 70, "Damage without direction is not silently blocked");
                Set(shield, "_raise", 0f);
                health.TakeDamage(new DamageData(10, health, Vector3.forward));
                Check(health.CurrentValue == 60, "Lowered shield does not protect");
                Set(shield, "_raise", 1f); Set(equipment, "_shield", null);
                health.TakeDamage(new DamageData(10, health, Vector3.forward));
                Check(health.CurrentValue == 50, "Unequipped shield does not protect");
                Set(equipment, "_shield", config); Set(equipment, "_lastActiveHandItem", HandItemType.Bow);
                Check(!shield.IsBlocking, "Bow disables off-hand block");
                Set(equipment, "_lastActiveHandItem", HandItemType.Sword);
                foreach (bool splash in new[] { false, true })
                foreach (bool front in new[] { true, false })
                {
                    health.SetCurrentValue(100, false);
                    var projectileObject = new GameObject("Projectile fixture");
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(projectileObject, scene);
                    var projectile = projectileObject.AddComponent<ShieldValidationProjectile>();
                    try
                    {
                        projectile.transform.position = (front ? Vector3.forward : Vector3.back) * .5f;
                        Set(projectile, "_damageData", new DamageData(20, health), typeof(Projectile));
                        Set(projectile, "_target", go.transform, typeof(Projectile));
                        Set(projectile, "_velocity", front ? Vector3.back : Vector3.forward, typeof(Projectile));
                        Set(projectile, "_enemyShot", true, typeof(Projectile));
                        if (splash) typeof(Projectile).GetMethod("Splash", Flags).Invoke(projectile, new object[] { collider });
                        else typeof(Projectile).GetMethod("OnHit", Flags, null, new[] { typeof(MineArena.Interfaces.IDamageable), typeof(Collider) }, null).Invoke(projectile, new object[] { health, collider });
                        Check(health.CurrentValue == (front ? 100 : 80), (splash ? "Potion splash" : "Arrow") + (front ? " blocked in front" : " damages from rear"));
                        Check(projectile.Released, "Projectile released after impact");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(projectileObject); }
                }
                ValidateDurability(go, equipment, shield, health, config, Check);
                Check(config.CraftCosts.Count == 2 && config.CraftCosts[0].Amount == 6 && config.CraftCosts[1].Amount == 1, "Recipe costs 6 planks and 1 ingot");
                Check(config.Slot == ArmorSlot.OffHand && config.Resist == 0, "Shield occupies off hand without passive armor");
            }
            finally { UnityEngine.Object.DestroyImmediate(go); EditorSceneManager.ClosePreviewScene(scene); }
            var character = PrefabUtility.LoadPrefabContents("Assets/Scenes/PlayerV2.0.prefab");
            try
            {
                var controller = character.GetComponent<PlayerShield>() ?? character.AddComponent<PlayerShield>();
                typeof(PlayerShield).GetMethod("Awake", Flags).Invoke(controller, null);
                var hand = (Transform)typeof(PlayerShield).GetField("_hand", Flags).GetValue(controller);
                var arm = (Transform)typeof(PlayerShield).GetField("_arm", Flags).GetValue(controller);
                var forearm = (Transform)typeof(PlayerShield).GetField("_forearm", Flags).GetValue(controller);
                var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Shield/Shield.prefab"), hand);
                Set(controller, "_visual", model.transform);
                var animator = character.GetComponent<Animator>();
                var clips = animator.runtimeAnimatorController.animationClips.Distinct().ToArray();
                int poses = 0;
                foreach (var clip in clips)
                for (int frame = 0; frame < 12; frame++)
                foreach (float raise in new[] { 0f, .5f, 1f })
                {
                    clip.SampleAnimation(character, clip.length * frame / 12f);
                    arm.localRotation *= Quaternion.Euler(-65f * raise, 0, -15f * raise);
                    Set(controller, "_raise", raise);
                    Set(controller, "_impact", 1f);
                    typeof(PlayerShield).GetMethod("UpdateVisualPose", Flags).Invoke(controller, null);
                    foreach (var bone in new[] { arm, forearm, hand })
                        if (Vector3.Dot(model.transform.position - bone.position, model.transform.forward) < .199f)
                            throw new Exception("Arm intersects shield in " + clip.name);
                    poses++;
                }
                Check(poses > 0, "Arm clearance across " + poses + " sampled animation/block/recoil poses");
            }
            finally { PrefabUtility.UnloadPrefabContents(character); }
            var inventory = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/UI/InventoryWindow.prefab");
            var inventoryRects = inventory.GetComponentsInChildren<RectTransform>(true);
            string[] slotNames = { "EquipHelmet", "EquipChest", "EquipLeggins", "EquipBoots", "EquipOffHand" };
            for (int i = 0; i < slotNames.Length; i++)
            {
                var slot = inventoryRects.Single(t => t.name == slotNames[i]);
                Check(slot.sizeDelta == new Vector2(82, 82) && slot.anchoredPosition == new Vector2(38, -181 - 98 * i), "Aligned equipment slot " + slot.name);
                if (i == 4) Check(slot.GetComponentsInChildren<TMPro.TMP_Text>(true).Length == 0, "Shield slot has no text label");
            }
            var preview = new PreviewRenderUtility();
            try
            {
                var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Shield/Shield.prefab"));
                preview.AddSingleGO(model);
                preview.camera.transform.position = new Vector3(.9f, .35f, 2f);
                preview.camera.transform.LookAt(Vector3.zero);
                preview.camera.nearClipPlane = .01f; preview.camera.farClipPlane = 10;
                preview.camera.fieldOfView = 38;
                preview.lights[0].intensity = 1.4f; preview.lights[0].transform.rotation = Quaternion.Euler(35, -140, 0);
                preview.lights[1].intensity = .7f;
                preview.BeginStaticPreview(new Rect(0, 0, 512, 512));
                preview.Render(true);
                var picture = preview.EndStaticPreview();
                File.WriteAllBytes("Temp/shield-preview.png", picture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(picture);
                preview.camera.transform.position = new Vector3(-.9f, .35f, -2f);
                preview.lights[0].transform.rotation = Quaternion.Euler(35, 40, 0);
                preview.camera.transform.LookAt(Vector3.zero);
                preview.BeginStaticPreview(new Rect(0, 0, 512, 512));
                preview.Render(true);
                var rear = preview.EndStaticPreview();
                File.WriteAllBytes("Temp/shield-preview-back.png", rear.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(rear);
            }
            finally { preview.Cleanup(); }
            File.WriteAllLines("Temp/shield-validation-result.txt", report);
        }
    }
}
