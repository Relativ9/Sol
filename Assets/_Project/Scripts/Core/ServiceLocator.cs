using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Centralized service registry for Sol system.
    /// Manages lifecycle and provides decoupled access to core services.
    /// </summary>
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator _instance;
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        [Header("Core Services")]
        [SerializeField] private TimeManager _timeManager;

        [Header("Calculator Configuration")]
        [Tooltip("Enable debug logging for celestial calculations")]
        [SerializeField] private bool _enableCalculatorDebugLogging = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeServices();
        }

        private void InitializeServices()
        {
            // Register TimeManager first (other services depend on it)
            if (_timeManager != null)
            {
                RegisterService<ITimeManager>(_timeManager);
                Debug.Log("[ServiceLocator] ITimeManager registered");
            }
            else
            {
                Debug.LogError("[ServiceLocator] TimeManager reference missing!");
                return; // Can't initialize calculator without TimeManager
            }

            // Register CelestialCalculator (depends on ITimeManager)
            var celestialCalculator = new CelestialCalculator(_timeManager);
            celestialCalculator.enableDebugLogging = _enableCalculatorDebugLogging;
            RegisterService<ICelestialCalculator>(celestialCalculator);
            Debug.Log("[ServiceLocator] ICelestialCalculator registered");

            // Future services go here...
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
