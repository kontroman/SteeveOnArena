#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [CustomPropertyDrawer(typeof(HiddenAttribute), true)]
    public class HiddenDrawer : GgPropertyDrawer
    {
        #region GgPropertyHeight

        protected override float GgPropertyHeight(SerializedProperty property, float propertyHeight, float approxFieldWidth)
        {
            return -standardSpacing;
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region OnGgGUI

        protected override void OnGgGUI(Rect position, SerializedProperty property, GUIContent label, GgGUIDefaults defaultCache)
        {
            // blank
        }

        #endregion
        
    } // class end
}

#endif