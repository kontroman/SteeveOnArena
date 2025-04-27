using System;
using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    [Serializable]
    public class GgEventArgs_Component : GgEventArgs
    {
        public Component component;
        
        public GgEventArgs_Component(Component component)
        {
            this.component = component;
        }

    } // class end
}