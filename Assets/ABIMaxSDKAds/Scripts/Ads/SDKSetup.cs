using System;
using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Api;
using SDK;
using Sirenix.OdinInspector;

using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using GoogleMobileAds.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[CreateAssetMenu(fileName = "SDKAdsSetup", menuName = "Tools/SDK Ads Setup", order = 1)]
public partial class SDKSetup : ScriptableObject
{
    private const string MAX_MEDIATION_SYMBOL = "UNITY_AD_MAX";
    private const string ADMOB_MEDIATION_SYMBOL = "UNITY_AD_ADMOB";
    private const string IRONSOURCE_MEDIATION_SYMBOL = "UNITY_AD_IRONSOURCE";

    public bool IsActiveAppsflyer = true;
    public float interstitialCappingTime = 30;
    [HideInInspector]public MaxAdSetup maxAdsSetup;
    [HideInInspector]public AdmobAdSetup admobAdsSetup;
    
    public AdsMediationType GetAdsMediationType(AdsType adsType)
    {
        return adsType switch
        {
            AdsType.BANNER => bannerAdsMediationType,
            AdsType.COLLAPSIBLE_BANNER => collapsibleBannerAdsMediationType,
            AdsType.INTERSTITIAL => interstitialAdsMediationType,
            AdsType.REWARDED => rewardedAdsMediationType,
            AdsType.MREC => mrecAdsMediationType,
            AdsType.APP_OPEN => appOpenAdsMediationType,
            _ => AdsMediationType.NONE
        };
    }

    public bool IsActiveAdsType(AdsType adsType)
    {
        return GetAdsMediationType(adsType) != AdsMediationType.NONE;
    }
    
#if UNITY_EDITOR
    [Button(ButtonSizes.Medium)]
    public void Setup()
    {
        AdsManager adsManager = FindObjectOfType<AdsManager>();
        if (adsManager != null)
        {
            adsManager.UpdateAdsMediationConfig(this);
            EditorUtility.SetDirty(adsManager);
            EditorSceneManager.MarkSceneDirty(adsManager.gameObject.scene);
        }
        else
        {
            Debug.LogError("Please add Manager Prefab to scene (Assets/ABIMaxSDKAds/Prefabs/Manager.prefab)");
        }
        
        SetupSymbol();
        if (adsManager)
        {
#if UNITY_AD_MAX
            if (adsMediationType == AdsMediationType.MAX)
            {
                string assetPath = "Assets/MaxSdk/Resources/AppLovinSettings.asset";
                AppLovinSettings applovinSettings = AssetDatabase.LoadAssetAtPath<AppLovinSettings>(assetPath);
                applovinSettings.SdkKey = sdkKey_MAX;
                EditorUtility.SetDirty(applovinSettings);
                AssetDatabase.SaveAssets();
            }
#endif
        }
        
    }
    private void AddDefineSymbol(string defineSymbol)
    {
        string currentDefineSymbols =
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        string[] defineSymbols = currentDefineSymbols.Split(';');
        List<string> defineSymbolList = new List<string>(defineSymbols);
        currentDefineSymbols = string.Join(";", defineSymbolList.ToArray());
        if (currentDefineSymbols.Contains(defineSymbol)) return;
        currentDefineSymbols += ";" + defineSymbol;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
            currentDefineSymbols);
    }

    private void RemoveDefineSymbol(string defineSymbol)
    {
        string currentDefineSymbols =
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        string[] defineSymbols = currentDefineSymbols.Split(';');
        List<string> defineSymbolList = new List<string>(defineSymbols);
        if (defineSymbolList.Contains(defineSymbol))
        {
            defineSymbolList.Remove(defineSymbol);
        }
        currentDefineSymbols = string.Join(";", defineSymbolList.ToArray());
        PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
            currentDefineSymbols);
    }

    private void SetupSymbol()
    {
        string defineSymbol = MAX_MEDIATION_SYMBOL;
        string removeSymbol = IRONSOURCE_MEDIATION_SYMBOL;
        switch (adsMediationType)
        {
            case AdsMediationType.MAX:
            {
                defineSymbol = MAX_MEDIATION_SYMBOL;
                removeSymbol = IRONSOURCE_MEDIATION_SYMBOL;
            }
                break;
            case AdsMediationType.ADMOB:
            {
                defineSymbol = ADMOB_MEDIATION_SYMBOL;
            }
                break;
            case AdsMediationType.IRONSOURCE:
            {
                defineSymbol = IRONSOURCE_MEDIATION_SYMBOL;
                removeSymbol = MAX_MEDIATION_SYMBOL;
            }
                break;
            case AdsMediationType.NONE:
            {

            }
                break;
        }


        string currentDefineSymbols =
            PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        string[] defineSymbols = currentDefineSymbols.Split(';');
        List<string> defineSymbolList = new List<string>(defineSymbols);
        defineSymbolList.Remove(removeSymbol);
        currentDefineSymbols = string.Join(";", defineSymbolList.ToArray());
        if (!currentDefineSymbols.Contains(defineSymbol))
        {
            currentDefineSymbols += ";" + defineSymbol;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup,
                currentDefineSymbols);
        }
        
        string appsflyerDefineSymbol = "UNITY_APPSFLYER";
        if (IsActiveAppsflyer)
        {
            AddDefineSymbol(appsflyerDefineSymbol);   
        }
        else
        {
            RemoveDefineSymbol(appsflyerDefineSymbol);
        }
    }
