using Devotion.SDK.Services.SaveSystem;
using UnityEditor;
using UnityEngine;

namespace Devotion.SDK.Editor
{
    internal static class DevotionMenuItems
    {
        private const string RootPath = "Devotion";
        private const string ClearPrefsMenu = RootPath + "/Clear All Prefs";
        private const string PlayWithClearMenu = RootPath + "/Play (clear all prefs)";

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
    }
}
