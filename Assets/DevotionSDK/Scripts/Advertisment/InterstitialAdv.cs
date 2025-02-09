using System;
using UnityEngine;

namespace Devotion.SDK.Advertisment
{
    [Serializable]
    public class InterstitialAdv
    {
        [SerializeField] private string _id;
        [SerializeField] private bool _isEnabled;
        [SerializeField] private int _showInterval;

        public string ID => _id;
        public bool IsEnabled => _isEnabled;
        public int ShowInterval => _showInterval;
    }
}