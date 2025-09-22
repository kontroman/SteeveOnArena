using MineArena.AI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace MineArena.ObjectPools
{
    [CreateAssetMenu(fileName = "New ObjectPoolPreset", menuName = "ObjectPool")]
    public class ObjectPoolPreset : ScriptableObject
    {
        public int MaxPoolSize = 50;
        public int DefaultPoolSize = 10;
        public List<GameObject> Preset = new List<GameObject>();
    }
}
