#if UNITY_EDITOR
#if GASKELLGAMES
using System.IO;
using Gaskellgames.EditorOnly;
using UnityEditor;
using UnityEngine;

namespace Gaskellgames.FolderSystem.EditorOnly
{
    /// <summary>
    /// Code updated by Gaskellgames
    /// </summary>
    
    [InitializeOnLoad]
    public static class ProjectFolderIcons
    {
        #region Variables
        
        private static FolderIconSettings_SO settings;

        public static FolderIconSettings_SO Settings
        {
            get
            {
                if (settings) { return settings; }
                settings = EditorExtensions.GetAssetByType<FolderIconSettings_SO>();
                return settings;
            }
        }

        #endregion

        //----------------------------------------------------------------------------------------------------
        
        #region Constructors

        /// <summary>
        /// Update icon dictionary and run folder icon code during relevant OnGUI update loop.
        /// Note: Runs during the InitializeOnLoad call, so may be a delay in showing up for 'new users'
        /// </summary>
        static ProjectFolderIcons()
        {
            while (settings == null)
            {
                settings = EditorExtensions.GetAssetByType<FolderIconSettings_SO>();
            }
            settings.Initialise();
            EditorApplication.projectWindowItemOnGUI += DrawFolderIcon;
        }

        #endregion

        //----------------------------------------------------------------------------------------------------

        #region Private Functions
        
        /// <summary>
        /// Draw a specified folder icon (based on guid) at a specified position
        /// </summary>
        /// <param name="guid"></param>
        /// <param name="position"></param>
        private static void DrawFolderIcon(string guid, Rect position)
        {
            // null checks
            if (!settings) { return; }
            if (settings.ProjectFolderState == FolderIconSettings_SO.SelectedState.Off) { return; }
            if (settings.folderIconDictionary == null) { return; }
            
            // get asset path & dictionary info
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileName(path);

            // check for valid draw
            if (path == "") { return; }
            if (Event.current.type != EventType.Repaint) { return; }
            if (!settings.folderIconDictionary.ContainsKey(fileName)) { return; }
            if (!File.GetAttributes(path).HasFlag(FileAttributes.Directory)) { return; }

            // get image position
            float positionX = position.x - 1;
            float positionY = position.y - 1;
            float positionWidth = position.height + 2;
            float positionHeight = position.height + 2;
            if (20 < position.height)
            {
                positionWidth = position.width + 2;
                positionHeight = position.width + 2;
            }
            else if (position.x < 20)
            {
                positionX = position.x + 2;
            }
            Rect imagePosition = new Rect(positionX, positionY, positionWidth, positionHeight);

            // get image texture
            Texture texture = settings.folderIconDictionary[fileName];
            if (texture == null) { return; }

            // draw image texture at image position
            GUI.DrawTexture(imagePosition, texture);
        }

        #endregion
        
    } // class end
}
        
#endif
#endif