using UnityEngine;
using UnityEditor;

namespace Sol
{
    /// <summary>
    /// Custom editor for SeasonalData ScriptableObject
    /// Provides conditional field display based on settings to reduce inspector clutter
    /// Shows only relevant fields based on axis modes, sync settings, and weather configuration
    /// Updated to include weather system configuration UI and Primary Star sync warnings
    /// </summary>
    [CustomEditor(typeof(SeasonalData))]
    public class SeasonalDataEditor : Editor
    {
        private SerializedProperty season;
        private SerializedProperty seasonDescription;
        
        // Primary Star Properties
        private SerializedProperty primaryStarActive;
        private SerializedProperty primaryXAxisEnabled;
        private SerializedProperty primaryXAxisMode;
        private SerializedProperty primaryXAxisSpeed;
        private SerializedProperty primaryXAxisMinRange;
        private SerializedProperty primaryXAxisMaxRange;
        private SerializedProperty primaryYAxisEnabled;
        private SerializedProperty primaryYAxisMode;
        private SerializedProperty primaryYAxisSpeed;
        private SerializedProperty primaryYAxisMinRange;
        private SerializedProperty primaryYAxisMaxRange;
        private SerializedProperty primarySyncXWithY;
        
        // Red Dwarf Properties
        private SerializedProperty redDwarfActive;
        private SerializedProperty redDwarfXAxisEnabled;
        private SerializedProperty redDwarfXAxisMode;
        private SerializedProperty redDwarfXAxisSpeed;
        private SerializedProperty redDwarfXAxisMinRange;
        private SerializedProperty redDwarfXAxisMaxRange;
        private SerializedProperty redDwarfYAxisEnabled;
        private SerializedProperty redDwarfYAxisMode;
        private SerializedProperty redDwarfYAxisSpeed;
        private SerializedProperty redDwarfYAxisMinRange;
        private SerializedProperty redDwarfYAxisMaxRange;
        private SerializedProperty redDwarfSyncXWithY;

        // Weather Properties
        private SerializedProperty weatherEnabled;
        private SerializedProperty snowChancePerDay;
        private SerializedProperty minSnowDurationHours;
        private SerializedProperty maxSnowDurationHours;
        private SerializedProperty minClearDurationHours;
        private SerializedProperty maxClearDurationHours;
        private SerializedProperty weatherCheckIntervalHours;

