using UnityEngine;
using System.Collections.Generic;

namespace Sol.Editor
{
    /// <summary>
    /// Time scale preset values for common configurations
    /// </summary>
    public enum TimeScalePreset
    {
        RealTime = 1,
        DoubleTime = 2,
        Fast = 10,
        VeryFast = 20,
        OneMinute = 60,
        TwoMinutes = 120,
        FourMinutes = 240,
        TenMinutes = 600,
        OneHour = 3600,
        Custom = -1
    }
    
    [System.Serializable]
    public class SeasonalBodyConfig
    {
        public string bodyName;
        public float orbitalAngle = 23.5f;
        public float baseElevation = 180f;
        public float orbitalPeriod = 1f;
        public float phaseOffset = 0f;
    
        [Header("Light Settings")]
        public float lightIntensity = 100000f;
        public float lightTemperature = 6500f;
        public Color lightColor = Color.white;
    }

    /// <summary>
    /// Configuration for one complete season
    /// </summary>
    [System.Serializable]
    public class SeasonConfig
    {
        public string seasonName;
        public List<SeasonalBodyConfig> sunConfigs = new List<SeasonalBodyConfig>();
        public List<SeasonalBodyConfig> moonConfigs = new List<SeasonalBodyConfig>();
    }

    /// <summary>
    /// Data Transfer Object for Sol system setup configuration.
    /// </summary>
    [System.Serializable]
    public class SetupConfig
    {
        #region Scene Setup
        
        [Header("Scene Setup")]
        public bool createTimeManager = true;
        public bool createWorldTimeData = true;
        
        #endregion

        #region Calendar Configuration

        [Header("Calendar Configuration")]
        [Tooltip("Time progression multiplier (60 = 1 real second equals 1 game minute)")]
        public float timeScale = 10f;

        [Tooltip("Number of hours displayed per day")]
        public int hoursPerDay = 20;

        [Tooltip("Number of minutes per hour")]
        public int minutesPerHour = 60;

        [Tooltip("Number of seconds per minute")]
        public int secondsPerMinute = 60;

        [Tooltip("Total number of days in one year")]
        public int totalDaysInYear = 832;

        [Tooltip("Number of months in a year")]
        public int monthsPerYear = 8;

        [Tooltip("Days in each month (should divide evenly into year)")]
        public int daysPerMonth = 104;

        [Tooltip("Names of the months")]
        public string[] monthNames = new string[]
        {
            "Glavyr", "Tharven", "Solmyr", "Aethon",
            "Lumis", "Verdis", "Harvyx", "Frosten"
        };

        [Tooltip("Number of days for smooth season transitions")]
        public int seasonTransitionDays = 10;
        
        [Header("Seasonal Body Configurations")]
        public List<SeasonConfig> seasonConfigs = new List<SeasonConfig>();

        #endregion

        #region Seasonal Data

        [Header("Seasonal Data")]
        public bool createSeasonalData = true;
        
        [Tooltip("Number of seasons to create (2-12)")]
        public int numberOfSeasons = 4;
        
        [Tooltip("Names for each season")]
        public string[] seasonNames = new string[] { "Lansomr", "Svik", "Evinotr", "Gro" };

        #endregion

        #region Celestial Bodies

        [Header("Celestial Bodies")]
        [Tooltip("List of suns in the system (names only, behavior defined per-season)")]
        public List<CelestialBodyIdentity> suns = new List<CelestialBodyIdentity>();
        
        [Tooltip("List of moons in the system (names only, behavior defined per-season)")]
        public List<CelestialBodyIdentity> moons = new List<CelestialBodyIdentity>();

        #endregion

        #region Sky and Fog

        [Header("Sky and Fog")]
        public bool createSkyAndFog = true;
        public string hdrpProfilePath = "";

        #endregion

        #region Demo Content

        [Header("Demo Content")]
        public bool createDemoScene = false;

        #endregion

        #region Advanced Options

        [Header("Advanced Options")]
        public string dataFolderPath = "Assets/Sol/Data";
        public string prefabFolderPath = "Assets/Sol/Prefabs";

        #endregion

        #region Constructor

        public SetupConfig()
        {
            // Add default sun
            suns.Add(new CelestialBodyIdentity { name = "Sol", createDirectionalLight = true });
            
            // Add default moon
            moons.Add(new CelestialBodyIdentity { name = "Luna", createDirectionalLight = true });
        }

