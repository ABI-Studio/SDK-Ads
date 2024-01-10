using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Firebase.RemoteConfig;
using System.Threading.Tasks;
using Firebase.Extensions;

namespace SDK
{
    public class FirebaseRemoteConfigManager
    {
        public void InitRemoteConfig(System.Action onFetchAndActivateSuccessful)
        {
            Dictionary<string, object> defaults =
                    new Dictionary<string, object>();

            defaults.Add(ABI.Keys.key_remote_aoa_active, true);
            defaults.Add(ABI.Keys.key_remote_aoa_time_between_step_load, 10);
            defaults.Add(ABI.Keys.key_remote_aoa_show_first_time_active, true);
            defaults.Add(ABI.Keys.key_remote_aoa_pause_time_need_to_show_ads, 5);
            defaults.Add(ABI.Keys.key_remote_interstital_capping_time_day1, 60);
            defaults.Add(ABI.Keys.key_remote_interstital_capping_time_day2, 45);
            defaults.Add(ABI.Keys.key_remote_inter_reward_interspersed, true);
            defaults.Add(ABI.Keys.key_remote_inter_reward_interspersed_time, 10);

            defaults.Add(ABI.Keys.key_remote_free_ads, "");
            FirebaseRemoteConfig remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            remoteConfig.SetDefaultsAsync(defaults).ContinueWithOnMainThread(task =>
            {
                FetchRemoteConfig(onFetchAndActivateSuccessful);
            });
        }
        
        public ConfigValue GetValues(string key)
        {
            return FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        }

        public void FetchRemoteConfig(System.Action onFetchAndActivateSuccessful)
        {
            if (ABIFirebaseManager.Instance.FirebaseApp == null)
            {
                return;
            }
            Debug.Log("Fetching data...");
            FirebaseRemoteConfig remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            remoteConfig.FetchAsync(System.TimeSpan.Zero).ContinueWithOnMainThread(
                previousTask=>
                {
                    if (!previousTask.IsCompleted)
                    {
                        Debug.LogError($"{nameof(remoteConfig.FetchAsync)} incomplete: Status '{previousTask.Status}'");
                        return;
                    }
                    ActivateRetrievedRemoteConfigValues(onFetchAndActivateSuccessful);
                });
            
        }

        private void ActivateRetrievedRemoteConfigValues(System.Action onFetchAndActivateSuccessful)
        {
            FirebaseRemoteConfig remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            ConfigInfo info = remoteConfig.Info;
            if(info.LastFetchStatus == LastFetchStatus.Success)
            {
                remoteConfig.ActivateAsync().ContinueWithOnMainThread(
                    previousTask =>
                    {
                        Debug.Log($"Remote data loaded and ready (last fetch time {info.FetchTime}).");
                        onFetchAndActivateSuccessful();
                    });
            }
        }
        
    }
}

