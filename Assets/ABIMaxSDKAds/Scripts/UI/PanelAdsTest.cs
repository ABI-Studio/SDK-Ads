using System;
using System.Collections;
using System.Collections.Generic;
using SDK;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelAdsTest : MonoBehaviour
{
    public Button m_ButtonBanner;
    public Button m_ButtonMREC;

    public TextMeshProUGUI m_TextInterstitialStatus;
    public TextMeshProUGUI m_TextRewardedStatus;
    public TextMeshProUGUI m_TextBannerStatus;
    public TextMeshProUGUI m_TextMRECStatus;
    
    private bool IsShowingMREC;
    private bool IsShowingBanner;
    private void Start()
    {
        m_ButtonMREC.onClick.AddListener(() =>
        {
            if (IsShowingMREC)
            {
                HideMrec();
            }
            else
            {
                ShowMrec();
            }
            IsShowingMREC = !IsShowingMREC;
        });
        m_ButtonBanner.onClick.AddListener(() =>
        {
            if (IsShowingBanner)
            {
                HideBanner();
            }
            else
            {
                ShowBanner();
            }
            IsShowingBanner = !IsShowingBanner;
        });
    }

    private void Update()
    {
        if (AdsManager.Instance.IsBannerShowing())
        {
            m_TextBannerStatus.text = "Banner Ad Showing";
        }
        else
        {
            m_TextBannerStatus.text = AdsManager.Instance.IsBannerLoaded() ? "Banner Ad Loaded" : "Banner Ad Not Loaded";
        }
        if (AdsManager.Instance.IsMRecShowing())
        {
            m_TextMRECStatus.text = "MREC Ad Showing";
        }
        else
        {
            m_TextMRECStatus.text = AdsManager.Instance.IsMRecLoaded() ? "MREC Ad Loaded" : "MREC Ad Not Loaded";
        }
        m_TextInterstitialStatus.text = AdsManager.Instance.IsInterstitialAdLoaded() ? "Interstitial Ad Loaded" : "Interstitial Ad Not Loaded";
        m_TextRewardedStatus.text = AdsManager.Instance.IsRewardVideoLoaded() ? "Rewarded Ad Loaded" : "Rewarded Ad Not Loaded";
    }

    public void ShowBanner()
    {
        AdsManager.Instance.ShowBannerAds();
    }
    public void HideBanner()
    {
        AdsManager.Instance.HideBannerAds();
    }
    public void ShowInter()
    {
        AdsManager.Instance.ShowInterstitial(null,null,false);
    }

    public void ShowReward()
    {
        AdsManager.Instance.ShowRewardVideo(null,null);
    }
    public void ShowMrec()
    {
        AdsManager.Instance.ShowMRecAds();
    }
    public void HideMrec()
    {
        AdsManager.Instance.HideMRecAds();
    }
}
