using Firebase.Analytics;
using Firebase.Extensions;
using Firebase.RemoteConfig;
using UnityEngine;
using UnityEngine.Events;

namespace SDK {
    [ScriptOrder(-10)]
    public class ABIFirebaseManager : MonoBehaviour {
        public FirebaseAnalyticsManager m_FirebaseAnalyticsManager;
        public FirebaseRemoteConfigManager m_FirebaseRemoteConfigManager;

        public UnityAction m_FirebaseInitedCallback;
        public UnityAction m_FirebaseInitedSuccessCallback;

        private static ABIFirebaseManager m_Instance;
        public static ABIFirebaseManager Instance {
            get {
                return m_Instance;
            }
        }

        private void Awake() {
            m_Instance = this;
            DontDestroyOnLoad(gameObject);
            Init();

        }
        public void Init() {
            m_FirebaseAnalyticsManager = new FirebaseAnalyticsManager();
            m_FirebaseRemoteConfigManager = new FirebaseRemoteConfigManager();
            Debug.Log("Start Config");
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task => {
                if (task.Result == Firebase.DependencyStatus.Available) {
                    InitializeFirebase();
                } else {
                    Debug.LogError("Could not resolve all Firebase dependencies: " + task.Result);
                }
            });
        }
        private async void InitializeFirebase() {
            await m_FirebaseRemoteConfigManager.InitRemoteConfig();
            IsFirebaseReady = true;
            OnFetchSuccess();
            if (m_FirebaseInitedSuccessCallback != null) {
                m_FirebaseInitedSuccessCallback();
            }
        }
        private void OnFetchSuccess() {
            Debug.Log("Fetch Success");
            Debug.Log("---------------------Update All RemoteConfigs----------------------");
            EventManager.AddEventNextFrame(() => EventManager.TriggerEvent("UpdateRemoteConfigs"));
        }
        public bool IsFirebaseReady { get; private set; } = false;

        public void LogFirebaseEvent(string eventName, string eventParamete, double eventValue) {
            if (IsFirebaseReady) {
                m_FirebaseAnalyticsManager.LogEvent(eventName, eventParamete, eventValue);
            }
        }
        public void LogFirebaseEvent(string eventName, Parameter[] paramss) {
            if (IsFirebaseReady) {
                m_FirebaseAnalyticsManager.LogEvent(eventName, paramss);
            }
        }
        public void LogFirebaseEvent(string eventName) {
            if (IsFirebaseReady) {
                m_FirebaseAnalyticsManager.LogEvent(eventName);
            }
        }
        public void SetUserProperty(string propertyName, string property) {
            if (IsFirebaseReady) {
                m_FirebaseAnalyticsManager.SetUserProperty(propertyName, property);
            }
        }
        public void FetchData(UnityAction successCallback) {
            m_FirebaseRemoteConfigManager.FetchData(successCallback);
        }
        public ConfigValue GetConfigValue(string key) {
            return m_FirebaseRemoteConfigManager.GetValues(key);
        }
    }
}

