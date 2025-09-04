using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Sol.Editor
{
    /// <summary>
    /// Utility methods for Sol setup operations
    /// </summary>
    public static class SolSetupUtilities
    {
        /// <summary>
        /// Create folder structure if it doesn't exist
        /// </summary>
        public static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string[] pathParts = folderPath.Split('/');
            string currentPath = pathParts[0];

            for (int i = 1; i < pathParts.Length; i++)
            {
                string newPath = currentPath + "/" + pathParts[i];
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    AssetDatabase.CreateFolder(currentPath, pathParts[i]);
                }
                currentPath = newPath;
            }
        }

        /// <summary>
        /// Create default seasonal data with sensible configurations
        /// </summary>
        public static SeasonalData CreateDefaultSeasonalData(string[] seasonNames, bool includeSun, bool includeMoon, string sunName, string moonName)
        {
            SeasonalData seasonalData = ScriptableObject.CreateInstance<SeasonalData>();

            // Configure seasons (this will depend on your actual SeasonalData structure)
            // Example implementation - adjust based on your actual data structure:
            /*
            seasonalData.seasons = new Season[seasonNames.Length];
            for (int i = 0; i < seasonNames.Length; i++)
            {
                seasonalData.seasons[i] = new Season
                {
                    name = seasonNames[i],
                    orbitalAngle = Mathf.Lerp(-23.5f, 23.5f, (float)i / (seasonNames.Length - 1)),
                    // Add other default properties
                };
            }

            List<CelestialBody> celestialBodies = new List<CelestialBody>();

            if (includeSun)
            {
                celestialBodies.Add(new CelestialBody
                {
                    name = sunName,
                    active = true,
                    yAxisEnabled = true,
                    yAxisSpeed = 1.0f,
                    baseElevation = 0f,
                    phaseOffset = 0f,
                    orbitalPeriod = 1f
                });
            }

            if (includeMoon)
            {
                celestialBodies.Add(new CelestialBody
                {
                    name = moonName,
                    active = true,
                    yAxisEnabled = true,
                    yAxisSpeed = 1.0f,
                    baseElevation = 0f,
                    phaseOffset = 0f,
                    orbitalPeriod = 29.5f // Realistic lunar cycle
                });
            }

            seasonalData.celestialBodies = celestialBodies.ToArray();
            */

            return seasonalData;
        }

        /// <summary>
        /// Create default WorldTimeData with sensible configurations
        /// </summary>
        public static WorldTimeData CreateDefaultWorldTimeData()
        {
            WorldTimeData worldTimeData = ScriptableObject.CreateInstance<WorldTimeData>();

            // Configure with sensible defaults - adjust based on your WorldTimeData structure
            /*
            worldTimeData.dayLengthInSeconds = 300f; // 5 minute days
            worldTimeData.timeScale = 1f;
            worldTimeData.startHour = 6f; // Start at dawn
            worldTimeData.pauseOnStart = false;
            */

            return worldTimeData;
        }

        /// <summary>
        /// Validate scene setup and provide recommendations
        /// </summary>
        public static string ValidateSceneSetup()
        {
            var issues = new System.Text.StringBuilder();

            // Check for TimeManager
            if (Object.FindObjectOfType<TimeManager>() == null)
            {
                issues.AppendLine("• No TimeManager found in scene");
            }

            // Check for directional light
            var lights = Object.FindObjectsOfType<Light>();
            bool hasDirectionalLight = false;
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    hasDirectionalLight = true;
                    break;
                }
            }

            if (!hasDirectionalLight)
            {
                issues.AppendLine("• No directional light found for sun simulation");
            }

            // Check for main camera
            if (Camera.main == null)
            {
                issues.AppendLine("• No main camera found in scene");
            }

            // Check for WorldTimeData asset
            var worldTimeDataAssets = AssetDatabase.FindAssets("t:WorldTimeData");
            if (worldTimeDataAssets.Length == 0)
            {
                issues.AppendLine("• No WorldTimeData asset found in project");
            }

            // Check for SeasonalData asset
            var seasonalDataAssets = AssetDatabase.FindAssets("t:SeasonalData");
            if (seasonalDataAssets.Length == 0)
            {
                issues.AppendLine("• No SeasonalData asset found in project");
            }

            return issues.Length > 0 ? issues.ToString() : "Scene setup looks good!";
        }

        /// <summary>
        /// Get recommended settings based on project type
        /// </summary>
        public static SolSetupWizard.SetupConfig GetRecommendedConfig(ProjectType projectType)
        {
            var config = new SolSetupWizard.SetupConfig();

            switch (projectType)
            {
                case ProjectType.Minimal:
                    config.createTimeManager = true;
                    config.createWorldTimeData = true;
                    config.createSeasonalData = true;
                    config.createSun = true;
                    config.createMoon = false;
                    config.createDirectionalLight = false;
                    config.createDemoScene = false;
                    break;

                case ProjectType.Standard:
                    config.createTimeManager = true;
                    config.createWorldTimeData = true;
                    config.createSeasonalData = true;
                    config.createSun = true;
                    config.createMoon = true;
                    config.createDirectionalLight = true;
                    config.createDemoScene = false;
                    break;

                case ProjectType.Complete:
                    config.createTimeManager = true;
                    config.createWorldTimeData = true;
                    config.createSeasonalData = true;
                    config.createSun = true;
                    config.createMoon = true;
                    config.createDirectionalLight = true;
                    config.createDemoScene = true;
                    break;
            }

            return config;
        }

        public enum ProjectType
        {
            Minimal,
            Standard,
            Complete
        }
    }
}