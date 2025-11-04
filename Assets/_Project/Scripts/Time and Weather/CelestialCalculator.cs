using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Calculates celestial body rotations using axis-angle quaternion mathematics.
    /// Handles both sun and moon positioning with realistic orbital mechanics, seasonal variations,
    /// and multi-day lunar cycles. Avoids gimbal lock by using proper quaternion composition.
    /// </summary>
    public class CelestialCalculator : ICelestialCalculator
    {
        public bool enableDebugLogging { get; set; } = false;
        
        private ITimeManager _timeManager;

        #region Constructor

        public CelestialCalculator(ITimeManager timeManager)
        {
            _timeManager = timeManager;
        }

        #endregion

        #region Public Interface

        public Quaternion CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, 
            Vector3 baseRotation, float celestialTime, bool isMoon)
        {
            if (seasonalData == null)
            {
                if (enableDebugLogging)
                    Debug.LogWarning($"[CelestialCalculator] No seasonal data provided for {celestialBodyName}");
                return Quaternion.Euler(baseRotation);
            }

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

            // Calculate celestial rotation using axis-angle quaternion composition (avoids gimbal lock)
            Quaternion celestialRotation = CreateOrbitalPathAxisAngle(seasonalData, celestialBody, azimuth);
            
            // Apply base rotation offset for fine-tuning
            Quaternion baseRotationQuat = Quaternion.Euler(baseRotation);
            Quaternion finalRotation = celestialRotation * baseRotationQuat;

            if (enableDebugLogging && Time.frameCount % 300 == 0)
            {
                Vector3 eulerForLogging = finalRotation.eulerAngles;
                Debug.Log($"[CelestialCalculator] {celestialBodyName}: Azimuth={azimuth:F1}°, Elevation calc from angles");
            }

            return finalRotation;
        }

        #endregion

        #region Core Calculation Methods

        /// <summary>
        /// Create the orbital path using axis-angle quaternion composition.
        /// This avoids gimbal lock by applying rotations in the correct order around proper axes.
        /// </summary>
        private Quaternion CreateOrbitalPathAxisAngle(SeasonalData seasonalData, SeasonalData.CelestialBodySeasonalConfig celestialBody, float azimuth)
        {
            // Get the effective orbital angle based on current season
            float effectiveOrbitalAngle = seasonalData.GetEffectiveOrbitalAngle(celestialBody);
            float individualBaseElevation = celestialBody.baseElevation;

            // Calculate elevation variation using sinusoidal function for smooth seasonal changes
            float phaseShiftedAzimuth = azimuth - 90f;
            float phaseShiftedRad = phaseShiftedAzimuth * Mathf.Deg2Rad;
            float elevationChange = Mathf.Sin(phaseShiftedRad) * effectiveOrbitalAngle;
            float elevation = individualBaseElevation - elevationChange;

            // Build rotation using axis-angle composition (no gimbal lock)
            // 1. Start with identity
            Quaternion rotation = Quaternion.identity;
            
            // 2. Apply azimuth rotation around Y-axis (horizontal rotation)
            rotation *= Quaternion.AngleAxis(azimuth, Vector3.up);
            
            // 3. Apply elevation rotation around local X-axis (tilt up/down)
            rotation *= Quaternion.AngleAxis(elevation, Vector3.right);

            return rotation;
        }

        #endregion

        #region Azimuth Calculation

        private float CalculateAzimuth(SeasonalData.CelestialBodySeasonalConfig celestialBody, float celestialTime, bool isMoon)
        {
            float effectiveSpeed = GetEffectiveYAxisSpeed(celestialBody);
            float baseAzimuth = effectiveSpeed * celestialTime * 360f;
            float phaseOffset = celestialBody.phaseOffset;
            
            float orbitalDrift = 0f;
            if (isMoon && celestialBody.orbitalPeriod > 0)
            {
                orbitalDrift = CalculateOrbitalDrift(celestialBody);
            }
    
            float finalAzimuth = baseAzimuth + phaseOffset + orbitalDrift;
            return Mathf.Repeat(finalAzimuth, 360f);
        }

        private float GetEffectiveYAxisSpeed(SeasonalData.CelestialBodySeasonalConfig celestialBody)
        {
            if (celestialBody.yAxisOverrideSpeed && _timeManager?.WorldTimeData != null)
            {
                float dayLengthInSeconds = _timeManager.WorldTimeData.TotalGameSecondsPerDay;
                if (dayLengthInSeconds > 0)
                {
                    if (enableDebugLogging && Time.frameCount % 600 == 0)
                    {
                        Debug.Log($"[CelestialCalculator] Y-axis speed synced with day length: 1 rotation per {dayLengthInSeconds}s");
                    }
                    return 1f;
                }
            }
            
            return celestialBody.yAxisSpeed;
        }

        private float CalculateOrbitalDrift(SeasonalData.CelestialBodySeasonalConfig celestialBody)
        {
            if (_timeManager == null) return 0f;
            
            int currentDay = _timeManager.CurrentDay;
            float timeWithinDay = _timeManager.CelestialTime % 1f;
            float totalElapsedDays = currentDay + timeWithinDay;
            float orbitalProgress = (totalElapsedDays % celestialBody.orbitalPeriod) / celestialBody.orbitalPeriod;
            
            return orbitalProgress * 360f;
        }

        #endregion
    }
}
