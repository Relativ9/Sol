using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Sol
{
    /// <summary>
    /// Controls volumetric cloud density, wind speed and wind direction.
    /// 
    /// Density: Each in-game day a random number of density changes (0-4) are scheduled.
    /// Density moves toward each target over 2 in-game hours, creating natural cloud
    /// build and dissipate cycles. Values below zero clamp to clear sky.
    /// 
    /// Wind direction: A prevailing direction is re-randomised once per in-game
    /// day from the season's configured value. A turbulence system runs each
    /// real second with a configurable chance to temporarily gust to a random
    /// direction for a short duration, returning to prevailing afterwards.
    /// This is intentionally designed as a hook for future storm systems.
    /// 
    /// Wind speed: Global horizontal wind speed is scaled by CelestialTimeScale
    /// so clouds move at a visually consistent rate regardless of time acceleration.
    /// 
    /// Can optionally throttle visual updates for performance optimization.
    /// </summary>
    public class CloudController : MonoBehaviour
    {
        [Header("HDRP Volume")]
        [SerializeField] private Volume skyAndFogVolume;

        [Header("Wind Turbulence")]
        [Tooltip("Probability per real second of a turbulence gust occurring")]
        [Range(0f, 1f)]
        [SerializeField] private float turbulenceChancePerSecond = 0.05f;

        [Tooltip("Minimum duration of a turbulence gust in real seconds")]
        [SerializeField] private float turbulenceMinDuration = 0.5f;

        [Tooltip("Maximum duration of a turbulence gust in real seconds")]
        [SerializeField] private float turbulenceMaxDuration = 5f;

        [Tooltip("How quickly wind direction lerps toward its target (smooths both prevailing shifts and gusts)")]
        [Range(0.01f, 1f)]
        [SerializeField] private float windDirectionLerpSpeed = 0.02f;

        [Header("Update Optimization")]
        [Tooltip("Enable update throttling to reduce cloud calculations per frame")]
        [SerializeField] private bool enableUpdateThrottling = false;

        [Tooltip("Target updates per second when throttling is enabled (e.g., 30 for 30 FPS updates)")]
        [SerializeField] private float updatesPerSecond = 30f;

        private VolumetricClouds _volumetricClouds;
        private ITimeManager _timeManager;
        private IUpdateFrequencyOptimizer _updateOptimizer;

        // Season tracking
        private SeasonalData _cachedSeasonalData;
        private int _lastSeasonIndex = -1;

        // Cloud density state
        private float _currentDensity = 0f;
        private float _densityTarget = 0f;
        private float _densityChangesRemainingToday = 0;
        private int _lastDensityDay = -1;

        // Wind direction state
        private float _prevailingDirection;
        private float _currentWindDirection;
        private float _targetWindDirection;
        private int _lastWindDay = -1;

        // Turbulence state — always updated every frame, never throttled
        private bool _isGusting = false;
        private float _gustTimer = 0f;
        private float _secondTimer = 0f;

        private float _lastAppliedWindSpeed = -1f;

        private void Start()
        {
            _timeManager = ServiceLocator.Get<ITimeManager>();

            if (_timeManager == null)
            {
                Debug.LogError("[CloudController] ITimeManager not available.");
                enabled = false;
                return;
            }

            if (skyAndFogVolume == null || skyAndFogVolume.profile == null)
            {
                Debug.LogError("[CloudController] Sky and Fog Volume or profile not assigned.");
                enabled = false;
                return;
            }

            if (!skyAndFogVolume.profile.TryGet(out _volumetricClouds))
            {
                Debug.LogError("[CloudController] No VolumetricClouds found in Volume profile.");
                enabled = false;
                return;
            }

            if (enableUpdateThrottling)
                _updateOptimizer = new UpdateFrequencyOptimizer(updatesPerSecond);
        }

        private void Update()
        {
            if (_volumetricClouds == null || _timeManager == null)
                return;

            // Turbulence timers always advance every frame regardless of throttling
            // — gust durations must be real-time accurate
            UpdateTurbulenceTimers();

            if (enableUpdateThrottling && _updateOptimizer != null)
            {
                if (!_updateOptimizer.ShouldUpdate(Time.time))
                    return;
            }

            RefreshSeasonalData();
            if (_cachedSeasonalData == null)
                return;

            UpdateCloudDensity();
            UpdateWindDirection();
            UpdateWindSpeed();
        }

        // ─── Seasonal Data ────────────────────────────────────────────────────

        private void RefreshSeasonalData()
        {
            int seasonIndex = _timeManager.CurrentSeasonIndex;
            if (seasonIndex == _lastSeasonIndex)
                return;

            _cachedSeasonalData = _timeManager.GetSeasonalData(seasonIndex);
            _lastSeasonIndex = seasonIndex;
        }

        // ─── Turbulence Timers ────────────────────────────────────────────────

        private void UpdateTurbulenceTimers()
        {
            _secondTimer += Time.deltaTime;
            if (_secondTimer >= 1f)
            {
                _secondTimer -= 1f;

                if (!_isGusting && Random.value < turbulenceChancePerSecond)
                {
                    _isGusting = true;
                    _gustTimer = Random.Range(turbulenceMinDuration, turbulenceMaxDuration);
                    _targetWindDirection = Random.Range(0f, 360f);
                }
            }

            if (_isGusting)
            {
                _gustTimer -= Time.deltaTime;
                if (_gustTimer <= 0f)
                {
                    _isGusting = false;
                    _targetWindDirection = _prevailingDirection;
                }
            }
        }

        // ─── Cloud Density ────────────────────────────────────────────────────

        private void UpdateCloudDensity()
        {
            int currentDay = _timeManager.CurrentDay;

            if (currentDay != _lastDensityDay)
            {
                _lastDensityDay = currentDay;
                _densityChangesRemainingToday = Random.Range(0, 5);
                PickNewDensityTarget();
            }

            float twoInGameHoursPerRealSecond =
                _timeManager.CelestialTimeScale /
                _timeManager.WorldTimeData.TotalGameSecondsPerDay /
                12f;

            _currentDensity = Mathf.MoveTowards(
                _currentDensity,
                _densityTarget,
                twoInGameHoursPerRealSecond * Time.deltaTime);

            if (Mathf.Approximately(_currentDensity, _densityTarget) && _densityChangesRemainingToday > 0)
            {
                _densityChangesRemainingToday--;
                PickNewDensityTarget();
            }

            _volumetricClouds.densityMultiplier.value = Mathf.Max(0f, _currentDensity);
        }

        private void PickNewDensityTarget()
        {
            _densityTarget = Random.Range(
                _cachedSeasonalData.cloudDensityMin,
                _cachedSeasonalData.cloudDensityMax);
        }

        // ─── Wind Direction ───────────────────────────────────────────────────

        private void UpdateWindDirection()
        {
            int currentDay = _timeManager.CurrentDay;

            if (currentDay != _lastWindDay)
            {
                float drift = Random.Range(-45f, 45f);
                _prevailingDirection = Mathf.Repeat(
                    _cachedSeasonalData.prevailingWindDirection + drift, 360f);

                if (!_isGusting)
                    _targetWindDirection = _prevailingDirection;

                _lastWindDay = currentDay;
            }

            _currentWindDirection = Mathf.LerpAngle(
                _currentWindDirection,
                _targetWindDirection,
                windDirectionLerpSpeed * Time.deltaTime * 60f);

            _volumetricClouds.orientation.value = new WindParameter.WindParamaterValue
            {
                mode = WindParameter.WindOverrideMode.Custom,
                customValue = _currentWindDirection
            };
        }

        // ─── Wind Speed ───────────────────────────────────────────────────────

        private void UpdateWindSpeed()
        {
            _volumetricClouds.globalWindSpeed.value = new WindParameter.WindParamaterValue
            {
                mode = WindParameter.WindOverrideMode.Multiply,
                multiplyValue = _timeManager.CelestialTimeScale
            };
        }

        // ─── Runtime Control ──────────────────────────────────────────────────

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
                    _updateOptimizer = new UpdateFrequencyOptimizer(updatesPerSecond);
                else
                    _updateOptimizer.SetUpdateFrequency(updatesPerSecond);
            }
        }
    }
}
