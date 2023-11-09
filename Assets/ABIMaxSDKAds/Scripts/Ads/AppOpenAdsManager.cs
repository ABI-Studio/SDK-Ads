using System;
using System.Collections;
using Firebase.RemoteConfig;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;
using static MaxSdkCallbacks;

namespace SDK {
    [AddComponentMenu("GoogleMobileAds/Samples/AppOpenAdController")]
    public class AppOpenAdsManager : MonoBehaviour {
#if UNITY_ANDROID
        public string AD_UNIT_ID = "ca-app-pub-3940256099942544/3419835294";
#elif UNITY_IOS
        private string AD_UNIT_ID = "ca-app-pub-3940256099942544/5662855259";
#else
        private string  AD_UNIT_ID = "unexpected_platform";
#endif
        private AppOpenAd m_Ad;

        public bool m_IsActiveByInspector;
        private bool m_UpdateRemoteConfigSuccess;
        private bool m_IsShowingAd = false;
        public bool m_IsDoneShowAdsFirstTime { get; set; }

        private DateTime m_CloseAdsTime;
        private DateTime m_StartPauseTime;

        private bool m_IsActiveShowAdsFirstTime = true;
        private bool m_IsActiveByRemoteConfig = false;
        private double m_AoaTimeBetweenStep = 15;
        private double m_AoaPauseTimeNeedToShowAds = 15;

        private bool m_IsActiveByOtherSource;
        private Coroutine m_IgnoreShowAds;
        private DateTime m_ExpireTime;
        private bool IsAdAvailable {
            get {
                return m_Ad != null 
                    && (DateTime.UtcNow - m_CloseAdsTime).TotalSeconds > m_AoaTimeBetweenStep 
                    && DateTime.Now < m_ExpireTime;
            }
        }
        private static AppOpenAdsManager m_Instance;
        public static AppOpenAdsManager Instance {
            get {
                return m_Instance;
            }
        }
        #region Init
        private void Awake() {
            DontDestroyOnLoad(gameObject);
            m_Instance = this;

            m_IsDoneShowAdsFirstTime = false;
            m_UpdateRemoteConfigSuccess = false;
            m_IsActiveByOtherSource = true;

            EventManager.StartListening("UpdateRemoteConfigs", UpdateRemoteConfigs);
            EventManager.StartListening("ShowAdsFirstTime", ShowAdsFirstTime);
            EventManager.StartListening("DeactiveAOA", () => {
                m_IsActiveByOtherSource = false;
                Debug.Log("============= Deactive AOA==============");
            });
            EventManager.StartListening("ActiveAOA", () => {
                EventManager.AddEventNextFrame(() => {
                    SetActiveByOtherSource(true, 2);
                });
                Debug.Log("============= Active AOA ==============");
            });
            AppStateEventNotifier.AppStateChanged += OnAppStateChanged;

        }
        private void Start() {
            MobileAds.Initialize((InitializationStatus initStatus) => {
                LoadAd();
            });
        } 
        #endregion

