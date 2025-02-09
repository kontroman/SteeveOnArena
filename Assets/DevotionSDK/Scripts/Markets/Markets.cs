using Devotion.SDK.Enums;
using Devotion.SDK.Markets.Confgs;
using System;
using UnityEngine;

namespace Devotion.SDK.Markets
{
    [Serializable]
    public class Markets
    {
        [SerializeField] private MarketType _marketType;
        [SerializeField] private RemoteAdsSettings _adsSettings;
        [SerializeField] private RateAppConfig _rateAppConfig;

        public MarketType Market => _marketType;
        public RemoteAdsSettings AdsSettings => _adsSettings;
        public RateAppConfig RateAppConfig => _rateAppConfig;
    }
}