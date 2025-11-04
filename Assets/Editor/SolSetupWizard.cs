using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Sol.Editor
{
    /// <summary>
    /// Setup wizard for creating and configuring the Sol time and celestial system.
    /// Guides users through calendar, seasonal, and celestial body configuration.
    /// </summary>
    public class SolSetupWizard : EditorWindow
    {
        #region Fields

        private SetupConfig _config;
        private Vector2 _scrollPosition;
        private bool _showValidation = false;
        private List<string> _validationErrors = new List<string>();

        // Foldout states
        private bool _showCalendarMonthNames = false;
        private bool _showSeasonNames = false;
        private bool _showSuns = true;
        private bool _showMoons = true;
        private bool _showAdvancedOptions = false;

        // Time scale preset
        private TimeScalePreset _selectedTimeScalePreset = TimeScalePreset.Fast;
        private bool _useCustomTimeScale = false;

        // Tab state
        private int _currentTab = 0;
        private readonly string[] _tabNames = new string[] 
        { 
            "Calendar", 
            "Celestial Bodies",
            "Seasonal Config",
            "Atmosphere",
            "Advanced" 
        };

        #endregion

        #region Unity Methods

        [MenuItem("Sol/Setup Wizard")]
        public static void ShowWindow()
        {
            var window = GetWindow<SolSetupWizard>("Sol Setup Wizard");
            window.minSize = new Vector2(600f, 700f);
            window.Show();
        }
        
        [MenuItem("Tools/Sol/Setup Wizard", priority = 100)]
        public static void ShowWindowFromTools()
        {
            ShowWindow();
        }

        private void OnEnable()
        {
            if (_config == null)
            {
                _config = new SetupConfig();
            }
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawHeader();
            DrawPresetButtons();
            
            EditorGUILayout.Space(10);
            
            DrawTabs();
            
            EditorGUILayout.Space(10);

            switch (_currentTab)
            {
                case 0:
                    DrawCalendarTab();
                    break;
                case 1:
                    DrawCelestialBodiesTab(); 
                    break;
                case 2:
                    DrawSeasonalConfigurationTab();
                    break;
                case 3:
                    DrawAtmosphereTab();
                    break;
                case 4:
                    DrawAdvancedTab();
                    break;
            }

            EditorGUILayout.Space(10);

            DrawValidationSection();
            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region UI Drawing Methods

        private void DrawHeader()
        {
            EditorGUILayout.Space(10);
            
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Sol System Setup Wizard", titleStyle);
            
            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Italic
            };
            EditorGUILayout.LabelField("Configure your world's time, seasons, and celestial mechanics", subtitleStyle);
            
            EditorGUILayout.Space(10);
        }

        private void DrawPresetButtons()
        {
            EditorGUILayout.LabelField("Quick Start Presets", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Earth-like", GUILayout.Height(30)))
            {
                CalendarPresets.ApplyEarthPreset(_config);
                _selectedTimeScalePreset = TimeScalePreset.OneMinute;
                _useCustomTimeScale = false;
                GUI.FocusControl(null);
            }
            
            if (GUILayout.Button("Sol (Default)", GUILayout.Height(30)))
            {
                CalendarPresets.ApplySolPreset(_config);
                _selectedTimeScalePreset = TimeScalePreset.Fast;
                _useCustomTimeScale = false;
                GUI.FocusControl(null);
            }
            
            if (GUILayout.Button("Mars", GUILayout.Height(30)))
            {
                CalendarPresets.ApplyMarsPreset(_config);
                _selectedTimeScalePreset = TimeScalePreset.OneMinute;
                _useCustomTimeScale = false;
                GUI.FocusControl(null);
            }
            
            if (GUILayout.Button("Alien World", GUILayout.Height(30)))
            {
                CalendarPresets.ApplyAlienPreset(_config);
                _selectedTimeScalePreset = TimeScalePreset.VeryFast;
                _useCustomTimeScale = false;
                GUI.FocusControl(null);
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            _currentTab = GUILayout.Toolbar(_currentTab, _tabNames, GUILayout.Height(25));
        }

        private void DrawCalendarTab()
        {
            EditorGUILayout.LabelField("Calendar Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawTimeScaleSection();
            EditorGUILayout.Space(10);
            DrawTimeStructureSection();
            EditorGUILayout.Space(10);
            DrawCalendarConfigurationSection();
        }
        
        private void DrawCelestialBodiesTab()
        {
            EditorGUILayout.LabelField("Celestial Bodies Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Define which celestial bodies exist in your world. " +
                "Their seasonal behavior will be configured in the 'Seasonal Config' tab.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // ===== SUNS SECTION =====
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Suns / Stars", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(5);
            
            // Sun list
            if (_config.suns == null)
                _config.suns = new List<CelestialBodyIdentity>();
            
            for (int i = 0; i < _config.suns.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField($"Sun {i + 1}:", GUILayout.Width(60));
                
                _config.suns[i].name = EditorGUILayout.TextField(_config.suns[i].name);
                
                _config.suns[i].createDirectionalLight = EditorGUILayout.Toggle(
                    new GUIContent("Light", "Create directional light component"),
                    _config.suns[i].createDirectionalLight,
                    GUILayout.Width(60)
                );
                
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    _config.suns.RemoveAt(i);
                    GUI.FocusControl(null);
                    break;
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
            
            // Add sun button
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("+ Add Sun", GUILayout.Height(25)))
            {
                _config.suns.Add(new CelestialBodyIdentity 
                { 
                    name = $"Sun {_config.suns.Count + 1}",
                    createDirectionalLight = true
                });
                GUI.FocusControl(null);
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // ===== MOONS SECTION =====
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Moons / Satellites", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(5);
            
            // Moon list
            if (_config.moons == null)
                _config.moons = new List<CelestialBodyIdentity>();
            
            for (int i = 0; i < _config.moons.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.LabelField($"Moon {i + 1}:", GUILayout.Width(60));
                
                _config.moons[i].name = EditorGUILayout.TextField(_config.moons[i].name);
                
                _config.moons[i].createDirectionalLight = EditorGUILayout.Toggle(
                    new GUIContent("Light", "Create directional light component"),
                    _config.moons[i].createDirectionalLight,
                    GUILayout.Width(60)
                );
                
                GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                if (GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    _config.moons.RemoveAt(i);
                    GUI.FocusControl(null);
                    break;
                }
                GUI.backgroundColor = Color.white;
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
            
            // Add moon button
            GUI.backgroundColor = new Color(0.7f, 0.7f, 1f);
            if (GUILayout.Button("+ Add Moon", GUILayout.Height(25)))
            {
                _config.moons.Add(new CelestialBodyIdentity 
                { 
                    name = $"Moon {_config.moons.Count + 1}",
                    createDirectionalLight = true
                });
                GUI.FocusControl(null);
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // ===== INFO BOX =====
            EditorGUILayout.HelpBox(
                "💡 Tip: After defining your celestial bodies here, go to the 'Seasonal Config' tab to configure how each body behaves in each season (orbital angles, light intensity, etc.).",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            // ===== QUICK PRESETS =====
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Earth-like\n(1 Sun, 1 Moon)"))
            {
                _config.suns.Clear();
                _config.suns.Add(new CelestialBodyIdentity { name = "Sun", createDirectionalLight = true });
                
                _config.moons.Clear();
                _config.moons.Add(new CelestialBodyIdentity { name = "Moon", createDirectionalLight = true });
                
                GUI.FocusControl(null);
            }
            
            if (GUILayout.Button("Binary Star\n(2 Suns, 1 Moon)"))
            {
                _config.suns.Clear();
                _config.suns.Add(new CelestialBodyIdentity { name = "Primary Star", createDirectionalLight = true });
                _config.suns.Add(new CelestialBodyIdentity { name = "Secondary Star", createDirectionalLight = true });
                
                _config.moons.Clear();
                _config.moons.Add(new CelestialBodyIdentity { name = "Moon", createDirectionalLight = true });
                
                GUI.FocusControl(null);
            }
            
            if (GUILayout.Button("Multi-Moon\n(1 Sun, 3 Moons)"))
            {
                _config.suns.Clear();
                _config.suns.Add(new CelestialBodyIdentity { name = "Sun", createDirectionalLight = true });
                
                _config.moons.Clear();
                _config.moons.Add(new CelestialBodyIdentity { name = "Moon Alpha", createDirectionalLight = true });
                _config.moons.Add(new CelestialBodyIdentity { name = "Moon Beta", createDirectionalLight = true });
                _config.moons.Add(new CelestialBodyIdentity { name = "Moon Gamma", createDirectionalLight = false });
                
                GUI.FocusControl(null);
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // ===== VALIDATION WARNING =====
            // Check for duplicate names
            List<string> duplicates = new List<string>();
            HashSet<string> seen = new HashSet<string>();
            
            foreach (var sun in _config.suns)
            {
                if (!string.IsNullOrWhiteSpace(sun.name))
                {
                    if (!seen.Add(sun.name))
                        duplicates.Add(sun.name);
                }
            }
            
            foreach (var moon in _config.moons)
            {
                if (!string.IsNullOrWhiteSpace(moon.name))
                {
                    if (!seen.Add(moon.name))
                        duplicates.Add(moon.name);
                }
            }
            
            if (duplicates.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ Duplicate celestial body names detected: {string.Join(", ", duplicates)}\n\n" +
                    "Each celestial body must have a unique name.",
                    MessageType.Warning
                );
            }
            
            // Check for empty names
            bool hasEmptyNames = false;
            foreach (var sun in _config.suns)
            {
                if (string.IsNullOrWhiteSpace(sun.name))
                {
                    hasEmptyNames = true;
                    break;
                }
            }
            
            if (!hasEmptyNames)
            {
                foreach (var moon in _config.moons)
                {
                    if (string.IsNullOrWhiteSpace(moon.name))
                    {
                        hasEmptyNames = true;
                        break;
                    }
                }
            }
            
            if (hasEmptyNames)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ One or more celestial bodies have no name. All bodies must be named.",
                    MessageType.Warning
                );
            }
        }

        private void DrawSeasonsAndBodiesTab()
        {
            EditorGUILayout.LabelField("Seasons & Celestial Bodies", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawSeasonalConfigurationSection();
            EditorGUILayout.Space(10);
            DrawCelestialBodiesSection();
        }

        private void DrawAtmosphereTab()
        {
            EditorGUILayout.LabelField("Atmospheric Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawSkyAndFogSection();
        }

        private void DrawAdvancedTab()
        {
            EditorGUILayout.LabelField("Advanced Options", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawSceneSetupSection();
            EditorGUILayout.Space(10);
            DrawPathConfigurationSection();
            EditorGUILayout.Space(10);
            DrawDemoContentSection();
        }

        #endregion

        #region Section Drawing Methods

        private int _selectedSeasonIndex = 0;
        private bool[] _seasonBodyFoldouts = new bool[0]; // Track foldout states per season

        private void DrawSeasonalConfigurationTab()
        {
            EditorGUILayout.LabelField("Seasonal Celestial Body Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Configure how each celestial body behaves in each season. " +
                "Set orbital angles, elevations, and light properties per season.",
                MessageType.Info
            );

            EditorGUILayout.Space(10);

            // Initialize configs if needed
            if (_config.seasonConfigs == null || _config.seasonConfigs.Count != _config.numberOfSeasons)
            {
                if (GUILayout.Button("Initialize Seasonal Configurations", GUILayout.Height(30)))
                {
                    _config.InitializeSeasonalConfigs();
                    _seasonBodyFoldouts = new bool[_config.numberOfSeasons];
                    GUI.FocusControl(null);
                }
                
                EditorGUILayout.HelpBox(
                    "Click 'Initialize Seasonal Configurations' to create configuration templates for all seasons and celestial bodies.",
                    MessageType.Warning
                );
                return;
            }

            // Season selector tabs
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Select Season:", GUILayout.Width(100));
            
            string[] seasonTabNames = new string[_config.numberOfSeasons];
            for (int i = 0; i < _config.numberOfSeasons; i++)
            {
                seasonTabNames[i] = _config.seasonNames[i];
            }
            
            _selectedSeasonIndex = GUILayout.Toolbar(_selectedSeasonIndex, seasonTabNames, GUILayout.Height(25));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Draw configuration for selected season
            if (_selectedSeasonIndex >= 0 && _selectedSeasonIndex < _config.seasonConfigs.Count)
            {
                DrawSeasonConfigSection(_config.seasonConfigs[_selectedSeasonIndex], _selectedSeasonIndex);
            }

            EditorGUILayout.Space(10);

            // Utility buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Copy from Previous Season"))
            {
                CopySeasonConfig(_selectedSeasonIndex, _selectedSeasonIndex - 1);
            }
            
            if (GUILayout.Button("Copy from Next Season"))
            {
                CopySeasonConfig(_selectedSeasonIndex, _selectedSeasonIndex + 1);
            }
            
            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog(
                    "Reset Season Config",
                    $"Reset all celestial body configurations for {_config.seasonNames[_selectedSeasonIndex]}?",
                    "Reset",
                    "Cancel"))
                {
                    ResetSeasonConfig(_selectedSeasonIndex);
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSeasonConfigSection(SeasonConfig seasonConfig, int seasonIndex)
        {
            EditorGUILayout.LabelField($"Season: {seasonConfig.seasonName}", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(5);
            
            // Suns section
            if (seasonConfig.sunConfigs.Count > 0)
            {
                EditorGUILayout.LabelField("Suns", EditorStyles.boldLabel);
                
                foreach (var sunConfig in seasonConfig.sunConfigs)
                {
                    DrawCelestialBodyConfig(sunConfig, true);
                    EditorGUILayout.Space(5);
                }
            }
            
            EditorGUILayout.Space(10);
            
            // Moons section
            if (seasonConfig.moonConfigs.Count > 0)
            {
                EditorGUILayout.LabelField("Moons", EditorStyles.boldLabel);
                
                foreach (var moonConfig in seasonConfig.moonConfigs)
                {
                    DrawCelestialBodyConfig(moonConfig, false);
                    EditorGUILayout.Space(5);
                }
            }
        }

        private void DrawCelestialBodyConfig(SeasonalBodyConfig bodyConfig, bool isSun)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField(bodyConfig.bodyName, EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            
            // Orbital settings
            EditorGUILayout.LabelField("Orbital Mechanics", EditorStyles.miniBoldLabel);
            
            bodyConfig.orbitalAngle = EditorGUILayout.Slider(
                new GUIContent("Orbital Angle", "Tilt of orbit (like Earth's 23.5° axial tilt)"),
                bodyConfig.orbitalAngle,
                -90f,
                90f
            );
            
            bodyConfig.baseElevation = EditorGUILayout.Slider(
                new GUIContent("Base Elevation", "Starting elevation angle in orbit"),
                bodyConfig.baseElevation,
                0f,
                360f
            );
            
            bodyConfig.orbitalPeriod = EditorGUILayout.FloatField(
                new GUIContent("Orbital Period", "Days to complete one full orbit"),
                bodyConfig.orbitalPeriod
            );
            
            if (!isSun) // Moons have phases
            {
                bodyConfig.phaseOffset = EditorGUILayout.Slider(
                    new GUIContent("Phase Offset", "Initial phase angle (0-360)"),
                    bodyConfig.phaseOffset,
                    0f,
                    360f
                );
            }
            
            EditorGUILayout.Space(5);
            
            // Light settings
            EditorGUILayout.LabelField("Light Properties", EditorStyles.miniBoldLabel);
            
            bodyConfig.lightIntensity = EditorGUILayout.FloatField(
                new GUIContent("Light Intensity", "Lux value for directional light"),
                bodyConfig.lightIntensity
            );
            
            bodyConfig.lightTemperature = EditorGUILayout.Slider(
                new GUIContent("Temperature (K)", "Color temperature in Kelvin"),
                bodyConfig.lightTemperature,
                1000f,
                20000f
            );
            
            bodyConfig.lightColor = EditorGUILayout.ColorField(
                new GUIContent("Light Color", "Base light color"),
                bodyConfig.lightColor
            );
            
            EditorGUI.indentLevel--;
            
            EditorGUILayout.EndVertical();
        }

        private void CopySeasonConfig(int targetIndex, int sourceIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= _config.seasonConfigs.Count)
            {
                Debug.LogWarning("[SolSetup] Invalid source season index for copy");
                return;
            }
            
            var source = _config.seasonConfigs[sourceIndex];
            var target = _config.seasonConfigs[targetIndex];
            
            // Deep copy sun configs
            target.sunConfigs.Clear();
            foreach (var sunConfig in source.sunConfigs)
            {
                target.sunConfigs.Add(new SeasonalBodyConfig
                {
                    bodyName = sunConfig.bodyName,
                    orbitalAngle = sunConfig.orbitalAngle,
                    baseElevation = sunConfig.baseElevation,
                    orbitalPeriod = sunConfig.orbitalPeriod,
                    phaseOffset = sunConfig.phaseOffset,
                    lightIntensity = sunConfig.lightIntensity,
                    lightTemperature = sunConfig.lightTemperature,
                    lightColor = sunConfig.lightColor
                });
            }
            
            // Deep copy moon configs
            target.moonConfigs.Clear();
            foreach (var moonConfig in source.moonConfigs)
            {
                target.moonConfigs.Add(new SeasonalBodyConfig
                {
                    bodyName = moonConfig.bodyName,
                    orbitalAngle = moonConfig.orbitalAngle,
                    baseElevation = moonConfig.baseElevation,
                    orbitalPeriod = moonConfig.orbitalPeriod,
                    phaseOffset = moonConfig.phaseOffset,
                    lightIntensity = moonConfig.lightIntensity,
                    lightTemperature = moonConfig.lightTemperature,
                    lightColor = moonConfig.lightColor
                });
            }
            
            Debug.Log($"[SolSetup] Copied configuration from {source.seasonName} to {target.seasonName}");
        }

        private void ResetSeasonConfig(int seasonIndex)
        {
            var seasonConfig = _config.seasonConfigs[seasonIndex];
            
            // Reset suns
            foreach (var sunConfig in seasonConfig.sunConfigs)
            {
                sunConfig.orbitalAngle = 23.5f;
                sunConfig.baseElevation = 180f;
                sunConfig.orbitalPeriod = 1f;
                sunConfig.phaseOffset = 0f;
                sunConfig.lightIntensity = 100000f;
                sunConfig.lightTemperature = 6500f;
                sunConfig.lightColor = Color.white;
            }
            
            // Reset moons
            foreach (var moonConfig in seasonConfig.moonConfigs)
            {
                moonConfig.orbitalAngle = 23.5f;
                moonConfig.baseElevation = 180f;
                moonConfig.orbitalPeriod = 29.5f;
                moonConfig.phaseOffset = 0f;
                moonConfig.lightIntensity = 500f;
                moonConfig.lightTemperature = 4000f;
                moonConfig.lightColor = new Color(0.8f, 0.8f, 1f);
            }
        }
        
        private void DrawTimeScaleSection()
        {
            EditorGUILayout.LabelField("Time Progression", EditorStyles.boldLabel);

            // Preset dropdown
            EditorGUI.BeginChangeCheck();
            _selectedTimeScalePreset = (TimeScalePreset)EditorGUILayout.EnumPopup(
                new GUIContent("Time Scale Preset", "How fast game time progresses relative to real time"),
                _selectedTimeScalePreset
            );

            if (EditorGUI.EndChangeCheck() && _selectedTimeScalePreset != TimeScalePreset.Custom)
            {
                _config.timeScale = (float)_selectedTimeScalePreset;
                _useCustomTimeScale = false;
            }

            // Custom time scale field
            if (_selectedTimeScalePreset == TimeScalePreset.Custom || _useCustomTimeScale)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                _config.timeScale = EditorGUILayout.FloatField(
                    new GUIContent("Custom Time Scale", "Custom multiplier for time progression"),
                    _config.timeScale
                );
                if (EditorGUI.EndChangeCheck())
                {
                    _useCustomTimeScale = true;
                    _selectedTimeScalePreset = TimeScalePreset.Custom;
                }
                EditorGUI.indentLevel--;
            }

            // Display calculated values
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                GetTimeScaleDescription(_config.timeScale),
                MessageType.Info
            );
        }

        private void DrawTimeStructureSection()
        {
            EditorGUILayout.LabelField("Time Display Structure", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "These values define how time is DISPLAYED (clock format), not how fast it progresses.",
                MessageType.Info
            );

            _config.hoursPerDay = EditorGUILayout.IntField(
                new GUIContent("Hours Per Day", "Number of hours displayed per day cycle"),
                _config.hoursPerDay
            );

            _config.minutesPerHour = EditorGUILayout.IntField(
                new GUIContent("Minutes Per Hour", "Number of minutes per hour"),
                _config.minutesPerHour
            );

            _config.secondsPerMinute = EditorGUILayout.IntField(
                new GUIContent("Seconds Per Minute", "Number of seconds per minute"),
                _config.secondsPerMinute
            );

            // Show calculated day length
            EditorGUILayout.Space(5);
            int totalGameSeconds = _config.hoursPerDay * _config.minutesPerHour * _config.secondsPerMinute;
            float realDayLengthSeconds = totalGameSeconds / _config.timeScale;
            
            EditorGUILayout.HelpBox(
                $"Complete day cycle: {FormatDuration(realDayLengthSeconds)} in real-time\n" +
                $"(Display shows: {totalGameSeconds:N0} game-seconds per day)",
                MessageType.None
            );
        }

        private void DrawCalendarConfigurationSection()
        {
            EditorGUILayout.LabelField("Calendar Structure", EditorStyles.boldLabel);

            _config.totalDaysInYear = EditorGUILayout.IntField(
                new GUIContent("Total Days in Year", "Total number of days in one complete year"),
                _config.totalDaysInYear
            );

            _config.monthsPerYear = EditorGUILayout.IntField(
                new GUIContent("Months Per Year", "Number of months in the calendar year"),
                _config.monthsPerYear
            );

            _config.daysPerMonth = EditorGUILayout.IntField(
                new GUIContent("Days Per Month", "Number of days in each month (uniform)"),
                _config.daysPerMonth
            );

            // Validation warning
            int calculatedYearLength = _config.monthsPerYear * _config.daysPerMonth;
            if (calculatedYearLength != _config.totalDaysInYear)
            {
                EditorGUILayout.HelpBox(
                    $"Warning: {_config.monthsPerYear} months × {_config.daysPerMonth} days = {calculatedYearLength} days, " +
                    $"but year is set to {_config.totalDaysInYear} days!",
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space(5);

            // Month names array
            if (_config.monthNames == null || _config.monthNames.Length != _config.monthsPerYear)
            {
                _config.monthNames = new string[_config.monthsPerYear];
                for (int i = 0; i < _config.monthNames.Length; i++)
                {
                    _config.monthNames[i] = $"Month {i + 1}";
                }
            }

            _showCalendarMonthNames = EditorGUILayout.Foldout(_showCalendarMonthNames, $"Month Names ({_config.monthsPerYear})", true);
            if (_showCalendarMonthNames)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < _config.monthNames.Length; i++)
                {
                    _config.monthNames[i] = EditorGUILayout.TextField($"Month {i + 1}", _config.monthNames[i]);
                }
                EditorGUI.indentLevel--;
            }
        }

        private void DrawSeasonalConfigurationSection()
        {
            EditorGUILayout.LabelField("Seasonal Configuration", EditorStyles.boldLabel);

            _config.createSeasonalData = EditorGUILayout.Toggle(
                new GUIContent("Create Seasonal Data", "Generate seasonal data assets"),
                _config.createSeasonalData
            );

            if (!_config.createSeasonalData)
            {
                EditorGUILayout.HelpBox("Seasonal data creation is disabled. No season assets will be generated.", MessageType.Info);
                return;
            }

            EditorGUI.indentLevel++;

            _config.numberOfSeasons = EditorGUILayout.IntSlider(
                new GUIContent("Number of Seasons", "How many distinct seasons (2-12)"),
                _config.numberOfSeasons,
                2,
                12
            );

            _config.seasonTransitionDays = EditorGUILayout.IntField(
                new GUIContent("Transition Days", "Number of days for smooth season transitions"),
                _config.seasonTransitionDays
            );

            EditorGUILayout.Space(5);

            // Season names array
            if (_config.seasonNames == null || _config.seasonNames.Length != _config.numberOfSeasons)
            {
                _config.seasonNames = new string[_config.numberOfSeasons];
                for (int i = 0; i < _config.seasonNames.Length; i++)
                {
                    _config.seasonNames[i] = $"Season {i + 1}";
                }
            }

            _showSeasonNames = EditorGUILayout.Foldout(_showSeasonNames, $"Season Names ({_config.numberOfSeasons})", true);
            if (_showSeasonNames)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < _config.seasonNames.Length; i++)
                {
                    _config.seasonNames[i] = EditorGUILayout.TextField($"Season {i + 1}", _config.seasonNames[i]);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "Celestial body orbital mechanics are configured PER SEASON below. " +
                "Each season can have different orbital angles, elevations, and celestial behaviors.",
                MessageType.Info
            );
        }

        private void DrawCelestialBodiesSection()
        {
            EditorGUILayout.LabelField("Celestial Body Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Define which celestial bodies exist in your world. Their seasonal behavior " +
                "(orbital mechanics, appearance) will be configured in the generated SeasonalData assets.",
                MessageType.Info
            );

            EditorGUILayout.Space(5);

            // Suns
            DrawCelestialBodyList("Suns", ref _showSuns, _config.suns, "Sun");

            EditorGUILayout.Space(10);

            // Moons
            DrawCelestialBodyList("Moons", ref _showMoons, _config.moons, "Moon");
        }

        private void DrawCelestialBodyList(string label, ref bool foldout, List<CelestialBodyIdentity> bodies, string defaultName)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            foldout = EditorGUILayout.Foldout(foldout, $"{label} ({bodies.Count})", true);
            
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                int count = bodies.Count(b => b.name.Contains(defaultName));
                string newName = count == 0 ? defaultName : $"{defaultName} {count + 1}";
                bodies.Add(new CelestialBodyIdentity { name = newName, createDirectionalLight = true });
            }
            
            EditorGUILayout.EndHorizontal();

            if (foldout)
            {
                EditorGUI.indentLevel++;

                for (int i = 0; i < bodies.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();

                    bodies[i].name = EditorGUILayout.TextField(
                        new GUIContent($"{label.TrimEnd('s')} {i + 1}", "Unique name for this celestial body"),
                        bodies[i].name
                    );

                    bodies[i].createDirectionalLight = EditorGUILayout.Toggle(
                        new GUIContent("Light", "Create a directional light component"),
                        bodies[i].createDirectionalLight,
                        GUILayout.Width(50)
                    );

                    if (GUILayout.Button("×", GUILayout.Width(25)))
                    {
                        bodies.RemoveAt(i);
                        break;
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSkyAndFogSection()
        {
            _config.createSkyAndFog = EditorGUILayout.Toggle(
                new GUIContent("Create Sky & Fog", "Set up HDRP sky and fog volumes"),
                _config.createSkyAndFog
            );

            if (_config.createSkyAndFog)
            {
                EditorGUI.indentLevel++;
                _config.hdrpProfilePath = EditorGUILayout.TextField(
                    new GUIContent("HDRP Profile Path", "Path to HDRP volume profile asset"),
                    _config.hdrpProfilePath
                );
                EditorGUI.indentLevel--;

                EditorGUILayout.HelpBox(
                    "Atmospheric settings (colors, fog, exposure) are configured per-season in SeasonalData assets.",
                    MessageType.Info
                );
            }
        }

        private void DrawSceneSetupSection()
        {
            EditorGUILayout.LabelField("Scene Setup", EditorStyles.boldLabel);

            _config.createTimeManager = EditorGUILayout.Toggle(
                new GUIContent("Create Time Manager", "Create TimeManager GameObject in scene"),
                _config.createTimeManager
            );

            _config.createWorldTimeData = EditorGUILayout.Toggle(
                new GUIContent("Create World Time Data", "Generate WorldTimeData ScriptableObject"),
                _config.createWorldTimeData
            );
        }

        private void DrawPathConfigurationSection()
        {
            EditorGUILayout.LabelField("Asset Paths", EditorStyles.boldLabel);

            _config.dataFolderPath = EditorGUILayout.TextField(
                new GUIContent("Data Folder", "Where to save ScriptableObject assets"),
                _config.dataFolderPath
            );

            _config.prefabFolderPath = EditorGUILayout.TextField(
                new GUIContent("Prefab Folder", "Where to save prefab assets"),
                _config.prefabFolderPath
            );
        }

        private void DrawDemoContentSection()
        {
            EditorGUILayout.LabelField("Demo Content", EditorStyles.boldLabel);

            _config.createDemoScene = EditorGUILayout.Toggle(
                new GUIContent("Create Demo Scene", "Generate a demo scene with Sol system"),
                _config.createDemoScene
            );

            if (_config.createDemoScene)
            {
                EditorGUILayout.HelpBox("A demo scene will be created with the configured Sol system.", MessageType.Info);
            }
        }

        private void DrawValidationSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Configuration Validation", EditorStyles.boldLabel);
            
            if (GUILayout.Button(_showValidation ? "Hide" : "Validate", GUILayout.Width(80)))
            {
                _showValidation = !_showValidation;
                if (_showValidation)
                {
                    _validationErrors = _config.Validate();
                }
            }
            EditorGUILayout.EndHorizontal();

            if (_showValidation)
            {
                if (_validationErrors.Count == 0)
                {
                    EditorGUILayout.HelpBox("✓ Configuration is valid!", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox($"Found {_validationErrors.Count} issue(s):", MessageType.Error);
                    foreach (string error in _validationErrors)
                    {
                        EditorGUILayout.LabelField("• " + error, EditorStyles.wordWrappedLabel);
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Generate Sol System", GUILayout.Height(40)))
            {
                GenerateSolSystem();
            }

            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(40)))
            {
                if (EditorUtility.DisplayDialog(
                    "Reset Configuration",
                    "Are you sure you want to reset all settings to defaults?",
                    "Reset",
                    "Cancel"))
                {
                    _config = new SetupConfig();
                    CalendarPresets.ApplySolPreset(_config);
                    _selectedTimeScalePreset = TimeScalePreset.Fast;
                    _useCustomTimeScale = false;
                    GUI.FocusControl(null);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Helper Methods

        private string GetTimeScaleDescription(float timeScale)
        {
            if (timeScale <= 0) return "Invalid time scale!";

            float realSecondsPerGameMinute = 60f / timeScale;
            float realSecondsPerGameHour = 3600f / timeScale;

            string description = $"Time Scale: {timeScale}x\n";
            description += $"• 1 real second = {timeScale:F1} game seconds\n";
            description += $"• 1 game minute = {realSecondsPerGameMinute:F2} real seconds\n";
            description += $"• 1 game hour = {FormatDuration(realSecondsPerGameHour)}";

            return description;
        }

        private string FormatDuration(float seconds)
        {
            if (seconds < 60)
                return $"{seconds:F1}s";
            if (seconds < 3600)
                return $"{seconds / 60f:F1}m";
            return $"{seconds / 3600f:F2}h";
        }

        #endregion

        #region Generation
        
        private void GenerateSolSystem()
        {
            Debug.Log("[SolSetupWizard] Starting Sol system generation...");

            try
            {
                // Validate configuration
                var errors = _config.Validate();
                if (errors.Count > 0)
                {
                    string errorMessage = "Cannot generate Sol system due to configuration errors:\n\n";
                    errorMessage += string.Join("\n", errors);
                    
                    EditorUtility.DisplayDialog(
                        "Configuration Errors",
                        errorMessage,
                        "OK"
                    );
                    
                    Debug.LogError("[SolSetupWizard] Validation failed:\n" + errorMessage);
                    return;
                }

                // Confirm generation
                bool confirmed = EditorUtility.DisplayDialog(
                    "Generate Sol System",
                    "This will create:\n\n" +
                    $"• WorldTimeData asset\n" +
                    $"• {_config.numberOfSeasons} SeasonalData assets\n" +
                    $"• TimeManager in scene\n" +
                    $"• {_config.suns.Count} sun object(s)\n" +
                    $"• {_config.moons.Count} moon object(s)\n\n" +
                    "Continue?",
                    "Generate",
                    "Cancel"
                );

                if (!confirmed)
                {
                    Debug.Log("[SolSetupWizard] Generation cancelled by user");
                    return;
                }

                // Initialize seasonal configs if using them
                if (_config.seasonConfigs == null || _config.seasonConfigs.Count != _config.numberOfSeasons)
                {
                    bool initializeConfigs = EditorUtility.DisplayDialog(
                        "Initialize Seasonal Configs?",
                        "Seasonal configurations are not set up. Would you like to use default values?\n\n" +
                        "You can customize these later in the 'Seasonal Config' tab.",
                        "Use Defaults",
                        "Skip"
                    );

                    if (initializeConfigs)
                    {
                        _config.InitializeSeasonalConfigs();
                    }
                }

                // Call the utility
                SolSetupUtilities.GenerateSolSystem(_config);

                Debug.Log("[SolSetupWizard] ✓ Generation complete!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SolSetupWizard] Generation failed: {e.Message}\n{e.StackTrace}");
                
                EditorUtility.DisplayDialog(
                    "Error",
                    $"Failed to generate Sol system:\n\n{e.Message}\n\nSee Console for details.",
                    "OK"
                );
            }
        }


        #endregion
    }
}
