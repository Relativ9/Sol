using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sol
{
    /// <summary>
    /// Stores celestial body configurations and atmospheric settings for a specific season.
    /// Each season defines how celestial bodies behave during that period.
    /// Preserves calculation methods used by other systems.
    /// </summary>
    [CreateAssetMenu(fileName = "New Seasonal Data", menuName = "Sol/Seasonal Data")]
    public class SeasonalData : ScriptableObject
    {
        [Header("Season Identity")]
        [Tooltip("Name of this season")]
        public string seasonName = "New Season";

        [Header("Celestial Bodies")]
        [SerializeField] 
        [Tooltip("Star/sun configurations for this season")]
        private List<CelestialBodySeasonalConfig> stars = new List<CelestialBodySeasonalConfig>();
        
        [SerializeField]
        [Tooltip("Moon configurations for this season")]
        private List<CelestialBodySeasonalConfig> moons = new List<CelestialBodySeasonalConfig>();

        [Header("Common Orbital Settings")]
        [SerializeField] 
        [Tooltip("Use a common orbital angle for all celestial bodies in this season")]
        private bool useCommonOrbitalAngle = true;
        
        [SerializeField]
        [Tooltip("Common orbital angle for all celestial bodies (Earth's axial tilt is 23.5°)")]
        [Range(-90f, 90f)]
        private float commonOrbitalAngle = 23.5f;

        [Header("Atmospheric Settings")]
        [Tooltip("Ambient light color during daytime")]
        public Color dayAmbientColor = new Color(0.5f, 0.5f, 0.5f);

        [Tooltip("Ambient light color during nighttime")]
        public Color nightAmbientColor = new Color(0.1f, 0.1f, 0.2f);

        [Tooltip("Sky exposure multiplier")]
        [Range(0f, 2f)]
        public float skyExposure = 1.0f;

        [Tooltip("Fog density during this season")]
        [Range(0f, 1f)]
        public float fogDensity = 0.01f;

        [Tooltip("Fog color tint")]
        public Color fogColor = Color.gray;

        #region Properties

        /// <summary>
        /// Public accessor for stars list
        /// </summary>
        public List<CelestialBodySeasonalConfig> Stars => stars;

        /// <summary>
        /// Public accessor for moons list
        /// </summary>
        public List<CelestialBodySeasonalConfig> Moons => moons;

        #endregion

        #region Celestial Body Configuration Class

        /// <summary>
        /// Configuration for a single celestial body within a season.
        /// Defines all orbital mechanics and visual appearance for this season.
        /// </summary>
        [Serializable]
        public class CelestialBodySeasonalConfig
        {
            [Header("Identity")]
            [Tooltip("Name of this celestial body (must match across seasons)")]
            public string name = "Celestial Body";

            [Tooltip("Is this body active during this season?")]
            public bool active = true;

            [Header("Orbital Override Settings")]
            [Tooltip("Override the common orbital angle with individual setting")]
            public bool overrideOrbitalAngle = false;

            [Header("Azimuth (Y-Axis) - Continuous Orbit")]
            [Tooltip("Enable continuous orbital motion around the sky")]
            public bool yAxisEnabled = true;

            [Tooltip("Orbital speed multiplier (1.0 = one orbit per day)")]
            public float yAxisSpeed = 1f;

            [Tooltip("Override to sync with day length from TimeManager")]
            public bool yAxisOverrideSpeed = false;

            [Header("Orbital Path Configuration")]
            [Tooltip("Angle of orbital path relative to horizon (0° = flat circle, 45° = angled orbit)")]
            [Range(-90f, 90f)]
            public float orbitalAngle = 23.5f;

            [Tooltip("Base elevation when celestial body is at Y=0° (starting point of orbit)")]
            [Range(0f, 360f)]
            public float baseElevation = 180f;

            [Header("Moon-Specific Settings")]
            [Tooltip("Orbital period in days (creates monthly drift effect)")]
            [Min(0.1f)]
            public float orbitalPeriod = 29.5f;

            [Tooltip("Phase offset in degrees from sun position")]
            [Range(0f, 360f)]
            public float phaseOffset = 0f;

            [Header("Light Configuration")]
            [Tooltip("Create/control a directional light for this body")]
            public bool hasDirectionalLight = true;

            [Tooltip("Use color temperature instead of direct color")]
            public bool useColorTemperature = true;

            [Tooltip("Light color temperature (Kelvin)")]
            [Range(1000f, 20000f)]
            public float lightTemperature = 6500f;

            [Tooltip("Direct light color (used if color temperature disabled)")]
            public Color lightColor = Color.white;

            [Tooltip("Light intensity (lux)")]
            [Min(0f)]
            public float lightIntensity = 100000f;

            [Tooltip("Should this light cast shadows?")]
            public bool castShadows = true;

            [Header("Visual Appearance")]
            [Tooltip("Angular diameter in degrees (Sun ~0.53°, Moon ~0.52°)")]
            [Range(0.1f, 10f)]
            public float angularDiameter = 0.53f;

            [Tooltip("Surface color/tint of the celestial body")]
            public Color surfaceColor = new Color(1f, 0.95f, 0.8f, 1f);

            [Tooltip("Flare size multiplier")]
            [Range(0f, 5f)]
            public float flareSize = 1f;

            [Tooltip("Flare falloff distance")]
            [Range(1f, 50f)]
            public float flareFalloff = 10f;

            [Tooltip("Flare brightness multiplier")]
            [Range(0f, 10f)]
            public float flareBrightness = 2f;

            [Header("Moon Surface (Moons Only)")]
            [Tooltip("Optional surface texture for moons")]
            public Texture2D surfaceTexture = null;

            /// <summary>
            /// Calculates flare tint color based on surface color and brightness
            /// </summary>
            public Color GetFlareTintColor()
            {
                return surfaceColor * flareBrightness;
            }
        }

        #endregion

        #region Query Methods (Used by Other Systems)

        /// <summary>
        /// Gets a celestial body by name from either stars or moons.
        /// Critical method used by CelestialRotator and other systems.
        /// </summary>
        /// <param name="bodyName">Name of the celestial body to find</param>
        /// <returns>CelestialBodySeasonalConfig if found, null otherwise</returns>
        public CelestialBodySeasonalConfig GetCelestialBodyByName(string bodyName)
        {
            var star = stars?.FirstOrDefault(s => s.name == bodyName);
            if (star != null) return star;

            var moon = moons?.FirstOrDefault(m => m.name == bodyName);
            return moon;
        }

        /// <summary>
        /// Gets the effective orbital angle for a celestial body.
        /// Respects both common orbital angle and individual overrides.
        /// Critical method used by CelestialRotator for positioning calculations.
        /// </summary>
        /// <param name="celestialBody">The celestial body to get orbital angle for</param>
        /// <returns>Effective orbital angle in degrees</returns>
        public float GetEffectiveOrbitalAngle(CelestialBodySeasonalConfig celestialBody)
        {
            if (celestialBody == null) return commonOrbitalAngle;
            
            return (useCommonOrbitalAngle && !celestialBody.overrideOrbitalAngle) 
                ? commonOrbitalAngle 
                : celestialBody.orbitalAngle;
        }

        /// <summary>
        /// Gets all active celestial bodies (both stars and moons).
        /// Used by systems that need to iterate over all active bodies.
        /// </summary>
        /// <returns>Combined list of all active celestial bodies</returns>
        public List<CelestialBodySeasonalConfig> GetAllActiveCelestialBodies()
        {
            var allBodies = new List<CelestialBodySeasonalConfig>();
            
            if (stars != null)
                allBodies.AddRange(stars.Where(s => s.active));
            
            if (moons != null)
                allBodies.AddRange(moons.Where(m => m.active));
            
            return allBodies;
        }

        /// <summary>
        /// Gets star configuration by name
        /// </summary>
        public CelestialBodySeasonalConfig GetStarByName(string starName)
        {
            return stars?.FirstOrDefault(s => s.name == starName);
        }

        /// <summary>
        /// Gets moon configuration by name
        /// </summary>
        public CelestialBodySeasonalConfig GetMoonByName(string moonName)
        {
            return moons?.FirstOrDefault(m => m.name == moonName);
        }

        /// <summary>
        /// Checks if a celestial body with the given name exists in this season
        /// </summary>
        public bool HasCelestialBody(string bodyName)
        {
            return GetCelestialBodyByName(bodyName) != null;
        }

        /// <summary>
        /// Gets count of all celestial bodies in this season
        /// </summary>
        public int GetTotalCelestialBodyCount()
        {
            return (stars?.Count ?? 0) + (moons?.Count ?? 0);
        }

        #endregion

        #region Default Configurations

        /// <summary>
        /// Creates a default sun configuration for this season
        /// </summary>
        public static CelestialBodySeasonalConfig CreateDefaultSun(string name = "Sol", float orbitalAngle = 23.5f, float baseElevation = 180f)
        {
            return new CelestialBodySeasonalConfig
            {
                name = name,
                active = true,
                overrideOrbitalAngle = false,
                yAxisEnabled = true,
                yAxisSpeed = 1f,
                yAxisOverrideSpeed = false,
                orbitalAngle = orbitalAngle,
                baseElevation = baseElevation,
                orbitalPeriod = 1f,
                phaseOffset = 0f,
                hasDirectionalLight = true,
                useColorTemperature = true,
                lightTemperature = 6500f,
                lightColor = Color.white,
                lightIntensity = 100000f,
                castShadows = true,
                angularDiameter = 0.53f,
                surfaceColor = new Color(1f, 0.95f, 0.8f, 1f),
                flareSize = 1f,
                flareFalloff = 10f,
                flareBrightness = 2f,
                surfaceTexture = null
            };
        }

        /// <summary>
        /// Creates a default moon configuration for this season
        /// </summary>
        public static CelestialBodySeasonalConfig CreateDefaultMoon(string name = "Luna", float orbitalAngle = 23.5f, float baseElevation = 180f)
        {
            return new CelestialBodySeasonalConfig
            {
                name = name,
                active = true,
                overrideOrbitalAngle = false,
                yAxisEnabled = true,
                yAxisSpeed = 1f,
                yAxisOverrideSpeed = false,
                orbitalAngle = orbitalAngle,
                baseElevation = baseElevation,
                orbitalPeriod = 29.5f,
                phaseOffset = 180f,
                hasDirectionalLight = true,
                useColorTemperature = true,
                lightTemperature = 4000f,
                lightColor = new Color(0.8f, 0.8f, 1f),
                lightIntensity = 500f,
                castShadows = false,
                angularDiameter = 0.52f,
                surfaceColor = new Color(0.7f, 0.7f, 0.75f, 1f),
                flareSize = 0.5f,
                flareFalloff = 5f,
                flareBrightness = 0.5f,
                surfaceTexture = null
            };
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validates seasonal data configuration
        /// </summary>
        private void OnValidate()
        {
            // Ensure we have at least one star
            if (stars == null || stars.Count == 0)
            {
                if (stars == null) stars = new List<CelestialBodySeasonalConfig>();
                stars.Add(CreateDefaultSun());
                Debug.Log($"[SeasonalData] {seasonName}: Added default sun");
            }

            // Check for duplicate star names
            if (stars != null)
            {
                HashSet<string> starNames = new HashSet<string>();
                foreach (var star in stars)
                {
                    if (string.IsNullOrEmpty(star.name))
                    {
                        Debug.LogWarning($"[SeasonalData] {seasonName}: A star has no name!");
                        continue;
                    }
                    if (!starNames.Add(star.name))
                    {
                        Debug.LogWarning($"[SeasonalData] {seasonName}: Duplicate star name '{star.name}'");
                    }
                }
            }

            // Check for duplicate moon names
            if (moons != null)
            {
                HashSet<string> moonNames = new HashSet<string>();
                foreach (var moon in moons)
                {
                    if (string.IsNullOrEmpty(moon.name))
                    {
                        Debug.LogWarning($"[SeasonalData] {seasonName}: A moon has no name!");
                        continue;
                    }
                    if (!moonNames.Add(moon.name))
                    {
                        Debug.LogWarning($"[SeasonalData] {seasonName}: Duplicate moon name '{moon.name}'");
                    }
                }
            }

            // Validate common orbital angle
            if (useCommonOrbitalAngle)
            {
                commonOrbitalAngle = Mathf.Clamp(commonOrbitalAngle, -90f, 90f);
            }
        }

        #endregion

        #region Editor Utilities

#if UNITY_EDITOR
        /// <summary>
        /// Context menu to add a default sun
        /// </summary>
        [ContextMenu("Add Default Sun")]
        private void AddDefaultSun()
        {
            if (stars == null) stars = new List<CelestialBodySeasonalConfig>();
            
            int sunCount = stars.Count(s => s.name.Contains("Sol") || s.name.Contains("Sun"));
            string newName = sunCount == 0 ? "Sol" : $"Sun {sunCount + 1}";
            
            stars.Add(CreateDefaultSun(newName, commonOrbitalAngle, 180f));
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[SeasonalData] Added sun: {newName}");
        }

        /// <summary>
        /// Context menu to add a default moon
        /// </summary>
        [ContextMenu("Add Default Moon")]
        private void AddDefaultMoon()
        {
            if (moons == null) moons = new List<CelestialBodySeasonalConfig>();
            
            int moonCount = moons.Count;
            string newName = moonCount == 0 ? "Luna" : $"Moon {moonCount + 1}";
            
            moons.Add(CreateDefaultMoon(newName, commonOrbitalAngle, 180f));
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[SeasonalData] Added moon: {newName}");
        }

        /// <summary>
        /// Context menu to sync all bodies to common orbital angle
        /// </summary>
        [ContextMenu("Sync All to Common Orbital Angle")]
        private void SyncAllToCommonOrbitalAngle()
        {
            int updated = 0;
            
            if (stars != null)
            {
                foreach (var star in stars)
                {
                    if (!star.overrideOrbitalAngle)
                    {
                        star.orbitalAngle = commonOrbitalAngle;
                        updated++;
                    }
                }
            }
            
            if (moons != null)
            {
                foreach (var moon in moons)
                {
                    if (!moon.overrideOrbitalAngle)
                    {
                        moon.orbitalAngle = commonOrbitalAngle;
                        updated++;
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[SeasonalData] Synced {updated} celestial bodies to common orbital angle: {commonOrbitalAngle}°");
        }
#endif

        #endregion
    }
}
