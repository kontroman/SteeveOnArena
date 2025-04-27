#if UNITY_EDITOR
using UnityEngine;

namespace Gaskellgames.EditorOnly
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    //[CreateAssetMenu(fileName = "Sample_Attribute_NavMeshMask", menuName = "Gaskellgames/GGSamplePage")]
    public class Sample_Attribute_NavMeshMask : ScriptableObject
    {
        // ---------- NavMeshMask ----------

        [SerializeField, NavMeshMask]
        private int navMeshMask;

        [field: SerializeField, NavMeshMask]
        private int NavMeshMask { get; set; }

    } // class end
}

#endif