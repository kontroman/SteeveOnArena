using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MineArena.AI;
using MineArena.Commands;
using MineArena.Interfaces;
using MineArena.ObjectPools;
using MineArena.Structs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Health = MineArena.Game.Health.Health;
using Object = UnityEngine.Object;

namespace MineArena.Editor
{
    public static class MobCombatValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly List<string> Report = new List<string>();
        private static readonly Vector3 Origin = new Vector3(10000, 1000, 10000);
        private static Scene _scene;

        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            if (File.Exists("Temp/projectile-preview.request") && !EditorApplication.isCompiling &&
                !EditorApplication.isUpdating && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                File.Delete("Temp/projectile-preview.request");
                RenderProjectilePreview();
            }
            const string request = "Temp/mob-combat.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            Validate();
        };

        [MenuItem("MineArena/Validation/Mob Combat And Projectiles")]
        public static void Validate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Report.Clear();
            var previousScene = SceneManager.GetActiveScene();
            _scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                ValidateMobPrefabs();
                ValidateProjectiles();
                ValidateExplosion();
                RenderProjectiles();
                Report.Add("ALL CHECKS PASSED (Editor physics and real combat methods; not a gameplay playtest).");
            }
            catch (Exception e)
            {
                Report.Add("FAIL " + e);
                Debug.LogException(e);
            }
            finally
            {
                EditorSceneManager.CloseScene(_scene, true);
                SceneManager.SetActiveScene(previousScene);
                File.WriteAllLines("Documentation/mob-combat-validation.txt", Report);
            }
        }

        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
            Report.Add("PASS " + message);
        }

        private static void Set(object target, string name, object value)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, Private);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }
            throw new MissingFieldException(name);
        }

        private static object Get(object target, string name) => target.GetType().GetField(name, Private).GetValue(target);
        private static void Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);

        private static GameObject Root(string name, Vector3 offset)
        {
            var root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, _scene);
            root.transform.position = Origin + offset;
            return root;
        }

        private static CombatValidationHealth Target(string name, Vector3 offset, bool trigger = false)
        {
            var root = Root(name, offset);
            var collider = root.AddComponent<BoxCollider>();
            collider.isTrigger = trigger;
            var health = root.AddComponent<CombatValidationHealth>();
            Set(health, "_maxHealth", 100f);
            health.SetCurrentValue(100f, false);
            return health;
        }

        private static void ValidateMobPrefabs()
        {
            var pool = AssetDatabase.LoadAssetAtPath<ObjectPoolPreset>("Assets/Scripts/ObjectPool/Pools/MobsPoolsPeresets.asset");
            Check(pool.Preset.Count == Enum.GetValues(typeof(MobTypes)).Length, "Pool covers all ten mob types");
            var types = new HashSet<MobTypes>();
            foreach (var prefab in pool.Preset)
            {
                var mob = prefab.GetComponent<Mob>();
                var preset = (MobPreset)typeof(Mob).GetField("_preset", Private).GetValue(mob);
                Check(preset != null && types.Add(preset.MobType), prefab.name + ": unique preset");
                Check(prefab.GetComponentsInChildren<Mob>(true).Length == 1 &&
                    prefab.GetComponentsInChildren<MobHealth>(true).Length == 1 &&
                    prefab.GetComponentsInChildren<MobCombat>(true).Length == 1 &&
                    prefab.GetComponentsInChildren<NavMeshAgent>(true).Length == 1, prefab.name + ": one health/combat/agent owner");
                Check(preset.MaxHealth > 0 && preset.Damage > 0 && preset.AttackDelay >= 1 && preset.Speed > 0,
                    prefab.name + ": valid balance values");
                Check(prefab.GetComponentsInChildren<Collider>(true).Any(c => c.enabled && c.GetComponentInParent<MobHealth>(true) != null &&
                    c.GetComponentInParent<MobHealth>(true).gameObject.layer == 8), prefab.name + ": reachable by player weapon layer mask");
                if (preset.IsRangeAttack || preset.AttackType == MobAttackType.Range)
                {
                    var fire = (Transform)Get(prefab.GetComponent<MobCombat>(), "_firePoint");
                    Check(fire != null && fire.IsChildOf(prefab.transform) && preset.Projectile != null &&
                        preset.Projectile.GetComponent<Projectile>() != null, prefab.name + ": projectile and fire point wired");
                    Check(preset.Projectile.GetComponentsInChildren<Renderer>(true).All(r => r.sharedMaterials.All(m => m != null && m.shader != null)),
                        prefab.name + ": projectile materials and shaders resolve");
                    ValidateRanged(preset);
                }
                var root = Root("Health test " + prefab.name, Vector3.up * 50);
                root.SetActive(false);
                var health = root.AddComponent<MobHealth>();
                health.SetParameters(preset);
                float maximum = health.MaxValue;
                health.TakeDamage(new DamageData(22f, health));
                Check(Mathf.Approximately(health.CurrentValue, maximum - 22f), prefab.name + ": receives exactly one wooden-sword hit");
                health.SetCurrentValue(0, false);
                health.TakeDamage(new DamageData(22f, health));
                Check(health.CurrentValue == 0, prefab.name + ": dead health ignores further damage");
                health.SetParameters(preset);
                Check(health.CurrentValue == maximum, prefab.name + ": respawn restores preset health");
                var agent = root.AddComponent<NavMeshAgent>();
                agent.enabled = false;
                var movement = root.AddComponent<MobMovement>();
                movement.SetParameters(preset);
                Check(agent.speed == preset.Speed && agent.stoppingDistance == preset.AttackRange,
                    prefab.name + ": movement preset works before Start");
                Object.DestroyImmediate(root);
                if (!preset.IsRangeAttack && preset.AttackType != MobAttackType.Explosion) ValidateMelee(preset);
                Report.Add($"BALANCE {preset.MobType}: HP={preset.MaxHealth}, damage={preset.Damage}, interval={preset.AttackDelay:0.0}s, nominal DPS={preset.Damage / preset.AttackDelay:0.00}, wood hits={Mathf.CeilToInt(preset.MaxHealth / 22)}");
            }
        }

        private static void ValidateMelee(MobPreset preset)
        {
            var target = Target("Melee target", new Vector3(0, 0, 1));
            var root = Root("Melee attacker", Vector3.zero);
            root.AddComponent<BoxCollider>();
            root.AddComponent<MobMovement>();
            var combat = root.AddComponent<MobCombat>();
            combat.SetParameters(preset);
            Set(combat, "_playerTransform", target.transform);
            Set(combat, "_damageData", new DamageData(preset.Damage, target));
            Set(combat, "_isAttack", true);
            Physics.SyncTransforms();
            Call(combat, "ApplyAttackHit");
            Call(combat, "ApplyAttackHit");
            Check(Mathf.Approximately(target.CurrentValue, 100f - preset.Damage), preset.MobType + ": melee hits once per cycle");
            Set(combat, "_attackHitApplied", false);
            target.transform.position += Vector3.forward * 20;
            Physics.SyncTransforms();
            Call(combat, "ApplyAttackHit");
            Check(Mathf.Approximately(target.CurrentValue, 100f - preset.Damage), preset.MobType + ": no out-of-range hit");
            target.transform.position -= Vector3.forward * 20;
            var wall = Root("Melee wall", new Vector3(0, 0, 0.5f));
            wall.AddComponent<BoxCollider>().size = new Vector3(5, 5, 0.05f);
            Physics.SyncTransforms();
            Call(combat, "ApplyAttackHit");
            Check(Mathf.Approximately(target.CurrentValue, 100f - preset.Damage), preset.MobType + ": wall blocks melee");
            Object.DestroyImmediate(wall);
            combat.HandleDeath();
            Call(combat, "ApplyAttackHit");
            Check(Mathf.Approximately(target.CurrentValue, 100f - preset.Damage), preset.MobType + ": dead mob cannot attack");
            Object.DestroyImmediate((Object)Get(combat, "_attackCommand"));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target.gameObject);
        }

        private static CombatValidationProjectile Shot(string asset, Transform target, Transform owner, float damage)
        {
            var root = Root("Projectile test", new Vector3(0, 0, 0));
            var shot = root.AddComponent<CombatValidationProjectile>();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Objects/Projectile/" + asset + ".prefab").GetComponent<Projectile>();
            foreach (var field in new[] { "enemySpeed", "gravity", "splashRadius", "sweepRadius", "speed" })
                Set(shot, field, typeof(Projectile).GetField(field, Private).GetValue(prefab));
            Physics.SyncTransforms();
            shot.SetParameters(target, new DamageData(damage, target.GetComponent<IDamageable>()), owner);
            return shot;
        }

        private static void ValidateRanged(MobPreset preset)
        {
            var target = Target("Ranged target", Vector3.forward * 8);
            var root = Root("Ranged attacker", Vector3.zero);
            root.AddComponent<MobMovement>();
            var combat = root.AddComponent<MobCombat>();
            combat.SetParameters(preset);
            Set(combat, "_playerTransform", target.transform);
            Set(combat, "_damageData", new DamageData(preset.Damage, target));
            Set(combat, "_firePoint", root.transform);
            Set(combat, "_isAttack", true);
            var before = new HashSet<Projectile>(Object.FindObjectsOfType<Projectile>());
            Physics.SyncTransforms();
            Call(combat, "ApplyAttackHit");
            Call(combat, "ApplyAttackHit");
            var shots = Object.FindObjectsOfType<Projectile>().Where(p => !before.Contains(p)).ToArray();
            Check(shots.Length == 1 && shots[0].GetType() == preset.Projectile.GetComponent<Projectile>().GetType(),
                preset.MobType + ": actual ranged command launches exactly one configured projectile");
            Check(target.CurrentValue == 100, preset.MobType + ": launching causes no instant damage");
            Check((Transform)typeof(Projectile).GetField("_owner", Private).GetValue(shots[0]) == root.transform,
                preset.MobType + ": launch passes shooter for collision exclusion");
            foreach (var shot in shots) Object.DestroyImmediate(shot.gameObject);
            Object.DestroyImmediate((Object)Get(combat, "_attackCommand"));
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(target.gameObject);
        }

        private static void Advance(Projectile shot, float seconds)
        {
            Physics.SyncTransforms();
            typeof(Projectile).GetMethod("Step", Private).Invoke(shot, new object[] { seconds });
        }

        private static void ValidateProjectiles()
        {
            var owner = Root("Shooter", Vector3.zero);
            owner.AddComponent<BoxCollider>();
            var target = Target("Projectile target", new Vector3(0, 0, 8), true);
            var child = new GameObject("Second hitbox", typeof(BoxCollider));
            child.transform.SetParent(target.transform, false);
            child.GetComponent<BoxCollider>().isTrigger = true;
            var arrow = Shot("Arrow", target.transform, owner.transform, 9);
            Advance(arrow, 1f);
            Check(target.CurrentValue == 91, "Arrow: owner ignored, trigger/child hitboxes hit exactly once even across a one-second frame");
            Object.DestroyImmediate(arrow.gameObject);
            target.RestoreFullHealth();
            var wall = Root("Thin wall", new Vector3(0, 0, 4));
            wall.AddComponent<BoxCollider>().size = new Vector3(5, 10, 0.02f);
            arrow = Shot("Arrow", target.transform, owner.transform, 9);
            Advance(arrow, 1f);
            Check(target.CurrentValue == 100 && arrow.transform.position.z < wall.transform.position.z, "Arrow: thin wall blocks damage without tunnelling");
            Object.DestroyImmediate(arrow.gameObject);
            var potion = Shot("Potion", target.transform, owner.transform, 13);
            Advance(potion, 2f);
            Check(target.CurrentValue == 100 && !potion.gameObject.activeSelf, "Potion: shatters on wall without damaging through it");
            Object.DestroyImmediate(potion.gameObject);
            Object.DestroyImmediate(wall);
            potion = Shot("Potion", target.transform, owner.transform, 13);
            Advance(potion, 0.4f);
            Check(potion.transform.position.y > Origin.y + 1, "Potion: visible ballistic arc");
            Advance(potion, 1.6f);
            Check(target.CurrentValue == 87, "Potion: lands at aim point and applies one hit");
            Object.DestroyImmediate(potion.gameObject);
            target.RestoreFullHealth();
            arrow = Shot("Arrow", target.transform, owner.transform, 9);
            target.transform.position += Vector3.right * 3;
            Advance(arrow, 1f);
            Check(target.CurrentValue == 100, "Arrow: target can dodge, projectile does not home");
            Object.DestroyImmediate(arrow.gameObject);
            target.transform.position = Origin + Vector3.forward * 8;
            potion = Shot("Potion", target.transform, owner.transform, 13);
            target.transform.position += Vector3.right * 0.9f;
            var ground = Root("Potion ground", new Vector3(0, -0.6f, 6));
            ground.AddComponent<BoxCollider>().size = new Vector3(10, 0.2f, 15);
            Advance(potion, 2f);
            Check(target.CurrentValue == 87, $"Potion: nearby ground impact splashes player once (HP={target.CurrentValue}, impact={potion.transform.position - Origin})");
            Object.DestroyImmediate(potion.gameObject);
            Object.DestroyImmediate(ground);
            target.RestoreFullHealth();
            target.TakeDamage(new DamageData(-20, target));
            target.TakeDamage(new DamageData(float.NaN, target));
            Check(target.CurrentValue == 100, "Invalid damage cannot heal or corrupt health");
            Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(owner);
        }

        private static void ValidateExplosion()
        {
            var owner = Target("Creeper", Vector3.zero);
            var target = Target("Explosion target", Vector3.forward);
            var extra = new GameObject("Extra hitbox", typeof(BoxCollider));
            extra.transform.SetParent(target.transform, false);
            var command = ScriptableObject.CreateInstance<ExplosionAttackCommand>();
            Physics.SyncTransforms();
            command.Execute(new ExplosionAttackData(owner.transform.position, 28, 2.5f, ~0, owner.gameObject));
            Check(target.CurrentValue == 72 && owner.Deaths == 1, "Creeper: one damage application per target and one self-death");
            target.RestoreFullHealth();
            owner.RestoreFullHealth();
            var wall = Root("Explosion wall", new Vector3(0, 0, 0.5f));
            wall.AddComponent<BoxCollider>().size = new Vector3(5, 5, 0.05f);
            Physics.SyncTransforms();
            command.Execute(new ExplosionAttackData(owner.transform.position, 28, 2.5f, ~0, owner.gameObject));
            Check(target.CurrentValue == 100, "Creeper: solid cover blocks blast");
            Object.DestroyImmediate(command);
            Object.DestroyImmediate(owner.gameObject);
            Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(wall);
        }

        private static void RenderProjectiles()
        {
            var arrow = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Objects/Projectile/Arrow.prefab"));
            var potion = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Objects/Projectile/Potion.prefab"));
            SceneManager.MoveGameObjectToScene(arrow, _scene);
            SceneManager.MoveGameObjectToScene(potion, _scene);
            arrow.transform.SetPositionAndRotation(Origin + Vector3.left * 0.65f, Quaternion.Euler(0, 65, 0));
            potion.transform.SetPositionAndRotation(Origin + Vector3.right * 0.6f, Quaternion.identity);
            var cameraRoot = Root("Projectile preview", new Vector3(0, 1, -4));
            var camera = cameraRoot.AddComponent<Camera>();
            camera.transform.LookAt(Origin);
            camera.orthographic = true;
            camera.orthographicSize = 0.6f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.19f, 0.19f);
            var lightRoot = Root("Projectile preview light", Vector3.up * 3);
            var light = lightRoot.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.9f;
            lightRoot.transform.rotation = Quaternion.Euler(40, -35, 0);
            var texture = new RenderTexture(1200, 480, 24);
            var result = new Texture2D(1200, 480, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                camera.Render();
                RenderTexture.active = texture;
                result.ReadPixels(new Rect(0, 0, 1200, 480), 0, 0);
                result.Apply();
                File.WriteAllBytes("Documentation/UI/EnemyProjectiles.png", result.EncodeToPNG());
                Report.Add("PASS Rendered both actual projectile prefabs to Documentation/UI/EnemyProjectiles.png");
            }
            finally
            {
                camera.targetTexture = null;
                RenderTexture.active = previous;
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(result);
                Object.DestroyImmediate(arrow);
                Object.DestroyImmediate(potion);
                Object.DestroyImmediate(cameraRoot);
                Object.DestroyImmediate(lightRoot);
            }
        }

        [MenuItem("MineArena/Combat/Preview Projectiles")]
        public static void RenderProjectilePreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var previousScene = SceneManager.GetActiveScene();
            _scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try { RenderProjectiles(); }
            finally
            {
                EditorSceneManager.CloseScene(_scene, true);
                SceneManager.SetActiveScene(previousScene);
            }
        }
    }

    public sealed class CombatValidationHealth : Health
    {
        public int Deaths;
        protected override void Die() => Deaths++;
    }

    public sealed class CombatValidationProjectile : Projectile
    {
        protected override void Release() => gameObject.SetActive(false);
    }
}
