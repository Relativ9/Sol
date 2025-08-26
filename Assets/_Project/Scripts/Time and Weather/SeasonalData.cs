using UnityEngine;

namespace Sol
{

    /// <summary>
    /// Oscillate makes the body moves back and forth between the two rotational values
    /// Continous rotates 360 degrees on the axis at a set speed.
    /// </summary>
    public enum CelestialRotationMode
    {
        Oscillate,
        Continuous,
    }
    
    [CreateAssetMenu(fileName = "SeasonalData", menuName = "Sol/Seasonal Data")]
    public class SeasonalData : ScriptableObject
    {
        [Header("Season Configuration")]
        [SerializeField] private Season season = Season.LongNight;
        
        [Header("Primary Star Configuration")]
        [SerializeField] private bool primaryStarActive = true;
        
        [Header("Primary Star X-Axis (Elevation)")]
        [SerializeField] private bool primaryXAxisEnabled = false;
        [SerializeField] private CelestialRotationMode primaryXAxisMode = CelestialRotationMode.Oscillate;
        [SerializeField] private float primaryXAxisSpeed = 0.1f;
        [SerializeField] private bool primarySyncXWithY = false;
        [SerializeField] private float primaryXAxisMinRange = -30f;
        [SerializeField] private float primaryXAxisMaxRange = 30f;
        
        [Header("Primary Star Y-Axis (Azimuth)")]
        [SerializeField] private bool primaryYAxisEnabled = true;
        [SerializeField] private CelestialRotationMode primaryYAxisMode = CelestialRotationMode.Continuous;
        [SerializeField] private float primaryYAxisSpeed = 0.25f;
        [SerializeField] private bool primaryYAxisOverrideSpeed = false; // New: override day sync
        [SerializeField] private float primaryYAxisMinRange = 0f; // New: for oscillate mode
        [SerializeField] private float primaryYAxisMaxRange = 360f; // New: for oscillate mode
        
        [Header("Red Dwarf Configuration")]
        [SerializeField] private bool redDwarfActive = false;
        
        [Header("Red Dwarf X-Axis (Elevation)")]
        [SerializeField] private bool redDwarfXAxisEnabled = false;
        [SerializeField] private CelestialRotationMode redDwarfXAxisMode = CelestialRotationMode.Oscillate;
        [SerializeField] private float redDwarfXAxisSpeed = 0.05f;
        [SerializeField] private bool redDwarfSyncXWithY = false;
        [SerializeField] private float redDwarfXAxisMinRange = -20f;
        [SerializeField] private float redDwarfXAxisMaxRange = 20f;
        
        [Header("Red Dwarf Y-Axis (Azimuth)")]
        [SerializeField] private bool redDwarfYAxisEnabled = false;
        [SerializeField] private CelestialRotationMode redDwarfYAxisMode = CelestialRotationMode.Continuous;
        [SerializeField] private float redDwarfYAxisSpeed = 0.15f;
        [SerializeField] private bool redDwarfYAxisOverrideSpeed = false; // New: override day sync
        [SerializeField] private float redDwarfYAxisMinRange = 0f; // New: for oscillate mode
        [SerializeField] private float redDwarfYAxisMaxRange = 360f; // New: for oscillate mode
        
        [Header("Weather Configuration")]
        [SerializeField] private WeatherData weatherData;
        [SerializeField] private bool overrideWeatherEnabled = false;

        // Properties
        public Season Season => season;
        
        // Primary Star Properties
        public bool PrimaryStarActive => primaryStarActive;
        public bool PrimaryXAxisEnabled => primaryXAxisEnabled;
        public CelestialRotationMode PrimaryXAxisMode => primaryXAxisMode;
        public float PrimaryXAxisSpeed => primaryXAxisSpeed;
        public bool PrimarySyncXWithY => primarySyncXWithY;
        public float PrimaryXAxisMinRange => primaryXAxisMinRange;
        public float PrimaryXAxisMaxRange => primaryXAxisMaxRange;
        public bool PrimaryYAxisEnabled => primaryYAxisEnabled;
        public CelestialRotationMode PrimaryYAxisMode => primaryYAxisMode;
        public float PrimaryYAxisSpeed => primaryYAxisSpeed;
        public bool PrimaryYAxisOverrideSpeed => primaryYAxisOverrideSpeed; // New
        public float PrimaryYAxisMinRange => primaryYAxisMinRange; // New
        public float PrimaryYAxisMaxRange => primaryYAxisMaxRange; // New
        
