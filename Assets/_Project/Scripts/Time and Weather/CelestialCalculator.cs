using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Celestial calculator implementation for celestial body rotations
    /// Handles axis synchronization, oscillation modes, and day-length synchronization
    /// Takes responsibility for celestial body synchronization logic
    /// </summary>
    public class CelestialCalculator : MonoBehaviour, ICelestialCalculator
    {
        [Header("Day Synchronization")]
        [SerializeField] private bool enableDaySynchronization = true;
        [SerializeField] private bool logSynchronizationChanges = true;
        [SerializeField] private bool autoApplySynchronization = true;

        // Cache for time manager reference
        private ITimeManager _timeManager;

        /// <summary>
        /// Initialize with time manager reference for day synchronization
        /// </summary>
        public void Initialize(ITimeManager timeManager)
        {
            _timeManager = timeManager;
            
            if (enableDaySynchronization && autoApplySynchronization)
            {
                // Subscribe to time scale changes to re-sync if needed
                if (_timeManager != null)
                {
                    _timeManager.OnTimeScaleChanged += OnTimeScaleChanged;
                }
            }
        }

        /// <summary>
        /// Calculates celestial rotation with automatic day synchronization if enabled
        /// </summary>
        public Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime)
        {
            if (seasonalData == null) return baseRotation;

            // Apply day synchronization if enabled and we have a time manager
            if (enableDaySynchronization && _timeManager != null)
            {
                return CalculateCelestialRotationWithDaySync(seasonalData, celestialBodyName, baseRotation, celestialTime, _timeManager.DayLengthInSeconds);
            }

            return CalculateCelestialRotationInternal(seasonalData, celestialBodyName, baseRotation, celestialTime);
        }

        /// <summary>
        /// Calculates celestial rotation with explicit day length for synchronization
        /// </summary>
        public Vector3 CalculateCelestialRotationWithDaySync(SeasonalData seasonalData, string celestialBodyName, 
            Vector3 baseRotation, float celestialTime, float dayLengthInSeconds)
        {
            if (seasonalData == null) return baseRotation;

            // Get the synchronized seasonal data
            SeasonalData syncedData = GetDaySynchronizedSeasonalData(seasonalData, dayLengthInSeconds);
            
            return CalculateCelestialRotationInternal(syncedData, celestialBodyName, baseRotation, celestialTime);
        }

        /// <summary>
        /// Interpolates between two seasonal calculations for smooth seasonal transitions
        /// </summary>
        public Vector3 InterpolateCelestialRotation(SeasonalData fromSeasonalData, SeasonalData toSeasonalData, 
            string celestialBodyName, Vector3 baseRotation, float celestialTime, float interpolationFactor)
        {
            if (fromSeasonalData == null || toSeasonalData == null) 
                return baseRotation;

            // Calculate rotation for both seasons
            Vector3 fromRotation = CalculateCelestialRotation(fromSeasonalData, celestialBodyName, baseRotation, celestialTime);
            Vector3 toRotation = CalculateCelestialRotation(toSeasonalData, celestialBodyName, baseRotation, celestialTime);

            // Interpolate between the two rotations
            return Vector3.Lerp(fromRotation, toRotation, interpolationFactor);
        }

        /// <summary>
        /// Validates seasonal data for common configuration errors
        /// </summary>
        public bool ValidateSeasonalData(SeasonalData seasonalData)
        {
            if (seasonalData == null) return false;
            return seasonalData.IsValid();
        }

        /// <summary>
        /// Creates a day-synchronized copy of seasonal data
        /// Adjusts Y-axis speeds to match day length for realistic planetary rotation
        /// </summary>
        private SeasonalData GetDaySynchronizedSeasonalData(SeasonalData originalData, float dayLengthInSeconds)
        {
            // For now, we'll modify the calculation rather than the data itself
            // This preserves the original SeasonalData assets
            return originalData;
        }

        /// <summary>
        /// Internal calculation method that handles the actual rotation math
        /// </summary>
        private Vector3 CalculateCelestialRotationInternal(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime)
        {
            Vector3 calculatedRotation = baseRotation;

            // Get settings based on celestial body name
            if (celestialBodyName.ToLower().Contains("primary") || celestialBodyName.ToLower().Contains("sun"))
            {
                calculatedRotation = CalculatePrimaryStarRotation(seasonalData, baseRotation, celestialTime);
            }
            else if (celestialBodyName.ToLower().Contains("dwarf") || celestialBodyName.ToLower().Contains("red"))
            {
                calculatedRotation = CalculateRedDwarfRotation(seasonalData, baseRotation, celestialTime);
            }

            return calculatedRotation;
        }

        private Vector3 CalculatePrimaryStarRotation(SeasonalData seasonalData, Vector3 baseRotation, float celestialTime)
        {
            if (!seasonalData.PrimaryStarActive) return baseRotation;

            Vector3 rotation = baseRotation;

            // Calculate X-axis (elevation) rotation
            if (seasonalData.PrimaryXAxisEnabled)
            {
                float xAxisSpeed = GetEffectiveXAxisSpeed(
                    seasonalData.PrimaryXAxisSpeed,
                    seasonalData.PrimarySyncXWithY,
                    seasonalData.PrimaryYAxisEnabled,
                    seasonalData.PrimaryYAxisSpeed
                );

                rotation.x = CalculateAxisRotation(
                    seasonalData.PrimaryXAxisMode,
                    xAxisSpeed,
                    seasonalData.PrimaryXAxisMinRange,
                    seasonalData.PrimaryXAxisMaxRange,
                    celestialTime,
                    baseRotation.x
                );
            }

            // Calculate Y-axis (azimuth) rotation
            if (seasonalData.PrimaryYAxisEnabled)
            {
                float yAxisSpeed = GetEffectiveYAxisSpeed(seasonalData.PrimaryYAxisSpeed, "Primary Star");

                rotation.y = CalculateAxisRotation(
                    seasonalData.PrimaryYAxisMode,
                    yAxisSpeed,
                    0f, // Y-axis typically doesn't use min/max for continuous rotation
                    360f,
                    celestialTime,
                    baseRotation.y
                );
            }

            return rotation;
        }

        private Vector3 CalculateRedDwarfRotation(SeasonalData seasonalData, Vector3 baseRotation, float celestialTime)
        {
            if (!seasonalData.RedDwarfActive) return baseRotation;

            Vector3 rotation = baseRotation;

            // Calculate X-axis (elevation) rotation
            if (seasonalData.RedDwarfXAxisEnabled)
            {
                float xAxisSpeed = GetEffectiveXAxisSpeed(
                    seasonalData.RedDwarfXAxisSpeed,
                    seasonalData.RedDwarfSyncXWithY,
                    seasonalData.RedDwarfYAxisEnabled,
                    seasonalData.RedDwarfYAxisSpeed
                );

                rotation.x = CalculateAxisRotation(
                    seasonalData.RedDwarfXAxisMode,
                    xAxisSpeed,
                    seasonalData.RedDwarfXAxisMinRange,
                    seasonalData.RedDwarfXAxisMaxRange,
                    celestialTime,
                    baseRotation.x
                );
            }

            // Calculate Y-axis (azimuth) rotation
            if (seasonalData.RedDwarfYAxisEnabled)
            {
                float yAxisSpeed = GetEffectiveYAxisSpeed(seasonalData.RedDwarfYAxisSpeed, "Red Dwarf");

                rotation.y = CalculateAxisRotation(
                    seasonalData.RedDwarfYAxisMode,
                    yAxisSpeed,
                    0f,
                    360f,
                    celestialTime,
                    baseRotation.y
                );
            }

            return rotation;
        }

        /// <summary>
        /// Gets the effective Y-axis speed, applying day synchronization if enabled
        /// </summary>
        private float GetEffectiveYAxisSpeed(float originalSpeed, string celestialBodyName)
        {
            if (!enableDaySynchronization || _timeManager == null)
            {
                return originalSpeed;
            }

            // Calculate required speed for day synchronization (360° per day)
            float requiredSpeed = 360f / _timeManager.DayLengthInSeconds;
            
            // Check if the original speed is already synchronized (within tolerance)
            const float tolerance = 0.001f;
            if (Mathf.Abs(originalSpeed - requiredSpeed) <= tolerance)
            {
                return originalSpeed; // Already synchronized
            }

            // Log synchronization if enabled
            if (logSynchronizationChanges)
            {
                Debug.Log($"[CelestialCalculator] Synchronizing {celestialBodyName} Y-axis: {originalSpeed:F3} → {requiredSpeed:F3} deg/sec for day length {_timeManager.DayLengthInSeconds}s");
            }

            return requiredSpeed;
        }

        /// <summary>
        /// Gets the effective X-axis speed, handling synchronization with Y-axis if enabled
        /// </summary>
        private float GetEffectiveXAxisSpeed(float originalSpeed, bool syncWithY, bool yAxisEnabled, float yAxisSpeed)
        {
            if (!syncWithY || !yAxisEnabled)
            {
                return originalSpeed;
            }

            // Get the effective Y-axis speed (which may be day-synchronized)
            float effectiveYSpeed = GetEffectiveYAxisSpeed(yAxisSpeed, "X-axis sync calculation");
            
            // Calculate X-axis speed to sync with Y-axis rotation
            return CalculateXAxisSyncSpeed(effectiveYSpeed);
        }

        private float CalculateAxisRotation(CelestialRotationMode mode, float speed, float minRange, float maxRange, 
            float celestialTime, float baseValue)
        {
            switch (mode)
            {
                case CelestialRotationMode.Continuous:
                    // Continuous rotation - speed is degrees per second
                    return baseValue + (speed * celestialTime);

                case CelestialRotationMode.Oscillate:
                    // Oscillating rotation between min and max ranges
                    // Complete one full cycle (min→max→min) per oscillation period
                    float oscillationValue = Mathf.Sin(celestialTime * speed); // -1 to 1
                    float normalizedValue = (oscillationValue + 1f) * 0.5f; // 0 to 1
                    return Mathf.Lerp(minRange, maxRange, normalizedValue);

                default:
                    return baseValue;
            }
        }

        /// <summary>
        /// Calculates X-axis speed to sync with Y-axis rotation
        /// Ensures exactly one X-axis oscillation per complete Y-axis rotation (360 degrees)
        /// </summary>
        private float CalculateXAxisSyncSpeed(float yAxisRotationSpeed)
        {
            // One complete Y rotation = 360 degrees
            // Time for one Y rotation = 360 / yAxisRotationSpeed seconds
            // For one complete X oscillation in that time: speed = 2π / time
            // Since sin(speed * time) completes one cycle when speed * time = 2π
            
            float timeForOneYRotation = 360f / yAxisRotationSpeed;
            float xAxisSpeed = (2f * Mathf.PI) / timeForOneYRotation;
    
            return xAxisSpeed;
        }

        /// <summary>
        /// Event handler for time scale changes - re-synchronize if needed
        /// </summary>
        private void OnTimeScaleChanged()
        {
            if (logSynchronizationChanges)
            {
                Debug.Log("[CelestialCalculator] Time scale changed - celestial synchronization will be recalculated");
            }
        }

        /// <summary>
        /// Public API methods for external control
        /// </summary>
        public void SetDaySynchronizationEnabled(bool enabled)
        {
            enableDaySynchronization = enabled;
            
            if (logSynchronizationChanges)
            {
                Debug.Log($"[CelestialCalculator] Day synchronization: {(enabled ? "Enabled" : "Disabled")}");
            }
        }

        public bool IsDaySynchronizationEnabled()
        {
            return enableDaySynchronization;
        }

        /// <summary>
        /// Validates if a celestial body's Y-axis is synchronized with the given day length
        /// </summary>
        public bool IsYAxisSynchronizedWithDay(float yAxisSpeed, float dayLengthInSeconds)
        {
            if (!enableDaySynchronization) return true; // Always "synchronized" if sync is disabled
            
            float requiredSpeed = 360f / dayLengthInSeconds;
            const float tolerance = 0.001f;
            
            return Mathf.Abs(yAxisSpeed - requiredSpeed) <= tolerance;
        }

        /// <summary>
        /// Gets the required Y-axis speed for day synchronization
        /// </summary>
        public float GetRequiredYAxisSpeedForDay(float dayLengthInSeconds)
        {
            return 360f / dayLengthInSeconds;
        }

        /// <summary>
        /// Cleanup method
        /// </summary>
        public void Cleanup()
        {
            if (_timeManager != null)
            {
                _timeManager.OnTimeScaleChanged -= OnTimeScaleChanged;
            }
        }
    }
}