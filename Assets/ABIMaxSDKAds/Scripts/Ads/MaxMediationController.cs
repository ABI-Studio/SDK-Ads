
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace SDK {
    public class MaxMediationController : AdsMediationController {
#if UNITY_AD_MAX
        public string m_MaxSdkKey = "";

        private string m_ApplicationKey;
        private bool m_IsWatchSuccess = false;

        public string m_RewardAdUnitID;
        public string m_InterstitialAdUnitID;
        public string m_BannerAdUnitID;

        private void Awake() {
        }
        private void Start() {

#if UNITY_ANDROID
            m_ApplicationKey = m_MaxSdkKey;
#endif
            Debug.Log("unity-script: MyAppStart Start called");

            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration => {
                // AppLovin SDK is initialized, configure and start loading ads.
                Debug.Log("MAX SDK Initialized");
                AdsManager.Instance.UpdateAdsMediation();
            };
            MaxSdk.SetSdkKey(m_MaxSdkKey);
            MaxSdk.InitializeSdk();
            //ShowBannerAds();
        }
        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo impressionData) {
            double revenue = impressionData.Revenue;
            ImpressionData impression = new ImpressionData {
                ad_platform = "AppLovin",
                ad_source = impressionData.NetworkName,
                ad_unit_name = impressionData.AdUnitIdentifier,
                ad_format = impressionData.AdFormat,
                ad_revenue = revenue,
                ad_currency = "USD"
            };
            ABIAnalyticsManager.Instance.TrackAdImpression(impression);
        }
        #region Interstitial
        public override void InitInterstitialAd(UnityAction adClosedCallback, UnityAction adLoadSuccessCallback, UnityAction adLoadFailedCallback, UnityAction adShowSuccessCallback, UnityAction adShowFailCallback) {
            base.InitInterstitialAd(adClosedCallback, adLoadSuccessCallback, adLoadFailedCallback, adShowSuccessCallback, adShowFailCallback);
            // Attach callbacks
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialAdShowSucceededEvent;

            // Load the first interstitial
            RequestInterstitialAd();
        }
        public override void RequestInterstitialAd() {
            base.RequestInterstitialAd();
            Debug.Log("Request MAX Interstitial");
            MaxSdk.LoadInterstitial(m_InterstitialAdUnitID);
        }
        public override void ShowInterstitialAd() {
            base.ShowInterstitialAd();
            Debug.Log("Show MAX Interstitial");
            MaxSdk.ShowInterstitial(m_InterstitialAdUnitID);
        }
        public override bool IsInterstitialLoaded() {
            return MaxSdk.IsInterstitialReady(m_InterstitialAdUnitID);
        }
        void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("Load MAX Interstitial Success");
            if (m_InterstitialAdLoadSuccessCallback != null) {
                m_InterstitialAdLoadSuccessCallback();
            }
        }
        void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo) {
            Debug.Log("Load MAX Interstitial Fail");
            if (m_InterstitialAdLoadFailCallback != null) {
                m_InterstitialAdLoadFailCallback();
            }
        }
        void InterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got InterstitialAdShowFailedEvent, code :  " + errorInfo.Code + ", description : " + errorInfo.Message);
            if (m_InterstitialAdShowFailCallback != null) {
                m_InterstitialAdShowFailCallback();
            }
        }
        void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("Interstitial dismissed");
            if (m_InterstitialAdCloseCallback != null) {
                m_InterstitialAdCloseCallback();
            }
        }
        void OnInterstitialAdShowSucceededEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got InterstitialAdShowSuccee");
            if (m_InterstitialAdShowSuccessCallback != null) {
                m_InterstitialAdShowSuccessCallback();
            }
        }
        #endregion

        #region Rewards Video
        public override void InitRewardVideoAd(UnityAction videoClosed, UnityAction videoLoadSuccess, UnityAction videoLoadFailed, UnityAction videoStart) {
            base.InitRewardVideoAd(videoClosed, videoLoadSuccess, videoLoadFailed, videoStart);

            Debug.Log("Init MAX RewardedVideoAd");
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += Rewarded_OnAdStartedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += Rewarded_OnAdShowFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += Rewarded_OnAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += Rewarded_OnAdRewardedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += Rewarded_OnAdClosedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += Rewarded_OnAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += Rewarded_OnAdLoadedFailEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            RequestRewardVideoAd();
        }


        public override void RequestRewardVideoAd() {
            base.RequestRewardVideoAd();
            Debug.Log("Request MAX RewardedVideoAd");
#if UNITY_EDITOR
            Rewarded_OnAdLoadedFailEvent("", null);
#else
        MaxSdk.LoadRewardedAd(m_RewardAdUnitID);
#endif
        }
        public override void ShowRewardVideoAd(UnityAction successCallback, UnityAction failedCallback) {
            base.ShowRewardVideoAd(successCallback, failedCallback);
#if UNITY_EDITOR
            m_IsWatchSuccess = false;
            Rewarded_OnAdRewardedEvent("", new MaxSdkBase.Reward(), null);
#else
        m_IsWatchSuccess = false;
        MaxSdk.ShowRewardedAd(m_RewardAdUnitID);
#endif
        }
        public override bool IsRewardVideoLoaded() {
#if UNITY_EDITOR
            return false;
#else
        return MaxSdk.IsRewardedAdReady(m_RewardAdUnitID);
#endif
        }

        /************* RewardedVideo Delegates *************/
        private void Rewarded_OnAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("RewardedVideoAd MAX Loaded Success");
            if (m_RewardedVideoLoadSuccessCallback != null) {
                m_RewardedVideoLoadSuccessCallback();
            }
        }
        private void Rewarded_OnAdLoadedFailEvent(string adUnitID, MaxSdkBase.ErrorInfo adError) {
            Debug.Log("RewardedVideoAd MAX Loaded Fail");
            if (m_RewardedVideoLoadFailedCallback != null) {
                m_RewardedVideoLoadFailedCallback();
            }
        }
        void Rewarded_OnAdRewardedEvent(string adUnitID, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo) {
#if !UNITY_EDITOR
        Debug.Log("unity-script: I got RewardedVideoAdRewardedEvent");
#endif
            m_IsWatchSuccess = true;
            if (Application.platform == RuntimePlatform.Android) {
                if (m_RewardedVideoEarnSuccessCallback != null) {
                    Debug.Log("Watch video Success Callback!");
                    m_RewardedVideoEarnSuccessCallback();
                    m_RewardedVideoEarnSuccessCallback = null;
                }
            } else if (Application.platform == RuntimePlatform.IPhonePlayer) {
                if (m_RewardedVideoEarnSuccessCallback != null) {
                    Debug.Log("Watch video Success Callback!");
                    EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
                    m_RewardedVideoEarnSuccessCallback = null;
                }
            }
        }
        void Rewarded_OnAdClosedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got RewardedVideoAdClosedEvent");
            if (m_RewardedVideoEarnSuccessCallback != null && m_IsWatchSuccess) {
                EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
                m_RewardedVideoEarnSuccessCallback = null;
            }
            if (m_RewardedVideoCloseCallback != null) {
                m_RewardedVideoCloseCallback();
            }
        }
        void Rewarded_OnAdStartedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got RewardedVideoAdStartedEvent");
            if (m_RewardedVideoShowStartCallback != null) {
                m_RewardedVideoShowStartCallback();
            }
        }
        void RewardedVideoAdEndedEvent() {
            Debug.Log("unity-script: I got RewardedVideoAdEndedEvent");
            m_IsWatchSuccess = true;
        }
        void Rewarded_OnAdShowFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got RewardedVideoAdShowFailedEvent, code :  " + errorInfo.Code + ", description : " + errorInfo.Message);
            if (m_RewardedVideoLoadFailedCallback != null) {
                m_RewardedVideoLoadFailedCallback();
            }
        }
        void Rewarded_OnAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got RewardedVideoAdClickedEvent");
        }
        #endregion

        #region Banner
        public override void InitBannerAds(UnityAction bannerLoadedSuccessCallback, UnityAction bannerAdLoadedFailCallback) {
            base.InitBannerAds(bannerLoadedSuccessCallback, bannerAdLoadedFailCallback);
            Debug.Log("Banner MAX Init");
            MaxSdk.CreateBanner(m_BannerAdUnitID, MaxSdkBase.BannerPosition.BottomCenter);
            MaxSdk.SetBannerBackgroundColor(m_BannerAdUnitID, Color.black);
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += BannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        }
        public override void ShowBannerAds() {
            base.ShowBannerAds();
            MaxSdk.ShowBanner(m_BannerAdUnitID);
        }
        void BannerAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got BannerAdLoadedEvent");
            ShowBannerAds();
        }

        
        #endregion
#endif

        public override AdsMediationType GetAdsMediationType() {
            return AdsMediationType.MAX;
        }
    }

}
