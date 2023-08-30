using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Events;
using Firebase.RemoteConfig;

namespace SDK {
    public enum AdsMediationType {
        MAX,
        ADMOB,
        IRONSOURCE
    }
    public enum AdsType {
        BANNER,
        INTERSTITIAL,
        REWARDED
    }
    public enum WatchVideoRewardType {

        NONE,
    }
    [Serializable]
    public class AdsConfig {
        public AdsType adsType;
        public AdsMediationType adsMediationType = AdsMediationType.MAX;
        public bool isActive = true;
        private int adsReloadTime = 0;
        public int maxReloadTime = 3;
        private int currentAdsID = 0;
        public List<AdsMediationController> adsMediations = new List<AdsMediationController>();

        public void Init() {
            int num = 0;
            while (num < adsMediations.Count) {
                AdsMediationController ads = adsMediations[num];
                if (!ads.IsActive(adsType)) {
                    adsMediations.RemoveAt(num);
                    continue;
                }
                num++;
            }
        }
        public void RefreshLoadAds() {
            adsReloadTime = 0;
        }
        public void MarkReloadFail() {
            adsReloadTime++;
            if (IsGoodToChangeAds()) {
                adsReloadTime = 0;
                currentAdsID++;
                if (currentAdsID >= adsMediations.Count) {
                    currentAdsID = 0;
                }
            }
        }
        public bool IsGoodToChangeAds() {
            return adsReloadTime >= maxReloadTime;
        }
        public AdsMediationController GetAdsMediation() {
            return adsMediations[currentAdsID];
        }
        public AdsMediationController GetAdsMediation(AdsMediationType adsType) {
            for (int i = 0; i < adsMediations.Count; i++) {
                AdsMediationController adsMediationController = adsMediations[i];
                if (adsMediationController.GetAdsMediationType() == adsType) {
                    return adsMediationController;
                }
            }
            return adsMediations[0];
        }
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

        private double m_AdsLoadingCooldown = 0f;
        private double m_MaxLoadingCooldown = 5f;

        private double m_InterstitialCappingAdsCooldown = 300;
        private double m_MaxInterstitialCappingAdsTime = 300;
        private int m_RewardInterruptCountTime = 0;
        private int m_MaxRewardInterruptCount = 6;
        private bool IsActiveInterruptReward = false;

        public AdsConfig m_RewardVideoAdsConfig;
        public AdsConfig m_InterstitialAdsConfig;
        public AdsConfig m_BannerAdsConfig;

        public UnityAction m_InterstitialAdCloseCallback;
        public UnityAction m_InterstitialAdLoadSuccessCallback;
        public UnityAction m_InterstitialAdLoadFailCallback;
        public UnityAction m_InterstitialAdShowSuccessCallback;
        public UnityAction m_InterstitialAdShowFailCallback;

        public UnityAction m_RewardedVideoCloseCallback;
        public UnityAction m_RewardedVideoLoadSuccessCallback;
        public UnityAction m_RewardedVideoLoadFailedCallback;
        public UnityAction m_RewardedVideoEarnSuccessCallback;
        public UnityAction m_RewardedVideoShowStartCallback;
        public UnityAction m_RewardedVideoShowFailCallback;

        private string m_RewardedPlacement;