        private void OnEnable()
        {
            // Find all serialized properties
            season = serializedObject.FindProperty("season");
            seasonDescription = serializedObject.FindProperty("seasonDescription");
            
            // Primary Star
            primaryStarActive = serializedObject.FindProperty("primaryStarActive");
            primaryXAxisEnabled = serializedObject.FindProperty("primaryXAxisEnabled");
            primaryXAxisMode = serializedObject.FindProperty("primaryXAxisMode");
            primaryXAxisSpeed = serializedObject.FindProperty("primaryXAxisSpeed");
            primaryXAxisMinRange = serializedObject.FindProperty("primaryXAxisMinRange");
            primaryXAxisMaxRange = serializedObject.FindProperty("primaryXAxisMaxRange");
            primaryYAxisEnabled = serializedObject.FindProperty("primaryYAxisEnabled");
            primaryYAxisMode = serializedObject.FindProperty("primaryYAxisMode");
            primaryYAxisSpeed = serializedObject.FindProperty("primaryYAxisSpeed");
            primaryYAxisMinRange = serializedObject.FindProperty("primaryYAxisMinRange");
            primaryYAxisMaxRange = serializedObject.FindProperty("primaryYAxisMaxRange");
            primarySyncXWithY = serializedObject.FindProperty("primarySyncXWithY");
            
            // Red Dwarf
            redDwarfActive = serializedObject.FindProperty("redDwarfActive");
            redDwarfXAxisEnabled = serializedObject.FindProperty("redDwarfXAxisEnabled");
            redDwarfXAxisMode = serializedObject.FindProperty("redDwarfXAxisMode");
            redDwarfXAxisSpeed = serializedObject.FindProperty("redDwarfXAxisSpeed");
            redDwarfXAxisMinRange = serializedObject.FindProperty("redDwarfXAxisMinRange");
            redDwarfXAxisMaxRange = serializedObject.FindProperty("redDwarfXAxisMaxRange");
            redDwarfYAxisEnabled = serializedObject.FindProperty("redDwarfYAxisEnabled");
            redDwarfYAxisMode = serializedObject.FindProperty("redDwarfYAxisMode");
            redDwarfYAxisSpeed = serializedObject.FindProperty("redDwarfYAxisSpeed");
            redDwarfYAxisMinRange = serializedObject.FindProperty("redDwarfYAxisMinRange");
            redDwarfYAxisMaxRange = serializedObject.FindProperty("redDwarfYAxisMaxRange");
            redDwarfSyncXWithY = serializedObject.FindProperty("redDwarfSyncXWithY");

            // Weather
            weatherEnabled = serializedObject.FindProperty("weatherEnabled");
            snowChancePerDay = serializedObject.FindProperty("snowChancePerDay");
            minSnowDurationHours = serializedObject.FindProperty("minSnowDurationHours");
            maxSnowDurationHours = serializedObject.FindProperty("maxSnowDurationHours");
            minClearDurationHours = serializedObject.FindProperty("minClearDurationHours");
            maxClearDurationHours = serializedObject.FindProperty("maxClearDurationHours");
            weatherCheckIntervalHours = serializedObject.FindProperty("weatherCheckIntervalHours");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Season Information
            EditorGUILayout.LabelField("Season Information", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(season);
            EditorGUILayout.PropertyField(seasonDescription);
            
            EditorGUILayout.Space(10);

            // Primary Star Section
            DrawCelestialBodySection(
                "Primary Star",
                primaryStarActive,
                primaryXAxisEnabled, primaryXAxisMode, primaryXAxisSpeed, primaryXAxisMinRange, primaryXAxisMaxRange,
                primaryYAxisEnabled, primaryYAxisMode, primaryYAxisSpeed, primaryYAxisMinRange, primaryYAxisMaxRange,
                primarySyncXWithY
            );

            EditorGUILayout.Space(10);

            // Red Dwarf Section
            DrawCelestialBodySection(
                "Red Dwarf",
                redDwarfActive,
                redDwarfXAxisEnabled, redDwarfXAxisMode, redDwarfXAxisSpeed, redDwarfXAxisMinRange, redDwarfXAxisMaxRange,
                redDwarfYAxisEnabled, redDwarfYAxisMode, redDwarfYAxisSpeed, redDwarfYAxisMinRange, redDwarfYAxisMaxRange,
                redDwarfSyncXWithY
            );

            EditorGUILayout.Space(10);

            // Weather Section
            DrawWeatherSection();

            // Validation Section
            EditorGUILayout.Space(15);
            DrawValidationSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCelestialBodySection(
            string sectionName,
            SerializedProperty active,
            SerializedProperty xEnabled, SerializedProperty xMode, SerializedProperty xSpeed, SerializedProperty xMin, SerializedProperty xMax,
            SerializedProperty yEnabled, SerializedProperty yMode, SerializedProperty ySpeed, SerializedProperty yMin, SerializedProperty yMax,
            SerializedProperty syncXWithY)
        {
            // Section Header
            EditorGUILayout.LabelField(sectionName, EditorStyles.boldLabel);
            
            using (new EditorGUI.IndentLevelScope())
            {
                // Active toggle
                EditorGUILayout.PropertyField(active, new GUIContent("Active"));
                
                if (!active.boolValue)
                {
                    EditorGUILayout.HelpBox($"{sectionName} is disabled. Enable to configure settings.", MessageType.Info);
                    return;
                }

                EditorGUILayout.Space(5);

                // X-Axis Section
                EditorGUILayout.LabelField("X-Axis (Elevation)", EditorStyles.miniBoldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(xEnabled, new GUIContent("Enabled"));
                    
                    if (xEnabled.boolValue)
                    {
                        EditorGUILayout.PropertyField(xMode, new GUIContent("Mode"));
                        
                        // Show sync option only if Y-axis is enabled and continuous
                        if (yEnabled.boolValue && yMode.enumValueIndex == (int)CelestialRotationMode.Continuous)
                        {
                            EditorGUILayout.PropertyField(syncXWithY, new GUIContent("Sync with Y-Axis"));
                        }
                        
                        // Show speed only if not synced
                        if (!syncXWithY.boolValue || !yEnabled.boolValue || yMode.enumValueIndex != (int)CelestialRotationMode.Continuous)
                        {
                            EditorGUILayout.PropertyField(xSpeed, new GUIContent("Speed"));
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("X-Axis speed is synchronized with Y-Axis rotation.", MessageType.Info);
                        }
                        
                        // Show ranges for oscillate mode
                        if (xMode.enumValueIndex == (int)CelestialRotationMode.Oscillate)
                        {
                            EditorGUILayout.PropertyField(xMin, new GUIContent("Min Range (degrees)"));
                            EditorGUILayout.PropertyField(xMax, new GUIContent("Max Range (degrees)"));
                            
                            // Validation warning
                            if (xMin.floatValue >= xMax.floatValue)
                            {
                                EditorGUILayout.HelpBox("Min range must be less than Max range!", MessageType.Warning);
                            }
                        }
                    }
                }

                EditorGUILayout.Space(5);

                // Y-Axis Section
                EditorGUILayout.LabelField("Y-Axis (Azimuth)", EditorStyles.miniBoldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(yEnabled, new GUIContent("Enabled"));
                    
                    if (yEnabled.boolValue)
                    {
                        EditorGUILayout.PropertyField(yMode, new GUIContent("Mode"));
                        EditorGUILayout.PropertyField(ySpeed, new GUIContent("Speed"));
                        
                        // Primary Star Y-axis sync warning
                        if (sectionName == "Primary Star" && yMode.enumValueIndex == (int)CelestialRotationMode.Continuous)
                        {
                            // Check if TimeManager exists and has sync enabled
                            TimeManager timeManager = FindObjectOfType<TimeManager>();
                            if (timeManager != null && timeManager.EnforceCelestialDaySync)
                            {
                                SeasonalData seasonalData = (SeasonalData)target;
                                float expectedSpeed = seasonalData.GetRequiredCelestialYAxisSpeed(timeManager.DayLengthInSeconds);
        
                                if (!seasonalData.AreAllCelestialYAxisSyncedWithDay(timeManager.DayLengthInSeconds))
                                {
                                    EditorGUILayout.HelpBox($"Celestial Y-axis speeds will be automatically synchronized to {expectedSpeed:F3} deg/sec to match day length.", MessageType.Info);
                                }
                                else
                                {
                                    EditorGUILayout.HelpBox($"✓ All celestial Y-axis speeds are synchronized with day length ({expectedSpeed:F3} deg/sec).", MessageType.Info);
                                }
                            }
                            else
                            {
                                EditorGUILayout.HelpBox("Celestial Y-axis speeds should match day length when TimeManager's 'Enforce Celestial Day Sync' is enabled.", MessageType.Info);
                            }
                        }
                        
                        // Show ranges for oscillate mode
                        if (yMode.enumValueIndex == (int)CelestialRotationMode.Oscillate)
                        {
                            EditorGUILayout.PropertyField(yMin, new GUIContent("Min Range (degrees)"));
                            EditorGUILayout.PropertyField(yMax, new GUIContent("Max Range (degrees)"));
                            
                            // Validation warning
                            if (yMin.floatValue >= yMax.floatValue)
                            {
                                EditorGUILayout.HelpBox("Min range must be less than Max range!", MessageType.Warning);
                            }
                            
                            // Info about oscillation
                            EditorGUILayout.HelpBox("Y-Axis oscillation creates unusual orbital patterns. Use with caution!", MessageType.Info);
                        }
                    }
                }
            }
        }

        private void DrawWeatherSection()
        {
            EditorGUILayout.LabelField("Weather Configuration", EditorStyles.boldLabel);
            
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.PropertyField(weatherEnabled, new GUIContent("Weather Enabled"));
                
                if (!weatherEnabled.boolValue)
                {
                    EditorGUILayout.HelpBox("Weather is disabled for this season.", MessageType.Info);
                    return;
                }

                EditorGUILayout.Space(5);

                // Snow Probability
                EditorGUILayout.LabelField("Snow Probability", EditorStyles.miniBoldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.Slider(snowChancePerDay, 0f, 1f, new GUIContent("Snow Chance Per Day"));
                    
                    // Show percentage and seasonal context
                    float percentage = snowChancePerDay.floatValue * 100f;
                    string contextInfo = GetSeasonalWeatherContext(percentage);
                    EditorGUILayout.HelpBox($"{percentage:F0}% chance per day. {contextInfo}", MessageType.Info);
                }

                EditorGUILayout.Space(5);

                // Snow Duration
                EditorGUILayout.LabelField("Snow Duration", EditorStyles.miniBoldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(minSnowDurationHours, new GUIContent("Min Duration (hours)"));
                    EditorGUILayout.PropertyField(maxSnowDurationHours, new GUIContent("Max Duration (hours)"));
                    
                                        if (minSnowDurationHours.floatValue >= maxSnowDurationHours.floatValue)
                    {
                        EditorGUILayout.HelpBox("Min duration must be less than Max duration!", MessageType.Warning);
                    }
                }

                EditorGUILayout.Space(5);

                // Clear Weather Duration
                EditorGUILayout.LabelField("Clear Weather Duration", EditorStyles.miniBoldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(minClearDurationHours, new GUIContent("Min Duration (hours)"));
                    EditorGUILayout.PropertyField(maxClearDurationHours, new GUIContent("Max Duration (hours)"));
                    
                    if (minClearDurationHours.floatValue >= maxClearDurationHours.floatValue)
                    {
                        EditorGUILayout.HelpBox("Min duration must be less than Max duration!", MessageType.Warning);
                    }
                }

                EditorGUILayout.Space(5);

                // Weather Check Interval
                EditorGUILayout.LabelField("Weather System", EditorStyles.miniBoldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(weatherCheckIntervalHours, new GUIContent("Check Interval (hours)"));
                    EditorGUILayout.HelpBox("How often the weather system evaluates for changes. Lower values = more responsive weather.", MessageType.Info);
                }
            }
        }

        private string GetSeasonalWeatherContext(float percentage)
        {
            if (percentage >= 50f)
                return "Very snowy season - expect frequent snowfall.";
            else if (percentage >= 20f)
                return "Moderately snowy season - occasional snowfall.";
            else if (percentage >= 10f)
                return "Light snow season - rare snowfall.";
            else if (percentage > 0f)
                return "Minimal snow season - very rare snowfall.";
            else
                return "No snow expected this season.";
        }

        private void DrawValidationSection()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            
            SeasonalData seasonalData = (SeasonalData)target;
            
            if (seasonalData.IsValid())
            {
                EditorGUILayout.HelpBox("✓ All settings are valid!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("⚠ Some settings are invalid. Check celestial ranges, speeds, and weather durations.", MessageType.Warning);
            }

            // Quick setup buttons
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.miniBoldLabel);
            
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Reset to Defaults"))
                {
                    ResetToDefaults();
                }
                
                if (GUILayout.Button("Apply Weather Presets"))
                {
                    ShowWeatherPresetMenu();
                }
            }

