using Devotion.SDK.Enums;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.SDK.Base
{
    [Serializable]
    public class RemoteConfig
    {
        [SerializeField] private List<Markets.Markets> markets;
        [SerializeField] private string remoteGameConfig;

        public List<Markets.Markets> Markets => markets;
    }
}