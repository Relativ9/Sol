using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Sol.Editor
{
    /// <summary>
    /// Utility class for generating Sol system assets and scene objects.
    /// Handles creation of WorldTimeData, SeasonalData, and scene setup.
    /// </summary>
    public static class SolSetupUtilities
    {
        #region Public Methods
        
        /// <summary>
        /// Main entry point for generating the complete Sol system.
        /// </summary>
        public static void GenerateSolSystem(SetupConfig config)
        {
            LogStep("Starting Sol system generation...");

            try
            {
                // STEP 1: Validate
                LogStep("Validating configuration...");
                var validationErrors = config.Validate();
                if (validationErrors.Count > 0)
                {
                    LogStep("Validation failed", false);
                    // ... show dialog
                    return;
                }
                LogStep("Configuration validated");

                // STEP 2: Folders
                LogStep("Creating folders...");
                EnsureFoldersExist(config);
                LogStep("Folders ready");

                // ... etc for each step
            }
            catch (System.Exception e)
            {
                LogStep($"Generation failed: {e.Message}", false);
                throw;
            }
        }


        #endregion

        #region Folder Management

        private static void EnsureFoldersExist(SetupConfig config)
        {
            EnsureFolderExists(config.dataFolderPath);
            EnsureFolderExists(config.prefabFolderPath);
            
            // Create subfolders for organization
            EnsureFolderExists(Path.Combine(config.dataFolderPath, "Seasons"));
        }

        private static void EnsureFolderExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentFolder = Path.GetDirectoryName(path);
                string folderName = Path.GetFileName(path);

                if (!string.IsNullOrEmpty(parentFolder) && !AssetDatabase.IsValidFolder(parentFolder))
                {
                    EnsureFolderExists(parentFolder);
                }

                AssetDatabase.CreateFolder(parentFolder, folderName);
                Debug.Log($"[SolSetup] Created folder: {path}");
            }

            // Save WorldTimeData with linked seasonal data
            EditorUtility.SetDirty(worldTimeData);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Sol Setup] Created and linked {seasonalDataAssets.Count} SeasonalData assets");

            return seasonalDataAssets.ToArray();
        }

        #endregion

        #region WorldTimeData Generation

        private static WorldTimeData GenerateWorldTimeData(SetupConfig config)
        {
            Debug.Log("[SolSetup] Generating WorldTimeData...");

            string assetPath = Path.Combine(config.dataFolderPath, "WorldTimeData.asset");

            // Check if asset already exists
            WorldTimeData existingData = AssetDatabase.LoadAssetAtPath<WorldTimeData>(assetPath);
            if (existingData != null)
            {
                if (!EditorUtility.DisplayDialog(
                    "WorldTimeData Exists",
                    $"WorldTimeData already exists at:\n{assetPath}\n\nOverwrite it?",
                    "Overwrite",
                    "Keep Existing"))
                {
                    Debug.Log("[SolSetup] Using existing WorldTimeData");
                    return existingData;
                }
            }

            // Create new WorldTimeData
            WorldTimeData worldTimeData = ScriptableObject.CreateInstance<WorldTimeData>();

            // Configure time settings using SerializedObject for proper access
            SerializedObject so = new SerializedObject(worldTimeData);
            
            so.FindProperty("timeScale").floatValue = config.timeScale;
            so.FindProperty("hoursPerDay").intValue = config.hoursPerDay;
            so.FindProperty("minutesPerHour").intValue = config.minutesPerHour;
            so.FindProperty("secondsPerMinute").intValue = config.secondsPerMinute;
            so.FindProperty("totalDaysInYear").intValue = config.totalDaysInYear;
            so.FindProperty("daysPerMonth").intValue = config.daysPerMonth;
            so.FindProperty("seasonTransitionDays").intValue = config.seasonTransitionDays;

            // Configure months
            SerializedProperty monthsProperty = so.FindProperty("months");
            monthsProperty.ClearArray();

            for (int i = 0; i < config.monthNames.Length; i++)
            {
                monthsProperty.InsertArrayElementAtIndex(i);
                SerializedProperty monthElement = monthsProperty.GetArrayElementAtIndex(i);
                
                monthElement.FindPropertyRelative("name").stringValue = config.monthNames[i];
                monthElement.FindPropertyRelative("index").intValue = i;
                monthElement.FindPropertyRelative("monthColor").colorValue = Color.white;
            }

            so.ApplyModifiedProperties();

            // Save asset
            if (existingData != null)
            {
                EditorUtility.CopySerialized(worldTimeData, existingData);
                EditorUtility.SetDirty(existingData);
                Debug.Log($"[SolSetup] ✓ Updated WorldTimeData at {assetPath}");
                return existingData;
            }
            else
            {
                AssetDatabase.CreateAsset(worldTimeData, assetPath);
                Debug.Log($"[SolSetup] ✓ Created WorldTimeData at {assetPath}");
                return worldTimeData;
            }
        }

            if (onSeasonChangedProp != null)
            {
                GameEvent seasonEvent = FindGameEventByName("OnSeasonChanged");
                if (seasonEvent != null)
                {
                    onSeasonChangedProp.objectReferenceValue = seasonEvent;
                    Debug.Log($"[Sol Setup] Assigned OnSeasonChanged event: {AssetDatabase.GetAssetPath(seasonEvent)}");
                }
                else
                {
                    Debug.LogWarning("[Sol Setup] Could not find 'OnSeasonChanged' GameEvent asset. Assign manually.");
                }
            }

        #region SeasonalData Generation

        private static List<SeasonalData> GenerateSeasonalData(SetupConfig config)
        {
            Debug.Log($"[SolSetup] Generating {config.numberOfSeasons} SeasonalData assets...");

            List<SeasonalData> seasonalDataList = new List<SeasonalData>();
            string seasonFolder = Path.Combine(config.dataFolderPath, "Seasons");

            for (int i = 0; i < config.numberOfSeasons; i++)
            {
                string seasonName = config.seasonNames[i];
                string assetPath = Path.Combine(seasonFolder, $"{seasonName}Data.asset");

                // Check if asset already exists
                SeasonalData existingData = AssetDatabase.LoadAssetAtPath<SeasonalData>(assetPath);
                SeasonalData seasonalData;

                if (existingData != null)
                {
                    Debug.Log($"[SolSetup] Updating existing SeasonalData: {seasonName}");
                    seasonalData = existingData;
                }
                else
                {
                    Debug.Log($"[SolSetup] Creating new SeasonalData: {seasonName}");
                    seasonalData = ScriptableObject.CreateInstance<SeasonalData>();
                    AssetDatabase.CreateAsset(seasonalData, assetPath);
                }

                // Configure season
                ConfigureSeasonalData(seasonalData, config, i);

                EditorUtility.SetDirty(seasonalData);
                seasonalDataList.Add(seasonalData);
            }

            return seasonalDataList;
        }

        private static void ConfigureSeasonalData(SeasonalData seasonalData, SetupConfig config, int seasonIndex)
        {
            SerializedObject so = new SerializedObject(seasonalData);
            
            // Set season name
            so.FindProperty("seasonName").stringValue = config.seasonNames[seasonIndex];

            // Calculate seasonal variations (example implementation)
            float seasonProgress = (float)seasonIndex / config.numberOfSeasons;
            
            // Configure atmospheric settings with seasonal variation
            so.FindProperty("dayAmbientColor").colorValue = Color.Lerp(
                new Color(0.5f, 0.5f, 0.5f),
                new Color(0.6f, 0.55f, 0.5f),
                Mathf.Sin(seasonProgress * Mathf.PI)
            );

            so.FindProperty("nightAmbientColor").colorValue = Color.Lerp(
                new Color(0.1f, 0.1f, 0.2f),
                new Color(0.05f, 0.05f, 0.15f),
                Mathf.Sin(seasonProgress * Mathf.PI)
            );

            so.FindProperty("skyExposure").floatValue = 1.0f + Mathf.Sin(seasonProgress * Mathf.PI) * 0.2f;
            so.FindProperty("fogDensity").floatValue = 0.01f + Mathf.Sin(seasonProgress * Mathf.PI) * 0.005f;
            so.FindProperty("fogColor").colorValue = Color.gray;

            // Get serialized lists for celestial bodies
            SerializedProperty starsProperty = so.FindProperty("stars");
            SerializedProperty moonsProperty = so.FindProperty("moons");

            // Clear existing
            starsProperty.ClearArray();
            moonsProperty.ClearArray();

            // Add configured suns with seasonal orbital variations
            for (int i = 0; i < config.suns.Count; i++)
            {
                starsProperty.InsertArrayElementAtIndex(i);
                ConfigureSeasonalSun(starsProperty.GetArrayElementAtIndex(i), config.suns[i], config, seasonIndex);
            }

            // Add configured moons with seasonal orbital variations
            for (int i = 0; i < config.moons.Count; i++)
            {
                moonsProperty.InsertArrayElementAtIndex(i);
                ConfigureSeasonalMoon(moonsProperty.GetArrayElementAtIndex(i), config.moons[i], config, seasonIndex);
            }

            so.ApplyModifiedProperties();
        }

        private static void ConfigureSeasonalSun(
            SerializedProperty sunProperty,
            CelestialBodyIdentity identity, 
            SetupConfig config, 
            int seasonIndex)
        {
            // Calculate seasonal orbital variation
            float seasonProgress = (float)seasonIndex / config.numberOfSeasons;
            
            // Vary orbital angle by season (simulates axial tilt effect)
            float baseOrbitalAngle = 23.5f;
            float orbitalAngleVariation = Mathf.Sin(seasonProgress * 2f * Mathf.PI) * 23.5f;
            float seasonalOrbitalAngle = baseOrbitalAngle + orbitalAngleVariation;

            // Vary base elevation slightly (creates different sunrise/sunset positions)
            float baseElevation = 180f + Mathf.Sin(seasonProgress * 2f * Mathf.PI) * 5f;

            // Configure basic properties
            sunProperty.FindPropertyRelative("name").stringValue = identity.name;
            sunProperty.FindPropertyRelative("active").boolValue = true;
            sunProperty.FindPropertyRelative("overrideOrbitalAngle").boolValue = false;
            
            // Orbital settings
            sunProperty.FindPropertyRelative("yAxisEnabled").boolValue = true;
            sunProperty.FindPropertyRelative("yAxisSpeed").floatValue = 1f;
            sunProperty.FindPropertyRelative("yAxisOverrideSpeed").boolValue = false;
            sunProperty.FindPropertyRelative("orbitalAngle").floatValue = seasonalOrbitalAngle;
            sunProperty.FindPropertyRelative("baseElevation").floatValue = baseElevation;
            
            // Sun-specific
            sunProperty.FindPropertyRelative("orbitalPeriod").floatValue = 1f;
            sunProperty.FindPropertyRelative("phaseOffset").floatValue = 0f;

            // Light configuration
            sunProperty.FindPropertyRelative("hasDirectionalLight").boolValue = identity.createDirectionalLight;
            sunProperty.FindPropertyRelative("useColorTemperature").boolValue = true;
            sunProperty.FindPropertyRelative("lightTemperature").floatValue = 6500f;
            sunProperty.FindPropertyRelative("lightColor").colorValue = Color.white;
            sunProperty.FindPropertyRelative("lightIntensity").floatValue = 100000f;
            sunProperty.FindPropertyRelative("castShadows").boolValue = true;

            // Visual appearance
            sunProperty.FindPropertyRelative("angularDiameter").floatValue = 0.53f;
            sunProperty.FindPropertyRelative("surfaceColor").colorValue = new Color(1f, 0.95f, 0.8f, 1f);
            sunProperty.FindPropertyRelative("flareSize").floatValue = 1f;
            sunProperty.FindPropertyRelative("flareFalloff").floatValue = 10f;
            sunProperty.FindPropertyRelative("flareBrightness").floatValue = 2f;
        }

        private static void ConfigureSeasonalMoon(
            SerializedProperty moonProperty,
            CelestialBodyIdentity identity, 
            SetupConfig config, 
            int seasonIndex)
        {
            // Calculate seasonal orbital variation
            float seasonProgress = (float)seasonIndex / config.numberOfSeasons;
            
            // Moons can have different orbital variations
            float baseOrbitalAngle = 23.5f;
            float orbitalAngleVariation = Mathf.Sin(seasonProgress * 2f * Mathf.PI) * 15f;
            float seasonalOrbitalAngle = baseOrbitalAngle + orbitalAngleVariation;

            float baseElevation = 180f + Mathf.Sin(seasonProgress * 2f * Mathf.PI) * 10f;

            // Configure basic properties
            moonProperty.FindPropertyRelative("name").stringValue = identity.name;
            moonProperty.FindPropertyRelative("active").boolValue = true;
            moonProperty.FindPropertyRelative("overrideOrbitalAngle").boolValue = false;
            
            // Orbital settings
            moonProperty.FindPropertyRelative("yAxisEnabled").boolValue = true;
            moonProperty.FindPropertyRelative("yAxisSpeed").floatValue = 1f;
            moonProperty.FindPropertyRelative("yAxisOverrideSpeed").boolValue = false;
            moonProperty.FindPropertyRelative("orbitalAngle").floatValue = seasonalOrbitalAngle;
            moonProperty.FindPropertyRelative("baseElevation").floatValue = baseElevation;
            
            // Moon-specific
            moonProperty.FindPropertyRelative("orbitalPeriod").floatValue = 29.5f;
            moonProperty.FindPropertyRelative("phaseOffset").floatValue = seasonProgress * 360f; // Vary by season

            // Light configuration
            moonProperty.FindPropertyRelative("hasDirectionalLight").boolValue = identity.createDirectionalLight;
            moonProperty.FindPropertyRelative("useColorTemperature").boolValue = true;
            moonProperty.FindPropertyRelative("lightTemperature").floatValue = 4000f;
            moonProperty.FindPropertyRelative("lightColor").colorValue = new Color(0.8f, 0.8f, 1f);
            moonProperty.FindPropertyRelative("lightIntensity").floatValue = 500f;
            moonProperty.FindPropertyRelative("castShadows").boolValue = false;

            // Visual appearance
            moonProperty.FindPropertyRelative("angularDiameter").floatValue = 0.52f;
            moonProperty.FindPropertyRelative("surfaceColor").colorValue = new Color(0.7f, 0.7f, 0.75f, 1f);
            moonProperty.FindPropertyRelative("flareSize").floatValue = 0.5f;
            moonProperty.FindPropertyRelative("flareFalloff").floatValue = 5f;
            moonProperty.FindPropertyRelative("flareBrightness").floatValue = 0.5f;
        }

        #endregion

        #region Season Linking

        private static void LinkSeasonsToWorldTime(WorldTimeData worldTimeData, List<SeasonalData> seasonalDataList, SetupConfig config)
        {
            Debug.Log($"[SolSetup] Linking {seasonalDataList.Count} seasons to WorldTimeData...");

            SerializedObject so = new SerializedObject(worldTimeData);
            SerializedProperty seasonsProperty = so.FindProperty("seasons");

            seasonsProperty.ClearArray();

            // Calculate days per season (evenly distributed)
            int daysPerSeason = config.totalDaysInYear / config.numberOfSeasons;
            int remainderDays = config.totalDaysInYear % config.numberOfSeasons;

            for (int i = 0; i < seasonalDataList.Count; i++)
            {
                seasonsProperty.InsertArrayElementAtIndex(i);
                SerializedProperty seasonElement = seasonsProperty.GetArrayElementAtIndex(i);
                
                // Calculate length for this season (distribute remainder days to first seasons)
                int seasonLength = daysPerSeason + (i < remainderDays ? 1 : 0);
                
                seasonElement.FindPropertyRelative("seasonName").stringValue = config.seasonNames[i];
                seasonElement.FindPropertyRelative("lengthInDays").intValue = seasonLength;
                seasonElement.FindPropertyRelative("seasonalData").objectReferenceValue = seasonalDataList[i];
                seasonElement.FindPropertyRelative("overrideAmbientColors").boolValue = false;
                seasonElement.FindPropertyRelative("seasonColor").colorValue = GetSeasonColor(i, config.numberOfSeasons);
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(worldTimeData);

            Debug.Log("[SolSetup] ✓ Seasons linked to WorldTimeData");
        }

        private static Color GetSeasonColor(int seasonIndex, int totalSeasons)
        {
            // Generate distinct colors for each season
            float hue = (float)seasonIndex / totalSeasons;
            return Color.HSVToRGB(hue, 0.6f, 0.9f);
        }
        
        /// <summary>
        /// Creates a directional light for a celestial body with HDRP configuration.
        /// </summary>
        private static Light CreateCelestialLight(CelestialBodyConfig config, bool enableShadows, bool isMoon, Transform parent)
        {
            GameObject lightObj = new GameObject($"{config.name}");
            lightObj.transform.SetParent(parent);
            lightObj.transform.localPosition = Vector3.zero;
            lightObj.transform.localRotation = Quaternion.identity;

            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            
            // Use white color - HDRP will handle temperature via HDAdditionalLightData
            light.color = Color.white;
            light.intensity = config.lightIntensity;
            light.shadows = enableShadows ? LightShadows.Soft : LightShadows.None;

        #region Scene Setup
        
        private static void CreateTimeManagerInScene(SetupConfig config, WorldTimeData worldTimeData)
        {
            Debug.Log("[SolSetup] Creating TimeManager in scene...");

            // Validate inputs
            if (config == null)
            {
                Debug.LogError("[SolSetup] Config is null!");
                return;
            }

            if (config.suns == null)
            {
                Debug.LogWarning("[SolSetup] Config.suns is null, initializing empty list");
                config.suns = new List<CelestialBodyIdentity>();
            }

            if (config.moons == null)
            {
                Debug.LogWarning("[SolSetup] Config.moons is null, initializing empty list");
                config.moons = new List<CelestialBodyIdentity>();
            }

            // Check if TimeManager already exists
            TimeManager existingManager = Object.FindObjectOfType<TimeManager>();
            if (existingManager != null)
            {
                bool updateExisting = EditorUtility.DisplayDialog(
                    "TimeManager Exists",
                    "A TimeManager already exists in the scene. Update its configuration?",
                    "Update",
                    "Skip"
                );

                if (!updateExisting)
                {
                    Debug.Log("[SolSetup] Skipped TimeManager creation");
                    return;
                }

                // Update existing manager
                if (worldTimeData != null)
                {
                    SerializedObject so = new SerializedObject(existingManager);
                    so.FindProperty("worldTimeData").objectReferenceValue = worldTimeData;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(existingManager);
                }

                Debug.Log("[SolSetup] ✓ Updated existing TimeManager");
                Selection.activeGameObject = existingManager.gameObject;
                return;
            }

            // Create new TimeManager
            GameObject timeManagerObj = new GameObject("TimeManager");
            TimeManager timeManager = timeManagerObj.AddComponent<TimeManager>();

            if (worldTimeData != null)
            {
                SerializedObject so = new SerializedObject(timeManager);
                so.FindProperty("worldTimeData").objectReferenceValue = worldTimeData;
                so.ApplyModifiedProperties();
            }
            else
            {
                Debug.LogWarning("[SolSetup] WorldTimeData is null, TimeManager created without data reference");
            }

            // Create celestial bodies parent
            GameObject celestialBodiesParent = new GameObject("CelestialBodies");
            celestialBodiesParent.transform.SetParent(timeManagerObj.transform);
            celestialBodiesParent.transform.localPosition = Vector3.zero;
            celestialBodiesParent.transform.localRotation = Quaternion.identity;

            // Create sun objects
            int sunCount = 0;
            foreach (var sunIdentity in config.suns)
            {
                if (sunIdentity == null)
                {
                    Debug.LogWarning($"[SolSetup] Null sun identity at index {sunCount}, skipping");
                    continue;
                }

                if (string.IsNullOrEmpty(sunIdentity.name))
                {
                    Debug.LogWarning($"[SolSetup] Sun at index {sunCount} has no name, skipping");
                    continue;
                }

                try
                {
                    CreateCelestialBodyObject(
                        sunIdentity.name, 
                        celestialBodiesParent.transform, 
                        true, 
                        sunIdentity.createDirectionalLight
                    );
                    sunCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SolSetup] Failed to create sun '{sunIdentity.name}': {e.Message}");
                }
            }

            // Create moon objects
            int moonCount = 0;
            foreach (var moonIdentity in config.moons)
            {
                if (moonIdentity == null)
                {
                    Debug.LogWarning($"[SolSetup] Null moon identity at index {moonCount}, skipping");
                    continue;
                }

                if (string.IsNullOrEmpty(moonIdentity.name))
                {
                    Debug.LogWarning($"[SolSetup] Moon at index {moonCount} has no name, skipping");
                    continue;
                }

                try
                {
                    CreateCelestialBodyObject(
                        moonIdentity.name, 
                        celestialBodiesParent.transform, 
                        false, 
                        moonIdentity.createDirectionalLight
                    );
                    moonCount++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SolSetup] Failed to create moon '{moonIdentity.name}': {e.Message}");
                }
            }

            Selection.activeGameObject = timeManagerObj;
            EditorUtility.SetDirty(timeManagerObj);

            Debug.Log($"[SolSetup] ✓ Created TimeManager with {sunCount} suns and {moonCount} moons");
        }
        
        /// <summary>
        /// Adds HDRP Volume component and assigns the SolDefaultSkyProfile.
        /// </summary>
        private static bool TryAddAndConfigureVolumeComponent(GameObject volumeObject)
        {
            Debug.Log("[Sol Setup] Configuring Volume component...");


        private static GameObject CreateCelestialBodyObject(string name, Transform parent, bool isSun, bool createLight)
        {
            GameObject celestialObj = new GameObject(name);
            celestialObj.transform.SetParent(parent);
            celestialObj.transform.localPosition = Vector3.zero;
            celestialObj.transform.localRotation = Quaternion.identity;

            // Add CelestialRotator component
            CelestialRotator rotator = celestialObj.AddComponent<CelestialRotator>();
            SerializedObject so = new SerializedObject(rotator);
            so.FindProperty("celestialBodyName").stringValue = name;
            so.FindProperty("isMoon").boolValue = !isSun;
            so.ApplyModifiedProperties();

            // Create directional light if requested
            if (createLight)
            {
                GameObject lightObj = new GameObject($"{name}_Light");
                lightObj.transform.SetParent(celestialObj.transform);
                lightObj.transform.localPosition = Vector3.zero;
                lightObj.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);

                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.shadows = isSun ? LightShadows.Soft : LightShadows.None;
                    
                if (isSun)
                {
                    light.intensity = 100000f; // Lux for sun
                    light.color = Color.white;
                }
                else
                {
                    light.intensity = 500f; // Lux for moon
                    light.color = new Color(0.8f, 0.8f, 1f);
                }

    #if UNITY_2019_1_OR_NEWER && USING_HDRP
                    // Add HD Additional Light Data for HDRP
                    var hdLight = lightObj.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
                    hdLight.EnableColorTemperature(true);
                    hdLight.SetColor(Color.white, isSun ? 6500f : 4000f);
    #endif
            }

            Debug.Log($"[SolSetup] Created celestial body: {name}");
            return celestialObj;
        }

        #endregion

        #region Sky and Fog Setup

        private static void SetupSkyAndFog(SetupConfig config)
        {
            Debug.Log("[SolSetup] Setting up Sky and Fog...");

#if UNITY_2019_1_OR_NEWER && USING_HDRP
            // HDRP implementation
            SetupHDRPSkyAndFog(config);
#else
            EditorUtility.DisplayDialog(
                "Sky and Fog",
                "Sky and Fog setup currently requires HDRP.\n\nFor Built-in RP, configure manually using Lighting settings.",
                "OK"
            );
#endif
        }

