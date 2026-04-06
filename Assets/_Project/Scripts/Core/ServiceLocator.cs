using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Centralized service registry for Sol system.
    /// Manages lifecycle and provides decoupled access to core services.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator _instance;
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

        }

        public static void RegisterService<T>(object service) where T : class
        {
            if (_instance == null)
            {
                Debug.LogError("[ServiceLocator] Instance not initialized!");
                return;
            }

            Type serviceType = typeof(T);

            if (_instance._services.ContainsKey(serviceType))
            {
                Debug.LogWarning($"[ServiceLocator] Service {serviceType.Name} already registered. Overwriting.");
                _instance._services[serviceType] = service;
            }
            else
            {
                _instance._services.Add(serviceType, service);
            }
        }

        public static T Get<T>() where T : class
        {
            if (_instance == null)
            {
                Debug.LogError("[ServiceLocator] Instance not initialized!");
                return null;
            }

            Type serviceType = typeof(T);

            if (_instance._services.TryGetValue(serviceType, out object service))
            {
                return service as T;
            }

            Debug.LogError($"[ServiceLocator] Service {serviceType.Name} not registered!");
            return null;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _services.Clear();
                _instance = null;
            }
        }
    }
}
