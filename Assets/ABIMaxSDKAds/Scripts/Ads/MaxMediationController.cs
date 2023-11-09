
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace SDK {
    public class MaxMediationController : AdsMediationController {
#if UNITY_AD_MAX
        private bool m_IsWatchSuccess = false;
        public MaxAdSetup m_MaxAdConfig;
        private bool m_IsInited = false;

        private void Awake() {
        }
        private void Start() {
            
        }

        public override void Init()
        {
            base.Init();
            if (m_IsInited) return;
            m_IsInited = true;
            Debug.Log("unity-script: MyAppStart Start called");

            MaxSdkCallbacks.OnSdkInitializedEvent += sdkConfiguration => {
                // AppLovin SDK is initialized, configure and start loading ads.
                Debug.Log("MAX SDK Initialized");
                AdsManager.Instance.InitAdsType();
            };
            MaxSdk.SetSdkKey(m_MaxAdConfig.SDKKey);
            MaxSdk.InitializeSdk();
        }

        private void OnAdRevenuePaidEvent(AdsType adsType, string adUnitId, MaxSdkBase.AdInfo impressionData) {
            double revenue = impressionData.Revenue;
            ImpressionData impression = new ImpressionData {
                ad_platform = "AppLovin",
                ad_source = impressionData.NetworkName,
                ad_unit_name = impressionData.AdUnitIdentifier,
                ad_format = impressionData.AdFormat,
                ad_revenue = revenue,
                ad_currency = "USD",
                ad_type = adsType
            };
            AdRevenuePaidCallback?.Invoke(impression);
        }
        #region Interstitial
        public override void InitInterstitialAd(UnityAction adClosedCallback, UnityAction adLoadSuccessCallback, UnityAction adLoadFailedCallback, UnityAction adShowSuccessCallback, UnityAction adShowFailCallback) {
            base.InitInterstitialAd(adClosedCallback, adLoadSuccessCallback, adLoadFailedCallback, adShowSuccessCallback, adShowFailCallback);
            // Attach callbacks
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.INTERSTITIAL, adUnitID, adInfo);};
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialAdShowSucceededEvent;

            // Load the first interstitial
            RequestInterstitialAd();
        }
        public override void RequestInterstitialAd() {
            base.RequestInterstitialAd();
            Debug.Log("Request MAX Interstitial");
            MaxSdk.LoadInterstitial(m_MaxAdConfig.InterstitialAdUnitID);
        }
        public override void ShowInterstitialAd() {
            base.ShowInterstitialAd();
            Debug.Log("Show MAX Interstitial");
            MaxSdk.ShowInterstitial(m_MaxAdConfig.InterstitialAdUnitID);
        }
        public override bool IsInterstitialLoaded() {
            return MaxSdk.IsInterstitialReady(m_MaxAdConfig.InterstitialAdUnitID);
        }
        void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("Load MAX Interstitial Success");
            m_InterstitialAdLoadSuccessCallback?.Invoke();
        }
        void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo) {
            Debug.Log("Load MAX Interstitial Fail");
            m_InterstitialAdLoadFailCallback?.Invoke();
        }
        void InterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got InterstitialAdShowFailedEvent, code :  " + errorInfo.Code + ", description : " + errorInfo.Message);
            m_InterstitialAdShowFailCallback?.Invoke();
        }
        void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("Interstitial dismissed");
            m_InterstitialAdCloseCallback?.Invoke();
        }
        void OnInterstitialAdShowSucceededEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got InterstitialAdShowSuccee");
            m_InterstitialAdShowSuccessCallback?.Invoke();
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
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.REWARDED, adUnitID, adInfo);};
            RequestRewardVideoAd();
        }


        public override void RequestRewardVideoAd() {
            base.RequestRewardVideoAd();
            Debug.Log("Request MAX RewardedVideoAd");
#if UNITY_EDITOR
            Rewarded_OnAdLoadedFailEvent("", null);
#else
            MaxSdk.LoadRewardedAd(m_MaxAdConfig.RewardedAdUnitID);
#endif
        }
        public override void ShowRewardVideoAd(UnityAction successCallback, UnityAction failedCallback) {
            base.ShowRewardVideoAd(successCallback, failedCallback);
#if UNITY_EDITOR
            m_IsWatchSuccess = false;
            Rewarded_OnAdRewardedEvent("", new MaxSdkBase.Reward(), null);
#else
        m_IsWatchSuccess = false;
        MaxSdk.ShowRewardedAd(m_MaxAdConfig.RewardedAdUnitID);
#endif
        }
        public override bool IsRewardVideoLoaded() {
#if UNITY_EDITOR
            return false;
#else
            return MaxSdk.IsRewardedAdReady(m_MaxAdConfig.RewardedAdUnitID);
#endif
        }

        /************* RewardedVideo Delegates *************/
        private void Rewarded_OnAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("RewardedVideoAd MAX Loaded Success");
            m_RewardedVideoLoadSuccessCallback?.Invoke();
        }
        private void Rewarded_OnAdLoadedFailEvent(string adUnitID, MaxSdkBase.ErrorInfo adError) {
            Debug.Log("RewardedVideoAd MAX Loaded Fail");
            m_RewardedVideoLoadFailedCallback?.Invoke();
        }
        void Rewarded_OnAdRewardedEvent(string adUnitID, MaxSdkBase.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
#if !UNITY_EDITOR
        Debug.Log("unity-script: I got RewardedVideoAdRewardedEvent");
#endif
            m_IsWatchSuccess = true;
            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                {
                    if (m_RewardedVideoEarnSuccessCallback != null) {
                        Debug.Log("Watch video Success Callback!");
                        m_RewardedVideoEarnSuccessCallback();
                        m_RewardedVideoEarnSuccessCallback = null;
                    }

                    break;
                }
                case RuntimePlatform.IPhonePlayer:
                {
                    if (m_RewardedVideoEarnSuccessCallback != null) {
                        Debug.Log("Watch video Success Callback!");
                        EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
                        m_RewardedVideoEarnSuccessCallback = null;
                    }

                    break;
                }
            }
        }
        void Rewarded_OnAdClosedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got RewardedVideoAdClosedEvent");
            if (m_RewardedVideoEarnSuccessCallback != null && m_IsWatchSuccess) {
                EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
                m_RewardedVideoEarnSuccessCallback = null;
            }

            m_RewardedVideoCloseCallback?.Invoke();
        }
        void Rewarded_OnAdStartedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got RewardedVideoAdStartedEvent");
            m_RewardedVideoShowStartCallback?.Invoke();
        }
        void RewardedVideoAdEndedEvent() {
            Debug.Log("unity-script: I got RewardedVideoAdEndedEvent");
            m_IsWatchSuccess = true;
        }
        void Rewarded_OnAdShowFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got RewardedVideoAdShowFailedEvent, code :  " + errorInfo.Code + ", description : " + errorInfo.Message);
            m_RewardedVideoLoadFailedCallback?.Invoke();
        }
        void Rewarded_OnAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("unity-script: I got RewardedVideoAdClickedEvent");
        }
        #endregion

        #region Banner

        private bool m_IsBannerLoaded;
        public override void InitBannerAds(UnityAction bannerLoadedSuccessCallback, UnityAction bannerAdLoadedFailCallback, UnityAction bannerAdsCollapsedCallback, UnityAction bannerAdsExpandedCallback) {
            base.InitBannerAds(bannerLoadedSuccessCallback, bannerAdLoadedFailCallback, bannerAdsCollapsedCallback, bannerAdsExpandedCallback);
            Debug.Log("Banner MAX Init ID = " + m_MaxAdConfig.BannerAdUnitID);
            MaxSdk.CreateBanner(m_MaxAdConfig.BannerAdUnitID, MaxSdkBase.BannerPosition.BottomCenter);
            MaxSdk.SetBannerBackgroundColor(m_MaxAdConfig.BannerAdUnitID, Color.black);
            
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += BannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += BannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += BannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.BANNER, adUnitID, adInfo);};
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent += OnBannerAdCollapsedEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent += OnBannerAdExpandedEvent;
        }

        public override void ShowBannerAds() {
            base.ShowBannerAds();
            Debug.Log("MAX Mediation Banner Call Show");
            MaxSdk.ShowBanner(m_MaxAdConfig.BannerAdUnitID);
        }
        public override void HideBannerAds()
        {
            base.HideBannerAds();
            Debug.Log("MAX Mediation Banner Call Hide");
            MaxSdk.HideBanner(m_MaxAdConfig.BannerAdUnitID);
        }

        public override bool IsBannerLoaded()
        {
            return m_IsBannerLoaded;
        }

        private void BannerAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo) {
            Debug.Log("MAX Mediation Banner Loaded Success");
            m_BannerAdLoadedSuccessCallback?.Invoke();
            m_IsBannerLoaded = true;
        }
        private void BannerAdLoadFailedEvent(string arg1, MaxSdkBase.ErrorInfo arg2)
        {
            Debug.Log("MAX Mediation Banner Loaded Fail");
            m_BannerAdLoadedFailCallback?.Invoke();
            m_IsBannerLoaded = false;
        }
        private void BannerAdClickedEvent(string arg1, MaxSdkBase.AdInfo arg2)
        {
            Debug.Log("MAX Mediation Banner Clicked");
        }
        private void OnBannerAdCollapsedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("MAX Mediation Banner Collapsed");
            m_BannerAdsCollapsedCallback?.Invoke();
        }
        private void OnBannerAdExpandedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("MAX Mediation Banner Expanded");
            m_BannerAdsExpandedCallback?.Invoke();
        }
        #endregion
        
        #region MREC
        private bool m_IsMRecLoaded = false;
        public override void InitRMecAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback, UnityAction adClickedCallback, UnityAction adExpandedCallback, UnityAction adCollapsedCallback)
        {
            base.InitRMecAds(adLoadedCallback, adLoadFailedCallback, adClickedCallback, adExpandedCallback, adCollapsedCallback);
            Debug.Log("MAX Start Init MREC");
            MaxSdkCallbacks.MRec.OnAdLoadedEvent      += OnMRecAdLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent  += OnMRecAdLoadFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent     += OnMRecAdClickedEvent;
            MaxSdkCallbacks.MRec.OnAdExpandedEvent    += OnMRecAdExpandedEvent;
            MaxSdkCallbacks.MRec.OnAdCollapsedEvent   += OnMRecAdCollapsedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.MREC, adUnitID, adInfo);};
            MaxSdk.CreateMRec(m_MaxAdConfig.MrecAdUnitID, MaxSdkBase.AdViewPosition.BottomCenter);
        }
        public override bool IsMRecLoaded()
        {
            return m_IsMRecLoaded;
        }
        private void OnMRecAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("MAX Mediation MREC Loaded Success");
            m_IsMRecLoaded = true;
            m_MRecAdLoadedCallback?.Invoke();
        }
        private void OnMRecAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            Debug.Log("MAX Mediation MREC Loaded Fail");
            m_IsMRecLoaded = false;
            m_MRecAdLoadFailCallback?.Invoke();
        }
        private void OnMRecAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            m_MRecAdClickedCallback?.Invoke();
        }
        private void OnMRecAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            m_MRecAdExpandedCallback?.Invoke();
        }
        private void OnMRecAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            m_MRecAdCollapsedCallback?.Invoke();
        }
        public override void ShowMRecAds()
        {
            base.ShowMRecAds();
            MaxSdk.ShowMRec(m_MaxAdConfig.MrecAdUnitID);
        }
        public override void HideMRecAds()
        {
            base.HideMRecAds();
            MaxSdk.HideMRec(m_MaxAdConfig.MrecAdUnitID);
        }
        #endregion
        
        #region App Open Ads

        public override void InitAppOpenAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback, 
            UnityAction adClosedCallback, UnityAction adDisplayedCallback, UnityAction adFailedToDisplayCallback)
        {
            base.InitAppOpenAds(adLoadedCallback, adLoadFailedCallback, 
                adClosedCallback, adDisplayedCallback, adFailedToDisplayCallback);
            
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenAdLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenAdLoadFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAppOpenAdClickedEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += (adUnitID, adInfo) => { OnAdRevenuePaidEvent(AdsType.APP_OPEN, adUnitID, adInfo);};
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenAdHiddenEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenAdDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenAdDisplayFailedEvent;
            RequestAppOpenAds();
        }
        public override void ShowAppOpenAds()
        {
            base.ShowAppOpenAds();
            MaxSdk.ShowAppOpenAd(m_MaxAdConfig.AppOpenAdUnitID);
        }
        public override void RequestAppOpenAds()
        {
            MaxSdk.LoadAppOpenAd(m_MaxAdConfig.AppOpenAdUnitID);
        }
        public override bool IsAppOpenAdsLoaded()
        {
            return MaxSdk.IsAppOpenAdReady(m_MaxAdConfig.AppOpenAdUnitID);
        }
        private void OnAppOpenAdLoadedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("MAX Mediation App Open Ads Loaded Success");
            m_AppOpenAdLoadedCallback?.Invoke();
        }
        private void OnAppOpenAdLoadFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo)
        {
            Debug.Log("MAX Mediation App Open Ads Loaded Fail");
            m_AppOpenAdLoadFailedCallback?.Invoke();
        }
        private void OnAppOpenAdClickedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
        }
        private void OnAppOpenAdDisplayedEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("MAX Mediation App Open Ads Displayed");
            m_AppOpenAdDisplayedCallback?.Invoke();
        }
        private void OnAppOpenAdDisplayFailedEvent(string adUnitID, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("MAX Mediation App Open Ads Displayed Fail");
            m_AppOpenAdFailedToDisplayCallback?.Invoke();
        }
        private void OnAppOpenAdHiddenEvent(string adUnitID, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("MAX Mediation App Open Ads Hidden");
            m_AppOpenAdClosedCallback?.Invoke();
        }
        #endregion
#endif
        public override AdsMediationType GetAdsMediationType() {
            return AdsMediationType.MAX;
        }
    }

}
