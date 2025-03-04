using Devotion.SDK.Advertisment;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Devotion.SDK.Confgs
{
    [Serializable]
    public class RemoteAdsSettings
    {
        [SerializeField] private int _interstitialShowIntervalOnStart;
        [SerializeField] private int _rewardShowIntervalOnStart;
        [SerializeField] private int _showInterstitialInterval;

        [SerializeField] private List<InterstitialAdv> _interstitial;
        [SerializeField] private List<RewardAdv> _rewardVideos;

        public int ShowInterstitialInterval => _showInterstitialInterval;

        public List<InterstitialAdv> Interstitial => _interstitial;
        public List<RewardAdv> RewardVideos => _rewardVideos;
    }
}