using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using Firebase.RemoteConfig;
using UnityEngine.Serialization;

namespace SDK {
    public enum AdsMediationType {
        NONE,
        MAX,
        ADMOB,
        IRONSOURCE
    }
    public enum AdsType {
        BANNER,
        INTERSTITIAL,
        REWARDED,
        MREC,
        APP_OPEN
    }
    public enum WatchVideoRewardType {
        NONE,
    }
    
    [ScriptOrder(-99)]
    public class AdsManager : MonoBehaviour {
        public bool IsCheatAds;
        private static AdsManager m_Instance;
        public static AdsManager Instance {
            get {
                return m_Instance;
            }
        }
        public  SDKSetup m_SDKSetup;
        private double m_AdsLoadingCooldown = 0f;
        private double m_MaxLoadingCooldown = 5f;

        private double m_InterstitialCappingAdsCooldown = 0;
        private double m_MaxInterstitialCappingAdsTime = 30;
        private int m_RewardInterruptCountTime = 0;
        private int m_MaxRewardInterruptCount = 6;
        private bool m_IsActiveInterruptReward = false;
        private bool m_IsUpdateRemoteConfigSuccess = false;
        
        public AdsMediationType m_MainAdsMediationType = AdsMediationType.MAX;

        public AdsConfig m_RewardVideoAdsConfig;
        public AdsConfig m_InterstitialAdsConfig;
        public AdsConfig m_BannerAdsConfig;
        public AdsConfig m_MRecAdsConfig;
        public AdsConfig m_AppOpenAdsConfig;

        public List<AdsMediationController> m_AdsMediationControllers = new List<AdsMediationController>();

