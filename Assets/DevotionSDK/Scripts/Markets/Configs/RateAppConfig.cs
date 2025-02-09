using Devotion.Helpers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.SDK.Markets.Confgs
{
    //TODO: add social service
    [Serializable]
    public class RateAppConfig
    {
        [SerializeField] private bool _useRateApp;
        [SerializeField] private List<RateOnSession> _rateOnSessions;

        public int GetLevelOnSession(int sessionNumber)
        {
            if (!_useRateApp) return (int)RateAppResult.NotFound;

            var rateConfig = _rateOnSessions.Find(item => item.SessionNumber == sessionNumber)
                            ?? _rateOnSessions.Find(item => item.SessionNumber == -1);

            return rateConfig?.Level ?? (int)RateAppResult.NotFound;
        }
    }

    public enum RateAppResult
    {
        NotFound = -1
    }
}