#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    public class GaskellgamesHub : GgEditorWindow
    {
        #region Variables
        
        private static readonly int windowWidth = 725;
        private static readonly int windowHeight = 530;
        private static readonly int logoSize = 75;

        // package paths - downloadable
        private const string packageRefName_GgCore = "GgCore";
        private const string relativePath_GgCore = "/Editor/Icons/";
        
        private const string packageRefName_AudioSystem = "AudioSystem";
        private const string packageRefName_CameraSystem = "CameraSystem";
        private const string packageRefName_CharacterController = "CharacterController";
        private const string packageRefName_FolderSystem = "FolderSystem";
        private const string packageRefName_InputEventSystem = "InputEventSystem";
        private const string packageRefName_LogicSystem = "LogicSystem";
        private const string packageRefName_PlatformController = "PlatformController";
        private const string packageRefName_PoolingSystem = "PoolingSystem";
        private const string packageRefName_SceneController = "SceneController";
        private const string packageRefName_SplineSystem = "SplineSystem";
        
        private const string relativePath = "/Editor/Icons/";
        
        // package icons - helper
        private Texture icon_PlatformController;
        private Texture icon_CameraSystem;
        private Texture icon_CharacterController;
        private Texture icon_AudioSystem;
        private Texture icon_SceneController;
        private Texture icon_FolderSystem;
        private Texture icon_SplineSystem;
        
        // package icons - helper
        private Texture icon_InputEventSystem;
        private Texture icon_PoolingSystem;
        private Texture icon_LogicSystem;
        
        // package icons - powered by
        private Texture icon_GgCore;
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Menu Item
        
        [MenuItem(MenuItemUtility.Hub_ToolsMenu_Path, false, MenuItemUtility.Hub_Priority)]
        private static void OpenWindow_ToolsMenu()
        {
            OpenWindow_WindowMenu();
        }

        [MenuItem(MenuItemUtility.Hub_WindowMenu_Path, false, MenuItemUtility.Hub_Priority)]
        public static void OpenWindow_WindowMenu()
        {
            OpenWindow<GaskellgamesHub>("Gaskellgames Hub", windowWidth, windowHeight, true);
        }
        
        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Overriding Functions

        protected override void OnInitialise()
        {
            InitialiseSettings();
        }

        protected override void OnFocusChange(bool hasFocus)
        {
            if (!hasFocus) { return; }
            InitialiseSettings();
        }

        protected override List<ToolbarItem> LeftToolbar()
        {
            string copyright = isWindowWide ? "Copyright \u00a9 2022-2025 Gaskellgames. All rights reserved." : "\u00a9 Gaskellgames.";
            List<ToolbarItem> leftToolbar = new List<ToolbarItem>
            {
                new (null, new GUIContent(copyright)),
            };

            return leftToolbar;
        }

        protected override List<ToolbarItem> RightToolbar()
        {
            bool hasVersion = GgPackageRef.TryGetVersion(packageRefName_GgCore, out version);
            string versionAsString = isWindowWide
                ? hasVersion ? version.GetVersionLong() : "Version ?.?.?"
                : hasVersion ? version.GetVersionShort() : "v?.?.?";
            List<ToolbarItem> leftToolbar = new List<ToolbarItem>
            {
                new (null, new GUIContent(versionAsString)),
            };

            return leftToolbar;
        }

        protected override GenericMenu OptionsToolbar()
        {
            GenericMenu toolsMenu = new GenericMenu();
            toolsMenu.AddItem(new GUIContent("Gaskellgames Unity Page"), false, OnSupport_AssetStoreLink);
            toolsMenu.AddItem(new GUIContent("Gaskellgames Discord"), false, OnSupport_DiscordLink);
            toolsMenu.AddItem(new GUIContent("Gaskellgames Website"), false, OnSupport_WebsiteLink);
            return toolsMenu;
        }
        
        protected override void OnPageGUI()
        {
            // draw window content
            EditorWindowUtility.TryDrawBanner(banner, true, true);
            DrawWelcomeMessage();
            EditorExtensions.DrawInspectorLine(InspectorExtensions.backgroundSeperatorColor, 4, 0);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Settings:", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            HandleShowAtStartUp();
            HandleShowPackageBanners();
            HandleShowHierarchyIcons();
            HandleTransformExtension();
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Powered By:", EditorStyles.boldLabel, GUILayout.Width(logoSize + 10));
            DrawPackageLogo(icon_GgCore, "Gg Core");
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            
            EditorExtensions.DrawInspectorLine(InspectorExtensions.backgroundSeperatorColor, 4, 0);
            EditorGUILayout.LabelField("Downloaded Packages:", EditorStyles.boldLabel);
            HandleDownloadedPackages();
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Private Functions
        
        private void InitialiseSettings()
        {
            banner = EditorWindowUtility.LoadInspectorBanner(packageRefName_GgCore, relativePath_GgCore + "InspectorBanner_SettingsHub.png");
            LoadPackageIcons();
        }

        private void LoadPackageIcons()
        {
            string filePath = string.Empty;
            
            if (GgPackageRef.TryGetFullFilePath(packageRefName_GgCore, relativePath_GgCore, out filePath))
                icon_GgCore = (Texture)AssetDatabase.LoadAssetAtPath(filePath + "Logo_GgCore.png", typeof(Texture));
            
            if (GgPackageRef.TryGetFullFilePath(packageRefName_AudioSystem, relativePath, out filePath))
                icon_AudioSystem = (Texture)AssetDatabase.LoadAssetAtPath(filePath + "Logo_AudioSystem.png", typeof(Texture));
            
            if (GgPackageRef.TryGetFullFilePath(packageRefName_CameraSystem, relativePath, out filePath))
                icon_CameraSystem = (Texture)AssetDatabase.LoadAssetAtPath(filePath + "Logo_CameraSystem.png", typeof(Texture));
            
            if (GgPackageRef.TryGetFullFilePath(packageRefName_CharacterController, relativePath, out filePath))
                icon_CharacterController = (Texture)AssetDatabase.LoadAssetAtPath(filePath + "Logo_CharacterController.png", typeof(Texture));
            
            if (GgPackageRef.TryGetFullFilePath(packageRefName_FolderSystem, relativePath, out filePath))
                icon_FolderSystem = (Texture)AssetDatabase.LoadAssetAtPath(filePath + "Logo_HierarchyFolderSystem.png", typeof(Texture));
            
            if (GgPackageRef.TryGetFullFilePath(packageRefName_InputEventSystem, relativePath, out filePath))
                icon_InputEventSystem = (Texture)AssetDatabase.LoadAssetAtPath(filePath + "Logo_InputEventSystem.png", typeof(Texture));
            
            if (GgPackageRef.TryGetFullFilePath(packageRefName_LogicSystem, relativePath, out filePath))
                icon_LogicSystem = (Texture)AssetDatabase.LoadAssetAtPath(filePath + "Logo_LogicSystem.png", typeof(Texture));
            
            if (GgPackageRef.TryGetFullFilePath(packageRefName_PlatformController, relativePath, out filePath))
                icon_PlatformController = (Texture)AssetDatabase.LoadAssetAtPath(filePath + "Logo_PlatformController.png", typeof(Texture));
            
            if (GgPackageRef.TryGetFullFilePath(packageRefName_PoolingSystem, relativePath, out filePath))
                icon_PoolingSystem = (Texture)AssetDatabase.LoadAssetAtPath(filePath + "Logo_PoolingSystem.png", typeof(Texture));
            
            if (GgPackageRef.TryGetFullFilePath(packageRefName_SceneController, relativePath, out filePath))
                icon_SceneController = (Texture)AssetDatabase.LoadAssetAtPath(filePath + "Logo_SceneController.png", typeof(Texture));
            
            if (GgPackageRef.TryGetFullFilePath(packageRefName_SplineSystem, relativePath, out filePath))
                icon_SplineSystem = (Texture)AssetDatabase.LoadAssetAtPath(filePath + "Logo_SplineSystem.png", typeof(Texture));
        
        }

        private void DrawWelcomeMessage()
        {
            GUI.enabled = false;
            float defaultHeight = EditorStyles.textField.fixedHeight;
            EditorStyles.textField.fixedHeight = 100;
            EditorGUILayout.TextArea("Thank you for installing a Gaskellgames asset, and welcome to the settings hub!\n\n" +
                                     "Any settings options you choose here will be applied to all relevant Gaskellgames packages.\n\n" +
                                     "Links to the Unity Asset Store page, Gaskellgames Discord and Gaskellgames Website are available via the 'options' dropdown\n" +
                                     "menu above. (Note: Please read through each packages documentation pdf before contacting Gaskellgames with any queries.)");
            EditorStyles.textField.fixedHeight = defaultHeight;
            GUI.enabled = true;
        }

        private void HandleShowAtStartUp()
        {
            GUIContent label = new GUIContent("Show Hub On Startup", "Option to show/hide this window when loading into a project.");
            GaskellgamesHubSettings_SO.HubAutoLaunchOptions selected = (GaskellgamesHubSettings_SO.HubAutoLaunchOptions)EditorGUILayout.EnumPopup(label, EditorWindowUtility.settings.showHubOnStartup, GUILayout.Width(Screen.width * 0.4f));
            if (EditorWindowUtility.settings.showHubOnStartup == selected) { return; }
            EditorWindowUtility.settings.showHubOnStartup = selected;
            EditorUtility.SetDirty(EditorWindowUtility.settings);
        }
        
        private void HandleShowPackageBanners()
        {
            GUIContent label = new GUIContent("Show Package Banners", "Show or hide the Gaskellgames package header for component scripts.");
            GaskellgamesHubSettings_SO.PackageBannerOptions selected = (GaskellgamesHubSettings_SO.PackageBannerOptions)EditorGUILayout.EnumPopup(label, EditorWindowUtility.settings.showPackageBanners, GUILayout.Width(Screen.width * 0.4f));
            if (EditorWindowUtility.settings.showPackageBanners == selected) { return; }
            EditorWindowUtility.settings.showPackageBanners = selected;
            EditorUtility.SetDirty(EditorWindowUtility.settings);
        }
        
        private void HandleShowHierarchyIcons()
        {
            GUIContent label = new GUIContent("Show Hierarchy Icons", "Show or hide the hierarchy icons for Gaskellgames components.");
            GaskellgamesHubSettings_SO.HierarchyIconOptions selected = (GaskellgamesHubSettings_SO.HierarchyIconOptions)EditorGUILayout.EnumPopup(label, EditorWindowUtility.settings.showHierarchyIcons, GUILayout.Width(Screen.width * 0.4f));
            if (EditorWindowUtility.settings.showHierarchyIcons == selected) { return; }
            EditorWindowUtility.settings.showHierarchyIcons = selected;
            EditorUtility.SetDirty(EditorWindowUtility.settings);
        }
        
        private void HandleTransformExtension()
        {
            GUIContent label = new GUIContent("Transform Inspector", "Enable or Disable the Gaskellgames transform utilities extension.");
            GaskellgamesHubSettings_SO.TransformInspectorOptions selected = (GaskellgamesHubSettings_SO.TransformInspectorOptions)EditorGUILayout.EnumPopup(label, EditorWindowUtility.settings.transformInspectorOptions, GUILayout.Width(Screen.width * 0.4f));
            if (EditorWindowUtility.settings.transformInspectorOptions == selected) { return; }
            EditorWindowUtility.settings.transformInspectorOptions = selected;
            EditorUtility.SetDirty(EditorWindowUtility.settings);
        }
        
        private void HandleDownloadedPackages()
        {
            EditorGUILayout.BeginHorizontal();
            
            float xMin = spacing;
            xMin = DrawPackageLogo(icon_AudioSystem, "Audio Controller", true, xMin);
            xMin = DrawPackageLogo(icon_CameraSystem, "Camera System", true, xMin);
            xMin = DrawPackageLogo(icon_CharacterController, "Character Controller", true, xMin);
            xMin = DrawPackageLogo(icon_FolderSystem, "Folder System", true, xMin);
            xMin = DrawPackageLogo(icon_InputEventSystem, "Input Event System", true, xMin);
            xMin = DrawPackageLogo(icon_LogicSystem, "Logic System", true, xMin);
            xMin = DrawPackageLogo(icon_PlatformController, "Platform Controller", true, xMin);
            xMin = DrawPackageLogo(icon_PoolingSystem, "Pooling System", true, xMin);
            xMin = DrawPackageLogo(icon_SceneController, "Scene Controller", true, xMin);
            xMin = DrawPackageLogo(icon_SplineSystem, "Spline System", true, xMin);
            
            EditorGUILayout.EndHorizontal();
        }

        private float DrawPackageLogo(Texture packageLogo, string toolTip, bool autoWrap = false, float xMin = 0)
        {
            if (!packageLogo) { return xMin; }
            
            // handle auto wrap
            if (autoWrap && pageWidth < (xMin + logoSize + spacing))
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                xMin = spacing;
            }
            
            // draw package logo
            GUIContent label = new GUIContent(packageLogo, toolTip);
            GUILayout.Box(label, GUILayout.Width(logoSize), GUILayout.Height(logoSize));
            
            return xMin + logoSize + spacing;
        }

        #endregion
        
    } // class end
}

#endif