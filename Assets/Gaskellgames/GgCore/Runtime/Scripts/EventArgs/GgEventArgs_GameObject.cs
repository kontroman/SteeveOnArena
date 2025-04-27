using System;
using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [Serializable]
    public class GgEventArgs_GameObject : GgEventArgs
    {
        public GameObject gameObject;
        
        public GgEventArgs_GameObject(GameObject gameObject)
        {
            this.gameObject = gameObject;
        }

    } // class end
}