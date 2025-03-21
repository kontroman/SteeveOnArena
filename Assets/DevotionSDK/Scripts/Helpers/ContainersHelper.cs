using Devotion.Interfaces;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.SDK.Helpers
{
    public static class ContainersHelper
    {
        [System.Serializable]
        public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
        {
            [SerializeField] private List<TKey> keys = new List<TKey>();
            [SerializeField] private List<TValue> values = new List<TValue>();

            public void OnBeforeSerialize()
            {
                keys.Clear();
                values.Clear();

                foreach (var pair in this)
                {
                    keys.Add(pair.Key);
                    values.Add(pair.Value);
                }
            }

            public void OnAfterDeserialize()
            {
                this.Clear();

                for (int i = 0; i < keys.Count; i++)
                {
                    this[keys[i]] = values[i];
                }
            }
        }

        [System.Serializable]
        public class TagActionPair
        {
            public string tag;
            public TriggerAction action;
        }
    }
}