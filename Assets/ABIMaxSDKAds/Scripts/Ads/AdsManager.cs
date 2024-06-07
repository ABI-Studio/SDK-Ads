using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using ABI;
using UnityEngine.Events;
using Firebase.RemoteConfig;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine.Serialization;

namespace SDK
{
    public enum AdsMediationType
    {
        NONE,
        MAX,
        ADMOB,
        IRONSOURCE
    }

    public enum AdsType
    {
        BANNER,
        INTERSTITIAL,
        REWARDED,
        MREC,
        APP_OPEN,
        COLLAPSIBLE_BANNER
    }

    [ScriptOrder(-99)]
    public class AdsManager : MonoBehaviour
    {
        #region Fields

        public bool IsCheatAds;
        public static AdsManager Instance { get; private set; }

        public SDKSetup m_SDKSetup;
        private double m_AdsLoadingCooldown = 0f;
        private double m_MaxLoadingCooldown = 5f;
        private double m_InterstitialCappingAdsCooldown = 0;
        private double m_MaxInterstitialCappingTimeDay1 = 30;
        private double m_MaxInterstitialCappingTimeDay2 = 30;
        private int m_RewardInterruptCountTime = 0;
        private int m_MaxRewardInterruptCount = 6;
        private bool m_IsActiveInterruptReward = false;
        private bool m_IsUpdateRemoteConfigSuccess = false;
        private bool IsInitedAdsType;
        private bool IsRemoveAds;
        public bool IsLinkRewardWithRemoveAds;
        
        public AdsMediationType m_MainAdsMediationType = AdsMediationType.MAX;
        public List<AdsConfig> m_AdsConfigs = new List<AdsConfig>();
        public List<AdsMediationController> m_AdsMediationControllers = new List<AdsMediationController>();
        
        private const string key_local_remove_ads = "key_local_remove_ads";

        #endregion