        #region RemoteConfigs
        private void UpdateRemoteConfigs() {
            Debug.Log("Update RemoteConfig");
            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_aoa_active);
                m_IsActiveByRemoteConfig = configValue.BooleanValue;
                Debug.Log("AOA active = " + m_IsActiveByRemoteConfig);
            }

            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_aoa_show_first_time_active);
                m_IsActiveShowAdsFirstTime = configValue.BooleanValue;
                Debug.Log("AOA active show first time = " + m_IsActiveShowAdsFirstTime);
            }

            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_aoa_time_between_step_load);
                m_AoaTimeBetweenStep = configValue.DoubleValue;
                Debug.Log("AOA Load time = " + m_AoaTimeBetweenStep);
            }

            {
                ConfigValue configValue = ABIFirebaseManager.Instance.GetConfigValue(ABI.Keys.key_remote_aoa_pause_time_need_to_show_ads);
                m_AoaPauseTimeNeedToShowAds = configValue.DoubleValue;
                Debug.Log("AOA Pause time = " + m_AoaPauseTimeNeedToShowAds);
            }

            m_UpdateRemoteConfigSuccess = true;
        }
        #endregion

        #region Command

        private void LoadAd() {
            if (m_Ad != null) {
                m_Ad.Destroy();
                m_Ad = null;
            }

            AdRequest request = new AdRequest();

            // Load an app open ad for portrait orientation
            AppOpenAd.Load(AD_UNIT_ID, request, ((appOpenAd, error) => {
                if (error != null) {
                    // Handle the error.
                    Debug.LogFormat("Failed to load the ad. (reason: {0})", error.GetMessage());
                    return;
                }

                // App open ads can be preloaded for up to 4 hours.
                m_ExpireTime = DateTime.Now + TimeSpan.FromHours(4);

                Debug.Log("Load Ads Success");
                // App open ad is loaded.
                m_Ad = appOpenAd;
                RegisterEventHandlers(appOpenAd);

            }));
        }

        private void ShowAdIfAvailable() {
            if (m_Ad == null || !m_Ad.CanShowAd()) return;
            if (!IsAdAvailable || m_IsShowingAd || !m_IsActiveByInspector || !m_IsActiveByRemoteConfig || !m_IsActiveByOtherSource) return;
            Debug.Log("Call show AppOpenAds");
            ShowAds();
        }

        private void ShowAds() {
            m_Ad.Show();
        }
        private void RegisterEventHandlers(AppOpenAd ad) {
            ad.OnAdFullScreenContentClosed += HandleAdDidDismissFullScreenContent;
            ad.OnAdFullScreenContentFailed += HandleAdFailedToPresentFullScreenContent;
            ad.OnAdFullScreenContentOpened += HandleAdDidPresentFullScreenContent;
            ad.OnAdImpressionRecorded += HandleAdDidRecordImpression;
            ad.OnAdPaid += HandlePaidEvent;
        }

        IEnumerator WaitFechingSuccessAndShow() {
            while (m_Ad == null && m_UpdateRemoteConfigSuccess) {
                yield return new WaitForSeconds(0.5f);
            }


            if (!m_IsDoneShowAdsFirstTime) {
                ShowAdIfAvailable();

                m_IsDoneShowAdsFirstTime = true;
                Debug.Log("Show App Open Ads First Time");
            }
        }

        private void SetActiveByOtherSource(bool value, float time) {
            if (m_IgnoreShowAds != null) {
                StopCoroutine(m_IgnoreShowAds);
            }
            m_IgnoreShowAds = StartCoroutine(CoSetActiveByOtherSource(value, time));
        }

        private IEnumerator CoSetActiveByOtherSource(bool value, float time) {
            yield return new WaitForSeconds(time);
            m_IsActiveByOtherSource = value;
        }
        private void ShowAdsFirstTime() {
            if (!m_IsActiveByRemoteConfig || !m_IsActiveByInspector || !m_IsActiveByOtherSource) return;
            if (m_IsDoneShowAdsFirstTime || !m_IsActiveShowAdsFirstTime) return;
            Debug.Log("---------------------------------Show Ads Fisrt Time--------------------------");
            StartCoroutine(WaitFechingSuccessAndShow());
        }
        #endregion

        #region Event
        private void HandleAdDidDismissFullScreenContent() {
            Debug.Log("Closed app open ad");
            // Set the ad to null to indicate that AppOpenAdManager no longer has another ad to show.
            m_Ad = null;
            m_IsShowingAd = false;
            m_CloseAdsTime = DateTime.UtcNow;
            LoadAd();
        }

        private void HandleAdFailedToPresentFullScreenContent(AdError args) {
            Debug.LogFormat("Failed to present the ad (reason: {0})", args.GetMessage());
            // Set the ad to null to indicate that AppOpenAdManager no longer has another ad to show.
            m_Ad = null;
            LoadAd();
        }

        private void HandleAdDidPresentFullScreenContent() {
            Debug.Log("Displayed app open ad");
            m_IsShowingAd = true;
        }

        private void HandleAdDidRecordImpression() {
            Debug.Log("Recorded ad impression");
        }

        private void HandlePaidEvent(AdValue args) {
            Debug.LogFormat("Received paid event. (currency: {0}, value: {1}",
                    args.CurrencyCode, args.Value);
        }
        private void OnAppStateChanged(AppState state) {
            Debug.Log("App State changed to : " + state);

            // if the app is Foregrounded and the ad is available, show it.
            if (state == AppState.Foreground) {
                if (IsAdAvailable) {
                    ShowAdIfAvailable();
                }
            }
        }
        private void OnDestroy() {
            // Always unlisten to events when complete.
            AppStateEventNotifier.AppStateChanged -= OnAppStateChanged;
        }
        public void OnApplicationPause(bool paused) {
            if (!m_IsActiveByRemoteConfig || !m_IsActiveByInspector || !m_IsActiveByOtherSource) return;
            switch (paused)
            {
                case true:
                    m_StartPauseTime = DateTime.UtcNow;
                    break;
                case false when (DateTime.Now - m_StartPauseTime).TotalSeconds > m_AoaPauseTimeNeedToShowAds:
                    ShowAdIfAvailable();
                    break;
            }
        } 
        #endregion
    }
}