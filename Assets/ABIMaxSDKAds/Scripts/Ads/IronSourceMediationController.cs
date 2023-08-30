
using SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class IronSourceMediationController : AdsMediationController {
#if UNITY_AD_IRONSOURCE
#if UNITY_ANDROID
    public string Android_Key;
    public string IOS_Key;

    private bool m_IsWatchSuccess = false;
    public string AppKey {
        get {
#if UNITY_ANDROID
            return Android_Key;
#elif UNITY_IPHONE
            return IOS_Key;
#else
            return Android_Key;
#endif

        }
    }
    private void Awake() {
    }
    private void Start() {

     
        Debug.Log("unity-script: MyAppStart Start called");

        //IronSource tracking sdk
        //IronSource.Agent.reportAppStarted();

        //Dynamic config example
        IronSourceConfig.Instance.setClientSideCallbacks(true);

        string id = IronSource.Agent.getAdvertiserId();
        Debug.Log("unity-script: IronSource.Agent.getAdvertiserId : " + id);

        Debug.Log("unity-script: IronSource.Agent.validateIntegration");
        IronSource.Agent.validateIntegration();

        Debug.Log("unity-script: unity version" + IronSource.unityVersion());

        // SDK init
        Debug.Log("unity-script: IronSource.Agent.init");
        string uniqueUserID = SystemInfo.deviceUniqueIdentifier;
        IronSource.Agent.setUserId(uniqueUserID);
        IronSource.Agent.setConsent(true);
        //IronSource.Agent.init(m_ApplicationKey);

        //IronSource.Agent.initISDemandOnly(m_ApplicationKey, IronSourceAdUnits.REWARDED_VIDEO, IronSourceAdUnits.INTERSTITIAL);


        IronSource.Agent.init(AppKey, IronSourceAdUnits.REWARDED_VIDEO, IronSourceAdUnits.INTERSTITIAL);

        // Add Interstitial Events
        IronSourceEvents.onInterstitialAdReadyEvent += OnInterstitialAdReadyEvent;
        IronSourceEvents.onInterstitialAdLoadFailedEvent += OnInterstitialAdLoadFailedEvent;
        IronSourceEvents.onInterstitialAdShowSucceededEvent += OnInterstitialAdShowSucceededEvent;
        IronSourceEvents.onInterstitialAdShowFailedEvent += InterstitialAdShowFailedEvent;
        IronSourceEvents.onInterstitialAdClickedEvent += InterstitialAdClickedEvent;
        IronSourceEvents.onInterstitialAdOpenedEvent += InterstitialAdOpenedEvent;
        IronSourceEvents.onInterstitialAdClosedEvent += InterstitialAdClosedEvent;

        //Add Rewarded Video Events
        IronSourceEvents.onRewardedVideoAdOpenedEvent += RewardedVideoAdOpenedEvent;
        IronSourceEvents.onRewardedVideoAdClosedEvent += RewardedVideoAdClosedEvent;
        IronSourceEvents.onRewardedVideoAvailabilityChangedEvent += RewardedVideoAvailabilityChangedEvent;
        IronSourceEvents.onRewardedVideoAdStartedEvent += RewardedVideoAdStartedEvent;
        IronSourceEvents.onRewardedVideoAdEndedEvent += RewardedVideoAdEndedEvent;
        IronSourceEvents.onRewardedVideoAdRewardedEvent += RewardedVideoAdRewardedEvent;
        IronSourceEvents.onRewardedVideoAdShowFailedEvent += RewardedVideoAdShowFailedEvent;
        IronSourceEvents.onRewardedVideoAdClickedEvent += RewardedVideoAdClickedEvent;

        IronSourceEvents.onBannerAdLoadedEvent += IronSourceEvents_onBannerAdLoadedEvent;
        IronSourceEvents.onBannerAdLoadFailedEvent += IronSourceEvents_onBannerAdLoadFailedEvent;


        IronSourceEvents.onSdkInitializationCompletedEvent += IronSourceEvents_onSdkInitializationCompletedEvent;
        IronSourceEvents.onImpressionDataReadyEvent += IronSourceEvents_onImpressionSuccessEvent;
        // Load Banner example
        //IronSource.Agent.loadBanner (IronSourceBannerSize.BANNER, IronSourceBannerPosition.BOTTOM);
        IronSource.Agent.loadInterstitial();
    }

    private void IronSourceEvents_onSdkInitializationCompletedEvent() {
        Debug.Log("Init Iron Success");
    }

    private void IronSourceEvents_onImpressionSuccessEvent(IronSourceImpressionData impressionData) {
        if (impressionData != null) {
            double revenue = (double)impressionData.revenue;
            ImpressionData impression = new ImpressionData {
                ad_platform = "ironSource",
                ad_source = impressionData.adNetwork,
                ad_unit_name = impressionData.instanceName,
                ad_format = impressionData.adUnit,
                ad_revenue = revenue,
                ad_currency = "USD"
            };
            ABIAnalyticsManager.Instance.TrackAdImpression(impression);
        }

    }

    #region InterstitialAd
    public override void InitInterstitialAd(UnityAction adClosedCallback, UnityAction adLoadSuccessCallback, UnityAction adLoadFailedCallback, UnityAction adShowSuccessCallback, UnityAction adShowFailCallback) {
        base.InitInterstitialAd(adClosedCallback, adLoadSuccessCallback, adLoadFailedCallback, adShowSuccessCallback, adShowFailCallback);
        Debug.Log("Init ironsource Interstitial");

        IronSource.Agent.init(AppKey, IronSourceAdUnits.INTERSTITIAL);
    }
    public override void RequestInterstitialAd() {
        base.RequestInterstitialAd();
        Debug.Log("Request ironsource Interstitial");
        IronSource.Agent.loadInterstitial();
    }
    public override void ShowInterstitialAd() {
        base.ShowInterstitialAd();
        Debug.Log("Show Iron source interstitial");
        IronSource.Agent.showInterstitial();
    }
    public override bool IsInterstitialLoaded() {
        return IronSource.Agent.isInterstitialReady();
    }
    void OnInterstitialAdReadyEvent() {
        Debug.Log("Load Iron Interstitial Success");
        if (m_InterstitialAdLoadSuccessCallback != null) {
            m_InterstitialAdLoadSuccessCallback();
        }
    }

    void OnInterstitialAdLoadFailedEvent(IronSourceError error) {
        Debug.Log("Load Iron Interstitial Fail");
        if (m_InterstitialAdLoadFailCallback != null) {
            m_InterstitialAdLoadFailCallback();
        }
    }
    void OnInterstitialAdShowSucceededEvent() {
        Debug.Log("unity-script: I got InterstitialAdShowSuccee");
        if (m_InterstitialAdShowSuccessCallback != null) {
            m_InterstitialAdShowSuccessCallback();
        }
    }

    void InterstitialAdShowFailedEvent(IronSourceError error) {
        Debug.Log("unity-script: I got InterstitialAdShowFailedEvent, code :  " + error.getCode() + ", description : " + error.getDescription());
        if (m_InterstitialAdShowFailCallback != null) {
            m_InterstitialAdShowFailCallback();
        }
    }

    void InterstitialAdClickedEvent() {
        Debug.Log("unity-script: I got InterstitialAdClickedEvent");
    }

    void InterstitialAdOpenedEvent() {
        Debug.Log("unity-script: I got InterstitialAdOpenedEvent");
    }

    void InterstitialAdClosedEvent() {
        Debug.Log("unity-script: I got InterstitialAdClosedEvent");
        if (m_InterstitialAdCloseCallback != null) {
            m_InterstitialAdCloseCallback();
        }
    }
    #endregion InterstitialAd

    #region RewardAd
    public override void InitRewardVideoAd(UnityAction videoClosed, UnityAction videoLoadSuccess, UnityAction videoLoadFailed, UnityAction videoStart) {
        base.InitRewardVideoAd(videoClosed, videoLoadSuccess, videoLoadFailed, videoStart);
        Debug.Log("Init ironsource video");
        IronSource.Agent.init(AppKey, IronSourceAdUnits.REWARDED_VIDEO);
    }
    public override void RequestRewardVideoAd() {
        base.RequestRewardVideoAd();
        Debug.Log("Request ironsource Video");
        IronSource.Agent.loadRewardedVideo();
    }
    public override void ShowRewardVideoAd(UnityAction successCallback, UnityAction failedCallback) {
        base.ShowRewardVideoAd(successCallback, failedCallback);
#if !UNITY_EDITOR
        m_IsWatchSuccess = false;
        IronSource.Agent.showRewardedVideo();
#else
        m_IsWatchSuccess = false;
        RewardedVideoAdRewardedEvent(null);
#endif
    }
    public override bool IsRewardVideoLoaded() {
#if !UNITY_EDITOR
        return IronSource.Agent.isRewardedVideoAvailable();
#else
        return true;
#endif
    }

    /************* RewardedVideo Delegates *************/
    void RewardedVideoAdOpenedEvent() {
        Debug.Log("unity-script: I got RewardedVideoAdOpenedEvent");
    }
    void RewardedVideoAdRewardedEvent(IronSourcePlacement ssp) {
#if !UNITY_EDITOR
        Debug.Log("unity-script: I got RewardedVideoAdRewardedEvent, amount = " + ssp.getPlacementName() + " name = " + ssp.getRewardName()); 
#endif
        m_IsWatchSuccess = true;
        if (Application.platform == RuntimePlatform.Android) {
            if (m_RewardedVideoEarnSuccessCallback != null) {
                Debug.Log("Watch video Success Callback!");
                EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
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
    private void RewardedVideoAvailabilityChangedEvent(bool isAvailable) {
        Debug.Log("unity-script: I got RewardedVideoAdsAvailable");
        if (isAvailable) {
            Debug.Log("RewardedVideoAd Iron Loaded Success");
            if (m_RewardedVideoLoadSuccessCallback != null) {
                m_RewardedVideoLoadSuccessCallback();
            }
        } else {
            Debug.Log("RewardedVideoAd Iron Loaded Fail");
            if (m_RewardedVideoLoadFailedCallback != null) {
                m_RewardedVideoLoadFailedCallback();
            }
        }
    }

    void RewardedVideoAdClosedEvent() {
        Debug.Log("unity-script: I got RewardedVideoAdClosedEvent");
        if (m_RewardedVideoEarnSuccessCallback != null && m_IsWatchSuccess) {
            Debug.Log("Do Callback Success");
            EventManager.AddEventNextFrame(m_RewardedVideoEarnSuccessCallback);
            m_RewardedVideoEarnSuccessCallback = null;
        } else {
            Debug.Log("Don't have any callback");
        }
        if (m_RewardedVideoCloseCallback != null) {
            m_RewardedVideoCloseCallback();
        }
    }
    void RewardedVideoAdStartedEvent() {
        Debug.Log("unity-script: I got RewardedVideoAdStartedEvent");
        if (m_RewardedVideoShowStartCallback != null) {
            m_RewardedVideoShowStartCallback();
        }
    }
    void RewardedVideoAdEndedEvent() {
        Debug.Log("unity-script: I got RewardedVideoAdEndedEvent");
        m_IsWatchSuccess = true;
    }
    void RewardedVideoAdShowFailedEvent(IronSourceError error) {
        Debug.Log("unity-script: I got RewardedVideoAdShowFailedEvent, code :  " + error.getCode() + ", description : " + error.getDescription());
        if (m_RewardedVideoLoadFailedCallback != null) {
            m_RewardedVideoLoadFailedCallback();
        }
    }
    void RewardedVideoAdClickedEvent(IronSourcePlacement ssp) {
        Debug.Log("unity-script: I got RewardedVideoAdClickedEvent, name = " + ssp.getRewardName());
    }

    #endregion RewardAd

    #region Banner
    bool isBannerLoaded = false;
    public override void InitBannerAds(UnityAction bannerLoadedCallback, UnityAction bannerAdLoadedFailCallback) {
        base.InitBannerAds(bannerLoadedCallback, bannerAdLoadedFailCallback);
        IronSource.Agent.init(AppKey, IronSourceAdUnits.BANNER);
    }
    public override void RequestBannerAds() {
        base.RequestBannerAds();
        isBannerLoaded = false;
        IronSource.Agent.loadBanner(IronSourceBannerSize.BANNER, IronSourceBannerPosition.BOTTOM);
    }
    public override void ShowBannerAds() {
        base.ShowBannerAds();
        IronSource.Agent.displayBanner();
    }
    public override void HideBannerAds() {
        base.HideBannerAds();
        IronSource.Agent.hideBanner();
    }
    public override void DestroyBannerAds() {
        base.DestroyBannerAds();
        IronSource.Agent.destroyBanner();
    }
    private void IronSourceEvents_onBannerAdLoadFailedEvent(IronSourceError obj) {
        Debug.Log("Ironsource Banner Load Fail");
        isBannerLoaded = false;
    }

    private void IronSourceEvents_onBannerAdLoadedEvent() {
        Debug.Log("Ironsource Banner Load Success");
        isBannerLoaded = true;
        if (m_BannerAdLoadedSuccessCallback != null) {
            m_BannerAdLoadedSuccessCallback();
        }
    }
    public override bool IsBannerLoaded() {
        return isBannerLoaded;
    }
    #endregion

    void OnApplicationPause(bool isPaused) {
        IronSource.Agent.onApplicationPause(isPaused);
    }

    
#endif

#endif
    public override AdsMediationType GetAdsMediationType() {
        return AdsMediationType.IRONSOURCE;
    }
}

