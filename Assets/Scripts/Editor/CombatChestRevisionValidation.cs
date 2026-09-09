using System;
using System.IO;
using System.Reflection;
using MineArena.PlayerSystem;
using MineArena.UI;
using MineArena.UI.FortuneWheel;
using MineArena.Windows.InfoPopup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MineArena.Editor
{
    public static class CombatChestRevisionValidation
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += () =>
        {
            const string request = "Temp/combat-chest.request";
            if (!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(request);
            Validate();
        };

        [MenuItem("MineArena/Validation/Aim Desert And Chest Icons")]
        private static void Validate()
        {
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                var origin = new Vector3(10000, 1000, 10000);
                var player = new GameObject("Aim validation").AddComponent<PlayerAttack>();
                player.transform.position = origin;
                var ray = new Ray(origin + new Vector3(0, 10, -10), new Vector3(0, -1, 1));
                var resolve = typeof(PlayerAttack).GetMethod("ResolveAttackAim", Private);
                var expected = (Vector3)resolve.Invoke(player, new object[] { ray, origin, 500f });
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.transform.position = origin + new Vector3(0, 5, -5);
                wall.transform.localScale = new Vector3(8, 8, 1);
                Physics.SyncTransforms();
                if (!Physics.Raycast(ray, out _, 500f)) throw new Exception("Test wall not hit");
                var actual = (Vector3)resolve.Invoke(player, new object[] { ray, origin, 500f });
                if (Vector3.Distance(expected, actual) > 0.001f) throw new Exception("Building redirected aim");
                if (Mathf.Abs(actual.y - origin.y) > 0.001f) throw new Exception("Aim is not on horizontal plane");

                var config = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/ScriptableObjects/Levels/Level2_Sand.asset"));
                if (Quaternion.Angle(config.FindProperty("levelPrefabRotation").quaternionValue, Quaternion.identity) > 0.01f) throw new Exception("Desert still rotated vertically");

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/DevotionSDK/Prefabs/UI/InfoPopupWindow.prefab");
                var window = UnityEngine.Object.Instantiate(prefab).GetComponent<InfoPopupWindow>();
                var block = AssetDatabase.LoadAssetAtPath<MineArena.Items.ItemConfig>("Assets/ScriptableObjects/Configs/Drops/Stone.asset");
                var flat = AssetDatabase.LoadAssetAtPath<MineArena.Items.ItemConfig>("Assets/ScriptableObjects/Configs/Equipment/Swords/IronSwordItem.asset");
                var prize = new ItemPrize();
                var itemField = typeof(ItemPrize).GetField("_itemConfig", Private);
                itemField.SetValue(prize, block);
                window.Setup(prize);
                var icon = window.GetComponentInChildren<ResourceIcon>(true);
                var image = (Image)typeof(InfoPopupWindow).GetField("_iconImage", Private).GetValue(window);
                if (icon == null || !icon.gameObject.activeSelf || image.enabled) throw new Exception("Block reward is not using ResourceIcon");
                int faces = 0;
                foreach (var face in icon.GetComponentsInChildren<Image>()) if (face.enabled && face.sprite != null) faces++;
                if (faces != 3) throw new Exception("Expected three resource faces, got " + faces);
                itemField.SetValue(prize, flat);
                window.Setup(prize);
                if (icon.gameObject.activeSelf || !image.enabled || image.sprite != flat.Icon) throw new Exception("Flat reward failed after block reward");
                File.WriteAllText("Documentation/combat-chest-validation.txt", "PASS: real physics wall hit does not redirect attack aim; aim remains horizontal.\nPASS: desert level rotation is identity, matching authored prefab.\nPASS: chest resource reward has three textured faces; switching to equipment restores flat icon.\nEditor validation, not a gameplay playtest.\n");
            }
            catch (Exception e) { File.WriteAllText("Documentation/combat-chest-validation.txt", e.ToString()); Debug.LogException(e); }
            finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
        }
    }
}