#endif
}
public partial class SDKSetup
{
    [BoxGroup("SDK Key")]public AdsMediationType adsMediationType;

    [BoxGroup("SDK Key")]
    [ShowInInspector, ShowIf("@adsMediationType == AdsMediationType.MAX")]
    public string sdkKey_MAX
    {
        get => maxAdsSetup.SDKKey;
        set => maxAdsSetup.SDKKey = value;
    }
}
public partial class SDKSetup
{
    [BoxGroup("INTERSTITIAL")]public AdsMediationType interstitialAdsMediationType;
    [BoxGroup("INTERSTITIAL")][ShowInInspector, ShowIf("@interstitialAdsMediationType == AdsMediationType.MAX")]
    public string interstitialAdUnitID_MAX
    {
        get => maxAdsSetup.InterstitialAdUnitID;
        set => maxAdsSetup.InterstitialAdUnitID = value;
    }
    [BoxGroup("INTERSTITIAL")][ShowInInspector, ShowIf("@interstitialAdsMediationType == AdsMediationType.ADMOB")]
    public List<string> interstitialAdUnitID_ADMOB
    {
        get => admobAdsSetup.InterstitialAdUnitIDList;
        set => admobAdsSetup.InterstitialAdUnitIDList = value;
    }
}
public partial class SDKSetup
{
    [BoxGroup("REWARDED")]public AdsMediationType rewardedAdsMediationType;
    [BoxGroup("REWARDED")][ShowInInspector, ShowIf("@rewardedAdsMediationType == AdsMediationType.MAX")]
    public string rewardedAdUnitID_MAX
    {
        get => maxAdsSetup.RewardedAdUnitID;
        set => maxAdsSetup.RewardedAdUnitID = value;
    }
    [BoxGroup("REWARDED")][ShowInInspector, ShowIf("@rewardedAdsMediationType == AdsMediationType.ADMOB")]
    public List<string> rewardedAdUnitID_ADMOB
    {
        get => admobAdsSetup.RewardedAdUnitIDList;
        set => admobAdsSetup.RewardedAdUnitIDList = value;
    }
}
public partial class SDKSetup
{
    [BoxGroup("BANNER")] public AdsMediationType bannerAdsMediationType;
    #if UNITY_AD_MAX
    [BoxGroup("BANNER")][ShowInInspector, ShowIf("@bannerAdsMediationType == AdsMediationType.MAX")] public MaxSdkBase.BannerPosition maxBannerAdsPosition;
    #endif
    #if UNITY_AD_ADMOB
    [BoxGroup("BANNER")][ShowInInspector, ShowIf("@bannerAdsMediationType == AdsMediationType.ADMOB")] public AdPosition admobBannerAdsPosition;
    #endif
    
    [BoxGroup("BANNER")][ShowInInspector, ShowIf("@bannerAdsMediationType != AdsMediationType.NONE")] public bool isBannerShowingOnStart = false;

