//
//  AppsFlyerAdRevenueWrapper.mm
//  Unity-iPhone
//
//  Created by Jonathan Wesfield on 25/11/2019.
//

#import "AppsFlyerAdRevenueWrapper.h"

static bool AdRevenueTypeMoPub = NO;
static BOOL AdRevenueTypeUnityAds = NO;
static BOOL AdRevenueTypeFacebookAudience = NO;
static BOOL AdRevenueTypeGoogleAdMob = NO;
static BOOL AdRevenueTypeAppLovin = NO;

@implementation AppsFlyerAdRevenueWrapper

+ (BOOL) isMoPubSet { return AdRevenueTypeMoPub; }
+ (BOOL) isUnityAdsSet { return AdRevenueTypeUnityAds; }
+ (BOOL) isFacebookAudienceSet { return AdRevenueTypeFacebookAudience; }
+ (BOOL) isGoogleAdMobSet { return AdRevenueTypeGoogleAdMob; }
+ (BOOL) isAppLovinSet { return AdRevenueTypeAppLovin; }

extern "C" {
    
    void setAppsFlyerAdRevenueType(int type){
        switch (type){
            case 1:
                AdRevenueTypeMoPub = YES;
                break;
            case 2:
                AdRevenueTypeUnityAds = YES;
                break;
            case 3:
                AdRevenueTypeFacebookAudience = YES;
                break;
            case 4:
                AdRevenueTypeGoogleAdMob = YES;
                break;
            case 5:
                AdRevenueTypeAppLovin = YES;
                break;
            default:
                break;
        }
    }
    
    const void _start(int length, int* adRevenueTypes){
        if(length > 0 && adRevenueTypes) {
            for(int i = 0; i < length; i++) {
                setAppsFlyerAdRevenueType(adRevenueTypes[i]);
            }
        }
        
        [AppsFlyerAdRevenue start];
    }
    const void _setIsDebugAdrevenue(bool isDebug){
        [[AppsFlyerAdRevenue shared] setIsDebug:isDebug];
    }

    const void _logAdRevenue(const char* monetizationNetwork,
                             int mediationNetwork,
                             double eventRevenue,
                             const char* revenueCurrency,
                             const char* additionalParameters){
        [[AppsFlyerAdRevenue shared] logAdRevenueWithMonetizationNetwork:stringFromChar(monetizationNetwork)
                                                        mediationNetwork:(AppsFlyerAdRevenueMediationNetworkType) mediationNetwork
                                                            eventRevenue:[NSNumber numberWithDouble:eventRevenue]
                                                         revenueCurrency:stringFromChar(revenueCurrency)
                                                    additionalParameters:dictionaryFromJson(additionalParameters)];
    }   
}

@end
