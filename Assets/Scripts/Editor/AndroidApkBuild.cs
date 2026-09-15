using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace MineArena.Editor
{
    public static class AndroidApkBuild
    {
        private const string Request = "Temp/android-apk.request";
        private const string Status = "Builds/Android/build-status.txt";
        [InitializeOnLoadMethod]
        private static void Watch() => EditorApplication.update += Tick;
        private static void Tick()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            try
            {
                Directory.CreateDirectory("Builds/Android");
                if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                {
                    File.WriteAllText(Status, "Switching to Android");
                    if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android)) throw new Exception("Android target switch failed");
                    return;
                }
                File.Delete(Request);
                Build();
            }
            catch (Exception e) { File.Delete(Request); File.WriteAllText(Status, "FAILED\n" + e); Debug.LogException(e); }
        }
        [MenuItem("MineArena/Build/Android APK")]
        private static void RequestBuild() { Directory.CreateDirectory("Temp"); File.WriteAllText(Request, "build"); }
        private static void Build()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0) throw new Exception("No enabled build scenes");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isDirty || !scenes.Contains(scene.path)) continue;
                string backup = "Builds/Android/SceneBackups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "/" + Path.GetFileName(scene.path);
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                File.Copy(scene.path, backup, false);
                if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save " + scene.path);
            }
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.Android.useCustomKeystore = false;
            File.WriteAllText(Status, "Building APK (IL2CPP ARM64 + ARMv7)");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = scenes,
                locationPathName = "Builds/Android/MineArena.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            var summary = report.summary;
            File.WriteAllText(Status, summary.result + "\n" + summary.outputPath + "\nBytes: " + summary.totalSize + "\nDuration: " + summary.totalTime + "\nErrors: " + summary.totalErrors);
            if (summary.result != BuildResult.Succeeded) throw new Exception("APK build " + summary.result + ": " + summary.totalErrors + " errors; see Editor.log");
        }
    }
}
