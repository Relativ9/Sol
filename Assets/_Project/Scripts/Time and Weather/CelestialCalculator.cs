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
//         public static void CalculatePathRanges(float pathAngle, float sunriseElevation, out float minRange, out float maxRange)
//         {
//             // Calculate the elevation change needed for the desired path angle
//             // Over 180° of Y rotation (horizon to horizon)
//             float yAxisSpan = 180f; // Degrees of Y rotation from sunrise to sunset
//     
//             // Calculate elevation change using trigonometry
//             float elevationChange = yAxisSpan * Mathf.Tan(pathAngle * Mathf.Deg2Rad);
//     
//             // Determine min and max based on sunrise elevation
//             if (pathAngle > 0f)
//             {
//                 // Rising path: starts at sunrise elevation, goes higher
//                 minRange = sunriseElevation;
//                 maxRange = sunriseElevation - elevationChange; // Lower X values = higher in sky
//             }
//             else
//             {
//                 // Flat path: same elevation throughout
//                 minRange = maxRange = sunriseElevation;
//             }
//     
//             // Clamp to avoid gimbal lock zone (80°-100°)
//             minRange = ClampAwayFromGimbalLock(minRange);
//             maxRange = ClampAwayFromGimbalLock(maxRange);
//         }
//         
//         private static float ClampAwayFromGimbalLock(float elevation)
//         {
//             // If in the danger zone, push it to a safe value
//             if (elevation >= 80f && elevation <= 100f)
//             {
//                 return elevation < 90f ? 79f : 101f;
//             }
//             return elevation;
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
//             // if (isMoon)
//             // {
//             //     rotation = ApplyMoonOrbitalDrift(rotation, celestialBody, celestialTime);
//             // }
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
//                     rotation.x,
//                     celestialBody.invertDayNightCycle  // Pass the inversion flag
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
//                     rotation.y,
//                     celestialBody.invertDayNightCycle  // Pass the inversion flag
//                 );
//             }
//
//             return rotation;
//         }
//         // private Vector3 CalculateCelestialBodyRotation(SeasonalData.CelestialBody celestialBody, Vector3 baseRotation, float celestialTime, bool isMoon)
//         // {
//         //     if (!celestialBody.active) return baseRotation;
//         //
//         //     Vector3 rotation = baseRotation;
//         //
//         //     // For moons, apply orbital period drift to the base rotation
//         //     if (isMoon)
//         //     {
//         //         rotation = ApplyMoonOrbitalDrift(rotation, celestialBody, celestialTime);
//         //     }
//         //
//         //     // Calculate X-axis (elevation) rotation
//         //     if (celestialBody.xAxisEnabled)
//         //     {
//         //         float xAxisSpeed = GetEffectiveXAxisSpeed(
//         //             celestialBody.xAxisSpeed,
//         //             celestialBody.syncXWithY,
//         //             celestialBody.yAxisEnabled,
//         //             celestialBody.yAxisSpeed,
//         //             celestialBody.yAxisOverrideSpeed
//         //         );
//         //
//         //         rotation.x = CalculateAxisRotation(
//         //             celestialBody.xAxisMode,
//         //             xAxisSpeed,
//         //             celestialBody.xAxisMinRange,
//         //             celestialBody.xAxisMaxRange,
//         //             celestialTime,
//         //             rotation.x
//         //         );
//         //     }
//         //
//         //     // Calculate Y-axis (azimuth) rotation
//         //     if (celestialBody.yAxisEnabled)
//         //     {
//         //         float yAxisSpeed = GetEffectiveYAxisSpeed(
//         //             celestialBody.yAxisSpeed, 
//         //             celestialBody.yAxisOverrideSpeed,
//         //             celestialBody.name
//         //         );
//         //
//         //         rotation.y = CalculateAxisRotation(
//         //             celestialBody.yAxisMode,
//         //             yAxisSpeed,
//         //             celestialBody.yAxisMinRange,
//         //             celestialBody.yAxisMaxRange,
//         //             celestialTime,
//         //             rotation.y
//         //         );
//         //     }
//         //
//         //     return rotation;
//         // }
//
//         // /// <summary>
//         // /// Applies orbital period drift to moon base rotation
//         // /// This creates the monthly drift effect for moons
//         // /// </summary>
//         // private Vector3 ApplyMoonOrbitalDrift(Vector3 baseRotation, SeasonalData.CelestialBody moon, float celestialTime)
//         // {
//         //     if (_timeManager?.WorldTimeData == null) return baseRotation;
//         //
//         //     // Get current day from time manager
//         //     int currentDay = _timeManager.CurrentDayOfYear;
//         //     
//         //     // Calculate orbital progress (0-1) based on current day and orbital period
//         //     float orbitalProgress = (currentDay % moon.orbitalPeriod) / moon.orbitalPeriod;
//         //     
//         //     // Convert to degrees (0-360)
//         //     float orbitalDrift = orbitalProgress * 360f;
//         //     
//         //     // Apply drift to base rotation (this affects the starting position for daily rotation)
//         //     Vector3 driftedRotation = baseRotation;
//         //     driftedRotation.y += orbitalDrift;
//         //     
//         //     return driftedRotation;
//         // }
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
//         // private float CalculateAxisRotation(CelestialRotationMode mode, float speed, float minRange, float maxRange, 
//         //     float celestialTime, float baseValue)
//         // {
//         //     switch (mode)
//         //     {
//         //         case CelestialRotationMode.Continuous:
//         //             // EXACTLY like your original working code
//         //             return baseValue + (speed * celestialTime);
//         //
//         //         case CelestialRotationMode.Oscillate:
//         //             // Oscillating rotation between min and max ranges
//         //             // Complete one full cycle (min→max→min) per oscillation period
//         //             float oscillationValue = Mathf.Sin(celestialTime * speed); // -1 to 1
//         //             float normalizedValue = (oscillationValue + 1f) * 0.5f; // 0 to 1
//         //             return Mathf.Lerp(minRange, maxRange, normalizedValue);
//         //
//         //         default:
//         //             return baseValue;
//         //     }
//         // }
//         
//         private float CalculateAxisRotation(CelestialRotationMode mode, float speed, float minRange, float maxRange, 
//             float celestialTime, float baseValue, bool invertCycle = false)
//         {
//             float effectiveCelestialTime = invertCycle ? -celestialTime : celestialTime;
//     
//             switch (mode)
//             {
//                 case CelestialRotationMode.Continuous:
//                     // EXACTLY like your original working code, but with potential inversion
//                     return baseValue + (speed * effectiveCelestialTime);
//
//                 case CelestialRotationMode.Oscillate:
//                     // Oscillating rotation between min and max ranges
//                     // Complete one full cycle (min→max→min) per oscillation period
//                     float oscillationValue = Mathf.Sin(effectiveCelestialTime * speed); // -1 to 1
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
    public class CelestialCalculator : ICelestialCalculator
    {
        public bool enableDebugLogging { get; set; } = false;
        private TimeManager timeManager;

        public CelestialCalculator(TimeManager timeManager)
        {
            this.timeManager = timeManager;
        }

        public Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, 
            Vector3 baseRotation, float celestialTime, bool isMoon)
        {
            if (seasonalData == null)
            {
                if (enableDebugLogging)
                    Debug.LogWarning($"[CelestialCalculator] No seasonal data provided for {celestialBodyName}");
                return baseRotation;
            }

            var celestialBody = seasonalData.GetCelestialBodyByName(celestialBodyName);
            if (celestialBody == null || !celestialBody.active)
            {
                if (enableDebugLogging)
                    Debug.LogWarning($"[CelestialCalculator] Celestial body '{celestialBodyName}' not found or inactive");
                return baseRotation;
            }

            // Calculate using quaternions to avoid gimbal lock
            Quaternion celestialRotation = CalculateCelestialRotationQuaternion(celestialBody, celestialTime, isMoon);
            
            // Apply base rotation offset
            Quaternion baseRotationQuat = Quaternion.Euler(baseRotation);
            Quaternion finalRotation = celestialRotation * baseRotationQuat;

            // Convert back to Euler for Unity transform
            Vector3 eulerResult = finalRotation.eulerAngles;

            if (enableDebugLogging && Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
            {
                Debug.Log($"[CelestialCalculator] {celestialBodyName}: Azimuth={eulerResult.y:F1}°, Elevation={eulerResult.x:F1}°");
            }

            return eulerResult;
        }

        private Quaternion CalculateCelestialRotationQuaternion(CelestialBody celestialBody, float celestialTime, bool isMoon)
        {
            // Calculate azimuth (Y-axis rotation)
            float azimuth = 0f;
            if (celestialBody.yAxisEnabled)
            {
                azimuth = CalculateAzimuth(celestialBody, celestialTime, isMoon);
            }

            // Create the orbital path using proper spherical coordinates
            return CreateOrbitalPathSpherical(celestialBody, azimuth);
        }

        private Quaternion CreateOrbitalPathSpherical(CelestialBody celestialBody, float azimuth)
        {
            // // Convert to spherical coordinates for proper orbital path
            // // This creates the same path as the original Euler system but without gimbal lock
            //
            // // Calculate elevation based on orbital angle and azimuth (same as original Euler system)
            // float azimuthRad = azimuth * Mathf.Deg2Rad;
            // float elevationChange = Mathf.Sin(azimuthRad) * celestialBody.orbitalAngle;
            // float elevation = celestialBody.baseElevation - elevationChange;
            //
            // // Convert spherical coordinates (azimuth, elevation) to quaternion rotation
            // // This is equivalent to: Quaternion.Euler(elevation, azimuth, 0)
            // // But using quaternion multiplication to avoid gimbal lock
            //
            // Quaternion azimuthRotation = Quaternion.AngleAxis(azimuth, Vector3.up);
            // Quaternion elevationRotation = Quaternion.AngleAxis(elevation, Vector3.right);
            //
            // // Combine rotations: first elevation, then azimuth
            // return azimuthRotation * elevationRotation;
            
            // Calculate elevation with proper phase for noon = highest
            // Use cosine instead of sine, or shift the phase
            float azimuthRad = azimuth * Mathf.Deg2Rad;
    
            // Shift by 90° in the calculation so noon (180°) gives maximum elevation
            float phaseShiftedAzimuth = azimuth - 90f; // Shift back 90°
            float phaseShiftedRad = phaseShiftedAzimuth * Mathf.Deg2Rad;
    
            float elevationChange = Mathf.Sin(phaseShiftedRad) * celestialBody.orbitalAngle;
            float elevation = celestialBody.baseElevation - elevationChange;
    
            // Convert to quaternion
            Quaternion azimuthRotation = Quaternion.AngleAxis(azimuth, Vector3.up);
            Quaternion elevationRotation = Quaternion.AngleAxis(elevation, Vector3.right);
    
            return azimuthRotation * elevationRotation;
        }

        private float CalculateAzimuth(CelestialBody celestialBody, float celestialTime, bool isMoon)
        {
            float effectiveSpeed = GetEffectiveYAxisSpeed(celestialBody);
    
            // Direct mapping: celestialTime 0.0-1.0 maps to azimuth 0°-360°
            // This gives us: dawn=0°, noon=180°, dusk=360°/0°
            float baseAzimuth = effectiveSpeed * celestialTime * 360f;
    
            // Add phase offset and orbital drift
            float phaseOffset = celestialBody.phaseOffset;
            float orbitalDrift = 0f;
            if (isMoon && celestialBody.orbitalPeriod > 0)
            {
                orbitalDrift = CalculateOrbitalDrift(celestialBody);
            }
    
            float finalAzimuth = baseAzimuth + phaseOffset + orbitalDrift;
            return Mathf.Repeat(finalAzimuth, 360f);
            
            // // Get effective speed (sync with day length if enabled)
            // float effectiveSpeed = GetEffectiveYAxisSpeed(celestialBody);
            //
            // // Calculate base azimuth (continuous rotation)
            // float baseAzimuth = effectiveSpeed * celestialTime * 360f; // Convert to degrees
            //
            // // Add phase offset for moons or timing differences
            // float phaseOffset = celestialBody.phaseOffset;
            //
            // // Add orbital drift for moons
            // float orbitalDrift = 0f;
            // if (isMoon && celestialBody.orbitalPeriod > 0)
            // {
            //     orbitalDrift = CalculateOrbitalDrift(celestialBody);
            // }
            //
            // // Combine all components
            // float finalAzimuth = baseAzimuth + phaseOffset + orbitalDrift;
            //
            // // Normalize to 0-360 range
            // return Mathf.Repeat(finalAzimuth, 360f);
        }

        private float GetEffectiveYAxisSpeed(CelestialBody celestialBody)
        {
            if (celestialBody.yAxisOverrideSpeed && timeManager?.WorldTimeData != null)
            {
                // Sync with day length: 1 full rotation per day
                float dayLengthInSeconds = timeManager.WorldTimeData.dayLengthInSeconds;
                if (dayLengthInSeconds > 0)
                {
                    if (enableDebugLogging && Time.frameCount % 600 == 0) // Log every 10 seconds
                    {
                        Debug.Log($"[CelestialCalculator] Y-axis speed synced with day length: 1 rotation per {dayLengthInSeconds}s");
                    }
                    return 1f; // 1 rotation per day regardless of day length
                }
            }
            
            return celestialBody.yAxisSpeed;
        }

        private float CalculateOrbitalDrift(CelestialBody celestialBody)
        {
            if (timeManager == null) return 0f;
            
            int currentDay = timeManager.CurrentDayOfYear;
            float timeWithinDay = timeManager.CelestialTime % 1f; // Get fractional part for smooth drift
            
            // Total elapsed time in days (including fractional part for smooth movement)
            float totalElapsedDays = currentDay + timeWithinDay;
            
            // Calculate orbital progress (0-1) based on total elapsed time and orbital period
            float orbitalProgress = (totalElapsedDays % celestialBody.orbitalPeriod) / celestialBody.orbitalPeriod;
            
            // Convert to degrees (0-360)
            return orbitalProgress * 360f;
        }
    }
}

