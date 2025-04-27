#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [CustomPropertyDrawer(typeof(LabelWidthAttribute), true)]
    public class LabelWidthDrawer : GgPropertyDrawer
    {
        #region GgPropertyHeight
        
        protected override float GgPropertyHeight(SerializedProperty property, float propertyHeight, float approxFieldWidth)
        {
            return propertyHeight;
        }
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region OnGgGUI
        
        protected override void OnGgGUI(Rect position, SerializedProperty property, GUIContent label, GgGUIDefaults defaultCache)
        {
            EditorGUIUtility.labelWidth = IsListOrArray(property) ? labelWidth : AttributeAsType<LabelWidthAttribute>().labelWidth;
            GgGUI.CustomPropertyField(position, property, label);
        }
        
        #endregion
        
    } // class end
}

#endif