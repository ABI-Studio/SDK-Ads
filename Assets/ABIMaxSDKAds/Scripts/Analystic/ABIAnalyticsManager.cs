using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Firebase.Analytics;

namespace SDK {
    [ScriptOrder(-98)]
    public class ABIAnalyticsManager : MonoBehaviour {
        private static ABIAnalyticsManager instance;
        public static ABIAnalyticsManager Instance { get { return instance; } }

        void Awake() {
            if (instance) {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            instance = this;
        }

        #region Ads Tracking

        #region Revenue
        public const string key_ad_rewarded_revenue = "ad_rewarded_revenue";
        public const string key_ad_rewarded_count = "ad_rewarded_count";
        public const string key_ad_inters_revenue = "ad_inters_revenue";
        public const string key_ad_inters_count = "ad_inters_count";

        public const string ad_inters_show_count = "ad_inters_show_count_";
        public const string ad_rewarded_show_count = "ad_rewarded_show_count_";

        public static void TrackAdImpression(ImpressionData impressionData) {
            double revenue = impressionData.ad_revenue;
            var impressionParameters = new[] {
                new Parameter("ad_platform", impressionData.ad_platform),
                new Parameter("ad_source", impressionData.ad_source),
                new Parameter("ad_unit_name", impressionData.ad_unit_name),
                new Parameter("ad_format", impressionData.ad_format),
                new Parameter("value", revenue),
                new Parameter("currency", "USD"), // All AppLovin revenue is sent in USD
            };
            FirebaseAnalytics.LogEvent("ad_impression", impressionParameters);
            FirebaseAnalytics.LogEvent("ad_impression_abi", impressionParameters);

            EventManager.AddEventNextFrame(() => {
                TrackLocalAdImpression(impressionData.ad_format, impressionData);
            });

        }
        public static void TrackLocalAdImpression(string adFormat, ImpressionData impressionData) {
            List<int> trackPoints = new List<int> { 5, 15, 30, 50, 100 };
            switch (adFormat) {
                case "REWARDED": {
                        
                        int totalWatched = PlayerPrefs.GetInt(key_ad_rewarded_count, 0);
                        totalWatched++;
                        PlayerPrefs.SetInt(key_ad_rewarded_count, totalWatched);

                        bool isTracking = trackPoints.Contains(totalWatched);
                        if (!isTracking) return;

                        string eventName = ad_rewarded_show_count + totalWatched;

                        float totalRevenue = PlayerPrefs.GetFloat(key_ad_rewarded_revenue, 0);
                        totalRevenue += (float)impressionData.ad_revenue;
                        PlayerPrefs.SetFloat(key_ad_rewarded_revenue, totalRevenue);

                        Parameter[] parameters = new Parameter[] {
                            new Parameter("revenue_inters", totalRevenue)
                        };
                        ABIFirebaseManager.Instance.LogFirebaseEvent(eventName, parameters);
#if UNITY_APPSFLYER
                        ABIAppsflyerManager.TrackRewarded_ShowCount(totalWatched);
#endif
                    }
                    break;
                case "INTER": {
                        int totalWatched = PlayerPrefs.GetInt(key_ad_inters_count, 0);
                        totalWatched++;
                        PlayerPrefs.SetInt(key_ad_inters_count, totalWatched);

                        bool isTracking = trackPoints.Contains(totalWatched);
                        if (!isTracking) return;

                        string eventName = ad_inters_show_count + totalWatched;

                        float totalRevenue = PlayerPrefs.GetFloat(key_ad_inters_revenue, 0);
                        totalRevenue += (float)impressionData.ad_revenue;
                        PlayerPrefs.SetFloat(key_ad_inters_revenue, totalRevenue);

                        Parameter[] parameters = new Parameter[] {
                            new Parameter("revenue_reward", totalRevenue)
                        };
                        ABIFirebaseManager.Instance.LogFirebaseEvent(eventName, parameters);
#if UNITY_APPSFLYER
                    ABIAppsflyerManager.Instance.TrackInterstitial_ShowCount(totalWatched, totalRevenue);
#endif
                    }
                    break;
            }
        }
        #endregion

        #region Rewarded Ads
        public const string ads_reward_complete = "ads_reward_complete";
        public const string ads_reward_click = "ads_reward_click";
        public const string ads_reward_show = "ads_reward_show";
        public const string ads_reward_fail = "ads_reward_fail";
        public const string ads_reward_loadsuccess = "ads_reward_loadsuccess";

        public void TrackAdsReward_ClickOnButton() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ads_reward_click);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackRewarded_ClickShowButton();
#endif
        }
        public void TrackAdsReward_StartShow() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ads_reward_show);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackRewarded_Displayed();
#endif
        }
        public void TrackAdsReward_ShowFail() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ads_reward_fail);
        }
        public void TrackAdsReward_ShowCompleted(string placement) {
            Parameter[] parameters = new Parameter[] {
                new Parameter("placement", placement)
            };
            ABIFirebaseManager.Instance.LogFirebaseEvent(ads_reward_complete, parameters);
        }
        public void TrackAdsReward_LoadSuccess() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ads_reward_loadsuccess);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackRewarded_LoadedSuccess();
#endif
        }
        #endregion

        #region Interstitial Ads
        public const string ad_inter_fail = "ad_inter_fail";
        public const string ad_inter_load = "ad_inter_load";
        public const string ad_inter_show = "ad_inter_show";
        public const string ad_inter_click = "ad_inter_click";

        public void TrackAdsInterstitial_LoadedSuccess() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ad_inter_load);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackInterstitial_LoadedSuccess();
#endif
        }
        public void TrackAdsInterstitial_ShowSuccess() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ad_inter_show);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackInterstitial_Displayed();
#endif
        }
        public void TrackAdsInterstitial_ShowFail() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ad_inter_fail);
        }
        public void TrackAdsInterstitial_ClickOnButton() {
            ABIFirebaseManager.Instance.LogFirebaseEvent(ad_inter_click);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.TrackInterstitial_ClickShowButton();
#endif
        }
        #endregion

        #endregion

        public void TrackSessionStart(int id) {
            string eventName = "session_start_" + id;
            ABIFirebaseManager.Instance.LogFirebaseEvent(eventName);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.SendEvent(eventName, null);
#endif
        }
        public void TrackEventMapComplete(int map) {

            string eventName = "map" + map + "_completed";
            ABIFirebaseManager.Instance.LogFirebaseEvent(eventName);
#if UNITY_APPSFLYER
            ABIAppsflyerManager.SendEvent(eventName, null);
#endif
        }
    }
    public class ImpressionData {
        public string ad_platform;
        public string ad_source;
        public string ad_unit_name;
        public string ad_format;
        public double ad_revenue;
        public string ad_currency;
        public AdsType ad_type;
    }
}
