#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [CustomPropertyDrawer(typeof(LabelTextAttribute), true)]
    public class LabelTextDrawer : GgPropertyDrawer
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
            label.text = IsListOrArray(property) ? label.text : AttributeAsType<LabelTextAttribute>().label;
            GgGUI.CustomPropertyField(position, property, label);
        }
        
        #endregion
        
    } // class end
}

#endif