        #endregion
        
            
        /// <summary>
        /// Initialize seasonal configurations based on current celestial bodies and season names
        /// </summary>
        public void InitializeSeasonalConfigs()
        {
            // Clear existing
            seasonConfigs.Clear();
    
            for (int i = 0; i < numberOfSeasons; i++)
            {
                SeasonConfig seasonConfig = new SeasonConfig
                {
                    seasonName = i < seasonNames.Length ? seasonNames[i] : $"Season {i + 1}"
                };
        
                // Create configs for each sun
                foreach (var sun in suns)
                {
                    seasonConfig.sunConfigs.Add(new SeasonalBodyConfig
                    {
                        bodyName = sun.name,
                        orbitalAngle = 23.5f,
                        baseElevation = 180f,
                        orbitalPeriod = 1f,
                        phaseOffset = 0f,
                        lightIntensity = 100000f,
                        lightTemperature = 6500f,
                        lightColor = Color.white
                    });
                }
        
                // Create configs for each moon
                foreach (var moon in moons)
                {
                    seasonConfig.moonConfigs.Add(new SeasonalBodyConfig
                    {
                        bodyName = moon.name,
                        orbitalAngle = 23.5f,
                        baseElevation = 180f,
                        orbitalPeriod = 29.5f,
                        phaseOffset = 0f,
                        lightIntensity = 500f,
                        lightTemperature = 4000f,
                        lightColor = new Color(0.8f, 0.8f, 1f)
                    });
                }
        
                seasonConfigs.Add(seasonConfig);
            }
        }

        #region Validation

        public List<string> Validate()
        {
            List<string> errors = new List<string>();

            // Validate time settings
            if (timeScale <= 0f)
                errors.Add("Time scale must be greater than 0");

            if (hoursPerDay <= 0)
                errors.Add("Hours per day must be greater than 0");

            if (minutesPerHour <= 0)
                errors.Add("Minutes per hour must be greater than 0");

            if (secondsPerMinute <= 0)
                errors.Add("Seconds per minute must be greater than 0");

            // Validate year structure
            if (totalDaysInYear <= 0)
                errors.Add("Total days in year must be greater than 0");

            if (monthsPerYear <= 0)
                errors.Add("Months per year must be greater than 0");

            if (daysPerMonth <= 0)
                errors.Add("Days per month must be greater than 0");

            // Validate calendar alignment
            int calculatedYearLength = monthsPerYear * daysPerMonth;
            if (calculatedYearLength != totalDaysInYear)
            {
                errors.Add($"Calendar mismatch: {monthsPerYear} months × {daysPerMonth} days = {calculatedYearLength} days, but year is {totalDaysInYear} days");
            }

            // Validate month names
            if (monthNames == null || monthNames.Length != monthsPerYear)
                errors.Add($"Month names array must have {monthsPerYear} entries");

            // Validate seasonal data
            if (createSeasonalData)
            {
                if (numberOfSeasons < 2 || numberOfSeasons > 12)
                    errors.Add("Number of seasons must be between 2 and 12");

                if (seasonNames == null || seasonNames.Length != numberOfSeasons)
                    errors.Add($"Season names array must have {numberOfSeasons} entries");

                if (seasonNames != null)
                {
                    for (int i = 0; i < seasonNames.Length; i++)
                    {
                        if (string.IsNullOrWhiteSpace(seasonNames[i]))
                            errors.Add($"Season {i + 1} has no name");
                    }
                }
            }

            // Validate celestial bodies
            if (suns.Count == 0)
                errors.Add("At least one sun is required");

            // Check for duplicate celestial body names
            HashSet<string> celestialNames = new HashSet<string>();
            foreach (var sun in suns)
            {
                if (string.IsNullOrWhiteSpace(sun.name))
                {
                    errors.Add("A sun has no name");
                    continue;
                }
                if (!celestialNames.Add(sun.name))
                    errors.Add($"Duplicate celestial body name: {sun.name}");
            }
            foreach (var moon in moons)
            {
                if (string.IsNullOrWhiteSpace(moon.name))
                {
                    errors.Add("A moon has no name");
                    continue;
                }
                if (!celestialNames.Add(moon.name))
                    errors.Add($"Duplicate celestial body name: {moon.name}");
            }

            // Validate paths
            if (string.IsNullOrWhiteSpace(dataFolderPath))
                errors.Add("Data folder path cannot be empty");

            if (string.IsNullOrWhiteSpace(prefabFolderPath))
                errors.Add("Prefab folder path cannot be empty");

            return errors;
        }

        #endregion
    }

    /// <summary>
    /// Simplified celestial body identity - just defines that a body exists.
    /// Seasonal behavior is defined in SeasonalData assets.
    /// </summary>
    [System.Serializable]
    public class CelestialBodyIdentity
    {
        [Tooltip("Name of this celestial body (must be unique, used to match across seasons)")]
        public string name = "Celestial Body";
        
