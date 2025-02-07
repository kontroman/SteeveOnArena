using Devotion.SDK.Enums;
using Devotion.SDK.Markets.Confgs;
using System;
using UnityEngine;

namespace Devotion.SDK.Markets
{
    [Serializable]
    public class Markets
    {
        [SerializeField] private MarketType marketType;
        [SerializeField] private RemoteAdsSettings adsSettings;
        [SerializeField] private RateAppConfig rateAppConfig;

        public MarketType Market => marketType;
        public RemoteAdsSettings AdsSettings => adsSettings;
        public RateAppConfig RateAppConfig => rateAppConfig;
    }
}