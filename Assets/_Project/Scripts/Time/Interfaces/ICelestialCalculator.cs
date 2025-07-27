using UnityEngine;

namespace Sol
{
        /// <summary>
    /// Interface for celestial rotation calculations
    /// Updated to work directly with SeasonalData and celestial body names
    /// Allows different calculation strategies while maintaining dependency inversion
    /// </summary>
    public interface ICelestialCalculator
    {
        /// <summary>
        /// Calculates celestial rotation for a specific celestial body using seasonal data
        /// </summary>
        /// <param name="seasonalData">Current seasonal data containing all celestial settings</param>
        /// <param name="celestialBodyName">Name of celestial body (PrimaryStar, RedDwarf, etc.)</param>
        /// <param name="baseRotation">Starting rotation values</param>
        /// <param name="celestialTime">Current celestial time for calculations</param>
        /// <returns>Calculated rotation vector</returns>
        Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime);

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
        Vector3 InterpolateCelestialRotation(
            SeasonalData fromSeasonalData, 
            SeasonalData toSeasonalData, 
            string celestialBodyName,
            Vector3 baseRotation, 
            float celestialTime, 
            float interpolationFactor);

        /// <summary>
        /// Validates seasonal data for common configuration errors
        /// </summary>
        /// <param name="seasonalData">Seasonal data to validate</param>
        /// <returns>True if settings are valid</returns>
        bool ValidateSeasonalData(SeasonalData seasonalData);
    }
}
