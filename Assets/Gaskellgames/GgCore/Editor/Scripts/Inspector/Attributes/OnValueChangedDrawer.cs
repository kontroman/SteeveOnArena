#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [CustomPropertyDrawer(typeof(OnValueChangedAttribute), true)]
    public class OnValueChangedDrawer : GgPropertyDrawer
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
            EditorGUI.BeginChangeCheck();
            GgGUI.CustomPropertyField(position, property, label);
            if (EditorGUI.EndChangeCheck())
            {
                OnValueChangedAttribute attributeAsType = AttributeAsType<OnValueChangedAttribute>();
                Object[] targets = property.serializedObject.targetObjects;
                foreach (Object target in targets)
                {
                    Type type = target.GetType();
                    MethodInfo method = type.GetMethod(attributeAsType.methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (method == null)
                    {
                        VerboseLogs.Log($"Unable to find method '{attributeAsType.methodName}' on type '{type.Name}'.", LogType.Error);
                    }
                    else if (0 < method.GetParameters().Length)
                    {
                        VerboseLogs.Log($"Method '{attributeAsType.methodName}' on type '{type.Name}' cannot be called as it requires at least one parameter.", LogType.Error);
                    }
                    else
                    {
                        InvokeMethodNextFrame(method, target, null);
                    }
                }
            }
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Private Functions

        private async void InvokeMethodNextFrame(MethodInfo method, object target, object[] parameters)
        {
            await TaskExtensions.WaitUntilNextFrame();
            method.Invoke(target, parameters);
        }

        #endregion

    } // class end
}

#endif