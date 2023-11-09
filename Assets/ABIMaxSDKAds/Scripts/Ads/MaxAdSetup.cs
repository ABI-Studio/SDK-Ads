using System.Collections;
using System.Collections.Generic;
using SDK;
using UnityEngine;

[System.Serializable]
public class MaxAdSetup
{
    [SerializeField]public AdUnitID sdkKey;
    [SerializeField]public AdUnitID interstitialAdUnitID;
    [SerializeField]public AdUnitID rewardedAdUnitID;
    [SerializeField]public AdUnitID bannerAdUnitID;
    [SerializeField]public AdUnitID mrecAdUnitID;
    [SerializeField]public AdUnitID appOpenAdUnitID;

    public string SDKKey
    {
        get => sdkKey.ID;
        set => sdkKey.ID = value;
    }

    public string InterstitialAdUnitID
    {
        get => interstitialAdUnitID.ID;
        set => interstitialAdUnitID.ID = value;
    }

    public string RewardedAdUnitID
    {
        get => rewardedAdUnitID.ID;
        set => rewardedAdUnitID.ID = value;
    }
    public string BannerAdUnitID
    {
        get => bannerAdUnitID.ID;
        set => bannerAdUnitID.ID = value;
    }

    public string MrecAdUnitID
    {
        get => mrecAdUnitID.ID;
        set => mrecAdUnitID.ID = value;
    }

    public string AppOpenAdUnitID
    {
        get => appOpenAdUnitID.ID;
        set => appOpenAdUnitID.ID = value;
    }
}