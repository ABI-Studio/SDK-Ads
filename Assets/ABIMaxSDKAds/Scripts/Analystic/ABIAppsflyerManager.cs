using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AppsFlyerSDK;
namespace SDK
{
#if UNITY_APPSFLYER
    public class ABIAppsflyerManager : MonoBehaviour, IAppsFlyerConversionData
    {
        private static ABIAppsflyerManager instance;
        public static ABIAppsflyerManager Instance { get { return instance; } }

        private void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                return;
            }
            DontDestroyOnLoad(gameObject);
            instance = this;
            AppsFlyerAdRevenue.start(AppsFlyerAdRevenueType.Generic);
        }

        public static void SendEvent(string eventName, Dictionary<string, string> pairs)
        {
            AppsFlyer.sendEvent(eventName, pairs);
        }
        public static void SendEvent(string eventName)
        {
            AppsFlyer.sendEvent(eventName, null);
        }

        #region Conversion
        public void onConversionDataSuccess(string conversionData)
        {
        }

        public void onConversionDataFail(string error)
        {
            AppsFlyer.AFLog("onConversionDataFail", error);
        }

        public void onAppOpenAttribution(string attributionData)
        {
            AppsFlyer.AFLog("onAppOpenAttribution", attributionData);
        }

        public void onAppOpenAttributionFailure(string error)
        {
            AppsFlyer.AFLog("onAppOpenAttributionFailure", error);
        }
        #endregion

        #region Tracking
        public const string af_inters_logicgame = "af_inters_logicgame";
        public const string af_inters_successfullyloaded = "af_inters_successfullyloaded";
        public const string af_inters_displayed = "af_inters_displayed";

        public const string af_inters_show_count = "af_inters_show_count_";

        public const string af_rewarded_logicgame = "af_rewarded_logicgame";
        public const string af_rewarded_successfullyloaded = "af_rewarded_successfullyloaded";
        public const string af_rewarded_displayed = "af_rewarded_displayed";

        public const string af_rewarded_show_count = "af_rewarded_show_count_";
        
        public const string af_level_achieved = "af_level_achieved";

        /// <summary>
        /// 
        /// </summary>
        public static void TrackInterstitial_ClickShowButton()
        {
            SendEvent(af_inters_logicgame);
        }
        public static void TrackInterstitial_LoadedSuccess()
        {
            SendEvent(af_inters_successfullyloaded);
        }
        public static void TrackInterstitial_Displayed()
        {
            SendEvent(af_inters_displayed);
        }
        public void TrackInterstitial_ShowCount(int total, float revenue) {
            if (total == 0) return;
            bool isTracking = total % 5 == 0;

            if (!isTracking) return;
            string eventName = af_inters_show_count + total;
            SendEvent(eventName);
        }
        public static void TrackRewarded_ClickShowButton()
        {
            SendEvent(af_rewarded_logicgame);
        }
        public static void TrackRewarded_LoadedSuccess()
        {
            SendEvent(af_rewarded_successfullyloaded);
        }
        public static void TrackRewarded_Displayed()
        {
            SendEvent(af_rewarded_displayed);
        }
        public static void TrackRewarded_ShowCount(int total) {
            if (total == 0) return;
            bool isTracking = total % 5 == 0;

            if (!isTracking) return;
            string eventName = af_rewarded_show_count + total;
            SendEvent(eventName);
        }
        public static void TrackAppflyerPurchase(string purchaseId, decimal cost, string currency) {
            float fCost = (float)cost;
            fCost *= 0.63f;
            Dictionary<string, string> eventValue = new Dictionary<string, string>();
            eventValue.Add(AFInAppEvents.REVENUE, fCost.ToString());
            eventValue.Add(AFInAppEvents.CURRENCY, currency);
            eventValue.Add(AFInAppEvents.QUANTITY, "1");
            AppsFlyer.sendEvent(AFInAppEvents.PURCHASE, eventValue);
        }

        public static void TrackAppsflyerAdRevenue(ImpressionData impressionData)
        {
            Dictionary<string,string> eventValue = new Dictionary<string, string>();
            eventValue.Add("ad_platform","applovin");
            eventValue.Add("ad_source", impressionData.ad_source);
            eventValue.Add("ad_unit_name", impressionData.ad_unit_name);
            eventValue.Add("ad_format", impressionData.ad_format);
            eventValue.Add("placement","");
            eventValue.Add("value", impressionData.ad_revenue.ToString());
            eventValue.Add("currency", impressionData.ad_currency);
            AppsFlyerAdRevenue.logAdRevenue(impressionData.ad_source, 
                AppsFlyerAdRevenueMediationNetworkType.AppsFlyerAdRevenueMediationNetworkTypeApplovinMax,
                impressionData.ad_revenue,
                "USD",
                eventValue);
        }
        #endregion

    }
#endif
}

