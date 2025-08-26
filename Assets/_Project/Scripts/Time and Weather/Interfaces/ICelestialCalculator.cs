using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Interface for celestial calculation systems
    /// Handles celestial body rotations, axis synchronization, and day-length synchronization
    /// </summary>
    public interface ICelestialCalculator
    {
        /// <summary>
        /// Initialize the calculator with time manager reference for day synchronization
        /// </summary>
        /// <param name="timeManager">Time manager instance for accessing day length</param>
        void Initialize(ITimeManager timeManager);

        /// <summary>
        /// Calculates celestial rotation for a specific celestial body using seasonal data
        /// Automatically applies day synchronization if enabled
        /// </summary>
        /// <param name="seasonalData">Current seasonal data containing celestial settings</param>
        /// <param name="celestialBodyName">Name of celestial body (PrimaryStar, RedDwarf)</param>
        /// <param name="baseRotation">Starting rotation values</param>
        /// <param name="celestialTime">Current celestial time for calculations</param>
        /// <returns>Calculated rotation vector</returns>
        Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime);

        /// <summary>
        /// Calculates celestial rotation with explicit day length for synchronization
        /// Useful for testing or when time manager is not available
        /// </summary>
        /// <param name="seasonalData">Current seasonal data containing celestial settings</param>
        /// <param name="celestialBodyName">Name of celestial body</param>
        /// <param name="baseRotation">Starting rotation values</param>
        /// <param name="celestialTime">Current celestial time</param>
        /// <param name="dayLengthInSeconds">Day length for synchronization calculations</param>
        /// <returns>Calculated rotation vector with day synchronization applied</returns>
        Vector3 CalculateCelestialRotationWithDaySync(SeasonalData seasonalData, string celestialBodyName, 
            Vector3 baseRotation, float celestialTime, float dayLengthInSeconds);

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
        Vector3 InterpolateCelestialRotation(SeasonalData fromSeasonalData, SeasonalData toSeasonalData, 
            string celestialBodyName, Vector3 baseRotation, float celestialTime, float interpolationFactor);

        /// <summary>
        /// Validates seasonal data for common configuration errors
        /// </summary>
        /// <param name="seasonalData">Seasonal data to validate</param>
        /// <returns>True if settings are valid</returns>
        bool ValidateSeasonalData(SeasonalData seasonalData);

        /// <summary>
        /// Sets whether day synchronization is enabled
        /// When enabled, Y-axis speeds are automatically adjusted to complete 360° per day
        /// </summary>
        /// <param name="enabled">True to enable day synchronization</param>
        void SetDaySynchronizationEnabled(bool enabled);

        /// <summary>
        /// Gets whether day synchronization is currently enabled
        /// </summary>
        /// <returns>True if day synchronization is enabled</returns>
        bool IsDaySynchronizationEnabled();

        /// <summary>
        /// Validates if a celestial body's Y-axis is synchronized with the given day length
        /// </summary>
        /// <param name="yAxisSpeed">Current Y-axis speed in degrees per second</param>
        /// <param name="dayLengthInSeconds">Day length to check synchronization against</param>
        /// <returns>True if the Y-axis speed matches the required speed for day synchronization</returns>
        bool IsYAxisSynchronizedWithDay(float yAxisSpeed, float dayLengthInSeconds);

        /// <summary>
        /// Gets the required Y-axis speed for day synchronization
        /// </summary>
        /// <param name="dayLengthInSeconds">Length of one day in seconds</param>
        /// <returns>Required Y-axis speed in degrees per second for 360° rotation per day</returns>
        float GetRequiredYAxisSpeedForDay(float dayLengthInSeconds);

        /// <summary>
        /// Cleanup method for releasing resources and unsubscribing from events
        /// </summary>
        void Cleanup();
    }
}