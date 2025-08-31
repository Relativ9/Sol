// using UnityEngine;
//
// namespace Sol
// {
//     /// <summary>
//     /// Interface for celestial body rotation calculations and day synchronization.
//     /// Provides abstraction for different calculation implementations.
//     /// </summary>
//     public interface ICelestialCalculator
//     {
//         /// <summary>
//         /// Initialize the calculator with required dependencies
//         /// </summary>
//         void Initialize(ITimeManager timeManager);
//
//         /// <summary>
//         /// Calculate celestial rotation for a specific celestial body
//         /// </summary>
//         Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime, bool isMoon = false);
//
//         /// <summary>
//         /// Calculate celestial rotation with explicit day synchronization
//         /// </summary>
//         Vector3 CalculateCelestialRotationWithDaySync(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime, float dayLengthInSeconds, bool isMoon = false);
//
//         /// <summary>
//         /// Interpolate between two seasonal configurations
//         /// </summary>
//         Vector3 InterpolateCelestialRotation(SeasonalData fromSeasonalData, SeasonalData toSeasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime, float interpolationFactor, bool isMoon = false);
//
//         /// <summary>
//         /// Validate seasonal data configuration
//         /// </summary>
//         bool ValidateSeasonalData(SeasonalData seasonalData);
//
//         /// <summary>
//         /// Control day synchronization
//         /// </summary>
//         void SetDaySynchronizationEnabled(bool enabled);
//         bool IsDaySynchronizationEnabled();
//
//         /// <summary>
//         /// Day synchronization utilities
//         /// </summary>
//         bool IsYAxisSynchronizedWithDay(float yAxisSpeed, float dayLengthInSeconds);
//         float GetRequiredYAxisSpeedForDay(float dayLengthInSeconds);
//
//         /// <summary>
//         /// Cleanup resources
//         /// </summary>
//         void Cleanup();
//     }
// }
//
// using UnityEngine;
//
// namespace Sol
// {
//     /// <summary>
//     /// Celestial calculator implementation for celestial body rotations
//     /// Handles axis synchronization, oscillation modes, day-length synchronization, and moon orbital periods
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
//         public Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime, bool isMoon = false)
//         {
//             if (seasonalData == null) return baseRotation;
//
//             // Apply day synchronization if enabled and we have a time manager
//             if (enableDaySynchronization && _timeManager != null && _timeManager.WorldTimeData != null)
//             {
//                 return CalculateCelestialRotationWithDaySync(seasonalData, celestialBodyName, baseRotation, celestialTime, _timeManager.WorldTimeData.dayLengthInSeconds, isMoon);
//             }
//             return CalculateCelestialRotationInternal(seasonalData, celestialBodyName, baseRotation, celestialTime, isMoon);
//         }
//
//         /// <summary>
//         /// Calculates celestial rotation with explicit day length for synchronization
//         /// </summary>
//         public Vector3 CalculateCelestialRotationWithDaySync(SeasonalData seasonalData, string celestialBodyName, 
//             Vector3 baseRotation, float celestialTime, float dayLengthInSeconds, bool isMoon = false)
//         {
//             if (seasonalData == null) return baseRotation;
//
//             return CalculateCelestialRotationInternal(seasonalData, celestialBodyName, baseRotation, celestialTime, isMoon);
//         }
//
//         /// <summary>
//         /// Interpolates between two seasonal calculations for smooth seasonal transitions
//         /// </summary>
//         public Vector3 InterpolateCelestialRotation(SeasonalData fromSeasonalData, SeasonalData toSeasonalData, 
//             string celestialBodyName, Vector3 baseRotation, float celestialTime, float interpolationFactor, bool isMoon = false)
//         {
//             if (fromSeasonalData == null || toSeasonalData == null) 
//                 return baseRotation;
//
//             // Calculate rotation for both seasons
//             Vector3 fromRotation = CalculateCelestialRotation(fromSeasonalData, celestialBodyName, baseRotation, celestialTime, isMoon);
//             Vector3 toRotation = CalculateCelestialRotation(toSeasonalData, celestialBodyName, baseRotation, celestialTime, isMoon);
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
//         private Vector3 CalculateCelestialRotationInternal(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime, bool isMoon)
//         {
//             // Get the celestial body from the seasonal data
//             var celestialBody = seasonalData.GetCelestialBodyByName(celestialBodyName);
//             if (celestialBody == null)
//             {
//                 Debug.LogWarning($"[CelestialCalculator] Celestial body '{celestialBodyName}' not found in seasonal data");
//                 return baseRotation;
//             }
//
//             return CalculateCelestialBodyRotation(celestialBody, baseRotation, celestialTime, isMoon);
//         }
//
//         /// <summary>
//         /// Calculates rotation for a specific celestial body
//         /// </summary>
//         private Vector3 CalculateCelestialBodyRotation(SeasonalData.CelestialBody celestialBody, Vector3 baseRotation, float celestialTime, bool isMoon)
//         {
//             if (!celestialBody.active) return baseRotation;
//
//             Vector3 rotation = baseRotation;
//
//             // For moons, apply orbital period drift to the base rotation
//             if (isMoon)
//             {
//                 rotation = ApplyMoonOrbitalDrift(rotation, celestialBody, celestialTime);
//             }
//
//             // Calculate X-axis (elevation) rotation
//             if (celestialBody.xAxisEnabled)
//             {
//                 float xAxisSpeed = GetEffectiveXAxisSpeed(
//                     celestialBody.xAxisSpeed,
//                     celestialBody.syncXWithY,
//                     celestialBody.yAxisEnabled,
//                     celestialBody.yAxisSpeed,
//                     celestialBody.yAxisOverrideSpeed
//                 );
//
//                 rotation.x = CalculateAxisRotation(
//                     celestialBody.xAxisMode,
//                     xAxisSpeed,
//                     celestialBody.xAxisMinRange,
//                     celestialBody.xAxisMaxRange,
//                     celestialTime,
//                     rotation.x
//                 );
//             }
//
//             // Calculate Y-axis (azimuth) rotation
//             if (celestialBody.yAxisEnabled)
//             {
//                 float yAxisSpeed = GetEffectiveYAxisSpeed(
//                     celestialBody.yAxisSpeed, 
//                     celestialBody.yAxisOverrideSpeed,
//                     celestialBody.name
//                 );
//
//                 rotation.y = CalculateAxisRotation(
//                     celestialBody.yAxisMode,
//                     yAxisSpeed,
//                     celestialBody.yAxisMinRange,
//                     celestialBody.yAxisMaxRange,
//                     celestialTime,
//                     rotation.y
//                 );
//             }
//
//             return rotation;
//         }
//
//         /// <summary>
//         /// Applies orbital period drift to moon base rotation
//         /// This creates the monthly drift effect for moons
//         /// </summary>
//         private Vector3 ApplyMoonOrbitalDrift(Vector3 baseRotation, SeasonalData.CelestialBody moon, float celestialTime)
//         {
//             if (_timeManager?.WorldTimeData == null) return baseRotation;
//
//             // Get current day from time manager
//             int currentDay = _timeManager.CurrentDay;
//             
//             // Calculate orbital progress (0-1) based on current day and orbital period
//             float orbitalProgress = (currentDay % moon.orbitalPeriod) / moon.orbitalPeriod;
//             
//             // Convert to degrees (0-360)
//             float orbitalDrift = orbitalProgress * 360f;
//             
//             // Apply drift to base rotation (this affects the starting position for daily rotation)
//             Vector3 driftedRotation = baseRotation;
//             driftedRotation.y += orbitalDrift;
//             
//             return driftedRotation;
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
//                     // EXACTLY like your original working code
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