            // Weather Statistics (if weather enabled)
            if (weatherEnabled.boolValue)
            {
                EditorGUILayout.Space(5);
                DrawWeatherStatistics();
            }
        }

        private void ShowWeatherPresetMenu()
        {
            GenericMenu menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Long Night (50% snow)"), false, () => ApplyWeatherPreset(0.5f, "Long Night"));
            menu.AddItem(new GUIContent("Transition 1 (20% snow)"), false, () => ApplyWeatherPreset(0.2f, "Transition 1"));
            menu.AddItem(new GUIContent("Transition 2 (10% snow)"), false, () => ApplyWeatherPreset(0.1f, "Transition 2"));
            menu.AddItem(new GUIContent("Equinox (5% snow)"), false, () => ApplyWeatherPreset(0.05f, "Equinox"));
            menu.AddItem(new GUIContent("Polar Summer (0% snow)"), false, () => ApplyWeatherPreset(0.0f, "Polar Summer"));
            
            menu.ShowAsContext();
        }

        private void ApplyWeatherPreset(float snowChance, string presetName)
        {
            snowChancePerDay.floatValue = snowChance;
            
            // Adjust durations based on season type
            if (snowChance >= 0.5f) // Long Night
            {
                minSnowDurationHours.floatValue = 4f;
                maxSnowDurationHours.floatValue = 12f;
                minClearDurationHours.floatValue = 2f;
                maxClearDurationHours.floatValue = 8f;
            }
            else if (snowChance >= 0.2f) // Transition seasons
            {
                minSnowDurationHours.floatValue = 2f;
                maxSnowDurationHours.floatValue = 6f;
                minClearDurationHours.floatValue = 6f;
                maxClearDurationHours.floatValue = 18f;
            }
            else if (snowChance > 0f) // Light snow seasons
            {
                minSnowDurationHours.floatValue = 1f;
                maxSnowDurationHours.floatValue = 3f;
                minClearDurationHours.floatValue = 12f;
                maxClearDurationHours.floatValue = 48f;
            }
            
            serializedObject.ApplyModifiedProperties();
            
            Debug.Log($"Applied {presetName} weather preset: {snowChance * 100f:F0}% snow chance per day");
        }

