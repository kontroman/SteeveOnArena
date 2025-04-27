using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gaskellgames
{
    /// <summary>
    /// Code created by Gaskellgames
    /// </summary>

    #region SerializedDictionaryBase

    public abstract class SerializedDictionaryBase
    {
        public abstract void Initialise();
    }

    #endregion
    
    [System.Serializable]
    public class SerializedDictionary<TKey, TValue> : SerializedDictionaryBase
    {
        #region Variables
        
        [SerializeField]
        private List<SerializedDictionaryKeyValueList<TKey, TValue>> serializedDictionary;
        
        private Dictionary<TKey, TValue> dictionary;
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------

        #region Constructors

        /// <summary>
        /// Construct a new blank serializedDictionary
        /// </summary>
        public SerializedDictionary()
        {
            dictionary = new Dictionary<TKey, TValue>();
            serializedDictionary = new List<SerializedDictionaryKeyValueList<TKey, TValue>>();
        }
        
        /// <summary>
        /// Construct this SerializedDictionary from another dictionary of the same key value type
        /// </summary>
        /// <param name="otherDictionary"></param>
        public SerializedDictionary(Dictionary<TKey, TValue> otherDictionary)
        {
            // get key values from otherDictionary
            List<TValue> allValues = otherDictionary.Values.ToList();
            List<TKey> allKeys = otherDictionary.Keys.ToList();
            
            // check valid data
            if (allValues.Count != allKeys.Count) { return; }
            
            // convert otherDictionary to serializedDictionary
            serializedDictionary = new List<SerializedDictionaryKeyValueList<TKey, TValue>>();
            for (int i = 0; i < allKeys.Count; i++)
            {
                TryAdd(allKeys[i], allValues[i]);
            }
            Initialise();
        }

        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region private Functions
        
        private void InitialiseIfRequired()
        {
            if (dictionary != null) { return; }
            Initialise();
        }
        
        #endregion
        
        //----------------------------------------------------------------------------------------------------
        
        #region Public Functions
        
        /// <summary>
        /// Initialise the dictionary from the serialized key values
        /// </summary>
        public sealed override void Initialise()
        {
            dictionary = new Dictionary<TKey, TValue>();
            serializedDictionary ??= new List<SerializedDictionaryKeyValueList<TKey, TValue>>();
            
            foreach (SerializedDictionaryKeyValueList<TKey, TValue> keyValue in serializedDictionary)
            {
                foreach (TKey key in keyValue.keys)
                {
                    dictionary.TryAdd(key, keyValue.value);
                }
            }
        }
        
        /// <summary>
        /// Gets the IEqualityComparer&lt;<see cref="TKey"/>&gt; that is used to determine equality of keys for the dictionary.
        /// </summary>
        public IEqualityComparer<TKey> Comparer
        {
            get
            {
                InitialiseIfRequired();
                return dictionary.Comparer;
            }
        }
        
        /// <summary>
        /// Gets the number of key/value pairs contained in the dictionary.
        /// </summary>
        public int Count
        {
            get
            {
                InitialiseIfRequired();
                return dictionary.Count;
            }
        }
        
        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        public Dictionary<TKey, TValue>.KeyCollection Keys
        {
            get
            {
                InitialiseIfRequired();
                return dictionary.Keys;
            }
        }
        
        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        public Dictionary<TKey, TValue>.ValueCollection Values
        {
            get
            {
                InitialiseIfRequired();
                return dictionary.Values;
            }
        }
        
        /// <summary>
        /// Convert to a standard none-serialized dictionary.
        /// </summary>
        /// <returns></returns>
        public Dictionary<TKey, TValue> ToDictionary()
        {
            InitialiseIfRequired();
            return dictionary;
        }
        
        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(TKey key, TValue value)
        {
            InitialiseIfRequired();
            
            // handle dictionary
            dictionary.Add(key, value);
            
            // handle serialize list
            bool addedToExisting = false;
            foreach (var keyValueList in serializedDictionary)
            {
                if (!keyValueList.IsValue(value)) { continue; }
                keyValueList.AddKey(key);
                addedToExisting = true;
            }
            if (!addedToExisting)
            {
                serializedDictionary.Add(new SerializedDictionaryKeyValueList<TKey, TValue>(key , value));
            }
        }
        
        /// <summary>
        /// Attempts to add the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>True if key and value successfully added to the dictionary</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            InitialiseIfRequired();
            
            // handle dictionary
            if (!dictionary.TryAdd(key, value)) { return false; }
            
            // handle serialize list
            bool addedToExisting = false;
            foreach (var keyValueList in serializedDictionary)
            {
                if (!keyValueList.IsValue(value)) { continue; }
                keyValueList.AddKey(key);
                addedToExisting = true;
            }
            if (!addedToExisting)
            {
                serializedDictionary.Add(new SerializedDictionaryKeyValueList<TKey, TValue>(key , value));
            }
            return true;
        }
        
        /// <summary>
        /// Determines whether the dictionary contains a specific key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(TKey key)
        {
            InitialiseIfRequired();
            return dictionary.ContainsKey(key);
        }
        
        /// <summary>
        /// Determines whether the dictionary contains a specific value.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool ContainsValue(TValue value)
        {
            InitialiseIfRequired();
            return dictionary.ContainsValue(value);
        }
        
        /// <summary>
        /// Removes all keys and values from the dictionary.
        /// </summary>
        public void Clear()
        {
            InitialiseIfRequired();
            
            // handle dictionary
            dictionary.Clear();
            
            // handle serialize list
            serializedDictionary.Clear();
        }
        
        /// <summary>
        /// Removes all keys and values from the dictionary.
        /// </summary>
        public void ClearInvalidEntries()
        {
            bool reInitialise = false;
            for (int i = serializedDictionary.Count - 1; i >= 0; i--)
            {
                SerializedDictionaryKeyValueList<TKey, TValue> keyValueList = serializedDictionary[i];
                if (!keyValueList.IsValidValue())
                {
                    serializedDictionary.RemoveAt(i);
                    reInitialise = true;
                }
                if (keyValueList.ClearInValidKeys())
                {
                    reInitialise = true;
                }
                if (!keyValueList.IsValidKeysCount())
                {
                    serializedDictionary.RemoveAt(i);
                    reInitialise = true;
                }
            }
            
            if (reInitialise) { Initialise(); }
        }
        
        /// <summary>
        /// Ensures that the dictionary can hold up to a specified number of entries without any further expansion of its backing storage.
        /// </summary>
        /// <param name="capacity"></param>
        public void EnsureCapacity(int capacity)
        {
            InitialiseIfRequired();
            dictionary.EnsureCapacity(capacity);
        }
        
        /// <summary>
        /// Returns an enumerator that iterates through the dictionary.
        /// </summary>
        /// <returns></returns>
        public Dictionary<TKey,TValue>.Enumerator GetEnumerator()
        {
            InitialiseIfRequired();
            return dictionary.GetEnumerator();
        }
        
        /// <summary>
        /// Tries to get the value associated with the specified key in the dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public TValue GetValueOrDefault(TKey key)
        {
            InitialiseIfRequired();
            return dictionary.GetValueOrDefault(key);
        }
        
        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public bool TryGetValue(TKey key, out TValue value)
        {
            InitialiseIfRequired();
            return dictionary.TryGetValue(key, out value);
        }
        
        /// <summary>
        /// Try to get all keys used for a specific value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="keys"></param>
        /// <returns>Returns true if at least one key exists for a valid value, false otherwise</returns>
        public bool TryGetKeysForValue(TValue value, out List<TKey> keys)
        {
            List<TValue> allValues = Values.ToList();
            List<TKey> allKeys = Keys.ToList();
            
            // should always be true!
            if (allValues.Count != allKeys.Count && 0 < allValues.Count)
            {
                keys = new List<TKey>();
                return false;
            }

            keys = new List<TKey>();
            for (int i = 0; i < allValues.Count; i++)
            {
                if (value.Equals(allValues[i]))
                {
                    keys.Add(allKeys[i]);
                }
            }
            return true;
        }
        
        /// <summary>
        /// Sets the capacity of this dictionary to what it would be if it had been originally initialized with all its entries.
        /// </summary>
        public void TrimExcess()
        {
            InitialiseIfRequired();
            dictionary.TrimExcess();
        }
        
        /// <summary>
        /// Removes the value with the specified key from the dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <returns>True if key and value successfully removed from the dictionary</returns>
        public bool Remove(TKey key)
        {
            InitialiseIfRequired();
            
            // handle dictionary
            if (!dictionary.Remove(key)) { return false; }
            
            // handle serialize list
            for (int i = serializedDictionary.Count - 1; i >= 0; i--)
            {
                SerializedDictionaryKeyValueList<TKey, TValue> keyValueList = serializedDictionary[i];
                if (!keyValueList.ContainsKey(key)) { continue; }
                if (keyValueList.IsOnlyKey(key))
                {
                    serializedDictionary.Remove(keyValueList);
                }
                else
                {
                    keyValueList.RemoveKey(key);
                }
            }
            return true;
        }
        
        /// <summary>
        /// Removes the value with the specified key from the dictionary.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>True if key and value successfully removed from the dictionary</returns>
        public bool Remove(TKey key, out TValue value)
        {
            InitialiseIfRequired();
            
            // handle dictionary
            if (!dictionary.Remove(key, out value)) { return false; }
            
            // handle serialize list
            for (int i = serializedDictionary.Count - 1; i >= 0; i--)
            {
                SerializedDictionaryKeyValueList<TKey, TValue> keyValueList = serializedDictionary[i];
                if (!keyValueList.ContainsKey(key)) { continue; }
                if (keyValueList.IsOnlyKey(key))
                {
                    serializedDictionary.Remove(keyValueList);
                }
                else
                {
                    keyValueList.RemoveKey(key);
                }
            }
            return true;
        }

        #endregion
        
    } // class end
}