        private void Awake() {
            if (m_Instance != null) {
                Destroy(gameObject);
                return;
            }
            m_Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("ADD Listner ADSMANAGER");
            EventManager.StartListening("UpdateRemoteConfigs", UpdateRemoteConfigs);
            m_IsActiveInterruptReward = true;
        }
        private void Start()
        {
            InitConfig();
            InitAdsMediation();
        }
        private void UpdateRemoteConfigs() {
            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_interstital_rate_time);
                m_MaxInterstitialCappingAdsTime = configValue.DoubleValue;
                Debug.Log("=============== MAX " + m_MaxInterstitialCappingAdsTime);
            }
            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_inter_reward_interspersed);
                m_IsActiveInterruptReward = configValue.BooleanValue;
                Debug.Log("=============== Active " + m_IsActiveInterruptReward);
            }
            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_inter_reward_interspersed_time);
                m_MaxRewardInterruptCount = (int)configValue.DoubleValue;
                Debug.Log("=============== MAX " + m_MaxRewardInterruptCount);
            }
            UpdateAOARemoteConfig();
            m_IsUpdateRemoteConfigSuccess = true;
        }
        
        private void Update() {
            float dt = Time.deltaTime;
            if (m_InterstitialCappingAdsCooldown > 0) {
                m_InterstitialCappingAdsCooldown -= dt;
            }
            if (m_AdsLoadingCooldown > 0) {
                m_AdsLoadingCooldown -= dt;
                if (m_AdsLoadingCooldown <= 0) {
                    if (!IsRewardVideoLoaded()) {
                        RequestRewardBasedVideo();
                    }
                    if (!IsInterstitialAdLoaded()) {
                        RequestInterstitial();
                    }
                }
            }
            UpdateBanner();
        }

        private void InitConfig()
        {
            m_InterstitialAdsConfig.Init(GetAdsMediationController(m_SDKSetup.GetAdsMediationType(AdsType.INTERSTITIAL)),OnAdRevenuePaidEvent);
            m_RewardVideoAdsConfig.Init(GetAdsMediationController(m_SDKSetup.GetAdsMediationType(AdsType.REWARDED)),OnAdRevenuePaidEvent);
            m_BannerAdsConfig.Init(GetAdsMediationController(m_SDKSetup.GetAdsMediationType(AdsType.BANNER)),OnAdRevenuePaidEvent);
            m_MRecAdsConfig.Init(GetAdsMediationController(m_SDKSetup.GetAdsMediationType(AdsType.MREC)),OnAdRevenuePaidEvent);
            m_AppOpenAdsConfig.Init(GetAdsMediationController(m_SDKSetup.GetAdsMediationType(AdsType.APP_OPEN)),OnAdRevenuePaidEvent);
        }
        private void InitAdsMediation() {
            if (!GetSelectedMediation(AdsType.INTERSTITIAL).IsInited) {
                GetSelectedMediation(AdsType.INTERSTITIAL).Init();
            }
            if (!GetSelectedMediation(AdsType.REWARDED).IsInited) {
                GetSelectedMediation(AdsType.REWARDED).Init();
            }
            if(!GetSelectedMediation(AdsType.BANNER).IsInited) {
                GetSelectedMediation(AdsType.BANNER).Init();
            }
            if(!GetSelectedMediation(AdsType.MREC).IsInited) {
                GetSelectedMediation(AdsType.MREC).Init();
            }
            if(!GetSelectedMediation(AdsType.APP_OPEN).IsInited) {
                GetSelectedMediation(AdsType.APP_OPEN).Init();
            }
        }
        public void InitAdsType() {
            //Setup Interstitial
            SetupInterstitial();

            //Setup Reward Video
            SetupRewardVideo();

            //Setup Banner
            SetupBannerAds();

            //Setup RMecAds
            SetupRMecAds();
            
            //Setup AppOpenAds
            SetupAppOpenAds();
        }
        #region EditorUpdate

        public void UpdateAdsMediationConfig()
        {
            m_MainAdsMediationType = m_SDKSetup.adsMediationType;
            m_RewardVideoAdsConfig.adsMediationType = m_SDKSetup.rewardedAdsMediationType;
            m_InterstitialAdsConfig.adsMediationType = m_SDKSetup.interstitialAdsMediationType;
            m_BannerAdsConfig.adsMediationType = m_SDKSetup.bannerAdsMediationType;
            m_MRecAdsConfig.adsMediationType = m_SDKSetup.mrecAdsMediationType;
            m_AppOpenAdsConfig.adsMediationType = m_SDKSetup.appOpenAdsMediationType;
            UpdateMaxMediation();
            UpdateAdmobMediation();
        }

        private void UpdateMaxMediation()
        {
            MaxMediationController maxMediationController = GetAdsMediationController(AdsMediationType.MAX) as MaxMediationController;
            if (maxMediationController == null) return;
            if (m_SDKSetup.adsMediationType == AdsMediationType.MAX)
            {
                maxMediationController.m_MaxAdConfig.sdkKey = m_SDKSetup.maxAdsSetup.sdkKey;
            }
            if (m_SDKSetup.interstitialAdsMediationType == AdsMediationType.MAX)
            {
                maxMediationController.m_MaxAdConfig.InterstitialAdUnitID =
                    m_SDKSetup.maxAdsSetup.InterstitialAdUnitID;
            }
            if (m_SDKSetup.rewardedAdsMediationType == AdsMediationType.MAX)
            {
                maxMediationController.m_MaxAdConfig.RewardedAdUnitID =
                    m_SDKSetup.maxAdsSetup.RewardedAdUnitID;
            }
            if(m_SDKSetup.bannerAdsMediationType == AdsMediationType.MAX)
            {
                maxMediationController.m_MaxAdConfig.BannerAdUnitID = m_SDKSetup.maxAdsSetup.BannerAdUnitID;
            }
            if(m_SDKSetup.mrecAdsMediationType == AdsMediationType.MAX)
            {
                maxMediationController.m_MaxAdConfig.MrecAdUnitID = m_SDKSetup.maxAdsSetup.MrecAdUnitID;
            }
            if(m_SDKSetup.appOpenAdsMediationType == AdsMediationType.MAX)
            {
                maxMediationController.m_MaxAdConfig.AppOpenAdUnitID = m_SDKSetup.maxAdsSetup.AppOpenAdUnitID;
            }
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(maxMediationController);
            #endif
        }

        private void UpdateAdmobMediation()
        {
            AdmobMediationController admobMediationController = GetAdsMediationController(AdsMediationType.ADMOB) as AdmobMediationController;
            if (admobMediationController == null) return;
            if (m_SDKSetup.interstitialAdsMediationType == AdsMediationType.ADMOB)
            {
                m_MainAdsMediationType = AdsMediationType.ADMOB;
                admobMediationController.m_AdmobAdSetup.InterstitialAdUnitIDList =
                    m_SDKSetup.admobAdsSetup.InterstitialAdUnitIDList;
            }
            if (m_SDKSetup.rewardedAdsMediationType == AdsMediationType.ADMOB)
            {
                admobMediationController.m_AdmobAdSetup.RewardedAdUnitIDList =
                    m_SDKSetup.admobAdsSetup.RewardedAdUnitIDList;
            }
            if(m_SDKSetup.bannerAdsMediationType == AdsMediationType.ADMOB)
            {
                admobMediationController.m_AdmobAdSetup.BannerAdUnitIDList = m_SDKSetup.admobAdsSetup.BannerAdUnitIDList;
            }
            if(m_SDKSetup.mrecAdsMediationType == AdsMediationType.ADMOB)
            {
                admobMediationController.m_AdmobAdSetup.MrecAdUnitIDList = m_SDKSetup.admobAdsSetup.MrecAdUnitIDList;
            }
            if(m_SDKSetup.appOpenAdsMediationType == AdsMediationType.ADMOB)
            {
                admobMediationController.m_AdmobAdSetup.AppOpenAdUnitIDList = m_SDKSetup.admobAdsSetup.AppOpenAdUnitIDList;
            }
            #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(admobMediationController);
            #endif
        }

        #endregion
        
        #region Interstitial
        public UnityAction m_InterstitialAdCloseCallback;
        public UnityAction m_InterstitialAdLoadSuccessCallback;
        public UnityAction m_InterstitialAdLoadFailCallback;
        public UnityAction m_InterstitialAdShowSuccessCallback;
        public UnityAction m_InterstitialAdShowFailCallback;
        private void SetupInterstitial()
        {
            m_InterstitialAdsConfig.isActive = m_SDKSetup.IsActiveAdsType(AdsType.INTERSTITIAL);
            if (!m_SDKSetup.IsActiveAdsType(AdsType.INTERSTITIAL))return;
            foreach (AdsMediationController t in m_InterstitialAdsConfig.adsMediations)
            {
                t.InitInterstitialAd(
                    OnInterstitialAdClosed,
                    OnInterstitialAdSuccessToLoad,
                    OnInterstitialAdFailedToLoad,
                    OnInterstitialAdShowSuccess,
                    OnInterstitialAdShowFail
                );
            }
        }
        public void RequestInterstitial() {
            if (GetSelectedMediation(AdsType.INTERSTITIAL).IsInterstitialLoaded()) return;
#if !UNITY_EDITOR
            GetSelectedMediation(AdsType.INTERSTITIAL).RequestInterstitialAd();
#endif
        }
        public void ShowInterstitial(UnityAction closedCallback, UnityAction showSuccessCallback, bool isTracking = true, bool isSkipCapping = false) {
            if (!isSkipCapping) {
                if (m_InterstitialCappingAdsCooldown > 0) return; 
            }
            m_InterstitialAdCloseCallback = closedCallback;
            if (isTracking) {
                ABIAnalyticsManager.Instance.TrackAdsInterstitial_ClickOnButton();
            }
            if (IsInterstitialAdLoaded()) {
                ShowSelectedInterstitialAd(showSuccessCallback);
            }
        }
        private void ShowSelectedInterstitialAd(UnityAction showSuccessCallback) {
            m_InterstitialAdShowSuccessCallback = showSuccessCallback;
            GetSelectedMediation(AdsType.INTERSTITIAL).ShowInterstitialAd();
        }
        public bool IsInterstitialAdLoaded() {
            bool isInterstitialAdLoaded = GetSelectedMediation(AdsType.INTERSTITIAL).IsInterstitialLoaded();
            return isInterstitialAdLoaded;
        }
        public void ResetAdsLoadingCooldown() {
            m_AdsLoadingCooldown = m_MaxLoadingCooldown;
        }
        public void ResetAdsInterstitialCappingTime() {
            m_InterstitialCappingAdsCooldown = m_MaxInterstitialCappingAdsTime;
        }
        
        public void OnInterstitialAdSuccessToLoad() {
            m_InterstitialAdsConfig.RefreshLoadAds();
            m_InterstitialAdLoadSuccessCallback?.Invoke();
            ABIAnalyticsManager.Instance.TrackAdsInterstitial_LoadedSuccess();
        }
        private void OnInterstitialAdFailedToLoad() {
            ResetAdsLoadingCooldown();
            m_InterstitialAdLoadFailCallback?.Invoke();
        }
        private void OnInterstitialAdShowSuccess() {
            m_InterstitialAdShowSuccessCallback?.Invoke();
            ABIAnalyticsManager.Instance.TrackAdsInterstitial_ShowSuccess();
            MarkShowingAds(true);
        }
        private void OnInterstitialAdShowFail() {
            m_InterstitialAdsConfig.MarkReloadFail();
            m_InterstitialAdShowFailCallback?.Invoke();
            ABIAnalyticsManager.Instance.TrackAdsInterstitial_ShowFail();
        }
        private void OnInterstitialAdClosed() {
            RequestInterstitial();
            ResetAdsInterstitialCappingTime();
            m_InterstitialAdCloseCallback?.Invoke();
            MarkShowingAds(false);
        }
        
        public bool IsReadyToShowInterstitial()
        {
            return IsInterstitialAdLoaded() && m_InterstitialCappingAdsCooldown > 0;
        }
        #endregion

        #region Banner Ads
        private float m_BannerCountTime;
        private float m_BannerResetTime = 10f;
        private bool m_IsBannerShowing;
        private void UpdateBanner() {
            if (!m_BannerAdsConfig.isActive) return;
            m_BannerCountTime += Time.deltaTime;
            if (m_BannerCountTime >= m_BannerResetTime) {
                m_BannerCountTime = 0;
                DestroyBanner();
                RequestBanner();
            }
        }
        private void SetupBannerAds() {
            m_BannerAdsConfig.isActive = m_SDKSetup.IsActiveAdsType(AdsType.BANNER);
            if (!m_SDKSetup.IsActiveAdsType(AdsType.BANNER)) return;
            foreach (AdsMediationController t in m_BannerAdsConfig.adsMediations)
            {
                t.InitBannerAds(OnBannerLoadedSucess, OnBannerLoadedFail, OnBannerCollapsed, OnBannerExpanded);
            }
        }

        public bool IsBannerShowing()
        {
            return m_IsBannerShowing;
        }
        public void RequestBanner() {
            if (!m_BannerAdsConfig.isActive) return;
            GetSelectedMediation(AdsType.BANNER).RequestBannerAds();
        }
        public void ShowBannerAds() {
            GetSelectedMediation(AdsType.BANNER).ShowBannerAds();
        }
        public void HideBannerAds() {
            GetSelectedMediation(AdsType.BANNER).HideBannerAds();
        }
        public void DestroyBanner() {
            GetSelectedMediation(AdsType.BANNER).DestroyBannerAds();
        }
        public bool IsBannerLoaded() {
            return GetSelectedMediation(AdsType.BANNER).IsBannerLoaded();
        }
        private void OnBannerLoadedSucess() {
            Debug.Log("Banner Loaded");            
            m_BannerCountTime = 0;
        }

        private void OnBannerLoadedFail()
        {
            Debug.Log("Banner Load Fail");
        }

        private void OnBannerExpanded()
        {
            Debug.Log("Banner Expanded");
            m_IsBannerShowing = true;
        }

        private void OnBannerCollapsed()
        {
            Debug.Log("Banner Collapsed");
            m_IsBannerShowing = false;
        }
        #endregion

        #region Reward Ads
        public UnityAction m_RewardedVideoCloseCallback;
        public UnityAction m_RewardedVideoLoadSuccessCallback;
        public UnityAction m_RewardedVideoLoadFailedCallback;
        public UnityAction m_RewardedVideoEarnSuccessCallback;
        public UnityAction m_RewardedVideoShowStartCallback;
        public UnityAction m_RewardedVideoShowFailCallback;
        
        private string m_RewardedPlacement;
        // Reward Video Setup
        private void SetupRewardVideo() {
            m_RewardVideoAdsConfig.isActive = m_SDKSetup.IsActiveAdsType(AdsType.REWARDED);
            if (!m_SDKSetup.IsActiveAdsType(AdsType.REWARDED)) return;
            foreach (AdsMediationController t in m_RewardVideoAdsConfig.adsMediations)
            {
                t.InitRewardVideoAd(
                    OnRewardVideoClosed,
                    OnRewardVideoLoadSuccess,
                    OnRewardVideoLoadFail,
                    OnRewardVideoStart
                );
            }
        }
        public void RequestRewardBasedVideo() {
            if (GetSelectedMediation(AdsType.REWARDED).IsRewardVideoLoaded()) return;
            GetSelectedMediation(AdsType.REWARDED).RequestRewardVideoAd();
        }
        public void ShowRewardVideo(string rewardedPlacement, UnityAction successCallback, UnityAction failedCallback = null) {
            if (IsCheatAds) {
                successCallback?.Invoke();
                return;
            }
            m_RewardedPlacement = rewardedPlacement;
            m_RewardedVideoEarnSuccessCallback = successCallback;
            m_RewardedVideoShowFailCallback = failedCallback;
            if (m_IsActiveInterruptReward && IsReadyToShowRewardInterrupt() && IsInterstitialAdLoaded()) {
                ShowInterstitial(null, () => {
                    successCallback();
                    ResetRewardInterruptCount();
                }, false, true);
            } else {
                ABIAnalyticsManager.Instance.TrackAdsReward_ClickOnButton();
                if (IsReadyToShowRewardVideo()) {
                    GetSelectedMediation(AdsType.REWARDED).ShowRewardVideoAd(OnRewardVideoEarnSuccess, OnRewardVideoShowFail);
                }
            }
        }
        public bool IsRewardVideoLoaded() {
            return GetSelectedMediation(AdsType.REWARDED).IsRewardVideoLoaded();
        }
        private void OnRewardVideoEarnSuccess() {
            if (m_RewardedVideoEarnSuccessCallback != null) {
                m_RewardedVideoEarnSuccessCallback();
            }
            m_RewardInterruptCountTime++;
            ABIAnalyticsManager.Instance.TrackAdsReward_ShowCompleted(m_RewardedPlacement);
        }
        private void OnRewardVideoStart() {
            if (m_RewardedVideoShowStartCallback != null) {
                m_RewardedVideoShowStartCallback();
            }
            ABIAnalyticsManager.Instance.TrackAdsReward_StartShow();
            MarkShowingAds(true);
        }
        private void OnRewardVideoShowFail() {
            if (m_RewardedVideoShowFailCallback != null) {
                m_RewardedVideoShowFailCallback();
            }
            ABIAnalyticsManager.Instance.TrackAdsReward_ShowFail();
        }
        private void OnRewardVideoClosed() {
            ResetAdsInterstitialCappingTime();
            RequestRewardBasedVideo();
            if (m_RewardedVideoCloseCallback != null) {
                m_RewardedVideoCloseCallback();
            }
            MarkShowingAds(false);
        }
        private void OnRewardVideoLoadSuccess() {
            m_RewardVideoAdsConfig.RefreshLoadAds();
            if (m_RewardedVideoLoadSuccessCallback != null) {
                m_RewardedVideoLoadSuccessCallback();
            }
            ABIAnalyticsManager.Instance.TrackAdsReward_LoadSuccess();
        }
        private void OnRewardVideoLoadFail() {
            ResetAdsLoadingCooldown();
            m_RewardVideoAdsConfig.MarkReloadFail();
            if (m_RewardedVideoLoadFailedCallback != null) {
                m_RewardedVideoLoadFailedCallback();
            }
        }

        public bool IsReadyToShowRewardVideo()
        {
            return IsRewardVideoLoaded();
        }
        public bool IsReadyToShowRewardInterrupt()
        {
            return m_RewardInterruptCountTime >= m_MaxRewardInterruptCount;
        }
        public void ResetRewardInterruptCount() {
            m_RewardInterruptCountTime = 0;
        }
        #endregion Reward Ads
        
        #region MRec Ads
        public UnityAction m_MRecAdLoadedCallback;
        public UnityAction m_MRecAdLoadFailCallback;
        public UnityAction m_MRecAdClickedCallback;
        public UnityAction m_MRecAdExpandedCallback;
        public UnityAction m_MRecAdCollapsedCallback;
        private bool m_IsMRecShowing;
        private void SetupRMecAds()
        {
            m_MRecAdsConfig.isActive = m_SDKSetup.IsActiveAdsType(AdsType.MREC);
            if (!m_SDKSetup.IsActiveAdsType(AdsType.MREC)) return;
            foreach (AdsMediationController t in m_MRecAdsConfig.adsMediations)
            {
                t.InitRMecAds(OnMRecAdLoadedEvent, OnMRecAdLoadFailedEvent, OnMRecAdClickedEvent, OnMRecAdExpandedEvent, OnMRecAdCollapsedEvent);
            }
        }

        public bool IsMRecShowing()
        {
            return m_IsMRecShowing;
        }

        public bool IsMRecLoaded()
        {
            return GetSelectedMediation(AdsType.MREC).IsMRecLoaded();
        }
        private void OnMRecAdLoadedEvent()
        {
            m_MRecAdLoadedCallback?.Invoke();
        }
        private void OnMRecAdLoadFailedEvent()
        {
            m_MRecAdLoadFailCallback?.Invoke();
        }
        private void OnMRecAdClickedEvent()
        {
            m_MRecAdClickedCallback?.Invoke();
        }
        private void OnMRecAdExpandedEvent()
        {
            m_MRecAdExpandedCallback?.Invoke();
            m_IsMRecShowing = true;
        }
        private void OnMRecAdCollapsedEvent()
        {
            m_MRecAdCollapsedCallback?.Invoke();
            m_IsMRecShowing = false;
        }
        public void ShowMRecAds()
        {
            if(IsCheatAds) return;
            GetSelectedMediation(AdsType.MREC).ShowMRecAds();
            HideBannerAds();
        }
        public void HideMRecAds()
        {
            if(IsCheatAds) return;
            GetSelectedMediation(AdsType.MREC).HideMRecAds();
        }
        #endregion
        
        #region App Open Ads

        private bool m_IsActiveAoaByRemoteConfig = true;
        private bool m_IsActiveShowAdsFirstTime = true;
        private bool m_IsDoneShowAdsFirstTime = false;
        private double m_AoaTimeBetweenShow = 0;    
        private double m_AoaPauseTimeNeedToShowAds = 0;
        private DateTime m_ExpireTime;
        private DateTime m_CloseAdsTime;
        private DateTime m_StartPauseTime;
        public bool IsShowingAds { get; private set; }

        private void SetupAppOpenAds()
        {
            m_AppOpenAdsConfig.isActive = m_SDKSetup.IsActiveAdsType(AdsType.APP_OPEN);
            if (!m_SDKSetup.IsActiveAdsType(AdsType.APP_OPEN)) return;
            foreach (AdsMediationController t in m_AppOpenAdsConfig.adsMediations)
            {
                t.InitAppOpenAds(OnAppOpenAdLoadedEvent, OnAppOpenAdLoadFailedEvent, OnAppOpenAdClosedEvent, OnAppOpenAdDisplayedEvent, OnAppOpenAdFailedToDisplayEvent);
            }
            ShowAdsFirstTime();
        }

        private void ShowAppOpenAds()
        {
            if (IsAppOpenAdsReady())
            {
                GetSelectedMediation(AdsType.APP_OPEN).ShowAppOpenAds();    
            }
        }

        private void ForceShowAppOpenAds()
        {
            if (IsAppOpenAdsLoaded())
            {
                GetSelectedMediation(AdsType.APP_OPEN).ShowAppOpenAds();
            }
        }
        private void RequestAppOpenAds()
        {
            GetSelectedMediation(AdsType.APP_OPEN).RequestAppOpenAds();
        }

        private bool IsAppOpenAdsReady()
        {
            Debug.Log("Status " + GetSelectedMediation(AdsType.APP_OPEN)?.IsAppOpenAdsLoaded() + " Remote= " + m_IsActiveAoaByRemoteConfig + " AdsConfig=" + m_AppOpenAdsConfig.isActive + " Time=" + (DateTime.Now - m_StartPauseTime).TotalSeconds + " Need=" + m_AoaPauseTimeNeedToShowAds
            + " IsShowingAds=" + IsShowingAds + " Close Time=" + (DateTime.Now - m_CloseAdsTime).TotalSeconds + " Need=" + m_AoaTimeBetweenShow);
            if (!IsActiveAppOpenAds()) return false;
            return IsAppOpenAdsLoaded();
        }

        private bool IsActiveAppOpenAds()
        {
            if (!m_IsActiveAoaByRemoteConfig)return false;
            if (IsShowingAds) return false;
            if ((DateTime.Now - m_CloseAdsTime).TotalSeconds < m_AoaTimeBetweenShow) return false;
            return true;
        }

        private bool IsAppOpenAdsLoaded()
        {
            return GetSelectedMediation(AdsType.APP_OPEN) != null && GetSelectedMediation(AdsType.APP_OPEN).IsAppOpenAdsLoaded();
        }
        private void UpdateAOARemoteConfig()
        {
            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_aoa_active);
                m_IsActiveAoaByRemoteConfig = configValue.BooleanValue;
                Debug.Log("AOA active = " + m_IsActiveAoaByRemoteConfig);
            }

            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_aoa_show_first_time_active);
                m_IsActiveShowAdsFirstTime = configValue.BooleanValue;
                Debug.Log("AOA active show first time = " + m_IsActiveShowAdsFirstTime);
            }

            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_aoa_time_between_step_load);
                m_AoaTimeBetweenShow = configValue.DoubleValue;
                Debug.Log("AOA Load time = " + m_AoaTimeBetweenShow);
            }

            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_aoa_pause_time_need_to_show_ads);
                m_AoaPauseTimeNeedToShowAds = configValue.DoubleValue;
                Debug.Log("AOA Pause time = " + m_AoaPauseTimeNeedToShowAds);
            }
        }
        private void ShowAdsFirstTime() {
            Debug.Log("---------------------------------Show Ads Fisrt Time--------------------------");
            StartCoroutine(coWaitFechingSuccessAndShow());
        }
        IEnumerator coWaitFechingSuccessAndShow() {
            if (m_IsDoneShowAdsFirstTime) yield break;
            while (!m_IsUpdateRemoteConfigSuccess) {
                yield return new WaitForSeconds(0.5f);
            }

            float num = 0;
            while (!IsAppOpenAdsLoaded())
            {
                num += 0.5f;
                if (num >= 3) yield break;
                yield return new WaitForSeconds(0.5f);
            }
            ForceShowAppOpenAds();

            m_IsDoneShowAdsFirstTime = true;
            Debug.Log("Show App Open Ads First Time");
        }

        private void MarkShowingAds(bool isShowing)
        {
            if (isShowing)
            {
                IsShowingAds = true;
            }
            else
            {
                EventManager.AddEventNextFrame(() =>
                {
                    StartCoroutine(coWaitingMarkShowingAdsDone());
                });
            }
        }

        IEnumerator coWaitingMarkShowingAdsDone()
        {
            yield return new WaitForSeconds(2f);
            IsShowingAds = false;
        }
        private void OnAppOpenAdLoadedEvent()
        {
            Debug.Log("AdsManager AOA Loaded");
        }
        private void OnAppOpenAdLoadFailedEvent()
        {
            Debug.Log("AdsManager AOA Load Fail");
        }
        private void OnAppOpenAdClosedEvent()
        {
            Debug.Log("AdsManager Closed app open ad");
            IsShowingAds = false;
            m_CloseAdsTime = DateTime.Now;
            RequestAppOpenAds();
        }
        private void OnAppOpenAdDisplayedEvent()
        {
            Debug.Log("AdsManager Displayed app open ad");
            IsShowingAds = true;
        }
        private void OnAppOpenAdFailedToDisplayEvent()
        {
            Debug.Log("AdsManager Failed to display app open ad");
            IsShowingAds = false;
        }
        #endregion
        
        private void OnAdRevenuePaidEvent(ImpressionData impressionData) {
            Debug.Log("Paid Ad Revenue - Ads Type = " + impressionData.ad_type);
            ABIAnalyticsManager.TrackAdImpression(impressionData);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackAppsflyerAdRevenue(impressionData);
#endif
        }
        public AdsMediationController GetSelectedMediation(AdsType adsType)
        {
            return adsType switch
            {
                AdsType.BANNER => m_BannerAdsConfig.GetAdsMediation(),
                AdsType.INTERSTITIAL => m_InterstitialAdsConfig.GetAdsMediation(),
                AdsType.REWARDED => m_RewardVideoAdsConfig.GetAdsMediation(),
                AdsType.MREC => m_MRecAdsConfig.GetAdsMediation(),
                AdsType.APP_OPEN => m_AppOpenAdsConfig.GetAdsMediation(),
                _ => null
            };
        }
        private AdsMediationController GetAdsMediationController(AdsMediationType adsMediationType)
        {
            return adsMediationType switch
            {
                AdsMediationType.MAX => m_AdsMediationControllers[0],
                AdsMediationType.ADMOB => m_AdsMediationControllers[1],
                AdsMediationType.IRONSOURCE => m_AdsMediationControllers[2],
                _ => null
            };
        }
        private void OnApplicationPause(bool paused)
        {
            Debug.Log("OnApplicationPause " + paused + " Remote=" + m_IsActiveAoaByRemoteConfig + " AdsConfig=" +
                      m_AppOpenAdsConfig.isActive + " Time=" + (DateTime.Now - m_StartPauseTime).TotalSeconds +
                      " Need=" + m_AoaPauseTimeNeedToShowAds);
            if (!m_IsActiveAoaByRemoteConfig || !m_AppOpenAdsConfig.isActive) return;
            switch (paused)
            {
                case true:
                    m_StartPauseTime = DateTime.Now;
                    break;
                case false when (DateTime.Now - m_StartPauseTime).TotalSeconds > m_AoaPauseTimeNeedToShowAds:
                    ShowAppOpenAds();
                    break;
            }
        }
    }
}