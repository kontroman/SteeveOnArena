#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames: https://github.com/Gaskellgames/Unity_TransformUtilities
    /// </summary>

    [CustomEditor(typeof(Transform)), CanEditMultipleObjects]
    public class TransformEditor : Editor
    {
        #region Serialized Properties & Variables
        
        private enum Axis { X, Y, Z }
        
        private Type transformInspectorType => typeof(EditorApplication).Assembly.GetType("UnityEditor.TransformInspector");
        private Editor transformEditor;
        private List<Transform> transformTargets = new List<Transform>();
        private Texture iconTexture;
        private Rect[] repaintPositions;
        private GUIStyle iconButtonStyle = new GUIStyle();
        private GUIStyle buttonStyle2 = new GUIStyle();
        
        private readonly float standardGap = 4; // double standard gap width
        private static bool utilitiesOpen;
        private static Axis axis = Axis.X;

        #endregion

        //----------------------------------------------------------------------------------------------------
        
        #region OnEnable / OnDisable

        private void OnEnable()
        {
            AssignTransformEditor();
        }

        private void OnDisable()
        {
            ClearTransformEditor();
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region OnInspectorGUI

        public override void OnInspectorGUI()
        {
            // null check
            if (!transformEditor)
            {
                base.OnInspectorGUI();
                return;
            }
            
            // get reference changes
            serializedObject.Update();
            
            GUI.changed = false;
            if (EditorWindowUtility.settings.showTransformInspector_DefaultUnity)
            {
                transformEditor.OnInspectorGUI();
            }
            else
            {
                repaintPositions = new Rect[6];
                CreateButtons();
                
                // draw inspector
                if (EditorWindowUtility.settings.showTransformInspector_AlignTools) { OnInspectorGUI_AlignTools(); }
                if (EditorWindowUtility.settings.showTransformInspector_ResetButtons) { OnInspectorGUI_ResetButtons(); }
                else { transformEditor.OnInspectorGUI(); }
                if (EditorWindowUtility.settings.showTransformInspector_TransformUtilities) { OnInspectorGUI_TransformUtilities(); }
                
                // force update window (to have snappy hover on buttons) if mouse over buttons
                foreach (var repaintPosition in repaintPositions)
                {
                    if (repaintPosition.Contains(Event.current.mousePosition)) { Repaint(); }
                }
            }
            
            // apply reference changes
            serializedObject.ApplyModifiedProperties();
        }

        private void OnInspectorGUI_ResetButtons()
        {
	        // start wrapped transform editor and custom reset buttons
	        GUILayout.BeginHorizontal();
	        
            // default transform
            GUILayout.BeginVertical();
            transformEditor.OnInspectorGUI();
            GUILayout.EndVertical();
            
            // reset buttons
            GUILayout.BeginVertical(GUILayout.Width(20));
            iconTexture = EditorGUIUtility.IconContent("d_Refresh").image;
            if (GUILayout.Button(iconTexture, iconButtonStyle, GUILayout.Width(20), GUILayout.Height(20)))
            {
                string names = "";
                foreach (var transform in transformTargets)
                {
                    if (transform.localPosition == Vector3.zero) { continue;}
                    
                    Undo.RecordObject(transform, $"Local position reset for {transform.gameObject.name}");
                    transform.localPosition = Vector3.zero;
                    names += names == "" ? transform.gameObject.name : ", " + transform.gameObject.name;
                }
                if (names != "")
                {
                    AssignTransformEditor();
                    Debug.Log($"Local position reset for '{names}'", this);
                }
            }
            repaintPositions[0] = GUILayoutUtility.GetLastRect();
            GUILayout.Space(1);
            iconTexture = EditorGUIUtility.IconContent("d_Refresh").image;
            if (GUILayout.Button(iconTexture, iconButtonStyle, GUILayout.Width(20), GUILayout.Height(20)))
            {
                string names = "";
                foreach (var transform in transformTargets)
                {
                    if (transform.localEulerAngles == Vector3.zero) { continue;}
                    Undo.RecordObject(transform, $"Local rotation reset for {transform.gameObject.name}");
                    transform.localEulerAngles = Vector3.zero;
                    names += names == "" ? transform.gameObject.name : ", " + transform.gameObject.name;
                }
                if (names != "")
                {
                    AssignTransformEditor();
                    Debug.Log($"Local rotation reset for '{names}'", this);
                }
            }
            repaintPositions[1] = GUILayoutUtility.GetLastRect();
            GUILayout.Space(1);
            iconTexture = EditorGUIUtility.IconContent("d_Refresh").image;
            if (GUILayout.Button(iconTexture, iconButtonStyle, GUILayout.Width(20), GUILayout.Height(20)))
            {
                string names = "";
                foreach (var transform in transformTargets)
                {
                    if (transform.localScale == Vector3.one) { continue;}
                    Undo.RecordObject(transform, $"Local scale reset for {transform.gameObject.name}");
                    transform.localScale = Vector3.one;
                    names += names == "" ? transform.gameObject.name : ", " + transform.gameObject.name;
                }
                if (names != "")
                {
                    AssignTransformEditor();
                    Debug.Log($"Local scale reset for '{names}'", this);
                }
            }
            repaintPositions[2] = GUILayoutUtility.GetLastRect();
            GUILayout.EndVertical();
            
            // end wrapped transform editor and custom reset buttons
            GUILayout.EndHorizontal();
        }

        private void OnInspectorGUI_TransformUtilities()
        {
            // get & update references
            Color defaultBackground = GUI.backgroundColor;
            
            // scale warning: top
            GUILayout.Space(2);
            DrawScaleWarning(transformTargets, 1, -2);
            
            // utilities
            EditorExtensions.BeginCustomInspectorBackground(InspectorExtensions.backgroundNormalColor, 1, -3);
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = new Color32(223, 223, 223, 079);
            if (GUILayout.Button(new GUIContent("Transform Utilities", "View readonly info"), buttonStyle2, GUILayout.Width(100), GUILayout.Height(buttonStyle2.fontSize)))
            {
                utilitiesOpen = !utilitiesOpen;
            }
            repaintPositions[3] = GUILayoutUtility.GetLastRect();
            GUI.backgroundColor = defaultBackground;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            if (utilitiesOpen)
            {
                GUILayout.Space(-1);
                EditorExtensions.DrawInspectorLineFull(InspectorExtensions.backgroundSeperatorColor, 2, 2);
                GUILayout.Space(2);
                GUI.enabled = false;
                GUILayout.Space(2);
                if (1 == transformTargets.Count && NonUniformScaleInObject(transformTargets[0]) || NonUniformScaleInParent(transformTargets))
                {
                    // non-uniform scale
                    GUI.enabled = true;
                    EditorGUILayout.HelpBox("Non-uniform scale detected. It is recommended to keep scale at '1, 1, 1' where possible.", MessageType.Warning);
                    GUI.enabled = false;
                }
                GUILayout.Space(2);
                OnInspectorGUI_GlobalPositions();
                EditorExtensions.DrawInspectorLineFull(InspectorExtensions.backgroundSeperatorColor, 2, 2);
                OnInspectorGUI_AssetTags();
                GUILayout.Space(2);
                GUI.enabled = true;
            }
            GUILayout.Space(2);
            EditorExtensions.EndCustomInspectorBackground();
            
            // scale warning: bottom
            DrawScaleWarning(transformTargets, 2, -3);
        }

        private void OnInspectorGUI_AlignTools()
        {
            Color32 defaultBackground = GUI.backgroundColor;
            bool defaultGUI = GUI.enabled;
            GUI.enabled = true;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(EditorGUIUtility.labelWidth);
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = axis == Axis.X ? InspectorExtensions.buttonSelectedColor : defaultBackground;
            if (GUILayout.Button(GgGUI.IconContent("Icon_AxisX"), EditorStyles.miniButtonLeft, GUILayout.Width(25), GUILayout.Height(20)))
            {
                Undo.RecordObject(this, $"Axis{axis}");
                axis = Axis.X;
            }
            GUI.backgroundColor = axis == Axis.Y ? InspectorExtensions.buttonSelectedColor : defaultBackground;
            if (GUILayout.Button(GgGUI.IconContent("Icon_AxisY"), EditorStyles.miniButtonMid, GUILayout.Width(25), GUILayout.Height(20)))
            {
                Undo.RecordObject(this, $"Axis{axis}");
                axis = Axis.Y;
            }
            GUI.backgroundColor = axis == Axis.Z ? InspectorExtensions.buttonSelectedColor : defaultBackground;
            if (GUILayout.Button(GgGUI.IconContent("Icon_AxisZ"), EditorStyles.miniButtonRight, GUILayout.Width(25), GUILayout.Height(20)))
            {
                Undo.RecordObject(this, $"Axis{axis}");
                axis = Axis.Z;
            }
            GUILayout.Space(20);
            GUI.backgroundColor = defaultBackground;
            GUI.enabled = 1 < targets.Length;
            if (GUILayout.Button(GgGUI.IconContent("Icon_AlignMin"), EditorStyles.miniButtonLeft, GUILayout.Width(25), GUILayout.Height(20)))
            {
	            AlignMin(axis);
	            VerboseLogs.Log($"Selected targets aligned to their min point in the {axis} axis.", this);
            }
            if (GUILayout.Button(GgGUI.IconContent("Icon_AlignMid"), EditorStyles.miniButtonMid, GUILayout.Width(25), GUILayout.Height(20)))
            {
	            AlignMid(axis);
	            VerboseLogs.Log($"Selected targets aligned to their mid point in the {axis} axis.", this);
            }
            if (GUILayout.Button(GgGUI.IconContent("Icon_AlignMax"), EditorStyles.miniButtonMid, GUILayout.Width(25), GUILayout.Height(20)))
            {
	            AlignMax(axis);
	            VerboseLogs.Log($"Selected targets aligned to their max point in the {axis} axis.", this);
            }
            if (GUILayout.Button(GgGUI.IconContent("Icon_AlignDis"), EditorStyles.miniButtonRight, GUILayout.Width(25), GUILayout.Height(20)))
            {
	            AlignDistribute(axis);
	            VerboseLogs.Log($"Selected targets distributed evenly in the {axis} axis.", this);
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUI.enabled = defaultGUI;
        }
        
        private void OnInspectorGUI_GlobalPositions()
        {
            bool defaultGUI = GUI.enabled;
            GUI.enabled = false;
            if (targets.Length <= 1)
            {
                // global transform properties
                EditorGUILayout.Vector3Field(new GUIContent("Global Position", "The world space position of the Transform."), transformTargets[0].position);
                EditorGUILayout.Vector3Field(new GUIContent("Global Rotation", "A Quaternion that stores the rotation of the Transform in world space."), transformTargets[0].eulerAngles);
                EditorGUILayout.Vector3Field(new GUIContent("Lossy Scale", "The global scale of the object (Read Only)."), transformTargets[0].lossyScale);
            }
            else
            {
                EditorGUILayout.LabelField("Global properties not available when multiple objects are selected.");
            }
            GUI.enabled = defaultGUI;
        }

        private void OnInspectorGUI_AssetTags()
        {
            bool defaultGUI = GUI.enabled;
            GUI.enabled = false;
            if (targets.Length <= 1)
            {
                // prefab labels
                EditorGUILayout.LabelField(new GUIContent("Asset Labels:", "If this gameObject is a prefab, all labels applied to it will show here."));
                Object sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(transformTargets[0].gameObject);
                string[] labels = EditorExtensions.GetAllObjectLabels(sourceObject);
                if (0 < labels.Length)
                {
                    float labelLineWidth = 0;
                    EditorGUILayout.BeginHorizontal();
                    Color defaultBackground = GUI.backgroundColor;
                    GUI.backgroundColor = new Color32(179, 179, 179, 255);
                    for (int i = 0; i < labels.Length; i++)
                    {
                        float labelWidth = StringExtensions.GetStringWidth(labels[i]) + (standardGap * 1.5f);
                        labelLineWidth += labelWidth + standardGap;
                        if (Screen.width <= labelLineWidth + standardGap)
                        {
                            GUILayout.FlexibleSpace();
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            labelLineWidth = labelWidth + standardGap;
                        }
                        GUILayout.Button(labels[i], GUILayout.Width(labelWidth));
                    }
                    GUILayout.FlexibleSpace();
                    GUI.backgroundColor = defaultBackground;
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.LabelField("n/a");
                }
            }
            else
            {
                EditorGUILayout.LabelField("Labels not available when multiple objects are selected.");
            }
            GUI.enabled = defaultGUI;
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Private Functions

        private void ClearTransformEditor()
        {
            // clear target transforms
            transformTargets = new List<Transform>();
            
            // destroy editor instance if exists
            if (!transformEditor) { return; }
            MethodInfo disableMethod = transformEditor.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (disableMethod != null) { disableMethod.Invoke(transformEditor, null); }
            DestroyImmediate(transformEditor);
            transformEditor = null;
        }
        
        private void AssignTransformEditor()
        {
            ClearTransformEditor();
            
            // null checks
            if (target == null || transformInspectorType == null) { return; }
            if (1 < targets.Length && targets[0] != null)
            {
                // assign selected targets
                transformTargets.Clear();
                foreach (var targetObject in targets)
                {
                    Transform transform = targetObject as Transform;
                    if (transform && !transformTargets.Contains(transform))
                    {
                        transformTargets.Add(transform);
                    }
                }
                
                // assign transform editor
                transformEditor = CreateEditorWithContext(targets, target, transformInspectorType);
            }
            else
            {
                // assign selected targets
                transformTargets.Clear();
                Transform transform = target as Transform;
                transformTargets.Add(transform);
                
                // assign transform editor
                transformEditor = CreateEditor(target, transformInspectorType);
            }
        }
        
        private void DrawScaleWarning(List<Transform> transformTargets, float paddingTop = -4F, float paddingBottom = -15F, float paddingLeft = -18F, float paddingRight = -4F)
        {
            if (1 == transformTargets.Count && NonUniformScaleInObject(transformTargets[0]))
            {
                EditorExtensions.BeginCustomInspectorBackground(new Color32(223, 050, 050, 255), paddingTop, paddingBottom, paddingLeft, paddingRight);
                EditorExtensions.EndCustomInspectorBackground();
            }
            else if (NonUniformScaleInParent(transformTargets))
            {
                EditorExtensions.BeginCustomInspectorBackground(new Color32(179, 128, 000, 255), paddingTop, paddingBottom, paddingLeft, paddingRight);
                EditorExtensions.EndCustomInspectorBackground();
            }
            else
            {
                EditorExtensions.BeginCustomInspectorBackground(new Color32(028, 128, 028, 255), paddingTop, paddingBottom, paddingLeft, paddingRight);
                EditorExtensions.EndCustomInspectorBackground();
            }
        }
        
        private bool NonUniformScaleInParent(List<Transform> transformTargets)
        {
            foreach (var transformTarget in transformTargets)
            {
                if (transformTarget.lossyScale != Vector3.one) { return true; }
            }
            
            return false;
        }

        private bool NonUniformScaleInObject(Transform transformTarget)
        {
            return transformTarget.localScale != Vector3.one;
        }
        
        private void CreateButtons()
        {
            // button style 1 (reset button)
            iconButtonStyle.fontSize = 9;
            iconButtonStyle.alignment = TextAnchor.MiddleCenter;
            iconButtonStyle.normal.textColor = InspectorExtensions.textNormalColor;
            iconButtonStyle.hover.textColor = InspectorExtensions.textNormalColor;
            iconButtonStyle.active.textColor = InspectorExtensions.textNormalColor;
            iconButtonStyle.normal.background = InspectorExtensions.CreateTexture(20, 20, 1, true, InspectorExtensions.blankColor, InspectorExtensions.blankColor);
            iconButtonStyle.hover.background = InspectorExtensions.CreateTexture(20, 20, 1, true, InspectorExtensions.buttonHoverColor, InspectorExtensions.blankColor);
            iconButtonStyle.active.background = InspectorExtensions.CreateTexture(20, 20, 1, true, InspectorExtensions.buttonActiveColor, InspectorExtensions.buttonActiveBorderColor);

            // button style 2 (utilities)
            buttonStyle2.fontSize = 10;
            buttonStyle2.normal.textColor = InspectorExtensions.textDisabledColor;
        }

		private void AlignMin(Axis axis)
		{
			if (transformTargets.Count <= 1) { return; }
			
			float min = GetMinPosition(axis);
			Undo.RecordObjects(transformTargets.ToArray(), $"AlignMin{axis}");
			foreach (Transform target in transformTargets)
			{
				Vector3 position = target.position;
				switch (axis)
				{
					case Axis.X:
						position = new Vector3(min, position.y, position.z);
						break;
					
					case Axis.Y:
						position = new Vector3(position.x, min, position.z);
						break;
					
					case Axis.Z:
						position = new Vector3(position.x, position.y, min);
						break;
				}
				target.position = position;
			}
		}
		
		private void AlignMid(Axis axis)
		{
			if (transformTargets.Count <= 1) { return; }
			
			float min = GetMinPosition(axis);
			float max = GetMaxPosition(axis);
			float mid = (min + max) * 0.5f;
			Undo.RecordObjects(transformTargets.ToArray(), $"AlignMid{axis}");
			foreach (Transform target in transformTargets)
			{
				Vector3 position = target.position;
				switch (axis)
				{
					case Axis.X:
						position = new Vector3(mid, position.y, position.z);
						break;
					
					case Axis.Y:
						position = new Vector3(position.x, mid, position.z);
						break;
					
					case Axis.Z:
						position = new Vector3(position.x, position.y, mid);
						break;
				}
				target.position = position;
			}
		}
		
		private void AlignMax(Axis axis)
		{
			if (transformTargets.Count <= 1) { return; }
			
			float max = GetMaxPosition(axis);
			Undo.RecordObjects(transformTargets.ToArray(), $"AlignMax{axis}");
			foreach (Transform target in transformTargets)
			{
				Vector3 position = target.position;
				switch (axis)
				{
					case Axis.X:
						position = new Vector3(max, position.y, position.z);
						break;
					
					case Axis.Y:
						position = new Vector3(position.x, max, position.z);
						break;
					
					case Axis.Z:
						position = new Vector3(position.x, position.y, max);
						break;
				}
				target.position = position;
			}
		}
		
		private void AlignDistribute(Axis axis)
		{
			if (transformTargets.Count <= 1) { return; }
			
			float min = GetMinPosition(axis);
			float max = GetMaxPosition(axis);
			float gap = transformTargets.Count <= 1 ? 0 : (max - min) / (float)(transformTargets.Count - 1);

			IOrderedEnumerable<Transform> orderedEnumerable = null;
			switch (axis)
			{
				case Axis.X:
					orderedEnumerable = transformTargets.OrderBy((c) => c.transform.position.x);
					break;
					
				case Axis.Y:
					orderedEnumerable = transformTargets.OrderBy((c) => c.transform.position.y);
					break;
					
				case Axis.Z:
					orderedEnumerable = transformTargets.OrderBy((c) => c.transform.position.z);
					break;
			}
			List<Transform> sortedTargets = new List<Transform>(orderedEnumerable);
			if (sortedTargets.Count <= 0) { return; }

			Undo.RecordObjects(sortedTargets.ToArray(), $"AlignDistribute{axis}");
			for (var i = 0; i < sortedTargets.Count; i++)
			{
				Vector3 position = sortedTargets[i].position;
				switch (axis)
				{
					case Axis.X:
						position = new Vector3(min + (gap * i), position.y, position.z);
						break;
					
					case Axis.Y:
						position = new Vector3(position.x, min + (gap * i), position.z);
						break;
					
					case Axis.Z:
						position = new Vector3(position.x, position.y, min + (gap * i));
						break;
				}
				sortedTargets[i].position = position;
			}
		}
		
		private float GetMinPosition(Axis axis)
		{
			bool isFirst = true;
			float min = 0;
			switch (axis)
			{
				case Axis.X:
					foreach (Transform target in transformTargets)
					{
						if (min < target.position.x && !isFirst) { continue;}
						min = target.position.x;
						isFirst = false;
					}
					break;
				
				case Axis.Y:
					foreach (Transform target in transformTargets)
					{
						if (min < target.position.y && !isFirst) { continue;}
						min = target.position.y;
						isFirst = false;
					}
					break;
				
				case Axis.Z:
					foreach (Transform target in transformTargets)
					{
						if (min < target.position.z && !isFirst) { continue;}
						min = target.position.z;
						isFirst = false;
					}
					break;
			}
			return min;
		}

		private float GetMaxPosition(Axis axis)
		{
			bool isFirst = true;
			float max = 0;
			switch (axis)
			{
				case Axis.X:
					foreach (Transform target in transformTargets)
					{
						if (target.position.x < max && !isFirst) { continue;}
						max = target.position.x;
						isFirst = false;
					}
					break;
				
				case Axis.Y:
					foreach (Transform target in transformTargets)
					{
						if (target.position.y < max && !isFirst) { continue;}
						max = target.position.y;
						isFirst = false;
					}
					break;
				
				case Axis.Z:
					foreach (Transform target in transformTargets)
					{
						if (target.position.z < max && !isFirst) { continue;}
						max = target.position.z;
						isFirst = false;
					}
					break;
			}
			return max;
		}

		#endregion

    } // class end
}
#endif
