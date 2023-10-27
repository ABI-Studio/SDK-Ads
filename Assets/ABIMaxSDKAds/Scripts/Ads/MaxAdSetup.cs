using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MaxAdSetup", menuName = "Tools/Max Ads Setup", order = 1)]
public class MaxAdSetup : ScriptableObject
{
    public string sdkKey;
    public string interstitialAdUnitID;
    public string rewardedAdUnitID;
    public string bannerAdUnitID;
}
