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
//         public Quaternion CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, 
//             Vector3 baseRotation, float celestialTime, bool isMoon)
//         {
//             if (seasonalData == null)
//             {
//                 if (enableDebugLogging)
//                     Debug.LogWarning($"[CelestialCalculator] No seasonal data provided for {celestialBodyName}");
//                 return Quaternion.Euler(baseRotation);
//             }
//
//             var celestialBody = seasonalData.GetCelestialBodyByName(celestialBodyName);
//             if (celestialBody == null || !celestialBody.active)
//             {
//                 if (enableDebugLogging)
//                     Debug.LogWarning($"[CelestialCalculator] Celestial body '{celestialBodyName}' not found or inactive");
//                 return Quaternion.Euler(baseRotation);
//             }
//
//             // Calculate azimuth
//             float azimuth = 0f;
//             if (celestialBody.yAxisEnabled)
//             {
//                 azimuth = CalculateAzimuth(celestialBody, celestialTime, isMoon);
//             }
//
//             // Calculate using pure quaternions
//             Quaternion celestialRotation = CreateOrbitalPathSpherical(seasonalData, celestialBody, azimuth);
//             
//             // Apply base rotation offset
//             Quaternion baseRotationQuat = Quaternion.Euler(baseRotation);
//             Quaternion finalRotation = celestialRotation * baseRotationQuat;
//
//             if (enableDebugLogging && Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
//             {
//                 Vector3 eulerForLogging = finalRotation.eulerAngles;
//                 Debug.Log($"[CelestialCalculator] {celestialBodyName}: Azimuth={eulerForLogging.y:F1}°, Elevation={eulerForLogging.x:F1}°");
//             }
//
//             return finalRotation;
//         }
//         
//         private Quaternion CreateOrbitalPathSpherical(SeasonalData seasonalData, CelestialBody celestialBody, float azimuth)
//         {
//             float effectiveOrbitalAngle = seasonalData.GetEffectiveOrbitalAngle(celestialBody);
//             float individualBaseElevation = celestialBody.baseElevation;
//
//             // Calculate elevation variation (same human-friendly math)
//             float phaseShiftedAzimuth = azimuth - 90f;
//             float phaseShiftedRad = phaseShiftedAzimuth * Mathf.Deg2Rad;
//             float elevationChange = Mathf.Sin(phaseShiftedRad) * effectiveOrbitalAngle;
//             float elevation = individualBaseElevation - elevationChange;
//
//             // Pure quaternion approach - build rotation step by step
//             return CreatePureQuaternionRotation(azimuth, elevation);
//         }
//
//         private Quaternion CreatePureQuaternionRotation(float azimuth, float elevation)
//         {
//             // Convert to direction vector using spherical coordinates
//             float azimuthRad = azimuth * Mathf.Deg2Rad;
//             float elevationRad = (90f - elevation) * Mathf.Deg2Rad; // Convert to mathematical elevation
//             
//             Vector3 direction = new Vector3(
//                 Mathf.Sin(elevationRad) * Mathf.Sin(azimuthRad),  // X
//                 Mathf.Cos(elevationRad),                          // Y (up)
//                 Mathf.Sin(elevationRad) * Mathf.Cos(azimuthRad)   // Z
//             );
//             
//             // Create quaternion that looks in this direction
//             return Quaternion.LookRotation(direction, Vector3.up);
//         }
//
//         private float CalculateAzimuth(CelestialBody celestialBody, float celestialTime, bool isMoon)
//         {
//             float effectiveSpeed = GetEffectiveYAxisSpeed(celestialBody);
//     
//             // Direct mapping: celestialTime 0.0-1.0 maps to azimuth 0°-360°
//             // This gives us: dawn=0°, noon=180°, dusk=360°/0°
//             float baseAzimuth = effectiveSpeed * celestialTime * 360f;
//     
//             // Add phase offset and orbital drift
//             float phaseOffset = celestialBody.phaseOffset;
//             float orbitalDrift = 0f;
//             if (isMoon && celestialBody.orbitalPeriod > 0)
//             {
//                 orbitalDrift = CalculateOrbitalDrift(celestialBody);
//             }
//     
//             float finalAzimuth = baseAzimuth + phaseOffset + orbitalDrift;
//             return Mathf.Repeat(finalAzimuth, 360f);
//         }
//
//         private float GetEffectiveYAxisSpeed(CelestialBody celestialBody)
//         {
//             if (celestialBody.yAxisOverrideSpeed && timeManager?.worldTimeData != null)
//             {
//                 // Sync with day length: 1 full rotation per day
//                 float dayLengthInSeconds = timeManager.worldTimeData.dayLengthInSeconds;
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
//             int currentDay = timeManager.CurrentDay;
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

