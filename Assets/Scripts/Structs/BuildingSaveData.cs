using System;
using UnityEngine;

namespace MineArena.Structs
{
    [Serializable]
    public class BuildingSaveData
    {
        public int Level;
        public Transform transform;

        public BuildingSaveData(int level, Transform transform)
        {
            this.Level = level;
            this.transform = transform;
        }
    }
}