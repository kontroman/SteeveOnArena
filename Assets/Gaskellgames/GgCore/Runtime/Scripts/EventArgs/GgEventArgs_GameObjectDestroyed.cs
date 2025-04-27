using System;
using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [Serializable]
    public class GgEventArgs_GameObjectDestroyed : GgEventArgs
    {
        public int instanceID;
        public GameObject parent;
        
        public GgEventArgs_GameObjectDestroyed(int instanceID, GameObject parent)
        {
            this.instanceID = instanceID;
            this.parent = parent;
        }

    } // class end
}