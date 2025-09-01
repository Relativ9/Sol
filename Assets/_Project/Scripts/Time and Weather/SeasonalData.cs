// using System;
// using System.Collections.Generic;
// using UnityEngine;
//
// namespace Sol
// {
//     /// <summary>
//     /// Oscillate makes the body moves back and forth between the two rotational values
//     /// Continous rotates 360 degrees on the axis at a set speed.
//     /// </summary>
//     public enum CelestialRotationMode
//     {
//         Oscillate,
//         Continuous,
//         Fixed,
//     }
//     
//     [CreateAssetMenu(fileName = "SeasonalData", menuName = "Sol/Seasonal Data")]
//     public class SeasonalData : ScriptableObject
//     {
//         [Header("Season Configuration")]
//         [Tooltip("Which season this configuration applies to")]
//         [SerializeField] private Season season = Season.LongNight;
//         
//         [Header("Celestial Bodies")]
//         [Tooltip("Star configurations for this season")]
//         public List<CelestialBody> stars = new List<CelestialBody>();
//         
//         [Tooltip("Moon configurations for this season")]
//         public List<CelestialBody> moons = new List<CelestialBody>();
//         
//         [Header("Weather Configuration")]
//         [Tooltip("Weather data asset to use for this season (leave empty to disable weather)")]
//         [SerializeField] private WeatherData weatherData;
//         
//         [Tooltip("Force disable weather for this season even if weather data is assigned")]
//         [SerializeField] private bool overrideWeatherEnabled = false;
//
//         [Serializable]
//         public class CelestialBody
//         {
//             [Header("Basic Configuration")]
//             [Tooltip("Name of this celestial body")]
//             public string name = "Unnamed";
//             
//             [Tooltip("Whether this celestial body is visible and active during this season")]
//             public bool active = true;
//             
//             [Header("X-Axis (Elevation)")]
//             [Tooltip("Enable X-axis rotation to create elevation changes")]
//             public bool xAxisEnabled = false;
//             
//             [Tooltip("Oscillate: moves between min/max values. Continuous: rotates 360° at set speed")]
//             public CelestialRotationMode xAxisMode = CelestialRotationMode.Fixed;
//             
//             [Tooltip("Rotation speed in degrees per celestial time unit")]
//             public float xAxisSpeed = 0.1f;
//             
//             [Tooltip("Sync X-axis speed to complete one full min-max-min cycle per Y-axis rotation")]
//             public bool syncXWithY = false;
//             
//             [Tooltip("Minimum elevation angle in degrees (180 is horizon, 120 is high in sky)")]
//             [Range(95f, 240f)]
//             public float xAxisMinRange = 120f;
//             
//             [Tooltip("Maximum elevation angle in degrees (180 is horizon, 120 is high in sky)")]
//             [Range(95f, 240f)]
//             public float xAxisMaxRange = 240f;
//             
//             [Header("Y-Axis (Azimuth)")]
//             [Tooltip("Enable Y-axis rotation for azimuth movement")]
//             public bool yAxisEnabled = true;
//             
//             [Tooltip("Oscillate: moves between min/max values. Continuous: rotates 360° creating day/night cycle")]
//             public CelestialRotationMode yAxisMode = CelestialRotationMode.Continuous;
//             
//             [Tooltip("Custom rotation speed in degrees per celestial time unit")]
//             public float yAxisSpeed = 0.25f;
//             
//             [Tooltip("Override automatic day sync - use custom speed instead of syncing with TimeManager")]
//             public bool yAxisOverrideSpeed = false;
//             
//             [Tooltip("Minimum azimuth angle in degrees (used in Oscillate mode)")]
//             public float yAxisMinRange = 0f;
//             
//             [Tooltip("Maximum azimuth angle in degrees (used in Oscillate mode)")]
//             public float yAxisMaxRange = 360f;
//             
//             [Header("Moon-Specific Settings")]
//             [Tooltip("Orbital period in days (affects base rotation drift for moons)")]
//             public float orbitalPeriod = 104f;
//             
//             [Tooltip("Invert day/night cycle (moon rises when stars set, useful for night moons)")]
//             public bool invertDayNightCycle = false;
//             
//             [Header("Path Configuration")]
//             [Tooltip("Desired path angle in degrees (0°=horizontal, 45°=diagonal, 90°=vertical)")]
//             [Range(0f, 89f)]
//             public float desiredPathAngle = 30f;
//
//             [Tooltip("Starting elevation when sun rises (Y=0°)")]
//             [Range(160f, 200f)]
//             public float sunriseElevation = 180f; // Horizon
//
//             [Tooltip("Use path angle calculation instead of manual min/max ranges")]
//             public bool usePathAngleCalculation = true;
//             
//         }
//
//         // Properties
//         public Season Season => season;
//         public WeatherData WeatherData => weatherData;
//         public bool HasWeather => weatherData != null && !overrideWeatherEnabled;
//
//         /// <summary>
//         /// Gets a celestial body by name (searches both stars and moons)
//         /// </summary>
//         public CelestialBody GetCelestialBodyByName(string bodyName)
//         {
//             foreach (var star in stars)
//             {
//                 if (star.name.Equals(bodyName, StringComparison.OrdinalIgnoreCase))
//                     return star;
//             }
//             
//             foreach (var moon in moons)
//             {
//                 if (moon.name.Equals(bodyName, StringComparison.OrdinalIgnoreCase))
//                     return moon;
//             }
//             
//             return null;
//         }
//
//         /// <summary>
//         /// Checks if a celestial body is a moon
//         /// </summary>
//         public bool IsMoon(string bodyName)
//         {
//             foreach (var moon in moons)
//             {
//                 if (moon.name.Equals(bodyName, StringComparison.OrdinalIgnoreCase))
//                     return true;
//             }
//             return false;
//         }
//
//         public bool IsValid()
//         {
//             bool weatherValid = weatherData != null || overrideWeatherEnabled;
//             bool celestialValid = ValidateCelestialSettings();
//             return weatherValid && celestialValid;
//         }
//
//         private bool ValidateCelestialSettings()
//         {
//             foreach (var star in stars)
//             {
//                 if (star.active && !ValidateCelestialBody(star))
//                     return false;
//             }
//             
//             foreach (var moon in moons)
//             {
//                 if (moon.active && !ValidateCelestialBody(moon))
//                     return false;
//             }
//             
//             return true;
//         }
//
//         private bool ValidateCelestialBody(CelestialBody body)
//         {
//             if (body.xAxisEnabled && (body.xAxisSpeed < 0f || body.xAxisSpeed > 10f))
//             {
//                 Debug.LogWarning($"{body.name} X-axis speed ({body.xAxisSpeed}) seems unreasonable in {season}");
//                 return false;
//             }
//             
//             if (body.yAxisEnabled && body.yAxisOverrideSpeed && (body.yAxisSpeed < 0f || body.yAxisSpeed > 10f))
//             {
//                 Debug.LogWarning($"{body.name} Y-axis speed ({body.yAxisSpeed}) seems unreasonable in {season}");
//                 return false;
//             }
//             
//             return true;
//         }
//
//         private void OnValidate()
//         {
//             // Validate all celestial bodies
//             foreach (var star in stars)
//             {
//                 ValidateAndClampCelestialBody(star);
//             }
//             
//             foreach (var moon in moons)
//             {
//                 ValidateAndClampCelestialBody(moon);
//             }
//         }
//
//         private void ValidateAndClampCelestialBody(CelestialBody body)
//         {
//             // Clamp speeds to reasonable ranges
//             body.xAxisSpeed = Mathf.Max(0f, body.xAxisSpeed);
//             body.yAxisSpeed = Mathf.Max(0f, body.yAxisSpeed);
//             body.orbitalPeriod = Mathf.Max(1f, body.orbitalPeriod);
//
//             // Ensure min/max ranges are valid for X-axis
//             if (body.xAxisMinRange > body.xAxisMaxRange)
//             {
//                 float temp = body.xAxisMinRange;
//                 body.xAxisMinRange = body.xAxisMaxRange;
//                 body.xAxisMaxRange = temp;
//             }
//
//             // Ensure min/max ranges are valid for Y-axis
//             if (body.yAxisMinRange > body.yAxisMaxRange)
//             {
//                 float temp = body.yAxisMinRange;
//                 body.yAxisMinRange = body.yAxisMaxRange;
//                 body.yAxisMaxRange = temp;
//             }
//
//             // Clamp Y-axis ranges to reasonable values
//             body.yAxisMinRange = Mathf.Clamp(body.yAxisMinRange, -360f, 360f);
//             body.yAxisMaxRange = Mathf.Clamp(body.yAxisMaxRange, -360f, 360f);
//         }
//     }
// }

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Sol
{
    [System.Serializable]
    public class CelestialBody
    {
        [Header("Basic Configuration")]
        public string name = "Sol";
        public bool active = true;
        
        [Header("Azimuth (Y-Axis) - Continuous Orbit")]
        [Tooltip("Enable continuous orbital motion around the sky")]
        public bool yAxisEnabled = true;
        
        [Tooltip("Orbital speed multiplier (1.0 = one orbit per day)")]
        public float yAxisSpeed = 1f;
        
        [Tooltip("Override to sync with day length from TimeManager")]
        public bool yAxisOverrideSpeed = false;
        
        [Header("Orbital Path Configuration")]
        [Tooltip("Angle of orbital path relative to horizon (0° = flat circle, 45° = angled orbit)")]
        [Range(0f, 89f)]
        public float orbitalAngle = 30f;
        
        [Tooltip("Base elevation when celestial body is at Y=0° (starting point of orbit)")]
        [Range(0f, 360f)]
        public float baseElevation = 180f; // Horizon
        
        [Header("Moon-Specific Settings")]
        [Tooltip("Orbital period in days (creates monthly drift effect)")]
        public float orbitalPeriod = 29.5f;
        
        [Tooltip("Phase offset in degrees from sun position")]
        [Range(0f, 360f)]
        public float phaseOffset = 0f;
    }

    [CreateAssetMenu(fileName = "New Seasonal Data", menuName = "Sol/Seasonal Data")]
    public class SeasonalData : ScriptableObject
    {
        [Header("Celestial Bodies")]
        [SerializeField] private List<CelestialBody> stars = new List<CelestialBody>();
        [SerializeField] private List<CelestialBody> moons = new List<CelestialBody>();

        public List<CelestialBody> Stars => stars;
        public List<CelestialBody> Moons => moons;

        public CelestialBody GetCelestialBodyByName(string name)
        {
            var star = stars.FirstOrDefault(s => s.name == name);
            if (star != null) return star;

            var moon = moons.FirstOrDefault(m => m.name == name);
            return moon;
        }

        public List<CelestialBody> GetAllActiveCelestialBodies()
        {
            var allBodies = new List<CelestialBody>();
            allBodies.AddRange(stars.Where(s => s.active));
            allBodies.AddRange(moons.Where(m => m.active));
            return allBodies;
        }

        private void OnValidate()
        {
            // Ensure we have at least one star
            if (stars.Count == 0)
            {
                stars.Add(new CelestialBody { name = "Sol" });
            }
        }
    }
}