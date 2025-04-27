using System;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [Serializable]
    public class GgEventArgs_PrefabInstance : GgEventArgs
    {
        public int instanceID;
        public SceneData sceneDate;
        
        public GgEventArgs_PrefabInstance(int instanceID, SceneData sceneDate)
        {
            this.instanceID = instanceID;
            this.sceneDate = sceneDate;
        }

    } // class end
}