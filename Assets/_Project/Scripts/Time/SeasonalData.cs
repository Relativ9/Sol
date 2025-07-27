using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Defines the rotation behavior modes for celestial bodies
    /// </summary>
    public enum CelestialRotationMode
    {
        /// <summary>
        /// Continuous rotation in one direction at constant speed
        /// Suitable for standard day/night cycles and orbital motion
        /// </summary>
        Continuous = 0,
        
        /// <summary>
        /// Oscillating rotation between min and max ranges
        /// Suitable for seasonal elevation changes and pendulum-like motion
        /// </summary>
        Oscillate = 1
    }

    /// <summary>
    /// Complete seasonal data containing all celestial body settings and weather configuration for a specific season
    /// CelestialRotator and WeatherManager access these properties directly via TimeManager
    /// Updated to include weather system configuration and Primary Star sync support
    /// </summary>
    [CreateAssetMenu(fileName = "SeasonalData", menuName = "Sol/Seasonal Data")]
    public class SeasonalData : ScriptableObject
    {
        [Header("Season Information")]
        [SerializeField] private Season season;
        [SerializeField] private string seasonDescription = "";
        
        [Header("Primary Star Configuration")]
        [SerializeField] private bool primaryStarActive = true;
        
        [Header("Primary Star - X Axis (Elevation)")]
        [SerializeField] private bool primaryXAxisEnabled = true;
        [SerializeField] private CelestialRotationMode primaryXAxisMode = CelestialRotationMode.Oscillate;
        [SerializeField] private float primaryXAxisSpeed = 0.5f;
        [SerializeField] private float primaryXAxisMinRange = 0f;
        [SerializeField] private float primaryXAxisMaxRange = 90f;
        
        [Header("Primary Star - Y Axis (Azimuth)")]
        [SerializeField] private bool primaryYAxisEnabled = true;
        [SerializeField] private CelestialRotationMode primaryYAxisMode = CelestialRotationMode.Continuous;
        [SerializeField] private float primaryYAxisSpeed = 15f;
        [SerializeField] private float primaryYAxisMinRange = 0f;
        [SerializeField] private float primaryYAxisMaxRange = 360f;
        
        [Header("Primary Star - Axis Synchronization")]
        [SerializeField] private bool primarySyncXWithY = false;
        
        [Header("Red Dwarf Configuration")]
        [SerializeField] private bool redDwarfActive = true;
        
        [Header("Red Dwarf - X Axis (Elevation)")]
        [SerializeField] private bool redDwarfXAxisEnabled = true;
        [SerializeField] private CelestialRotationMode redDwarfXAxisMode = CelestialRotationMode.Oscillate;
        [SerializeField] private float redDwarfXAxisSpeed = 0.4f;
        [SerializeField] private float redDwarfXAxisMinRange = 0f;
        [SerializeField] private float redDwarfXAxisMaxRange = 70f;
        
        [Header("Red Dwarf - Y Axis (Azimuth)")]
        [SerializeField] private bool redDwarfYAxisEnabled = true;
        [SerializeField] private CelestialRotationMode redDwarfYAxisMode = CelestialRotationMode.Continuous;
        [SerializeField] private float redDwarfYAxisSpeed = 15f;
        [SerializeField] private float redDwarfYAxisMinRange = 0f;
        [SerializeField] private float redDwarfYAxisMaxRange = 360f;
        
        [Header("Red Dwarf - Axis Synchronization")]
        [SerializeField] private bool redDwarfSyncXWithY = false;

        [Header("Weather Configuration")]
        [SerializeField] private bool weatherEnabled = true;
        [SerializeField] private float snowChancePerDay = 0.5f; // 50% chance per day for LongNight
        [SerializeField] private float minSnowDurationHours = 2f; // 2 hours of game time
        [SerializeField] private float maxSnowDurationHours = 8f; // 8 hours of game time
        [SerializeField] private float minClearDurationHours = 4f; // 4 hours of game time
        [SerializeField] private float maxClearDurationHours = 16f; // 16 hours of game time
        [SerializeField] private float weatherCheckIntervalHours = 1f; // Check for weather changes every game hour

        // Public properties for season identification
        public Season Season => season;
        public string SeasonDescription => seasonDescription;

        // Primary Star Properties
        public bool PrimaryStarActive => primaryStarActive;
        public bool PrimaryXAxisEnabled => primaryXAxisEnabled;
        public CelestialRotationMode PrimaryXAxisMode => primaryXAxisMode;
        public float PrimaryXAxisSpeed => primaryXAxisSpeed;
        public float PrimaryXAxisMinRange => primaryXAxisMinRange;
        public float PrimaryXAxisMaxRange => primaryXAxisMaxRange;
        public bool PrimaryYAxisEnabled => primaryYAxisEnabled;
        public CelestialRotationMode PrimaryYAxisMode => primaryYAxisMode;
        public float PrimaryYAxisSpeed => primaryYAxisSpeed;
        public float PrimaryYAxisMinRange => primaryYAxisMinRange;
        public float PrimaryYAxisMaxRange => primaryYAxisMaxRange;
        public bool PrimarySyncXWithY => primarySyncXWithY;

        // Red Dwarf Properties
        public bool RedDwarfActive => redDwarfActive;
        public bool RedDwarfXAxisEnabled => redDwarfXAxisEnabled;
        public CelestialRotationMode RedDwarfXAxisMode => redDwarfXAxisMode;
        public float RedDwarfXAxisSpeed => redDwarfXAxisSpeed;
        public float RedDwarfXAxisMinRange => redDwarfXAxisMinRange;
        public float RedDwarfXAxisMaxRange => redDwarfXAxisMaxRange;
        public bool RedDwarfYAxisEnabled => redDwarfYAxisEnabled;
        public CelestialRotationMode RedDwarfYAxisMode => redDwarfYAxisMode;
        public float RedDwarfYAxisSpeed => redDwarfYAxisSpeed;
        public float RedDwarfYAxisMinRange => redDwarfYAxisMinRange;
        public float RedDwarfYAxisMaxRange => redDwarfYAxisMaxRange;
        public bool RedDwarfSyncXWithY => redDwarfSyncXWithY;

        // Weather Properties
        public bool WeatherEnabled => weatherEnabled;
        public float SnowChancePerDay => snowChancePerDay;
        public float MinSnowDurationHours => minSnowDurationHours;
        public float MaxSnowDurationHours => maxSnowDurationHours;
        public float MinClearDurationHours => minClearDurationHours;
        public float MaxClearDurationHours => maxClearDurationHours;
        public float WeatherCheckIntervalHours => weatherCheckIntervalHours;

        // Weather duration conversion methods (convert hours to seconds based on day length)
        public float GetMinSnowDurationSeconds(float dayLengthInSeconds)
        {
            return (minSnowDurationHours / 24f) * dayLengthInSeconds;
        }

        public float GetMaxSnowDurationSeconds(float dayLengthInSeconds)
        {
            return (maxSnowDurationHours / 24f) * dayLengthInSeconds;
        }

        public float GetMinClearDurationSeconds(float dayLengthInSeconds)
        {
            return (minClearDurationHours / 24f) * dayLengthInSeconds;
        }

        public float GetMaxClearDurationSeconds(float dayLengthInSeconds)
        {
            return (maxClearDurationHours / 24f) * dayLengthInSeconds;
        }

        public float GetWeatherCheckIntervalSeconds(float dayLengthInSeconds)
        {
            return (weatherCheckIntervalHours / 24f) * dayLengthInSeconds;
        }

        /// <summary>
        /// Sets Y-axis speed for all active celestial bodies (used by TimeManager for day sync enforcement)
        /// </summary>
        /// <param name="speed">Speed in degrees per second</param>
        public void SetAllCelestialYAxisSpeeds(float speed)
        {
            float clampedSpeed = Mathf.Max(0f, speed);
            
            // Set Primary Star Y-axis speed if applicable
            if (primaryStarActive && primaryYAxisEnabled && primaryYAxisMode == CelestialRotationMode.Continuous)
            {
                primaryYAxisSpeed = clampedSpeed;
            }
            
            // Set Red Dwarf Y-axis speed if applicable
            if (redDwarfActive && redDwarfYAxisEnabled && redDwarfYAxisMode == CelestialRotationMode.Continuous)
            {
                redDwarfYAxisSpeed = clampedSpeed;
            }
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }

        /// <summary>
        /// Gets the expected Y-axis speed for all celestial bodies based on day length
        /// </summary>
        /// <param name="dayLengthInSeconds">Length of one day in seconds</param>
        /// <returns>Required speed in degrees per second for full rotation per day</returns>
        public float GetRequiredCelestialYAxisSpeed(float dayLengthInSeconds)
        {
            return 360f / dayLengthInSeconds;
        }

        /// <summary>
        /// Checks if all celestial Y-axis speeds match the expected day length
        /// </summary>
        /// <param name="dayLengthInSeconds">Expected day length in seconds</param>
        /// <param name="tolerance">Tolerance for speed comparison</param>
        /// <returns>True if all applicable celestial bodies match within tolerance</returns>
        public bool AreAllCelestialYAxisSyncedWithDay(float dayLengthInSeconds, float tolerance = 0.01f)
        {
            float requiredSpeed = GetRequiredCelestialYAxisSpeed(dayLengthInSeconds);
            
            // Check Primary Star if applicable
            if (primaryStarActive && primaryYAxisEnabled && primaryYAxisMode == CelestialRotationMode.Continuous)
            {
                if (Mathf.Abs(primaryYAxisSpeed - requiredSpeed) > tolerance)
                    return false;
            }
            
            // Check Red Dwarf if applicable
            if (redDwarfActive && redDwarfYAxisEnabled && redDwarfYAxisMode == CelestialRotationMode.Continuous)
            {
                if (Mathf.Abs(redDwarfYAxisSpeed - requiredSpeed) > tolerance)
                    return false;
            }
            
            return true; // All applicable celestial bodies are synced
        }

        /// <summary>
        /// Gets a list of celestial bodies that are not synced with day length
        /// </summary>
        /// <param name="dayLengthInSeconds">Expected day length in seconds</param>
        /// <param name="tolerance">Tolerance for speed comparison</param>
        /// <returns>List of celestial body names that are out of sync</returns>
        public string[] GetUnsyncedCelestialBodies(float dayLengthInSeconds, float tolerance = 0.01f)
        {
            var unsyncedBodies = new System.Collections.Generic.List<string>();
            float requiredSpeed = GetRequiredCelestialYAxisSpeed(dayLengthInSeconds);
            
            // Check Primary Star
            if (primaryStarActive && primaryYAxisEnabled && primaryYAxisMode == CelestialRotationMode.Continuous)
            {
                if (Mathf.Abs(primaryYAxisSpeed - requiredSpeed) > tolerance)
                    unsyncedBodies.Add($"Primary Star (current: {primaryYAxisSpeed:F3}, expected: {requiredSpeed:F3})");
            }
            
            // Check Red Dwarf
            if (redDwarfActive && redDwarfYAxisEnabled && redDwarfYAxisMode == CelestialRotationMode.Continuous)
            {
                if (Mathf.Abs(redDwarfYAxisSpeed - requiredSpeed) > tolerance)
                    unsyncedBodies.Add($"Red Dwarf (current: {redDwarfYAxisSpeed:F3}, expected: {requiredSpeed:F3})");
            }
            
            return unsyncedBodies.ToArray();
        }
        /// <summary>
        /// Validates that all celestial body and weather settings are properly configured
        /// </summary>
        public bool IsValid()
        {
            // Validate Primary Star
            if (primaryStarActive)
            {
                if (primaryXAxisEnabled && primaryXAxisMinRange >= primaryXAxisMaxRange)
                    return false;
                if (primaryYAxisEnabled && primaryYAxisSpeed <= 0f)
                    return false;
                if (primaryYAxisMode == CelestialRotationMode.Oscillate && primaryYAxisMinRange >= primaryYAxisMaxRange)
                    return false;
            }

            // Validate Red Dwarf
            if (redDwarfActive)
            {
                if (redDwarfXAxisEnabled && redDwarfXAxisMinRange >= redDwarfXAxisMaxRange)
                    return false;
                if (redDwarfYAxisEnabled && redDwarfYAxisSpeed <= 0f)
                    return false;
                if (redDwarfYAxisMode == CelestialRotationMode.Oscillate && redDwarfYAxisMinRange >= redDwarfYAxisMaxRange)
                    return false;
            }

            // Validate Weather Settings
            if (weatherEnabled)
            {
                if (snowChancePerDay < 0f || snowChancePerDay > 1f)
                    return false;
                if (minSnowDurationHours <= 0f || maxSnowDurationHours <= 0f)
                    return false;
                if (minSnowDurationHours >= maxSnowDurationHours)
                    return false;
                if (minClearDurationHours <= 0f || maxClearDurationHours <= 0f)
                    return false;
                if (minClearDurationHours >= maxClearDurationHours)
                    return false;
                if (weatherCheckIntervalHours <= 0f)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Editor validation
        /// </summary>
        private void OnValidate()
        {
            // Only clamp speeds to positive values - don't restrict ranges
            primaryXAxisSpeed = Mathf.Max(0f, primaryXAxisSpeed);
            primaryYAxisSpeed = Mathf.Max(0f, primaryYAxisSpeed);
            
            redDwarfXAxisSpeed = Mathf.Max(0f, redDwarfXAxisSpeed);
            redDwarfYAxisSpeed = Mathf.Max(0f, redDwarfYAxisSpeed);

            // Ensure min < max for celestial ranges, but don't clamp the actual values
            if (primaryXAxisMinRange >= primaryXAxisMaxRange)
                primaryXAxisMaxRange = primaryXAxisMinRange + 1f;
            if (primaryYAxisMinRange >= primaryYAxisMaxRange)
                primaryYAxisMaxRange = primaryYAxisMinRange + 1f;

            if (redDwarfXAxisMinRange >= redDwarfXAxisMaxRange)
                redDwarfXAxisMaxRange = redDwarfXAxisMinRange + 1f;
            if (redDwarfYAxisMinRange >= redDwarfYAxisMaxRange)
                redDwarfYAxisMaxRange = redDwarfYAxisMinRange + 1f;

            // Validate weather settings
            snowChancePerDay = Mathf.Clamp01(snowChancePerDay);
            minSnowDurationHours = Mathf.Max(0.1f, minSnowDurationHours);
            maxSnowDurationHours = Mathf.Max(0.1f, maxSnowDurationHours);
            minClearDurationHours = Mathf.Max(0.1f, minClearDurationHours);
            maxClearDurationHours = Mathf.Max(0.1f, maxClearDurationHours);
            weatherCheckIntervalHours = Mathf.Max(0.1f, weatherCheckIntervalHours);

            // Ensure min < max for weather durations
            if (minSnowDurationHours >= maxSnowDurationHours)
                maxSnowDurationHours = minSnowDurationHours + 1f;
            if (minClearDurationHours >= maxClearDurationHours)
                maxClearDurationHours = minClearDurationHours + 1f;
        }
    }
}