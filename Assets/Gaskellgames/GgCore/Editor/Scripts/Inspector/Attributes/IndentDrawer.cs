#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [CustomPropertyDrawer(typeof(IndentAttribute), true)]
    public class IndentDrawer : GgPropertyDrawer
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
            if (AttributeAsType<IndentAttribute>().specificIndentLevel)
            {
                int indentDelta = AttributeAsType<IndentAttribute>().indentLevel;
                EditorGUI.indentLevel += indentDelta;
                GgGUI.CustomPropertyField(position, property, label);
                EditorGUI.indentLevel -= indentDelta;
            }
            else
            {
                EditorGUI.indentLevel++;
                GgGUI.CustomPropertyField(position, property, label);
                EditorGUI.indentLevel--;
            }
        }
        
        #endregion

    } // class end
}
#endif