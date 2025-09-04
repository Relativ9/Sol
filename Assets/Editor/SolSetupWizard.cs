using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Sol.Editor
{
    /// <summary>
    /// Setup wizard for the Sol Time & Celestial System.
    /// Provides one-click setup for new projects with sensible defaults and sample configurations.
    /// </summary>
    public class SolSetupWizard : EditorWindow
    {
        #region Window Management
        
        [MenuItem("Tools/Sol/Setup Wizard", priority = 1)]
        public static void ShowWindow()
        {
            SolSetupWizard window = GetWindow<SolSetupWizard>("Sol Setup Wizard");
            window.minSize = new Vector2(500, 600);
            window.maxSize = new Vector2(500, 800);
            window.Show();
        }

        #endregion

        #region Setup Configuration

        [System.Serializable]
        public class SetupConfig
        {
            [Header("Scene Setup")]
            public bool createTimeManager = true;
            public bool createWorldTimeData = true;
            public bool createDirectionalLight = true;

            [Header("Seasonal Data")]
            public bool createSeasonalData = true;
            public int numberOfSeasons = 4;
            public string[] seasonNames = { "Spring", "Summer", "Autumn", "Winter" };

            [Header("Celestial Bodies")]
            public bool createSun = true;
            public bool createMoon = true;
            public string sunName = "Sol";
            public string moonName = "Luna";

            [Header("Demo Content")]
            public bool createDemoScene = false;

            [Header("Asset Paths")]
            public string dataFolderPath = "Assets/Sol/Data";
            public string prefabFolderPath = "Assets/Sol/Prefabs";
        }

        #endregion

        #region Private Fields

        private SetupConfig config = new SetupConfig();
        private Vector2 scrollPosition;
        private bool showAdvancedOptions = false;
        private GUIStyle headerStyle;
        private GUIStyle boxStyle;
        private bool isSetupInProgress = false;
        private string setupStatus = "";

        #endregion

        #region GUI Drawing

        private void OnEnable()
        {
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        private void OnGUI()
        {
            if (headerStyle == null) InitializeStyles();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawSceneSetupSection();
            EditorGUILayout.Space(10);

            DrawSeasonalDataSection();
            EditorGUILayout.Space(10);

            DrawCelestialBodiesSection();
            EditorGUILayout.Space(10);

            DrawDemoContentSection();
            EditorGUILayout.Space(10);

            if (showAdvancedOptions)
            {
                DrawAdvancedOptionsSection();
                EditorGUILayout.Space(10);
            }

            DrawAdvancedToggle();
            EditorGUILayout.Space(10);

            DrawSetupButtons();

            if (isSetupInProgress)
            {
                DrawProgressSection();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            EditorGUILayout.LabelField("Sol Time & Celestial System", headerStyle);
            EditorGUILayout.Space(5);
            
            EditorGUILayout.HelpBox(
                "Welcome to Sol! This wizard will help you set up a complete time and celestial system in your scene. " +
                "Choose the components you want to create and click 'Setup Scene' to get started.",
                MessageType.Info
            );
            
            EditorGUILayout.EndVertical();
        }

        private void DrawSceneSetupSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Scene Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            config.createTimeManager = EditorGUILayout.Toggle(
                new GUIContent("Create TimeManager", "Creates the core TimeManager component that controls time progression"),
                config.createTimeManager
            );

            config.createWorldTimeData = EditorGUILayout.Toggle(
                new GUIContent("Create WorldTimeData", "Creates the WorldTimeData asset that defines day length, time scale, and other time settings"),
                config.createWorldTimeData
            );

            config.createDirectionalLight = EditorGUILayout.Toggle(
                new GUIContent("Create Directional Light (Sun)", "Creates a directional light configured as the sun with CelestialRotator"),
                config.createDirectionalLight
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawSeasonalDataSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Seasonal Data", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            config.createSeasonalData = EditorGUILayout.Toggle(
                new GUIContent("Create Seasonal Data", "Creates a SeasonalData asset with default seasonal configurations"),
                config.createSeasonalData
            );

            if (config.createSeasonalData)
            {
                EditorGUI.indentLevel++;
                
                config.numberOfSeasons = EditorGUILayout.IntSlider(
                    new GUIContent("Number of Seasons", "How many seasons to create (2-12)"),
                    config.numberOfSeasons, 2, 12
                );

                // Resize season names array if needed
                if (config.seasonNames.Length != config.numberOfSeasons)
                {
                    System.Array.Resize(ref config.seasonNames, config.numberOfSeasons);
                    for (int i = 0; i < config.seasonNames.Length; i++)
                    {
                        if (string.IsNullOrEmpty(config.seasonNames[i]))
                        {
                            config.seasonNames[i] = $"Season {i + 1}";
                        }
                    }
                }

                EditorGUILayout.LabelField("Season Names:", EditorStyles.miniBoldLabel);
                for (int i = 0; i < config.seasonNames.Length; i++)
                {
                    config.seasonNames[i] = EditorGUILayout.TextField($"Season {i + 1}", config.seasonNames[i]);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCelestialBodiesSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Celestial Bodies", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            config.createSun = EditorGUILayout.Toggle(
                new GUIContent("Create Sun", "Creates a sun celestial body configuration"),
                config.createSun
            );

            if (config.createSun)
            {
                EditorGUI.indentLevel++;
                config.sunName = EditorGUILayout.TextField("Sun Name", config.sunName);
                EditorGUI.indentLevel--;
            }

            config.createMoon = EditorGUILayout.Toggle(
                new GUIContent("Create Moon", "Creates a moon celestial body configuration with orbital mechanics"),
                config.createMoon
            );

            if (config.createMoon)
            {
                EditorGUI.indentLevel++;
                config.moonName = EditorGUILayout.TextField("Moon Name", config.moonName);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDemoContentSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Demo Content", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            config.createDemoScene = EditorGUILayout.Toggle(
                new GUIContent("Create Demo Scene", "Creates a separate demo scene showcasing the system features"),
                config.createDemoScene
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedOptionsSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Advanced Options", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            config.dataFolderPath = EditorGUILayout.TextField(
                new GUIContent("Data Folder Path", "Where to create SeasonalData and WorldTimeData assets"),
                config.dataFolderPath
            );

            config.prefabFolderPath = EditorGUILayout.TextField(
                new GUIContent("Prefab Folder Path", "Where to create prefab assets"),
                config.prefabFolderPath
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedToggle()
        {
            showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "Advanced Options", true);
        }

        private void DrawSetupButtons()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            
            EditorGUI.BeginDisabledGroup(isSetupInProgress);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Setup Scene", GUILayout.Height(30)))
            {
                PerformSetup();
            }
            
            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(30)))
            {
                ResetToDefaults();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
        }

        private void DrawProgressSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Setup Progress", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(setupStatus);
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Setup Logic

        private void PerformSetup()
        {
            isSetupInProgress = true;
            setupStatus = "Starting setup...";

            try
            {
                WorldTimeData worldTimeData = null;

                if (config.createWorldTimeData)
                {
                    setupStatus = "Creating WorldTimeData...";
                    worldTimeData = CreateWorldTimeData();
                }

                if (config.createTimeManager)
                {
                    setupStatus = "Creating TimeManager...";
                    CreateTimeManager(worldTimeData);
                }

                if (config.createSeasonalData)
                {
                    setupStatus = "Creating Seasonal Data...";
                    CreateSeasonalData();
                }

                if (config.createDirectionalLight)
                {
                    setupStatus = "Creating Directional Light...";
                    CreateDirectionalLight();
                }

                if (config.createDemoScene)
                {
                    setupStatus = "Creating Demo Scene...";
                    CreateDemoScene();
                }

                setupStatus = "Setup completed successfully!";
                
                EditorUtility.DisplayDialog("Sol Setup Complete", 
                    "Scene setup completed successfully! Your Sol Time & Celestial System is ready to use.", 
                    "OK");
            }
            catch (System.Exception e)
            {
                setupStatus = $"Setup failed: {e.Message}";
                EditorUtility.DisplayDialog("Setup Error", 
                    $"An error occurred during setup: {e.Message}", 
                    "OK");
            }
            finally
            {
                isSetupInProgress = false;
                Repaint();
            }
        }

        private WorldTimeData CreateWorldTimeData()
        {
            // Ensure data folder exists
            SolSetupUtilities.EnsureFolderExists(config.dataFolderPath);

            // Create WorldTimeData asset
            WorldTimeData worldTimeData = ScriptableObject.CreateInstance<WorldTimeData>();
            
            // Configure with sensible defaults
            ConfigureWorldTimeData(worldTimeData);

            // Save asset
            string assetPath = $"{config.dataFolderPath}/DefaultWorldTimeData.asset";
            AssetDatabase.CreateAsset(worldTimeData, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Sol Setup] WorldTimeData created at {assetPath}");
            return worldTimeData;
        }

        private void ConfigureWorldTimeData(WorldTimeData worldTimeData)
        {
            // Configure with sensible defaults - adjust based on your WorldTimeData structure
            // Example configuration:
            /*
            worldTimeData.dayLengthInSeconds = 300f; // 5 minute days
            worldTimeData.timeScale = 1f;
            worldTimeData.startHour = 6f; // Start at dawn
            worldTimeData.pauseOnStart = false;
            */
        }

        private void CreateTimeManager(WorldTimeData worldTimeData)
        {
            // Check if TimeManager already exists
            TimeManager existingTimeManager = FindObjectOfType<TimeManager>();
            if (existingTimeManager != null)
            {
                Debug.Log("[Sol Setup] TimeManager already exists in scene, updating configuration.");
                
                // Assign WorldTimeData if provided
                if (worldTimeData != null)
                {
                    // Use reflection or direct assignment based on your TimeManager implementation
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

        private void AssignWorldTimeDataToTimeManager(TimeManager timeManager, WorldTimeData worldTimeData)
        {
            // Use reflection to assign WorldTimeData - adjust based on your TimeManager implementation
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

        private void CreateSeasonalData()
        {
            // Ensure data folder exists
            SolSetupUtilities.EnsureFolderExists(config.dataFolderPath);

            // Create SeasonalData asset
            SeasonalData seasonalData = ScriptableObject.CreateInstance<SeasonalData>();
            
            // Configure seasonal data with wizard settings
            ConfigureSeasonalData(seasonalData);

            // Save asset
            string assetPath = $"{config.dataFolderPath}/DefaultSeasonalData.asset";
            AssetDatabase.CreateAsset(seasonalData, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Sol Setup] Seasonal data created at {assetPath}");
        }

        private void ConfigureSeasonalData(SeasonalData seasonalData)
        {
            // This method would configure the seasonal data based on the wizard settings
            // The exact implementation depends on your SeasonalData structure
            
            // Example configuration (adjust based on your actual SeasonalData implementation):
            /*
            seasonalData.seasons = new Season[config.numberOfSeasons];
            for (int i = 0; i < config.numberOfSeasons; i++)
            {
                seasonalData.seasons[i] = new Season
                {
                    name = config.seasonNames[i],
                    orbitalAngle = Mathf.Lerp(-23.5f, 23.5f, (float)i / (config.numberOfSeasons - 1)),
                    // Add other default season properties
                };
            }

            List<CelestialBody> celestialBodies = new List<CelestialBody>();

            if (config.createSun)
            {
                celestialBodies.Add(new CelestialBody
                {
                    name = config.sunName,
                    active = true,
                    yAxisEnabled = true,
                    yAxisSpeed = 1.0f,
                    baseElevation = 0f,
                    phaseOffset = 0f,
                    orbitalPeriod = 1f
                });
            }

            if (config.createMoon)
            {
                celestialBodies.Add(new CelestialBody
                {
                    name = config.moonName,
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
        }

        private void CreateDirectionalLight()
        {
            // Check if a directional light already exists
            Light existingLight = FindObjectOfType<Light>();
            if (existingLight != null && existingLight.type == LightType.Directional)
            {
                Debug.Log("[Sol Setup] Directional light already exists, adding CelestialRotator component.");
                
                // Add CelestialRotator if it doesn't exist
                if (existingLight.GetComponent<CelestialRotator>() == null)
                {
                    CelestialRotator existingRotator = existingLight.gameObject.AddComponent<CelestialRotator>();
                    // Configure rotator for sun
                    ConfigureSunRotator(existingRotator);
                }
                return;
            }

            // Create new directional light
            GameObject sunLightGO = new GameObject("Sun Light");
            Light sunLight = sunLightGO.AddComponent<Light>();
            
            // Configure light settings
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(1f, 0.95f, 0.8f); // Warm sunlight color
            sunLight.intensity = 1.0f;
            sunLight.shadows = LightShadows.Soft;

            // Add and configure CelestialRotator
            CelestialRotator newRotator = sunLightGO.AddComponent<CelestialRotator>();
            ConfigureSunRotator(newRotator);

            Debug.Log("[Sol Setup] Directional light with CelestialRotator created successfully.");
        }

        private void ConfigureSunRotator(CelestialRotator rotator)
        {
            // Use reflection or direct field access to configure the rotator
            // This assumes the fields are accessible - adjust based on your implementation
            
            var celestialBodyNameField = typeof(CelestialRotator).GetField("celestialBodyName", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            celestialBodyNameField?.SetValue(rotator, config.sunName);

            var isMoonField = typeof(CelestialRotator).GetField("isMoon", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            isMoonField?.SetValue(rotator, false);

            var smoothRotationField = typeof(CelestialRotator).GetField("smoothRotation", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            smoothRotationField?.SetValue(rotator, true);

            var enableDebugLoggingField = typeof(CelestialRotator).GetField("enableDebugLogging", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            enableDebugLoggingField?.SetValue(rotator, false);
        }
        private void CreateDemoScene()
        {
            // Create a new scene for the demo
            var newScene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(
                UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, 
                UnityEditor.SceneManagement.NewSceneMode.Additive
            );

            // Set up demo scene content
            SetupDemoSceneContent();

            // Save the demo scene
            string demoScenePath = "Assets/Sol/Scenes/SolDemo.unity";
            
            // Ensure scenes folder exists
            string scenesFolder = "Assets/Sol/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesFolder))
            {
                SolSetupUtilities.EnsureFolderExists(scenesFolder);
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(newScene, demoScenePath);

            Debug.Log($"[Sol Setup] Demo scene created: {demoScenePath}");
        }

        private void SetupDemoSceneContent()
        {
            // Create demo objects in the new scene
            
            // Ground plane
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10, 1, 10);

            // Some demo objects to cast shadows
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Demo Cube";
            cube.transform.position = new Vector3(0, 0.5f, 0);

            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Demo Sphere";
            sphere.transform.position = new Vector3(3, 0.5f, 0);

            // Demo camera
            GameObject cameraGO = new GameObject("Demo Camera");
            Camera demoCamera = cameraGO.AddComponent<Camera>();
            demoCamera.transform.position = new Vector3(0, 2, -5);
            demoCamera.transform.LookAt(Vector3.zero);
            demoCamera.tag = "MainCamera";

            Debug.Log("[Sol Setup] Demo scene content created.");
        }

        private void ResetToDefaults()
        {
            config = new SetupConfig();
            setupStatus = "";
            isSetupInProgress = false;
            Repaint();
        }

        #endregion

        #region Public Methods for Quick Setup

        /// <summary>
        /// Apply a preset configuration to the wizard
        /// </summary>
        public void ApplyPresetConfig(SetupConfig presetConfig)
        {
            config = presetConfig;
            Repaint();
        }

        /// <summary>
        /// Perform setup with the current configuration (for external calls)
        /// </summary>
        public void PerformSetupWithCurrentConfig()
        {
            PerformSetup();
        }

        #endregion
    }
}