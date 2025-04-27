#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    [CustomEditor(typeof(GgPackageRef)), CanEditMultipleObjects]
    public class GgPackageRefEditor : GgEditor
    {
        #region Serialized Properties / Variables

        private GgPackageRef targetAsType;
        
        private SerializedProperty version;
        private SerializedProperty pathRef;
        private SerializedProperty link;
        
        private const string packageRefName = "GgCore";
        private const string bannerRelativePath = "/Editor/Icons/InspectorBanner_GgCore.png";
        private Texture banner;
        
        #endregion

        //----------------------------------------------------------------------------------------------------
        
        #region OnEnable
        
        private void OnEnable()
        {
            if (!targetAsType) { targetAsType = target as GgPackageRef; }
            banner = EditorWindowUtility.LoadInspectorBanner(packageRefName, bannerRelativePath);
            
            version = serializedObject.FindProperty("version");
            pathRef = serializedObject.FindProperty("pathRef");
            link = serializedObject.FindProperty("link");
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region OnInspectorGUI

        public override void OnInspectorGUI()
        {
            // get & update references
            serializedObject.Update();
            
            // draw banner if turned on in Gaskellgames settings
            EditorWindowUtility.TryDrawBanner(banner);
            
            // draw custom inspector:
            EditorGUILayout.PropertyField(version);
            EditorGUILayout.PropertyField(pathRef);
            
            // apply reference changes
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

    } // class end
}

#endif