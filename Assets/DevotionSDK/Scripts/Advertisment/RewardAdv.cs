using System;
using UnityEngine;

namespace Devotion.SDK.Advertisment
{
    [Serializable]
    public class RewardAdv
    {
        [SerializeField] private string _id;
        [SerializeField] private bool _isEnabled;
        [SerializeField] private int _numberOfUses;
        [SerializeField] private int _recoverySeconds;

        public string ID => _id;
        public bool IsEnabled => _isEnabled;
        public int NumberOfUses => _numberOfUses;
        public int RecoverySeconds => _recoverySeconds;
    }
}