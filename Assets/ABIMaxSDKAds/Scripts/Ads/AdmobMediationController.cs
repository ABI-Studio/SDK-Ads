using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using static MaxSdkCallbacks;
//using GoogleMobileAdsMediationTestSuite.Api;
namespace SDK {
    public class AdmobMediationController : AdsMediationController {
        public string m_AndroidAdmobID_AppId;
        public AdmobAdSetup m_AdmobAdSetup;
        public AdPosition m_BannerPosition;

        private InterstitialAd m_InterstitialAds;
        private RewardedAd m_RewardVideoAds;
        private BannerView m_BannerViewAds;
        private BannerView m_MRECAds;
        private AppOpenAd m_AppOpenAd;
        private bool m_IsWatchSuccess = false;

        public override void Init() {
            base.Init();
            // Initialize the Mobile Ads SDK.
            MobileAds.Initialize((initStatus) => {
                Dictionary<string, AdapterStatus> map = initStatus.getAdapterStatusMap();
                foreach (KeyValuePair<string, AdapterStatus> keyValuePair in map) {
                    string className = keyValuePair.Key;
                    AdapterStatus status = keyValuePair.Value;
                    switch (status.InitializationState) {
                        case AdapterState.NotReady:
                            // The adapter initialization did not complete.
                            MonoBehaviour.print("Adapter: " + className + " not ready.");
                            break;
                        case AdapterState.Ready:
                            // The adapter was successfully initialized.
                            MonoBehaviour.print("Adapter: " + className + " is initialized.");
                            break;
                    }
                }
            });
            RequestConfiguration requestConfiguration = new RequestConfiguration();
            requestConfiguration.TestDeviceIds.Add("8EC8C174AE81E71DF002C15B0B8458D9");
            MobileAds.SetRequestConfiguration(requestConfiguration);
        }
        private void Start() {
            
        }
        
        #region Banner Ads
        public override void InitBannerAds(UnityAction bannerLoadedCallback, UnityAction bannerAdLoadedFailCallback, UnityAction bannerAdsCollapsedCallback, UnityAction bannerAdsExpandedCallback) {
            base.InitBannerAds(bannerLoadedCallback, bannerAdLoadedFailCallback, bannerAdsCollapsedCallback, bannerAdsExpandedCallback);
            RequestBannerAds();
        }
        public void CreateBannerView() {
            Debug.Log("Creating banner view");
            if (m_BannerViewAds != null) {
                DestroyBannerAd();
            }
            string adUnitId = GetBannerID();
            // Create a 320x50 banner at top of the screen
            m_BannerViewAds = new BannerView(adUnitId, AdSize.Banner, m_BannerPosition);
            RegisterBannerEvents(m_BannerViewAds);
        }
        public override void RequestBannerAds() {
            base.RequestBannerAds();

            if (m_BannerViewAds == null) {
                CreateBannerView();
            }
            AdRequest adRequest = new AdRequest();
            adRequest.Keywords.Add("unity-admob-sample");

            // Load the banner with the request.
            m_BannerViewAds.LoadAd(adRequest);
        }
        private void RegisterBannerEvents(BannerView bannerView) {
            bannerView.OnBannerAdLoaded += OnAdBannerLoaded;
            bannerView.OnBannerAdLoadFailed += OnAdBannerFailedToLoad;
            bannerView.OnAdFullScreenContentOpened += OnAdBannerOpened;
            bannerView.OnAdFullScreenContentClosed += OnAdBannerClosed;
            bannerView.OnAdPaid += OnAdBannerPaid;
        }
        public override void ShowBannerAds() {
            base.ShowBannerAds();
            m_BannerViewAds.Show();
        }
        public override void HideBannerAds() {
            base.HideBannerAds();
            m_BannerViewAds.Hide();
        }
        public void OnAdBannerLoaded() {
            Debug.Log("HandleAdLoaded event received");
            m_AdmobAdSetup.BannerAdUnitID.Refresh();
            m_BannerAdLoadedSuccessCallback?.Invoke();
        }
        public void OnAdBannerFailedToLoad(LoadAdError args) {
            Debug.Log("AdmobBanner Fail: " + args.GetMessage());
            m_AdmobAdSetup.BannerAdUnitID.ChangeID();
            m_BannerAdLoadedFailCallback?.Invoke();
        }
        public void OnAdBannerOpened() {
            Debug.Log("AdmobBanner Opened");
            m_BannerAdsExpandedCallback?.Invoke();
        }
        public void OnAdBannerClosed() {
            Debug.Log("AdmobBanner Closed");
            m_BannerAdsCollapsedCallback?.Invoke();
        }
        private void OnAdBannerPaid(AdValue adValue) {
        }
        /// <summary>
        /// Destroys the ad.
        /// </summary>
        public void DestroyBannerAd() {
            if (m_BannerViewAds != null) {
                Debug.Log("Destroying banner ad.");
                m_BannerViewAds.Destroy();
                m_BannerViewAds = null;
            }
        }
        public string GetBannerID() {
            return m_AdmobAdSetup.BannerAdUnitID.ID;
        }
        #endregion

