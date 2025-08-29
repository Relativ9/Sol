// using UnityEngine;
//
// namespace Sol
// {
//     /// <summary>
//     /// Celestial calculator implementation for celestial body rotations
//     /// Handles axis synchronization, oscillation modes, and day-length synchronization
//     /// Takes responsibility for celestial body synchronization logic
//     /// </summary>
//     public class CelestialCalculator : MonoBehaviour, ICelestialCalculator
//     {
//         [Header("Day Synchronization")]
//         [SerializeField] private bool enableDaySynchronization = true;
//         [SerializeField] private bool logSynchronizationChanges = true;
//         [SerializeField] private bool autoApplySynchronization = true;
//
//         // Cache for time manager reference
//         private ITimeManager _timeManager;
//
//         /// <summary>
//         /// Initialize with time manager reference for day synchronization
//         /// </summary>
//         public void Initialize(ITimeManager timeManager)
//         {
//             _timeManager = timeManager;
//             if (enableDaySynchronization && autoApplySynchronization)
//             {
//                 // Subscribe to time scale changes to re-sync if needed
//                 if (_timeManager != null)
//                 {
//                     _timeManager.OnTimeScaleChanged += OnTimeScaleChanged;
//                 }
//             }
//         }
//
//         /// <summary>
//         /// Calculates celestial rotation with automatic day synchronization if enabled
//         /// </summary>
//         public Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime)
//         {
//             if (seasonalData == null) return baseRotation;
//
//             // Apply day synchronization if enabled and we have a time manager
//             if (enableDaySynchronization && _timeManager != null && _timeManager.WorldTimeData != null)
//             {
//                 return CalculateCelestialRotationWithDaySync(seasonalData, celestialBodyName, baseRotation, celestialTime, _timeManager.WorldTimeData.dayLengthInSeconds);
//             }
//             return CalculateCelestialRotationInternal(seasonalData, celestialBodyName, baseRotation, celestialTime);
//         }
//
//         /// <summary>
//         /// Calculates celestial rotation with explicit day length for synchronization
//         /// </summary>
//         public Vector3 CalculateCelestialRotationWithDaySync(SeasonalData seasonalData, string celestialBodyName, 
//             Vector3 baseRotation, float celestialTime, float dayLengthInSeconds)
//         {
//             if (seasonalData == null) return baseRotation;
//
//             // Get the synchronized seasonal data
//             SeasonalData syncedData = GetDaySynchronizedSeasonalData(seasonalData, dayLengthInSeconds);
//             
//             return CalculateCelestialRotationInternal(syncedData, celestialBodyName, baseRotation, celestialTime);
//         }
//
//         /// <summary>
//         /// Interpolates between two seasonal calculations for smooth seasonal transitions
//         /// </summary>
//         public Vector3 InterpolateCelestialRotation(SeasonalData fromSeasonalData, SeasonalData toSeasonalData, 
//             string celestialBodyName, Vector3 baseRotation, float celestialTime, float interpolationFactor)
//         {
//             if (fromSeasonalData == null || toSeasonalData == null) 
//                 return baseRotation;
//
//             // Calculate rotation for both seasons
//             Vector3 fromRotation = CalculateCelestialRotation(fromSeasonalData, celestialBodyName, baseRotation, celestialTime);
//             Vector3 toRotation = CalculateCelestialRotation(toSeasonalData, celestialBodyName, baseRotation, celestialTime);
//
//             // Interpolate between the two rotations
//             return Vector3.Lerp(fromRotation, toRotation, interpolationFactor);
//         }
//
//         /// <summary>
//         /// Validates seasonal data for common configuration errors
//         /// </summary>
//         public bool ValidateSeasonalData(SeasonalData seasonalData)
//         {
//             if (seasonalData == null) return false;
//             return seasonalData.IsValid();
//         }
//
//         /// <summary>
//         /// Creates a day-synchronized copy of seasonal data
//         /// Adjusts Y-axis speeds to match day length for realistic planetary rotation
//         /// </summary>
//         private SeasonalData GetDaySynchronizedSeasonalData(SeasonalData originalData, float dayLengthInSeconds)
//         {
//             // For now, we'll modify the calculation rather than the data itself
//             // This preserves the original SeasonalData assets
//             return originalData;
//         }
//
//         /// <summary>
//         /// Internal calculation method that handles the actual rotation math
//         /// </summary>
//         private Vector3 CalculateCelestialRotationInternal(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime)
//         {
//             Vector3 calculatedRotation = baseRotation;
//
//             // Get settings based on celestial body name
//             if (celestialBodyName.ToLower().Contains("primary") || celestialBodyName.ToLower().Contains("sun"))
//             {
//                 calculatedRotation = CalculatePrimaryStarRotation(seasonalData, baseRotation, celestialTime);
//             }
//             else if (celestialBodyName.ToLower().Contains("dwarf") || celestialBodyName.ToLower().Contains("red"))
//             {
//                 calculatedRotation = CalculateRedDwarfRotation(seasonalData, baseRotation, celestialTime);
//             }
//
//             return calculatedRotation;
//         }
//
//         private Vector3 CalculatePrimaryStarRotation(SeasonalData seasonalData, Vector3 baseRotation, float celestialTime)
//         {
//             if (!seasonalData.PrimaryStarActive) return baseRotation;
//
//             Vector3 rotation = baseRotation;
//
//             // Calculate X-axis (elevation) rotation
//             if (seasonalData.PrimaryXAxisEnabled)
//             {
//                 float xAxisSpeed = GetEffectiveXAxisSpeed(
//                     seasonalData.PrimaryXAxisSpeed,
//                     seasonalData.PrimarySyncXWithY,
//                     seasonalData.PrimaryYAxisEnabled,
//                     seasonalData.PrimaryYAxisSpeed
//                 );
//
//                 rotation.x = CalculateAxisRotation(
//                     seasonalData.PrimaryXAxisMode,
//                     xAxisSpeed,
//                     seasonalData.PrimaryXAxisMinRange,
//                     seasonalData.PrimaryXAxisMaxRange,
//                     celestialTime,
//                     baseRotation.x
//                 );
//             }
//
//             // Calculate Y-axis (azimuth) rotation
//             if (seasonalData.PrimaryYAxisEnabled)
//             {
//                 float yAxisSpeed = GetEffectiveYAxisSpeed(seasonalData.PrimaryYAxisSpeed, "Primary Star");
//
//                 rotation.y = CalculateAxisRotation(
//                     seasonalData.PrimaryYAxisMode,
//                     yAxisSpeed,
//                     0f, // Y-axis typically doesn't use min/max for continuous rotation
//                     360f,
//                     celestialTime,
//                     baseRotation.y
//                 );
//             }
//
//             return rotation;
//         }
//
//         private Vector3 CalculateRedDwarfRotation(SeasonalData seasonalData, Vector3 baseRotation, float celestialTime)
//         {
//             if (!seasonalData.RedDwarfActive) return baseRotation;
//
//             Vector3 rotation = baseRotation;
//
//             // Calculate X-axis (elevation) rotation
//             if (seasonalData.RedDwarfXAxisEnabled)
//             {
//                 float xAxisSpeed = GetEffectiveXAxisSpeed(
//                     seasonalData.RedDwarfXAxisSpeed,
//                     seasonalData.RedDwarfSyncXWithY,
//                     seasonalData.RedDwarfYAxisEnabled,
//                     seasonalData.RedDwarfYAxisSpeed
//                 );
//
//                 rotation.x = CalculateAxisRotation(
//                     seasonalData.RedDwarfXAxisMode,
//                     xAxisSpeed,
//                     seasonalData.RedDwarfXAxisMinRange,
//                     seasonalData.RedDwarfXAxisMaxRange,
//                     celestialTime,
//                     baseRotation.x
//                 );
//             }
//
//             // Calculate Y-axis (azimuth) rotation
//             if (seasonalData.RedDwarfYAxisEnabled)
//             {
//                 float yAxisSpeed = GetEffectiveYAxisSpeed(seasonalData.RedDwarfYAxisSpeed, "Red Dwarf");
//
//                 rotation.y = CalculateAxisRotation(
//                     seasonalData.RedDwarfYAxisMode,
//                     yAxisSpeed,
//                     0f,
//                     360f,
//                     celestialTime,
//                     baseRotation.y
//                 );
//             }
//
//             return rotation;
//         }
//
//         /// <summary>
//         /// Gets the effective Y-axis speed, applying day synchronization if enabled
//         /// </summary>
//         private float GetEffectiveYAxisSpeed(float originalSpeed, string celestialBodyName)
//         {
//             
//             if (!enableDaySynchronization || _timeManager == null || _timeManager.WorldTimeData == null)
//             {
//                 return originalSpeed;
//             }
//
//             // For day synchronization with celestialTime (0-1), we want 360° per full day
//             // So speed should be 360 when multiplied by celestialTime = 1
//             float requiredSpeed = 360f; // Not divided by dayLengthInSeconds!
//
//             // Log synchronization if enabled
//             if (logSynchronizationChanges)
//             {
//                 Debug.Log($"[CelestialCalculator] Synchronizing {celestialBodyName} Y-axis: {originalSpeed:F3} → {requiredSpeed:F3} for celestialTime-based rotation");
//             }
//
//             return requiredSpeed;
//             // if (!enableDaySynchronization || _timeManager == null || _timeManager.WorldTimeData == null)
//             // {
//             //     return originalSpeed;
//             // }
//             //
//             // // Calculate required speed for day synchronization (360° per day)
//             // float requiredSpeed = 360f / _timeManager.WorldTimeData.dayLengthInSeconds;
//             //
//             // // Check if the original speed is already synchronized (within tolerance)
//             // const float tolerance = 0.001f;
//             // if (Mathf.Abs(originalSpeed - requiredSpeed) <= tolerance)
//             // {
//             //     return originalSpeed; // Already synchronized
//             // }
//             //
//             // // Log synchronization if enabled
//             // if (logSynchronizationChanges)
//             // {
//             //     Debug.Log($"[CelestialCalculator] Synchronizing {celestialBodyName} Y-axis: {originalSpeed:F3} → {requiredSpeed:F3} deg/sec for day length {_timeManager.WorldTimeData.dayLengthInSeconds}s");
//             // }
//             //
//             // return requiredSpeed;
//         }
//
//         /// <summary>
//         /// Gets the effective X-axis speed, handling synchronization with Y-axis if enabled
//         /// </summary>
//         private float GetEffectiveXAxisSpeed(float originalSpeed, bool syncWithY, bool yAxisEnabled, float yAxisSpeed)
//         {
//             if (!syncWithY || !yAxisEnabled)
//             {
//                 return originalSpeed;
//             }
//
//             // Get the effective Y-axis speed (which may be day-synchronized)
//             float effectiveYSpeed = GetEffectiveYAxisSpeed(yAxisSpeed, "X-axis sync calculation");
//             
//             // Calculate X-axis speed to sync with Y-axis rotation
//             return CalculateXAxisSyncSpeed(effectiveYSpeed);
//         }
//
//         private float CalculateAxisRotation(CelestialRotationMode mode, float speed, float minRange, float maxRange, 
//             float celestialTime, float baseValue)
//         {
//             switch (mode)
//             {
//                 case CelestialRotationMode.Continuous:
//                     
//                     // if (enableDaySynchronization && _timeManager != null)
//                     // {
//                     //     // For day sync: celestialTime (0-1) * 360° = degrees rotated today
//                     //     return baseValue + (celestialTime * 360f);
//                     // }
//                     // else
//                     // {
//                     //     // Original behavior: speed * time
//                     //     return baseValue + (speed * celestialTime);
//                     // }
//                     // Continuous rotation - speed is degrees per second
//                     return baseValue + (speed * celestialTime);
//
//                 case CelestialRotationMode.Oscillate:
//                     // Oscillating rotation between min and max ranges
//                     // Complete one full cycle (min→max→min) per oscillation period
//                     float oscillationValue = Mathf.Sin(celestialTime * speed); // -1 to 1
//                     float normalizedValue = (oscillationValue + 1f) * 0.5f; // 0 to 1
//                     return Mathf.Lerp(minRange, maxRange, normalizedValue);
//
//                 default:
//                     return baseValue;
//             }
//         }
//
//         /// <summary>
//         /// Calculates X-axis speed to sync with Y-axis rotation
//         /// Ensures exactly one X-axis oscillation per complete Y-axis rotation (360 degrees)
//         /// </summary>
//         private float CalculateXAxisSyncSpeed(float yAxisRotationSpeed)
//         {
//             // One complete Y rotation = 360 degrees
//             // Time for one Y rotation = 360 / yAxisRotationSpeed seconds
//             // For one complete X oscillation in that time: speed = 2π / time
//             // Since sin(speed * time) completes one cycle when speed * time = 2π
//             
//             float timeForOneYRotation = 360f / yAxisRotationSpeed;
//             float xAxisSpeed = (2f * Mathf.PI) / timeForOneYRotation;
//     
//             return xAxisSpeed;
//         }
//
//         /// <summary>
//         /// Event handler for time scale changes - re-synchronize if needed
//         /// </summary>
//         private void OnTimeScaleChanged()
//         {
//             if (logSynchronizationChanges)
//             {
//                 Debug.Log("[CelestialCalculator] Time scale changed - celestial synchronization will be recalculated");
//             }
//         }
//
//         /// <summary>
//         /// Public API methods for external control
//         /// </summary>
//         public void SetDaySynchronizationEnabled(bool enabled)
//         {
//             enableDaySynchronization = enabled;
//             
//             if (logSynchronizationChanges)
//             {
//                 Debug.Log($"[CelestialCalculator] Day synchronization: {(enabled ? "Enabled" : "Disabled")}");
//             }
//         }
//
//         public bool IsDaySynchronizationEnabled()
//         {
//             return enableDaySynchronization;
//         }
//
//         /// <summary>
//         /// Validates if a celestial body's Y-axis is synchronized with the given day length
//         /// </summary>
//         public bool IsYAxisSynchronizedWithDay(float yAxisSpeed, float dayLengthInSeconds)
//         {
//             if (!enableDaySynchronization) return true; // Always "synchronized" if sync is disabled
//             float requiredSpeed = 360f / dayLengthInSeconds;
//             const float tolerance = 0.001f;
//             return Mathf.Abs(yAxisSpeed - requiredSpeed) <= tolerance;
//         }
//
//         /// <summary>
//         /// Gets the required Y-axis speed for day synchronization
//         /// </summary>
//         public float GetRequiredYAxisSpeedForDay(float dayLengthInSeconds)
//         {
//             return 360f / dayLengthInSeconds;
//         }
//
//         /// <summary>
//         /// Cleanup method
//         /// </summary>
//         public void Cleanup()
//         {
//             if (_timeManager != null)
//             {
//                 _timeManager.OnTimeScaleChanged -= OnTimeScaleChanged;
//             }
//         }
//     }
// }

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
            if (enableDaySynchronization && _timeManager != null && _timeManager.WorldTimeData != null)
            {
                return CalculateCelestialRotationWithDaySync(seasonalData, celestialBodyName, baseRotation, celestialTime, _timeManager.WorldTimeData.dayLengthInSeconds);
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

            return CalculateCelestialRotationInternal(seasonalData, celestialBodyName, baseRotation, celestialTime);
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
                    seasonalData.PrimaryYAxisSpeed,
                    seasonalData.PrimaryYAxisOverrideSpeed
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
                float yAxisSpeed = GetEffectiveYAxisSpeed(
                    seasonalData.PrimaryYAxisSpeed, 
                    seasonalData.PrimaryYAxisOverrideSpeed,
                    "Primary Star"
                );

                rotation.y = CalculateAxisRotation(
                    seasonalData.PrimaryYAxisMode,
                    yAxisSpeed,
                    seasonalData.PrimaryYAxisMinRange,
                    seasonalData.PrimaryYAxisMaxRange,
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
                    seasonalData.RedDwarfYAxisSpeed,
                    seasonalData.RedDwarfYAxisOverrideSpeed
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
                float yAxisSpeed = GetEffectiveYAxisSpeed(
                    seasonalData.RedDwarfYAxisSpeed,
                    seasonalData.RedDwarfYAxisOverrideSpeed,
                    "Red Dwarf"
                );

                rotation.y = CalculateAxisRotation(
                    seasonalData.RedDwarfYAxisMode,
                    yAxisSpeed,
                    seasonalData.RedDwarfYAxisMinRange,
                    seasonalData.RedDwarfYAxisMaxRange,
                    celestialTime,
                    baseRotation.y
                );
            }

            return rotation;
        }

        /// <summary>
        /// Gets the effective Y-axis speed, applying day synchronization if enabled
        /// </summary>
        private float GetEffectiveYAxisSpeed(float originalSpeed, bool overrideSpeed, string celestialBodyName)
        {
            // If override is enabled, use the custom speed
            if (overrideSpeed)
            {
                return originalSpeed;
            }

            // Otherwise, use day synchronization (360 degrees per celestial time unit)
            if (enableDaySynchronization)
            {
                if (logSynchronizationChanges)
                {
                    Debug.Log($"[CelestialCalculator] Synchronizing {celestialBodyName} Y-axis: {originalSpeed:F3} → 360.0 for day sync");
                }
                return 360f; // One full rotation per celestial day
            }

            return originalSpeed;
        }

        /// <summary>
        /// Gets the effective X-axis speed, handling synchronization with Y-axis if enabled
        /// </summary>
        private float GetEffectiveXAxisSpeed(float originalSpeed, bool syncWithY, bool yAxisEnabled, float yAxisSpeed, bool yAxisOverrideSpeed)
        {
            if (!syncWithY || !yAxisEnabled)
            {
                return originalSpeed;
            }

            // Get the effective Y-axis speed (which may be day-synchronized)
            float effectiveYSpeed = GetEffectiveYAxisSpeed(yAxisSpeed, yAxisOverrideSpeed, "X-axis sync calculation");
            
            // Calculate X-axis speed to sync with Y-axis rotation
            return CalculateXAxisSyncSpeed(effectiveYSpeed);
        }

        private float CalculateAxisRotation(CelestialRotationMode mode, float speed, float minRange, float maxRange, 
            float celestialTime, float baseValue)
        {
            switch (mode)
            {
                case CelestialRotationMode.Continuous:
                    // EXACTLY like your original working code
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
            const float tolerance = 0.001f;
            return Mathf.Abs(yAxisSpeed - 360f) <= tolerance; // 360 degrees per celestial time unit
        }

        /// <summary>
        /// Gets the required Y-axis speed for day synchronization
        /// </summary>
        public float GetRequiredYAxisSpeedForDay(float dayLengthInSeconds)
        {
            return 360f; // One full rotation per celestial day
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

//LAST WORKING ONE WITHOUT THE OFFSET
// using UnityEngine;
//
// namespace Sol
// {
//     /// <summary>
//     /// Celestial calculator implementation for celestial body rotations
//     /// Handles axis synchronization, oscillation modes, and day-length synchronization
//     /// Takes responsibility for celestial body synchronization logic
//     /// </summary>
//     public class CelestialCalculator : MonoBehaviour, ICelestialCalculator
//     {
//         [Header("Day Synchronization")]
//         [SerializeField] private bool enableDaySynchronization = true;
//         [SerializeField] private bool logSynchronizationChanges = true;
//         [SerializeField] private bool autoApplySynchronization = true;
//
//         // Cache for time manager reference
//         private ITimeManager _timeManager;
//
//         /// <summary>
//         /// Initialize with time manager reference for day synchronization
//         /// </summary>
//         public void Initialize(ITimeManager timeManager)
//         {
//             _timeManager = timeManager;
//             if (enableDaySynchronization && autoApplySynchronization)
//             {
//                 // Subscribe to time scale changes to re-sync if needed
//                 if (_timeManager != null)
//                 {
//                     _timeManager.OnTimeScaleChanged += OnTimeScaleChanged;
//                 }
//             }
//         }
//
//         /// <summary>
//         /// Calculates celestial rotation with automatic day synchronization if enabled
//         /// </summary>
//         public Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime)
//         {
//             if (seasonalData == null) return baseRotation;
//
//             // Apply day synchronization if enabled and we have a time manager
//             if (enableDaySynchronization && _timeManager != null && _timeManager.WorldTimeData != null)
//             {
//                 return CalculateCelestialRotationWithDaySync(seasonalData, celestialBodyName, baseRotation, celestialTime, _timeManager.WorldTimeData.dayLengthInSeconds);
//             }
//             return CalculateCelestialRotationInternal(seasonalData, celestialBodyName, baseRotation, celestialTime);
//         }
//
//         /// <summary>
//         /// Calculates celestial rotation with explicit day length for synchronization
//         /// </summary>
//         public Vector3 CalculateCelestialRotationWithDaySync(SeasonalData seasonalData, string celestialBodyName, 
//             Vector3 baseRotation, float celestialTime, float dayLengthInSeconds)
//         {
//             if (seasonalData == null) return baseRotation;
//
//             return CalculateCelestialRotationInternal(seasonalData, celestialBodyName, baseRotation, celestialTime);
//         }
//
//         /// <summary>
//         /// Interpolates between two seasonal calculations for smooth seasonal transitions
//         /// </summary>
//         public Vector3 InterpolateCelestialRotation(SeasonalData fromSeasonalData, SeasonalData toSeasonalData, 
//             string celestialBodyName, Vector3 baseRotation, float celestialTime, float interpolationFactor)
//         {
//             if (fromSeasonalData == null || toSeasonalData == null) 
//                 return baseRotation;
//
//             // Calculate rotation for both seasons
//             Vector3 fromRotation = CalculateCelestialRotation(fromSeasonalData, celestialBodyName, baseRotation, celestialTime);
//             Vector3 toRotation = CalculateCelestialRotation(toSeasonalData, celestialBodyName, baseRotation, celestialTime);
//
//             // Interpolate between the two rotations
//             return Vector3.Lerp(fromRotation, toRotation, interpolationFactor);
//         }
//
//         /// <summary>
//         /// Validates seasonal data for common configuration errors
//         /// </summary>
//         public bool ValidateSeasonalData(SeasonalData seasonalData)
//         {
//             if (seasonalData == null) return false;
//             return seasonalData.IsValid();
//         }
//
//         /// <summary>
//         /// Internal calculation method that handles the actual rotation math
//         /// </summary>
//         private Vector3 CalculateCelestialRotationInternal(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime)
//         {
//             Vector3 calculatedRotation = baseRotation;
//
//             // Get settings based on celestial body name
//             if (celestialBodyName.ToLower().Contains("primary") || celestialBodyName.ToLower().Contains("sun"))
//             {
//                 calculatedRotation = CalculatePrimaryStarRotation(seasonalData, baseRotation, celestialTime);
//             }
//             else if (celestialBodyName.ToLower().Contains("dwarf") || celestialBodyName.ToLower().Contains("red"))
//             {
//                 calculatedRotation = CalculateRedDwarfRotation(seasonalData, baseRotation, celestialTime);
//             }
//
//             return calculatedRotation;
//         }
//
//         private Vector3 CalculatePrimaryStarRotation(SeasonalData seasonalData, Vector3 baseRotation, float celestialTime)
//         {
//             if (!seasonalData.PrimaryStarActive) return baseRotation;
//
//             Vector3 rotation = baseRotation;
//
//             // Calculate X-axis (elevation) rotation
//             if (seasonalData.PrimaryXAxisEnabled)
//             {
//                 float xAxisSpeed = GetEffectiveXAxisSpeed(
//                     seasonalData.PrimaryXAxisSpeed,
//                     seasonalData.PrimarySyncXWithY,
//                     seasonalData.PrimaryYAxisEnabled,
//                     seasonalData.PrimaryYAxisSpeed,
//                     seasonalData.PrimaryYAxisOverrideSpeed
//                 );
//
//                 rotation.x = CalculateAxisRotation(
//                     seasonalData.PrimaryXAxisMode,
//                     xAxisSpeed,
//                     seasonalData.PrimaryXAxisMinRange,
//                     seasonalData.PrimaryXAxisMaxRange,
//                     celestialTime,
//                     baseRotation.x
//                 );
//             }
//
//             // Calculate Y-axis (azimuth) rotation
//             if (seasonalData.PrimaryYAxisEnabled)
//             {
//                 float yAxisSpeed = GetEffectiveYAxisSpeed(
//                     seasonalData.PrimaryYAxisSpeed, 
//                     seasonalData.PrimaryYAxisOverrideSpeed,
//                     "Primary Star"
//                 );
//
//                 rotation.y = CalculateAxisRotation(
//                     seasonalData.PrimaryYAxisMode,
//                     yAxisSpeed,
//                     seasonalData.PrimaryYAxisMinRange,
//                     seasonalData.PrimaryYAxisMaxRange,
//                     celestialTime,
//                     baseRotation.y
//                 );
//             }
//
//             return rotation;
//         }
//
//         private Vector3 CalculateRedDwarfRotation(SeasonalData seasonalData, Vector3 baseRotation, float celestialTime)
//         {
//             if (!seasonalData.RedDwarfActive) return baseRotation;
//
//             Vector3 rotation = baseRotation;
//
//             // Calculate X-axis (elevation) rotation
//             if (seasonalData.RedDwarfXAxisEnabled)
//             {
//                 float xAxisSpeed = GetEffectiveXAxisSpeed(
//                     seasonalData.RedDwarfXAxisSpeed,
//                     seasonalData.RedDwarfSyncXWithY,
//                     seasonalData.RedDwarfYAxisEnabled,
//                     seasonalData.RedDwarfYAxisSpeed,
//                     seasonalData.RedDwarfYAxisOverrideSpeed
//                 );
//
//                 rotation.x = CalculateAxisRotation(
//                     seasonalData.RedDwarfXAxisMode,
//                     xAxisSpeed,
//                     seasonalData.RedDwarfXAxisMinRange,
//                     seasonalData.RedDwarfXAxisMaxRange,
//                     celestialTime,
//                     baseRotation.x
//                 );
//             }
//
//             // Calculate Y-axis (azimuth) rotation
//             if (seasonalData.RedDwarfYAxisEnabled)
//             {
//                 float yAxisSpeed = GetEffectiveYAxisSpeed(
//                     seasonalData.RedDwarfYAxisSpeed,
//                     seasonalData.RedDwarfYAxisOverrideSpeed,
//                     "Red Dwarf"
//                 );
//
//                 rotation.y = CalculateAxisRotation(
//                     seasonalData.RedDwarfYAxisMode,
//                     yAxisSpeed,
//                     seasonalData.RedDwarfYAxisMinRange,
//                     seasonalData.RedDwarfYAxisMaxRange,
//                     celestialTime,
//                     baseRotation.y
//                 );
//             }
//
//             return rotation;
//         }
//
//         /// <summary>
//         /// Gets the effective Y-axis speed, applying day synchronization if enabled
//         /// </summary>
//         private float GetEffectiveYAxisSpeed(float originalSpeed, bool overrideSpeed, string celestialBodyName)
//         {
//             // If override is enabled, use the custom speed
//             if (overrideSpeed)
//             {
//                 return originalSpeed;
//             }
//
//             // Otherwise, use day synchronization (360 degrees per celestial time unit)
//             if (enableDaySynchronization)
//             {
//                 if (logSynchronizationChanges)
//                 {
//                     Debug.Log($"[CelestialCalculator] Synchronizing {celestialBodyName} Y-axis: {originalSpeed:F3} → 360.0 for day sync");
//                 }
//                 return 360f; // One full rotation per celestial day
//             }
//
//             return originalSpeed;
//         }
//
//         /// <summary>
//         /// Gets the effective X-axis speed, handling synchronization with Y-axis if enabled
//         /// </summary>
//         private float GetEffectiveXAxisSpeed(float originalSpeed, bool syncWithY, bool yAxisEnabled, float yAxisSpeed, bool yAxisOverrideSpeed)
//         {
//             if (!syncWithY || !yAxisEnabled)
//             {
//                 return originalSpeed;
//             }
//
//             // Get the effective Y-axis speed (which may be day-synchronized)
//             float effectiveYSpeed = GetEffectiveYAxisSpeed(yAxisSpeed, yAxisOverrideSpeed, "X-axis sync calculation");
//             
//             // Calculate X-axis speed to sync with Y-axis rotation
//             return CalculateXAxisSyncSpeed(effectiveYSpeed);
//         }
//
//         private float CalculateAxisRotation(CelestialRotationMode mode, float speed, float minRange, float maxRange, 
//             float celestialTime, float baseValue)
//         {
//             switch (mode)
//             {
//                 case CelestialRotationMode.Continuous:
//                     // Continuous rotation - ADD to base rotation to preserve offset
//                     return baseValue + (speed * celestialTime);
//
//                 case CelestialRotationMode.Oscillate:
//                     // Oscillating rotation between min and max ranges
//                     // Use a proper oscillation frequency based on speed
//                     float frequency = speed * 0.01f; // Convert speed to reasonable frequency
//                     float oscillationValue = Mathf.Sin(celestialTime * frequency * 2f * Mathf.PI); // -1 to 1
//                     float normalizedValue = (oscillationValue + 1f) * 0.5f; // 0 to 1
//                     float oscillationResult = Mathf.Lerp(minRange, maxRange, normalizedValue);
//                     
//                     // ADD oscillation to base value to preserve starting position
//                     return baseValue + oscillationResult;
//
//                 default:
//                     return baseValue;
//             }
//         }
//
//         /// <summary>
//         /// Calculates X-axis speed to sync with Y-axis rotation
//         /// Ensures exactly one X-axis oscillation per complete Y-axis rotation (360 degrees)
//         /// </summary>
//         private float CalculateXAxisSyncSpeed(float yAxisRotationSpeed)
//         {
//             // For one complete Y rotation (360 degrees), we want one complete X oscillation
//             // Since Y rotates at yAxisRotationSpeed degrees per celestial time unit,
//             // it takes (360 / yAxisRotationSpeed) time units to complete one rotation
//             // We want X to complete one oscillation in that same time
//             // So X frequency should be: yAxisRotationSpeed / 360
//             
//             return yAxisRotationSpeed / 360f * 100f; // Scale up for visible oscillation
//         }
//
//         /// <summary>
//         /// Event handler for time scale changes - re-synchronize if needed
//         /// </summary>
//         private void OnTimeScaleChanged()
//         {
//             if (logSynchronizationChanges)
//             {
//                 Debug.Log("[CelestialCalculator] Time scale changed - celestial synchronization will be recalculated");
//             }
//         }
//
//         /// <summary>
//         /// Public API methods for external control
//         /// </summary>
//         public void SetDaySynchronizationEnabled(bool enabled)
//         {
//             enableDaySynchronization = enabled;
//             
//             if (logSynchronizationChanges)
//             {
//                 Debug.Log($"[CelestialCalculator] Day synchronization: {(enabled ? "Enabled" : "Disabled")}");
//             }
//         }
//
//         public bool IsDaySynchronizationEnabled()
//         {
//             return enableDaySynchronization;
//         }
//
//         /// <summary>
//         /// Validates if a celestial body's Y-axis is synchronized with the given day length
//         /// </summary>
//         public bool IsYAxisSynchronizedWithDay(float yAxisSpeed, float dayLengthInSeconds)
//         {
//             if (!enableDaySynchronization) return true; // Always "synchronized" if sync is disabled
//             const float tolerance = 0.001f;
//             return Mathf.Abs(yAxisSpeed - 360f) <= tolerance; // 360 degrees per celestial time unit
//         }
//
//         /// <summary>
//         /// Gets the required Y-axis speed for day synchronization
//         /// </summary>
//         public float GetRequiredYAxisSpeedForDay(float dayLengthInSeconds)
//         {
//             return 360f; // One full rotation per celestial day
//         }
//
//         /// <summary>
//         /// Cleanup method
//         /// </summary>
//         public void Cleanup()
//         {
//             if (_timeManager != null)
//             {
//                 _timeManager.OnTimeScaleChanged -= OnTimeScaleChanged;
//             }
//         }
//     }
// }

// using UnityEngine;
//
// namespace Sol
// {
//     /// <summary>
//     /// Concrete implementation of celestial body rotation calculations and day synchronization.
//     /// Handles mathematical calculations for celestial body movements based on seasonal data.
//     /// Follows Single Responsibility Principle by focusing solely on celestial rotation mathematics.
//     /// </summary>
//     public class CelestialCalculator : ICelestialCalculator
//     {
//         #region Private Fields
//
//         /// <summary>
//         /// Reference to the time manager for accessing temporal data and day synchronization
//         /// </summary>
//         private ITimeManager timeManager;
//
//         /// <summary>
//         /// Whether the calculator has been properly initialized
//         /// </summary>
//         private bool isInitialized;
//
//         /// <summary>
//         /// Cached day length for performance optimization during calculations
//         /// </summary>
//         private float cachedDayLength;
//
//         /// <summary>
//         /// Last update time for cache invalidation
//         /// </summary>
//         private float lastCacheUpdate;
//
//         /// <summary>
//         /// Cache update interval in seconds
//         /// </summary>
//         private const float CACHE_UPDATE_INTERVAL = 1f;
//
//         /// <summary>
//         /// Default tolerance for day synchronization speed matching
//         /// </summary>
//         private const float DEFAULT_SYNC_TOLERANCE = 0.1f;
//
//         /// <summary>
//         /// Speed value that represents one full rotation per day (360 degrees)
//         /// </summary>
//         private const float FULL_ROTATION_SPEED = 360f;
//
//         #endregion
//
//         #region Initialization and Lifecycle
//
//         /// <summary>
//         /// Initializes the calculator with required dependencies.
//         /// Must be called before using any calculation methods.
//         /// </summary>
//         /// <param name="timeManager">Time manager instance for accessing temporal data</param>
//         public void Initialize(ITimeManager timeManager)
//         {
//             this.timeManager = timeManager ?? throw new System.ArgumentNullException(nameof(timeManager));
//             
//             // Cache initial day length
//             UpdateCachedDayLength();
//             
//             isInitialized = true;
//             
//             Debug.Log("[CelestialCalculator] Initialized successfully with TimeManager");
//         }
//
//         /// <summary>
//         /// Cleans up resources and references when the calculator is no longer needed.
//         /// Should be called when destroying or replacing the calculator.
//         /// </summary>
//         public void Cleanup()
//         {
//             timeManager = null;
//             isInitialized = false;
//             cachedDayLength = 0f;
//             lastCacheUpdate = 0f;
//             
//             Debug.Log("[CelestialCalculator] Cleanup completed");
//         }
//
//         #endregion
//
//         #region Core Rotation Calculations
//
//         /// <summary>
//         /// Calculates celestial rotation for a specific celestial body using seasonal data.
//         /// Applies day synchronization automatically if enabled.
//         /// </summary>
//         /// <param name="seasonalData">Current seasonal data containing celestial settings</param>
//         /// <param name="celestialBodyName">Name of celestial body (e.g., 'PrimaryStar', 'RedDwarf')</param>
//         /// <param name="baseRotation">Starting rotation values to build upon</param>
//         /// <param name="celestialTime">Current celestial time for calculations (0-1)</param>
//         /// <returns>Calculated rotation vector in degrees</returns>
//         public Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime)
//         {
//             if (!ValidateInputs(seasonalData, celestialBodyName, celestialTime))
//             {
//                 return baseRotation;
//             }
//
//             // Update cached day length if needed
//             UpdateCachedDayLength();
//
//             // Get axis configurations for the celestial body
//             var xConfig = seasonalData.GetAxisConfiguration(celestialBodyName, RotationAxis.X);
//             var yConfig = seasonalData.GetAxisConfiguration(celestialBodyName, RotationAxis.Y);
//             var zConfig = seasonalData.GetAxisConfiguration(celestialBodyName, RotationAxis.Z);
//
//             // Calculate rotation for each axis
//             Vector3 calculatedRotation = new Vector3(
//                 CalculateAxisRotation(xConfig, celestialTime, baseRotation.x),
//                 CalculateAxisRotation(yConfig, celestialTime, baseRotation.y),
//                 CalculateAxisRotation(zConfig, celestialTime, baseRotation.z)
//             );
//
//             return calculatedRotation;
//         }
//
//         /// <summary>
//         /// Calculates celestial rotation with explicit day length for synchronization.
//         /// Useful for testing or when time manager is not available.
//         /// </summary>
//         /// <param name="seasonalData">Current seasonal data containing celestial settings</param>
//         /// <param name="celestialBodyName">Name of celestial body</param>
//         /// <param name="baseRotation">Starting rotation values to build upon</param>
//         /// <param name="celestialTime">Current celestial time for calculations (0-1)</param>
//         /// <param name="dayLengthInSeconds">Day length in real-world seconds for synchronization</param>
//         /// <returns>Calculated rotation vector in degrees</returns>
//         public Vector3 CalculateCelestialRotationWithDaySync(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime, float dayLengthInSeconds)
//         {
//             if (!ValidateInputs(seasonalData, celestialBodyName, celestialTime))
//             {
//                 return baseRotation;
//             }
//
//             if (dayLengthInSeconds <= 0f)
//             {
//                 Debug.LogWarning("[CelestialCalculator] Invalid day length provided, using cached value");
//                 dayLengthInSeconds = cachedDayLength;
//             }
//
//             // Get axis configurations for the celestial body
//             var xConfig = seasonalData.GetAxisConfiguration(celestialBodyName, RotationAxis.X);
//             var yConfig = seasonalData.GetAxisConfiguration(celestialBodyName, RotationAxis.Y);
//             var zConfig = seasonalData.GetAxisConfiguration(celestialBodyName, RotationAxis.Z);
//
//             // Apply day synchronization to Y-axis if enabled
//             if (yConfig.enabled && IsDaySynchronizationEnabled())
//             {
//                 yConfig = ApplyDaySyncToAxisConfig(yConfig, dayLengthInSeconds);
//             }
//
//             // Calculate rotation for each axis
//             Vector3 calculatedRotation = new Vector3(
//                 CalculateAxisRotation(xConfig, celestialTime, baseRotation.x),
//                 CalculateAxisRotation(yConfig, celestialTime, baseRotation.y),
//                 CalculateAxisRotation(zConfig, celestialTime, baseRotation.z)
//             );
//
//             return calculatedRotation;
//         }
//
//         /// <summary>
//         /// Interpolates between two seasonal configurations for smooth season transitions.
//         /// Provides seamless celestial body movement during season changes.
//         /// </summary>
//         /// <param name="fromSeason">Starting season's celestial configuration</param>
//         /// <param name="toSeason">Target season's celestial configuration</param>
//         /// <param name="celestialBodyName">Name of celestial body to interpolate</param>
//         /// <param name="baseRotation">Starting rotation values</param>
//         /// <param name="celestialTime">Current celestial time (0-1)</param>
//         /// <param name="transitionProgress">Transition progress (0 = from season, 1 = to season)</param>
//         /// <returns>Interpolated rotation vector in degrees</returns>
//         public Vector3 InterpolateCelestialRotation(SeasonalData fromSeason, SeasonalData toSeason, string celestialBodyName, Vector3 baseRotation, float celestialTime, float transitionProgress)
//         {
//             if (fromSeason == null || toSeason == null)
//             {
//                 Debug.LogWarning("[CelestialCalculator] Null seasonal data provided for interpolation");
//                 return baseRotation;
//             }
//
//             // Clamp transition progress
//             transitionProgress = Mathf.Clamp01(transitionProgress);
//
//             // Calculate rotation for both seasons
//             Vector3 fromRotation = CalculateCelestialRotation(fromSeason, celestialBodyName, baseRotation, celestialTime);
//             Vector3 toRotation = CalculateCelestialRotation(toSeason, celestialBodyName, baseRotation, celestialTime);
//
//             // Handle angle wrapping for smooth interpolation
//             Vector3 interpolatedRotation = new Vector3(
//                 InterpolateAngle(fromRotation.x, toRotation.x, transitionProgress),
//                 InterpolateAngle(fromRotation.y, toRotation.y, transitionProgress),
//                 InterpolateAngle(fromRotation.z, toRotation.z, transitionProgress)
//             );
//
//             return interpolatedRotation;
//         }
//
//         #endregion
//
//         #region Axis-Specific Calculations
//
//         /// <summary>
//         /// Calculates rotation for a specific axis using the given configuration.
//         /// Handles both oscillating and continuous rotation modes.
//         /// </summary>
//         /// <param name="axisConfig">Configuration for the specific axis</param>
//         /// <param name="celestialTime">Current celestial time (0-1)</param>
//         /// <param name="baseValue">Base rotation value for this axis</param>
//         /// <returns>Calculated rotation value in degrees</returns>
//         public float CalculateAxisRotation(AxisConfiguration axisConfig, float celestialTime, float baseValue)
//         {
//             if (!axisConfig.enabled || !axisConfig.IsValid)
//             {
//                 return baseValue;
//             }
//
//             return axisConfig.mode switch
//             {
//                 CelestialRotationMode.Oscillate => CalculateOscillatingRotation(
//                     axisConfig.speed, axisConfig.min, axisConfig.max, celestialTime, baseValue),
//                 CelestialRotationMode.Continuous => CalculateContinuousRotation(
//                     axisConfig.speed, celestialTime, baseValue),
//                 _ => baseValue
//             };
//         }
//
//         /// <summary>
//         /// Calculates oscillating rotation between min and max values.
//         /// Creates pendulum-like movement for celestial bodies.
//         /// </summary>
//         /// <param name="speed">Oscillation speed in degrees per celestial time unit</param>
//         /// <param name="min">Minimum rotation value in degrees</param>
//         /// <param name="max">Maximum rotation value in degrees</param>
//         /// <param name="celestialTime">Current celestial time (0-1)</param>
//         /// <param name="baseValue">Base rotation value to add to result</param>
//         /// <returns>Calculated oscillating rotation value in degrees</returns>
//         public float CalculateOscillatingRotation(float speed, float min, float max, float celestialTime, float baseValue)
//         {
//             if (min >= max)
//             {
//                 Debug.LogWarning($"[CelestialCalculator] Invalid oscillation range: min ({min}) >= max ({max})");
//                 return baseValue;
//             }
//
//             // Calculate oscillation using sine wave for smooth movement
//             float range = max - min;
//             float center = (min + max) * 0.5f;
//             
//             // Use speed to determine oscillation frequency
//             float frequency = speed / FULL_ROTATION_SPEED; // Convert speed to frequency
//             float oscillationValue = Mathf.Sin(celestialTime * 2f * Mathf.PI * frequency);
//             
//             // Map sine wave (-1 to 1) to oscillation range
//             float rotationValue = center + (oscillationValue * range * 0.5f);
//             
//             return baseValue + rotationValue;
//         }
//
//         /// <summary>
//         /// Calculates continuous rotation around a full 360-degree cycle.
//         /// Creates smooth circular movement for celestial bodies.
//         /// </summary>
//         /// <param name="speed">Rotation speed in degrees per celestial time unit</param>
//         /// <param name="celestialTime">Current celestial time (0-1)</param>
//         /// <param name="baseValue">Base rotation value to add to result</param>
//         /// <returns>Calculated continuous rotation value in degrees</returns>
//         public float CalculateContinuousRotation(float speed, float celestialTime, float baseValue)
//         {
//             if (speed < 0f)
//             {
//                 Debug.LogWarning($"[CelestialCalculator] Negative rotation speed ({speed}) detected, using absolute value");
//                 speed = Mathf.Abs(speed);
//             }
//
//             // Calculate rotation based on celestial time and speed
//             float rotationValue = celestialTime * speed;
//             
//             // Normalize to 0-360 degree range
//             rotationValue = rotationValue % 360f;
//             
//             return baseValue + rotationValue;
//         }
//
//         #endregion
//
//         #region Day Synchronization
//
//         /// <summary>
//         /// Whether day synchronization is currently enabled for calculations.
//         /// When enabled, Y-axis rotation speeds are automatically adjusted to match day length.
//         /// </summary>
//         /// <returns>True if day synchronization is enabled</returns>
//         public bool IsDaySynchronizationEnabled()
//         {
//             // Day synchronization is enabled if we have a valid time manager
//             return isInitialized && timeManager != null;
//         }
//
//         /// <summary>
//         /// Calculates the required Y-axis rotation speed to complete one full rotation per day.
//         /// Used for synchronizing celestial bodies with the day/night cycle.
//         /// </summary>
//         /// <param name="dayLengthInSeconds">Length of one day in real-world seconds</param>
//         /// <returns>Required Y-axis speed in degrees per celestial time unit</returns>
//         public float GetRequiredYAxisSpeedForDay(float dayLengthInSeconds)
//         {
//             if (dayLengthInSeconds <= 0f)
//             {
//                 Debug.LogWarning("[CelestialCalculator] Invalid day length for synchronization calculation");
//                 return FULL_ROTATION_SPEED;
//             }
//
//             // For day synchronization, we want exactly one full rotation (360°) per day
//             return FULL_ROTATION_SPEED;
//         }
//
//         /// <summary>
//         /// Checks if a given Y-axis speed is synchronized with the specified day length.
//         /// Allows for small tolerance in speed matching.
//         /// </summary>
//         /// <param name="currentSpeed">Current Y-axis rotation speed</param>
//         /// <param name="dayLengthInSeconds">Target day length in seconds</param>
//         /// <param name="tolerance">Acceptable difference in speed (default: 0.1 degrees)</param>
//         /// <returns>True if the speed is synchronized within tolerance</returns>
//         public bool IsYAxisSynchronizedWithDay(float currentSpeed, float dayLengthInSeconds, float tolerance = DEFAULT_SYNC_TOLERANCE)
//         {
//             float requiredSpeed = GetRequiredYAxisSpeedForDay(dayLengthInSeconds);
//             return Mathf.Abs(currentSpeed - requiredSpeed) <= tolerance;
//         }
//
//         /// <summary>
//         /// Applies day synchronization to a Y-axis speed value.
//         /// Modifies the speed to ensure one complete rotation per day.
//         /// </summary>
//         /// <param name="originalSpeed">Original Y-axis speed</param>
//         /// <param name="dayLengthInSeconds">Target day length in seconds</param>
//         /// <returns>Synchronized Y-axis speed</returns>
//         public float ApplyDaySynchronization(float originalSpeed, float dayLengthInSeconds)
//         {
//             return GetRequiredYAxisSpeedForDay(dayLengthInSeconds);
//         }
//
//         #endregion
//         
//                 #region Celestial Body Detection
//
//         /// <summary>
//         /// Determines if the given celestial body name represents a primary star.
//         /// Uses flexible name matching to support various naming conventions.
//         /// </summary>
//         /// <param name="celestialBodyName">Name to check (case-insensitive)</param>
//         /// <returns>True if the name represents a primary star</returns>
//         public bool IsPrimaryStar(string celestialBodyName)
//         {
//             if (string.IsNullOrEmpty(celestialBodyName))
//                 return false;
//
//             string lowerName = celestialBodyName.ToLower();
//             return lowerName.Contains("primary") || 
//                    lowerName.Contains("sun") || 
//                    lowerName.Contains("main") ||
//                    lowerName.Contains("star") && !lowerName.Contains("dwarf") && !lowerName.Contains("red");
//         }
//
//         /// <summary>
//         /// Determines if the given celestial body name represents a red dwarf star.
//         /// Uses flexible name matching to support various naming conventions.
//         /// </summary>
//         /// <param name="celestialBodyName">Name to check (case-insensitive)</param>
//         /// <returns>True if the name represents a red dwarf star</returns>
//         public bool IsRedDwarf(string celestialBodyName)
//         {
//             if (string.IsNullOrEmpty(celestialBodyName))
//                 return false;
//
//             string lowerName = celestialBodyName.ToLower();
//             return lowerName.Contains("dwarf") || 
//                    lowerName.Contains("red") && lowerName.Contains("star") ||
//                    lowerName.Contains("secondary");
//         }
//
//         /// <summary>
//         /// Gets the celestial body type for the given name.
//         /// Provides abstraction over celestial body identification.
//         /// </summary>
//         /// <param name="celestialBodyName">Name to identify</param>
//         /// <returns>Celestial body type or Unknown if not recognized</returns>
//         public CelestialBodyType GetCelestialBodyType(string celestialBodyName)
//         {
//             if (IsPrimaryStar(celestialBodyName))
//                 return CelestialBodyType.PrimaryStar;
//             
//             if (IsRedDwarf(celestialBodyName))
//                 return CelestialBodyType.RedDwarf;
//             
//             return CelestialBodyType.Unknown;
//         }
//
//         #endregion
//
//         #region Validation and Utilities
//
//         /// <summary>
//         /// Validates that the calculator is properly initialized and ready for use.
//         /// Checks all required dependencies and configuration.
//         /// </summary>
//         /// <returns>True if calculator is ready for calculations</returns>
//         public bool IsInitialized()
//         {
//             return isInitialized && timeManager != null;
//         }
//
//         /// <summary>
//         /// Validates seasonal data for celestial calculations.
//         /// Ensures the data contains valid configuration for the specified celestial body.
//         /// </summary>
//         /// <param name="seasonalData">Seasonal data to validate</param>
//         /// <param name="celestialBodyName">Celestial body name to validate for</param>
//         /// <returns>True if seasonal data is valid for calculations</returns>
//         public bool ValidateSeasonalData(SeasonalData seasonalData, string celestialBodyName)
//         {
//             if (seasonalData == null)
//             {
//                 Debug.LogWarning("[CelestialCalculator] Seasonal data is null");
//                 return false;
//             }
//
//             if (string.IsNullOrEmpty(celestialBodyName))
//             {
//                 Debug.LogWarning("[CelestialCalculator] Celestial body name is null or empty");
//                 return false;
//             }
//
//             // Check if the celestial body is active in this season
//             if (!seasonalData.IsCelestialBodyActive(celestialBodyName))
//             {
//                 Debug.LogWarning($"[CelestialCalculator] Celestial body '{celestialBodyName}' is not active in season '{seasonalData.Season}'");
//                 return false;
//             }
//
//             // Validate axis configurations
//             var xConfig = seasonalData.GetAxisConfiguration(celestialBodyName, RotationAxis.X);
//             var yConfig = seasonalData.GetAxisConfiguration(celestialBodyName, RotationAxis.Y);
//             var zConfig = seasonalData.GetAxisConfiguration(celestialBodyName, RotationAxis.Z);
//
//             bool hasValidAxis = (xConfig.enabled && xConfig.IsValid) ||
//                                (yConfig.enabled && yConfig.IsValid) ||
//                                (zConfig.enabled && zConfig.IsValid);
//
//             if (!hasValidAxis)
//             {
//                 Debug.LogWarning($"[CelestialCalculator] No valid axis configurations found for '{celestialBodyName}' in season '{seasonalData.Season}'");
//                 return false;
//             }
//
//             return true;
//         }
//
//         /// <summary>
//         /// Gets diagnostic information about the calculator's current state.
//         /// Useful for debugging and monitoring calculator health.
//         /// </summary>
//         /// <returns>Formatted diagnostic string</returns>
//         public string GetDiagnosticInfo()
//         {
//             var diagnostics = new System.Text.StringBuilder();
//             
//             diagnostics.AppendLine("=== CelestialCalculator Diagnostics ===");
//             diagnostics.AppendLine($"Initialized: {isInitialized}");
//             diagnostics.AppendLine($"Time Manager: {(timeManager != null ? "Connected" : "Not Connected")}");
//             diagnostics.AppendLine($"Day Sync Enabled: {IsDaySynchronizationEnabled()}");
//             diagnostics.AppendLine($"Cached Day Length: {cachedDayLength:F2} seconds");
//             diagnostics.AppendLine($"Last Cache Update: {lastCacheUpdate:F2}");
//             
//             if (timeManager != null)
//             {
//                 diagnostics.AppendLine($"Current Celestial Time: {timeManager.CelestialTime:F3}");
//                 diagnostics.AppendLine($"Current Season: {timeManager.CurrentSeason}");
//                 diagnostics.AppendLine($"Time Scale: {timeManager.TimeScale:F2}");
//             }
//             
//             return diagnostics.ToString();
//         }
//
//         #endregion
//
//         #region Configuration Access
//
//         /// <summary>
//         /// Gets the current time manager reference used by the calculator.
//         /// Provides access to the time management dependency.
//         /// </summary>
//         /// <returns>Current time manager instance or null if not initialized</returns>
//         public ITimeManager GetTimeManager()
//         {
//             return timeManager;
//         }
//
//         /// <summary>
//         /// Updates the time manager reference used for day synchronization.
//         /// Allows dynamic reconfiguration of the calculator's time source.
//         /// </summary>
//         /// <param name="newTimeManager">New time manager to use</param>
//         public void SetTimeManager(ITimeManager newTimeManager)
//         {
//             timeManager = newTimeManager;
//             
//             if (newTimeManager != null)
//             {
//                 UpdateCachedDayLength();
//                 isInitialized = true;
//                 Debug.Log("[CelestialCalculator] Time manager updated successfully");
//             }
//             else
//             {
//                 isInitialized = false;
//                 Debug.LogWarning("[CelestialCalculator] Time manager set to null - calculator disabled");
//             }
//         }
//
//         #endregion
//
//         #region Private Helper Methods
//
//         /// <summary>
//         /// Validates common input parameters for calculation methods.
//         /// Provides centralized validation logic with consistent error handling.
//         /// </summary>
//         /// <param name="seasonalData">Seasonal data to validate</param>
//         /// <param name="celestialBodyName">Celestial body name to validate</param>
//         /// <param name="celestialTime">Celestial time to validate</param>
//         /// <returns>True if all inputs are valid</returns>
//         private bool ValidateInputs(SeasonalData seasonalData, string celestialBodyName, float celestialTime)
//         {
//             if (!isInitialized)
//             {
//                 Debug.LogError("[CelestialCalculator] Calculator not initialized. Call Initialize() first.");
//                 return false;
//             }
//
//             if (seasonalData == null)
//             {
//                 Debug.LogWarning("[CelestialCalculator] Seasonal data is null");
//                 return false;
//             }
//
//             if (string.IsNullOrEmpty(celestialBodyName))
//             {
//                 Debug.LogWarning("[CelestialCalculator] Celestial body name is null or empty");
//                 return false;
//             }
//
//             if (celestialTime < 0f || celestialTime > 1f)
//             {
//                 Debug.LogWarning($"[CelestialCalculator] Celestial time ({celestialTime:F3}) is outside valid range (0-1)");
//                 return false;
//             }
//
//             return true;
//         }
//
//         /// <summary>
//         /// Updates the cached day length from the time manager.
//         /// Optimizes performance by avoiding frequent property access.
//         /// </summary>
//         private void UpdateCachedDayLength()
//         {
//             if (timeManager?.WorldTimeData != null && 
//                 (Time.time - lastCacheUpdate) > CACHE_UPDATE_INTERVAL)
//             {
//                 cachedDayLength = timeManager.WorldTimeData.dayLengthInSeconds;
//                 lastCacheUpdate = Time.time;
//             }
//         }
//
//         /// <summary>
//         /// Applies day synchronization to an axis configuration.
//         /// Creates a modified configuration with synchronized speed.
//         /// </summary>
//         /// <param name="originalConfig">Original axis configuration</param>
//         /// <param name="dayLengthInSeconds">Target day length for synchronization</param>
//         /// <returns>Modified axis configuration with synchronized speed</returns>
//         private AxisConfiguration ApplyDaySyncToAxisConfig(AxisConfiguration originalConfig, float dayLengthInSeconds)
//         {
//             var syncedConfig = originalConfig;
//             syncedConfig.speed = ApplyDaySynchronization(originalConfig.speed, dayLengthInSeconds);
//             return syncedConfig;
//         }
//
//         /// <summary>
//         /// Interpolates between two angles with proper wrapping for smooth transitions.
//         /// Handles 360-degree wraparound to prevent sudden jumps during interpolation.
//         /// </summary>
//         /// <param name="fromAngle">Starting angle in degrees</param>
//         /// <param name="toAngle">Target angle in degrees</param>
//         /// <param name="progress">Interpolation progress (0-1)</param>
//         /// <returns>Interpolated angle in degrees</returns>
//         private float InterpolateAngle(float fromAngle, float toAngle, float progress)
//         {
//             // Normalize angles to 0-360 range
//             fromAngle = NormalizeAngle(fromAngle);
//             toAngle = NormalizeAngle(toAngle);
//
//             // Calculate the shortest path between angles
//             float difference = toAngle - fromAngle;
//             
//             if (difference > 180f)
//             {
//                 difference -= 360f;
//             }
//             else if (difference < -180f)
//             {
//                 difference += 360f;
//             }
//
//             // Interpolate and normalize result
//             float result = fromAngle + (difference * progress);
//             return NormalizeAngle(result);
//         }
//
//         /// <summary>
//         /// Normalizes an angle to the 0-360 degree range.
//         /// Ensures consistent angle representation for calculations.
//         /// </summary>
//         /// <param name="angle">Angle to normalize</param>
//         /// <returns>Normalized angle in 0-360 range</returns>
//         private float NormalizeAngle(float angle)
//         {
//             angle = angle % 360f;
//             if (angle < 0f)
//                 angle += 360f;
//             return angle;
//         }
//
//         /// <summary>
//         /// Logs detailed calculation information for debugging purposes.
//         /// Only active when debug logging is enabled.
//         /// </summary>
//         /// <param name="celestialBodyName">Name of celestial body being calculated</param>
//         /// <param name="celestialTime">Current celestial time</param>
//         /// <param name="result">Calculated rotation result</param>
//         private void LogCalculationDetails(string celestialBodyName, float celestialTime, Vector3 result)
//         {
//             #if UNITY_EDITOR || DEVELOPMENT_BUILD
//             Debug.Log($"[CelestialCalculator] {celestialBodyName} - Time: {celestialTime:F3}, Rotation: ({result.x:F2}, {result.y:F2}, {result.z:F2})");
//             #endif
//         }
//
//         #endregion
//
//         #region Context Menu Debug Methods
//
//         #if UNITY_EDITOR
//         /// <summary>
//         /// Context menu method for testing calculator functionality in editor.
//         /// </summary>
//         [UnityEngine.ContextMenu("Test Calculator")]
//         private void TestCalculator()
//         {
//             Debug.Log(GetDiagnosticInfo());
//         }
//
//         /// <summary>
//         /// Context menu method for validating calculator state in editor.
//         /// </summary>
//         [UnityEngine.ContextMenu("Validate Calculator")]
//         private void ValidateCalculator()
//         {
//             bool isValid = IsInitialized();
//             string status = isValid ? "VALID" : "INVALID";
//             Debug.Log($"[CelestialCalculator] Validation Result: {status}");
//             
//             if (!isValid)
//             {
//                 Debug.LogWarning("Calculator issues detected. Check diagnostic info for details.");
//             }
//         }
//         #endif
//
//         #endregion
//     }
// }