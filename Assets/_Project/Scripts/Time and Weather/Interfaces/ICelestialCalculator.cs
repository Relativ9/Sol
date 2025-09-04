using UnityEngine;

namespace Sol
{
    public interface ICelestialCalculator
    {
        /// <summary>
        /// Enable or disable debug logging for this calculator
        /// </summary>
        bool enableDebugLogging { get; set; }

        /// <summary>
        /// Calculate the rotation for a celestial body as a pure quaternion (no gimbal lock)
        /// </summary>
        /// <param name="seasonalData">The seasonal data containing celestial body configurations</param>
        /// <param name="celestialBodyName">Name of the celestial body to calculate rotation for</param>
        /// <param name="baseRotation">Base rotation offset to apply</param>
        /// <param name="celestialTime">Current celestial time (0-1 represents one full day)</param>
        /// <param name="isMoon">Whether this celestial body is a moon (affects orbital calculations)</param>
        /// <returns>The calculated rotation as a pure quaternion</returns>
        Quaternion CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, 
            Vector3 baseRotation, float celestialTime, bool isMoon);
    }
}