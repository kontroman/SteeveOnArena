using System;
using UnityEditor;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [Serializable]
    public class GgEventArgs_AssetObjectDestroyed : GgEventArgs
    {
        public int instanceID;
        public string guid;
        
        public GgEventArgs_AssetObjectDestroyed(int instanceID, string guid)
        {
            this.instanceID = instanceID;
            this.guid = guid;
        }

#if UNITY_EDITOR
        public GgEventArgs_AssetObjectDestroyed(int instanceID, GUID guid)
        {
            this.instanceID = instanceID;
            this.guid = guid.ToString();
        }
#endif

    } // class end
}