// using UnityEngine;
//
// namespace Sol
// {
//     public class CelestialCalculator : ICelestialCalculator
//     {
//         public bool enableDebugLogging { get; set; } = false;
//         private TimeManager timeManager;
//
//         public CelestialCalculator(TimeManager timeManager)
//         {
//             this.timeManager = timeManager;
//         }
//
//         public Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, 
//             Vector3 baseRotation, float celestialTime, bool isMoon)
//         {
//             if (seasonalData == null)
//             {
//                 if (enableDebugLogging)
//                     Debug.LogWarning($"[CelestialCalculator] No seasonal data provided for {celestialBodyName}");
//                 return baseRotation;
//             }
//
//             var celestialBody = seasonalData.GetCelestialBodyByName(celestialBodyName);
//             if (celestialBody == null || !celestialBody.active)
//             {
//                 if (enableDebugLogging)
//                     Debug.LogWarning($"[CelestialCalculator] Celestial body '{celestialBodyName}' not found or inactive");
//                 return baseRotation;
//             }
//
//             Vector3 rotation = baseRotation;
//
//             // Calculate Y-axis (Azimuth) - Continuous orbital motion
//             if (celestialBody.yAxisEnabled)
//             {
//                 rotation.y = CalculateAzimuth(celestialBody, celestialTime, isMoon);
//             }
//
//             // Calculate X-axis (Elevation) - Based on orbital angle and current azimuth
//             rotation.x = CalculateElevationFromOrbitalAngle(celestialBody, rotation.y);
//
//             if (enableDebugLogging && Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
//             {
//                 Debug.Log($"[CelestialCalculator] {celestialBodyName}: Azimuth={rotation.y:F1}°, Elevation={rotation.x:F1}°");
//             }
//
//             return rotation;
//         }
//
//         private float CalculateAzimuth(CelestialBody celestialBody, float celestialTime, bool isMoon)
//         {
//             // Get effective speed (sync with day length if enabled)
//             float effectiveSpeed = GetEffectiveYAxisSpeed(celestialBody);
//             
//             // Calculate base azimuth (continuous rotation)
//             float baseAzimuth = effectiveSpeed * celestialTime * 360f; // Convert to degrees
//             
//             // Add phase offset for moons or timing differences
//             float phaseOffset = celestialBody.phaseOffset;
//             
//             // Add orbital drift for moons
//             float orbitalDrift = 0f;
//             if (isMoon && celestialBody.orbitalPeriod > 0)
//             {
//                 orbitalDrift = CalculateOrbitalDrift(celestialBody);
//             }
//             
//             // Combine all components
//             float finalAzimuth = baseAzimuth + phaseOffset + orbitalDrift;
//             
//             // Normalize to 0-360 range
//             return Mathf.Repeat(finalAzimuth, 360f);
//         }
//
//         private float CalculateElevationFromOrbitalAngle(CelestialBody celestialBody, float currentAzimuth)
//         {
//             // Calculate elevation based on orbital angle and current azimuth position
//             // This creates an angled orbital path around the sky
//             
//             float orbitalAngleRad = celestialBody.orbitalAngle * Mathf.Deg2Rad;
//             float azimuthRad = currentAzimuth * Mathf.Deg2Rad;
//             
//             // Calculate elevation change based on orbital angle
//             // When azimuth = 0°, elevation = baseElevation
//             // When azimuth = 180°, elevation = baseElevation + (orbital angle effect)
//             float elevationChange = Mathf.Sin(azimuthRad) * celestialBody.orbitalAngle;
//             
//             // Apply the elevation change to base elevation
//             float finalElevation = celestialBody.baseElevation - elevationChange; // Subtract because lower X = higher in sky
//             
//             // Clamp to avoid gimbal lock issues
//             finalElevation = ClampAwayFromGimbalLock(finalElevation);
//             
//             return finalElevation;
//         }
//
//         private float ClampAwayFromGimbalLock(float elevation)
//         {
//             // Avoid gimbal lock zone around 90°
//             if (elevation >= 85f && elevation <= 95f)
//             {
//                 return elevation < 90f ? 84f : 96f;
//             }
//             return elevation;
//         }
//
//         private float GetEffectiveYAxisSpeed(CelestialBody celestialBody)
//         {
//             if (celestialBody.yAxisOverrideSpeed && timeManager?.WorldTimeData != null)
//             {
//                 // Sync with day length: 1 full rotation per day
//                 float dayLengthInSeconds = timeManager.WorldTimeData.dayLengthInSeconds;
//                 if (dayLengthInSeconds > 0)
//                 {
//                     if (enableDebugLogging && Time.frameCount % 600 == 0) // Log every 10 seconds
//                     {
//                         Debug.Log($"[CelestialCalculator] Y-axis speed synced with day length: 1 rotation per {dayLengthInSeconds}s");
//                     }
//                     return 1f; // 1 rotation per day regardless of day length
//                 }
//             }
//             
//             return celestialBody.yAxisSpeed;
//         }
//
//         private float CalculateOrbitalDrift(CelestialBody celestialBody)
//         {
//             if (timeManager == null) return 0f;
//             
//             int currentDay = timeManager.CurrentDayOfYear;
//             float timeWithinDay = timeManager.CelestialTime % 1f; // Get fractional part for smooth drift
//             
//             // Total elapsed time in days (including fractional part for smooth movement)
//             float totalElapsedDays = currentDay + timeWithinDay;
//             
//             // Calculate orbital progress (0-1) based on total elapsed time and orbital period
//             float orbitalProgress = (totalElapsedDays % celestialBody.orbitalPeriod) / celestialBody.orbitalPeriod;
//             
//             // Convert to degrees (0-360)
//             return orbitalProgress * 360f;
//         }
//     }
// }