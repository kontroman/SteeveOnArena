using System;
using UnityEngine;

namespace Devotion.SDK.Confgs
{
    [Serializable]
    public class RateOnSession
    {
        [SerializeField] private int _sessionNumber;
        [SerializeField] private int _level;

        public int SessionNumber => _sessionNumber;
        public int Level => _level;
    }
}