using UnityEngine;
using UnityEditor;
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
            window.minSize = new Vector2(600, 700);
            window.maxSize = new Vector2(600, 1000);
            window.Show();
        }

        #endregion

        #region Setup Configuration

        [System.Serializable]
        public class CelestialBodyConfig
        {
            public string name = "Sol";
            public bool active = true;
            public bool yAxisEnabled = true;
            public float yAxisSpeed = 1.0f;
            public bool yAxisOverrideSpeed = true;
            public float orbitalAngle = 23.5f;
            public float baseElevation = 180f;
            public float orbitalPeriod = 1f;
            public float phaseOffset = 0f;
    
            [Header("Light Settings")]
            public bool createDirectionalLight = true;
            public float lightTemperature = 6500f; // Kelvin temperature
            public float lightIntensity = 1.0f;
            public bool castShadows = true; // Will be overridden for additional suns
        }

        [System.Serializable]
        public class MoonConfig : CelestialBodyConfig
        {
            public bool reflectSunLight = true;
            public string sunToReflect = "Sol";
    
            public MoonConfig()
            {
                name = "Luna";
                orbitalPeriod = 29.5f;
                phaseOffset = 180f;
                lightTemperature = 4000f; // Cooler moonlight
                lightIntensity = 0.2f;
                castShadows = false; // Moons typically don't cast shadows
            }
        }

        [System.Serializable]
        public class SetupConfig
        {
            [Header("Scene Setup")]
            public bool createTimeManager = true;
            public bool createWorldTimeData = true;

            [Header("Seasonal Data")]
            public bool createSeasonalData = true;
            public int numberOfSeasons = 4;
            public string[] seasonNames = { "Spring", "Summer", "Autumn", "Winter" };

            [Header("Celestial Bodies")]
            public List<CelestialBodyConfig> suns = new List<CelestialBodyConfig>();
            public List<MoonConfig> moons = new List<MoonConfig>();

            [Header("Sky and Fog")]
            public bool createSkyAndFog = true;
            public string hdrpProfilePath = ""; // Path to existing HDRP profile

            [Header("Demo Content")]
            public bool createDemoScene = false;

            [Header("Asset Paths")]
            public string dataFolderPath = "Assets/Sol/Data";
            public string prefabFolderPath = "Assets/Sol/Prefabs";

            public SetupConfig()
            {
                // Initialize with default sun
                suns.Add(new CelestialBodyConfig());
                
                // Initialize with default moon
                moons.Add(new MoonConfig());
            }
        }

        #endregion

        #region Private Fields

        private SetupConfig config = new SetupConfig();
        private Vector2 scrollPosition;
        private bool showAdvancedOptions = false;
        private bool showSunSettings = true;
        private bool showMoonSettings = true;
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

            DrawSkyAndFogSection();
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
                "Configure multiple suns and moons with individual settings.",
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

            // Suns Section
            showSunSettings = EditorGUILayout.Foldout(showSunSettings, $"Suns ({config.suns.Count})", true);
            if (showSunSettings)
            {
                EditorGUI.indentLevel++;
                
                for (int i = 0; i < config.suns.Count; i++)
                {
                    DrawCelestialBodyConfig(config.suns[i], $"Sun {i + 1}", false);
                }
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Sun"))
                {
                    config.suns.Add(new CelestialBodyConfig { name = $"Sun {config.suns.Count + 1}" });
                }
                if (config.suns.Count > 1 && GUILayout.Button("Remove Last Sun"))
                {
                    config.suns.RemoveAt(config.suns.Count - 1);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Moons Section
            showMoonSettings = EditorGUILayout.Foldout(showMoonSettings, $"Moons ({config.moons.Count})", true);
            if (showMoonSettings)
            {
                EditorGUI.indentLevel++;
                
                for (int i = 0; i < config.moons.Count; i++)
                {
                    DrawMoonConfig(config.moons[i], $"Moon {i + 1}");
                }
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Moon"))
                {
                    config.moons.Add(new MoonConfig { name = $"Moon {config.moons.Count + 1}" });
                }
                if (config.moons.Count > 1 && GUILayout.Button("Remove Last Moon"))
                {
                    config.moons.RemoveAt(config.moons.Count - 1);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCelestialBodyConfig(CelestialBodyConfig bodyConfig, string label, bool isMoon)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
    
            bodyConfig.name = EditorGUILayout.TextField("Name", bodyConfig.name);
            bodyConfig.active = EditorGUILayout.Toggle("Active", bodyConfig.active);
            bodyConfig.createDirectionalLight = EditorGUILayout.Toggle("Create Directional Light", bodyConfig.createDirectionalLight);
    
            if (bodyConfig.createDirectionalLight)
            {
                EditorGUI.indentLevel++;
                bodyConfig.lightTemperature = EditorGUILayout.Slider("Light Temperature (K)", bodyConfig.lightTemperature, 1000f, 20000f);
                bodyConfig.lightIntensity = EditorGUILayout.FloatField("Light Intensity", bodyConfig.lightIntensity);
        
                if (!isMoon) // Only show shadow casting for suns
                {
                    bodyConfig.castShadows = EditorGUILayout.Toggle("Cast Shadows", bodyConfig.castShadows);
                }
        
                EditorGUI.indentLevel--;
            }
    
            bodyConfig.yAxisEnabled = EditorGUILayout.Toggle("Y-Axis Enabled", bodyConfig.yAxisEnabled);
            if (bodyConfig.yAxisEnabled)
            {
                EditorGUI.indentLevel++;
                bodyConfig.yAxisSpeed = EditorGUILayout.FloatField("Y-Axis Speed", bodyConfig.yAxisSpeed);
                bodyConfig.yAxisOverrideSpeed = EditorGUILayout.Toggle("Override Speed", bodyConfig.yAxisOverrideSpeed);
                EditorGUI.indentLevel--;
            }
    
            bodyConfig.orbitalAngle = EditorGUILayout.Slider("Orbital Angle", bodyConfig.orbitalAngle, 0f, 89f);
            bodyConfig.baseElevation = EditorGUILayout.Slider("Base Elevation", bodyConfig.baseElevation, 0f, 360f);
            bodyConfig.orbitalPeriod = EditorGUILayout.FloatField("Orbital Period (days)", bodyConfig.orbitalPeriod);
            bodyConfig.phaseOffset = EditorGUILayout.Slider("Phase Offset", bodyConfig.phaseOffset, 0f, 360f);
    
            EditorGUILayout.EndVertical();
        }

        private void DrawMoonConfig(MoonConfig moonConfig, string label)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            
            DrawCelestialBodyConfig(moonConfig, "", true);
            
            moonConfig.reflectSunLight = EditorGUILayout.Toggle("Reflect Sun Light", moonConfig.reflectSunLight);
            if (moonConfig.reflectSunLight)
            {
                EditorGUI.indentLevel++;
                
                // Create dropdown for available suns
                List<string> sunNames = new List<string>();
                foreach (var sun in config.suns)
                {
                    sunNames.Add(sun.name);
                }
                
                if (sunNames.Count > 0)
                {
                    int currentIndex = sunNames.IndexOf(moonConfig.sunToReflect);
                    if (currentIndex < 0) currentIndex = 0;
                    
                    int newIndex = EditorGUILayout.Popup("Sun to Reflect", currentIndex, sunNames.ToArray());
                    moonConfig.sunToReflect = sunNames[newIndex];
                }
                else
                {
                    EditorGUILayout.LabelField("Sun to Reflect", "No suns available");
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }

        private void DrawSkyAndFogSection()
        {
            EditorGUILayout.BeginVertical(boxStyle);
            EditorGUILayout.LabelField("Sky and Fog", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            config.createSkyAndFog = EditorGUILayout.Toggle(
                new GUIContent("Create Sky and Fog Volume", "Creates HDRP Sky and Fog volume with default profile"),
                config.createSkyAndFog
            );

            if (config.createSkyAndFog)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginHorizontal();
                config.hdrpProfilePath = EditorGUILayout.TextField("HDRP Profile Path", config.hdrpProfilePath);
                        if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFilePanel("Select HDRP Volume Profile", "Assets", "asset");
                    if (!string.IsNullOrEmpty(path))
                    {
                        // Convert absolute path to relative path
                        if (path.StartsWith(Application.dataPath))
                        {
                            config.hdrpProfilePath = "Assets" + path.Substring(Application.dataPath.Length);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                if (string.IsNullOrEmpty(config.hdrpProfilePath))
                {
                    EditorGUILayout.HelpBox("Leave empty to create a default HDRP profile", MessageType.Info);
                }
                
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
                new GUIContent("Create Demo Scene", "Creates demo objects in the current scene to showcase the system"),
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
                SolSetupUtilities.PerformCompleteSetup(config, (status) => {
                    setupStatus = status;
                    Repaint();
                });

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

        private void ResetToDefaults()
        {
            config = new SetupConfig();
            setupStatus = "";
            isSetupInProgress = false;
            Repaint();
        }

        #endregion
    }
}
       
                   