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
        [SerializeField] private bool useRateApp;
        [SerializeField] private List<RateOnSession> _rateOnSessions;

        public int GetLevelOnSession(int sessionNumber)
        {
            if (!useRateApp) return -1;

            var rateConfig = _rateOnSessions.Find(item => item.SessionNumber == sessionNumber);
            if (rateConfig.IsNullOrDead()) rateConfig = _rateOnSessions.Find(item => item.SessionNumber == -1);

            return rateConfig.IsNullOrDead() ? -1 : rateConfig.Level;
        }
    }
}