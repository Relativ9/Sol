using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Sol
{
    [CustomEditor(typeof(SeasonalData))]
    public class SeasonalDataEditor : Editor
    {
        private bool _showStars = true;
        private bool _showMoons = true;
        private Dictionary<int, bool> _starFoldouts = new Dictionary<int, bool>();
        private Dictionary<int, bool> _moonFoldouts = new Dictionary<int, bool>();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Draw default script field
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((SeasonalData)target), typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            // Draw season configuration
            EditorGUILayout.PropertyField(serializedObject.FindProperty("season"));

            EditorGUILayout.Space();

            // Draw celestial body sections
            DrawStarsSection();
            DrawMoonsSection();

            EditorGUILayout.Space();

            // Draw weather section
            DrawWeatherSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStarsSection()
        {
            var starsProperty = serializedObject.FindProperty("stars");
            
            EditorGUILayout.BeginHorizontal();
            _showStars = EditorGUILayout.Foldout(_showStars, $"Stars ({starsProperty.arraySize})", true, EditorStyles.boldLabel);
            
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                starsProperty.InsertArrayElementAtIndex(starsProperty.arraySize);
                var newElement = starsProperty.GetArrayElementAtIndex(starsProperty.arraySize - 1);
                SetDefaultCelestialBodyValues(newElement, "New Star", false);
            }
            EditorGUILayout.EndHorizontal();

            if (_showStars)
            {
                EditorGUI.indentLevel++;
                
                for (int i = 0; i < starsProperty.arraySize; i++)
                {
                    DrawCelestialBodyElement(starsProperty, i, "Star", _starFoldouts, false);
                }
                
                if (starsProperty.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("No stars configured. Add at least one star for this season.", MessageType.Warning);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
        }

        private void DrawMoonsSection()
        {
            var moonsProperty = serializedObject.FindProperty("moons");
            
            EditorGUILayout.BeginHorizontal();
            _showMoons = EditorGUILayout.Foldout(_showMoons, $"Moons ({moonsProperty.arraySize})", true, EditorStyles.boldLabel);
            
            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                moonsProperty.InsertArrayElementAtIndex(moonsProperty.arraySize);
                var newElement = moonsProperty.GetArrayElementAtIndex(moonsProperty.arraySize - 1);
                SetDefaultCelestialBodyValues(newElement, "New Moon", true);
            }
            EditorGUILayout.EndHorizontal();

            if (_showMoons)
            {
                EditorGUI.indentLevel++;
                
                for (int i = 0; i < moonsProperty.arraySize; i++)
                {
                    DrawCelestialBodyElement(moonsProperty, i, "Moon", _moonFoldouts, true);
                }
                
                if (moonsProperty.arraySize == 0)
                {
                    EditorGUILayout.HelpBox("No moons configured. Moons are optional.", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
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

            // Header with name, active toggle, and delete button
            EditorGUILayout.BeginHorizontal();
            
            string displayName = string.IsNullOrEmpty(nameProperty.stringValue) ? $"Unnamed {typeName}" : nameProperty.stringValue;
            string headerText = $"{displayName} {(activeProperty.boolValue ? "" : "(Inactive)")}";
            
            foldouts[index] = EditorGUILayout.Foldout(foldouts[index], headerText, true);
            
            EditorGUILayout.PropertyField(activeProperty, GUIContent.none, GUILayout.Width(15));
            
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                arrayProperty.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            
            EditorGUILayout.EndHorizontal();

            if (foldouts[index])
            {
                EditorGUI.indentLevel++;
                
                // Basic configuration
                EditorGUILayout.PropertyField(nameProperty, new GUIContent("Name"));
                
                if (activeProperty.boolValue)
                {
                    // Moon-specific settings - ONLY DRAW ONCE HERE
                    if (isMoon)
                    {
                        EditorGUILayout.LabelField("Moon-Specific Settings", EditorStyles.miniBoldLabel);
                        EditorGUI.indentLevel++;
                        
                        var orbitalPeriodProperty = element.FindPropertyRelative("orbitalPeriod");
                        EditorGUILayout.PropertyField(orbitalPeriodProperty, new GUIContent("Orbital Period (Days)", "How many days for one complete orbit. Creates monthly drift effect."));
                        
                        if (orbitalPeriodProperty.floatValue <= 0)
                        {
                            EditorGUILayout.HelpBox("Orbital period must be greater than 0", MessageType.Error);
                        }
                        
                        var invertCycleProperty = element.FindPropertyRelative("invertDayNightCycle");
                        EditorGUILayout.PropertyField(invertCycleProperty, new GUIContent("Invert Day/Night Cycle", "Moon rises when stars set (useful for night moons)"));
                        
                        if (invertCycleProperty.boolValue)
                        {
                            EditorGUILayout.HelpBox("This moon will follow an inverted cycle - rising when stars set", MessageType.Info);
                        }
                        
                        EditorGUI.indentLevel--;
                        EditorGUILayout.Space();
                    }
                    
                    // X-Axis controls
                    DrawXAxisControls(element);
                    
                    // Y-Axis controls  
                    DrawYAxisControls(element);
                }
                else
                {
                    EditorGUILayout.HelpBox($"This {typeName.ToLower()} is inactive and will not be visible during this season.", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        // private void DrawCelestialBodyElement(SerializedProperty arrayProperty, int index, string typeName, Dictionary<int, bool> foldouts, bool isMoon)
        // {
        //     var element = arrayProperty.GetArrayElementAtIndex(index);
        //     var nameProperty = element.FindPropertyRelative("name");
        //     var activeProperty = element.FindPropertyRelative("active");
        //
        //     // Initialize foldout state if needed
        //     if (!foldouts.ContainsKey(index))
        //         foldouts[index] = false;
        //
        //     EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        //
        //     // Header with name, active toggle, and delete button
        //     EditorGUILayout.BeginHorizontal();
        //     
        //     string displayName = string.IsNullOrEmpty(nameProperty.stringValue) ? $"Unnamed {typeName}" : nameProperty.stringValue;
        //     string headerText = $"{displayName} {(activeProperty.boolValue ? "" : "(Inactive)")}";
        //     
        //     foldouts[index] = EditorGUILayout.Foldout(foldouts[index], headerText, true);
        //     
        //     EditorGUILayout.PropertyField(activeProperty, GUIContent.none, GUILayout.Width(15));
        //     
        //     if (GUILayout.Button("×", GUILayout.Width(20)))
        //     {
        //         arrayProperty.DeleteArrayElementAtIndex(index);
        //         EditorGUILayout.EndHorizontal();
        //         EditorGUILayout.EndVertical();
        //         return;
        //     }
        //     
        //     EditorGUILayout.EndHorizontal();
        //
        //     if (foldouts[index])
        //     {
        //         EditorGUI.indentLevel++;
        //         
        //         // Basic configuration
        //         EditorGUILayout.PropertyField(nameProperty, new GUIContent("Name"));
        //         
        //         if (activeProperty.boolValue)
        //         {
        //             // Moon-specific orbital period
        //             if (isMoon)
        //             {
        //                 var orbitalPeriodProperty = element.FindPropertyRelative("orbitalPeriod");
        //                 EditorGUILayout.PropertyField(orbitalPeriodProperty, new GUIContent("Orbital Period (Days)", "How many days for one complete orbit. Creates monthly drift effect."));
        //                 
        //                 if (orbitalPeriodProperty.floatValue <= 0)
        //                 {
        //                     EditorGUILayout.HelpBox("Orbital period must be greater than 0", MessageType.Error);
        //                 }
        //             }
        //             
        //             // X-Axis controls
        //             DrawXAxisControls(element);
        //             
        //             // Y-Axis controls  
        //             DrawYAxisControls(element);
        //         }
        //         else
        //         {
        //             EditorGUILayout.HelpBox($"This {typeName.ToLower()} is inactive and will not be visible during this season.", MessageType.Info);
        //         }
        //         
        //         if (isMoon)
        //         {
        //             var orbitalPeriodProperty = element.FindPropertyRelative("orbitalPeriod");
        //             EditorGUILayout.PropertyField(orbitalPeriodProperty, new GUIContent("Orbital Period (Days)", "How many days for one complete orbit. Creates monthly drift effect."));
        //
        //             if (orbitalPeriodProperty.floatValue <= 0)
        //             {
        //                 EditorGUILayout.HelpBox("Orbital period must be greater than 0", MessageType.Error);
        //             }
        //
        //             var invertCycleProperty = element.FindPropertyRelative("invertDayNightCycle");
        //             EditorGUILayout.PropertyField(invertCycleProperty, new GUIContent("Invert Day/Night Cycle", "Moon rises when stars set (useful for night moons)"));
        //
        //             if (invertCycleProperty.boolValue)
        //             {
        //                 EditorGUILayout.HelpBox("This moon will follow an inverted cycle - rising when stars set", MessageType.Info);
        //             }
        //         }
        //         
        //         EditorGUI.indentLevel--;
        //     }
        //     
        //
        //
        //     EditorGUILayout.EndVertical();
        //     EditorGUILayout.Space();
        //     
        // }

        private void DrawXAxisControls(SerializedProperty element)
        {
            EditorGUILayout.LabelField("X-Axis (Elevation)", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            var xAxisEnabledProperty = element.FindPropertyRelative("xAxisEnabled");
            EditorGUILayout.PropertyField(xAxisEnabledProperty, new GUIContent("Enabled"));
            
            if (xAxisEnabledProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                
                var xAxisModeProperty = element.FindPropertyRelative("xAxisMode");
                var syncXWithYProperty = element.FindPropertyRelative("syncXWithY");
                
                EditorGUILayout.PropertyField(xAxisModeProperty, new GUIContent("Rotation Mode"));
                EditorGUILayout.PropertyField(syncXWithYProperty, new GUIContent("Sync with Y-Axis", "Automatically sync X-axis speed with Y-axis rotation for realistic day cycles"));
                
                // Only show speed if not synced (for oscillate mode)
                if (xAxisModeProperty.enumValueIndex == (int)CelestialRotationMode.Oscillate)
                {
                    if (!syncXWithYProperty.boolValue)
                    {
                        var xAxisSpeedProperty = element.FindPropertyRelative("xAxisSpeed");
                        EditorGUILayout.PropertyField(xAxisSpeedProperty, new GUIContent("Speed", "Oscillation speed in radians per celestial time unit"));
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Speed is automatically calculated from Y-axis rotation", MessageType.Info);
                    }
                    
                    // Always show range controls for oscillate mode
                    var xAxisMinProperty = element.FindPropertyRelative("xAxisMinRange");
                    var xAxisMaxProperty = element.FindPropertyRelative("xAxisMaxRange");
                    EditorGUILayout.PropertyField(xAxisMinProperty, new GUIContent("Min Range", "Minimum elevation angle (180° = horizon, 120° = high in sky)"));
                    EditorGUILayout.PropertyField(xAxisMaxProperty, new GUIContent("Max Range", "Maximum elevation angle (180° = horizon, 120° = high in sky)"));
                    
                    if (xAxisMinProperty.floatValue > xAxisMaxProperty.floatValue)
                    {
                        EditorGUILayout.HelpBox("Min range should be less than max range", MessageType.Warning);
                    }
                }
                else // Continuous mode
                {
                    var xAxisSpeedProperty = element.FindPropertyRelative("xAxisSpeed");
                    EditorGUILayout.PropertyField(xAxisSpeedProperty, new GUIContent("Speed", "Rotation speed in degrees per celestial time unit"));
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawYAxisControls(SerializedProperty element)
        {
            EditorGUILayout.LabelField("Y-Axis (Azimuth)", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            var yAxisEnabledProperty = element.FindPropertyRelative("yAxisEnabled");
            EditorGUILayout.PropertyField(yAxisEnabledProperty, new GUIContent("Enabled"));
            
            if (yAxisEnabledProperty.boolValue)
            {
                EditorGUI.indentLevel++;
                
                var yAxisModeProperty = element.FindPropertyRelative("yAxisMode");
                EditorGUILayout.PropertyField(yAxisModeProperty, new GUIContent("Rotation Mode"));
                
                if (yAxisModeProperty.enumValueIndex == (int)CelestialRotationMode.Continuous)
                {
                    // For continuous mode, show day sync option
                    EditorGUILayout.HelpBox("Continuous mode syncs with TimeManager day length by default (360° per day)", MessageType.Info);
                    
                    var yAxisOverrideSpeedProperty = element.FindPropertyRelative("yAxisOverrideSpeed");
                    EditorGUILayout.PropertyField(yAxisOverrideSpeedProperty, new GUIContent("Override Day Sync", "Use custom speed instead of automatic day synchronization"));
                    
                    if (yAxisOverrideSpeedProperty.boolValue)
                    {
                        var yAxisSpeedProperty = element.FindPropertyRelative("yAxisSpeed");
                        EditorGUILayout.PropertyField(yAxisSpeedProperty, new GUIContent("Custom Speed", "Custom rotation speed in degrees per celestial time unit"));
                        EditorGUILayout.HelpBox("Using custom speed will break day/night synchronization", MessageType.Warning);
                    }
                }
                else // Oscillate mode
                {
                    var yAxisSpeedProperty = element.FindPropertyRelative("yAxisSpeed");
                    EditorGUILayout.PropertyField(yAxisSpeedProperty, new GUIContent("Speed", "Oscillation speed in radians per celestial time unit"));
                    
                    // Show range controls for oscillate mode on Y-axis too
                    var yAxisMinProperty = element.FindPropertyRelative("yAxisMinRange");
                    var yAxisMaxProperty = element.FindPropertyRelative("yAxisMaxRange");
                    EditorGUILayout.PropertyField(yAxisMinProperty, new GUIContent("Min Range", "Minimum azimuth angle in degrees"));
                    EditorGUILayout.PropertyField(yAxisMaxProperty, new GUIContent("Max Range", "Maximum azimuth angle in degrees"));
                    
                    if (yAxisMinProperty.floatValue > yAxisMaxProperty.floatValue)
                    {
                        EditorGUILayout.HelpBox("Min range should be less than max range", MessageType.Warning);
                    }
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawWeatherSection()
        {
            EditorGUILayout.LabelField("Weather Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("weatherData"), new GUIContent("Weather Data"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideWeatherEnabled"), new GUIContent("Override Weather Enabled"));
            
            // Show weather status
            var weatherData = serializedObject.FindProperty("weatherData");
            var overrideEnabled = serializedObject.FindProperty("overrideWeatherEnabled");
            
            if (weatherData.objectReferenceValue != null && !overrideEnabled.boolValue)
            {
                EditorGUILayout.HelpBox("Weather is ENABLED for this season", MessageType.Info);
            }
            else if (overrideEnabled.boolValue)
            {
                EditorGUILayout.HelpBox("Weather is DISABLED (overridden) for this season", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("No weather data assigned - weather will be disabled", MessageType.Warning);
            }
        }
        
        private void SetDefaultCelestialBodyValues(SerializedProperty element, string defaultName, bool isMoon)
        {
            element.FindPropertyRelative("name").stringValue = defaultName;
            element.FindPropertyRelative("active").boolValue = true;
            element.FindPropertyRelative("xAxisEnabled").boolValue = false;
            element.FindPropertyRelative("xAxisMode").enumValueIndex = (int)CelestialRotationMode.Oscillate;
            element.FindPropertyRelative("xAxisSpeed").floatValue = 0.1f;
            element.FindPropertyRelative("syncXWithY").boolValue = false;
            element.FindPropertyRelative("xAxisMinRange").floatValue = 120f;
            element.FindPropertyRelative("xAxisMaxRange").floatValue = 240f;
            element.FindPropertyRelative("yAxisEnabled").boolValue = true;
            element.FindPropertyRelative("yAxisMode").enumValueIndex = (int)CelestialRotationMode.Continuous;
            element.FindPropertyRelative("yAxisSpeed").floatValue = isMoon ? 0.15f : 0.25f;
            element.FindPropertyRelative("yAxisOverrideSpeed").boolValue = false;
            element.FindPropertyRelative("yAxisMinRange").floatValue = 0f;
            element.FindPropertyRelative("yAxisMaxRange").floatValue = 360f;
    
            if (isMoon)
            {
                element.FindPropertyRelative("orbitalPeriod").floatValue = 104f; // Default 1 month orbit
                element.FindPropertyRelative("invertDayNightCycle").boolValue = true; // Default to inverted for moons
            }
        }

        // private void SetDefaultCelestialBodyValues(SerializedProperty element, string defaultName, bool isMoon)
        // {
        //     element.FindPropertyRelative("name").stringValue = defaultName;
        //     element.FindPropertyRelative("active").boolValue = true;
        //     element.FindPropertyRelative("xAxisEnabled").boolValue = false;
        //     element.FindPropertyRelative("xAxisMode").enumValueIndex = (int)CelestialRotationMode.Oscillate;
        //     element.FindPropertyRelative("xAxisSpeed").floatValue = 0.1f;
        //     element.FindPropertyRelative("syncXWithY").boolValue = false;
        //     element.FindPropertyRelative("xAxisMinRange").floatValue = 120f;
        //     element.FindPropertyRelative("xAxisMaxRange").floatValue = 240f;
        //     element.FindPropertyRelative("yAxisEnabled").boolValue = true;
        //     element.FindPropertyRelative("yAxisMode").enumValueIndex = (int)CelestialRotationMode.Continuous;
        //     element.FindPropertyRelative("yAxisSpeed").floatValue = isMoon ? 0.15f : 0.25f;
        //     element.FindPropertyRelative("yAxisOverrideSpeed").boolValue = false;
        //     element.FindPropertyRelative("yAxisMinRange").floatValue = 0f;
        //     element.FindPropertyRelative("yAxisMaxRange").floatValue = 360f;
        //     
        //     if (isMoon)
        //     {
        //         element.FindPropertyRelative("orbitalPeriod").floatValue = 104f; // Default 1 month orbit
        //     }
        // }
    }
}