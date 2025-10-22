using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Sol.Editor
{
    public static class SolSetupUtilities
    {
        #region Main Setup Method

        public static void PerformCompleteSetup(SolSetupWizard.SetupConfig config, System.Action<string> statusCallback = null)
        {
            WorldTimeData worldTimeData = null;

            if (config.createWorldTimeData)
            {
                statusCallback?.Invoke("Creating WorldTimeData with realistic values...");
                worldTimeData = CreateWorldTimeDataWithRealisticValues(config);
            }

            if (config.createSeasonalData)
            {
                statusCallback?.Invoke("Creating seasonal data and populating lists...");
                CreateSeasonalDataAndPopulateLists(config, worldTimeData);
            }

            if (config.createTimeManager)
            {
                statusCallback?.Invoke("Creating TimeManager...");
                CreateTimeManagerWithData(config, worldTimeData);
            }

            // Create all celestial lights
            statusCallback?.Invoke("Creating celestial body lights...");
            CreateAllCelestialLights(config);

            if (config.createSkyAndFog)
            {
                statusCallback?.Invoke("Creating Sky and Fog Volume...");
                CreateSkyAndFogVolumeWithProfile(config);
            }

            if (config.createDemoScene)
            {
                statusCallback?.Invoke("Adding demo content to scene...");
                CreateDemoSceneWithContent(config);
            }

            statusCallback?.Invoke("Finalizing setup...");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #endregion

        #region Enhanced Celestial Light Creation
        
        /// <summary>
        /// Create all celestial body lights (suns and moons) with proper shadow management
        /// </summary>
        public static void CreateAllCelestialLights(SolSetupWizard.SetupConfig config)
        {
            // Create sun lights with shadow management
            for (int i = 0; i < config.suns.Count; i++)
            {
                var sunConfig = config.suns[i];
                if (sunConfig.createDirectionalLight)
                {
                    // Only first sun casts shadows, and only if enabled in config
                    bool shouldCastShadows = (i == 0) && sunConfig.castShadows;
                    CreateSunDirectionalLight(sunConfig, shouldCastShadows);
                }
            }

            // Create moon lights using prefab system with reflection mode
            foreach (var moonConfig in config.moons)
            {
                if (moonConfig.createDirectionalLight)
                {
                    CreateMoonDirectionalLightFromPrefab(moonConfig, config);
                }
            }
        }
        
        /// <summary>
        /// Create sun directional light with temperature and shadow control
        /// </summary>
        public static void CreateSunDirectionalLight(SolSetupWizard.CelestialBodyConfig sunConfig, bool shouldCastShadows)
        {
            GameObject sunLightGO = new GameObject($"{sunConfig.name} Light");
            Light sunLight = sunLightGO.AddComponent<Light>();
    
            // Configure sun light settings
            sunLight.type = LightType.Directional;
            sunLight.intensity = sunConfig.lightIntensity;
            sunLight.shadows = shouldCastShadows ? LightShadows.Soft : LightShadows.None;
    
#if UNITY_RENDER_PIPELINES_HIGH_DEFINITION
    // Set temperature for HDRP
    var hdAdditionalLightData = sunLightGO.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
    if (hdAdditionalLightData == null)
    {
        hdAdditionalLightData = sunLightGO.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
    }
    
    hdAdditionalLightData.useColorTemperature = true;
    hdAdditionalLightData.colorTemperature = sunConfig.lightTemperature;
    hdAdditionalLightData.lightUnit = UnityEngine.Rendering.HighDefinition.LightUnit.Lux;
#endif

            // Add and configure CelestialRotator
            CelestialRotator sunRotator = sunLightGO.AddComponent<CelestialRotator>();
            ConfigureCelestialRotator(sunRotator, sunConfig, false);

            string shadowInfo = shouldCastShadows ? "with shadows" : "without shadows";
            Debug.Log($"[Sol Setup] Sun directional light created: {sunConfig.name} {shadowInfo}");
        }

        /// <summary>
        /// Create moon directional light using the prefab system with sun reflection
        /// </summary>
        public static void CreateMoonDirectionalLightFromPrefab(SolSetupWizard.MoonConfig moonConfig, SolSetupWizard.SetupConfig config)
        {
                // Try to find the directional moon light prefab
    string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab DirectionalMoonLight");
    GameObject moonPrefab = null;

    if (prefabGuids.Length > 0)
    {
        string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[0]);
        moonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }

    GameObject moonLightGO;
    
    if (moonPrefab != null)
    {
        // Instantiate from prefab
        moonLightGO = PrefabUtility.InstantiatePrefab(moonPrefab) as GameObject;
        moonLightGO.name = $"{moonConfig.name} Light";
        Debug.Log($"[Sol Setup] Moon light created from prefab: {moonConfig.name}");
    }
    else
    {
        // Create manually if prefab not found
        moonLightGO = new GameObject($"{moonConfig.name} Light");
        Light moonLight = moonLightGO.AddComponent<Light>();
        
        moonLight.type = LightType.Directional;
        moonLight.intensity = moonConfig.lightIntensity;
        moonLight.shadows = LightShadows.None; // Moons don't cast shadows
        
        Debug.Log($"[Sol Setup] Moon light created manually (prefab not found): {moonConfig.name}");
    }

    // Configure moon light settings
    Light light = moonLightGO.GetComponent<Light>();
    if (light != null)
    {
        light.intensity = moonConfig.lightIntensity;
        light.shadows = LightShadows.None; // Moons don't cast shadows
        
        #if UNITY_RENDER_PIPELINES_HIGH_DEFINITION
        // Configure HDRP moon light with reflection mode
        var hdAdditionalLightData = moonLightGO.GetComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
        if (hdAdditionalLightData == null)
        {
            hdAdditionalLightData = moonLightGO.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalLightData>();
        }
        
        // Set moon to reflection mode instead of emission
        hdAdditionalLightData.useColorTemperature = true;
        hdAdditionalLightData.colorTemperature = moonConfig.lightTemperature;
        hdAdditionalLightData.lightUnit = UnityEngine.Rendering.HighDefinition.LightUnit.Lux;
        
        // Try to set reflection mode if the property exists
        var reflectionModeProperty = typeof(UnityEngine.Rendering.HighDefinition.HDAdditionalLightData)
            .GetField("reflectionMode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (reflectionModeProperty != null)
        {
            // Set to reflection mode (value depends on your HDRP version)
            reflectionModeProperty.SetValue(hdAdditionalLightData, true);
            Debug.Log($"[Sol Setup] Moon {moonConfig.name} set to reflection mode");
        }
        #endif
    }

    // Configure CelestialRotator
    CelestialRotator moonRotator = moonLightGO.GetComponent<CelestialRotator>();
    if (moonRotator == null)
    {
        moonRotator = moonLightGO.AddComponent<CelestialRotator>();
    }
    
    ConfigureCelestialRotator(moonRotator, moonConfig, true);

    // Configure sun reflection if enabled
    if (moonConfig.reflectSunLight)
    {
        ConfigureMoonSunReflection(moonLightGO, moonConfig, config);
    }
        }

        /// <summary>
        /// Configure CelestialRotator component with celestial body settings
        /// </summary>
        private static void ConfigureCelestialRotator(CelestialRotator rotator, SolSetupWizard.CelestialBodyConfig bodyConfig, bool isMoon)
        {
            var celestialBodyNameField = typeof(CelestialRotator).GetField("celestialBodyName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            celestialBodyNameField?.SetValue(rotator, bodyConfig.name);

            var isMoonField = typeof(CelestialRotator).GetField("isMoon", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isMoonField?.SetValue(rotator, isMoon);

            var smoothRotationField = typeof(CelestialRotator).GetField("smoothRotation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            smoothRotationField?.SetValue(rotator, true);

            var enableDebugLoggingField = typeof(CelestialRotator).GetField("enableDebugLogging", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            enableDebugLoggingField?.SetValue(rotator, false);
        }

        /// <summary>
        /// Configure moon to reflect sun light
        /// </summary>
        private static void ConfigureMoonSunReflection(GameObject moonLightGO, SolSetupWizard.MoonConfig moonConfig, SolSetupWizard.SetupConfig config)
        {
            // Try to find a component that handles sun reflection
            // This would depend on your specific moon reflection system
            var reflectionComponents = moonLightGO.GetComponents<MonoBehaviour>();
            
            foreach (var component in reflectionComponents)
            {
                var componentType = component.GetType();
                
                // Look for sun reference field
                var sunReferenceField = componentType.GetField("sunToReflect", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (sunReferenceField != null)
                {
                    sunReferenceField.SetValue(component, moonConfig.sunToReflect);
                    Debug.Log($"[Sol Setup] Configured moon {moonConfig.name} to reflect sun: {moonConfig.sunToReflect}");
                    break;
                }
            }
        }

        #endregion

        #region Enhanced Sky and Fog Creation

        /// <summary>
        /// Create Sky and Fog Volume with existing HDRP profile or default
        /// </summary>
        public static void CreateSkyAndFogVolumeWithProfile(SolSetupWizard.SetupConfig config)
        {
            GameObject skyVolumeGO = new GameObject("Sky and Fog Global Volume");
    
            var volume = skyVolumeGO.AddComponent<UnityEngine.Rendering.Volume>();
            volume.isGlobal = true;
            volume.priority = 0;
    
            UnityEngine.Rendering.VolumeProfile profile = null;
    
            // Try to load existing profile
            if (!string.IsNullOrEmpty(config.hdrpProfilePath))
            {
                profile = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.VolumeProfile>(config.hdrpProfilePath);
                if (profile != null)
                {
                    Debug.Log($"[Sol Setup] Using existing HDRP profile: {config.hdrpProfilePath}");
                }
                else
                {
                    Debug.LogWarning($"[Sol Setup] Could not load HDRP profile at: {config.hdrpProfilePath}");
                }
            }
    
            // Create default profile if none specified or loading failed
            if (profile == null)
            {
                profile = CreateDefaultHDRPProfile(config);
            }
    
            // Properly assign profile to volume
            if (profile != null)
            {
                volume.profile = profile;
        
                // Force refresh the volume
                EditorUtility.SetDirty(volume);
        
                Debug.Log($"[Sol Setup] Volume profile assigned successfully: {profile.name}");
            }
            else
            {
                Debug.LogError("[Sol Setup] Failed to create or assign volume profile!");
            }
        }

        /// <summary>
        /// Create a default HDRP profile with physically based sky and fog
        /// </summary>
        private static UnityEngine.Rendering.VolumeProfile CreateDefaultHDRPProfile(SolSetupWizard.SetupConfig config)
        {
                var profile = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
    profile.name = "Sol Default Sky Profile";
    
    #if UNITY_RENDER_PIPELINES_HIGH_DEFINITION
    try
    {
        // Add Physically Based Sky
        var physicallyBasedSky = profile.Add<UnityEngine.Rendering.HighDefinition.PhysicallyBasedSky>();
        physicallyBasedSky.active = true;
        physicallyBasedSky.earthPreset.value = true;
        
        // Add Fog component
        var fog = profile.Add<UnityEngine.Rendering.HighDefinition.Fog>();
        fog.active = true;
        fog.enabled.value = true;
        fog.colorMode.value = UnityEngine.Rendering.HighDefinition.FogColorMode.SkyColor;
        fog.meanFreePath.value = 400f;
        fog.baseHeight.value = 0f;
        fog.maximumHeight.value = 50f;
        
        // Add Exposure
        var exposure = profile.Add<UnityEngine.Rendering.HighDefinition.Exposure>();
        exposure.active = true;
        exposure.mode.value = UnityEngine.Rendering.HighDefinition.ExposureMode.Automatic;
        exposure.compensation.value = 0f;
        
        Debug.Log("[Sol Setup] Default HDRP profile created with Physically Based Sky and Fog.");
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[Sol Setup] Error creating HDRP profile components: {e.Message}");
        return null;
    }
    #else
    Debug.LogError("[Sol Setup] HDRP is required for Sol system!");
    return null;
    #endif
    
    // Save profile with proper path handling
    EnsureFolderExists(config.dataFolderPath);
    string profilePath = $"{config.dataFolderPath}/SolDefaultSkyProfile.asset";
    
    try
    {
        AssetDatabase.CreateAsset(profile, profilePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"[Sol Setup] Default HDRP profile saved: {profilePath}");
        return profile;
    }
    catch (System.Exception e)
    {
        Debug.LogError($"[Sol Setup] Error saving HDRP profile: {e.Message}");
        return null;
    }
        }

        #endregion

        #region Enhanced Seasonal Data Creation

        /// <summary>
        /// Create seasonal data objects and populate WorldTimeData list with multiple celestial bodies
        /// </summary>
        public static void CreateSeasonalDataAndPopulateLists(SolSetupWizard.SetupConfig config, WorldTimeData worldTimeData)
        {
            EnsureFolderExists(config.dataFolderPath);

            List<WorldTimeData.SeasonConfiguration> seasonConfigurations = new List<WorldTimeData.SeasonConfiguration>();
            
            // Calculate days per season (evenly distributed)
            int daysPerSeason = 832 / config.numberOfSeasons;
            
            for (int i = 0; i < config.numberOfSeasons; i++)
            {
                // Create SeasonalData asset
                SeasonalData seasonData = ScriptableObject.CreateInstance<SeasonalData>();
                ConfigureIndividualSeasonData(seasonData, config, i);
                
                // Save each season as separate asset
                string seasonAssetPath = $"{config.dataFolderPath}/{config.seasonNames[i]}SeasonData.asset";
                AssetDatabase.CreateAsset(seasonData, seasonAssetPath);
                
                // Create season configuration for WorldTimeData
                var seasonConfig = new WorldTimeData.SeasonConfiguration
                {
                    seasonName = config.seasonNames[i],
                    lengthInDays = daysPerSeason,
                    seasonalData = seasonData,
                    overrideAmbientColors = false,
                    seasonDayAmbient = Color.white,
                    seasonNightAmbient = Color.blue,
                    seasonColor = GetSeasonColor(i, config.numberOfSeasons)
                };
                
                seasonConfigurations.Add(seasonConfig);
                
                Debug.Log($"[Sol Setup] Season data created: {seasonAssetPath}");
            }

            // Update WorldTimeData with season configurations
            if (worldTimeData != null)
            {
                worldTimeData.seasons = seasonConfigurations;
                EditorUtility.SetDirty(worldTimeData);
                Debug.Log("[Sol Setup] WorldTimeData updated with seasonal configurations.");
            }
        }

        /// <summary>
        /// Configure individual season data with multiple celestial bodies
        /// </summary>
        private static void ConfigureIndividualSeasonData(SeasonalData seasonData, SolSetupWizard.SetupConfig config, int seasonIndex)
        {
            // Calculate realistic orbital angle (-23.5° to +23.5° for Earth-like)
            float orbitalAngle = Mathf.Lerp(-23.5f, 23.5f, (float)seasonIndex / (config.numberOfSeasons - 1));
            
            // Set the common orbital angle for this season
            var commonOrbitalAngleField = typeof(SeasonalData).GetField("commonOrbitalAngle", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            commonOrbitalAngleField?.SetValue(seasonData, orbitalAngle);
            
            // Enable common orbital angle usage
            var useCommonOrbitalAngleField = typeof(SeasonalData).GetField("useCommonOrbitalAngle", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            useCommonOrbitalAngleField?.SetValue(seasonData, true);
            
            // Get the stars and moons lists using reflection
            var starsField = typeof(SeasonalData).GetField("stars", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var moonsField = typeof(SeasonalData).GetField("moons", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var starsList = (List<CelestialBody>)starsField?.GetValue(seasonData) ?? new List<CelestialBody>();
            var moonsList = (List<CelestialBody>)moonsField?.GetValue(seasonData) ?? new List<CelestialBody>();
            
            // Clear existing bodies
            starsList.Clear();
            moonsList.Clear();
            
            // Add all configured suns to stars list
            foreach (var sunConfig in config.suns)
            {
                if (sunConfig.active)
                {
                    starsList.Add(CreateCelestialBodyFromConfig(sunConfig));
                }
            }
            
            // Add all configured moons to moons list
            foreach (var moonConfig in config.moons)
            {
                if (moonConfig.active)
                {
                    moonsList.Add(CreateCelestialBodyFromConfig(moonConfig));
                }
            }
            
            // Set the lists back
            starsField?.SetValue(seasonData, starsList);
            moonsField?.SetValue(seasonData, moonsList);
            
            Debug.Log($"[Sol Setup] Configured season data for: {config.seasonNames[seasonIndex]} with {starsList.Count} suns and {moonsList.Count} moons");
        }

        /// <summary>
        /// Create CelestialBody from configuration
        /// </summary>
        private static CelestialBody CreateCelestialBodyFromConfig(SolSetupWizard.CelestialBodyConfig bodyConfig)
        {
            return new CelestialBody
            {
                name = bodyConfig.name,
                active = bodyConfig.active,
                overrideOrbitalAngle = false, // Use common orbital angle
                yAxisEnabled = bodyConfig.yAxisEnabled,
                yAxisSpeed = bodyConfig.yAxisSpeed,
                yAxisOverrideSpeed = bodyConfig.yAxisOverrideSpeed,
                orbitalAngle = bodyConfig.orbitalAngle,
                baseElevation = bodyConfig.baseElevation,
                orbitalPeriod = bodyConfig.orbitalPeriod,
                phaseOffset = bodyConfig.phaseOffset
            };
        }

        #endregion

        #region WorldTimeData Creation

        /// <summary>
        /// Create WorldTimeData with realistic values based on your system
        /// </summary>
        public static WorldTimeData CreateWorldTimeDataWithRealisticValues(SolSetupWizard.SetupConfig config)
        {
            EnsureFolderExists(config.dataFolderPath);

            WorldTimeData worldTimeData = ScriptableObject.CreateInstance<WorldTimeData>();
            
            // Configure with realistic values based on your WorldTimeData structure
            worldTimeData.dayLengthInSeconds = 1440f; // 24 minutes = 1 day (60x speed)
            worldTimeData.totalDaysInYear = 832; // Your system's year length
            worldTimeData.hoursPerDay = 20; // Your system's hours per day
            worldTimeData.minutesPerHour = 60;
            worldTimeData.secondsPerMinute = 60;
            worldTimeData.seasonTransitionDays = 20;
            worldTimeData.daysPerMonth = 104;
            
            // Initialize empty seasons list (will be populated later)
            worldTimeData.seasons = new List<WorldTimeData.SeasonConfiguration>();

            // Save asset
            string assetPath = $"{config.dataFolderPath}/DefaultWorldTimeData.asset";
            AssetDatabase.CreateAsset(worldTimeData, assetPath);
            
            Debug.Log($"[Sol Setup] WorldTimeData created with realistic values at {assetPath}");
            return worldTimeData;
        }

        #endregion

        #region TimeManager Creation

        /// <summary>
        /// Create TimeManager and assign WorldTimeData
        /// </summary>
        public static void CreateTimeManagerWithData(SolSetupWizard.SetupConfig config, WorldTimeData worldTimeData)
        {
            // Check if TimeManager already exists
            TimeManager existingTimeManager = Object.FindObjectOfType<TimeManager>();
            if (existingTimeManager != null)
            {
                Debug.Log("[Sol Setup] TimeManager already exists in scene, updating configuration.");
                
                // Assign WorldTimeData if provided
                if (worldTimeData != null)
                {
                    AssignWorldTimeDataToTimeManager(existingTimeManager, worldTimeData);
                }
                return;
            }

            // Create TimeManager GameObject
            GameObject timeManagerGO = new GameObject("TimeManager");
            TimeManager timeManager = timeManagerGO.AddComponent<TimeManager>();

            // Assign WorldTimeData if created
            if (worldTimeData != null)
            {
                AssignWorldTimeDataToTimeManager(timeManager, worldTimeData);
            }

            Debug.Log("[Sol Setup] TimeManager created successfully.");
        }

        /// <summary>
        /// Assign WorldTimeData to TimeManager using reflection
        /// </summary>
        private static void AssignWorldTimeDataToTimeManager(TimeManager timeManager, WorldTimeData worldTimeData)
        {
            // Use reflection to assign WorldTimeData - adjust field name based on your TimeManager implementation
            var worldTimeDataField = typeof(TimeManager).GetField("worldTimeData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            
            if (worldTimeDataField != null)
            {
                worldTimeDataField.SetValue(timeManager, worldTimeData);
                Debug.Log("[Sol Setup] WorldTimeData assigned to TimeManager.");
            }
            else
            {
                Debug.LogWarning("[Sol Setup] Could not find worldTimeData field on TimeManager. Please assign manually.");
            }
        }

        #endregion

        #region Demo Scene Creation

        /// <summary>
        /// Add demo content to current scene
        /// </summary>
        public static void CreateDemoSceneWithContent(SolSetupWizard.SetupConfig config)
        {
            // Add demo objects to current scene
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Demo Ground";
            ground.transform.localScale = new Vector3(10, 1, 10);

            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Demo Cube";
            cube.transform.position = new Vector3(0, 0.5f, 0);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Demo Sphere";
            sphere.transform.position = new Vector3(3, 0.5f, 0);

            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = "Demo Cylinder";
            cylinder.transform.position = new Vector3(-3, 1f, 0);

            Debug.Log("[Sol Setup] Demo content added to current scene.");
        }

        #endregion

        #region Utility Methods

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
        /// Get appropriate color for season based on index
        /// </summary>
        private static Color GetSeasonColor(int seasonIndex, int totalSeasons)
        {
            // Generate colors around the color wheel
            float hue = (float)seasonIndex / totalSeasons;
            return Color.HSVToRGB(hue, 0.7f, 0.9f);
        }

        #endregion
    }
}

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Sol.Editor
{
    [CustomEditor(typeof(SeasonalData))]
    public class SeasonalDataEditor : UnityEditor.Editor
    {
        private Dictionary<int, bool> starFoldouts = new Dictionary<int, bool>();
        private Dictionary<int, bool> moonFoldouts = new Dictionary<int, bool>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawCommonOrbitalSettings();
            EditorGUILayout.Space(10);

            DrawStarsSection();
            EditorGUILayout.Space(10);

            DrawMoonsSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Seasonal Data Configuration", EditorStyles.largeLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Configure celestial bodies for this season (Langsomr, Svik, Evinotr, or Gro). Each body orbits continuously with angled orbital paths.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawCommonOrbitalSettings()
        {
            var useCommonOrbitalAngleProp = serializedObject.FindProperty("useCommonOrbitalAngle");
            var commonOrbitalAngleProp = serializedObject.FindProperty("commonOrbitalAngle");

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Common Orbital Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(useCommonOrbitalAngleProp, 
                new GUIContent("Use Common Orbital Angle", 
                "When enabled, all celestial bodies share the same orbital angle (realistic astronomy)"));

            if (useCommonOrbitalAngleProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(commonOrbitalAngleProp, 
                    new GUIContent("Common Orbital Angle", 
                    "Shared orbital angle for all celestial bodies (Earth's axial tilt is 23.5°)"));

                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox(
                    $"All celestial bodies will use {commonOrbitalAngleProp.floatValue:F1}° orbital angle unless they have 'Override Orbital Angle' enabled.\n\n" +
                    "Realistic values:\n• Earth: 23.5°\n• Mars: 25.2°\n• Jupiter: 3.1°", 
                    MessageType.Info);
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Each celestial body will use its individual orbital angle setting. Enable common orbital angle for realistic astronomy.", 
                    MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
        
        private void DrawStarsSection()
        {
            var starsProperty = serializedObject.FindProperty("stars");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Section header with add button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Stars (Suns)", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Star", GUILayout.Width(80)))
            {
                starsProperty.arraySize++;
                var newElement = starsProperty.GetArrayElementAtIndex(starsProperty.arraySize - 1);
                SetDefaultCelestialBodyValues(newElement, $"Star {starsProperty.arraySize}", false);
                starFoldouts[starsProperty.arraySize - 1] = true;
            }
            EditorGUILayout.EndHorizontal();
            
            if (starsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No stars configured. Add at least one star (sun) for proper lighting.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.Space(5);
                
                // Add padding to prevent foldout arrow cutoff
                EditorGUILayout.BeginVertical();
                GUILayout.Space(2); // Small top padding
                
                for (int i = 0; i < starsProperty.arraySize; i++)
                {
                    // Add left margin to ensure foldout arrow is visible
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(4); // Left padding for foldout arrow
                    
                    EditorGUILayout.BeginVertical();
                    DrawCelestialBodyElement(starsProperty, i, "Star", starFoldouts, false);
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                GUILayout.Space(2); // Small bottom padding
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawMoonsSection()
        {
            var moonsProperty = serializedObject.FindProperty("moons");
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Section header with add button
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Moons", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add Moon", GUILayout.Width(80)))
            {
                moonsProperty.arraySize++;
                var newElement = moonsProperty.GetArrayElementAtIndex(moonsProperty.arraySize - 1);
                SetDefaultCelestialBodyValues(newElement, $"Moon {moonsProperty.arraySize}", true);
                moonFoldouts[moonsProperty.arraySize - 1] = true;
            }
            EditorGUILayout.EndHorizontal();
            
            if (moonsProperty.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No moons configured. Moons provide nighttime lighting and atmospheric effects.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(5);
                
                // Add padding to prevent foldout arrow cutoff
                EditorGUILayout.BeginVertical();
                GUILayout.Space(2); // Small top padding
                
                for (int i = 0; i < moonsProperty.arraySize; i++)
                {
                    // Add left margin to ensure foldout arrow is visible
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(4); // Left padding for foldout arrow
                    
                    EditorGUILayout.BeginVertical();
                    DrawCelestialBodyElement(moonsProperty, i, "Moon", moonFoldouts, true);
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                GUILayout.Space(2); // Small bottom padding
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCelestialBodyElement(SerializedProperty arrayProperty, int index, string typeName, Dictionary<int, bool> foldouts, bool isMoon)
        {
            var element = arrayProperty.GetArrayElementAtIndex(index);
            var nameProperty = element.FindPropertyRelative("name");
            var activeProperty = element.FindPropertyRelative("active");

            // Initialize foldout state if needed
            if (!foldouts.ContainsKey(index))
                foldouts[index] = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Header with manual arrow and spaced frame
            Rect headerRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            
            string displayName = string.IsNullOrEmpty(nameProperty.stringValue) ? $"Unnamed {typeName}" : nameProperty.stringValue;
            string headerText = $"{displayName} {(activeProperty.boolValue ? "" : "(Inactive)")}";
            
            // Calculate layout
            float arrowWidth = 12f;
            float spacingAfterArrow = 8f; // The space you want between arrow and frame
            float frameStartX = headerRect.x + arrowWidth + spacingAfterArrow;
            float frameWidth = headerRect.width - arrowWidth - spacingAfterArrow;
            
            // Create rects
            Rect arrowRect = new Rect(headerRect.x, headerRect.y, arrowWidth, headerRect.height);
            Rect frameRect = new Rect(frameStartX, headerRect.y, frameWidth, headerRect.height);
            
            // Draw the foldout arrow manually
            if (GUI.Button(arrowRect, foldouts[index] ? "▼" : "▶", EditorStyles.label))
            {
                foldouts[index] = !foldouts[index];
            }
            
            // Draw the frame background
            GUI.Box(frameRect, "", EditorStyles.toolbar);
            
            // Calculate content areas within the frame
            float deleteButtonWidth = 25f;
            float activeToggleWidth = 15f;
            float activeLabelWidth = 40f;
            float padding = 4f;
            
            float controlsWidth = deleteButtonWidth + activeToggleWidth + activeLabelWidth + padding;
            float labelWidth = frameWidth - controlsWidth - (padding * 2);
            
            Rect labelRect = new Rect(frameRect.x + padding, frameRect.y, labelWidth, frameRect.height);
            Rect activeLabelRect = new Rect(labelRect.xMax, frameRect.y, activeLabelWidth, frameRect.height);
            Rect activeToggleRect = new Rect(activeLabelRect.xMax, frameRect.y, activeToggleWidth, frameRect.height);
            Rect deleteButtonRect = new Rect(activeToggleRect.xMax + padding, frameRect.y, deleteButtonWidth, frameRect.height);
            
            // Draw content within the frame
            EditorGUI.LabelField(labelRect, headerText);
            EditorGUI.LabelField(activeLabelRect, "Active");
            EditorGUI.PropertyField(activeToggleRect, activeProperty, GUIContent.none);
            
            // Delete button
            GUI.backgroundColor = Color.red;
            if (GUI.Button(deleteButtonRect, "×"))
            {
                arrayProperty.DeleteArrayElementAtIndex(index);
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.backgroundColor = Color.white;

            // Content area (only show if expanded)
            if (foldouts[index])
            {
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;
                
                // Basic configuration
                EditorGUILayout.PropertyField(nameProperty, new GUIContent("Name", "Display name for this celestial body"));
                
                if (activeProperty.boolValue)
                {
                    EditorGUILayout.Space(5);
                    DrawOrbitalControls(element);
                    EditorGUILayout.Space(5);
                    DrawOrbitalPathControls(element, isMoon);
                    
                    if (isMoon)
                    {
                        EditorGUILayout.Space(5);
                        DrawMoonControls(element);
                    }
                }
                else
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.HelpBox($"This {typeName.ToLower()} is inactive and will not be visible during this season.", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        private void DrawOrbitalControls(SerializedProperty element)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Orbital Motion (Azimuth)", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space(3);
            
            var yAxisEnabledProperty = element.FindPropertyRelative("yAxisEnabled");
            EditorGUILayout.PropertyField(yAxisEnabledProperty, new GUIContent("Enable Orbital Motion", "Whether this celestial body moves across the sky"));
            
            if (yAxisEnabledProperty.boolValue)
            {
                EditorGUILayout.Space(3);
                var yAxisOverrideProperty = element.FindPropertyRelative("yAxisOverrideSpeed");
                EditorGUILayout.PropertyField(yAxisOverrideProperty, new GUIContent("Sync with Day Length", "Automatically sync orbital speed with TimeManager day length (1 orbit per day)"));
                
                if (!yAxisOverrideProperty.boolValue)
                {
                    var yAxisSpeedProperty = element.FindPropertyRelative("yAxisSpeed");
                    EditorGUILayout.PropertyField(yAxisSpeedProperty, new GUIContent("Orbital Speed", "Speed multiplier (1.0 = one orbit per day, 2.0 = two orbits per day)"));
                    
                    if (yAxisSpeedProperty.floatValue <= 0)
                    {
                        EditorGUILayout.HelpBox("Orbital speed must be greater than 0", MessageType.Error);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Orbital speed is automatically synced with day length (1 orbit per day)", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("This celestial body will remain stationary in the sky", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawOrbitalPathControls(SerializedProperty element, bool isMoon)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Orbital Path Configuration", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space(3);
            
            var orbitalAngleProperty = element.FindPropertyRelative("orbitalAngle");
            var baseElevationProperty = element.FindPropertyRelative("baseElevation");
            var overrideOrbitalAngleProperty = element.FindPropertyRelative("overrideOrbitalAngle");
            
            // Check if common orbital angle is enabled
            var useCommonOrbitalAngleProp = serializedObject.FindProperty("useCommonOrbitalAngle");
            var commonOrbitalAngleProp = serializedObject.FindProperty("commonOrbitalAngle");
            
            if (useCommonOrbitalAngleProp.boolValue)
            {
                EditorGUILayout.PropertyField(overrideOrbitalAngleProperty, 
                    new GUIContent("Override Orbital Angle", 
                    "Override the common orbital angle with individual setting"));
                
                if (overrideOrbitalAngleProperty.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(orbitalAngleProperty, 
                        new GUIContent("Individual Orbital Angle", 
                        "Custom orbital angle for this celestial body"));
                    EditorGUI.indentLevel--;
                }
                else
                {
                                    EditorGUILayout.HelpBox(
                        $"Using common orbital angle: {commonOrbitalAngleProp.floatValue:F1}°", 
                        MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.PropertyField(orbitalAngleProperty, 
                    new GUIContent("Orbital Angle", 
                    "Angle of orbital path variation (0° = flat circle, 45° = angled orbit). Higher values create more dramatic seasonal arcs."));
            }
            
            EditorGUILayout.PropertyField(baseElevationProperty, 
                new GUIContent("Base Elevation", 
                "Center elevation of orbital path (90° = overhead, 180° = horizon, 270° = below horizon)"));
            
            // Calculate and show orbital path preview
            float effectiveOrbitalAngle = (useCommonOrbitalAngleProp.boolValue && !overrideOrbitalAngleProperty.boolValue) 
                ? commonOrbitalAngleProp.floatValue 
                : orbitalAngleProperty.floatValue;
                
            float minElevation = baseElevationProperty.floatValue + effectiveOrbitalAngle;
            float maxElevation = baseElevationProperty.floatValue - effectiveOrbitalAngle;
            
            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox($"Orbital Path Preview:\n• Highest point (noon): {maxElevation:F1}°\n• Lowest point (midnight): {minElevation:F1}°\n• Total variation: {effectiveOrbitalAngle * 2:F1}°", MessageType.None);
            
            // Seasonal recommendations
            if (!isMoon)
            {
                EditorGUILayout.Space(3);
                string seasonGuide = "Season Recommendations:\n";
                seasonGuide += "• Langsomr (Summer): Angle 45°, Base 135° (high arc)\n";
                seasonGuide += "• Svik (Autumn): Angle 30°, Base 180° (moderate arc)\n";
                seasonGuide += "• Evinotr (Winter): Angle 15°, Base 225° (low arc)\n";
                seasonGuide += "• Gro (Spring): Angle 30°, Base 180° (moderate arc)";
                
                EditorGUILayout.HelpBox(seasonGuide, MessageType.Info);
            }
            
            // Warn about extreme values
            if (IsNearGimbalLock(baseElevationProperty.floatValue))
            {
                EditorGUILayout.HelpBox("Warning: Base elevation near 90° may cause rotation issues. Consider using 85° or 95° instead.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawMoonControls(SerializedProperty element)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Moon-Specific Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space(3);
            
            var orbitalPeriodProperty = element.FindPropertyRelative("orbitalPeriod");
            var phaseOffsetProperty = element.FindPropertyRelative("phaseOffset");
            
            EditorGUILayout.PropertyField(orbitalPeriodProperty, new GUIContent("Orbital Period (Days)", "How many days for one complete orbit around the planet. Earth's moon: ~29.5 days"));
            EditorGUILayout.PropertyField(phaseOffsetProperty, new GUIContent("Phase Offset (Degrees)", "Angular offset from sun position (0° = same as sun, 180° = opposite sun for night visibility)"));
            
            if (orbitalPeriodProperty.floatValue <= 0)
            {
                EditorGUILayout.HelpBox("Orbital period must be greater than 0", MessageType.Error);
            }
            else if (orbitalPeriodProperty.floatValue < 1f)
            {
                EditorGUILayout.HelpBox("Very short orbital periods may cause rapid movement", MessageType.Warning);
            }
            
            // Phase offset helpers
            EditorGUILayout.Space(3);
            string phaseInfo = GetPhaseOffsetDescription(phaseOffsetProperty.floatValue);
            EditorGUILayout.HelpBox(phaseInfo, MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }

        private string GetPhaseOffsetDescription(float phaseOffset)
        {
            float normalizedOffset = Mathf.Repeat(phaseOffset, 360f);
            
            if (Mathf.Approximately(normalizedOffset, 0f))
                return "Phase: New Moon (same position as sun, not visible at night)";
            else if (Mathf.Approximately(normalizedOffset, 90f))
                return "Phase: First Quarter (90° ahead of sun, visible evening/night)";
            else if (Mathf.Approximately(normalizedOffset, 180f))
                return "Phase: Full Moon (opposite to sun, visible all night) ⭐ Recommended";
            else if (Mathf.Approximately(normalizedOffset, 270f))
                return "Phase: Last Quarter (90° behind sun, visible night/morning)";
            else
                return $"Phase: Custom offset ({normalizedOffset:F0}°)\nRecommended for night visibility: 180° (opposite sun)";
        }

        private void SetDefaultCelestialBodyValues(SerializedProperty element, string defaultName, bool isMoon)
        {
            element.FindPropertyRelative("name").stringValue = defaultName;
            element.FindPropertyRelative("active").boolValue = true;
            
            // Orbital settings
            element.FindPropertyRelative("yAxisEnabled").boolValue = true;
            element.FindPropertyRelative("yAxisSpeed").floatValue = 1f;
            element.FindPropertyRelative("yAxisOverrideSpeed").boolValue = true; // Default to day sync
            
            // Orbital override settings
            element.FindPropertyRelative("overrideOrbitalAngle").boolValue = false; // Default to using common angle
            
            // Orbital path settings
            if (isMoon)
            {
                // Moon defaults - moderate orbital path for night visibility
                element.FindPropertyRelative("orbitalAngle").floatValue = 25f;
                element.FindPropertyRelative("baseElevation").floatValue = 160f; // Slightly above horizon
                element.FindPropertyRelative("orbitalPeriod").floatValue = 29.5f;
                element.FindPropertyRelative("phaseOffset").floatValue = 180f; // Opposite to sun
            }
            else
            {
                // Sun defaults - moderate seasonal variation
                element.FindPropertyRelative("orbitalAngle").floatValue = 30f;
                element.FindPropertyRelative("baseElevation").floatValue = 180f; // Horizon level
                element.FindPropertyRelative("orbitalPeriod").floatValue = 0f; // Not used for suns
                element.FindPropertyRelative("phaseOffset").floatValue = 0f; // No offset for primary sun
            }
        }

        private bool IsNearGimbalLock(float elevation)
        {
            return elevation >= 85f && elevation <= 95f;
        }
    }
}