using System;
using System.Collections;
using System.IO;
using System.Reflection;
using MineArena.PlayerSystem;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MineArena.Editor
{
    public static class BowPlaybackValidation
    {
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/bow-playback.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            Validate();
        };

        [MenuItem("MineArena/Validation/Bow Aim Zombie And Spider")]
        public static void ValidateAim()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var fixture = new GameObject("Bow aim fixture");
                SceneManager.MoveGameObjectToScene(fixture, scene);
                fixture.transform.position = new Vector3(10000, 10000, 10000);
                var attack = fixture.AddComponent<PlayerAttack>();
                var resolve = typeof(PlayerAttack).GetMethod("ResolveBowAim", BindingFlags.Instance | BindingFlags.NonPublic);
                foreach (string name in new[] { "Zombie", "Spider" })
                {
                    var mob = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Mobs/" + name + ".prefab"));
                    SceneManager.MoveGameObjectToScene(mob, scene);
                    mob.transform.position = fixture.transform.position + Vector3.forward * 10;
                    Physics.SyncTransforms();
                    var body = mob.GetComponent<Collider>();
                    var center = body.bounds.center;
                    var ray = new Ray(center + Vector3.up * 5, Vector3.down);
                    var volume = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    SceneManager.MoveGameObjectToScene(volume, scene);
                    volume.transform.position = center + Vector3.up * 3;
                    volume.GetComponent<Collider>().isTrigger = true;
                    Physics.SyncTransforms();
                    var result = (Vector3)resolve.Invoke(attack, new object[] { ray, fixture.transform.position, 100f });
                    if (Vector3.Distance(result, center) > 0.001f)
                        throw new Exception(name + ": aim must select body center and ignore unrelated triggers.");
                    volume.GetComponent<Collider>().isTrigger = false;
                    Physics.SyncTransforms();
                    result = (Vector3)resolve.Invoke(attack, new object[] { ray, fixture.transform.position, 100f });
                    if (result.y <= center.y + 3)
                        throw new Exception(name + ": solid scenery must block aim.");
                    UnityEngine.Object.DestroyImmediate(volume);
                    UnityEngine.Object.DestroyImmediate(mob);
                }
                Debug.Log("PASS: bow selects Zombie trigger and Spider body; ignores area triggers; respects solid scenery.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [MenuItem("MineArena/Validation/Bow Playback Restart")]
        public static void Validate()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var controller = new AnimatorController();
            var clip = new AnimationClip();
            try
            {
                clip.SetCurve("", typeof(Transform), "localPosition.x", AnimationCurve.Linear(0, 0, 1.46f, 0));
                controller.AddLayer("Base Layer");
                controller.AddParameter("BowShoot", AnimatorControllerParameterType.Trigger);
                var machine = controller.layers[0].stateMachine;
                var bow = machine.AddState("BowShooting");
                bow.motion = clip;
                machine.defaultState = bow;
                var restart = machine.AddAnyStateTransition(bow);
                restart.AddCondition(AnimatorConditionMode.If, 0, "BowShoot");
                restart.hasExitTime = false;
                restart.duration = 0;
                restart.canTransitionToSelf = true;
                var fixture = new GameObject("Bow restart regression", typeof(Animator));
                SceneManager.MoveGameObjectToScene(fixture, scene);
                var animator = fixture.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var attack = fixture.AddComponent<PlayerAttack>();
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(PlayerAttack).GetField("_rawAnimator", flags).SetValue(attack, animator);
                animator.Play("BowShooting", 0, 1.05f);
                animator.Update(0);
                animator.SetTrigger("BowShoot");
                var wait = (IEnumerator)typeof(PlayerAttack).GetMethod("WaitForAnimationState", flags)
                    .Invoke(attack, new object[] { "BowShooting", 0, 1.5f, true });
                if (!wait.MoveNext()) throw new Exception("Outgoing animation prematurely completed the new shot.");
                animator.Update(0.01f);
                if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1) throw new Exception("Fixture did not restart.");
                if (!wait.MoveNext()) throw new Exception("New playback lost its pending shot at entry.");
                animator.Update(0.9f);
                if (!wait.MoveNext()) throw new Exception("Pending shot cleared before the release keyframe.");
                animator.Update(0.7f);
                if (wait.MoveNext()) throw new Exception("Completed playback did not unlock the next shot.");
                File.WriteAllText("Temp/bow-playback-result.txt", "PASS: outgoing completion ignored; restarted shot survives release time; completion unlocks next shot.");
            }
            catch (Exception error)
            {
                File.WriteAllText("Temp/bow-playback-result.txt", "FAIL: " + error);
                Debug.LogException(error);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                UnityEngine.Object.DestroyImmediate(controller);
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }
    }
}
