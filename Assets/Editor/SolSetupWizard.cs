using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Sol.Editor
{
    /// <summary>
    /// Setup wizard for the Sol Time & Celestial System.
    /// Provides one-click setup for new projects with sensible defaults and sample configurations.
    /// 
    /// ARCHITECTURE NOTE: This class is purely presentation layer (UI/UX).
    /// All business logic is delegated to SolSetupUtilities.
    /// Configuration data is defined in SolSetupConfig (shared DTO).
    /// </summary>
    public class SolSetupWizard : EditorWindow
    {
        #region Window Management

        [MenuItem("Tools/Sol/Setup Wizard", priority = 1)]
        public static void ShowWindow()
        {
            SolSetupWizard window = GetWindow<SolSetupWizard>("Sol Setup Wizard");
            window.minSize = new Vector2(600, 700);
            window.maxSize = new Vector2(600, 1400);
            window.Show();
        }

        #endregion

        #region Private Fields

        private SetupConfig _config;
        private Vector2 _scrollPosition;
        private bool _showAdvancedOptions = false;
        private bool _showSunSettings = true;
        private bool _showMoonSettings = true;
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private bool _isSetupInProgress = false;
        private string _setupStatus = "";

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            Debug.Log("[Sol Wizard] Window opened");
            
            if (_config == null)
            {
                _config = new SetupConfig();
                Debug.Log("[Sol Wizard] Configuration initialized with defaults");
            }
        }

        private void InitializeStyles()
        {
            _headerStyle = new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        #endregion

        #region GUI Drawing

        private void OnGUI()
        {
            if (_headerStyle == null) InitializeStyles();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

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

            if (_showAdvancedOptions)
            {
                DrawAdvancedOptionsSection();
                EditorGUILayout.Space(10);
            }

            DrawAdvancedToggle();
            EditorGUILayout.Space(10);

            DrawSetupButtons();

            if (_isSetupInProgress)
            {
                DrawProgressSection();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            EditorGUILayout.LabelField("Sol Time & Celestial System", _headerStyle);
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
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Scene Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _config.createTimeManager = EditorGUILayout.Toggle(
                new GUIContent("Create TimeManager", "Creates the core TimeManager component that controls time progression"),
                _config.createTimeManager
            );

            _config.createWorldTimeData = EditorGUILayout.Toggle(
                new GUIContent("Create WorldTimeData", "Creates the WorldTimeData asset that defines day length, time scale, and other time settings"),
                _config.createWorldTimeData
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawSeasonalDataSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Seasonal Data", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _config.createSeasonalData = EditorGUILayout.Toggle(
                new GUIContent("Create Seasonal Data", "Creates a SeasonalData asset with default seasonal configurations"),
                _config.createSeasonalData
            );

            if (_config.createSeasonalData)
            {
                EditorGUI.indentLevel++;
                
                _config.numberOfSeasons = EditorGUILayout.IntSlider(
                    new GUIContent("Number of Seasons", "How many seasons to create (2-12)"),
                    _config.numberOfSeasons, 2, 12
                );

                // Ensure seasonNames array matches numberOfSeasons
                if (_config.seasonNames.Length != _config.numberOfSeasons)
                {
                    System.Array.Resize(ref _config.seasonNames, _config.numberOfSeasons);
                    for (int i = 0; i < _config.seasonNames.Length; i++)
                    {
                        if (string.IsNullOrEmpty(_config.seasonNames[i]))
                        {
                            _config.seasonNames[i] = $"Season {i + 1}";
                        }
                    }
                }

                EditorGUILayout.LabelField("Season Names:", EditorStyles.miniBoldLabel);
                for (int i = 0; i < _config.seasonNames.Length; i++)
                {
                    _config.seasonNames[i] = EditorGUILayout.TextField($"Season {i + 1}", _config.seasonNames[i]);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawCelestialBodiesSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Celestial Bodies", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Suns Section
            _showSunSettings = EditorGUILayout.Foldout(_showSunSettings, $"Suns ({_config.suns.Count})", true);
            if (_showSunSettings)
            {
                EditorGUI.indentLevel++;
                
                for (int i = 0; i < _config.suns.Count; i++)
                {
                    DrawCelestialBodyConfig(_config.suns[i], $"Sun {i + 1}", false);
                }
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Sun"))
                {
                    _config.suns.Add(CelestialBodyConfig.CreateDefaultSun());
                }
                if (_config.suns.Count > 1 && GUILayout.Button("Remove Last Sun"))
                {
                    _config.suns.RemoveAt(_config.suns.Count - 1);
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Moons Section
            _showMoonSettings = EditorGUILayout.Foldout(_showMoonSettings, $"Moons ({_config.moons.Count})", true);
            if (_showMoonSettings)
            {
                EditorGUI.indentLevel++;
                
                for (int i = 0; i < _config.moons.Count; i++)
                {
                    DrawCelestialBodyConfig(_config.moons[i], $"Moon {i + 1}", true);
                }
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Moon"))
                {
                    _config.moons.Add(CelestialBodyConfig.CreateDefaultMoon());
                }
                if (_config.moons.Count > 1 && GUILayout.Button("Remove Last Moon"))
                {
                    _config.moons.RemoveAt(_config.moons.Count - 1);
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
        
                if (!isMoon)
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

            // Moon-specific settings
            if (isMoon)
            {
                EditorGUILayout.Space(5);
                bodyConfig.isMoon = true;
                bodyConfig.reflectSunLight = EditorGUILayout.Toggle("Reflect Sun Light", bodyConfig.reflectSunLight);
                
                if (bodyConfig.reflectSunLight)
                {
                    EditorGUI.indentLevel++;
                    
                    // Get list of sun names for popup
                    List<string> sunNames = new List<string>();
                    foreach (var sun in _config.suns)
                    {
                        sunNames.Add(sun.name);
                    }
                    
                    if (sunNames.Count > 0)
                    {
                        int currentIndex = sunNames.IndexOf(bodyConfig.sunToReflect);
                        if (currentIndex < 0) currentIndex = 0;
                        
                        int newIndex = EditorGUILayout.Popup("Sun to Reflect", currentIndex, sunNames.ToArray());
                        bodyConfig.sunToReflect = sunNames[newIndex];
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Sun to Reflect", "No suns available");
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }
    
            EditorGUILayout.EndVertical();
        }

        private void DrawSkyAndFogSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Sky and Fog", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _config.createSkyAndFog = EditorGUILayout.Toggle(
                new GUIContent("Create Sky and Fog Volume", "Creates HDRP Sky and Fog volume with default profile"),
                _config.createSkyAndFog
            );

            if (_config.createSkyAndFog)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.BeginHorizontal();
                _config.hdrpProfilePath = EditorGUILayout.TextField("HDRP Profile Path", _config.hdrpProfilePath);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string path = EditorUtility.OpenFilePanel("Select HDRP Volume Profile", "Assets", "asset");
                    if (!string.IsNullOrEmpty(path))
                    {
                        if (path.StartsWith(Application.dataPath))
                        {
                            _config.hdrpProfilePath = "Assets" + path.Substring(Application.dataPath.Length);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                if (string.IsNullOrEmpty(_config.hdrpProfilePath))
                {
                    EditorGUILayout.HelpBox("Leave empty to use SolDefaultSkyProfile", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDemoContentSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Demo Content", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _config.createDemoScene = EditorGUILayout.Toggle(
                new GUIContent("Create Demo Scene", "Creates demo objects in the current scene to showcase the system"),
                _config.createDemoScene
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedOptionsSection()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Advanced Options", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            _config.dataFolderPath = EditorGUILayout.TextField(
                new GUIContent("Data Folder Path", "Where to create SeasonalData and WorldTimeData assets"),
                _config.dataFolderPath
            );

            _config.prefabFolderPath = EditorGUILayout.TextField(
                new GUIContent("Prefab Folder Path", "Where to create prefab assets"),
                _config.prefabFolderPath
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawAdvancedToggle()
        {
            _showAdvancedOptions = EditorGUILayout.Foldout(_showAdvancedOptions, "Advanced Options", true);
        }

        private void DrawSetupButtons()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            
            EditorGUI.BeginDisabledGroup(_isSetupInProgress);
            
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
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Setup Progress", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_setupStatus);
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region Setup Logic

        /// <summary>
        /// Performs the complete Sol system setup.
        /// Validates configuration, then delegates to SolSetupUtilities for business logic.
        /// </summary>
        private void PerformSetup()
        {
            Debug.Log("[Sol Wizard] === SETUP INITIATED ===");
            
            _isSetupInProgress = true;
            _setupStatus = "Validating configuration...";
            Repaint();

            try
            {
                // STEP 1: Validate configuration
                List<string> validationErrors = _config.Validate();
                
                if (validationErrors.Count > 0)
                {
                    string errorMessage = "Configuration validation failed:\n\n" + string.Join("\n", validationErrors);
                    Debug.LogError($"[Sol Wizard] {errorMessage}");
                    EditorUtility.DisplayDialog("Configuration Error", errorMessage, "OK");
                    return;
                }

                Debug.Log("[Sol Wizard] Configuration validated successfully");
                Debug.Log($"[Sol Wizard] - Create WorldTimeData: {_config.createWorldTimeData}");
                Debug.Log($"[Sol Wizard] - Create TimeManager: {_config.createTimeManager}");
                Debug.Log($"[Sol Wizard] - Create Seasonal Data: {_config.createSeasonalData}");
                Debug.Log($"[Sol Wizard] - Suns: {_config.suns.Count}");
                Debug.Log($"[Sol Wizard] - Moons: {_config.moons.Count}");

                // STEP 2: Delegate to utilities (business logic layer)
                _setupStatus = "Executing setup...";
                Repaint();

                SolSetupUtilities.PerformCompleteSetup(_config, UpdateStatus);

                // STEP 3: Success!
                _setupStatus = "Setup completed successfully!";
                Debug.Log("[Sol Wizard] === SETUP COMPLETE ===");
                
                EditorUtility.DisplayDialog(
                    "Sol Setup Complete", 
                    "Scene setup completed successfully! Your Sol Time & Celestial System is ready to use.",
                    "OK"
                );
            }
            catch (System.NullReferenceException ex)
            {
                _setupStatus = $"Setup failed: Null reference";
                Debug.LogError($"[Sol Wizard] NULL REFERENCE: {ex.Message}");
                Debug.LogError($"[Sol Wizard] Stack: {ex.StackTrace}");
                
                EditorUtility.DisplayDialog(
                    "Setup Error - Null Reference", 
                    $"A null reference occurred:\n\n{ex.Message}\n\nCheck Console for details.", 
                    "OK"
                );
            }
            catch (System.Exception ex)
            {
                _setupStatus = $"Setup failed: {ex.Message}";
                Debug.LogError($"[Sol Wizard] EXCEPTION: {ex.Message}");
                Debug.LogError($"[Sol Wizard] Stack: {ex.StackTrace}");
                
                EditorUtility.DisplayDialog(
                    "Setup Error", 
                    $"An error occurred:\n\n{ex.Message}\n\nCheck Console for details.", 
                    "OK"
                );
            }
            finally
            {
                _isSetupInProgress = false;
                Repaint();
            }
        }

        /// <summary>
        /// Updates setup status and repaints window.
        /// Callback for SolSetupUtilities progress reporting.
        /// </summary>
        private void UpdateStatus(string status)
        {
            _setupStatus = status;
            Debug.Log($"[Sol Wizard] {status}");
            Repaint();
        }

        /// <summary>
        /// Resets configuration to defaults.
        /// </summary>
        private void ResetToDefaults()
        {
            if (EditorUtility.DisplayDialog(
                "Reset Configuration", 
                "Are you sure you want to reset all settings to defaults?", 
                "Yes", "Cancel"))
            {
                _config = new SetupConfig();
                _setupStatus = "";
                _isSetupInProgress = false;
                Repaint();
                Debug.Log("[Sol Wizard] Configuration reset to defaults");
            }
        }

        #endregion
    }
}
