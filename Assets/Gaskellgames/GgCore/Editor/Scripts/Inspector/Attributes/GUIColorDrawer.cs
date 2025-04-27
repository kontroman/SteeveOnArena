#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [CustomPropertyDrawer(typeof(GUIColorAttribute), true)]
    public class GUIColorDrawer : GgPropertyDrawer
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
            GUIColorAttribute attributeAsType = AttributeAsType<GUIColorAttribute>();
            Color32 GUIColor = new Color32(attributeAsType.R, attributeAsType.G, attributeAsType.B, attributeAsType.A);
            
            // draw property with altered GUI color
            if (attributeAsType.target == GUIColorAttribute.Target.Background)
            {
                GUI.backgroundColor = GUIColor;
                GgGUI.CustomPropertyField(position, property, label);
            }
            else if (attributeAsType.target == GUIColorAttribute.Target.Content)
            {
                GUI.contentColor = GUIColor;
                GgGUI.CustomPropertyField(position, property, label);
            }
            else
            {
                GUI.color = GUIColor;
                GgGUI.CustomPropertyField(position, property, label);
            }
        }
        
        #endregion

    } // class end
}

#endif