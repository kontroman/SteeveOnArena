#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    [InitializeOnLoad]
    public class DefineSymbols_GgCore
    {
        #region Variables

        private static readonly string[] ExtraScriptingDefineSymbols = new string[] { "GASKELLGAMES" };

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Constructors

        static DefineSymbols_GgCore()
        {
            AddExtraScriptingDefineSymbols(ExtraScriptingDefineSymbols);
        }
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Private Functions
        
        /// <summary>
        /// Add any new ScriptingDefineSymbols in extraScriptingDefineSymbols to the current ScriptingDefineSymbols in build settings
        /// </summary>
        /// <param name="extraScriptingDefineSymbols"></param>
        public static void AddExtraScriptingDefineSymbols(string[] extraScriptingDefineSymbols)
        {
            // add any new ScriptingDefineSymbols from ExtraScriptingDefineSymbols
            List<string> scriptingDefineSymbols = GetScriptDefineSymbols();
            scriptingDefineSymbols.TryAddRange(extraScriptingDefineSymbols.ToList());
            SetScriptDefineSymbols(scriptingDefineSymbols);
        }

        /// <summary>
        /// Get all ScriptingDefineSymbols from build settings.
        /// </summary>
        /// <returns></returns>
        private static List<string> GetScriptDefineSymbols()
        {
#if UNITY_6000_0_OR_NEWER
            UnityEditor.Build.NamedBuildTarget selectedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string scriptingDefineSymbolsForGroup = PlayerSettings.GetScriptingDefineSymbols(selectedBuildTarget);
#else
            string scriptingDefineSymbolsForGroup = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#endif
            return scriptingDefineSymbolsForGroup.Split(';').ToList();
        }

        /// <summary>
        /// Set the ScriptingDefineSymbols in build settings.
        /// </summary>
        /// <param name="scriptingDefineSymbols"></param>
        private static void SetScriptDefineSymbols(List<string> scriptingDefineSymbols)
        {
            string newScriptingDefineSymbols = string.Join(";", scriptingDefineSymbols.ToArray());
            
#if UNITY_6000_0_OR_NEWER
            UnityEditor.Build.NamedBuildTarget selectedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            PlayerSettings.SetScriptingDefineSymbols(selectedBuildTarget, newScriptingDefineSymbols);
#else
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, newScriptingDefineSymbols);
#endif
        }
        
        #endregion
        
    } // class end
}

#endif