#if UNITY_2019_1_OR_NEWER && USING_HDRP
        private static void SetupHDRPSkyAndFog(SetupConfig config)
        {
            // Check for existing sky volume
            var existingVolume = Object.FindObjectOfType<UnityEngine.Rendering.Volume>();
            
            if (existingVolume != null)
            {
                Debug.Log("[SolSetup] Found existing Volume, skipping sky setup");
                return;
            }

            // Create sky volume
            GameObject volumeObj = new GameObject("Sky and Fog Volume");
            var volume = volumeObj.AddComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;
            volume.priority = 0;

            // Create or load volume profile
            string profilePath = string.IsNullOrEmpty(config.hdrpProfilePath) 
                ? Path.Combine(config.dataFolderPath, "SkyAndFogProfile.asset")
                : config.hdrpProfilePath;

            var profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(profilePath);
            
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
                
                Debug.Log($"[SolSetup] Created HDRP Volume Profile at {profilePath}");
            }

            volume.sharedProfile = profile;
            EditorUtility.SetDirty(volumeObj);

            Debug.Log("[SolSetup] ✓ Created Sky and Fog Volume");
        }
#endif

        #endregion

        #region HelperMethods 
        
        private static void LogStep(string step, bool success = true)
        {
            if (success)
                Debug.Log($"[SolSetup] ✓ {step}");
            else
                Debug.LogError($"[SolSetup] ✗ {step}");
        }
        
        #endregion
        
        #region Demo Scene

        private static void CreateDemoScene(SetupConfig config)
        {
            Debug.Log("[SolSetup] Creating demo scene...");

            EditorUtility.DisplayDialog(
                "Demo Scene",
                "Demo scene creation is not yet implemented.\n\nYour Sol system is ready to use in the current scene!",
                "OK"
            );

            // TODO: Implement demo scene creation
        }

        #endregion
    }
}
