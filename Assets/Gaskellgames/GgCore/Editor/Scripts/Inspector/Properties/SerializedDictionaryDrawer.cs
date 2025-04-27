#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    [CustomPropertyDrawer(typeof(SerializedDictionary<,>))]
    public class SerializedDictionaryDrawer : PropertyDrawer
    {
        #region variables

        private SerializedProperty serializedDictionary;
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Property Height
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label) - (GgGUI.singleLineHeight + GgGUI.standardSpacing);
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region OnGUI

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // open property and get reference to instance
            EditorGUI.BeginProperty(position, label, property);

            // get reference to SerializeFields
            serializedDictionary ??= property.FindPropertyRelative("serializedDictionary");
            
            // draw property
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedDictionary, label);
            if (EditorGUI.EndChangeCheck())
            {
                InitialiseNextFrame(property);
            }
            
            // close property
            EditorGUI.EndProperty();
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Private Functions

        private async void InitialiseNextFrame(SerializedProperty property)
        {
            await TaskExtensions.WaitUntilNextFrame();

            if (property == null) { return; }
            SerializedDictionaryBase target = property.GetValue<SerializedDictionaryBase>();
            target?.Initialise();
        }

        #endregion

    } // class end
}
#endif