        private void Awake() {
            if (m_Instance != null) {
                Destroy(gameObject);
                return;
            }
            m_Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("ADD Listner ADSMANAGER");
            EventManager.StartListening("UpdateRemoteConfigs", UpdateRemoteConfigs);
            IsActiveInterruptReward = true;
        }
        private void Start() {
            UpdateAdsMediation();
            ResetAdsInterstitialCappingTime();
        }
        private void UpdateRemoteConfigs() {
            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_interstital_rate_time);
                m_MaxInterstitialCappingAdsTime = configValue.DoubleValue;
                Debug.Log("=============== MAX " + m_MaxInterstitialCappingAdsTime);
                ResetAdsInterstitialCappingTime();
            }
            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_inter_reward_interspersed);
                IsActiveInterruptReward = configValue.BooleanValue;
                Debug.Log("=============== Active " + IsActiveInterruptReward);
            }
            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_inter_reward_interspersed_time);
                m_MaxRewardInterruptCount = (int)configValue.DoubleValue;
                Debug.Log("=============== MAX " + m_MaxRewardInterruptCount);
            }
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

        public void UpdateAdsMediation() {
            m_InterstitialAdsConfig.Init();
            m_RewardVideoAdsConfig.Init();
            m_BannerAdsConfig.Init();

            InitAdsMediation();

            //Setup Interstitial
            SetupInterstitial();

            //Setup Reward Video
            SetupRewardVideo();

            //Setup Banner
            SetupBannerAds();
        }
        public void InitAdsMediation() {
            if (!GetSelectedInterstitialMediation().IsInited) {
                GetSelectedInterstitialMediation().Init();
            }
            if (!GetSelectedRewardVideosMediation().IsInited) {
                GetSelectedRewardVideosMediation().Init();
            }
        }
        public AdsMediationController GetSelectedInterstitialMediation() {
            return m_InterstitialAdsConfig.GetAdsMediation();
        }
        public AdsMediationController GetSelectedRewardVideosMediation() {
            return m_RewardVideoAdsConfig.GetAdsMediation();
        }
        public AdsMediationController GetSelectedBannerAdsMediation() {
            return m_BannerAdsConfig.GetAdsMediation();
        }

        #region Interstitial
        private void SetupInterstitial() {
            if (!m_InterstitialAdsConfig.isActive) return;
            for (int i = 0; i < m_InterstitialAdsConfig.adsMediations.Count; i++) {
                m_InterstitialAdsConfig.adsMediations[i].InitInterstitialAd(
                OnInterstitialAdClosed,
                OnInterstitialAdSuccessToLoad,
                OnInterstitialAdFailedToLoad,
                OnInterstitialAdShowSuccess,
                OnInterstitialAdShowFail
                );
            }
        }
        public void RequestInterstitial() {
            if (GetSelectedInterstitialMediation().IsInterstitialLoaded()) return;
#if !UNITY_EDITOR
        GetSelectedInterstitialMediation().RequestInterstitialAd();
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
            GetSelectedInterstitialMediation().ShowInterstitialAd();
        }
        public bool IsInterstitialAdLoaded() {
            bool isInterstitialAdLoaded = GetSelectedInterstitialMediation().IsInterstitialLoaded();
            return isInterstitialAdLoaded;
        }
        public void ResetAdsLoadingCooldown() {
            m_AdsLoadingCooldown = m_MaxLoadingCooldown;
        }
        public void ResetAdsInterstitialCappingTime() {
            m_InterstitialCappingAdsCooldown = m_MaxInterstitialCappingAdsTime;
        }
        private void OnInterstitialAdClosed() {
            RequestInterstitial();
            ResetAdsInterstitialCappingTime();
            if (m_InterstitialAdCloseCallback != null) {
                m_InterstitialAdCloseCallback();
            }
        }
        public void OnInterstitialAdSuccessToLoad() {
            m_InterstitialAdsConfig.RefreshLoadAds();
            if (m_InterstitialAdLoadSuccessCallback != null) {
                m_InterstitialAdLoadSuccessCallback();
            }
            ABIAnalyticsManager.Instance.TrackAdsInterstitial_LoadedSuccess();
        }
        private void OnInterstitialAdFailedToLoad() {
            ResetAdsLoadingCooldown();
            if (m_InterstitialAdLoadFailCallback != null) {
                m_InterstitialAdLoadFailCallback();
            }
        }
        private void OnInterstitialAdShowSuccess() {
            if (m_InterstitialAdShowSuccessCallback != null) {
                m_InterstitialAdShowSuccessCallback();
            }
            ABIAnalyticsManager.Instance.TrackAdsInterstitial_ShowSuccess();
        }
        private void OnInterstitialAdShowFail() {
            m_InterstitialAdsConfig.MarkReloadFail();
            if (m_InterstitialAdShowFailCallback != null) {
                m_InterstitialAdShowFailCallback();
            }
            ABIAnalyticsManager.Instance.TrackAdsInterstitial_ShowFail();
        }
        public bool IsReadyToShowInterstitial() {
            if (IsInterstitialAdLoaded() && m_InterstitialCappingAdsCooldown > 0) {
                return true;
            }
            return false;
        }
        #endregion

        #region Banner Ads
        private float m_BannerCountTime;
        private float m_BannerResetTime = 10f;
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
            if (!m_BannerAdsConfig.isActive) return;
            for (int i = 0; i < m_BannerAdsConfig.adsMediations.Count; i++) {
                m_BannerAdsConfig.adsMediations[i].InitBannerAds(OnBannerLoadedSucess, OnBannerLoadedFail);
            }
        }
        public void RequestBanner() {
            if (!m_BannerAdsConfig.isActive) return;
            GetSelectedBannerAdsMediation().RequestBannerAds();
        }
        public void ShowBannerAds() {
            GetSelectedBannerAdsMediation().ShowBannerAds();
        }
        public void HideBannerAds() {
            GetSelectedBannerAdsMediation().HideBannerAds();
        }
        public void DestroyBanner() {
            GetSelectedBannerAdsMediation().DestroyBannerAds();
        }
        public bool IsBannerLoaded() {
            return GetSelectedBannerAdsMediation().IsBannerLoaded();
        }
        private void OnBannerLoadedSucess() {
            m_BannerCountTime = 0;
        }
        private void OnBannerLoadedFail() { }
        #endregion

        #region Reward Ads
        // Reward Video Setup
        private void SetupRewardVideo() {
            if (!m_RewardVideoAdsConfig.isActive) return;
            for (int i = 0; i < m_RewardVideoAdsConfig.adsMediations.Count; i++) {
                m_RewardVideoAdsConfig.adsMediations[i].InitRewardVideoAd(
                OnRewardVideoClosed,
                OnRewardVideoLoadSuccess,
                OnRewardVideoLoadFail,
                OnRewardVideoStart
                );
            }
        }
        public void RequestRewardBasedVideo() {
            if (GetSelectedRewardVideosMediation().IsRewardVideoLoaded()) return;
            GetSelectedRewardVideosMediation().RequestRewardVideoAd();
        }
        public void ShowRewardVideo(string rewardedPlacement, UnityAction successCallback, UnityAction failedCallback = null) {
            if (IsCheatAds) {
                if (successCallback != null) successCallback();
                return;
            }
            m_RewardedPlacement = rewardedPlacement;
            m_RewardedVideoEarnSuccessCallback = successCallback;
            m_RewardedVideoShowFailCallback = failedCallback;
            if (IsActiveInterruptReward && IsReadyToShowRewardInterrupt() && IsInterstitialAdLoaded()) {
                ShowInterstitial(null, () => {
                    successCallback();
                    ResetRewardInterruptCount();
                }, false, true);
            } else {
                ABIAnalyticsManager.Instance.TrackAdsReward_ClickOnButton();
                if (IsReadyToShowRewardVideo()) {
                    GetSelectedRewardVideosMediation().ShowRewardVideoAd(OnRewardVideoEarnSuccess, OnRewardVideoShowFail);
                }
            }
        }
        public bool IsRewardVideoLoaded() {
            return GetSelectedRewardVideosMediation().IsRewardVideoLoaded();
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

        public bool IsReadyToShowRewardVideo() {
            if (IsRewardVideoLoaded()) {
                return true;
            }
            return false;
        }
        public bool IsReadyToShowRewardInterrupt() {
            if (m_RewardInterruptCountTime >= m_MaxRewardInterruptCount) {
                return true;
            } else {
                return false;
            }
        }
        public void ResetRewardInterruptCount() {
            m_RewardInterruptCountTime = 0;
        }
        #endregion Reward Ads

    }

    [Serializable]
    public class AdsID {
        public List<string> ids = new List<string>();
        private int currentID;
        public void ChangeID() {
            currentID++;
            if (currentID >= ids.Count) {
                currentID = 0;
            }
        }
        public void Refresh() {
            currentID = 0;
        }
        public string ID {
            get {
                if (ids.Count == 0) return "";
                return ids[currentID];
            }
        }
        public bool IsActive() {
            return ids.Count > 0;
        }
    }
}