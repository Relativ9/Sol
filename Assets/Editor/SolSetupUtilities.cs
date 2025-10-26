using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

#if UNITY_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace Sol.Editor
{
    /// <summary>
    /// Utility methods for setting up the Sol Time & Celestial System.
    /// Contains all business logic for scene setup, asset creation, and configuration.
    /// Follows Single Responsibility Principle - pure business logic, no UI concerns.
    /// </summary>
    public static class SolSetupUtilities
    {
        #region Main Setup Entry Point
        
        /// <summary>
        /// Performs complete setup of the Sol system based on provided configuration.
        /// This is the main entry point called by the wizard.
        /// </summary>
        /// <param name="config">Shared configuration DTO from SolSetupConfig</param>
        /// <param name="statusCallback">Optional callback for progress updates</param>
        public static void PerformCompleteSetup(SetupConfig config, System.Action<string> statusCallback = null)
        {
            Debug.Log("===== SOL SETUP START =====");
            
            // Validate config first
            if (config == null)
            {
                Debug.LogError("[Sol Setup] Config is null!");
                throw new System.ArgumentNullException(nameof(config), "Setup configuration cannot be null");
            }

            WorldTimeData worldTimeData = null;
            SeasonalData[] seasonalDataAssets = null;

            try
            {
                // STEP 0: Create ServiceLocator
                statusCallback?.Invoke("Creating ServiceLocator...");
                CreateServiceLocator();
                Debug.Log("[Sol Setup] ServiceLocator created");

                // STEP 1: Create WorldTimeData
                if (config.createWorldTimeData)
                {
                    statusCallback?.Invoke("Creating WorldTimeData...");
                    worldTimeData = CreateWorldTimeDataWithRealisticValues(config);
                    
                    if (worldTimeData == null)
                    {
                        throw new System.Exception("Failed to create WorldTimeData!");
                    }
                    
                    Debug.Log($"[Sol Setup] WorldTimeData created successfully");
                }

                // STEP 2: Create Seasonal Data Assets
                if (config.createSeasonalData && worldTimeData != null)
                {
                    statusCallback?.Invoke("Creating seasonal data assets...");
                    seasonalDataAssets = CreateSeasonalDataAssets(config, worldTimeData);
                    Debug.Log($"[Sol Setup] Created {seasonalDataAssets?.Length ?? 0} seasonal data assets");
                }

                // STEP 3: Create TimeManager in scene
                if (config.createTimeManager)
                {
                    statusCallback?.Invoke("Creating TimeManager...");
                    CreateTimeManagerInScene(worldTimeData);
                    Debug.Log($"[Sol Setup] TimeManager created in scene");
                }

                // ========================================
                // CRITICAL: Create Sky BEFORE Lights!
                // ========================================
                
                // STEP 4: Create Sky and Fog Volume FIRST
                // This initializes the Physically Based Sky which enables celestialBodyShadingSource property
                if (config.createSkyAndFog)
                {
                    statusCallback?.Invoke("Creating sky and fog volume...");
                    CreateSkyAndFogVolume(config);
                    Debug.Log($"[Sol Setup] Sky and fog volume created");
                    
                    // Small delay to ensure HDRP processes the volume
                    // This allows the celestialBodyShadingSource property to become available
                    System.Threading.Thread.Sleep(100);
                    Debug.Log("[Sol Setup] Waiting for HDRP to initialize sky...");
                }
                else
                {
                    Debug.LogWarning("[Sol Setup] Sky and Fog Volume not created. Celestial body properties may not be available!");
                }

                // STEP 5: NOW Create Celestial Lights (after sky is initialized)
                if (config.suns.Count > 0 || config.moons.Count > 0)
                {
                    statusCallback?.Invoke("Creating celestial lights...");
                    CreateAllCelestialLights(config);
                    Debug.Log($"[Sol Setup] Created {config.suns.Count} suns and {config.moons.Count} moons");
                }
                
                // STEP 5.5: NOW configure celestial body shading (after lights exist and sky is ready) )
                statusCallback?.Invoke("Configuring celestial body shading...");
                ConfigureCelestialBodyShadingForAllLights(config); // ← Pass config

                // STEP 6: Create Demo Content (if requested)
                if (config.createDemoScene)
                {
                    statusCallback?.Invoke("Creating demo content...");
                    CreateDemoContent();
                    Debug.Log($"[Sol Setup] Demo content created");
                }

                Debug.Log("===== SOL SETUP COMPLETE =====");
                statusCallback?.Invoke("Setup complete!");
                
                // Refresh scene view to show changes
                UnityEditor.SceneView.RepaintAll();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Sol Setup] Setup failed: {ex.Message}");
                Debug.LogError($"[Sol Setup] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        #endregion

        #region ServiceLocator Creation

        /// <summary>
        /// Creates ServiceLocator GameObject with the ServiceLocator component.
        /// This is the dependency injection container for the Sol system.
        /// </summary>
        private static void CreateServiceLocator()
        {
            Debug.Log("[Sol Setup] === Creating ServiceLocator ===");
            
            // Check if ServiceLocator already exists
            ServiceLocator existingServiceLocator = Object.FindObjectOfType<ServiceLocator>();
            
            if (existingServiceLocator != null)
            {
                Debug.LogWarning("[Sol Setup] ServiceLocator already exists in scene.");
                return;
            }
            
            // Create new GameObject for ServiceLocator
            GameObject serviceLocatorObject = new GameObject("ServiceLocator");
            ServiceLocator serviceLocator = serviceLocatorObject.AddComponent<ServiceLocator>();
            
            if (serviceLocator == null)
            {
                Debug.LogError("[Sol Setup] Failed to add ServiceLocator component!");
                return;
            }
            
            // Register undo
            Undo.RegisterCreatedObjectUndo(serviceLocatorObject, "Create ServiceLocator");
            
            Debug.Log("[Sol Setup] ServiceLocator created successfully");
        }

        #endregion

        #region WorldTimeData Creation

        /// <summary>
        /// Creates WorldTimeData asset with realistic default values.
        /// Matches the actual public property names in WorldTimeData.cs.
        /// </summary>
        private static WorldTimeData CreateWorldTimeDataWithRealisticValues(SetupConfig config)
        {
            Debug.Log("[Sol Setup] === Creating WorldTimeData ===");

            // Validate config
            if (config == null)
            {
                Debug.LogError("[Sol Setup] Config is null!");
                throw new System.ArgumentNullException(nameof(config), "Setup configuration cannot be null");
            }

            // Validate and ensure data folder path exists
            string dataFolderPath = config.dataFolderPath;
            if (string.IsNullOrEmpty(dataFolderPath))
            {
                dataFolderPath = "Assets/Sol/Data";
                Debug.LogWarning($"[Sol Setup] dataFolderPath was null/empty, using default: {dataFolderPath}");
            }

            // Ensure folder exists
            EnsureFolderExists(dataFolderPath);

            // Create the WorldTimeData asset
            WorldTimeData worldTimeData = ScriptableObject.CreateInstance<WorldTimeData>();
            
            if (worldTimeData == null)
            {
                Debug.LogError("[Sol Setup] Failed to create WorldTimeData ScriptableObject!");
                throw new System.Exception("Failed to create WorldTimeData instance");
            }

            // Set realistic values - using ACTUAL public field names
            worldTimeData.hoursPerDay = 24;
            worldTimeData.minutesPerHour = 60;
            worldTimeData.secondsPerMinute = 60;
            worldTimeData.daysPerMonth = 30; // 30 days per month
            
            // Calculate realistic day length
            // 1 real minute = 1 game hour (60x speed)
            // So 24 game hours = 24 real minutes = 1440 real seconds
            worldTimeData.dayLengthInSeconds = 1440f; // 24 minutes real time = 1 day game time
            
            // Calculate year length
            int numberOfSeasons = config.numberOfSeasons > 0 ? config.numberOfSeasons : 4;
            int daysPerSeason = 90; // Realistic ~3 months per season
            worldTimeData.totalDaysInYear = numberOfSeasons * daysPerSeason; // 360 days for 4 seasons
            
            // Update daysPerMonth to align with year/month system
            // If we want 12 months in a year: 360 / 12 = 30 days per month
            worldTimeData.daysPerMonth = worldTimeData.totalDaysInYear / 12;

            // Set transition days
            worldTimeData.seasonTransitionDays = 10;

            // Initialize season configurations
            if (worldTimeData.seasons == null)
            {
                worldTimeData.seasons = new List<WorldTimeData.SeasonConfiguration>();
            }
            worldTimeData.seasons.Clear();

            // Create season configurations based on config
            for (int i = 0; i < numberOfSeasons; i++)
            {
                string seasonName = "New Season";
                if (config.seasonNames != null && i < config.seasonNames.Length && !string.IsNullOrEmpty(config.seasonNames[i]))
                {
                    seasonName = config.seasonNames[i];
                }

                var seasonConfig = new WorldTimeData.SeasonConfiguration
                {
                    seasonName = seasonName,
                    lengthInDays = daysPerSeason,
                    seasonalData = null, // Will be linked after SeasonalData creation
                    overrideAmbientColors = false,
                    seasonDayAmbient = Color.white,
                    seasonNightAmbient = new Color(0.1f, 0.1f, 0.2f),
                    seasonColor = GetSeasonColor(i, numberOfSeasons)
                };

                worldTimeData.seasons.Add(seasonConfig);
            }

            // Set month names using SerializedObject (since months list is private)
            SerializedObject serializedWorldTime = new SerializedObject(worldTimeData);
            SerializedProperty monthsProp = serializedWorldTime.FindProperty("months");

            if (monthsProp != null && monthsProp.isArray)
            {
                string[] defaultMonthNames = new string[]
                {
                    "Glavyr", "Tharven", "Solmyr", "Aethon",
                    "Lumis", "Verdis", "Harvyx", "Frosten",
                    "Stormyr", "Wintern", "Icelyn", "Newyr"
                };

                monthsProp.arraySize = 12;
                
                for (int i = 0; i < 12; i++)
                {
                    SerializedProperty monthElement = monthsProp.GetArrayElementAtIndex(i);
                    SerializedProperty nameProp = monthElement.FindPropertyRelative("name");
                    SerializedProperty indexProp = monthElement.FindPropertyRelative("index");
                    SerializedProperty colorProp = monthElement.FindPropertyRelative("monthColor");

                    if (nameProp != null) nameProp.stringValue = defaultMonthNames[i];
                    if (indexProp != null) indexProp.intValue = i;
                    if (colorProp != null) colorProp.colorValue = GetMonthColor(i);
                }

                serializedWorldTime.ApplyModifiedProperties();
            }

            // Save as asset
            string assetPath = $"{dataFolderPath}/WorldTimeData.asset";
            
            // Check if asset already exists
            WorldTimeData existingAsset = AssetDatabase.LoadAssetAtPath<WorldTimeData>(assetPath);
            if (existingAsset != null)
            {
                Debug.LogWarning($"[Sol Setup] WorldTimeData already exists at {assetPath}. Overwriting...");
                EditorUtility.CopySerialized(worldTimeData, existingAsset);
                EditorUtility.SetDirty(existingAsset);
                AssetDatabase.SaveAssets();
                worldTimeData = existingAsset;
            }
            else
            {
                AssetDatabase.CreateAsset(worldTimeData, assetPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[Sol Setup] WorldTimeData asset created at: {assetPath}");
            }

            Debug.Log($"[Sol Setup] WorldTimeData configured:");
            Debug.Log($"  - Day length: {worldTimeData.dayLengthInSeconds}s real time ({worldTimeData.dayLengthInSeconds / 60f} minutes)");
            Debug.Log($"  - Hours per day: {worldTimeData.hoursPerDay}");
            Debug.Log($"  - Total days in year: {worldTimeData.totalDaysInYear}");
            Debug.Log($"  - Days per season: {daysPerSeason}");
            Debug.Log($"  - Days per month: {worldTimeData.daysPerMonth}");
            Debug.Log($"  - Number of seasons: {worldTimeData.seasons.Count}");

            return worldTimeData;
        }

        #endregion

        #region Seasonal Data Creation

        /// <summary>
        /// Creates SeasonalData assets for each season and links them to WorldTimeData.
        /// </summary>
        private static SeasonalData[] CreateSeasonalDataAssets(SetupConfig config, WorldTimeData worldTimeData)
        {
            Debug.Log("[Sol Setup] === Creating Seasonal Data Assets ===");

            if (worldTimeData == null || worldTimeData.seasons == null || worldTimeData.seasons.Count == 0)
            {
                Debug.LogWarning("[Sol Setup] No seasons configured in WorldTimeData!");
                return new SeasonalData[0];
            }

            string dataFolderPath = config.dataFolderPath;
            EnsureFolderExists(dataFolderPath);

            List<SeasonalData> seasonalDataAssets = new List<SeasonalData>();

            for (int i = 0; i < worldTimeData.seasons.Count; i++)
            {
                var seasonConfig = worldTimeData.seasons[i];
                string seasonName = seasonConfig.seasonName;

                // Create SeasonalData asset
                SeasonalData seasonalData = ScriptableObject.CreateInstance<SeasonalData>();
                
                if (seasonalData == null)
                {
                    Debug.LogError($"[Sol Setup] Failed to create SeasonalData for season: {seasonName}");
                    continue;
                }

                // Configure the seasonal data with default celestial body configs
                ConfigureSeasonalData(seasonalData, seasonName, config, i);

                // Save as asset
                string assetPath = $"{dataFolderPath}/SeasonalData_{seasonName}.asset";
                
                SeasonalData existingAsset = AssetDatabase.LoadAssetAtPath<SeasonalData>(assetPath);
                if (existingAsset != null)
                {
                    Debug.LogWarning($"[Sol Setup] SeasonalData for {seasonName} already exists. Overwriting...");
                    EditorUtility.CopySerialized(seasonalData, existingAsset);
                    EditorUtility.SetDirty(existingAsset);
                    seasonalData = existingAsset;
                }
                else
                {
                    AssetDatabase.CreateAsset(seasonalData, assetPath);
                    Debug.Log($"[Sol Setup] Created SeasonalData: {assetPath}");
                }

                // Link back to WorldTimeData
                seasonConfig.seasonalData = seasonalData;
                seasonalDataAssets.Add(seasonalData);
            }

            // Save WorldTimeData with linked seasonal data
            EditorUtility.SetDirty(worldTimeData);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Sol Setup] Created and linked {seasonalDataAssets.Count} SeasonalData assets");

            return seasonalDataAssets.ToArray();
        }

        /// <summary>
        /// Configures a SeasonalData asset with default celestial body configurations.
        /// Uses the correct Stars and Moons lists structure from SeasonalData.
        /// </summary>
        private static void ConfigureSeasonalData(SeasonalData seasonalData, string seasonName, SetupConfig config, int seasonIndex)
        {
            Debug.Log($"[Sol Setup] Configuring SeasonalData for season: {seasonName}");

            SerializedObject serializedSeasonalData = new SerializedObject(seasonalData);

            // Get the correct property names: "stars" and "moons"
            SerializedProperty starsProp = serializedSeasonalData.FindProperty("stars");
            SerializedProperty moonsProp = serializedSeasonalData.FindProperty("moons");
            
            if (starsProp == null || !starsProp.isArray)
            {
                Debug.LogError($"[Sol Setup] Could not find 'stars' array in SeasonalData!");
                return;
            }
            
            if (moonsProp == null || !moonsProp.isArray)
            {
                Debug.LogError($"[Sol Setup] Could not find 'moons' array in SeasonalData!");
                return;
            }

            // Clear existing
            starsProp.ClearArray();
            moonsProp.ClearArray();

            int starIndex = 0;
            int moonIndex = 0;

            // Add suns to stars list
            foreach (var sunConfig in config.suns)
            {
                if (!sunConfig.active) continue;
                
                starsProp.InsertArrayElementAtIndex(starIndex);
                SerializedProperty starProp = starsProp.GetArrayElementAtIndex(starIndex);
                
                ConfigureCelestialBodyInSeasonalData(starProp, sunConfig, seasonIndex, config.numberOfSeasons);
                starIndex++;
            }

            // Add moons to moons list
            foreach (var moonConfig in config.moons)
            {
                if (!moonConfig.active) continue;
                
                moonsProp.InsertArrayElementAtIndex(moonIndex);
                SerializedProperty moonProp = moonsProp.GetArrayElementAtIndex(moonIndex);
                
                ConfigureCelestialBodyInSeasonalData(moonProp, moonConfig, seasonIndex, config.numberOfSeasons);
                moonIndex++;
            }

            serializedSeasonalData.ApplyModifiedProperties();
            Debug.Log($"[Sol Setup] Configured {starIndex} stars and {moonIndex} moons for {seasonName}");
        }

        /// <summary>
        /// Configures a single CelestialBody in SeasonalData (the data class, not the component).
        /// </summary>
        private static void ConfigureCelestialBodyInSeasonalData(SerializedProperty bodyProp, CelestialBodyConfig config, int seasonIndex, int totalSeasons)
        {
            SerializedProperty nameProp = bodyProp.FindPropertyRelative("name");
            SerializedProperty activeProp = bodyProp.FindPropertyRelative("active");
            SerializedProperty yAxisEnabledProp = bodyProp.FindPropertyRelative("yAxisEnabled");
            SerializedProperty yAxisSpeedProp = bodyProp.FindPropertyRelative("yAxisSpeed");
            SerializedProperty yAxisOverrideProp = bodyProp.FindPropertyRelative("yAxisOverrideSpeed");
            SerializedProperty orbitalAngleProp = bodyProp.FindPropertyRelative("orbitalAngle");
            SerializedProperty baseElevationProp = bodyProp.FindPropertyRelative("baseElevation");
            SerializedProperty orbitalPeriodProp = bodyProp.FindPropertyRelative("orbitalPeriod");
            SerializedProperty phaseOffsetProp = bodyProp.FindPropertyRelative("phaseOffset");

            if (nameProp != null) nameProp.stringValue = config.name;
            if (activeProp != null) activeProp.boolValue = config.active;
            if (yAxisEnabledProp != null) yAxisEnabledProp.boolValue = config.yAxisEnabled;
            if (yAxisSpeedProp != null) yAxisSpeedProp.floatValue = config.yAxisSpeed;
            if (yAxisOverrideProp != null) yAxisOverrideProp.boolValue = config.yAxisOverrideSpeed;
            
            // Vary orbital angle slightly by season for realistic variation
            float seasonalVariation = (seasonIndex / (float)totalSeasons) * 5f - 2.5f; // ±2.5 degrees
            if (orbitalAngleProp != null) orbitalAngleProp.floatValue = config.orbitalAngle + seasonalVariation;
            
            if (baseElevationProp != null) baseElevationProp.floatValue = config.baseElevation;
            if (orbitalPeriodProp != null) orbitalPeriodProp.floatValue = config.orbitalPeriod;
            if (phaseOffsetProp != null) phaseOffsetProp.floatValue = config.phaseOffset;
        }

        #endregion

        #region TimeManager Creation
        
        /// <summary>
        /// Creates and configures TimeManager in the scene.
        /// Registers it with ServiceLocator.
        /// </summary>
        private static void CreateTimeManagerInScene(WorldTimeData worldTimeData)
        {
            Debug.Log("[Sol Setup] === Creating TimeManager ===");

            // Check if TimeManager already exists
            TimeManager existingTimeManager = Object.FindObjectOfType<TimeManager>();
    
            if (existingTimeManager != null)
            {
                Debug.LogWarning("[Sol Setup] TimeManager already exists in scene. Updating configuration...");
                ConfigureTimeManager(existingTimeManager, worldTimeData);
                RegisterTimeManagerWithServiceLocator(existingTimeManager);
                return;
            }

            // Create new GameObject for TimeManager
            GameObject timeManagerObject = new GameObject("TimeManager");
            TimeManager timeManager = timeManagerObject.AddComponent<TimeManager>();

            if (timeManager == null)
            {
                Debug.LogError("[Sol Setup] Failed to add TimeManager component!");
                return;
            }

            ConfigureTimeManager(timeManager, worldTimeData);
    
            // Register with ServiceLocator
            RegisterTimeManagerWithServiceLocator(timeManager);

            // Register undo
            Undo.RegisterCreatedObjectUndo(timeManagerObject, "Create TimeManager");

            Debug.Log("[Sol Setup] TimeManager created and configured");
        }
        
        /// <summary>
        /// Registers TimeManager with ServiceLocator.
        /// </summary>
        private static void RegisterTimeManagerWithServiceLocator(TimeManager timeManager)
        {
            ServiceLocator serviceLocator = Object.FindObjectOfType<ServiceLocator>();
    
            if (serviceLocator == null)
            {
                Debug.LogError("[Sol Setup] ServiceLocator not found! Cannot register TimeManager.");
                return;
            }

            SerializedObject serializedServiceLocator = new SerializedObject(serviceLocator);
            SerializedProperty timeManagerProp = serializedServiceLocator.FindProperty("_timeManager");
    
            if (timeManagerProp != null)
            {
                timeManagerProp.objectReferenceValue = timeManager;
                serializedServiceLocator.ApplyModifiedProperties();
                EditorUtility.SetDirty(serviceLocator);
                Debug.Log("[Sol Setup] TimeManager registered with ServiceLocator");
            }
            else
            {
                Debug.LogWarning("[Sol Setup] Could not find 'timeManager' property in ServiceLocator. Register manually.");
            }
        }
        
        /// <summary>
        /// Configures TimeManager with WorldTimeData reference and default values.
        /// Automatically discovers and assigns GameEvent assets using flexible search.
        /// </summary>
        private static void ConfigureTimeManager(TimeManager timeManager, WorldTimeData worldTimeData)
        {
            if (timeManager == null)
            {
                Debug.LogError("[Sol Setup] TimeManager is null!");
                return;
            }

            SerializedObject serializedTimeManager = new SerializedObject(timeManager);
            
            // Set WorldTimeData reference
            SerializedProperty worldTimeDataProp = serializedTimeManager.FindProperty("_worldTimeData");
            
            if (worldTimeDataProp != null && worldTimeData != null)
            {
                worldTimeDataProp.objectReferenceValue = worldTimeData;
                Debug.Log("[Sol Setup] TimeManager._worldTimeData set");
            }
            else
            {
                Debug.LogWarning("[Sol Setup] Could not find '_worldTimeData' property in TimeManager");
            }

            // Set default starting values
            SerializedProperty startingTimeOfDayProp = serializedTimeManager.FindProperty("_startingTimeOfDay");
            SerializedProperty startingDayProp = serializedTimeManager.FindProperty("_startingDay");
            SerializedProperty startingYearProp = serializedTimeManager.FindProperty("_startingYear");
            SerializedProperty timeScaleProp = serializedTimeManager.FindProperty("_timeScale");
            SerializedProperty isPausedProp = serializedTimeManager.FindProperty("_isPaused");

            if (startingTimeOfDayProp != null) startingTimeOfDayProp.floatValue = 0.25f; // 6 AM
            if (startingDayProp != null) startingDayProp.intValue = 1;
            if (startingYearProp != null) startingYearProp.intValue = 1;
            if (timeScaleProp != null) timeScaleProp.floatValue = 1f;
            if (isPausedProp != null) isPausedProp.boolValue = false;

            // Discover and assign GameEvent assets
            SerializedProperty onDayChangedProp = serializedTimeManager.FindProperty("_onDayChanged");
            SerializedProperty onSeasonChangedProp = serializedTimeManager.FindProperty("_onSeasonChanged");
            SerializedProperty onYearChangedProp = serializedTimeManager.FindProperty("_onYearChanged");

            if (onDayChangedProp != null)
            {
                GameEvent dayEvent = FindGameEventByName("OnDayChanged");
                if (dayEvent != null)
                {
                    onDayChangedProp.objectReferenceValue = dayEvent;
                    Debug.Log($"[Sol Setup] Assigned OnDayChanged event: {AssetDatabase.GetAssetPath(dayEvent)}");
                }
                else
                {
                    Debug.LogWarning("[Sol Setup] Could not find 'OnDayChanged' GameEvent asset. Assign manually.");
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

            if (onYearChangedProp != null)
            {
                GameEvent yearEvent = FindGameEventByName("OnYearChanged");
                if (yearEvent != null)
                {
                    onYearChangedProp.objectReferenceValue = yearEvent;
                    Debug.Log($"[Sol Setup] Assigned OnYearChanged event: {AssetDatabase.GetAssetPath(yearEvent)}");
                }
                else
                {
                    Debug.LogWarning("[Sol Setup] Could not find 'OnYearChanged' GameEvent asset. Assign manually.");
                }
            }

            serializedTimeManager.ApplyModifiedProperties();
            EditorUtility.SetDirty(timeManager);
            
            Debug.Log("[Sol Setup] TimeManager configured with default values and GameEvents");
        }
        
        /// <summary>
        /// Finds a GameEvent asset by searching the entire project for matching filename.
        /// Flexible search - works regardless of folder structure (Assets/Sol, Plugins/Sol, etc.)
        /// </summary>
        /// <param name="eventName">Name of the GameEvent asset (without .asset extension)</param>
        /// <returns>First matching GameEvent found, or null if not found</returns>
        private static GameEvent FindGameEventByName(string eventName)
        {
            // Search for all GameEvent assets in the project
            string[] guids = AssetDatabase.FindAssets($"{eventName} t:GameEvent");
    
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[Sol Setup] GameEvent '{eventName}' not found in project.");
                return null;
            }

            if (guids.Length > 1)
            {
                Debug.LogWarning($"[Sol Setup] Multiple GameEvents named '{eventName}' found. Using first match.");
            }

            // Load the first match
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameEvent gameEvent = AssetDatabase.LoadAssetAtPath<GameEvent>(path);
    
            if (gameEvent != null)
            {
                Debug.Log($"[Sol Setup] Found GameEvent '{eventName}' at: {path}");
            }
            else
            {
                Debug.LogError($"[Sol Setup] Failed to load GameEvent at path: {path}");
            }

            return gameEvent;
        }

        #endregion

        #region Celestial Light Creation

        /// <summary>
        /// Creates all celestial lights (suns and moons) based on configuration.
        /// Each gets a CelestialRotator component configured with wizard settings.
        /// </summary>
        private static void CreateAllCelestialLights(SetupConfig config)
        {
            Debug.Log("[Sol Setup] === Creating All Celestial Lights ===");

            // Find or create parent container
            Transform celestialContainer = GetOrCreateCelestialContainer();

            // Track which sun should cast shadows (only the first active one)
            bool shadowCasterAssigned = false;

            // Create sun lights
            foreach (var sunConfig in config.suns)
            {
                if (!sunConfig.active) continue;
                
                bool shouldCastShadows = sunConfig.castShadows && !shadowCasterAssigned;
                
                Light sunLight = CreateCelestialLight(sunConfig, shouldCastShadows, false, celestialContainer);
                
                if (sunLight != null)
                {
                    if (shouldCastShadows) shadowCasterAssigned = true;
                    Debug.Log($"[Sol Setup] Created sun light: {sunConfig.name} (Shadows: {shouldCastShadows})");
                }
            }

            // Create moon lights (never cast shadows)
            foreach (var moonConfig in config.moons)
            {
                if (!moonConfig.active) continue;
                
                Light moonLight = CreateCelestialLight(moonConfig, false, true, celestialContainer);
                
                if (moonLight != null)
                {
                    Debug.Log($"[Sol Setup] Created moon light: {moonConfig.name}");
                }
            }

            Debug.Log("[Sol Setup] === Celestial Light Creation Complete ===");
        }
        
        /// <summary>
        /// Configures celestial body shading AFTER the sky volume has been created.
        /// Call this AFTER CreateSkyAndFogVolume() to ensure the property is available.
        /// Uses configuration lists to accurately determine sun vs moon - NO name detection.
        /// </summary>
        private static void ConfigureCelestialBodyShadingForAllLights(SetupConfig config)
        {
            Debug.Log("[Sol Setup] === Configuring Celestial Body Shading ===");
            
            // Find the celestial container
            Transform celestialContainer = GameObject.Find("Celestial Bodies")?.transform;
            
            if (celestialContainer == null)
            {
                Debug.LogWarning("[Sol Setup] 'Celestial Bodies' container not found. Skipping shading configuration.");
                return;
            }
            
            // Get all lights under the celestial container
            Light[] celestialLights = celestialContainer.GetComponentsInChildren<Light>();
            
            Debug.Log($"[Sol Setup] Found {celestialLights.Length} celestial lights to configure");
            
            // Track configured lights to ensure we process all of them
            int configuredSuns = 0;
            int configuredMoons = 0;
            
            foreach (var light in celestialLights)
            {
                if (light.type != LightType.Directional) continue;
                
                HDAdditionalLightData hdLightData = light.GetComponent<HDAdditionalLightData>();
                
                if (hdLightData == null)
                {
                    Debug.LogWarning($"[Sol Setup] {light.name} has no HDAdditionalLightData");
                    continue;
                }
                
                // Determine if this is a moon by checking WHICH LIST it came from
                bool isMoon = IsLightInMoonList(light.name, config);
                
                // CORRECT LOGIC: Moons reflect, suns emit
                var targetSource = isMoon 
                    ? HDAdditionalLightData.CelestialBodyShadingSource.ReflectSunLight  // ✓ MOONS REFLECT
                    : HDAdditionalLightData.CelestialBodyShadingSource.Emission;        // ✓ SUNS EMIT
                
                hdLightData.celestialBodyShadingSource = targetSource;
                EditorUtility.SetDirty(hdLightData);
                
                string bodyType = isMoon ? "Moon" : "Sun/Star";
                if (isMoon) configuredMoons++;
                else configuredSuns++;
                
                Debug.Log($"[Sol Setup] ✓ Configured '{light.name}' as {bodyType} → {targetSource}");
            }
            
            Debug.Log($"[Sol Setup] Configured {configuredSuns} suns and {configuredMoons} moons");
            Debug.Log("[Sol Setup] === Celestial Body Shading Configuration Complete ===");
        }
        
        /// <summary>
        /// Determines if a light is a moon by checking if its name exists in the config.moons list.
        /// This is the ONLY source of truth - no name pattern matching.
        /// </summary>
        private static bool IsLightInMoonList(string lightName, SetupConfig config)
        {
            // Check if this light name matches ANY active moon in the moons list
            foreach (var moonConfig in config.moons)
            {
                if (moonConfig.active && lightName.Equals(moonConfig.name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true; // ✓ Found in moons list = IS a moon
                }
            }
    
            // Not in moons list = must be a sun (or error case)
            return false;
        }

        /// <summary>
        /// Gets or creates the celestial container GameObject.
        /// </summary>
        private static Transform GetOrCreateCelestialContainer()
        {
            GameObject container = GameObject.Find("Celestial Bodies");
            
            if (container == null)
            {
                container = new GameObject("Celestial Bodies");
                Undo.RegisterCreatedObjectUndo(container, "Create Celestial Bodies Container");
                Debug.Log("[Sol Setup] Created Celestial Bodies container");
            }
            
            return container.transform;
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

            // Try to add HDAdditionalLightData component
            TryAddHDAdditionalLightData(light, config);

            Undo.RegisterCreatedObjectUndo(lightObj, $"Create {config.name} Light");
            return light;
        }
        
        /// <summary>
        /// Attempts to add and configure HDAdditionalLightData component (HDRP).
        /// </summary>
        private static void TryAddHDAdditionalLightData(Light light, CelestialBodyConfig config)
        {
            if (light == null) return;

            // Get HDRP HDAdditionalLightData type
            var hdLightDataType = System.Type.GetType("UnityEngine.Rendering.HighDefinition.HDAdditionalLightData, Unity.RenderPipelines.HighDefinition.Runtime");
            
            if (hdLightDataType == null)
            {
                Debug.LogWarning($"[Sol Setup] HDRP not detected. Skipping advanced light configuration for '{config.name}'");
                return;
            }

            // Check if component already exists
            Component hdLightData = light.GetComponent(hdLightDataType);
            
            // If it doesn't exist, try to add it
            if (hdLightData == null)
            {
                try
                {
                    // GameObject.AddComponent(Type) signature
                    hdLightData = light.gameObject.AddComponent(hdLightDataType);
                    Debug.Log($"[Sol Setup] ✓ Added HDAdditionalLightData to '{config.name}'");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Sol Setup] Failed to add HDAdditionalLightData to '{config.name}': {e.Message}");
                    return;
                }
            }

            // Configure HDRP-specific properties
            ConfigureHDLightProperties(hdLightData, config);
        }
        
        /// <summary>
        /// Configures HDRP-specific light properties using reflection.
        /// </summary>
        private static void ConfigureHDLightProperties(Component hdLightData, CelestialBodyConfig config)
        {
            if (hdLightData == null) return;

            var hdLightDataType = hdLightData.GetType();

            try
            {
                // Set Color Temperature (if property exists)
                SetPropertyIfExists(hdLightDataType, hdLightData, "useColorTemperature", true);
                SetPropertyIfExists(hdLightDataType, hdLightData, "colorTemperature", config.lightTemperature);

                // Set Intensity Unit to Lux for directional lights (HDRP default)
                // LightUnit enum: Lux = 1 for directional lights
                var lightUnitEnumType = System.Type.GetType("UnityEngine.Rendering.HighDefinition.LightUnit, Unity.RenderPipelines.HighDefinition.Runtime");
                if (lightUnitEnumType != null)
                {
                    var luxValue = System.Enum.ToObject(lightUnitEnumType, 1); // Lux = 1
                    SetPropertyIfExists(hdLightDataType, hdLightData, "lightUnit", luxValue);
                }

                // Enable sky interaction for physically-based sky
                SetPropertyIfExists(hdLightDataType, hdLightData, "interactsWithSky", true);

                Debug.Log($"[Sol Setup] ✓ Configured HDRP properties for '{config.name}' (Temp: {config.lightTemperature}K, Intensity: {config.lightIntensity} lux)");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Sol Setup] Error configuring HDRP properties for '{config.name}': {e.Message}");
            }
        }
        
        /// <summary>
        /// Sets a property value using SerializedObject (Editor-safe way).
        /// Returns true if property was found and set successfully.
        /// </summary>
        private static bool SetPropertyIfExists(System.Type type, object instance, string propertyName, object value)
        {
            if (!(instance is Object unityObject))
            {
                Debug.LogError($"[Sol Setup] Instance is not a UnityEngine.Object, cannot use SerializedObject");
                return false;
            }

            try
            {
                SerializedObject serializedObject = new SerializedObject(unityObject);
        
                // Try to find the property with common naming conventions
                SerializedProperty prop = serializedObject.FindProperty(propertyName);
        
                if (prop == null)
                {
                    // Try with m_ prefix
                    prop = serializedObject.FindProperty("m_" + propertyName);
                }
        
                if (prop == null)
                {
                    // Try with capital first letter
                    string capitalizedName = char.ToUpper(propertyName[0]) + propertyName.Substring(1);
                    prop = serializedObject.FindProperty(capitalizedName);
                }
        
                if (prop == null)
                {
                    // Property doesn't exist
                    return false;
                }

                // Set the value based on property type
                bool wasSet = SetSerializedPropertyValue(prop, value);
        
                if (wasSet)
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(unityObject);
                    return true;
                }
        
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Sol Setup] Error setting property '{propertyName}': {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Helper to set a SerializedProperty value based on its type.
        /// </summary>
        private static bool SetSerializedPropertyValue(SerializedProperty prop, object value)
        {
            try
            {
                switch (prop.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        if (value is System.Enum enumValue)
                        {
                            prop.intValue = System.Convert.ToInt32(enumValue);
                        }
                        else
                        {
                            prop.intValue = System.Convert.ToInt32(value);
                        }
                        return true;
                        
                    case SerializedPropertyType.Boolean:
                        prop.boolValue = System.Convert.ToBoolean(value);
                        return true;
                        
                    case SerializedPropertyType.Float:
                        prop.floatValue = System.Convert.ToSingle(value);
                        return true;
                        
                    case SerializedPropertyType.String:
                        prop.stringValue = value.ToString();
                        return true;
                        
                    case SerializedPropertyType.Color:
                        if (value is Color color)
                        {
                            prop.colorValue = color;
                            return true;
                        }
                        break;
                        
                    case SerializedPropertyType.ObjectReference:
                        if (value is Object objRef)
                        {
                            prop.objectReferenceValue = objRef;
                            return true;
                        }
                        break;
                        
                    case SerializedPropertyType.Enum:
                        if (value is System.Enum enumVal)
                        {
                            prop.enumValueIndex = System.Convert.ToInt32(enumVal);
                        }
                        else
                        {
                            prop.enumValueIndex = System.Convert.ToInt32(value);
                        }
                        return true;
                        
                    default:
                        Debug.LogWarning($"[Sol Setup] Unsupported SerializedProperty type: {prop.propertyType}");
                        return false;
                }
                
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Sol Setup] Error setting SerializedProperty value: {e.Message}");
                return false;
            }
        }
        
        
        /// <summary>
        /// Configures a CelestialRotator component with all required settings.
        /// Uses safe property setting with type validation.
        /// </summary>
        private static void ConfigureCelestialRotator(CelestialRotator rotator, CelestialBodyConfig config, bool isMoon)
        {
            if (rotator == null)
            {
                Debug.LogError("[Sol Setup] CelestialRotator is null!");
                return;
            }

            SerializedObject serializedRotator = new SerializedObject(rotator);

            // Find all properties
            SerializedProperty isMoonProp = serializedRotator.FindProperty("isMoon");
            SerializedProperty baseRotationProp = serializedRotator.FindProperty("baseRotation");
            SerializedProperty baseRotationYProp = serializedRotator.FindProperty("baseRotationY");
            SerializedProperty celestialBodyNameProp = serializedRotator.FindProperty("celestialBodyName");
            SerializedProperty yAxisEnabledProp = serializedRotator.FindProperty("yAxisEnabled");
            SerializedProperty yAxisSpeedProp = serializedRotator.FindProperty("yAxisSpeed");
            SerializedProperty yAxisOverrideProp = serializedRotator.FindProperty("yAxisOverrideSpeed");

            // Safely set values with type checking
            if (isMoonProp != null && isMoonProp.propertyType == SerializedPropertyType.Boolean) 
            {
                isMoonProp.boolValue = isMoon;
                Debug.Log($"[Sol Setup] Set isMoon={isMoon} for {config.name}");
            }
            else if (isMoonProp == null)
            {
                Debug.LogWarning($"[Sol Setup] Property 'isMoon' not found in CelestialRotator");
            }
            
            if (baseRotationProp != null && baseRotationProp.propertyType == SerializedPropertyType.Float)
            {
                baseRotationProp.floatValue = config.baseElevation;
                Debug.Log($"[Sol Setup] Set baseRotation={config.baseElevation} for {config.name}");
            }
            else if (baseRotationProp == null)
            {
                Debug.LogWarning($"[Sol Setup] Property 'baseRotation' not found in CelestialRotator");
            }
            else
            {
                Debug.LogWarning($"[Sol Setup] Property 'baseRotation' is type {baseRotationProp.propertyType}, expected Float");
            }
            
            if (baseRotationYProp != null && baseRotationYProp.propertyType == SerializedPropertyType.Float) 
            {
                baseRotationYProp.floatValue = config.phaseOffset;
                Debug.Log($"[Sol Setup] Set baseRotationY={config.phaseOffset} for {config.name}");
            }
            else if (baseRotationYProp == null)
            {
                Debug.LogWarning($"[Sol Setup] Property 'baseRotationY' not found in CelestialRotator");
            }
            else
            {
                Debug.LogWarning($"[Sol Setup] Property 'baseRotationY' is type {baseRotationYProp.propertyType}, expected Float");
            }
            
            if (celestialBodyNameProp != null && celestialBodyNameProp.propertyType == SerializedPropertyType.String) 
            {
                celestialBodyNameProp.stringValue = config.name;
            }
            
            if (yAxisEnabledProp != null && yAxisEnabledProp.propertyType == SerializedPropertyType.Boolean) 
            {
                yAxisEnabledProp.boolValue = config.yAxisEnabled;
            }
            
            if (yAxisSpeedProp != null && yAxisSpeedProp.propertyType == SerializedPropertyType.Float) 
            {
                yAxisSpeedProp.floatValue = config.yAxisSpeed;
            }
            else if (yAxisSpeedProp != null)
            {
                Debug.LogWarning($"[Sol Setup] Property 'yAxisSpeed' is type {yAxisSpeedProp.propertyType}, expected Float");
            }
            
            if (yAxisOverrideProp != null && yAxisOverrideProp.propertyType == SerializedPropertyType.Boolean) 
            {
                yAxisOverrideProp.boolValue = config.yAxisOverrideSpeed;
            }

            serializedRotator.ApplyModifiedProperties();
            
            Debug.Log($"[Sol Setup] CelestialRotator configured for {config.name}");
        }

        #endregion
        
        #region Sky and Fog Creation

        /// <summary>
        /// Creates HDRP Sky and Fog Volume in the scene.
        /// Always uses the preconfigured SolDefaultSkyProfile asset.
        /// User can change the profile later if desired.
        /// </summary>
        private static void CreateSkyAndFogVolume(SetupConfig config)
        {
            Debug.Log("[Sol Setup] === Creating Sky and Fog Volume ===");

            // Check if volume already exists
            GameObject existingVolume = GameObject.Find("Sky and Fog Volume");
            if (existingVolume != null)
            {
                Debug.LogWarning("[Sol Setup] Sky and Fog Volume already exists. Skipping creation.");
                return;
            }

            // STEP 1: Create GameObject
            GameObject volumeObject = new GameObject("Sky and Fog Volume");
            
            // STEP 2: Register with Undo IMMEDIATELY (creates it in scene)
            Undo.RegisterCreatedObjectUndo(volumeObject, "Create Sky and Fog Volume");
            Debug.Log("[Sol Setup] ✓ Sky and Fog Volume GameObject created in scene");

            // STEP 3: NOW configure it (after it exists in scene)
            bool volumeConfigured = TryAddAndConfigureVolumeComponent(volumeObject);

            if (!volumeConfigured)
            {
                Debug.LogWarning("[Sol Setup] Could not fully configure HDRP Volume component.");
                Debug.LogWarning("[Sol Setup] GameObject created - add Volume component and profile manually.");
            }
            else
            {
                Debug.Log("[Sol Setup] ✓ Sky and Fog Volume fully configured");
            }
        }
        
        /// <summary>
        /// Adds HDRP Volume component and assigns the SolDefaultSkyProfile.
        /// </summary>
        private static bool TryAddAndConfigureVolumeComponent(GameObject volumeObject)
        {
            Debug.Log("[Sol Setup] Configuring Volume component...");

            if (volumeObject == null)
            {
                Debug.LogError("[Sol Setup] volumeObject is null!");
                return false;
            }

            // Find Volume type
            System.Type volumeType = System.Type.GetType("UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime");
            if (volumeType == null)
            {
                volumeType = System.Type.GetType("UnityEngine.Rendering.Volume, UnityEngine.CoreModule");
            }

            if (volumeType == null)
            {
                Debug.LogWarning("[Sol Setup] Volume type not found. HDRP may not be installed.");
                return false;
            }

            // Add Volume component
            Component volume = volumeObject.AddComponent(volumeType);
            if (volume == null)
            {
                Debug.LogError("[Sol Setup] Failed to add Volume component!");
                return false;
            }

            Debug.Log($"[Sol Setup] ✓ Added Volume component");
            Undo.RegisterCreatedObjectUndo(volume, "Add Volume Component");

            // Set isGlobal
            SerializedObject serializedVolume = new SerializedObject(volume);
            SerializedProperty isGlobalProp = serializedVolume.FindProperty("m_IsGlobal") 
                                            ?? serializedVolume.FindProperty("isGlobal");
            
            if (isGlobalProp != null)
            {
                isGlobalProp.boolValue = true;
                serializedVolume.ApplyModifiedProperties();
                Debug.Log("[Sol Setup] ✓ Volume.isGlobal = true");
            }

            // Find and assign profile
            Object profileAsset = FindSolDefaultSkyProfile();
            if (profileAsset == null)
            {
                Debug.LogError("[Sol Setup] SolDefaultSkyProfile not found!");
                return false;
            }

            // Assign profile
            SerializedProperty sharedProfileProp = serializedVolume.FindProperty("sharedProfile")
                                                ?? serializedVolume.FindProperty("m_Profile")
                                                ?? serializedVolume.FindProperty("profile");

            if (sharedProfileProp != null)
            {
                sharedProfileProp.objectReferenceValue = profileAsset;
                serializedVolume.ApplyModifiedProperties();
                Debug.Log($"[Sol Setup] ✓ Profile assigned: {profileAsset.name}");
            }
            else
            {
                Debug.LogError("[Sol Setup] Could not find profile property on Volume!");
                return false;
            }

            EditorUtility.SetDirty(volume);
            EditorUtility.SetDirty(volumeObject);
            AssetDatabase.SaveAssets();

            return true;
        }

        /// <summary>
        /// Finds the SolDefaultSkyProfile asset anywhere in the project.
        /// Searches entire project structure - works regardless of folder organization.
        /// </summary>
        private static Object FindSolDefaultSkyProfile()
        {
            Debug.Log("[Sol Setup] Searching for SolDefaultSkyProfile...");

            // Search for VolumeProfile assets named "SolDefaultSkyProfile"
            string[] guids = AssetDatabase.FindAssets("SolDefaultSkyProfile t:VolumeProfile");

            if (guids.Length == 0)
            {
                Debug.LogWarning("[Sol Setup] No VolumeProfile named 'SolDefaultSkyProfile' found in project.");
                
                // Try a broader search without the exact name
                guids = AssetDatabase.FindAssets("SolDefault t:VolumeProfile");
                
                if (guids.Length == 0)
                {
                    return null;
                }
                
                Debug.LogWarning("[Sol Setup] Found similar profile names:");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Debug.LogWarning($"  - {path}");
                }
                
                return null;
            }

            if (guids.Length > 1)
            {
                Debug.LogWarning($"[Sol Setup] Multiple VolumeProfiles named 'SolDefaultSkyProfile' found:");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    Debug.LogWarning($"  - {path}");
                }
                Debug.LogWarning("[Sol Setup] Using first match.");
            }

            // Load the profile asset
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            Object profile = AssetDatabase.LoadAssetAtPath<Object>(assetPath);

            if (profile != null)
            {
                Debug.Log($"[Sol Setup] ✓ Found SolDefaultSkyProfile at: {assetPath}");
                Debug.Log($"[Sol Setup]   Asset type: {profile.GetType().Name}");
            }
            else
            {
                Debug.LogError($"[Sol Setup] ✗ Failed to load asset at: {assetPath}");
            }

            return profile;
        }

        #endregion

        #region Demo Content Creation

        /// <summary>
        /// Creates demo content to showcase the Sol system.
        /// </summary>
        private static void CreateDemoContent()
        {
            Debug.Log("[Sol Setup] === Creating Demo Content ===");

            // Create a simple ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Demo Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10);
            
            Undo.RegisterCreatedObjectUndo(ground, "Create Demo Ground");

            // Create a demo cube
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Demo Cube";
            cube.transform.position = new Vector3(0, 0.5f, 0);
            
            Undo.RegisterCreatedObjectUndo(cube, "Create Demo Cube");

            Debug.Log("[Sol Setup] Demo content created");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper method to ensure a folder path exists, creating parent folders as needed.
        /// </summary>
        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            Debug.Log($"[Sol Setup] Creating folder structure: {folderPath}");
            
            string[] pathParts = folderPath.Split('/');
            string currentPath = pathParts[0]; // Start with "Assets"
            
            for (int i = 1; i < pathParts.Length; i++)
            {
                string nextPath = currentPath + "/" + pathParts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                {
                    string folderName = pathParts[i];
                    AssetDatabase.CreateFolder(currentPath, folderName);
                    Debug.Log($"[Sol Setup] Created folder: {nextPath}");
                }
                currentPath = nextPath;
            }
            
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Gets a color for a season based on its index.
        /// </summary>
        private static Color GetSeasonColor(int seasonIndex, int totalSeasons)
        {
            if (totalSeasons == 4)
            {
                switch (seasonIndex)
                {
                    case 0: return new Color(0.4f, 1f, 0.4f);    // Spring - Green
                    case 1: return new Color(1f, 1f, 0.4f);      // Summer - Yellow
                    case 2: return new Color(1f, 0.6f, 0.2f);    // Autumn - Orange
                    case 3: return new Color(0.7f, 0.9f, 1f);    // Winter - Light Blue
                }
            }
            
            // For other season counts, generate a color based on position in cycle
            float hue = seasonIndex / (float)totalSeasons;
            return Color.HSVToRGB(hue, 0.6f, 0.9f);
        }

        /// <summary>
        /// Gets a color for a month based on its index.
        /// </summary>
        private static Color GetMonthColor(int monthIndex)
        {
            // Cycle through a pleasing color palette
            float hue = (monthIndex / 12f) * 0.8f; // 0.8 to avoid wrapping back to red
            return Color.HSVToRGB(hue, 0.4f, 0.95f);
        }

        #endregion
    }
}
