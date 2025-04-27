using System.Collections.Generic;
using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>
    
    [System.Serializable]
    internal class SerializedDictionaryKeyValueList<TKey, TValue>
    {
        [SerializeField, Required]
        internal TValue value;
        
        [SerializeField, Required]
        internal List<TKey> keys;
        
        internal SerializedDictionaryKeyValueList(TKey key, TValue value)
        {
            this.keys = new List<TKey>() { key };
            this.value = value;
        }
        
        internal SerializedDictionaryKeyValueList(List<TKey> keys, TValue value)
        {
            this.keys = keys;
            this.value = value;
        }

        internal bool IsValidValue()
        {
            if (value is not Object valueAsObject) { return true; }
            return valueAsObject != null;
        }

        internal bool IsValidKeysCount()
        {
            return 0 < keys.Count;
        }
        
        internal bool IsValidKeys()
        {
            if (!IsValidKeysCount()) { return false; }
            bool isValid = true;
            foreach (TKey key in keys)
            {
                if (key is not Object keyAsObject) { continue; }
                if (keyAsObject == null) { isValid = false; }
            }
            return isValid;
        }

        internal bool ClearInValidKeys()
        {
            bool keyRemoved = false;
            for (int i = keys.Count - 1; i >= 0; i--)
            {
                TKey key = keys[i];
                if (key is not Object keyAsObject) { continue; }
                if (keyAsObject != null) { continue; }
                keys.RemoveAt(i);
                keyRemoved = true;
            }
            return keyRemoved;
        }

        internal bool IsValue(TValue value)
        {
            return EqualityComparer<TValue>.Default.Equals(this.value, value);
        }

        internal bool ContainsKey(TKey key)
        {
            return keys.Contains(key);
        }

        internal bool IsOnlyKey(TKey key)
        {
            return keys.Contains(key) && keys.Count == 1;
        }

        internal bool TryAddKey(TKey key)
        {
            if (keys.Contains(key)) { return false; }
            keys.Add(key);
            return true;
        }

        internal void AddKey(TKey key)
        {
            if (keys.Contains(key)) { return; }
            keys.Add(key);
        }

        internal bool RemoveKey(TKey key)
        {
            return keys.Remove(key);
        }
        
    } // class end
}