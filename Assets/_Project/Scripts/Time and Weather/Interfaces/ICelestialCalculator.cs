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
        /// Calculate the rotation for a celestial body based on seasonal data and current time
        /// </summary>
        /// <param name="seasonalData">The seasonal data containing celestial body configurations</param>
        /// <param name="celestialBodyName">Name of the celestial body to calculate rotation for</param>
        /// <param name="baseRotation">Base rotation offset to apply</param>
        /// <param name="celestialTime">Current celestial time (0-1 represents one full day)</param>
        /// <param name="isMoon">Whether this celestial body is a moon (affects orbital calculations)</param>
        /// <returns>The calculated rotation as Euler angles</returns>
        Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, 
            Vector3 baseRotation, float celestialTime, bool isMoon);
    }
}