using System;
using Object = UnityEngine.Object;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [Serializable]
    public class GgEventArgs_AssetObject : GgEventArgs
    {
        public Object asset;
        public string assetPath;
        
        public GgEventArgs_AssetObject(Object asset, string assetPath)
        {
            this.asset = asset;
            this.assetPath = assetPath;
        }

    } // class end
}