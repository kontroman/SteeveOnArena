using Devotion.SDK.Services.SaveSystem;
using MineArena.AI;
using MineArena.Drop;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace Devotion.SDK.Editor
{
    internal static class DevotionMenuItems
    {
        private const string RootPath = "Devotion";
        private const string ClearPrefsMenu = RootPath + "/Clear All Prefs";
        private const string PlayWithClearMenu = RootPath + "/Play (clear all prefs)";
        private const string SetupMobMenu = RootPath + "/Setup Mob";

        [MenuItem(PlayWithClearMenu, priority = 1)]
        private static void PlayWithClearedPrefs()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Devotion] Stop play mode before using Play (clear all prefs).");
                return;
            }

            ClearPreferences();
            EditorApplication.EnterPlaymode();
        }

        [MenuItem(ClearPrefsMenu, priority = 2)]
        private static void ClearPrefs()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Devotion] Stop play mode before clearing preferences.");
                return;
            }

            ClearPreferences();
        }

        private static void ClearPreferences()
        {
            PlayerPrefs.DeleteAll();
            SaveService.ClearAllSavedData();
            PlayerPrefs.Save();

            Debug.Log("[Devotion] PlayerPrefs and save-system data cleared.");
        }

        [MenuItem(SetupMobMenu, priority = 50)]
        private static void SetupMob()
        {
            var selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                EditorUtility.DisplayDialog("[Devotion] Setup Mob",
                    "Select at least one GameObject in the scene or prefab stage.", "Ok");
                return;
            }

            int processed = 0;
            foreach (var go in selection)
            {
                if (go == null)
                    continue;

                bool hasValidScene = go.scene.IsValid();
                bool inPrefabStage = PrefabStageUtility.GetPrefabStage(go) != null;

                if (!hasValidScene && !inPrefabStage)
                {
                    Debug.LogWarning($"[Devotion] {go.name} is not part of the active scene or prefab stage, skipped.");
                    continue;
                }

                SetupMobOnGameObject(go);
                processed++;
            }

            if (processed > 0)
            {
                Debug.Log($"[Devotion] Setup Mob applied to {processed} object(s).");
            }
        }

        [MenuItem(SetupMobMenu, validate = true)]
        private static bool ValidateSetupMob()
        {
            var selection = Selection.gameObjects;
            return selection != null && selection.Length > 0;
        }

        private static void SetupMobOnGameObject(GameObject go)
        {
            var animator = GetOrAddComponent<Animator>(go);
            var navMeshAgent = GetOrAddComponent<NavMeshAgent>(go);
            var capsuleCollider = GetOrAddComponent<CapsuleCollider>(go);
            var mob = GetOrAddComponent<Mob>(go);
            var mobMovement = GetOrAddComponent<MobMovement>(go);
            var mobCombat = GetOrAddComponent<MobCombat>(go);
            var mobHealth = GetOrAddComponent<MobHealth>(go);
            var mobAnimation = GetOrAddComponent<MobAnimationController>(go);
            GetOrAddComponent<Dropable>(go);

            ConfigureNavMeshAgent(navMeshAgent);
            ConfigureCapsuleCollider(capsuleCollider);

            AssignMobReferences(mob, mobCombat, mobMovement, mobHealth, mobAnimation);
            AssignAnimatorReference(mobAnimation, animator);
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            if (existing != null)
                return existing;

            return Undo.AddComponent<T>(go);
        }

        private static void ConfigureNavMeshAgent(NavMeshAgent agent)
        {
            if (agent == null) return;

            Undo.RecordObject(agent, "Configure NavMeshAgent");
            agent.stoppingDistance = Mathf.Max(agent.stoppingDistance, 1f);
            agent.updateUpAxis = true;
            agent.updateRotation = false;
        }

        private static void ConfigureCapsuleCollider(CapsuleCollider collider)
        {
            if (collider == null) return;

            Undo.RecordObject(collider, "Configure CapsuleCollider");
            if (collider.height < 2f) collider.height = 2f;
            if (collider.radius < 0.5f) collider.radius = 0.5f;
            collider.center = Vector3.up;
            collider.isTrigger = false;
        }

        private static void AssignMobReferences(
            Mob mob,
            MobCombat mobCombat,
            MobMovement mobMovement,
            MobHealth mobHealth,
            MobAnimationController mobAnimation)
        {
            if (mob == null) return;

            Undo.RecordObject(mob, "Assign Mob References");
            var serializedMob = new SerializedObject(mob);
            serializedMob.Update();
            SetObjectReference(serializedMob, "_mobCombat", mobCombat);
            SetObjectReference(serializedMob, "_mobMovement", mobMovement);
            SetObjectReference(serializedMob, "_mobHealth", mobHealth);
            SetObjectReference(serializedMob, "_mobAnimation", mobAnimation);
            serializedMob.ApplyModifiedProperties();
        }

        private static void AssignAnimatorReference(MobAnimationController mobAnimation, Animator animator)
        {
            if (mobAnimation == null) return;

            Undo.RecordObject(mobAnimation, "Assign Mob Animator");
            var serializedAnimation = new SerializedObject(mobAnimation);
            serializedAnimation.Update();
            SetObjectReference(serializedAnimation, "_animator", animator);
            serializedAnimation.ApplyModifiedProperties();
        }

        private static void SetObjectReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogWarning($"[Devotion] Could not find property '{propertyName}' on {serializedObject.targetObject.name}.");
                return;
            }

            property.objectReferenceValue = value;
        }
    }
}