        private void DrawWeatherStatistics()
        {
            EditorGUILayout.LabelField("Weather Statistics", EditorStyles.miniBoldLabel);
            
            using (new EditorGUI.IndentLevelScope())
            {
                // Calculate expected weather patterns
                float snowChance = snowChancePerDay.floatValue;
                float avgSnowDuration = (minSnowDurationHours.floatValue + maxSnowDurationHours.floatValue) / 2f;
                float avgClearDuration = (minClearDurationHours.floatValue + maxClearDurationHours.floatValue) / 2f;
                
                // Expected snow hours per day
                float expectedSnowHoursPerDay = snowChance * avgSnowDuration;
                float expectedClearHoursPerDay = 24f - expectedSnowHoursPerDay;
                
                EditorGUILayout.LabelField($"Expected snow: {expectedSnowHoursPerDay:F1} hours/day ({(expectedSnowHoursPerDay/24f)*100f:F0}%)");
                EditorGUILayout.LabelField($"Expected clear: {expectedClearHoursPerDay:F1} hours/day ({(expectedClearHoursPerDay/24f)*100f:F0}%)");
                
                // Weather check frequency
                float checksPerDay = 24f / weatherCheckIntervalHours.floatValue;
                EditorGUILayout.LabelField($"Weather checks: {checksPerDay:F1} times/day");
                
                // Performance note
                if (checksPerDay > 24f)
                {
                    EditorGUILayout.HelpBox("High check frequency may impact performance.", MessageType.Info);
                }
            }
        }

