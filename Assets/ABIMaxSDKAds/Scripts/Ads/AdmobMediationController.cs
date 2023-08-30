using GoogleMobileAds.Api;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using static MaxSdkCallbacks;
//using GoogleMobileAdsMediationTestSuite.Api;
namespace SDK {
    public class AdmobMediationController : AdsMediationController {
        public string m_AndroidAdmobID_AppId;
        public AdsID m_AdmobID_Intertitials;
        public AdsID m_AdmobID_RewardVideo;
        public AdsID m_AdmobID_Banner;
        public AdPosition m_BannerPosition;

        private InterstitialAd m_InterstitialAds;
        private RewardedAd m_RewardVideoAds;
        private BannerView bannerView;
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
        }
        private void Start() {
        }
        
        #region Banner Ads
        public override void InitBannerAds(UnityAction bannerLoadedCallback, UnityAction bannerAdLoadedFailCallback) {
            base.InitBannerAds(bannerLoadedCallback, bannerAdLoadedFailCallback);
            RequestBannerAds();
        }
        public void CreateBannerView() {
            Debug.Log("Creating banner view");
            if (bannerView != null) {
                DestroyBannerAd();
            }
            string adUnitId = GetBannerID();
            // Create a 320x50 banner at top of the screen
            bannerView = new BannerView(adUnitId, AdSize.Banner, m_BannerPosition);
            RegisterBannerEvents(bannerView);
        }
        public override void RequestBannerAds() {
            base.RequestBannerAds();

            if (bannerView == null) {
                CreateBannerView();
            }
            AdRequest adRequest = new AdRequest();
            adRequest.Keywords.Add("unity-admob-sample");

            // Load the banner with the request.
            bannerView.LoadAd(adRequest);
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
            bannerView.Show();
        }
        public override void HideBannerAds() {
            base.HideBannerAds();
            bannerView.Hide();
        }
        public void OnAdBannerLoaded() {
            Debug.Log("HandleAdLoaded event received");
            m_AdmobID_Banner.Refresh();
        }
        public void OnAdBannerFailedToLoad(LoadAdError args) {
            Debug.Log("AdmobBanner Fail: " + args.GetMessage());
            m_AdmobID_Banner.ChangeID();
        }
        public void OnAdBannerOpened() {
            Debug.Log("AdmobBanner Opened");
        }
        public void OnAdBannerClosed() {
            Debug.Log("AdmobBanner Closed");
        }
        private void OnAdBannerPaid(AdValue adValue) {
        }
        /// <summary>
        /// Destroys the ad.
        /// </summary>
        public void DestroyBannerAd() {
            if (bannerView != null) {
                Debug.Log("Destroying banner ad.");
                bannerView.Destroy();
                bannerView = null;
            }
        }
        public string GetBannerID() {
            return m_AdmobID_Banner.ID;
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
        public override bool IsInterstitialLoaded() {
            if (m_InterstitialAds != null) {
                return m_InterstitialAds.CanShowAd();
            }
            return false;
        }
        public override void ShowInterstitialAd() {
            base.ShowInterstitialAd();
            if (m_InterstitialAds.CanShowAd()) {
                m_InterstitialAds.Show();
            }
        }
        private void OnCloseInterstitialAd() {
            if (m_InterstitialAdCloseCallback != null) {
                m_InterstitialAdCloseCallback();
            }
            Debug.Log("Close Interstitial");
        }
        private void OnAdInterstitialSuccessToLoad() {
            if (m_InterstitialAdLoadSuccessCallback != null) {
                m_InterstitialAdLoadSuccessCallback();
            }
            m_AdmobID_Intertitials.Refresh();
            Debug.Log("Load Interstitial success");
        }
        private void OnAdInterstitialFailedToLoad() {
            if (m_InterstitialAdLoadFailCallback != null) {
                m_InterstitialAdLoadFailCallback();
            }
            m_AdmobID_Intertitials.ChangeID();
            Debug.Log("Load Interstitial failed Admob");
        }
        private void OnAdInterstitialOpening() {
            if (m_InterstitialAdShowSuccessCallback != null) {
                m_InterstitialAdShowSuccessCallback();
            }
        }
        private void OnAdInterstitialFailToShow(AdError e) {
            if (m_InterstitialAdShowFailCallback != null) {
                m_InterstitialAdShowFailCallback();
            }
        }
        public void DestroyInterstitialAd() {
            if (m_InterstitialAds != null) {
                Debug.Log("Destroying interstitial ad.");
                m_InterstitialAds.Destroy();
                m_InterstitialAds = null;
            }
        }
        public string GetInterstitialAdUnit() {
            return m_AdmobID_Intertitials.ID;
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
                if (m_RewardedVideoLoadFailedCallback != null) {
                    m_RewardedVideoLoadFailedCallback();
                }
                m_AdmobID_RewardVideo.ChangeID();
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
            if (IsRewardVideoLoaded()) {
                Debug.Log("RewardedVideoAd ADMOB Show");
                m_IsWatchSuccess = false;
                m_RewardVideoAds.Show((Reward reward) => {
                    OnRewardBasedVideoRewarded();
                });
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
            if (m_RewardedVideoLoadSuccessCallback != null) {
                m_RewardedVideoLoadSuccessCallback();
            }
            m_AdmobID_RewardVideo.Refresh();
        }
        private void OnRewardBasedVideoFailedToLoad() {
            Debug.Log("RewardedVideoAd ADMOB Load Fail");
            if (m_RewardedVideoLoadFailedCallback != null) {
                m_RewardedVideoLoadFailedCallback();
            }
            m_AdmobID_RewardVideo.ChangeID();
        }
        public void OnRewardedAdFailedToShow(AdError args) {
            Debug.Log("RewardedVideoAd ADMOB Show Fail " + args.GetMessage());
            if (m_RewardedVideoShowFailCallback != null) {
                m_RewardedVideoShowFailCallback();
            }
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
            return m_AdmobID_RewardVideo.ID;
        }
        #endregion

        private void OnApplicationQuit() {
            if (m_InterstitialAds != null) {
                m_InterstitialAds.Destroy();
            }
        }
        public override AdsMediationType GetAdsMediationType() {
            return AdsMediationType.ADMOB;
        }
        public override bool IsActive(AdsType adsType) {
            if (!m_IsActive) return false;
            switch (adsType) {
                case AdsType.BANNER:
                    return m_AdmobID_Banner.IsActive();
                case AdsType.INTERSTITIAL:
                    return m_AdmobID_Intertitials.IsActive();
                case AdsType.REWARDED:
                    return m_AdmobID_RewardVideo.IsActive();
            }
            return false;
        }
    }
}
