using System.Collections.Generic;
using UnityEngine;

namespace Sol.Editor
{
    /// <summary>
    /// Represents configuration for a celestial body (sun or moon).
    /// Contains orbital mechanics, light properties, and behavior settings.
    /// </summary>
    [System.Serializable]
    public class CelestialBodyConfig
    {
        [Header("Identity")]
        public string name = "Sol";
        public bool active = true;

        [Header("Orbital Mechanics")]
        public bool yAxisEnabled = true;
        public float yAxisSpeed = 1.0f;
        public bool yAxisOverrideSpeed = true;
        public float orbitalAngle = 23.5f;  // Axial tilt in degrees
        public float baseElevation = 180f;   // Starting position in degrees
        public float orbitalPeriod = 1f;     // How many in-game days for one orbit
        public float phaseOffset = 0f;       // Phase offset in degrees

        [Header("Light Settings")]
        public bool createDirectionalLight = true;
        public float lightTemperature = 6500f; // Kelvin (1000-20000)
        public float lightIntensity = 100000f; // Lux for HDRP
        public bool castShadows = true;

        [Header("Moon-Specific (Optional)")]
        public bool isMoon = false;
        public bool reflectSunLight = true;
        public string sunToReflect = "Sol"; // Name of sun to reflect light from

        /// <summary>
        /// Creates a default sun configuration.
        /// </summary>
        public static CelestialBodyConfig CreateDefaultSun()
        {
            return new CelestialBodyConfig
            {
                name = "Sol",
                orbitalPeriod = 1f,
                phaseOffset = 0f,
                lightTemperature = 6500f,
                lightIntensity = 100000f,
                castShadows = true,
                isMoon = false
            };
        }

        /// <summary>
        /// Creates a default moon configuration.
        /// </summary>
        public static CelestialBodyConfig CreateDefaultMoon()
        {
            return new CelestialBodyConfig
            {
                name = "Luna",
                orbitalPeriod = 29.5f,
                phaseOffset = 180f,
                lightTemperature = 4000f,
                lightIntensity = 5000f,
                castShadows = false,
                isMoon = true,
                reflectSunLight = true,
                sunToReflect = "Sol"
            };
        }
    }

    /// <summary>
    /// Complete configuration for Sol system setup.
    /// Includes scene setup, seasonal data, celestial bodies, and rendering settings.
    /// This is a Data Transfer Object (DTO) shared between wizard and utilities.
    /// </summary>
    [System.Serializable]
    public class SetupConfig
    {
        [Header("Scene Setup")]
        [Tooltip("Create the TimeManager component that controls time progression")]
        public bool createTimeManager = true;

        [Tooltip("Create the WorldTimeData ScriptableObject with time settings")]
        public bool createWorldTimeData = true;

        [Header("Seasonal Data")]
        [Tooltip("Create SeasonalData asset with seasonal configurations")]
        public bool createSeasonalData = true;

        [Tooltip("Number of seasons in the world (2-12)")]
        [Range(2, 12)]
        public int numberOfSeasons = 4;

        [Tooltip("Names for each season")]
        public string[] seasonNames = { "Spring", "Summer", "Autumn", "Winter" };

        [Header("Celestial Bodies")]
        [Tooltip("Configuration for all suns in the system")]
        public List<CelestialBodyConfig> suns = new List<CelestialBodyConfig>();

        [Tooltip("Configuration for all moons in the system")]
        public List<CelestialBodyConfig> moons = new List<CelestialBodyConfig>();

        [Header("Sky and Fog")]
        [Tooltip("Create HDRP Sky and Fog Volume with profile")]
        public bool createSkyAndFog = true;

        [Tooltip("Path to existing HDRP Volume Profile (leave empty to create default)")]
        public string hdrpProfilePath = "";

        [Header("Demo Content")]
        [Tooltip("Add demo objects to showcase the system")]
        public bool createDemoScene = false;

        [Header("Asset Paths")]
        [Tooltip("Folder where ScriptableObject data will be created")]
        public string dataFolderPath = "Assets/Sol/Data";

        [Tooltip("Folder where prefabs will be created")]
        public string prefabFolderPath = "Assets/Sol/Prefabs";

        // Private backing field for sky/fog profile path
        [SerializeField] 
        private string _skyFogProfilePath = "Assets/SolSetupWizard/DefaultSkyFogProfile.asset";

        /// <summary>
        /// Gets the sky fog profile path (read-only access).
        /// </summary>
        public string skyFogProfilePath => _skyFogProfilePath;

        /// <summary>
        /// Sets the sky fog profile path.
        /// </summary>
        public void SetSkyFogProfile(string path)
        {
            _skyFogProfilePath = path;
        }

        /// <summary>
        /// Default constructor initializes with sensible defaults.
        /// </summary>
        public SetupConfig()
        {
            // Initialize with one default sun
            suns.Add(CelestialBodyConfig.CreateDefaultSun());
            
            // Initialize with one default moon
            moons.Add(CelestialBodyConfig.CreateDefaultMoon());
        }

        /// <summary>
        /// Validates the configuration and returns error messages if invalid.
        /// </summary>
        public List<string> Validate()
        {
            List<string> errors = new List<string>();

            // Validate season count matches names
            if (seasonNames == null || seasonNames.Length != numberOfSeasons)
            {
                errors.Add($"Season names count ({seasonNames?.Length ?? 0}) doesn't match numberOfSeasons ({numberOfSeasons})");
            }

            // Validate sun names are unique
            HashSet<string> sunNames = new HashSet<string>();
            foreach (var sun in suns)
            {
                if (string.IsNullOrEmpty(sun.name))
                {
                    errors.Add("One or more suns have empty names");
                }
                else if (!sunNames.Add(sun.name))
                {
                    errors.Add($"Duplicate sun name: {sun.name}");
                }
            }

            // Validate moon names are unique
            HashSet<string> moonNames = new HashSet<string>();
            foreach (var moon in moons)
            {
                if (string.IsNullOrEmpty(moon.name))
                {
                    errors.Add("One or more moons have empty names");
                }
                else if (!moonNames.Add(moon.name))
                {
                    errors.Add($"Duplicate moon name: {moon.name}");
                }

                // Validate moon's sun reference exists
                if (moon.reflectSunLight && !sunNames.Contains(moon.sunToReflect))
                {
                    errors.Add($"Moon '{moon.name}' references non-existent sun '{moon.sunToReflect}'");
                }
            }

            return errors;
        }
    }
}