using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Interface for celestial body rotation calculations and day synchronization.
    /// Provides abstraction for different calculation implementations.
    /// </summary>
    public interface ICelestialCalculator
    {
        /// <summary>
        /// Initialize the calculator with required dependencies
        /// </summary>
        void Initialize(ITimeManager timeManager);

        /// <summary>
        /// Calculate celestial rotation for a specific celestial body
        /// </summary>
        Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime, bool isMoon = false);

        /// <summary>
        /// Calculate celestial rotation with explicit day synchronization
        /// </summary>
        Vector3 CalculateCelestialRotationWithDaySync(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime, float dayLengthInSeconds, bool isMoon = false);

        /// <summary>
        /// Interpolate between two seasonal configurations
        /// </summary>
        Vector3 InterpolateCelestialRotation(SeasonalData fromSeasonalData, SeasonalData toSeasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime, float interpolationFactor, bool isMoon = false);

        /// <summary>
        /// Validate seasonal data configuration
        /// </summary>
        bool ValidateSeasonalData(SeasonalData seasonalData);

        /// <summary>
        /// Control day synchronization
        /// </summary>
        void SetDaySynchronizationEnabled(bool enabled);
        bool IsDaySynchronizationEnabled();

        /// <summary>
        /// Day synchronization utilities
        /// </summary>
        bool IsYAxisSynchronizedWithDay(float yAxisSpeed, float dayLengthInSeconds);
        float GetRequiredYAxisSpeedForDay(float dayLengthInSeconds);

        /// <summary>
        /// Cleanup resources
        /// </summary>
        void Cleanup();
    }
}