#if UNITY_EDITOR
using Gaskellgames.EditorOnly;
using UnityEditor;
using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [CustomPropertyDrawer(typeof(HideInPlayModeAttribute), true)]
    public class HideInPlayModeDrawer : GgPropertyDrawer
    {
        #region GgPropertyHeight
        
        protected override float GgPropertyHeight(SerializedProperty property, float propertyHeight, float approxFieldWidth)
        {
            return Application.isPlaying ? -standardSpacing : propertyHeight;
        }
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region OnGgGUI
        
        protected override void OnGgGUI(Rect position, SerializedProperty property, GUIContent label, GgGUIDefaults defaultCache)
        {
            if (Application.isPlaying) { return; }
            GgGUI.CustomPropertyField(position, property, label);
        }
        
        #endregion

    } // class end
}

#endif