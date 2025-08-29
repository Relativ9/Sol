// using UnityEngine;
//
// namespace Sol
// {
//     /// <summary>
//     /// Interface for celestial calculation systems
//     /// Handles celestial body rotations, axis synchronization, and day-length synchronization
//     /// </summary>
//     public interface ICelestialCalculator
//     {
//         /// <summary>
//         /// Initialize the calculator with time manager reference for day synchronization
//         /// </summary>
//         /// <param name="timeManager">Time manager instance for accessing day length</param>
//         void Initialize(ITimeManager timeManager);
//
//         /// <summary>
//         /// Calculates celestial rotation for a specific celestial body using seasonal data
//         /// Automatically applies day synchronization if enabled
//         /// </summary>
//         /// <param name="seasonalData">Current seasonal data containing celestial settings</param>
//         /// <param name="celestialBodyName">Name of celestial body (PrimaryStar, RedDwarf)</param>
//         /// <param name="baseRotation">Starting rotation values</param>
//         /// <param name="celestialTime">Current celestial time for calculations</param>
//         /// <returns>Calculated rotation vector</returns>
//         Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime);
//
//         /// <summary>
//         /// Calculates celestial rotation with explicit day length for synchronization
//         /// Useful for testing or when time manager is not available
//         /// </summary>
//         /// <param name="seasonalData">Current seasonal data containing celestial settings</param>
//         /// <param name="celestialBodyName">Name of celestial body</param>
//         /// <param name="baseRotation">Starting rotation values</param>
//         /// <param name="celestialTime">Current celestial time</param>
//         /// <param name="dayLengthInSeconds">Day length for synchronization calculations</param>
//         /// <returns>Calculated rotation vector with day synchronization applied</returns>
//         Vector3 CalculateCelestialRotationWithDaySync(SeasonalData seasonalData, string celestialBodyName, 
//             Vector3 baseRotation, float celestialTime, float dayLengthInSeconds);
//
//         /// <summary>
//         /// Interpolates between two seasonal calculations for smooth seasonal transitions
//         /// </summary>
//         /// <param name="fromSeasonalData">Source seasonal data</param>
//         /// <param name="toSeasonalData">Target seasonal data</param>
//         /// <param name="celestialBodyName">Name of celestial body</param>
//         /// <param name="baseRotation">Starting rotation values</param>
//         /// <param name="celestialTime">Current celestial time</param>
//         /// <param name="interpolationFactor">Blend factor between seasons (0-1)</param>
//         /// <returns>Interpolated rotation vector</returns>
//         Vector3 InterpolateCelestialRotation(SeasonalData fromSeasonalData, SeasonalData toSeasonalData, 
//             string celestialBodyName, Vector3 baseRotation, float celestialTime, float interpolationFactor);
//
//         /// <summary>
//         /// Validates seasonal data for common configuration errors
//         /// </summary>
//         /// <param name="seasonalData">Seasonal data to validate</param>
//         /// <returns>True if settings are valid</returns>
//         bool ValidateSeasonalData(SeasonalData seasonalData);
//
//         /// <summary>
//         /// Sets whether day synchronization is enabled
//         /// When enabled, Y-axis speeds are automatically adjusted to complete 360° per day
//         /// </summary>
//         /// <param name="enabled">True to enable day synchronization</param>
//         void SetDaySynchronizationEnabled(bool enabled);
//
//         /// <summary>
//         /// Gets whether day synchronization is currently enabled
//         /// </summary>
//         /// <returns>True if day synchronization is enabled</returns>
//         bool IsDaySynchronizationEnabled();
//
//         /// <summary>
//         /// Validates if a celestial body's Y-axis is synchronized with the given day length
//         /// </summary>
//         /// <param name="yAxisSpeed">Current Y-axis speed in degrees per second</param>
//         /// <param name="dayLengthInSeconds">Day length to check synchronization against</param>
//         /// <returns>True if the Y-axis speed matches the required speed for day synchronization</returns>
//         bool IsYAxisSynchronizedWithDay(float yAxisSpeed, float dayLengthInSeconds);
//
//         /// <summary>
//         /// Gets the required Y-axis speed for day synchronization
//         /// </summary>
//         /// <param name="dayLengthInSeconds">Length of one day in seconds</param>
//         /// <returns>Required Y-axis speed in degrees per second for 360° rotation per day</returns>
//         float GetRequiredYAxisSpeedForDay(float dayLengthInSeconds);
//
//         /// <summary>
//         /// Cleanup method for releasing resources and unsubscribing from events
//         /// </summary>
//         void Cleanup();
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
        Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime);

        /// <summary>
        /// Calculate celestial rotation with explicit day synchronization
        /// </summary>
        Vector3 CalculateCelestialRotationWithDaySync(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime, float dayLengthInSeconds);

        /// <summary>
        /// Interpolate between two seasonal configurations
        /// </summary>
        Vector3 InterpolateCelestialRotation(SeasonalData fromSeasonalData, SeasonalData toSeasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime, float interpolationFactor);

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

