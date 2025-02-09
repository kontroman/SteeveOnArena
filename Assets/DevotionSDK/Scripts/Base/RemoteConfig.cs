using Devotion.SDK.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.SDK.Base
{
    [Serializable]
    public class RemoteConfig
    {
        [SerializeField] private List<Markets.Markets> _markets;
        [SerializeField] private string _remoteGameConfig;

        public List<Markets.Markets> Markets => _markets;
    }
}