        // Red Dwarf Properties
        public bool RedDwarfActive => redDwarfActive;
        public bool RedDwarfXAxisEnabled => redDwarfXAxisEnabled;
        public CelestialRotationMode RedDwarfXAxisMode => redDwarfXAxisMode;
        public float RedDwarfXAxisSpeed => redDwarfXAxisSpeed;
        public bool RedDwarfSyncXWithY => redDwarfSyncXWithY;
        public float RedDwarfXAxisMinRange => redDwarfXAxisMinRange;
        public float RedDwarfXAxisMaxRange => redDwarfXAxisMaxRange;
        public bool RedDwarfYAxisEnabled => redDwarfYAxisEnabled;
        public CelestialRotationMode RedDwarfYAxisMode => redDwarfYAxisMode;
        public float RedDwarfYAxisSpeed => redDwarfYAxisSpeed;
        public bool RedDwarfYAxisOverrideSpeed => redDwarfYAxisOverrideSpeed; // New
        public float RedDwarfYAxisMinRange => redDwarfYAxisMinRange; // New
        public float RedDwarfYAxisMaxRange => redDwarfYAxisMaxRange; // New
        
        // Weather Properties
        public WeatherData WeatherData => weatherData;
        public bool HasWeather => weatherData != null && !overrideWeatherEnabled;

        /// <summary>
        /// Validates that seasonal data configuration is reasonable
        /// </summary>
        /// <returns>True if settings are valid</returns>
        public bool IsValid()
        {
            // Basic validation - weather data exists or is intentionally disabled
            bool weatherValid = weatherData != null || overrideWeatherEnabled;
            
            // Validate celestial settings are reasonable
            bool celestialValid = ValidateCelestialSettings();
            
            return weatherValid && celestialValid;
        }

        /// <summary>
        /// Validates that celestial settings are reasonable
        /// </summary>
        /// <returns>True if settings are valid</returns>
        private bool ValidateCelestialSettings()
        {
            // Check for reasonable speed ranges
            if (primaryStarActive)
            {
                if (primaryXAxisEnabled && (primaryXAxisSpeed < 0f || primaryXAxisSpeed > 10f))
                {
                    Debug.LogWarning($"Primary Star X-axis speed ({primaryXAxisSpeed}) seems unreasonable in {season}");
                    return false;
                }
                
                if (primaryYAxisEnabled && primaryYAxisOverrideSpeed && (primaryYAxisSpeed < 0f || primaryYAxisSpeed > 10f))
                {
                    Debug.LogWarning($"Primary Star Y-axis speed ({primaryYAxisSpeed}) seems unreasonable in {season}");
                    return false;
                }
            }

            if (redDwarfActive)
            {
                if (redDwarfXAxisEnabled && (redDwarfXAxisSpeed < 0f || redDwarfXAxisSpeed > 10f))
                {
                    Debug.LogWarning($"Red Dwarf X-axis speed ({redDwarfXAxisSpeed}) seems unreasonable in {season}");
                    return false;
                }
                
                if (redDwarfYAxisEnabled && redDwarfYAxisOverrideSpeed && (redDwarfYAxisSpeed < 0f || redDwarfYAxisSpeed > 10f))
                {
                    Debug.LogWarning($"Red Dwarf Y-axis speed ({redDwarfYAxisSpeed}) seems unreasonable in {season}");
                    return false;
                }
            }

            return true;
        }

        private void OnValidate()
        {
            // Clamp speeds to reasonable ranges
            primaryXAxisSpeed = Mathf.Max(0f, primaryXAxisSpeed);
            primaryYAxisSpeed = Mathf.Max(0f, primaryYAxisSpeed);
            redDwarfXAxisSpeed = Mathf.Max(0f, redDwarfXAxisSpeed);
            redDwarfYAxisSpeed = Mathf.Max(0f, redDwarfYAxisSpeed);

            // Ensure min/max ranges are valid for X-axis
            if (primaryXAxisMinRange > primaryXAxisMaxRange)
            {
                float temp = primaryXAxisMinRange;
                primaryXAxisMinRange = primaryXAxisMaxRange;
                primaryXAxisMaxRange = temp;
            }

            if (redDwarfXAxisMinRange > redDwarfXAxisMaxRange)
            {
                float temp = redDwarfXAxisMinRange;
                redDwarfXAxisMinRange = redDwarfXAxisMaxRange;
                redDwarfXAxisMaxRange = temp;
            }

            // Ensure min/max ranges are valid for Y-axis (new)
            if (primaryYAxisMinRange > primaryYAxisMaxRange)
            {
                float temp = primaryYAxisMinRange;
                primaryYAxisMinRange = primaryYAxisMaxRange;
                primaryYAxisMaxRange = temp;
            }

            if (redDwarfYAxisMinRange > redDwarfYAxisMaxRange)
            {
                float temp = redDwarfYAxisMinRange;
                redDwarfYAxisMinRange = redDwarfYAxisMaxRange;
                redDwarfYAxisMaxRange = temp;
            }

            // Clamp Y-axis ranges to reasonable values
            primaryYAxisMinRange = Mathf.Clamp(primaryYAxisMinRange, -360f, 360f);
            primaryYAxisMaxRange = Mathf.Clamp(primaryYAxisMaxRange, -360f, 360f);
            redDwarfYAxisMinRange = Mathf.Clamp(redDwarfYAxisMinRange, -360f, 360f);
            redDwarfYAxisMaxRange = Mathf.Clamp(redDwarfYAxisMaxRange, -360f, 360f);
        }
    }
}