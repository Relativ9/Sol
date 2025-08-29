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
        [Tooltip("Which season this configuration applies to")]
        [SerializeField] private Season season = Season.LongNight;
        
        [Header("Primary Star Configuration")]
        [Tooltip("Whether the primary star is visible and active during this season")]
        [SerializeField] private bool primaryStarActive = true;
        
        [Header("Primary Star X-Axis (Elevation)")]
        [Tooltip("Enable X-axis rotation to create elevation changes (sun rising/setting effect)")]
        [SerializeField] private bool primaryXAxisEnabled = false;
        
        [Tooltip("Oscillate: moves between min/max values. Continuous: rotates 360° at set speed")]
        [SerializeField] private CelestialRotationMode primaryXAxisMode = CelestialRotationMode.Oscillate;
        
        [Tooltip("Rotation speed in degrees per celestial time unit (only used if not synced with Y-axis)")]
        [SerializeField] private float primaryXAxisSpeed = 0.1f;
        
        [Tooltip("Sync X-axis speed to complete one full min-max-min cycle per Y-axis rotation (realistic day cycle)")]
        [SerializeField] private bool primarySyncXWithY = false;
        
        [Tooltip("Minimum elevation angle in degrees (180 is the horizon, anything above is below. 120 is high in teh sky)")]
        [Range(120f, 240f)]
        [SerializeField] private float primaryXAxisMinRange;
        
        [Tooltip("Maximum elevation angle in degrees (180 is the horizon, anything above is below. 120 is high in teh sky)")]
        [Range(120f, 240f)]
        [SerializeField] private float primaryXAxisMaxRange;
        
        [Header("Primary Star Y-Axis (Azimuth)")]
        [Tooltip("Enable Y-axis rotation for azimuth movement (sun moving across sky)")]
        [SerializeField] private bool primaryYAxisEnabled = true;
        
        [Tooltip("Oscillate: moves between min/max values. Continuous: rotates 360° creating day/night cycle")]
        [SerializeField] private CelestialRotationMode primaryYAxisMode = CelestialRotationMode.Continuous;
        
        [Tooltip("Custom rotation speed in degrees per celestial time unit (only used if Override Speed is enabled)")]
        [SerializeField] private float primaryYAxisSpeed = 0.25f;
        
        [Tooltip("Override automatic day sync - use custom speed instead of syncing with TimeManager day length")]
        [SerializeField] private bool primaryYAxisOverrideSpeed = false;
        
        [Tooltip("Minimum azimuth angle in degrees (used in Oscillate mode)")]
        [SerializeField] private float primaryYAxisMinRange = 0f;
        
        [Tooltip("Maximum azimuth angle in degrees (used in Oscillate mode)")]
        [SerializeField] private float primaryYAxisMaxRange = 360f;
        
        [Header("Red Dwarf Configuration")]
        [Tooltip("Whether the red dwarf star is visible and active during this season")]
        [SerializeField] private bool redDwarfActive = false;
        
        [Header("Red Dwarf X-Axis (Elevation)")]
        [Tooltip("Enable X-axis rotation to create elevation changes for the red dwarf")]
        [SerializeField] private bool redDwarfXAxisEnabled = false;
        
        [Tooltip("Oscillate: moves between min/max values. Continuous: rotates 360° at set speed")]
        [SerializeField] private CelestialRotationMode redDwarfXAxisMode = CelestialRotationMode.Oscillate;
        
        [Tooltip("Rotation speed in degrees per celestial time unit (only used if not synced with Y-axis)")]
        [SerializeField] private float redDwarfXAxisSpeed = 0.05f;
        
        [Tooltip("Sync X-axis speed to complete one full min-max-min cycle per Y-axis rotation")]
        [SerializeField] private bool redDwarfSyncXWithY = false;
        
        [Tooltip("Minimum elevation angle in degrees (180 is the horizon, anything above is below. 120 is high in teh sky)")]
        [Range(120f, 240f)]
        [SerializeField] private float redDwarfXAxisMinRange;
        
        [Tooltip("Maximum elevation angle in degrees (180 is the horizon, anything above is below. 120 is high in teh sky)")]
        [Range(120f, 240f)]
        [SerializeField] private float redDwarfXAxisMaxRange;
        
        [Header("Red Dwarf Y-Axis (Azimuth)")]
        [Tooltip("Enable Y-axis rotation for azimuth movement of the red dwarf")]
        [SerializeField] private bool redDwarfYAxisEnabled = false;
        
        [Tooltip("Oscillate: moves between min/max values. Continuous: rotates 360° creating day/night cycle")]
        [SerializeField] private CelestialRotationMode redDwarfYAxisMode = CelestialRotationMode.Continuous;
        
        [Tooltip("Custom rotation speed in degrees per celestial time unit (only used if Override Speed is enabled)")]
        [SerializeField] private float redDwarfYAxisSpeed = 0.15f;
        
        [Tooltip("Override automatic day sync - use custom speed instead of syncing with TimeManager day length")]
        [SerializeField] private bool redDwarfYAxisOverrideSpeed = false;
        
        [Tooltip("Minimum azimuth angle in degrees (used in Oscillate mode)")]
        [SerializeField] private float redDwarfYAxisMinRange = 0f;
        
        [Tooltip("Maximum azimuth angle in degrees (used in Oscillate mode)")]
        [SerializeField] private float redDwarfYAxisMaxRange = 360f;
        
        [Header("Weather Configuration")]
        [Tooltip("Weather data asset to use for this season (leave empty to disable weather)")]
        [SerializeField] private WeatherData weatherData;
        
        [Tooltip("Force disable weather for this season even if weather data is assigned")]
        [SerializeField] private bool overrideWeatherEnabled = false;

        // Properties (unchanged)
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
        public bool PrimaryYAxisOverrideSpeed => primaryYAxisOverrideSpeed;
        public float PrimaryYAxisMinRange => primaryYAxisMinRange;
        public float PrimaryYAxisMaxRange => primaryYAxisMaxRange;
        
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
        public bool RedDwarfYAxisOverrideSpeed => redDwarfYAxisOverrideSpeed;
        public float RedDwarfYAxisMinRange => redDwarfYAxisMinRange;
        public float RedDwarfYAxisMaxRange => redDwarfYAxisMaxRange;
        
        // Weather Properties
        public WeatherData WeatherData => weatherData;
        public bool HasWeather => weatherData != null && !overrideWeatherEnabled;

        // Rest of the methods remain unchanged...
        public bool IsValid()
        {
            bool weatherValid = weatherData != null || overrideWeatherEnabled;
            bool celestialValid = ValidateCelestialSettings();
            return weatherValid && celestialValid;
        }

        private bool ValidateCelestialSettings()
        {
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

            // Ensure min/max ranges are valid for Y-axis
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