using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Firebase.RemoteConfig;
using System.Threading.Tasks;
namespace SDK
{
    public class FirebaseRemoteConfigManager
    {
        private UnityAction m_FetchSuccessCallback;
        //Firebase.DependencyStatus dependencyStatus = Firebase.DependencyStatus.UnavailableOther;
        public System.Threading.Tasks.Task InitRemoteConfig()
        {
            Dictionary<string, object> defaults =
                    new Dictionary<string, object>();

            defaults.Add(ABI.Keys.key_remote_aoa_active, true);
            defaults.Add(ABI.Keys.key_remote_aoa_time_between_step_load, 10);
            defaults.Add(ABI.Keys.key_remote_aoa_show_first_time_active, true);
            defaults.Add(ABI.Keys.key_remote_aoa_pause_time_need_to_show_ads, 5);
            defaults.Add(ABI.Keys.key_remote_interstital_rate_time, 30);
            defaults.Add(ABI.Keys.key_remote_room_pirce_w1, "");
            defaults.Add(ABI.Keys.key_remote_room_pirce_w2, "");
            defaults.Add(ABI.Keys.key_remote_room_vip_pirce_w2, "");
            defaults.Add(ABI.Keys.key_remote_inter_reward_interspersed, true);
            defaults.Add(ABI.Keys.key_remote_inter_reward_interspersed_time, 10);

            defaults.Add(ABI.Keys.key_remote_free_ads, "");
            FirebaseRemoteConfig remoteConfig = FirebaseRemoteConfig.DefaultInstance;
            return remoteConfig.SetDefaultsAsync(defaults)
                .ContinueWith(result => remoteConfig.FetchAndActivateAsync())
                .Unwrap();
        }
        public ConfigValue GetValues(string key)
        {
            return FirebaseRemoteConfig.DefaultInstance.GetValue(key);
        }

        public void FetchData(UnityAction fetchSuccessCallback)
        {
            m_FetchSuccessCallback = fetchSuccessCallback;
            // FetchAsync only fetches new data if the current data is older than the provided
            // timespan.  Otherwise it assumes the data is "recent enough", and does nothing.
            // By default the timespan is 12 hours, and for production apps, this is a good
            // number.  For this example though, it's set to a timespan of zero, so that
            // changes in the console will always show up immediately.
            try
            {
                Task fetchTask = FirebaseRemoteConfig.DefaultInstance.FetchAsync(
                    System.TimeSpan.Zero);
                fetchTask.ContinueWith(FetchComplete);

            }
            catch (System.Exception)
            {
            }
        }

        private void FetchComplete(Task fetchTask)
        {
            if (fetchTask.IsCanceled)
            {
                Debug.Log("Fetch canceled.");
            }
            else if (fetchTask.IsFaulted)
            {
                Debug.Log("Fetch encountered an error.");
            }
            else if (fetchTask.IsCompleted)
            {
                Debug.Log("Fetch completed successfully!");
            }
            var info = FirebaseRemoteConfig.DefaultInstance.Info;
            switch (info.LastFetchStatus)
            {
                case LastFetchStatus.Success:
                    FirebaseRemoteConfig.DefaultInstance.FetchAndActivateAsync();
                    Debug.Log(string.Format("Remote data loaded and ready (last fetch time {0}).",
                        info.FetchTime));
                    if (m_FetchSuccessCallback != null)
                    {
                        m_FetchSuccessCallback();
                    }
                    break;
                case Firebase.RemoteConfig.LastFetchStatus.Failure:
                    switch (info.LastFetchFailureReason)
                    {
                        case Firebase.RemoteConfig.FetchFailureReason.Error:
                            Debug.Log("Fetch failed for unknown reason");
                            break;
                        case Firebase.RemoteConfig.FetchFailureReason.Throttled:
                            Debug.Log("Fetch throttled until " + info.ThrottledEndTime);
                            break;
                    }
                    break;
                case Firebase.RemoteConfig.LastFetchStatus.Pending:
                    Debug.Log("Latest Fetch call still pending.");
                    break;
            }
        }
        
    }
}

