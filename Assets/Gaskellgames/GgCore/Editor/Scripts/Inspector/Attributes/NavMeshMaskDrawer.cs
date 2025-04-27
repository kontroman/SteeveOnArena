#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames: https://github.com/Gaskellgames
    /// </summary>
    
    [CustomPropertyDrawer(typeof(NavMeshMaskAttribute), true)]
    public class NavMeshMaskDrawer : GgPropertyDrawer
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
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                GgGUI.CustomPropertyField(position, property, label);
                return;
            }
            
#if UNITY_6000_0_OR_NEWER
            string[] navMeshAreaNames = UnityEngine.AI.NavMesh.GetAreaNames();
#else
            string[] navMeshAreaNames = GameObjectUtility.GetNavMeshAreaNames();
#endif

            if (GgGUI.MaskField(position, label, property.intValue, out int outputValue, navMeshAreaNames, property.hasMultipleDifferentValues))
            {
                property.intValue = outputValue;
            }
        }

        #endregion
        
    } // class end 
}
#endif