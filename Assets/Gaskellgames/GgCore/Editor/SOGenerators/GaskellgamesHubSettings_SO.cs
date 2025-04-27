using UnityEngine;

#if UNITY_EDITOR

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    //[CreateAssetMenu(fileName = "GaskellgamesHubSettings", menuName = "Gaskellgames/GaskellgamesHubSettings")]
    public class GaskellgamesHubSettings_SO : GgScriptableObject
    {
        #region Hub Auto Launch
        
        internal enum HubAutoLaunchOptions
        {
            Always,
            Never
        }
        
        [SerializeField, ReadOnly]
        internal HubAutoLaunchOptions showHubOnStartup = HubAutoLaunchOptions.Always;
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Package Banners
        
        internal enum PackageBannerOptions
        {
            Always,
            EditorWindow,
            Components,
            Never
        }
        
        [SerializeField, ReadOnly]
        internal PackageBannerOptions showPackageBanners = PackageBannerOptions.Always;

        internal bool showPackageBanners_EditorWindow => showPackageBanners == PackageBannerOptions.Always || showPackageBanners == PackageBannerOptions.EditorWindow;
        internal bool showPackageBanners_Components => showPackageBanners == PackageBannerOptions.Always || showPackageBanners == PackageBannerOptions.Components;
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Hierarchy Icons
        
        internal enum HierarchyIconOptions
        {
            AllIcons,
            ThreeIcons,
            TwoIcons,
            OneIcon,
            NoIcons,
        }
        
        [SerializeField, ReadOnly]
        internal HierarchyIconOptions showHierarchyIcons = HierarchyIconOptions.ThreeIcons;
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Transform Inspector
        
        internal enum TransformInspectorOptions
        {
            All,
            AlignTools,
            ResetButtons,
            TransformUtilities,
            DefaultUnity
        }

        [SerializeField, ReadOnly]
        internal TransformInspectorOptions transformInspectorOptions = TransformInspectorOptions.TransformUtilities;
        
        internal bool showTransformInspector_All => transformInspectorOptions == TransformInspectorOptions.All;
        internal bool showTransformInspector_AlignTools => transformInspectorOptions == TransformInspectorOptions.All || transformInspectorOptions == TransformInspectorOptions.AlignTools;
        internal bool showTransformInspector_ResetButtons => transformInspectorOptions == TransformInspectorOptions.All || transformInspectorOptions == TransformInspectorOptions.ResetButtons;
        internal bool showTransformInspector_TransformUtilities => transformInspectorOptions == TransformInspectorOptions.All || transformInspectorOptions == TransformInspectorOptions.TransformUtilities;
        internal bool showTransformInspector_DefaultUnity => transformInspectorOptions == TransformInspectorOptions.DefaultUnity;

        #endregion

    } // class end
}
#endif