using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Calculates celestial body rotations using pure quaternion mathematics to avoid gimbal lock.
    /// Handles both sun and moon positioning with realistic orbital mechanics, seasonal variations,
    /// and multi-day lunar cycles. Uses spherical coordinate conversion for smooth movement at all angles.
    /// </summary>
    public class CelestialCalculator : ICelestialCalculator
    {
        /// <summary>
        /// Enable or disable debug logging for rotation calculations and celestial body information
        /// </summary>
        public bool enableDebugLogging { get; set; } = false;
        
        /// <summary>
        /// Reference to the TimeManager for accessing current time and day progression
        /// </summary>
        private TimeManager timeManager;

        #region Constructor

        /// <summary>
        /// Initialize the celestial calculator with a TimeManager reference
        /// </summary>
        /// <param name="timeManager">TimeManager instance for accessing current time data</param>
        public CelestialCalculator(TimeManager timeManager)
        {
            this.timeManager = timeManager;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Calculate the rotation for a celestial body as a pure quaternion (no gimbal lock).
        /// This is the main entry point for celestial rotation calculations.
        /// </summary>
        /// <param name="seasonalData">The seasonal data containing celestial body configurations and orbital parameters</param>
        /// <param name="celestialBodyName">Name of the celestial body to calculate rotation for (must match SeasonalData exactly)</param>
        /// <param name="baseRotation">Base rotation offset to apply for fine-tuning alignment</param>
        /// <param name="celestialTime">Current celestial time (0-1 represents one full day cycle)</param>
        /// <param name="isMoon">Whether this celestial body is a moon (enables orbital drift over multiple days) or a sun/star (consistent daily pattern)</param>
        /// <returns>The calculated rotation as a pure quaternion, ready for direct application to transforms</returns>
        public Quaternion CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, 
            Vector3 baseRotation, float celestialTime, bool isMoon)
        {
            // Validate seasonal data
            if (seasonalData == null)
            {
                if (enableDebugLogging)
                    Debug.LogWarning($"[CelestialCalculator] No seasonal data provided for {celestialBodyName}");
                return Quaternion.Euler(baseRotation);
            }

            // Find and validate celestial body configuration
            var celestialBody = seasonalData.GetCelestialBodyByName(celestialBodyName);
            if (celestialBody == null || !celestialBody.active)
            {
                if (enableDebugLogging)
                    Debug.LogWarning($"[CelestialCalculator] Celestial body '{celestialBodyName}' not found or inactive");
                return Quaternion.Euler(baseRotation);
            }

            // Calculate azimuth (horizontal position) if Y-axis rotation is enabled
            float azimuth = 0f;
            if (celestialBody.yAxisEnabled)
            {
                azimuth = CalculateAzimuth(celestialBody, celestialTime, isMoon);
            }

            // Calculate celestial rotation using pure quaternion mathematics
            Quaternion celestialRotation = CreateOrbitalPathSpherical(seasonalData, celestialBody, azimuth);
            
            // Apply base rotation offset for fine-tuning
            Quaternion baseRotationQuat = Quaternion.Euler(baseRotation);
            Quaternion finalRotation = celestialRotation * baseRotationQuat;

            // Log rotation information periodically for debugging
            if (enableDebugLogging && Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
            {
                Vector3 eulerForLogging = finalRotation.eulerAngles;
                Debug.Log($"[CelestialCalculator] {celestialBodyName}: Azimuth={eulerForLogging.y:F1}°, Elevation={eulerForLogging.x:F1}°");
            }

            return finalRotation;
        }

        #endregion

        #region Core Calculation Methods

        /// <summary>
        /// Create the orbital path for a celestial body using spherical coordinates and pure quaternion rotation.
        /// This method handles the seasonal elevation changes and converts them to smooth quaternion rotations.
        /// </summary>
        /// <param name="seasonalData">Seasonal data containing orbital angle information</param>
        /// <param name="celestialBody">The celestial body configuration with base elevation and orbital parameters</param>
        /// <param name="azimuth">Current azimuth angle in degrees (0-360, where 0=North, 90=East, 180=South, 270=West)</param>
        /// <returns>Pure quaternion rotation representing the celestial body's position</returns>
        private Quaternion CreateOrbitalPathSpherical(SeasonalData seasonalData, CelestialBody celestialBody, float azimuth)
        {
            // Get the effective orbital angle based on current season
            float effectiveOrbitalAngle = seasonalData.GetEffectiveOrbitalAngle(celestialBody);
            float individualBaseElevation = celestialBody.baseElevation;

            // Calculate elevation variation using sinusoidal function for smooth seasonal changes
            // Phase shift by -90° so that azimuth 0° (dawn) corresponds to minimum elevation change
            float phaseShiftedAzimuth = azimuth - 90f;
            float phaseShiftedRad = phaseShiftedAzimuth * Mathf.Deg2Rad;
            float elevationChange = Mathf.Sin(phaseShiftedRad) * effectiveOrbitalAngle;
            float elevation = individualBaseElevation - elevationChange;

            // Convert to pure quaternion rotation using spherical coordinates
            return CreatePureQuaternionRotation(azimuth, elevation);
        }

        /// <summary>
        /// Create a pure quaternion rotation from azimuth and elevation angles using spherical coordinate conversion.
        /// This method eliminates gimbal lock by converting to a direction vector and using LookRotation.
        /// </summary>
        /// <param name="azimuth">Horizontal angle in degrees (0-360, where 0=North, 90=East, 180=South, 270=West)</param>
        /// <param name="elevation">Vertical angle in degrees (0=horizon, 90=zenith, -90=nadir)</param>
        /// <returns>Pure quaternion rotation that can smoothly handle any angle combination</returns>
        private Quaternion CreatePureQuaternionRotation(float azimuth, float elevation)
        {
            // Convert to direction vector using spherical coordinates
            // This approach eliminates gimbal lock by calculating the target direction directly
            float azimuthRad = azimuth * Mathf.Deg2Rad;
            float elevationRad = (90f - elevation) * Mathf.Deg2Rad; // Convert to mathematical elevation (0=up, 90=horizon)
            
            // Calculate 3D direction vector from spherical coordinates
            Vector3 direction = new Vector3(
                Mathf.Sin(elevationRad) * Mathf.Sin(azimuthRad),  // X component (East-West)
                Mathf.Cos(elevationRad),                          // Y component (Up-Down)
                Mathf.Sin(elevationRad) * Mathf.Cos(azimuthRad)   // Z component (North-South)
            );
            
            // Create quaternion that looks in the calculated direction
            // This single operation replaces multiple rotation combinations and eliminates gimbal lock
            return Quaternion.LookRotation(direction, Vector3.up);
        }

        #endregion

        #region Azimuth Calculation

        /// <summary>
        /// Calculate the azimuth (horizontal position) of a celestial body based on time progression.
        /// Handles both consistent daily cycles for suns and orbital drift for moons.
        /// </summary>
        /// <param name="celestialBody">Celestial body configuration containing speed and phase settings</param>
        /// <param name="celestialTime">Current time within the day (0-1 cycle)</param>
        /// <param name="isMoon">True for moons (adds orbital drift), false for suns (consistent daily pattern)</param>
        /// <returns>Azimuth angle in degrees (0-360)</returns>
        private float CalculateAzimuth(CelestialBody celestialBody, float celestialTime, bool isMoon)
        {
            // Get the effective rotation speed (may be synced with day length)
            float effectiveSpeed = GetEffectiveYAxisSpeed(celestialBody);
    
            // Direct mapping: celestialTime 0.0-1.0 maps to azimuth 0°-360°
            // This creates the basic daily cycle: dawn=0°, noon=180°, dusk=360°/0°
            float baseAzimuth = effectiveSpeed * celestialTime * 360f;
    
            // Add phase offset for fine-tuning celestial body timing
            float phaseOffset = celestialBody.phaseOffset;
            
            // Calculate orbital drift for moons (creates multi-day cycles)
            float orbitalDrift = 0f;
            if (isMoon && celestialBody.orbitalPeriod > 0)
            {
                orbitalDrift = CalculateOrbitalDrift(celestialBody);
            }
    
            // Combine all azimuth components and normalize to 0-360° range
            float finalAzimuth = baseAzimuth + phaseOffset + orbitalDrift;
            return Mathf.Repeat(finalAzimuth, 360f);
        }

        /// <summary>
        /// Get the effective Y-axis rotation speed, potentially synchronized with the world's day length.
        /// This allows celestial bodies to maintain consistent timing regardless of day duration changes.
        /// </summary>
        /// <param name="celestialBody">Celestial body configuration with speed override settings</param>
        /// <returns>Effective rotation speed multiplier</returns>
        private float GetEffectiveYAxisSpeed(CelestialBody celestialBody)
        {
            // Check if speed should be synchronized with day length
            if (celestialBody.yAxisOverrideSpeed && timeManager?.worldTimeData != null)
            {
                // Sync with day length: 1 full rotation per day regardless of day duration
                float dayLengthInSeconds = timeManager.worldTimeData.dayLengthInSeconds;
                if (dayLengthInSeconds > 0)
                {
                    if (enableDebugLogging && Time.frameCount % 600 == 0) // Log every 10 seconds
                    {
                        Debug.Log($"[CelestialCalculator] Y-axis speed synced with day length: 1 rotation per {dayLengthInSeconds}s");
                    }
                    return 1f; // 1 rotation per day regardless of day length
                }
            }
            
            // Use the configured speed from celestial body settings
            return celestialBody.yAxisSpeed;
        }

        /// <summary>
        /// Calculate orbital drift for moons, creating realistic multi-day lunar cycles.
        /// This makes moons rise and set at different times each day, simulating real orbital mechanics.
        /// </summary>
        /// <param name="celestialBody">Moon configuration with orbital period settings</param>
        /// <returns>Orbital drift angle in degrees (0-360)</returns>
        private float CalculateOrbitalDrift(CelestialBody celestialBody)
        {
            if (timeManager == null) return 0f;
            
            // Get current day and fractional time within the day
            int currentDay = timeManager.CurrentDay;
            float timeWithinDay = timeManager.CelestialTime % 1f; // Get fractional part for smooth drift
            
            // Calculate total elapsed time including fractional days for smooth movement
            float totalElapsedDays = currentDay + timeWithinDay;
            
            // Calculate orbital progress (0-1) based on total elapsed time and orbital period
            // This creates cycles that span multiple days (e.g., 29.5 days for realistic lunar cycles)
            float orbitalProgress = (totalElapsedDays % celestialBody.orbitalPeriod) / celestialBody.orbitalPeriod;
            
            // Convert orbital progress to degrees (0-360)
            // This drift accumulates over days, making the moon rise ~50 minutes later each day
            return orbitalProgress * 360f;
        }

        #endregion
    }
}