using System;
using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [Serializable]
    public class GgEventArgs_GameObjectChangedParent : GgEventArgs
    {
        public GameObject gameObject;
        public GameObject newParent;
        public GameObject oldParent;
        public SceneData newSceneData;
        public SceneData oldSceneData;
        
        public GgEventArgs_GameObjectChangedParent(GameObject gameObject, GameObject oldParent, GameObject newParent, SceneData oldSceneData, SceneData newSceneData)
        {
            this.gameObject = gameObject;
            this.newParent = newParent;
            this.oldParent = oldParent;
            this.newSceneData = newSceneData;
            this.oldSceneData = oldSceneData;
        }

    } // class end
}