    [BoxGroup("BANNER")][ShowInInspector, ShowIf("@bannerAdsMediationType == AdsMediationType.MAX")]
    public string bannerAdUnitID_MAX
    {
        get => maxAdsSetup.BannerAdUnitID;
        set => maxAdsSetup.BannerAdUnitID = value;
    }
    [BoxGroup("BANNER")][ShowInInspector, ShowIf("@bannerAdsMediationType == AdsMediationType.ADMOB")]
    public List<string> bannerAdUnitID_ADMOB
    {
        get => admobAdsSetup.BannerAdUnitIDList;
        set => admobAdsSetup.BannerAdUnitIDList = value;
    }
}
public partial class SDKSetup
{
    [BoxGroup("COLLAPSIBLE BANNER")] public AdsMediationType collapsibleBannerAdsMediationType;
    
    [BoxGroup("COLLAPSIBLE BANNER")][ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE")] public AdPosition adsPositionCollapsibleBanner;
    
    [BoxGroup("COLLAPSIBLE BANNER")][ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE")] public bool isShowingOnStartCollapsibleBanner = false;
    [BoxGroup("COLLAPSIBLE BANNER")][ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE")] public bool isAutoRefreshCollapsibleBanner = false;
    [BoxGroup("COLLAPSIBLE BANNER")][ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE && isAutoRefreshCollapsibleBanner")] public bool isAutoRefreshExtendCollapsibleBanner = false;
    [BoxGroup("COLLAPSIBLE BANNER")][ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE && isAutoRefreshCollapsibleBanner")] [Range(20f,60f)]public float autoRefreshTime = 30;
    [BoxGroup("COLLAPSIBLE BANNER")][ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE")] public bool isAutoCloseCollapsibleBanner = false;
    [BoxGroup("COLLAPSIBLE BANNER")][ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType != AdsMediationType.NONE && isAutoCloseCollapsibleBanner")] [Range(20f,60f)]public float autoCloseTime = 30;

    [BoxGroup("COLLAPSIBLE BANNER")][ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType == AdsMediationType.MAX")]
    public string collapsibleBannerAdUnitID_MAX
    {
        get => maxAdsSetup.CollapsibleBannerAdUnitID;
        set => maxAdsSetup.CollapsibleBannerAdUnitID = value;
    }
    [BoxGroup("COLLAPSIBLE BANNER")][ShowInInspector, ShowIf("@collapsibleBannerAdsMediationType == AdsMediationType.ADMOB")]
    public List<string> collapsibleBannerAdUnitID_ADMOB
    {
        get => admobAdsSetup.CollapsibleBannerAdUnitIDList;
        set => admobAdsSetup.CollapsibleBannerAdUnitIDList = value;
    }
}
public partial class SDKSetup
{
    [BoxGroup("MREC")] public AdsMediationType mrecAdsMediationType;
    [BoxGroup("MREC")] [ShowInInspector, ShowIf("@mrecAdsMediationType == AdsMediationType.MAX")]
    public string mrecAdUnitID_MAX
    {
        get => maxAdsSetup.MrecAdUnitID;
        set => maxAdsSetup.MrecAdUnitID = value;
    }
    [BoxGroup("MREC")] [ShowInInspector, ShowIf("@mrecAdsMediationType == AdsMediationType.ADMOB")]
    public List<string> mrecAdUnitID_ADMOB
    {
        get => admobAdsSetup.MrecAdUnitIDList;
        set => admobAdsSetup.MrecAdUnitIDList = value;
    }
}

public partial class SDKSetup
{
    [BoxGroup("APP OPEN")]public AdsMediationType appOpenAdsMediationType;
    [BoxGroup("APP OPEN")][ShowInInspector, ShowIf("@appOpenAdsMediationType == AdsMediationType.MAX")]
    public string appOpenAdUnitID_MAX
    {
        get => maxAdsSetup.AppOpenAdUnitID;
        set => maxAdsSetup.AppOpenAdUnitID = value;
    }
    [BoxGroup("APP OPEN")] [ShowInInspector, ShowIf("@appOpenAdsMediationType == AdsMediationType.ADMOB")]
    public List<string> appOpenAdUnitID_ADMOB
    {
        get => admobAdsSetup.AppOpenAdUnitIDList;
        set => admobAdsSetup.AppOpenAdUnitIDList = value;
    }
}