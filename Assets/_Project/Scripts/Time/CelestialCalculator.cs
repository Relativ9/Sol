using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Celestial calculator implementation for complex celestial body rotations
    /// Works directly with SeasonalData properties for seasonal transitions
    /// Handles axis synchronization where X-axis completes one cycle per Y-axis rotation
    /// </summary>
    public class CelestialCalculator : ICelestialCalculator
    {
                /// <summary>
        /// Calculates celestial rotation for a specific celestial body using seasonal data
        /// </summary>
        /// <param name="seasonalData">Current seasonal data containing celestial settings</param>
        /// <param name="celestialBodyName">Name of celestial body (PrimaryStar, RedDwarf)</param>
        /// <param name="baseRotation">Starting rotation values</param>
        /// <param name="celestialTime">Current celestial time for calculations</param>
        /// <returns>Calculated rotation vector</returns>
        public Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime)
        {
            if (seasonalData == null) return baseRotation;

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

        /// <summary>
        /// Interpolates between two seasonal calculations for smooth seasonal transitions
        /// </summary>
        /// <param name="fromSeasonalData">Source seasonal data</param>
        /// <param name="toSeasonalData">Target seasonal data</param>
        /// <param name="celestialBodyName">Name of celestial body</param>
        /// <param name="baseRotation">Starting rotation values</param>
        /// <param name="celestialTime">Current celestial time</param>
        /// <param name="interpolationFactor">Blend factor between seasons (0-1)</param>
        /// <returns>Interpolated rotation vector</returns>
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
        /// <param name="seasonalData">Seasonal data to validate</param>
        /// <returns>True if settings are valid</returns>
        public bool ValidateSeasonalData(SeasonalData seasonalData)
        {
            if (seasonalData == null) return false;
            return seasonalData.IsValid();
        }

        private Vector3 CalculatePrimaryStarRotation(SeasonalData seasonalData, Vector3 baseRotation, float celestialTime)
        {
            if (!seasonalData.PrimaryStarActive) return baseRotation;

            Vector3 rotation = baseRotation;

            // Calculate X-axis (elevation) rotation
            if (seasonalData.PrimaryXAxisEnabled)
            {
                float xAxisSpeed = seasonalData.PrimaryXAxisSpeed;
                
                // Handle axis synchronization
                if (seasonalData.PrimarySyncXWithY && seasonalData.PrimaryYAxisEnabled)
                {
                    xAxisSpeed = CalculateXAxisSyncSpeed(seasonalData.PrimaryYAxisSpeed);
                }

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
                rotation.y = CalculateAxisRotation(
                    seasonalData.PrimaryYAxisMode,
                    seasonalData.PrimaryYAxisSpeed,
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
                float xAxisSpeed = seasonalData.RedDwarfXAxisSpeed;
                
                // Handle axis synchronization
                if (seasonalData.RedDwarfSyncXWithY && seasonalData.RedDwarfYAxisEnabled)
                {
                    xAxisSpeed = CalculateXAxisSyncSpeed(seasonalData.RedDwarfYAxisSpeed);
                }

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
                rotation.y = CalculateAxisRotation(
                    seasonalData.RedDwarfYAxisMode,
                    seasonalData.RedDwarfYAxisSpeed,
                    0f,
                    360f,
                    celestialTime,
                    baseRotation.y
                );
            }

            return rotation;
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
                    // FIXED: Oscillating rotation between min and max ranges
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
        /// <param name="yAxisRotationSpeed">Y-axis rotation speed in degrees per second</param>
        /// <returns>Calculated X-axis oscillation speed</returns>
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
    }
}