        #region System

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            EventManager.StartListening("UpdateRemoteConfigs", UpdateRemoteConfigs);
            m_IsActiveInterruptReward = true;
            LoadRemoveAds();
        }
        private void Start()
        {
            InitConfig();
            InitAdsMediation();
        }
        private void UpdateRemoteConfigs()
        {
            {
                ConfigValue configValue =
                    ABIFirebaseManager.Instance.GetConfigValue(Keys.key_remote_interstital_capping_time_day1);
                m_MaxInterstitialCappingTimeDay1 = configValue.DoubleValue;
                Debug.Log("=============== MAX Day 1" + m_MaxInterstitialCappingTimeDay1);
            }
            {
                ConfigValue configValue =
                    ABIFirebaseManager.Instance.GetConfigValue(Keys.key_remote_interstital_capping_time_day2);
                m_MaxInterstitialCappingTimeDay2 = configValue.DoubleValue;
                Debug.Log("=============== MAX Day 2" + m_MaxInterstitialCappingTimeDay2);
            }
            {
                ConfigValue configValue =
                    ABIFirebaseManager.Instance.GetConfigValue(Keys.key_remote_inter_reward_interspersed);
                m_IsActiveInterruptReward = configValue.BooleanValue;
                Debug.Log("=============== Active " + m_IsActiveInterruptReward);
            }
            {
                ConfigValue configValue =
                    ABIFirebaseManager.Instance.GetConfigValue(Keys.key_remote_inter_reward_interspersed_time);
                m_MaxRewardInterruptCount = (int)configValue.DoubleValue;
                Debug.Log("=============== MAX Reward InteruptCount" + m_MaxRewardInterruptCount);
            }
            UpdateAOARemoteConfig();
            m_IsUpdateRemoteConfigSuccess = true;
        }
        private void Update()
        {
            float dt = Time.deltaTime;
            if (m_InterstitialCappingAdsCooldown > 0)
            {
                m_InterstitialCappingAdsCooldown -= dt;
            }

            if (m_AdsLoadingCooldown > 0)
            {
                m_AdsLoadingCooldown -= dt;
                if (m_AdsLoadingCooldown <= 0)
                {
                    if (!IsRewardVideoLoaded())
                    {
                        RequestRewardVideo();
                    }

                    if (!IsInterstitialAdLoaded())
                    {
                        RequestInterstitial();
                    }
                }
            }

            UpdateBanner();
            UpdateCollapsibleBanner(dt);
        }
        private void InitConfig()
        {
            foreach (AdsConfig adsConfig in m_AdsConfigs)
            {
                AdsMediationType adsMediationType = m_SDKSetup.GetAdsMediationType(adsConfig.adsType);
                adsConfig.Init(GetAdsMediationController(adsMediationType), OnAdRevenuePaidEvent);
            }
        }
        private void InitAdsMediation()
        {
            Debug.Log("Init Ads Mediation");
            {
                AdsMediationController adsMediationController = GetSelectedMediation(AdsType.INTERSTITIAL);
                if (adsMediationController != null && !adsMediationController.IsInited)
                {
                    GetSelectedMediation(AdsType.INTERSTITIAL).Init();
                }
            }

            {
                AdsMediationController adsMediationController = GetSelectedMediation(AdsType.REWARDED);
                if (adsMediationController != null && !adsMediationController.IsInited)
                {
                    GetSelectedMediation(AdsType.REWARDED).Init();
                }
            }

            {
                AdsMediationController adsMediationController = GetSelectedMediation(AdsType.BANNER);
                if (adsMediationController != null && !adsMediationController.IsInited)
                {
                    GetSelectedMediation(AdsType.BANNER).Init();
                }
            }
            
            {
                AdsMediationController adsMediationController = GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER);
                if (adsMediationController != null && !adsMediationController.IsInited)
                {
                    GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER).Init();
                }
            }

            {
                AdsMediationController adsMediationController = GetSelectedMediation(AdsType.MREC);
                if (adsMediationController != null && !adsMediationController.IsInited)
                {
                    GetSelectedMediation(AdsType.MREC).Init();
                }
            }

            {
                AdsMediationController adsMediationController = GetSelectedMediation(AdsType.APP_OPEN);
                if (adsMediationController != null && !adsMediationController.IsInited)
                {
                    GetSelectedMediation(AdsType.APP_OPEN).Init();
                }
            }
        }
        public void InitAdsType(AdsMediationType adsMediationType)
        {
            Debug.Log("Init Ads Type");
            //Setup Interstitial
            SetupInterstitial(adsMediationType);

            //Setup Reward Video
            SetupRewardVideo(adsMediationType);

            //Setup Banner
            SetupBannerAds(adsMediationType);
            
            //Setup Collapsible Banner
            SetupCollapsibleBannerAds(adsMediationType);

            //Setup RMecAds
            SetupMRecAds(adsMediationType);

            //Setup AppOpenAds
            SetupAppOpenAds(adsMediationType);

            IsInitedAdsType = true;
        }
        private void LoadRemoveAds()
        {
            IsRemoveAds = PlayerPrefs.GetInt(key_local_remove_ads, 0) == 1;
        }
        public void SetRemoveAds(bool isRemove)
        {
            IsRemoveAds = isRemove;
            PlayerPrefs.SetInt(key_local_remove_ads, isRemove ? 1 : 0);
            DestroyBanner();
            DestroyCollapsibleBanner();
        }
        private AdsConfig GetAdsConfig(AdsType adsType)
        {
            return m_AdsConfigs.Find(x => x.adsType == adsType);
        }
        private AdsMediationController GetSelectedMediation(AdsType adsType)
        {
            return adsType switch
            {
                AdsType.BANNER => BannerAdsConfig.GetAdsMediation(),
                AdsType.COLLAPSIBLE_BANNER => CollapsibleBannerAdsConfig.GetAdsMediation(),
                AdsType.INTERSTITIAL => InterstitialAdsConfig.GetAdsMediation(),
                AdsType.REWARDED => RewardVideoAdsConfig.GetAdsMediation(),
                AdsType.MREC => MRecAdsConfig.GetAdsMediation(),
                AdsType.APP_OPEN => AppOpenAdsConfig.GetAdsMediation(),
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
        #endregion

        #region EditorUpdate

        public void UpdateAdsMediationConfig()
        {
            if(m_SDKSetup == null) return;
            UpdateAdsMediationConfig(m_SDKSetup);
        }
        public void UpdateAdsMediationConfig(SDKSetup sdkSetup)
        {
            m_SDKSetup = sdkSetup;
            m_MainAdsMediationType = m_SDKSetup.adsMediationType;
            foreach (AdsConfig adsConfig in m_AdsConfigs)
            {
                AdsMediationType adsMediationType = m_SDKSetup.GetAdsMediationType(adsConfig.adsType);
                adsConfig.adsMediationType = adsMediationType;
            }

            IsLinkRewardWithRemoveAds = m_SDKSetup.IsLinkToRemoveAds;
            UpdateMaxMediation();
            UpdateAdmobMediation();
        }
        private void UpdateMaxMediation()
        {
#if UNITY_AD_MAX
            const AdsMediationType adsMediationType = AdsMediationType.MAX;
            MaxMediationController maxMediationController = GetAdsMediationController(adsMediationType) as MaxMediationController;
            if (maxMediationController == null) return;
            if (m_SDKSetup.adsMediationType == adsMediationType)
            {
                maxMediationController.m_MaxAdConfig.SDKKey = m_SDKSetup.maxAdsSetup.SDKKey;
            }

            maxMediationController.m_MaxAdConfig.InterstitialAdUnitID = m_SDKSetup.interstitialAdsMediationType == adsMediationType ? m_SDKSetup.maxAdsSetup.InterstitialAdUnitID : "";
            
            maxMediationController.m_MaxAdConfig.RewardedAdUnitID = m_SDKSetup.rewardedAdsMediationType == adsMediationType ? m_SDKSetup.maxAdsSetup.RewardedAdUnitID : "";
            
            maxMediationController.m_MaxAdConfig.BannerAdUnitID = m_SDKSetup.bannerAdsMediationType == adsMediationType ? m_SDKSetup.maxAdsSetup.BannerAdUnitID : "";
#if UNITY_AD_MAX
            maxMediationController.m_BannerPosition = m_SDKSetup.maxBannerAdsPosition;
#endif
            
            maxMediationController.m_MaxAdConfig.CollapsibleBannerAdUnitID = m_SDKSetup.collapsibleBannerAdsMediationType == adsMediationType ? m_SDKSetup.maxAdsSetup.CollapsibleBannerAdUnitID : "";
            
            maxMediationController.m_MaxAdConfig.MrecAdUnitID = m_SDKSetup.mrecAdsMediationType == adsMediationType ? m_SDKSetup.maxAdsSetup.MrecAdUnitID : "";
            
            maxMediationController.m_MaxAdConfig.AppOpenAdUnitID = m_SDKSetup.appOpenAdsMediationType == adsMediationType ? m_SDKSetup.maxAdsSetup.AppOpenAdUnitID : "";
            
#if UNITY_EDITOR
            EditorUtility.SetDirty(maxMediationController);
            Debug.Log("Update Max Mediation Done");
#endif
#endif
        }
        private void UpdateAdmobMediation()
        {
#if UNITY_AD_ADMOB
            const AdsMediationType adsMediationType = AdsMediationType.ADMOB;
            AdmobMediationController admobMediationController =
                GetAdsMediationController(adsMediationType) as AdmobMediationController;
            if (admobMediationController == null) return;
            if (m_SDKSetup.interstitialAdsMediationType == adsMediationType)
            {
                m_MainAdsMediationType = adsMediationType;
                admobMediationController.m_AdmobAdSetup.InterstitialAdUnitIDList = m_SDKSetup.admobAdsSetup.InterstitialAdUnitIDList;
            }
            else
            {
                admobMediationController.m_AdmobAdSetup.InterstitialAdUnitIDList = new List<string>();
            }
            admobMediationController.m_AdmobAdSetup.RewardedAdUnitIDList = m_SDKSetup.rewardedAdsMediationType == adsMediationType ? m_SDKSetup.admobAdsSetup.RewardedAdUnitIDList : new List<string>();

            {
                admobMediationController.m_AdmobAdSetup.BannerAdUnitIDList =
                    m_SDKSetup.bannerAdsMediationType == adsMediationType
                        ? m_SDKSetup.admobAdsSetup.BannerAdUnitIDList
                        : new List<string>();
                admobMediationController.IsBannerShowingOnStart = m_SDKSetup.isBannerShowingOnStart;
                admobMediationController.m_BannerPosition = m_SDKSetup.admobBannerAdsPosition;
            }

            {
                admobMediationController.m_AdmobAdSetup.CollapsibleBannerAdUnitIDList =
                    m_SDKSetup.collapsibleBannerAdsMediationType == adsMediationType
                        ? m_SDKSetup.admobAdsSetup.CollapsibleBannerAdUnitIDList
                        : new List<string>();
                admobMediationController.IsCollapsibleBannerShowingOnStart = m_SDKSetup.isShowingOnStartCollapsibleBanner;
                IsAutoCloseCollapsibleBanner = m_SDKSetup.isAutoCloseCollapsibleBanner;
                m_AutoCloseTimeCollapsibleBanner = m_SDKSetup.autoCloseTime;

                IsAutoRefreshCollapsibleBanner = m_SDKSetup.isAutoRefreshCollapsibleBanner;
                IsAutoRefreshExtendCollapsibleBanner = m_SDKSetup.isAutoRefreshExtendCollapsibleBanner;
                m_AutoRefreshTimeCollapsibleBanner = m_SDKSetup.autoRefreshTime;
                
                admobMediationController.m_CollapsibleBannerPosition = m_SDKSetup.adsPositionCollapsibleBanner;
            }
            admobMediationController.m_AdmobAdSetup.MrecAdUnitIDList = m_SDKSetup.mrecAdsMediationType == adsMediationType ? m_SDKSetup.admobAdsSetup.MrecAdUnitIDList : new List<string>();
            admobMediationController.m_AdmobAdSetup.AppOpenAdUnitIDList = m_SDKSetup.appOpenAdsMediationType == adsMediationType ? m_SDKSetup.admobAdsSetup.AppOpenAdUnitIDList : new List<string>();
#if UNITY_EDITOR
            EditorUtility.SetDirty(admobMediationController);
            Debug.Log("Update Admob Mediation Done");
#endif
#endif
        }

        #endregion

        #region Interstitial
        private AdsConfig InterstitialAdsConfig => GetAdsConfig(AdsType.INTERSTITIAL);

        private UnityAction m_InterstitialAdCloseCallback;
        private UnityAction m_InterstitialAdLoadSuccessCallback;
        private UnityAction m_InterstitialAdLoadFailCallback;
        private UnityAction m_InterstitialAdShowSuccessCallback;
        private UnityAction m_InterstitialAdShowFailCallback;

        private void SetupInterstitial(AdsMediationType adsMediationType)
        {
            if (adsMediationType != m_SDKSetup.interstitialAdsMediationType) return;
            if (IsRemoveAds)return;
            Debug.Log("Setup Interstitial");
            InterstitialAdsConfig.isActive = m_SDKSetup.IsActiveAdsType(AdsType.INTERSTITIAL);
            if (!m_SDKSetup.IsActiveAdsType(AdsType.INTERSTITIAL)) return;
            foreach (AdsMediationController t in InterstitialAdsConfig.adsMediations)
            {
                t.InitInterstitialAd(
                    OnInterstitialAdClosed,
                    OnInterstitialAdSuccessToLoad,
                    OnInterstitialAdFailedToLoad,
                    OnInterstitialAdShowSuccess,
                    OnInterstitialAdShowFail
                );
            }

            Debug.Log("Setup Interstitial Done");
        }

        private void RequestInterstitial()
        {
            if (GetSelectedMediation(AdsType.INTERSTITIAL).IsInterstitialLoaded()) return;
#if !UNITY_EDITOR
            GetSelectedMediation(AdsType.INTERSTITIAL).RequestInterstitialAd();
#endif
        }
        public void ShowInterstitial(UnityAction closedCallback = null, UnityAction showSuccessCallback = null,
            bool isTracking = true, bool isSkipCapping = false)
        {
            if (IsCheatAds)
            {
                showSuccessCallback?.Invoke();
                return;
            }
            if (!isSkipCapping)
            {
                if (m_InterstitialCappingAdsCooldown > 0) return;
            }

            m_InterstitialAdCloseCallback = closedCallback;
            if (isTracking)
            {
                ABIAnalyticsManager.Instance.TrackAdsInterstitial_ClickOnButton();
            }

            if (!IsRemoveAds)
            {
                if (IsInterstitialAdLoaded())
                {
                    ShowSelectedInterstitialAd(showSuccessCallback);
                }
            }
            else
            {
                m_InterstitialAdCloseCallback?.Invoke();
                showSuccessCallback?.Invoke();
            }
        }

        private void ShowSelectedInterstitialAd(UnityAction showSuccessCallback)
        {
            m_InterstitialAdShowSuccessCallback = showSuccessCallback;
            GetSelectedMediation(AdsType.INTERSTITIAL).ShowInterstitialAd();
        }

        public bool IsInterstitialAdLoaded()
        {
            bool isInterstitialAdLoaded = GetSelectedMediation(AdsType.INTERSTITIAL).IsInterstitialLoaded();
            return isInterstitialAdLoaded;
        }

        private void ResetAdsLoadingCooldown()
        {
            m_AdsLoadingCooldown = m_MaxLoadingCooldown;
        }

        private void ResetAdsInterstitialCappingTime()
        {
            m_InterstitialCappingAdsCooldown = m_MaxInterstitialCappingTimeDay1;
        }

        private void OnInterstitialAdSuccessToLoad()
        {
            InterstitialAdsConfig.RefreshLoadAds();
            m_InterstitialAdLoadSuccessCallback?.Invoke();
            ABIAnalyticsManager.Instance.TrackAdsInterstitial_LoadedSuccess();
        }

        private void OnInterstitialAdFailedToLoad()
        {
            MarkShowingAds(false);
            ResetAdsLoadingCooldown();
            m_InterstitialAdLoadFailCallback?.Invoke();
        }

        private void OnInterstitialAdShowSuccess()
        {
            MarkShowingAds(true);
            m_InterstitialAdShowSuccessCallback?.Invoke();
            ABIAnalyticsManager.Instance.TrackAdsInterstitial_ShowSuccess();
        }

        private void OnInterstitialAdShowFail()
        {
            InterstitialAdsConfig.MarkReloadFail();
            m_InterstitialAdShowFailCallback?.Invoke();
            ABIAnalyticsManager.Instance.TrackAdsInterstitial_ShowFail();
        }

        private void OnInterstitialAdClosed()
        {
            RequestInterstitial();
            ResetAdsInterstitialCappingTime();
            m_InterstitialAdCloseCallback?.Invoke();
            MarkShowingAds(false);
        }

        public bool IsReadyToShowInterstitial()
        {
            return IsInterstitialAdLoaded() && m_InterstitialCappingAdsCooldown <= 0;
        }

        #endregion

        #region Banner Ads

        private AdsConfig BannerAdsConfig => GetAdsConfig(AdsType.BANNER);
        public float BannerCountTime { get; private set; }
        private const float banner_reset_time = 15f;
        private bool m_IsBannerShowing;

        private void UpdateBanner()
        {
            if (IsRemoveAds) return;
            if (!BannerAdsConfig.isActive) return;
            if(!m_IsBannerShowing)return;
            BannerCountTime += Time.deltaTime;
            if (BannerCountTime >= banner_reset_time)
            {
                BannerCountTime = 0;
                DestroyBanner();
                RequestBanner();
            }
        }

        private void SetupBannerAds(AdsMediationType adsMediationType)
        {
            if (adsMediationType != m_SDKSetup.bannerAdsMediationType) return;
            if (IsCheatAds || IsRemoveAds) return;
            Debug.Log("Setup Banner");
            BannerAdsConfig.isActive = m_SDKSetup.IsActiveAdsType(AdsType.BANNER);
            if (!m_SDKSetup.IsActiveAdsType(AdsType.BANNER)) return;
            foreach (AdsMediationController t in BannerAdsConfig.adsMediations)
            {
                t.InitBannerAds(OnBannerLoadedSucess, OnBannerLoadedFail, OnBannerCollapsed, OnBannerExpanded);
            }

            Debug.Log("Setup Banner Done");
        }

        public bool IsBannerShowing()
        {
            return m_IsBannerShowing;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void RequestBanner()
        {
            if (!BannerAdsConfig.isActive) return;
            GetSelectedMediation(AdsType.BANNER).RequestBannerAds();
        }

        public void ShowBannerAds()
        {
            Debug.Log(("Call Show Banner Ads"));
            if (IsCheatAds || IsRemoveAds) return;
            GetSelectedMediation(AdsType.BANNER)?.ShowBannerAds();
            m_IsBannerShowing = true;
            BannerCountTime = 0;
        }

        public void HideBannerAds()
        {
            GetSelectedMediation(AdsType.BANNER)?.HideBannerAds();
            m_IsBannerShowing = false;
        }

        public void DestroyBanner()
        {
            GetSelectedMediation(AdsType.BANNER)?.DestroyBannerAds();
        }

        public bool IsBannerLoaded()
        {
            AdsMediationController mediation = GetSelectedMediation(AdsType.BANNER);
            return mediation != null && mediation.IsBannerLoaded();
        }

        private void OnBannerLoadedSucess()
        {
            Debug.Log("Banner Loaded");
            BannerCountTime = 0;
        }

        private void OnBannerLoadedFail()
        {
            Debug.Log("Banner Load Fail");
            BannerCountTime = 0;
        }

        private void OnBannerExpanded()
        {
            Debug.Log("Banner Expanded");
        }

        private void OnBannerCollapsed()
        {
            Debug.Log("Banner Collapsed");
        }

        #endregion
        
        #region Collapsible Banner

        private AdsConfig CollapsibleBannerAdsConfig => GetAdsConfig(AdsType.COLLAPSIBLE_BANNER);
        private bool IsExpandedCollapsibleBanner;
        private bool IsShowingCollapsibleBanner;
        
        [BoxGroup("Collapsible Banner")]public bool IsAutoRefreshCollapsibleBanner;
        [BoxGroup("Collapsible Banner")]public bool IsAutoRefreshExtendCollapsibleBanner;
        [BoxGroup("Collapsible Banner")]public float m_AutoRefreshTimeCollapsibleBanner;
        private float m_RefreshTimeCounterCollapsibleBanner;
        
        [BoxGroup("Collapsible Banner")]public bool IsAutoCloseCollapsibleBanner;
        [BoxGroup("Collapsible Banner")]public float m_AutoCloseTimeCollapsibleBanner = 20;
        private float m_CloseTimeCounterCollapsibleBanner;
        
        private UnityAction m_CollapsibleBannerCloseCallback;
        
        private void SetupCollapsibleBannerAds(AdsMediationType adsMediationType)
        {
            StartCoroutine(coDelayInitCollapsibleBannerAds(adsMediationType));
        }
        IEnumerator coDelayInitCollapsibleBannerAds(AdsMediationType adsMediationType)
        {
            yield return new WaitForSeconds(5);
            SetupCollapsibleBannerAdMediation(adsMediationType);
        }
        private void SetupCollapsibleBannerAdMediation(AdsMediationType adsMediationType)
        {
            if (IsCheatAds || IsRemoveAds) return;
            if (adsMediationType != m_SDKSetup.collapsibleBannerAdsMediationType) return;
            Debug.Log("Setup Banner");
            CollapsibleBannerAdsConfig.isActive = m_SDKSetup.IsActiveAdsType(AdsType.COLLAPSIBLE_BANNER);
            if (!m_SDKSetup.IsActiveAdsType(AdsType.COLLAPSIBLE_BANNER)) return;
            foreach (AdsMediationController t in CollapsibleBannerAdsConfig.adsMediations)
            {
                t.InitCollapsibleBannerAds(
                    OnCollapsibleBannerLoadedSucess, OnCollapsibleBannerLoadedFail, OnCollapsibleBannerCollapsed, 
                    OnCollapsibleBannerExpanded, OnCollapsibleBannerDestroyed, OnCollapsibleBannerHide);
            }

            Debug.Log("Setup Banner Done");
        }

        public bool IsCollapsibleBannerExpended()
        {
            return IsExpandedCollapsibleBanner;
        }
        public bool IsCollapsibleBannerShowing()
        {
            return IsShowingCollapsibleBanner;
        }
        private void UpdateCollapsibleBanner(float dt)
        {
            if (IsRemoveAds) return;
            if (IsAutoCloseCollapsibleBanner)
            {
                if (m_CloseTimeCounterCollapsibleBanner > 0)
                {
                    m_CloseTimeCounterCollapsibleBanner -= dt;
                    if (m_CloseTimeCounterCollapsibleBanner <= 0)
                    {
                        HideCollapsibleBannerAds();
                        m_CollapsibleBannerCloseCallback?.Invoke();
                    }
                }
            }

            if (IsAutoRefreshCollapsibleBanner)
            {
                if (m_RefreshTimeCounterCollapsibleBanner > 0)
                {
                    m_RefreshTimeCounterCollapsibleBanner -= dt;
                    if (m_RefreshTimeCounterCollapsibleBanner <= 0)
                    {
                        if (IsAutoRefreshExtendCollapsibleBanner)
                        {
                            ShowCollapsibleBannerAds();
                        }
                        else
                        {
                            RefreshCollapsibleBanner();
                        }

                        m_RefreshTimeCounterCollapsibleBanner = 0;
                    }
                }
            }
        }
        // ReSharper disable Unity.PerformanceAnalysis
        public void RequestCollapsibleBanner()
        {
            if (!CollapsibleBannerAdsConfig.isActive || IsRemoveAds) return;
            GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER)?.RequestCollapsibleBannerAds(false);
        }
        public void RefreshCollapsibleBanner()
        {
            if (!CollapsibleBannerAdsConfig.isActive || IsRemoveAds) return;
            GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER)?.RefreshCollapsibleBannerAds();
        }
        public void ShowCollapsibleBannerAds(bool isAutoClose = false, UnityAction closeCallback = null)
        {
            Debug.Log(("Call Show Collapsible Banner Ads"));
            if (IsCheatAds || IsRemoveAds) return;
            if(GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER) == null) return;
            IsAutoCloseCollapsibleBanner = isAutoClose;
            m_CollapsibleBannerCloseCallback = closeCallback;
            m_RefreshTimeCounterCollapsibleBanner = 0;
            GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER).ShowCollapsibleBannerAds();
        }
        public void HideCollapsibleBannerAds()
        {
            GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER)?.HideCollapsibleBannerAds();
        }
        public void DestroyCollapsibleBanner()
        {
            GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER)?.DestroyCollapsibleBannerAds();
            IsShowingCollapsibleBanner = false;
        }
        public bool IsCollapsibleBannerLoaded()
        {
            AdsMediationController mediation = GetSelectedMediation(AdsType.COLLAPSIBLE_BANNER);
            return mediation != null && mediation.IsCollapsibleBannerLoaded();
        }
        private void OnCollapsibleBannerLoadedSucess()
        {
            Debug.Log("Collapsible Banner Loaded");
            m_RefreshTimeCounterCollapsibleBanner = m_AutoRefreshTimeCollapsibleBanner;
        }
        private void OnCollapsibleBannerLoadedFail()
        {
            Debug.Log("Collapsible Banner Load Fail");
        }
        private void OnCollapsibleBannerExpanded()
        {
            Debug.Log("Collapsible Banner Expanded");
            IsExpandedCollapsibleBanner = true;
            IsShowingCollapsibleBanner = true;
            m_RefreshTimeCounterCollapsibleBanner = 0;
        }
        private void OnCollapsibleBannerCollapsed()
        {
            Debug.Log("Collapsible Banner Collapsed");
            IsExpandedCollapsibleBanner = false;
            m_CloseTimeCounterCollapsibleBanner = m_AutoCloseTimeCollapsibleBanner;
            m_RefreshTimeCounterCollapsibleBanner = m_AutoRefreshTimeCollapsibleBanner;
        }
        private void OnCollapsibleBannerDestroyed()
        {
            Debug.Log("Collapsible Banner Destroyed");
            IsShowingCollapsibleBanner = false;
        }
        private void OnCollapsibleBannerHide()
        {
            Debug.Log("Collapsible Banner Hide");
            IsShowingCollapsibleBanner = false;
        }
        public bool IsCollapsibleBannerShowingTimeOut()
        {
            return m_CloseTimeCounterCollapsibleBanner <= 0;
        }
        #endregion

        #region Reward Ads

        private AdsConfig RewardVideoAdsConfig => GetAdsConfig(AdsType.REWARDED);
        
        private UnityAction m_RewardedVideoCloseCallback;
        private UnityAction m_RewardedVideoLoadSuccessCallback;
        private UnityAction m_RewardedVideoLoadFailedCallback;
        private UnityAction m_RewardedVideoEarnSuccessCallback;
        private UnityAction m_RewardedVideoShowStartCallback;
        private UnityAction m_RewardedVideoShowFailCallback;

        private string m_RewardedPlacement;

        // Reward Video Setup
        private void SetupRewardVideo(AdsMediationType adsMediationType)
        {
            if(IsRemoveAds && IsLinkRewardWithRemoveAds) return;
            if(adsMediationType != m_SDKSetup.rewardedAdsMediationType) return;
            Debug.Log("Setup Reward Video");
            RewardVideoAdsConfig.isActive = m_SDKSetup.IsActiveAdsType(AdsType.REWARDED);
            if (!m_SDKSetup.IsActiveAdsType(AdsType.REWARDED)) return;
            foreach (AdsMediationController t in RewardVideoAdsConfig.adsMediations)
            {
                t.InitRewardVideoAd(
                    OnRewardVideoClosed,
                    OnRewardVideoLoadSuccess,
                    OnRewardVideoLoadFail,
                    OnRewardVideoStart
                );
            }

            Debug.Log("Setup Reward Video Done");
        }

        public void RequestRewardVideo()
        {
            if (IsRemoveAds && IsLinkRewardWithRemoveAds) return;
            if (GetSelectedMediation(AdsType.REWARDED).IsRewardVideoLoaded()) return;
            GetSelectedMediation(AdsType.REWARDED).RequestRewardVideoAd();
        }

        public void ShowRewardVideo(string rewardedPlacement, UnityAction successCallback,
            UnityAction failedCallback = null)
        {
            if (IsCheatAds)
            {
                successCallback?.Invoke();
                return;
            }
            m_RewardedPlacement = rewardedPlacement;
            m_RewardedVideoEarnSuccessCallback = successCallback;
            m_RewardedVideoShowFailCallback = failedCallback;
            ABIAnalyticsManager.Instance.TrackAdsReward_ClickOnButton();
            if (IsRemoveAds && IsLinkRewardWithRemoveAds)
            {
                OnRewardVideoEarnSuccess();
            }
            else
            {
                if (m_IsActiveInterruptReward && IsReadyToShowRewardInterrupt() && IsInterstitialAdLoaded())
                {
                    MarkShowingAds(true);
                    ShowInterstitial(null, () =>
                    {
                        successCallback();
                        ResetRewardInterruptCount();
                    }, false, true);
                }
                else
                {
                    if (IsReadyToShowRewardVideo())
                    {
                        MarkShowingAds(true);
                        GetSelectedMediation(AdsType.REWARDED)
                            .ShowRewardVideoAd(OnRewardVideoEarnSuccess, OnRewardVideoShowFail);
                    }
                }
            }
        }

        public bool IsRewardVideoLoaded()
        {
            return GetSelectedMediation(AdsType.REWARDED).IsRewardVideoLoaded();
        }

        private void OnRewardVideoEarnSuccess()
        {
            m_RewardedVideoEarnSuccessCallback?.Invoke();
            m_RewardInterruptCountTime++;
            ABIAnalyticsManager.Instance.TrackAdsReward_ShowCompleted(m_RewardedPlacement);
        }

        private void OnRewardVideoStart()
        {
            if (m_RewardedVideoShowStartCallback != null)
            {
                m_RewardedVideoShowStartCallback();
            }

            ABIAnalyticsManager.Instance.TrackAdsReward_StartShow();
            MarkShowingAds(true);
        }

        private void OnRewardVideoShowFail()
        {
            if (m_RewardedVideoShowFailCallback != null)
            {
                m_RewardedVideoShowFailCallback();
            }

            ABIAnalyticsManager.Instance.TrackAdsReward_ShowFail();
        }

        private void OnRewardVideoClosed()
        {
            ResetAdsInterstitialCappingTime();
            RequestRewardVideo();
            if (m_RewardedVideoCloseCallback != null)
            {
                m_RewardedVideoCloseCallback();
            }

            MarkShowingAds(false);
        }

        private void OnRewardVideoLoadSuccess()
        {
            RewardVideoAdsConfig.RefreshLoadAds();
            if (m_RewardedVideoLoadSuccessCallback != null)
            {
                m_RewardedVideoLoadSuccessCallback();
            }

            ABIAnalyticsManager.Instance.TrackAdsReward_LoadSuccess();
        }

        private void OnRewardVideoLoadFail()
        {
            ResetAdsLoadingCooldown();
            RewardVideoAdsConfig.MarkReloadFail();
            if (m_RewardedVideoLoadFailedCallback != null)
            {
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

        public void ResetRewardInterruptCount()
        {
            m_RewardInterruptCountTime = 0;
        }

        #endregion Reward Ads

        #region MRec Ads

        private AdsConfig MRecAdsConfig => GetAdsConfig(AdsType.MREC);
        private UnityAction m_MRecAdLoadedCallback;
        private UnityAction m_MRecAdLoadFailCallback;
        private UnityAction m_MRecAdClickedCallback;
        private UnityAction m_MRecAdExpandedCallback;
        private UnityAction m_MRecAdCollapsedCallback;
        private bool m_IsMRecShowing;

        private void SetupMRecAds(AdsMediationType adsMediationType)
        {
            if (IsRemoveAds) return;
            if (adsMediationType != m_SDKSetup.mrecAdsMediationType) return;
            Debug.Log("Setup MREC");
            MRecAdsConfig.isActive = m_SDKSetup.IsActiveAdsType(AdsType.MREC);
            if (!m_SDKSetup.IsActiveAdsType(AdsType.MREC)) return;
            foreach (AdsMediationController t in MRecAdsConfig.adsMediations)
            {
                t.InitRMecAds(OnMRecAdLoadedEvent, OnMRecAdLoadFailedEvent, OnMRecAdClickedEvent, OnMRecAdExpandedEvent,
                    OnMRecAdCollapsedEvent);
            }

            Debug.Log("Setup MREC Done");
        }

        public bool IsMRecShowing()
        {
            return m_IsMRecShowing;
        }

        public bool IsMRecLoaded()
        {
            return GetSelectedMediation(AdsType.MREC) != null && GetSelectedMediation(AdsType.MREC).IsMRecLoaded();
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
            if (IsCheatAds || IsRemoveAds) return;
            if (!m_SDKSetup.IsActiveAdsType(AdsType.MREC)) return;
            GetSelectedMediation(AdsType.MREC)?.ShowMRecAds();
            HideBannerAds();
        }

        public void HideMRecAds()
        {
            if (IsCheatAds) return;
            GetSelectedMediation(AdsType.MREC).HideMRecAds();
        }

        #endregion

        #region App Open Ads

        private AdsConfig AppOpenAdsConfig => GetAdsConfig(AdsType.APP_OPEN);
        private bool m_IsActiveAoaByRemoteConfig = true;
        private bool m_IsActiveShowAdsFirstTime = true;
        private bool m_IsDoneShowAdsFirstTime = false;
        private double m_AoaTimeBetweenShow = 0;
        private double m_AoaPauseTimeNeedToShowAds = 5;
        private DateTime m_CloseAdsTime = DateTime.Now;
        private DateTime m_StartPauseTime = DateTime.Now;
        private bool m_IsShowingAds;

        private bool IsShowingAds
        {
            get => m_IsShowingAds;
            set
            {
                m_IsShowingAds = value;
                Debug.Log("Set Showing Ads = " + value);
            } 
        }

        private void SetupAppOpenAds(AdsMediationType adsMediationType)
        {
            if(IsCheatAds || IsRemoveAds)return;
            if (adsMediationType != m_SDKSetup.appOpenAdsMediationType) return;
            Debug.Log("Setup App Open Ads");
            AppOpenAdsConfig.isActive = m_SDKSetup.IsActiveAdsType(AdsType.APP_OPEN);
            if (!m_SDKSetup.IsActiveAdsType(AdsType.APP_OPEN)) return;
            foreach (AdsMediationController t in AppOpenAdsConfig.adsMediations)
            {
                t.InitAppOpenAds(OnAppOpenAdLoadedEvent, OnAppOpenAdLoadFailedEvent, OnAppOpenAdClosedEvent,
                    OnAppOpenAdDisplayedEvent, OnAppOpenAdFailedToDisplayEvent);
            }

            ShowAdsFirstTime();
            Debug.Log("Setup App Open Ads Done");
        }

        private void ShowAppOpenAds()
        {
            if (IsCheatAds || IsRemoveAds)return;
            if (IsAppOpenAdsReady())
            {
                Debug.Log("Start Show App Open Ads");
                MarkShowingAds(true);
                GetSelectedMediation(AdsType.APP_OPEN).ShowAppOpenAds();
            }
        }
        private void DelayShowAppOpenAds()
        {
            StartCoroutine(coDelayShowAppOpenAds());
        }
        IEnumerator coDelayShowAppOpenAds()
        {
            yield return new WaitForSeconds(0.3f);
            ShowAppOpenAds();
        }
        
        private void ForceShowAppOpenAds()
        {
            if (IsCheatAds || IsRemoveAds) return;
            if (IsAppOpenAdsLoaded())
            {
                MarkShowingAds(true);
                Debug.Log("Start Force Show App Open Ads");
                GetSelectedMediation(AdsType.APP_OPEN).ShowAppOpenAds();
            }
        }

        private void RequestAppOpenAds()
        {
            if(IsRemoveAds)return;
            GetSelectedMediation(AdsType.APP_OPEN).RequestAppOpenAds();
        }

        private bool IsAppOpenAdsReady()
        {
            if (GetSelectedMediation(AdsType.APP_OPEN) == null) return false;
            Debug.Log("Status " + GetSelectedMediation(AdsType.APP_OPEN)?.IsAppOpenAdsLoaded() + " Remote= " +
                      m_IsActiveAoaByRemoteConfig + " AdsConfig=" + AppOpenAdsConfig.isActive + " Time=" +
                      (DateTime.Now - m_StartPauseTime).TotalSeconds + " Need=" + m_AoaPauseTimeNeedToShowAds
                      + " IsShowingAds=" + IsShowingAds + " Close Time=" +
                      (DateTime.Now - m_CloseAdsTime).TotalSeconds + " Need=" + m_AoaTimeBetweenShow);
            return IsActiveAppOpenAds() && IsAppOpenAdsLoaded();
        }

        private bool IsActiveAppOpenAds()
        {
            if (!m_IsActiveAoaByRemoteConfig) return false;
            if (IsShowingAds) return false;
            float totalTimeBetweenShow = (float) (DateTime.Now - m_CloseAdsTime).TotalSeconds;
            Debug.Log("Total Time Between Show = " + totalTimeBetweenShow + " Need = " + m_AoaTimeBetweenShow);
            return !(totalTimeBetweenShow < m_AoaTimeBetweenShow);
        }

        private bool IsAppOpenAdsLoaded()
        {
            return GetSelectedMediation(AdsType.APP_OPEN) != null &&
                   GetSelectedMediation(AdsType.APP_OPEN).IsAppOpenAdsLoaded();
        }

        private void UpdateAOARemoteConfig()
        {
            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_aoa_active);
                m_IsActiveAoaByRemoteConfig = configValue.BooleanValue;
                Debug.Log("AOA active = " + m_IsActiveAoaByRemoteConfig);
            }

            {
                ConfigValue configValue =
                    ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_aoa_show_first_time_active);
                m_IsActiveShowAdsFirstTime = configValue.BooleanValue;
                Debug.Log("AOA active show first time = " + m_IsActiveShowAdsFirstTime);
            }

            {
                ConfigValue configValue =
                    ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_aoa_time_between_step_load);
                m_AoaTimeBetweenShow = configValue.DoubleValue;
                Debug.Log("AOA Load time = " + m_AoaTimeBetweenShow);
            }

            {
                ConfigValue configValue =
                    ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_aoa_pause_time_need_to_show_ads);
                m_AoaPauseTimeNeedToShowAds = configValue.DoubleValue;
                Debug.Log("AOA Pause time = " + m_AoaPauseTimeNeedToShowAds);
            }
        }

        private void ShowAdsFirstTime()
        {
            Debug.Log("---------------------------------Show Ads Fisrt Time--------------------------");
            StartCoroutine(coWaitFechingSuccessAndShow());
        }

        IEnumerator coWaitFechingSuccessAndShow()
        {
            if (m_IsDoneShowAdsFirstTime) yield break;
            while (!m_IsUpdateRemoteConfigSuccess)
            {
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
                EventManager.AddEventNextFrame(() => { StartCoroutine(coWaitingMarkShowingAdsDone()); });
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
            MarkShowingAds(false);
            m_CloseAdsTime = DateTime.Now;
            RequestAppOpenAds();
        }

        private void OnAppOpenAdDisplayedEvent()
        {
            Debug.Log("AdsManager Displayed app open ad");
            MarkShowingAds(true);
        }

        private void OnAppOpenAdFailedToDisplayEvent()
        {
            Debug.Log("AdsManager Failed to display app open ad");
            MarkShowingAds(false);
        }

        #endregion

        private void OnAdRevenuePaidEvent(ImpressionData impressionData)
        {
            Debug.Log("Paid Ad Revenue - Ads Type = " + impressionData.ad_type);
            ABIAnalyticsManager.TrackAdImpression(impressionData);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackAppsflyerAdRevenue(impressionData);
#endif
        }
        private void OnApplicationPause(bool paused)
        {
            Debug.Log("OnApplicationPause " + paused + " Remote=" + m_IsActiveAoaByRemoteConfig + " AdsConfig=" +
                      AppOpenAdsConfig.isActive + " Time=" + (DateTime.Now - m_StartPauseTime).TotalSeconds +
                      " Need=" + m_AoaPauseTimeNeedToShowAds);
            if (!m_IsActiveAoaByRemoteConfig || !AppOpenAdsConfig.isActive) return;
            switch (paused)
            {
                case true:
                    m_StartPauseTime = DateTime.Now;
                    break;
                case false when (DateTime.Now - m_StartPauseTime).TotalSeconds > m_AoaPauseTimeNeedToShowAds:
                    DelayShowAppOpenAds();
                    break;
            }
        }
    }
    [System.Serializable]
    public class UUID
    {
        public string uuid;

        public static string Generate()
        {
            UUID newUuid = new UUID {uuid = System.Guid.NewGuid().ToString()};
            return newUuid.uuid;
        }
    }

}