#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    public class MenuItemUtility
    {
        #region Menu Items
        
        // Extensions
        
        public const string GameObjectMenu_Path = "GameObject";
        public const int GameObjectMenu_Priority = 0;
        
        // Gaskellgames
        private const string GgToolsMenu = "Tools/Gaskellgames";
        private const string GgWindowMenu = "Window/Gaskellgames";
        private const string GgGameObjectMenu = "GameObject/Gaskellgames";
        public const int GgGameObjectMenu_Priority = 10;
        
        // Gaskellgames Hub
        private const string Hub = "/Gaskellgames Hub";
        
        public const int Hub_Priority = 100;
        public const string Hub_ToolsMenu_Path = GgToolsMenu + Hub;
        public const string Hub_WindowMenu_Path = GgWindowMenu + Hub;
        
        // GgCore
        public const int GgCoreSamples_Priority = 101;
        public const string GgCoreSamples_ToolsMenu_Path = GgToolsMenu + "/GgCore Samples";
        public const string GgCoreSamples_WindowMenu_Path = GgWindowMenu + "/GgCore Samples";
        private const string GgCore_GameObjectMenu_Path = GgGameObjectMenu + "/GgCore";
        
        // Audio Controller
        public const int AudioController_Priority = 125;
        public const string AudioController_ToolsMenu_Path = GgToolsMenu + "/Audio Manager";
        public const string AudioController_WindowMenu_Path = GgWindowMenu + "/Audio Manager";
        public const string AudioController_GameObjectMenu_Path = GgGameObjectMenu + "/Audio Controller";
        
        // Camera System
        public const string CameraSystem_GameObjectMenu_Path = GgGameObjectMenu + "/Camera System";
        
        // Character Controller
        public const string CharacterController_GameObjectMenu_Path = GgGameObjectMenu + "/Character Controller";
        
        // Folder System
        public const int FolderSystem_Priority = 126;
        public const string FolderSystem_ToolsMenu_Path = GgToolsMenu + "/Folder Manager";
        public const string FolderSystem_WindowMenu_Path = GgWindowMenu + "/Folder Manager";
        public const string FolderSystem_GameObjectMenu_Path = GameObjectMenu_Path;
        
        // Icon Finder
        public const int IconFinder_Priority = 127;
        public const string IconFinder_ToolsMenu_Path = GgToolsMenu + "/Icon Finder";
        public const string IconFinder_WindowMenu_Path = GgWindowMenu + "/Icon Finder";
        
        // Input Event System
        public const string InputEventSystem_GameObjectMenu_Path = GgGameObjectMenu + "/Input Event System";
        
        // Logic System
        public const string LogicSystem_GameObjectMenu_Path = GgGameObjectMenu + "/Logic System";
        
        // Mask Map Generator
        public const int MaskMapGenerator_Priority = 128;
        public const string MaskMapGenerator_ToolsMenu_Path = GgToolsMenu + "/Mask Map Generator";
        public const string MaskMapGenerator_WindowMenu_Path = GgWindowMenu + "/Mask Map Generator";
        
        // Platform Controller
        public const string PlatformController_GameObjectMenu_Path = GgGameObjectMenu + "/Platform Controller";
        
        // Pooling System
        public const string PoolingSystem_GameObjectMenu_Path = GgGameObjectMenu + "/Pooling System";
        
        // Spline System
        public const string SplineSystem_GameObjectMenu_Path = GgGameObjectMenu + "/Spline System";
        
        // Scene Controller
        public const int SceneController_Priority = 129;
        public const string SceneController_ToolsMenu_Path = GgToolsMenu + "/Scene Manager";
        public const string SceneController_WindowMenu_Path = GgWindowMenu + "/Scene Manager";
        public const string SceneController_GameObjectMenu_Path = GgGameObjectMenu + "/Scene Controller";
        
        // TODO - delete when all updated!
        public const int AudioController_GameObjectMenu_Priority = GgGameObjectMenu_Priority;
        public const int AudioController_WindowMenu_Priority = AudioController_Priority;
        public const int AudioController_ToolsMenu_Priority = AudioController_Priority;
        public const int CameraSystem_GameObjectMenu_Priority = GgGameObjectMenu_Priority;
        public const int CharacterController_GameObjectMenu_Priority = GgGameObjectMenu_Priority;
        public const int FolderSystem_GameObjectMenu_Priority = GameObjectMenu_Priority;
        public const int FolderSystem_ToolsMenu_Priority = FolderSystem_Priority;
        public const int FolderSystem_WindowMenu_Priority = FolderSystem_Priority;
        public const int InputEventSystem_GameObjectMenu_Priority = GgGameObjectMenu_Priority;
        public const int LogicSystem_GameObjectMenu_Priority = GgGameObjectMenu_Priority;
        public const int PlatformController_GameObjectMenu_Priority = GgGameObjectMenu_Priority;
        public const int PoolingSystem_GameObjectMenu_Priority = GgGameObjectMenu_Priority;
        public const int SplineSystem_GameObjectMenu_Priority = GgGameObjectMenu_Priority;
        public const int SceneController_GameObjectMenu_Priority = GgGameObjectMenu_Priority;
        public const int SceneController_ToolsMenu_Priority = SceneController_Priority;
        public const int SceneController_WindowMenu_Priority = SceneController_Priority;
        // TODO - delete when all updated!
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Helper Functions

        public static GameObject SetupMenuItemInContext(MenuCommand menuCommand, string gameObjectName)
        {
            // create a custom gameObject, register in the undo system, parent and set position relative to context
            GameObject go = new GameObject(gameObjectName);
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            GameObject context = (GameObject)menuCommand.context;
            if (context != null) { go.transform.SetParent(context.transform); }
            EditorExtensions.GetSceneViewLookAt(out Vector3 point);
            go.transform.position = point;

            VerboseLogs.Log($"'{gameObjectName}' created.");
            
            return go;
        }

        public static GameObject AddChildItemInContext(Transform parent, string gameObjectName)
        {
            GameObject go = new GameObject(gameObjectName);
            go.transform.parent = parent;
            go.transform.localPosition = Vector3.zero;
            go.transform.localEulerAngles = Vector3.zero;
            
            return go;
        }
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Gameobject Menu
        
        [MenuItem(GgCore_GameObjectMenu_Path + "/Comment", false, GgGameObjectMenu_Priority)]
        private static void Gaskellgames_GameObjectMenu_Comment(MenuCommand menuCommand)
        {
            // create a custom gameObject, register in the undo system, parent and set position relative to context
            GameObject go = SetupMenuItemInContext(menuCommand, "Comment");
            
            // add scripts & components
            go.AddComponent<Comment>();
            
            // select newly created gameObject
            Selection.activeObject = go;
        }

        #endregion
        
    } // class end
}
#endif