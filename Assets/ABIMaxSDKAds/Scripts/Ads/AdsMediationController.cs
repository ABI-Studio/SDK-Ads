using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace SDK {
    public abstract class AdsMediationController : MonoBehaviour {
        [SerializeField]
        protected bool m_IsActive;
        protected AdsMediationType m_AdsMediationType;
        protected UnityAction m_InterstitialAdCloseCallback;
        protected UnityAction m_InterstitialAdLoadSuccessCallback;
        protected UnityAction m_InterstitialAdLoadFailCallback;
        protected UnityAction m_InterstitialAdShowSuccessCallback;
        protected UnityAction m_InterstitialAdShowFailCallback;

        protected UnityAction m_RewardedVideoCloseCallback;
        protected UnityAction m_RewardedVideoLoadSuccessCallback;
        protected UnityAction m_RewardedVideoLoadFailedCallback;
        protected UnityAction m_RewardedVideoEarnSuccessCallback;
        protected UnityAction m_RewardedVideoShowStartCallback;
        protected UnityAction m_RewardedVideoShowFailCallback;

        protected UnityAction m_BannerAdLoadedSuccessCallback;
        protected UnityAction m_BannerAdLoadedFailCallback;


        public bool IsInited = false;
        public virtual void Init() {
            IsInited = true;
        }
        public virtual void InitBannerAds(UnityAction bannerLoadedSuccessCallback, UnityAction bannerAdLoadedFailCallback) {
            m_BannerAdLoadedSuccessCallback = bannerLoadedSuccessCallback;
            m_BannerAdLoadedFailCallback = bannerAdLoadedFailCallback;
        }
        public virtual void RequestBannerAds() {
        }
        public virtual void ShowBannerAds() {
        }
        public virtual void HideBannerAds() {
        }
        public virtual void DestroyBannerAds() {
        }
        public virtual void InitInterstitialAd(UnityAction adClosedCallback, UnityAction adLoadSuccessCallback, UnityAction adLoadFailedCallback, UnityAction adShowSuccessCallback, UnityAction adShowFailCallback) {
            m_InterstitialAdCloseCallback = adClosedCallback;
            m_InterstitialAdLoadSuccessCallback = adLoadSuccessCallback;
            m_InterstitialAdLoadFailCallback = adLoadFailedCallback;
            m_InterstitialAdShowSuccessCallback = adShowSuccessCallback;
            m_InterstitialAdShowFailCallback = adShowFailCallback;
        }
        public virtual void ShowInterstitialAd() { }
        public virtual void RequestInterstitialAd() {
        }
        public virtual bool IsInterstitialLoaded() {
            return false;
        }
        public virtual void InitRewardVideoAd(UnityAction videoClosed, UnityAction videoLoadSuccess, UnityAction videoLoadFailed, UnityAction videoStart) {
            m_RewardedVideoCloseCallback = videoClosed;
            m_RewardedVideoLoadSuccessCallback = videoLoadSuccess;
            m_RewardedVideoLoadFailedCallback = videoLoadFailed;
            m_RewardedVideoShowStartCallback = videoStart;
        }
        public virtual void RequestRewardVideoAd() {
        }
        public virtual void ShowRewardVideoAd(UnityAction successCallback, UnityAction failedCallback) {
            m_RewardedVideoEarnSuccessCallback = successCallback;
            m_RewardedVideoShowFailCallback = failedCallback;
        }
        public virtual bool IsRewardVideoLoaded() {
            return false;
        }
        public virtual bool IsBannerLoaded() {
            return false;
        }
        public virtual bool IsActive() {
            return m_IsActive;
        }
        public virtual bool IsActive(AdsType adsType) {
            return m_IsActive;
        }
        public abstract AdsMediationType GetAdsMediationType();
    }
}