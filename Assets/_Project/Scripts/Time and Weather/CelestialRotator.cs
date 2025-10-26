using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Applies celestial body rotation calculated by CelestialCalculator service.
    /// Polls time from TimeManager and celestial calculator from ServiceLocator.
    /// Caches seasonal data and updates only when season changes.
    /// Can optionally throttle visual updates for performance optimization.
    /// </summary>
    public class CelestialRotator : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string celestialBodyName = "Sol";
        [SerializeField] private bool isMoon = false;
        [SerializeField] private Vector3 baseRotation = Vector3.zero;

        [Header("Update Optimization")]
        [Tooltip("Enable update throttling to reduce rotation calculations per frame")]
        [SerializeField] private bool enableUpdateThrottling = false;
        
        [Tooltip("Target updates per second when throttling is enabled (e.g., 30 for 30 FPS updates)")]
        [SerializeField] private float updatesPerSecond = 30f;

        private ITimeManager _timeManager;
        private ICelestialCalculator _celestialCalculator;
        private SeasonalData _cachedSeasonalData;
        private int _lastSeasonIndex = -1;
        private IUpdateFrequencyOptimizer _updateOptimizer;

        private void Start()
        {
            // Get services from ServiceLocator
            _timeManager = ServiceLocator.Get<ITimeManager>();
            _celestialCalculator = ServiceLocator.Get<ICelestialCalculator>();

            if (_timeManager == null)
            {
                Debug.LogError($"[CelestialRotator] TimeManager not available on {gameObject.name}");
                enabled = false;
                return;
            }

            if (_celestialCalculator == null)
            {
                Debug.LogError($"[CelestialRotator] CelestialCalculator not available on {gameObject.name}");
                enabled = false;
                return;
            }

            // Create update optimizer if throttling is enabled
            if (enableUpdateThrottling)
            {
                _updateOptimizer = new UpdateFrequencyOptimizer(updatesPerSecond);
            }
        }

        private void Update()
        {
            if (_timeManager == null || _celestialCalculator == null)
                return;

            // Optional throttling for visual updates
            if (enableUpdateThrottling && _updateOptimizer != null)
            {
                if (!_updateOptimizer.ShouldUpdate(Time.time))
                    return;
            }

            // Update cached seasonal data only when season changes
            int currentSeasonIndex = _timeManager.CurrentSeasonIndex;
            if (currentSeasonIndex != _lastSeasonIndex)
            {
                _cachedSeasonalData = _timeManager.GetSeasonalData(currentSeasonIndex);
                _lastSeasonIndex = currentSeasonIndex;
            }

            if (_cachedSeasonalData == null)
                return;

            float celestialTime = _timeManager.CelestialTime;

            Quaternion rotation = _celestialCalculator.CalculateCelestialRotation(
                _cachedSeasonalData,
                celestialBodyName,
                baseRotation,
                celestialTime,
                isMoon
            );

            transform.rotation = rotation;
        }

        /// <summary>
        /// Enable or disable update throttling at runtime.
        /// </summary>
        public void SetUpdateThrottling(bool enabled, float updatesPerSecond = 30f)
        {
            enableUpdateThrottling = enabled;
            this.updatesPerSecond = updatesPerSecond;

            if (enabled)
            {
                if (_updateOptimizer == null)
                {
                    _updateOptimizer = new UpdateFrequencyOptimizer(updatesPerSecond);
                }
                else
                {
                    _updateOptimizer.SetUpdateFrequency(updatesPerSecond);
                }
            }
        }
    }
}
