#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    [InitializeOnLoad]
    public static class HierarchyUtility
    {
        #region Variables
        
        private static GaskellgamesHubSettings_SO settings;
        private static SerializedDictionary<Type, string> hierarchyIcons;
        private static SerializedDictionary<int, List<Type>> hierarchyObjectCache;
        
        public static event Action onCacheHierarchyIcons;
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Constructor

        static HierarchyUtility()
        {
            GgGUI.onCacheGgGUIIcons -= Initialisation;
            GgGUI.onCacheGgGUIIcons += Initialisation;
        }
        
        private static void Initialisation()
        {
            // initialise references
            settings = EditorExtensions.GetAssetByType<GaskellgamesHubSettings_SO>();
            hierarchyIcons ??= new SerializedDictionary<Type, string>();
            onCacheHierarchyIcons?.Invoke();
            
            // handle scene loads
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            
            // handle scene updates
            GgEditorCallbacks.OnSceneUpdated -= OnSceneUpdated;
            GgEditorCallbacks.OnSceneUpdated += OnSceneUpdated;
            
            // handle component updates
            GgEditorCallbacks.OnGameObjectStructureUpdated -= OnGameObjectStructureUpdated;
            GgEditorCallbacks.OnGameObjectStructureUpdated += OnGameObjectStructureUpdated;
            
            // handle drawing icons
            EditorApplication.hierarchyWindowItemOnGUI -= DrawHierarchyIcons;
            EditorApplication.hierarchyWindowItemOnGUI += DrawHierarchyIcons;
            
            // initialise cache
            hierarchyObjectCache = new SerializedDictionary<int, List<Type>>();
            CacheAllOpenScenes();
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Callbacks
        
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            // initialise dictionary if required
            hierarchyObjectCache ??= new SerializedDictionary<int, List<Type>>();
            
            // if single scene opened, clear current cache
            if (mode == OpenSceneMode.Single)
            {
                hierarchyObjectCache.Clear();
            }

            CacheSceneObjects(scene);
        }

        private static void OnSceneUpdated(GgEventArgs_SceneData ggEventArgsSceneData)
        {
            // initialise dictionary if required
            hierarchyObjectCache ??= new SerializedDictionary<int, List<Type>>();

            CacheSceneObjects(ggEventArgsSceneData.sceneData.Scene);
        }

        private static void OnGameObjectStructureUpdated(GgEventArgs_GameObject ggEventArgsGameObject)
        {
            // initialise dictionary if required
            hierarchyObjectCache ??= new SerializedDictionary<int, List<Type>>();
            
            CacheAllComponents(ggEventArgsGameObject.gameObject);
        }
        
        private static void DrawHierarchyIcons(int instanceID, Rect position)
        {
            if (settings == null) { return; }
            if (hierarchyIcons == null) { return; }
            if (hierarchyObjectCache == null) { return; }
            
            // draw if exists
            if (!hierarchyObjectCache.TryGetValue(instanceID, out List<Type> types)) { return; }
            int maxIcons = 0;
            switch (settings.showHierarchyIcons)
            {
                default:
                case GaskellgamesHubSettings_SO.HierarchyIconOptions.AllIcons:
                    maxIcons = types.Count;
                    break;
                
                case GaskellgamesHubSettings_SO.HierarchyIconOptions.ThreeIcons:
                    maxIcons = Mathf.Min(types.Count, 3);
                    break;
                
                case GaskellgamesHubSettings_SO.HierarchyIconOptions.TwoIcons:
                    maxIcons = Mathf.Min(types.Count, 2);
                    break;
                
                case GaskellgamesHubSettings_SO.HierarchyIconOptions.OneIcon:
                    maxIcons = Mathf.Min(types.Count, 1);
                    break;
                
                case GaskellgamesHubSettings_SO.HierarchyIconOptions.NoIcons:
                    return;
            }
            for (int i = 0; i < maxIcons; i++)
            {
                if (!hierarchyIcons.TryGetValue(types[i], out string outValue)) { continue; }
                DrawHierarchyIcon(position, GgGUI.GetIcon(outValue), i);
            }
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Private Functions

        private static void CacheAllOpenScenes()
        {
            // force initialise/clear dictionary
            hierarchyObjectCache = new SerializedDictionary<int, List<Type>>();
            hierarchyObjectCache.Clear();
            
            // cache all gameObjects in all open scenes
            List<Scene> scenes = SceneExtensions.GetAllOpenScenes();
            foreach (Scene scene in scenes)
            {
                CacheSceneObjects(scene);
            }
        }
        
        /// <summary>
        /// Cache all gameObjects in scene
        /// </summary>
        /// <param name="scene"></param>
        private static void CacheSceneObjects(Scene scene)
        {
            GameObject[] rootObjects = scene.GetRootGameObjects();
            foreach (GameObject gameObject in rootObjects)
            {
                CacheAllChildComponentsRecursive(gameObject.transform);
            }
        }

        private static void CacheAllChildComponentsRecursive(Transform transform)
        {
            CacheAllComponents(transform.gameObject);
            foreach (Transform childTransform in transform)
            {
                CacheAllChildComponentsRecursive(childTransform);
            }
        }

        private static void CacheAllComponents(GameObject gameObject)
        {
            if (hierarchyIcons == null) { return; }
            int instanceID = gameObject.GetInstanceID();
            Component[] components = gameObject.GetComponents(typeof(Component));
            List<Type> types = new List<Type>();
            foreach (Component component in components)
            {
                // cache only components for which there is an icon to draw
                if (component == null) { continue; }
                Type componentType = component.GetType();
                if (!hierarchyIcons.TryGetValue(componentType, out string outValue)) { continue; }
                types.TryAdd(componentType);
            }
            hierarchyObjectCache.Remove(instanceID);
            hierarchyObjectCache.TryAdd(instanceID, types);
        }

        private static void DrawHierarchyIcon(Rect position, Texture icon, int indent = 0)
        {
            // check for valid draw
            if (Event.current.type != EventType.Repaint) { return; }

            // draw icon
            if (icon == null) { return; }
            float pixels = 16;
            float offset = pixels + (pixels * indent);
            EditorGUIUtility.SetIconSize(new Vector2(pixels, pixels));
            Rect iconPosition = new Rect(position.xMax - offset, position.yMin, position.width, position.height);
            GUIContent iconGUIContent = new GUIContent(icon);
            EditorGUI.LabelField(iconPosition, iconGUIContent);
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Public Functions
        
        /// <summary>
        /// Try to add custom icons to the HierarchyIcon_GgCore hierarchyIcons list. For best results, subscribe to
        /// the HierarchyIcon_GgCore.<see cref="onCacheHierarchyIcons"/> action using a script that implements <see cref="InitializeOnLoadAttribute"/>.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="name"></param>
        /// <param name="icon"></param>
        /// <returns></returns>
        public static bool TryAddHierarchyIcon(Type type, string name, Texture icon)
        {
            if (icon == null) { return false; }
            hierarchyIcons ??= new SerializedDictionary<Type, string>();
            if (!hierarchyIcons.TryAdd(type, name)) { return false; }
            if (GgGUI.TryAddCustomIcon(name, icon)) { return true; }
            
            hierarchyIcons.Remove(type);
            return false;
        }

        #endregion
        
    } // class end
}

#endif