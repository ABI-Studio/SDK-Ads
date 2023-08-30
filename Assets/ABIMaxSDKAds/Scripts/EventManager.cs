using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
namespace SDK {
    public class EventManager : MonoBehaviour {
        private Dictionary<string, UnityEvent> eventDictionary;
        private List<UnityEvent> eventStack = new List<UnityEvent>();

        private static EventManager m_Instance;

        public static EventManager Instance {
            get {
                return m_Instance;
            }
        }
        private void Awake() {
            if (m_Instance != null) {
                DestroyImmediate(gameObject);
            } else {
                m_Instance = this;
                m_Instance.Init();
                DontDestroyOnLoad(gameObject);
            }
        }
        void Init() {
            if (eventDictionary == null) {
                eventDictionary = new Dictionary<string, UnityEvent>();
            }
        }

        public static void StartListening(string eventName, UnityAction listener) {
            if (Instance == null) return;
            UnityEvent thisEvent = null;
            if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent)) {
                thisEvent.AddListener(listener);
            } else {
                thisEvent = new UnityEvent();
                thisEvent.AddListener(listener);
                Instance.eventDictionary.Add(eventName, thisEvent);
            }
        }

        public static void StopListening(string eventName, UnityAction listener) {
            if (m_Instance == null) return;
            UnityEvent thisEvent = null;
            if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent)) {
                thisEvent.RemoveListener(listener);
            }
        }

        public static void TriggerEvent(string eventName) {
            UnityEvent thisEvent = null;
            if (Instance) {
                if (Instance.eventDictionary.TryGetValue(eventName, out thisEvent)) {
                    thisEvent.Invoke();
                }
            }
        }
        public static void AddEventNextFrame(UnityAction listener) {
            UnityEvent thisEvent = new UnityEvent();
            thisEvent.AddListener(listener);
            Instance.eventStack.Add(thisEvent);
        }
        private void Update() {
            while (Instance.eventStack.Count > 0) {
                UnityEvent thisEvent = Instance.eventStack[0];
                thisEvent.Invoke();
                Instance.eventStack.RemoveAt(0);
            }
        }
    }


}
