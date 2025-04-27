using System;
using UnityEngine.SceneManagement;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [Serializable]
    public class GgEventArgs_SceneData : GgEventArgs
    {
        public SceneData sceneData;

        public GgEventArgs_SceneData(Scene scene)
        {
            sceneData = new SceneData(scene);
        }

    } // class end
}