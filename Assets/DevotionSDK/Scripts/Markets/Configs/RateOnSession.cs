using System;
using UnityEngine;

namespace Devotion.SDK.Markets.Confgs
{
    [Serializable]
    public class RateOnSession
    {
        [SerializeField] private int sessionNumber;
        [SerializeField] private int level;

        public int SessionNumber => sessionNumber;
        public int Level => level;
    }
}