// using UnityEngine;
//
// namespace Sol
// {
//     /// <summary>
//     /// Interface for celestial body rotation calculations and day synchronization.
//     /// Provides abstraction for different calculation strategies while maintaining consistent behavior.
//     /// Follows Interface Segregation Principle by grouping related celestial calculation functionality.
//     /// Supports Dependency Inversion by allowing different calculation implementations.
//     /// </summary>
//     public interface ICelestialCalculator
//     {
//         #region Initialization and Lifecycle
//
//         /// <summary>
//         /// Initializes the calculator with required dependencies.
//         /// Must be called before using any calculation methods.
//         /// </summary>
//         /// <param name="timeManager">Time manager instance for accessing temporal data</param>
//         void Initialize(ITimeManager timeManager);
//
//         /// <summary>
//         /// Cleans up resources and references when the calculator is no longer needed.
//         /// Should be called when destroying or replacing the calculator.
//         /// </summary>
//         void Cleanup();
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
//         Vector3 CalculateCelestialRotation(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime);
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
//         Vector3 CalculateCelestialRotationWithDaySync(SeasonalData seasonalData, string celestialBodyName, Vector3 baseRotation, float celestialTime, float dayLengthInSeconds);
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
//         Vector3 InterpolateCelestialRotation(SeasonalData fromSeason, SeasonalData toSeason, string celestialBodyName, Vector3 baseRotation, float celestialTime, float transitionProgress);
//
//         #endregion
//
//         #region Axis-Specific Calculations
//
//         // /// <summary>
//         // /// Calculates rotation for a specific axis using the given configuration.
//         // /// Handles both oscillating and continuous rotation modes.
//         // /// </summary>
//         // /// <param name="axisConfig">Configuration for the specific axis</param>
//         // /// <param name="celestialTime">Current celestial time (0-1)</param>
//         // /// <param name="baseValue">Base rotation value for this axis</param>
//         // /// <returns>Calculated rotation value in degrees</returns>
//         // float CalculateAxisRotation(AxisConfiguration axisConfig, float celestialTime, float baseValue);
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
//         float CalculateOscillatingRotation(float speed, float min, float max, float celestialTime, float baseValue);
//
//         /// <summary>
//         /// Calculates continuous rotation around a full 360-degree cycle.
//         /// Creates smooth circular movement for celestial bodies.
//         /// </summary>
//         /// <param name="speed">Rotation speed in degrees per celestial time unit</param>
//         /// <param name="celestialTime">Current celestial time (0-1)</param>
//         /// <param name="baseValue">Base rotation value to add to result</param>
//         /// <returns>Calculated continuous rotation value in degrees</returns>
//         float CalculateContinuousRotation(float speed, float celestialTime, float baseValue);
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
//         bool IsDaySynchronizationEnabled();
//
//         /// <summary>
//         /// Calculates the required Y-axis rotation speed to complete one full rotation per day.
//         /// Used for synchronizing celestial bodies with the day/night cycle.
//         /// </summary>
//         /// <param name="dayLengthInSeconds">Length of one day in real-world seconds</param>
//         /// <returns>Required Y-axis speed in degrees per celestial time unit</returns>
//         float GetRequiredYAxisSpeedForDay(float dayLengthInSeconds);
//
//         /// <summary>
//         /// Checks if a given Y-axis speed is synchronized with the specified day length.
//         /// Allows for small tolerance in speed matching.
//         /// </summary>
//         /// <param name="currentSpeed">Current Y-axis rotation speed</param>
//         /// <param name="dayLengthInSeconds">Target day length in seconds</param>
//         /// <param name="tolerance">Acceptable difference in speed (default: 0.1 degrees)</param>
//         /// <returns>True if the speed is synchronized within tolerance</returns>
//         bool IsYAxisSynchronizedWithDay(float currentSpeed, float dayLengthInSeconds, float tolerance = 0.1f);
//
//         /// <summary>
//         /// Applies day synchronization to a Y-axis speed value.
//         /// Modifies the speed to ensure one complete rotation per day.
//         /// </summary>
//         /// <param name="originalSpeed">Original Y-axis speed</param>
//         /// <param name="dayLengthInSeconds">Target day length in seconds</param>
//         /// <returns>Synchronized Y-axis speed</returns>
//         float ApplyDaySynchronization(float originalSpeed, float dayLengthInSeconds);
//
//         #endregion
//
//         #region Celestial Body Detection
//
//         /// <summary>
//         /// Determines if the given celestial body name represents a primary star.
//         /// Uses flexible name matching to support various naming conventions.
//         /// </summary>
//         /// <param name="celestialBodyName">Name to check (case-insensitive)</param>
//         /// <returns>True if the name represents a primary star</returns>
//         bool IsPrimaryStar(string celestialBodyName);
//
//         /// <summary>
//         /// Determines if the given celestial body name represents a red dwarf star.
//         /// Uses flexible name matching to support various naming conventions.
//         /// </summary>
//         /// <param name="celestialBodyName">Name to check (case-insensitive)</param>
//         /// <returns>True if the name represents a red dwarf star</returns>
//         bool IsRedDwarf(string celestialBodyName);
//
//         /// <summary>
//         /// Gets the celestial body type for the given name.
//         /// Provides abstraction over celestial body identification.
//         /// </summary>
//         /// <param name="celestialBodyName">Name to identify</param>
//         /// <returns>Celestial body type or Unknown if not recognized</returns>
//         CelestialBodyType GetCelestialBodyType(string celestialBodyName);
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
//         bool IsInitialized();
//
//         /// <summary>
//         /// Validates seasonal data for celestial calculations.
//         /// Ensures the data contains valid configuration for the specified celestial body.
//         /// </summary>
//         /// <param name="seasonalData">Seasonal data to validate</param>
//         /// <param name="celestialBodyName">Celestial body name to validate for</param>
//         /// <returns>True if seasonal data is valid for calculations</returns>
//         bool ValidateSeasonalData(SeasonalData seasonalData, string celestialBodyName);
//
//         /// <summary>
//         /// Gets diagnostic information about the calculator's current state.
//         /// Useful for debugging and monitoring calculator health.
//         /// </summary>
//         /// <returns>Formatted diagnostic string</returns>
//         string GetDiagnosticInfo();
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
//         ITimeManager GetTimeManager();
//
//         /// <summary>
//         /// Updates the time manager reference used for day synchronization.
//         /// Allows dynamic reconfiguration of the calculator's time source.
//         /// </summary>
//         /// <param name="newTimeManager">New time manager to use</param>
//         void SetTimeManager(ITimeManager newTimeManager);
//
//         #endregion
//     }
//
//     #region Supporting Enums
//
//     /// <summary>
//     /// Represents different types of celestial bodies supported by the calculation system.
//     /// Used for type-safe celestial body identification and configuration.
//     /// </summary>
//     public enum CelestialBodyType
//     {
//         /// <summary>
//         /// Unknown or unrecognized celestial body type
//         /// </summary>
//         Unknown,
//         
//         /// <summary>
//         /// Primary star (main sun/star in the system)
//         /// </summary>
//         PrimaryStar,
//         
//         /// <summary>
//         /// Red dwarf star (secondary star in binary systems)
//         /// </summary>
//         RedDwarf
//     }
//
//     #endregion
// }