        [Tooltip("Should this body have a directional light component?")]
        public bool createDirectionalLight = true;
    }

    /// <summary>
    /// Preset calendar configurations for quick setup
    /// </summary>
    public static class CalendarPresets
    {
        public static void ApplyEarthPreset(SetupConfig config)
        {
            config.timeScale = 60f;
            config.hoursPerDay = 24;
            config.minutesPerHour = 60;
            config.secondsPerMinute = 60;
            
            config.totalDaysInYear = 360;
            config.monthsPerYear = 12;
            config.daysPerMonth = 30;
            
            config.numberOfSeasons = 4;
            config.seasonNames = new string[] { "Spring", "Summer", "Autumn", "Winter" };
            config.seasonTransitionDays = 10;
            
            config.monthNames = new string[]
            {
                "January", "February", "March", "April",
                "May", "June", "July", "August",
                "September", "October", "November", "December"
            };

            // Reset celestial bodies to Earth defaults
            config.suns.Clear();
            config.suns.Add(new CelestialBodyIdentity { name = "Sun", createDirectionalLight = true });
            
            config.moons.Clear();
            config.moons.Add(new CelestialBodyIdentity { name = "Moon", createDirectionalLight = true });
        }
        
        public static void ApplySolPreset(SetupConfig config)
        {
            config.timeScale = 10f;
            config.hoursPerDay = 20;
            config.minutesPerHour = 60;
            config.secondsPerMinute = 60;
            
            config.totalDaysInYear = 832;
            config.monthsPerYear = 8;
            config.daysPerMonth = 104;
            
            config.numberOfSeasons = 4;
            config.seasonNames = new string[] { "Lansomr", "Svik", "Evinotr", "Gro" };
            config.seasonTransitionDays = 20;
            
            config.monthNames = new string[]
            {
                "Glavyr", "Tharven", "Solmyr", "Aethon",
                "Lumis", "Verdis", "Harvyx", "Frosten"
            };

            // Reset celestial bodies to Sol defaults
            config.suns.Clear();
            config.suns.Add(new CelestialBodyIdentity { name = "Sol", createDirectionalLight = true });
            
            config.moons.Clear();
            config.moons.Add(new CelestialBodyIdentity { name = "Luna", createDirectionalLight = true });
        }
        
        public static void ApplyMarsPreset(SetupConfig config)
        {
            config.timeScale = 59f;
            config.hoursPerDay = 24;
            config.minutesPerHour = 60;
            config.secondsPerMinute = 60;
            
            config.totalDaysInYear = 672; // Adjusted to divide evenly
            config.monthsPerYear = 24;
            config.daysPerMonth = 28;
            
            config.numberOfSeasons = 4;
            config.seasonNames = new string[] { "Spring", "Summer", "Autumn", "Winter" };
            config.seasonTransitionDays = 15;

            // Reset to single sun, two moons (Phobos and Deimos)
            config.suns.Clear();
            config.suns.Add(new CelestialBodyIdentity { name = "Sun", createDirectionalLight = true });
            
            config.moons.Clear();
            config.moons.Add(new CelestialBodyIdentity { name = "Phobos", createDirectionalLight = true });
            config.moons.Add(new CelestialBodyIdentity { name = "Deimos", createDirectionalLight = false });
        }
        
        public static void ApplyAlienPreset(SetupConfig config)
        {
            config.timeScale = 100f;
            config.hoursPerDay = 10;
            config.minutesPerHour = 100;
            config.secondsPerMinute = 100;
            
            config.totalDaysInYear = 500;
            config.monthsPerYear = 10;
            config.daysPerMonth = 50;
            
            config.numberOfSeasons = 5;
            config.seasonNames = new string[] 
            { 
                "First Bloom", "High Sun", "Golden Harvest",
                "Frost Descent", "Deep Cold"
            };
            config.seasonTransitionDays = 10;

            // Binary star system with three moons
            config.suns.Clear();
            config.suns.Add(new CelestialBodyIdentity { name = "Primary Star", createDirectionalLight = true });
            config.suns.Add(new CelestialBodyIdentity { name = "Secondary Star", createDirectionalLight = true });
            
            config.moons.Clear();
            config.moons.Add(new CelestialBodyIdentity { name = "Moon Alpha", createDirectionalLight = true });
            config.moons.Add(new CelestialBodyIdentity { name = "Moon Beta", createDirectionalLight = true });
            config.moons.Add(new CelestialBodyIdentity { name = "Moon Gamma", createDirectionalLight = false });
        }
    }
}