        private void ResetToDefaults()
        {
            // Reset Primary Star to typical values
            primaryStarActive.boolValue = true;
            primaryXAxisEnabled.boolValue = true;
            primaryXAxisMode.enumValueIndex = (int)CelestialRotationMode.Oscillate;
            primaryXAxisSpeed.floatValue = 0.5f;
            primaryXAxisMinRange.floatValue = 0f;
            primaryXAxisMaxRange.floatValue = 90f;
            primaryYAxisEnabled.boolValue = true;
            primaryYAxisMode.enumValueIndex = (int)CelestialRotationMode.Continuous;
            primaryYAxisSpeed.floatValue = 15f;
            primaryYAxisMinRange.floatValue = 0f;
            primaryYAxisMaxRange.floatValue = 360f;
            primarySyncXWithY.boolValue = false;

            // Reset Red Dwarf to typical values
            redDwarfActive.boolValue = true;
            redDwarfXAxisEnabled.boolValue = true;
            redDwarfXAxisMode.enumValueIndex = (int)CelestialRotationMode.Oscillate;
            redDwarfXAxisSpeed.floatValue = 0.4f;
            redDwarfXAxisMinRange.floatValue = 0f;
            redDwarfXAxisMaxRange.floatValue = 70f;
            redDwarfYAxisEnabled.boolValue = true;
            redDwarfYAxisMode.enumValueIndex = (int)CelestialRotationMode.Continuous;
            redDwarfYAxisSpeed.floatValue = 15f;
            redDwarfYAxisMinRange.floatValue = 0f;
            redDwarfYAxisMaxRange.floatValue = 360f;
            redDwarfSyncXWithY.boolValue = false;

            // Reset Weather to moderate values
            weatherEnabled.boolValue = true;
            snowChancePerDay.floatValue = 0.2f;
            minSnowDurationHours.floatValue = 2f;
            maxSnowDurationHours.floatValue = 8f;
            minClearDurationHours.floatValue = 4f;
            maxClearDurationHours.floatValue = 16f;
            weatherCheckIntervalHours.floatValue = 1f;

            serializedObject.ApplyModifiedProperties();
        }
    }
}