        #region Interstitial
        public override void InitInterstitialAd(UnityAction adClosedCallback, UnityAction adLoadSuccessCallback, UnityAction adLoadFailedCallback, UnityAction adShowSuccessCallback, UnityAction adShowFailCallback) {
            base.InitInterstitialAd(adClosedCallback, adLoadSuccessCallback, adLoadFailedCallback, adShowSuccessCallback, adShowFailCallback);
            RequestInterstitialAd();
        }
        public override void RequestInterstitialAd() {
            base.RequestInterstitialAd();
            Debug.Log("Request interstitial ads");

            if(m_InterstitialAds != null) {
                m_InterstitialAds.Destroy();
                m_InterstitialAds = null;
            }
            var adRequest = new AdRequest();
            adRequest.Keywords.Add("unity-admob-sample");

            string adUnitId = GetInterstitialAdUnit();
            InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) => {
                if (error != null || ad == null) {
                    Debug.LogError("interstitial ad failed to load an ad " + "with error : " + error);
                    OnAdInterstitialFailedToLoad();
                    return;
                }

                Debug.Log("Interstitial ad loaded with response : " + ad.GetResponseInfo());
                m_InterstitialAds = ad;
                RegisterInterstititalAd(ad);
                OnAdInterstitialSuccessToLoad();
            });
        }
        private void RegisterInterstititalAd(InterstitialAd interstitialAd) {
            interstitialAd.OnAdFullScreenContentClosed += OnCloseInterstitialAd;
            interstitialAd.OnAdFullScreenContentOpened += OnAdInterstitialOpening;
            interstitialAd.OnAdFullScreenContentFailed += OnAdInterstitialFailToShow;
        }
        public override bool IsInterstitialLoaded()
        {
            return m_InterstitialAds != null && m_InterstitialAds.CanShowAd();
        }
        public override void ShowInterstitialAd() {
            base.ShowInterstitialAd();
            if (m_InterstitialAds.CanShowAd()) {
                m_InterstitialAds.Show();
            }
        }
        private void OnCloseInterstitialAd() {
            m_InterstitialAdCloseCallback?.Invoke();
            Debug.Log("Close Interstitial");
        }
        private void OnAdInterstitialSuccessToLoad() {
            m_InterstitialAdLoadSuccessCallback?.Invoke();
            m_AdmobAdSetup.InterstitialAdUnitID.Refresh();
            Debug.Log("Load Interstitial success");
        }
        private void OnAdInterstitialFailedToLoad() {
            m_InterstitialAdLoadFailCallback?.Invoke();
            m_AdmobAdSetup.InterstitialAdUnitID.ChangeID();
            Debug.Log("Load Interstitial failed Admob");
        }
        private void OnAdInterstitialOpening()
        {
            m_InterstitialAdShowSuccessCallback?.Invoke();
        }
        private void OnAdInterstitialFailToShow(AdError e)
        {
            m_InterstitialAdShowFailCallback?.Invoke();
        }
        public void DestroyInterstitialAd() {
            if (m_InterstitialAds != null) {
                Debug.Log("Destroying interstitial ad.");
                m_InterstitialAds.Destroy();
                m_InterstitialAds = null;
            }
        }
        public string GetInterstitialAdUnit() {
            return m_AdmobAdSetup.InterstitialAdUnitID.ID;
        }
        #endregion

        #region Rewarded Ads
        public override void InitRewardVideoAd(UnityAction videoClosed, UnityAction videoLoadSuccess, UnityAction videoLoadFailed, UnityAction videoStart) {
            base.InitRewardVideoAd(videoClosed, videoLoadSuccess, videoLoadFailed, videoStart);
            Debug.Log("Init Reward Video");
        }

        public override void RequestRewardVideoAd() {
            base.RequestRewardVideoAd();
            if (m_RewardVideoAds != null) {
                DestroyRewardedAd();
            }

            string adUnitId = GetRewardedAdID();
            Debug.Log("RewardedVideoAd ADMOB Reloaded ID " + adUnitId);
            if (string.IsNullOrEmpty(adUnitId)) {
                m_RewardedVideoLoadFailedCallback?.Invoke();
                m_AdmobAdSetup.RewardedAdUnitID.ChangeID();
            }
            if (m_RewardVideoAds != null && m_RewardVideoAds.CanShowAd()) return;

            var adRequest = new AdRequest();

            RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) => {
                if (error != null || ad == null) {
                    Debug.LogError("Rewarded ad failed to load an ad with error : " + error);
                    OnRewardBasedVideoFailedToLoad();
                    return;
                }
                m_RewardVideoAds = ad;
                RegisterRewardAdEvent(ad);
                OnRewardBasedVideoLoaded();
            });
        }
        private void RegisterRewardAdEvent(RewardedAd rewardedAd) {
            rewardedAd.OnAdFullScreenContentOpened += OnRewardBasedVideoOpened;
            rewardedAd.OnAdFullScreenContentFailed += OnRewardedAdFailedToShow;
            rewardedAd.OnAdFullScreenContentClosed += OnRewardBasedVideoClosed;
            rewardedAd.OnAdPaid += OnAdRewardedAdPaid;
        }
        public override void ShowRewardVideoAd(UnityAction successCallback, UnityAction failedCallback) {
            base.ShowRewardVideoAd(successCallback, failedCallback);
            if (IsRewardVideoLoaded())
            {
                Debug.Log("RewardedVideoAd ADMOB Show");
                m_IsWatchSuccess = false;
                m_RewardVideoAds.Show((Reward reward) => { OnRewardBasedVideoRewarded(); });
            }
        }
        public override bool IsRewardVideoLoaded() {
#if UNITY_EDITOR
            return false;
#endif
            if (m_RewardVideoAds != null) {
                return m_RewardVideoAds.CanShowAd();
            }
            return false;
        }
        private void OnRewardBasedVideoClosed() {
            if (Application.platform == RuntimePlatform.IPhonePlayer) {
                if (m_IsWatchSuccess) {
                    if (m_RewardedVideoEarnSuccessCallback != null) {
                        EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
                    }
                }
            }
            if (m_RewardedVideoCloseCallback != null) {
                EventManager.AddEventNextFrame(m_RewardedVideoCloseCallback);
            }
        }
        private void OnRewardBasedVideoRewarded() {
            Debug.Log("RewardedVideoAd ADMOB Rewarded");
            m_IsWatchSuccess = true;
            if (Application.platform == RuntimePlatform.Android) {
                if (m_RewardedVideoEarnSuccessCallback != null) {
                    EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
                }
            }
        }
        private void OnRewardBasedVideoLoaded() {
            Debug.Log("RewardedVideoAd ADMOB Load Success");
            m_RewardedVideoLoadSuccessCallback?.Invoke();
            m_AdmobAdSetup.RewardedAdUnitID.Refresh();
        }
        private void OnRewardBasedVideoFailedToLoad() {
            Debug.Log("RewardedVideoAd ADMOB Load Fail");
            m_RewardedVideoLoadFailedCallback?.Invoke();
            m_AdmobAdSetup.RewardedAdUnitID.ChangeID();
        }
        public void OnRewardedAdFailedToShow(AdError args) {
            Debug.Log("RewardedVideoAd ADMOB Show Fail " + args.GetMessage());
            m_RewardedVideoShowFailCallback?.Invoke();
        }
        private void OnRewardBasedVideoOpened() {
            Debug.Log("Opened video success");
        }
        public void DestroyRewardedAd() {
            if (m_RewardVideoAds != null) {
                Debug.Log("Destroying rewarded ad.");
                m_RewardVideoAds.Destroy();
                m_RewardVideoAds = null;
            }
        }

        private void OnAdRewardedAdPaid(AdValue adValue) {
        }
        public string GetRewardedAdID() {
            return m_AdmobAdSetup.RewardedAdUnitID.ID;
        }
        #endregion
        
        #region MREC Ads

        public override void InitRMecAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback, UnityAction adClickedCallback,
            UnityAction adExpandedCallback, UnityAction adCollapsedCallback)
        {
            base.InitRMecAds(adLoadedCallback, adLoadFailedCallback, adClickedCallback, adExpandedCallback, adCollapsedCallback);
            RequestMRECAds();
        }
        public void CreateMRECAdsView() {
            Debug.Log("Creating MREC view");
            if (m_MRECAds != null) {
                DestroyMRECAds();
            }
            string adUnitId = GetBannerID();
            // Create a 320x50 banner at top of the screen
            m_MRECAds = new BannerView(adUnitId, AdSize.MediumRectangle, m_BannerPosition);
            RegisterMRECAdsEvents(m_MRECAds);
        }
        public void RequestMRECAds() {

            if (m_MRECAds == null) {
                CreateMRECAdsView();
            }
            AdRequest adRequest = new AdRequest();
            adRequest.Keywords.Add("unity-admob-sample");

            // Load the banner with the request.
            m_MRECAds.LoadAd(adRequest);
        }
        private void RegisterMRECAdsEvents(BannerView bannerView) {
            m_MRECAds.OnBannerAdLoaded += MRECAdsOnOnBannerAdLoaded;
            m_MRECAds.OnBannerAdLoadFailed += MRECAdsOnOnBannerAdLoadFailed;
            m_MRECAds.OnAdFullScreenContentOpened += MRECAdsOnOnAdFullScreenContentOpened;
            m_MRECAds.OnAdFullScreenContentClosed += MRECAdsOnOnAdFullScreenContentClosed;
            m_MRECAds.OnAdPaid += MRECAdsOnOnAdPaid;
        }

        public override void ShowMRecAds()
        {
            base.ShowMRecAds();
            m_MRECAds.Show();
        }

        public override void HideMRecAds()
        {
            base.HideMRecAds();
            m_MRECAds.Hide();
        }

        private void MRECAdsOnOnBannerAdLoaded()
        {
            Debug.Log("Admob MREC Ads Loaded");
            m_MRecAdLoadedCallback?.Invoke();
        }
        private void MRECAdsOnOnAdPaid(AdValue obj)
        {
            
        }
        private void MRECAdsOnOnAdFullScreenContentClosed()
        {
            Debug.Log("Admob MREC Ads Closed");
            m_MRecAdCollapsedCallback?.Invoke();
        }
        private void MRECAdsOnOnAdFullScreenContentOpened()
        {
            Debug.Log("Admob MREC Ads Opened");
            m_MRecAdExpandedCallback?.Invoke();
        }
        private void MRECAdsOnOnBannerAdLoadFailed(LoadAdError obj)
        {
            Debug.Log("Admob MREC Ads Failed to load the ad. (reason: {0})" + obj.GetMessage());
            m_MRecAdLoadFailCallback?.Invoke();
        }

        private void DestroyMRECAds()
        {
            if (m_MRECAds != null) {
                Debug.Log("Destroying MREC Ad.");
                m_MRECAds.Destroy();
                m_MRECAds = null;
            }
        }
        #endregion
        
        #region App Open Ads
        public override void InitAppOpenAds(UnityAction adLoadedCallback, UnityAction adLoadFailedCallback, UnityAction adClosedCallback,
            UnityAction adDisplayedCallback, UnityAction adFailedToDisplayCallback)
        {
            Debug.Log(("Init Admob App Open Ads"));
            base.InitAppOpenAds(adLoadedCallback, adLoadFailedCallback, adClosedCallback, adDisplayedCallback, adFailedToDisplayCallback);
            RequestAppOpenAds();
        }

        public override void RequestAppOpenAds()
        {
            base.RequestAppOpenAds();
            Debug.Log("Request Admob App Open Ads");
            if (m_AppOpenAd != null) {
                m_AppOpenAd.Destroy();
                m_AppOpenAd = null;
            }

            AdRequest request = new AdRequest();

            // Load an app open ad for portrait orientation
            AppOpenAd.Load(m_AdmobAdSetup.AppOpenAdUnitID.ID, request, ((appOpenAd, error) => {
                if (error != null) {
                    // Handle the error.
                    OnAppOpenAdFailedToLoad(error);
                    return;
                }
                OnAppOpenAdLoadedSuccess(appOpenAd);
            }));
        }

        public override void ShowAppOpenAds()
        {
            base.ShowAppOpenAds();
            if (m_AppOpenAd != null && m_AppOpenAd.CanShowAd()) {
                m_AppOpenAd.Show();
            }
        }

        private void RegisterAppOpenAdEventHandlers(AppOpenAd ad) {
            ad.OnAdFullScreenContentClosed += OnAppOpenAdDidDismissFullScreenContent;
            ad.OnAdFullScreenContentFailed += OnAppOpenAdFailedToPresentFullScreenContent;
            ad.OnAdFullScreenContentOpened += OnAppOpenAdDidPresentFullScreenContent;
            ad.OnAdImpressionRecorded += OnAppOpenAdDidRecordImpression;
            ad.OnAdPaid += OnAppOpenAppPaidEvent;
        }
        public override bool IsAppOpenAdsLoaded()
        {
            return m_AppOpenAd != null && m_AppOpenAd.CanShowAd();
        }
        #region App Open Ads Events

        private void OnAppOpenAdLoadedSuccess(AppOpenAd appOpenAd)
        {
            Debug.Log("Admob AppOpenAds Loaded");
            // App open ad is loaded.
            m_AppOpenAd = appOpenAd;
            RegisterAppOpenAdEventHandlers(appOpenAd);
            m_AppOpenAdLoadedCallback?.Invoke();
        }

        private void OnAppOpenAdFailedToLoad(LoadAdError error)
        {
            Debug.LogFormat("Admob AppOpenAd Failed to load the ad. (reason: {0})", error.GetMessage());
            m_AppOpenAdLoadFailedCallback?.Invoke();
            m_AdmobAdSetup.AppOpenAdUnitID.ChangeID();
        }
        private void OnAppOpenAdDidDismissFullScreenContent() {
            Debug.Log("Admob AppOpenAds Dismissed");
            m_AppOpenAd = null;
            m_AppOpenAdClosedCallback?.Invoke();
        }

        private void OnAppOpenAdFailedToPresentFullScreenContent(AdError args) {
            Debug.LogFormat("Admob AppOpenAd Failed to present the ad (reason: {0})", args.GetMessage());
            m_AppOpenAd = null;
            m_AppOpenAdFailedToDisplayCallback?.Invoke();
        }

        private void OnAppOpenAdDidPresentFullScreenContent() {
            Debug.Log("Admob AppOpenAds opened");
            m_AppOpenAdDisplayedCallback?.Invoke();
        }

        private void OnAppOpenAdDidRecordImpression() {
            Debug.Log("Admob AppOpenAds Recorded Impression");
        }

        private void OnAppOpenAppPaidEvent(AdValue args) {
            Debug.LogFormat("Received paid event. (currency: {0}, value: {1}",
                    args.CurrencyCode, args.Value);
        }
        #endregion
        #endregion
        private void OnApplicationQuit()
        {
            m_InterstitialAds?.Destroy();
        }
        public override AdsMediationType GetAdsMediationType() {
            return AdsMediationType.ADMOB;
        }
        public override bool IsActiveAdsType(AdsType adsType)
        {
            if (!m_IsActive) return false;
            return adsType switch
            {
                AdsType.BANNER => m_AdmobAdSetup.BannerAdUnitID.IsActive(),
                AdsType.INTERSTITIAL => m_AdmobAdSetup.InterstitialAdUnitID.IsActive(),
                AdsType.REWARDED => m_AdmobAdSetup.RewardedAdUnitID.IsActive(),
                AdsType.MREC => m_AdmobAdSetup.MrecAdUnitID.IsActive(),
                AdsType.APP_OPEN => m_AdmobAdSetup.AppOpenAdUnitID.IsActive(),
                _ => false
            };
        }
    }
    [Serializable]
    public class AdScheduleUnitID {
#if UNITY_ANDROID
        public List<string> AndroidID = new List<string>();
#elif UNITY_IOS
        public List<string> IosID = new List<string>();
#endif
        
        private int currentID;
        public void ChangeID() {
            currentID++;
            if (currentID >= CurrentPlatformID.Count) {
                currentID = 0;
            }
        }
        public void Refresh() {
            currentID = 0;
        }
        public string ID {
            get
            {
                return CurrentPlatformID.Count == 0 ? "" : CurrentPlatformID[currentID];
            }
        }
        public List<string> CurrentPlatformID
        {
            get
            {
#if UNITY_ANDROID
                return AndroidID;
#elif UNITY_IOS
                return IosID;
#else
                return null;
#endif
            }
            set
            {
#if UNITY_ANDROID
                AndroidID = value;
#elif UNITY_IOS
                IosID = value;
#endif
                
            }
        }

        public bool IsActive() {
            return CurrentPlatformID.Count > 0;
        }
    }
}
