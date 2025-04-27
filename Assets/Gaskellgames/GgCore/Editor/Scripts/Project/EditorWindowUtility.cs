#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    [InitializeOnLoad]
    public static class EditorWindowUtility
    {
        #region Variables
        
        public static GaskellgamesHubSettings_SO settings;
        public static ShaderAutoUpdater_SO shaderAutoUpdater;
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Constructor

        static EditorWindowUtility()
        {
            Initialisation();
            EditorApplication.update += RunOnceOnStartup;
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Private Functions

        private static void Initialisation()
        {
            settings = EditorExtensions.GetAssetByType<GaskellgamesHubSettings_SO>();
            shaderAutoUpdater = EditorExtensions.GetAssetByType<ShaderAutoUpdater_SO>();
        }
        
        private static void RunOnceOnStartup()
        {
            if (!settings || !shaderAutoUpdater) { Initialisation(); }
            if (SessionState.GetBool("EditorWindowUtilityFirstInit", false)) { return; }

            if (settings && settings.showHubOnStartup == GaskellgamesHubSettings_SO.HubAutoLaunchOptions.Always)
            {
                GaskellgamesHub.OpenWindow_WindowMenu();
            }

            if (shaderAutoUpdater)
            {
                shaderAutoUpdater.UpdateMaterialsForCurrentTargetPipeline();
            }
            
            SessionState.SetBool("EditorWindowUtilityFirstInit", true);
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Public Functions

        public static Texture LoadInspectorBanner(string ggPathRef, string relativeFilepath)
        {
            if (GgPackageRef.TryGetFullFilePath(ggPathRef, relativeFilepath, out string fullFilepath))
            {
                return (Texture)AssetDatabase.LoadAssetAtPath(fullFilepath, typeof(Texture));
            }

            return null;
        }
        
        public static bool TryDrawBanner(Texture banner, bool editorWindow = false, bool forceShow = false)
        {
            // null and condition check
            if (banner == null) { return false; }
            if (settings == null) { return false; }
            if (!forceShow && editorWindow && !settings.showPackageBanners_EditorWindow) { return false; }
            if (!forceShow && !editorWindow && !settings.showPackageBanners_Components) { return false; }
            
            float imageWidth = EditorGUIUtility.currentViewWidth;
            float imageHeight = imageWidth * banner.height / banner.width;
            Rect rect = GUILayoutUtility.GetRect(imageWidth, imageHeight);
            
            // adjust rect to account for offsets in inspectors
            if (!editorWindow)
            {
                float paddingTop = -4;
                float paddingLeft = -18;
                float paddingRight = -4;
                
                // calculate rect size
                float xMin = rect.x + paddingLeft;
                float yMin = rect.y + paddingTop;
                float width = rect.width - (paddingLeft + paddingRight);
                float height = rect.height;

                rect = new Rect(xMin, yMin, width, height);
            }
            
            GUI.DrawTexture(rect, banner, ScaleMode.ScaleToFit);
            return true;
        }

        #endregion
        
    } // class end
}

#endif