// using UnityEngine;
// using UnityEditor;
// using System.Collections.Generic;
//
// namespace Sol
// {
//     [CustomEditor(typeof(SeasonalData))]
//     public class SeasonalDataEditor : Editor
//     {
//         private bool _showStars = true;
//         private bool _showMoons = true;
//         private Dictionary<int, bool> _starFoldouts = new Dictionary<int, bool>();
//         private Dictionary<int, bool> _moonFoldouts = new Dictionary<int, bool>();
//
//         public override void OnInspectorGUI()
//         {
//             serializedObject.Update();
//
//             // Draw default script field
//             EditorGUI.BeginDisabledGroup(true);
//             EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((SeasonalData)target), typeof(MonoScript), false);
//             EditorGUI.EndDisabledGroup();
//
//             EditorGUILayout.Space();
//
//             // Draw season configuration
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("season"));
//
//             EditorGUILayout.Space();
//
//             // Draw celestial body sections
//             DrawStarsSection();
//             DrawMoonsSection();
//
//             EditorGUILayout.Space();
//
//             // Draw weather section
//             DrawWeatherSection();
//
//             serializedObject.ApplyModifiedProperties();
//         }
//
//         private void DrawStarsSection()
//         {
//             var starsProperty = serializedObject.FindProperty("stars");
//             
//             EditorGUILayout.BeginHorizontal();
//             _showStars = EditorGUILayout.Foldout(_showStars, $"Stars ({starsProperty.arraySize})", true, EditorStyles.boldLabel);
//             
//             if (GUILayout.Button("+", GUILayout.Width(25)))
//             {
//                 starsProperty.InsertArrayElementAtIndex(starsProperty.arraySize);
//                 var newElement = starsProperty.GetArrayElementAtIndex(starsProperty.arraySize - 1);
//                 SetDefaultCelestialBodyValues(newElement, "New Star", false);
//             }
//             EditorGUILayout.EndHorizontal();
//
//             if (_showStars)
//             {
//                 EditorGUI.indentLevel++;
//                 
//                 for (int i = 0; i < starsProperty.arraySize; i++)
//                 {
//                     DrawCelestialBodyElement(starsProperty, i, "Star", _starFoldouts, false);
//                 }
//                 
//                 if (starsProperty.arraySize == 0)
//                 {
//                     EditorGUILayout.HelpBox("No stars configured. Add at least one star for this season.", MessageType.Warning);
//                 }
//                 
//                 EditorGUI.indentLevel--;
//             }
//
//             EditorGUILayout.Space();
//         }
//
//         private void DrawMoonsSection()
//         {
//             var moonsProperty = serializedObject.FindProperty("moons");
//             
//             EditorGUILayout.BeginHorizontal();
//             _showMoons = EditorGUILayout.Foldout(_showMoons, $"Moons ({moonsProperty.arraySize})", true, EditorStyles.boldLabel);
//             
//             if (GUILayout.Button("+", GUILayout.Width(25)))
//             {
//                 moonsProperty.InsertArrayElementAtIndex(moonsProperty.arraySize);
//                 var newElement = moonsProperty.GetArrayElementAtIndex(moonsProperty.arraySize - 1);
//                 SetDefaultCelestialBodyValues(newElement, "New Moon", true);
//             }
//             EditorGUILayout.EndHorizontal();
//
//             if (_showMoons)
//             {
//                 EditorGUI.indentLevel++;
//                 
//                 for (int i = 0; i < moonsProperty.arraySize; i++)
//                 {
//                     DrawCelestialBodyElement(moonsProperty, i, "Moon", _moonFoldouts, true);
//                 }
//                 
//                 if (moonsProperty.arraySize == 0)
//                 {
//                     EditorGUILayout.HelpBox("No moons configured. Moons are optional.", MessageType.Info);
//                 }
//                 
//                 EditorGUI.indentLevel--;
//             }
//
//             EditorGUILayout.Space();
//         }
//         
//         private void DrawCelestialBodyElement(SerializedProperty arrayProperty, int index, string typeName, Dictionary<int, bool> foldouts, bool isMoon)
//         {
//             var element = arrayProperty.GetArrayElementAtIndex(index);
//             var nameProperty = element.FindPropertyRelative("name");
//             var activeProperty = element.FindPropertyRelative("active");
//
//             // Initialize foldout state if needed
//             if (!foldouts.ContainsKey(index))
//                 foldouts[index] = false;
//
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//
//             // Header with name, active toggle, and delete button
//             EditorGUILayout.BeginHorizontal();
//             
//             string displayName = string.IsNullOrEmpty(nameProperty.stringValue) ? $"Unnamed {typeName}" : nameProperty.stringValue;
//             string headerText = $"{displayName} {(activeProperty.boolValue ? "" : "(Inactive)")}";
//             
//             foldouts[index] = EditorGUILayout.Foldout(foldouts[index], headerText, true);
//             
//             EditorGUILayout.PropertyField(activeProperty, GUIContent.none, GUILayout.Width(15));
//             
//             if (GUILayout.Button("×", GUILayout.Width(20)))
//             {
//                 arrayProperty.DeleteArrayElementAtIndex(index);
//                 EditorGUILayout.EndHorizontal();
//                 EditorGUILayout.EndVertical();
//                 return;
//             }
//             
//             EditorGUILayout.EndHorizontal();
//
//             if (foldouts[index])
//             {
//                 EditorGUI.indentLevel++;
//                 
//                 // Basic configuration
//                 EditorGUILayout.PropertyField(nameProperty, new GUIContent("Name"));
//                 
//                 if (activeProperty.boolValue)
//                 {
//                     // Moon-specific settings - ONLY DRAW ONCE HERE
//                     if (isMoon)
//                     {
//                         EditorGUILayout.LabelField("Moon-Specific Settings", EditorStyles.miniBoldLabel);
//                         EditorGUI.indentLevel++;
//                         
//                         var orbitalPeriodProperty = element.FindPropertyRelative("orbitalPeriod");
//                         EditorGUILayout.PropertyField(orbitalPeriodProperty, new GUIContent("Orbital Period (Days)", "How many days for one complete orbit. Creates monthly drift effect."));
//                         
//                         if (orbitalPeriodProperty.floatValue <= 0)
//                         {
//                             EditorGUILayout.HelpBox("Orbital period must be greater than 0", MessageType.Error);
//                         }
//                         
//                         var invertCycleProperty = element.FindPropertyRelative("invertDayNightCycle");
//                         EditorGUILayout.PropertyField(invertCycleProperty, new GUIContent("Invert Day/Night Cycle", "Moon rises when stars set (useful for night moons)"));
//                         
//                         if (invertCycleProperty.boolValue)
//                         {
//                             EditorGUILayout.HelpBox("This moon will follow an inverted cycle - rising when stars set", MessageType.Info);
//                         }
//                         
//                         EditorGUI.indentLevel--;
//                         EditorGUILayout.Space();
//                     }
//                     
//                     // X-Axis controls
//                     DrawXAxisControls(element);
//                     
//                     // Y-Axis controls  
//                     DrawYAxisControls(element);
//                 }
//                 else
//                 {
//                     EditorGUILayout.HelpBox($"This {typeName.ToLower()} is inactive and will not be visible during this season.", MessageType.Info);
//                 }
//                 
//                 EditorGUI.indentLevel--;
//             }
//
//             EditorGUILayout.EndVertical();
//             EditorGUILayout.Space();
//         }
//
//         private void DrawXAxisControls(SerializedProperty element)
//         {
//             EditorGUILayout.LabelField("X-Axis (Elevation)", EditorStyles.miniBoldLabel);
//             EditorGUI.indentLevel++;
//
//             var xAxisEnabledProperty = element.FindPropertyRelative("xAxisEnabled");
//             EditorGUILayout.PropertyField(xAxisEnabledProperty, new GUIContent("Enabled"));
//             
//             // if (xAxisEnabledProperty.boolValue)
//             // {
//             //     EditorGUI.indentLevel++;
//             //     
//             //     var xAxisModeProperty = element.FindPropertyRelative("xAxisMode");
//             //     var syncXWithYProperty = element.FindPropertyRelative("syncXWithY");
//             //     
//             //     EditorGUILayout.PropertyField(xAxisModeProperty, new GUIContent("Rotation Mode"));
//             //     EditorGUILayout.PropertyField(syncXWithYProperty, new GUIContent("Sync with Y-Axis", "Automatically sync X-axis speed with Y-axis rotation for realistic day cycles"));
//             //     
//             //     // Only show speed if not synced (for oscillate mode)
//             //     if (xAxisModeProperty.enumValueIndex == (int)CelestialRotationMode.Oscillate)
//             //     {
//             //         if (!syncXWithYProperty.boolValue)
//             //         {
//             //             var xAxisSpeedProperty = element.FindPropertyRelative("xAxisSpeed");
//             //             EditorGUILayout.PropertyField(xAxisSpeedProperty, new GUIContent("Speed", "Oscillation speed in radians per celestial time unit"));
//             //         }
//             //         else
//             //         {
//             //             EditorGUILayout.HelpBox("Speed is automatically calculated from Y-axis rotation", MessageType.Info);
//             //         }
//             //         
//             //         // Always show range controls for oscillate mode
//             //         var xAxisMinProperty = element.FindPropertyRelative("xAxisMinRange");
//             //         var xAxisMaxProperty = element.FindPropertyRelative("xAxisMaxRange");
//             //         EditorGUILayout.PropertyField(xAxisMinProperty, new GUIContent("Min Range", "Minimum elevation angle (180° = horizon, 120° = high in sky)"));
//             //         EditorGUILayout.PropertyField(xAxisMaxProperty, new GUIContent("Max Range", "Maximum elevation angle (180° = horizon, 120° = high in sky)"));
//             //         
//             //         if (xAxisMinProperty.floatValue > xAxisMaxProperty.floatValue)
//             //         {
//             //             EditorGUILayout.HelpBox("Min range should be less than max range", MessageType.Warning);
//             //         }
//             //     }
//             //     else // Continuous mode
//             //     {
//             //         var xAxisSpeedProperty = element.FindPropertyRelative("xAxisSpeed");
//             //         EditorGUILayout.PropertyField(xAxisSpeedProperty, new GUIContent("Speed", "Rotation speed in degrees per celestial time unit"));
//             //     }
//             
//                 if (xAxisEnabledProperty.boolValue)
//                 {
//                     var usePathCalcProperty = element.FindPropertyRelative("usePathAngleCalculation");
//                     EditorGUILayout.PropertyField(usePathCalcProperty, new GUIContent("Use Path Angle Calculation"));
//                     
//                     if (usePathCalcProperty.boolValue)
//                     {
//                         // Path angle configuration
//                         var pathAngleProperty = element.FindPropertyRelative("desiredPathAngle");
//                         var sunriseElevationProperty = element.FindPropertyRelative("sunriseElevation");
//                         
//                         EditorGUILayout.PropertyField(pathAngleProperty, new GUIContent("Path Angle", "Angle of sun's path across sky"));
//                         EditorGUILayout.PropertyField(sunriseElevationProperty, new GUIContent("Sunrise Elevation", "Elevation when sun rises (180°=horizon)"));
//                         
//                         // Calculate and display the resulting ranges
//                         float minRange, maxRange;
//                         CelestialCalculator.CalculatePathRanges(pathAngleProperty.floatValue, sunriseElevationProperty.floatValue, out minRange, out maxRange);
//                         
//                         EditorGUILayout.HelpBox($"Calculated ranges: Min={minRange:F1}°, Max={maxRange:F1}°", MessageType.Info);
//                         
//                         // Auto-apply the calculated values
//                         element.FindPropertyRelative("xAxisMinRange").floatValue = minRange;
//                         element.FindPropertyRelative("xAxisMaxRange").floatValue = maxRange;
//                     }
//                     else
//                     {
//                         // Manual range configuration (your current system)
//                         var xAxisMinProperty = element.FindPropertyRelative("xAxisMinRange");
//                         var xAxisMaxProperty = element.FindPropertyRelative("xAxisMaxRange");
//                         
//                         EditorGUILayout.PropertyField(xAxisMinProperty, new GUIContent("Min Range"));
//                         EditorGUILayout.PropertyField(xAxisMaxProperty, new GUIContent("Max Range"));
//                         
//                         // Warn about gimbal lock
//                         if ((xAxisMinProperty.floatValue >= 80f && xAxisMinProperty.floatValue <= 100f) ||
//                             (xAxisMaxProperty.floatValue >= 80f && xAxisMaxProperty.floatValue <= 100f))
//                         {
//                             EditorGUILayout.HelpBox("Warning: Values near 90° may cause gimbal lock issues", MessageType.Warning);
//                         }
//                     }
//                 
//                 EditorGUI.indentLevel--;
//             }
//
//             EditorGUI.indentLevel--;
//             EditorGUILayout.Space();
//         }
//
//         private void DrawYAxisControls(SerializedProperty element)
//         {
//             EditorGUILayout.LabelField("Y-Axis (Azimuth)", EditorStyles.miniBoldLabel);
//             EditorGUI.indentLevel++;
//
//             var yAxisEnabledProperty = element.FindPropertyRelative("yAxisEnabled");
//             EditorGUILayout.PropertyField(yAxisEnabledProperty, new GUIContent("Enabled"));
//             
//             if (yAxisEnabledProperty.boolValue)
//             {
//                 EditorGUI.indentLevel++;
//                 
//                 var yAxisModeProperty = element.FindPropertyRelative("yAxisMode");
//                 EditorGUILayout.PropertyField(yAxisModeProperty, new GUIContent("Rotation Mode"));
//                 
//                 if (yAxisModeProperty.enumValueIndex == (int)CelestialRotationMode.Continuous)
//                 {
//                     // For continuous mode, show day sync option
//                     EditorGUILayout.HelpBox("Continuous mode syncs with TimeManager day length by default (360° per day)", MessageType.Info);
//                     
//                     var yAxisOverrideSpeedProperty = element.FindPropertyRelative("yAxisOverrideSpeed");
//                     EditorGUILayout.PropertyField(yAxisOverrideSpeedProperty, new GUIContent("Override Day Sync", "Use custom speed instead of automatic day synchronization"));
//                     
//                     if (yAxisOverrideSpeedProperty.boolValue)
//                     {
//                         var yAxisSpeedProperty = element.FindPropertyRelative("yAxisSpeed");
//                         EditorGUILayout.PropertyField(yAxisSpeedProperty, new GUIContent("Custom Speed", "Custom rotation speed in degrees per celestial time unit"));
//                         EditorGUILayout.HelpBox("Using custom speed will break day/night synchronization", MessageType.Warning);
//                     }
//                 }
//                 else // Oscillate mode
//                 {
//                     var yAxisSpeedProperty = element.FindPropertyRelative("yAxisSpeed");
//                     EditorGUILayout.PropertyField(yAxisSpeedProperty, new GUIContent("Speed", "Oscillation speed in radians per celestial time unit"));
//                     
//                     // Show range controls for oscillate mode on Y-axis too
//                     var yAxisMinProperty = element.FindPropertyRelative("yAxisMinRange");
//                     var yAxisMaxProperty = element.FindPropertyRelative("yAxisMaxRange");
//                     EditorGUILayout.PropertyField(yAxisMinProperty, new GUIContent("Min Range", "Minimum azimuth angle in degrees"));
//                     EditorGUILayout.PropertyField(yAxisMaxProperty, new GUIContent("Max Range", "Maximum azimuth angle in degrees"));
//                     
//                     if (yAxisMinProperty.floatValue > yAxisMaxProperty.floatValue)
//                     {
//                         EditorGUILayout.HelpBox("Min range should be less than max range", MessageType.Warning);
//                     }
//                 }
//                 
//                 EditorGUI.indentLevel--;
//             }
//
//             EditorGUI.indentLevel--;
//             EditorGUILayout.Space();
//         }
//
//         private void DrawWeatherSection()
//         {
//             EditorGUILayout.LabelField("Weather Settings", EditorStyles.boldLabel);
//             
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("weatherData"), new GUIContent("Weather Data"));
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("overrideWeatherEnabled"), new GUIContent("Override Weather Enabled"));
//             
//             // Show weather status
//             var weatherData = serializedObject.FindProperty("weatherData");
//             var overrideEnabled = serializedObject.FindProperty("overrideWeatherEnabled");
//             
//             if (weatherData.objectReferenceValue != null && !overrideEnabled.boolValue)
//             {
//                 EditorGUILayout.HelpBox("Weather is ENABLED for this season", MessageType.Info);
//             }
//             else if (overrideEnabled.boolValue)
//             {
//                 EditorGUILayout.HelpBox("Weather is DISABLED (overridden) for this season", MessageType.Warning);
//             }
//             else
//             {
//                 EditorGUILayout.HelpBox("No weather data assigned - weather will be disabled", MessageType.Warning);
//             }
//         }
//         
//         private void SetDefaultCelestialBodyValues(SerializedProperty element, string defaultName, bool isMoon)
//         {
//             element.FindPropertyRelative("name").stringValue = defaultName;
//             element.FindPropertyRelative("active").boolValue = true;
//             element.FindPropertyRelative("xAxisEnabled").boolValue = false;
//             element.FindPropertyRelative("xAxisMode").enumValueIndex = (int)CelestialRotationMode.Oscillate;
//             element.FindPropertyRelative("xAxisSpeed").floatValue = 0.1f;
//             element.FindPropertyRelative("syncXWithY").boolValue = false;
//             element.FindPropertyRelative("xAxisMinRange").floatValue = 120f;
//             element.FindPropertyRelative("xAxisMaxRange").floatValue = 240f;
//             element.FindPropertyRelative("yAxisEnabled").boolValue = true;
//             element.FindPropertyRelative("yAxisMode").enumValueIndex = (int)CelestialRotationMode.Continuous;
//             element.FindPropertyRelative("yAxisSpeed").floatValue = isMoon ? 0.15f : 0.25f;
//             element.FindPropertyRelative("yAxisOverrideSpeed").boolValue = false;
//             element.FindPropertyRelative("yAxisMinRange").floatValue = 0f;
//             element.FindPropertyRelative("yAxisMaxRange").floatValue = 360f;
//     
//             if (isMoon)
//             {
//                 element.FindPropertyRelative("orbitalPeriod").floatValue = 104f; // Default 1 month orbit
//                 element.FindPropertyRelative("invertDayNightCycle").boolValue = true; // Default to inverted for moons
//             }
//         }
//
//         // private void SetDefaultCelestialBodyValues(SerializedProperty element, string defaultName, bool isMoon)
//         // {
//         //     element.FindPropertyRelative("name").stringValue = defaultName;
//         //     element.FindPropertyRelative("active").boolValue = true;
//         //     element.FindPropertyRelative("xAxisEnabled").boolValue = false;
//         //     element.FindPropertyRelative("xAxisMode").enumValueIndex = (int)CelestialRotationMode.Oscillate;
//         //     element.FindPropertyRelative("xAxisSpeed").floatValue = 0.1f;
//         //     element.FindPropertyRelative("syncXWithY").boolValue = false;
//         //     element.FindPropertyRelative("xAxisMinRange").floatValue = 120f;
//         //     element.FindPropertyRelative("xAxisMaxRange").floatValue = 240f;
//         //     element.FindPropertyRelative("yAxisEnabled").boolValue = true;
//         //     element.FindPropertyRelative("yAxisMode").enumValueIndex = (int)CelestialRotationMode.Continuous;
//         //     element.FindPropertyRelative("yAxisSpeed").floatValue = isMoon ? 0.15f : 0.25f;
//         //     element.FindPropertyRelative("yAxisOverrideSpeed").boolValue = false;
//         //     element.FindPropertyRelative("yAxisMinRange").floatValue = 0f;
//         //     element.FindPropertyRelative("yAxisMaxRange").floatValue = 360f;
//         //     
//         //     if (isMoon)
//         //     {
//         //         element.FindPropertyRelative("orbitalPeriod").floatValue = 104f; // Default 1 month orbit
//         //     }
//         // }
//     }
// }

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

        // private void DrawStarsSection()
        // {
        //     var starsProperty = serializedObject.FindProperty("stars");
        //     
        //     EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        //     
        //     // Section header with add button
        //     EditorGUILayout.BeginHorizontal();
        //     EditorGUILayout.LabelField("Stars (Suns)", EditorStyles.boldLabel);
        //     GUILayout.FlexibleSpace();
        //     if (GUILayout.Button("Add Star", GUILayout.Width(80)))
        //     {
        //         starsProperty.arraySize++;
        //         var newElement = starsProperty.GetArrayElementAtIndex(starsProperty.arraySize - 1);
        //         SetDefaultCelestialBodyValues(newElement, $"Star {starsProperty.arraySize}", false);
        //         starFoldouts[starsProperty.arraySize - 1] = true;
        //     }
        //     EditorGUILayout.EndHorizontal();
        //     
        //     if (starsProperty.arraySize == 0)
        //     {
        //         EditorGUILayout.HelpBox("No stars configured. Add at least one star (sun) for proper lighting.", MessageType.Warning);
        //     }
        //     else
        //     {
        //         EditorGUILayout.Space(5);
        //         for (int i = 0; i < starsProperty.arraySize; i++)
        //         {
        //             DrawCelestialBodyElement(starsProperty, i, "Star", starFoldouts, false);
        //         }
        //     }
        //     
        //     EditorGUILayout.EndVertical();
        // }
        //
        // private void DrawMoonsSection()
        // {
        //     var moonsProperty = serializedObject.FindProperty("moons");
        //     
        //     EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        //     
        //     // Section header with add button
        //     EditorGUILayout.BeginHorizontal();
        //     EditorGUILayout.LabelField("Moons", EditorStyles.boldLabel);
        //     GUILayout.FlexibleSpace();
        //     if (GUILayout.Button("Add Moon", GUILayout.Width(80)))
        //     {
        //         moonsProperty.arraySize++;
        //         var newElement = moonsProperty.GetArrayElementAtIndex(moonsProperty.arraySize - 1);
        //         SetDefaultCelestialBodyValues(newElement, $"Moon {moonsProperty.arraySize}", true);
        //         moonFoldouts[moonsProperty.arraySize - 1] = true;
        //     }
        //     EditorGUILayout.EndHorizontal();
        //     
        //     if (moonsProperty.arraySize == 0)
        //     {
        //         EditorGUILayout.HelpBox("No moons configured. Moons provide nighttime lighting and atmospheric effects.", MessageType.Info);
        //     }
        //     else
        //     {
        //         EditorGUILayout.Space(5);
        //         for (int i = 0; i < moonsProperty.arraySize; i++)
        //         {
        //             DrawCelestialBodyElement(moonsProperty, i, "Moon", moonFoldouts, true);
        //         }
        //     }
        //     
        //     EditorGUILayout.EndVertical();
        // }
        
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
            
            EditorGUILayout.PropertyField(orbitalAngleProperty, new GUIContent("Orbital Angle", "Angle of orbital path variation (0° = flat circle, 45° = angled orbit). Higher values create more dramatic seasonal arcs."));
            EditorGUILayout.PropertyField(baseElevationProperty, new GUIContent("Base Elevation", "Center elevation of orbital path (90° = overhead, 180° = horizon, 270° = below horizon)"));
            
            // Calculate and show orbital path preview
            float minElevation = baseElevationProperty.floatValue + orbitalAngleProperty.floatValue;
            float maxElevation = baseElevationProperty.floatValue - orbitalAngleProperty.floatValue;
            
            EditorGUILayout.Space(3);
            EditorGUILayout.HelpBox($"Orbital Path Preview:\n• Highest point (noon): {maxElevation:F1}°\n• Lowest point (midnight): {minElevation:F1}°\n• Total variation: {orbitalAngleProperty.floatValue * 2:F1}°", MessageType.None);
            
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

// using UnityEngine;
// using UnityEditor;
// using System.Collections.Generic;
//
// namespace Sol.Editor
// {
//     [CustomEditor(typeof(SeasonalData))]
//     public class SeasonalDataEditor : UnityEditor.Editor
//     {
//         private Dictionary<int, bool> starFoldouts = new Dictionary<int, bool>();
//         private Dictionary<int, bool> moonFoldouts = new Dictionary<int, bool>();
//
//         public override void OnInspectorGUI()
//         {
//             serializedObject.Update();
//
//             DrawHeader();
//             EditorGUILayout.Space();
//
//             DrawStarsSection();
//             EditorGUILayout.Space();
//
//             DrawMoonsSection();
//
//             serializedObject.ApplyModifiedProperties();
//         }
//
//         private void DrawHeader()
//         {
//             EditorGUILayout.LabelField("Seasonal Data Configuration", EditorStyles.largeLabel);
//             EditorGUILayout.HelpBox("Configure celestial bodies for this season (Langsomr, Svik, Evinotr, or Gro). Each body orbits continuously with angled orbital paths.", MessageType.Info);
//         }
//
//         private void DrawStarsSection()
//         {
//             var starsProperty = serializedObject.FindProperty("stars");
//             
//             EditorGUILayout.LabelField("Stars", EditorStyles.boldLabel);
//             
//             if (starsProperty.arraySize == 0)
//             {
//                 EditorGUILayout.HelpBox("No stars configured. Add at least one star (sun) for proper lighting.", MessageType.Warning);
//             }
//
//             for (int i = 0; i < starsProperty.arraySize; i++)
//             {
//                 DrawCelestialBodyElement(starsProperty, i, "Star", starFoldouts, false);
//             }
//
//             EditorGUILayout.BeginHorizontal();
//             if (GUILayout.Button("Add Star"))
//             {
//                 starsProperty.arraySize++;
//                 var newElement = starsProperty.GetArrayElementAtIndex(starsProperty.arraySize - 1);
//                 SetDefaultCelestialBodyValues(newElement, $"Star {starsProperty.arraySize}", false);
//                 starFoldouts[starsProperty.arraySize - 1] = true;
//             }
//             EditorGUILayout.EndHorizontal();
//         }
//
//         private void DrawMoonsSection()
//         {
//             var moonsProperty = serializedObject.FindProperty("moons");
//             
//             EditorGUILayout.LabelField("Moons", EditorStyles.boldLabel);
//             
//             if (moonsProperty.arraySize == 0)
//             {
//                 EditorGUILayout.HelpBox("No moons configured. Moons provide nighttime lighting and atmospheric effects.", MessageType.Info);
//             }
//
//             for (int i = 0; i < moonsProperty.arraySize; i++)
//             {
//                 DrawCelestialBodyElement(moonsProperty, i, "Moon", moonFoldouts, true);
//             }
//
//             EditorGUILayout.BeginHorizontal();
//             if (GUILayout.Button("Add Moon"))
//             {
//                 moonsProperty.arraySize++;
//                 var newElement = moonsProperty.GetArrayElementAtIndex(moonsProperty.arraySize - 1);
//                 SetDefaultCelestialBodyValues(newElement, $"Moon {moonsProperty.arraySize}", true);
//                 moonFoldouts[moonsProperty.arraySize - 1] = true;
//             }
//             EditorGUILayout.EndHorizontal();
//         }
//
//         private void DrawCelestialBodyElement(SerializedProperty arrayProperty, int index, string typeName, Dictionary<int, bool> foldouts, bool isMoon)
//         {
//             var element = arrayProperty.GetArrayElementAtIndex(index);
//             var nameProperty = element.FindPropertyRelative("name");
//             var activeProperty = element.FindPropertyRelative("active");
//
//             // Initialize foldout state if needed
//             if (!foldouts.ContainsKey(index))
//                 foldouts[index] = false;
//
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//
//             // Header with name, active toggle, and delete button
//             EditorGUILayout.BeginHorizontal();
//             
//             string displayName = string.IsNullOrEmpty(nameProperty.stringValue) ? $"Unnamed {typeName}" : nameProperty.stringValue;
//             string headerText = $"{displayName} {(activeProperty.boolValue ? "" : "(Inactive)")}";
//             
//             foldouts[index] = EditorGUILayout.Foldout(foldouts[index], headerText, true);
//             
//             EditorGUILayout.PropertyField(activeProperty, GUIContent.none, GUILayout.Width(15));
//             
//             if (GUILayout.Button("×", GUILayout.Width(20)))
//             {
//                 arrayProperty.DeleteArrayElementAtIndex(index);
//                 EditorGUILayout.EndHorizontal();
//                 EditorGUILayout.EndVertical();
//                 return;
//             }
//             
//             EditorGUILayout.EndHorizontal();
//
//             if (foldouts[index])
//             {
//                 EditorGUI.indentLevel++;
//                 
//                 // Basic configuration
//                 EditorGUILayout.PropertyField(nameProperty, new GUIContent("Name"));
//                 
//                 if (activeProperty.boolValue)
//                 {
//                     DrawOrbitalControls(element);
//                     EditorGUILayout.Space();
//                     DrawOrbitalPathControls(element, isMoon);
//                     
//                     if (isMoon)
//                     {
//                         EditorGUILayout.Space();
//                         DrawMoonControls(element);
//                     }
//                 }
//                 else
//                 {
//                     EditorGUILayout.HelpBox($"This {typeName.ToLower()} is inactive and will not be visible during this season.", MessageType.Info);
//                 }
//                 
//                 EditorGUI.indentLevel--;
//             }
//
//             EditorGUILayout.EndVertical();
//             EditorGUILayout.Space();
//         }
//
//         private void DrawOrbitalControls(SerializedProperty element)
//         {
//             EditorGUILayout.LabelField("Orbital Motion (Azimuth)", EditorStyles.miniBoldLabel);
//             EditorGUI.indentLevel++;
//             
//             var yAxisEnabledProperty = element.FindPropertyRelative("yAxisEnabled");
//             EditorGUILayout.PropertyField(yAxisEnabledProperty, new GUIContent("Enable Orbital Motion"));
//             
//             if (yAxisEnabledProperty.boolValue)
//             {
//                 var yAxisOverrideProperty = element.FindPropertyRelative("yAxisOverrideSpeed");
//                 EditorGUILayout.PropertyField(yAxisOverrideProperty, new GUIContent("Sync with Day Length", "Automatically sync orbital speed with TimeManager day length"));
//                 
//                 if (!yAxisOverrideProperty.boolValue)
//                 {
//                     var yAxisSpeedProperty = element.FindPropertyRelative("yAxisSpeed");
//                     EditorGUILayout.PropertyField(yAxisSpeedProperty, new GUIContent("Orbital Speed", "Speed multiplier (1.0 = one orbit per day)"));
//                     
//                     if (yAxisSpeedProperty.floatValue <= 0)
//                     {
//                         EditorGUILayout.HelpBox("Orbital speed must be greater than 0", MessageType.Error);
//                     }
//                 }
//                 else
//                 {
//                     EditorGUILayout.HelpBox("Orbital speed is automatically synced with day length (1 orbit per day)", MessageType.Info);
//                 }
//             }
//             else
//             {
//                 EditorGUILayout.HelpBox("This celestial body will remain stationary in the sky", MessageType.Info);
//             }
//             
//             EditorGUI.indentLevel--;
//         }
//
//         private void DrawOrbitalPathControls(SerializedProperty element, bool isMoon)
//         {
//             EditorGUILayout.LabelField("Orbital Path Configuration", EditorStyles.miniBoldLabel);
//             EditorGUI.indentLevel++;
//             
//             var orbitalAngleProperty = element.FindPropertyRelative("orbitalAngle");
//             var baseElevationProperty = element.FindPropertyRelative("baseElevation");
//             
//             EditorGUILayout.PropertyField(orbitalAngleProperty, new GUIContent("Orbital Angle", "Angle of orbital path relative to horizon (0° = flat circle, 45° = angled orbit)"));
//             EditorGUILayout.PropertyField(baseElevationProperty, new GUIContent("Base Elevation", "Starting elevation when azimuth = 0° (180° = horizon)"));
//             
//             // Visual helper for orbital path
//             string pathGuide = "Orbital Path Guide:\n";
//             pathGuide += "• Orbital Angle 0° = Flat circle at base elevation\n";
//             pathGuide += "• Orbital Angle 30° = Moderate arc across sky\n";
//             pathGuide += "• Orbital Angle 60° = Steep arc across sky\n\n";
//             pathGuide += "Base Elevation Guide:\n";
//             pathGuide += "• 90° = Directly overhead\n";
//             pathGuide += "• 180° = Horizon\n";
//             pathGuide += "• 270° = Directly below";
//             
//             if (isMoon)
//             {
//                 pathGuide += "\n\nMoon Recommendations:\n";
//                 pathGuide += "• Orbital Angle: 15°-45°\n";
//                 pathGuide += "• Base Elevation: 160°-200°";
//             }
//             else
//             {
//                 pathGuide += "\n\nSun Recommendations:\n";
//                 pathGuide += "• Langsomr (Summer): Angle 45°, Base 150°\n";
//                 pathGuide += "• Svik (Autumn): Angle 30°, Base 180°\n";
//                 pathGuide += "• Evinotr (Winter): Angle 15°, Base 200°\n";
//                 pathGuide += "• Gro (Spring): Angle 30°, Base 180°";
//             }
//             
//             EditorGUILayout.HelpBox(pathGuide, MessageType.Info);
//             
//             // Warn about gimbal lock
//             if (IsNearGimbalLock(baseElevationProperty.floatValue))
//             {
//                 EditorGUILayout.HelpBox("Warning: Base elevation near 90° may cause rotation issues. Consider using 85° or 95° instead.", MessageType.Warning);
//             }
//             
//             // Calculate and show orbital path preview
//             float minElevation = baseElevationProperty.floatValue - orbitalAngleProperty.floatValue;
//             float maxElevation = baseElevationProperty.floatValue + orbitalAngleProperty.floatValue;
//             
//             EditorGUILayout.HelpBox($"Orbital Path Preview:\n• Lowest point: {minElevation:F1}°\n• Highest point: {maxElevation:F1}°\n• Total arc: {orbitalAngleProperty.floatValue * 2:F1}°", MessageType.None);
//             
//             EditorGUI.indentLevel--;
//         }
//
//         private void DrawMoonControls(SerializedProperty element)
//         {
//             EditorGUILayout.LabelField("Moon-Specific Settings", EditorStyles.miniBoldLabel);
//             EditorGUI.indentLevel++;
//             
//             var orbitalPeriodProperty = element.FindPropertyRelative("orbitalPeriod");
//             var phaseOffsetProperty = element.FindPropertyRelative("phaseOffset");
//             
//             EditorGUILayout.PropertyField(orbitalPeriodProperty, new GUIContent("Orbital Period (Days)", "How many days for one complete orbit around the planet"));
//             EditorGUILayout.PropertyField(phaseOffsetProperty, new GUIContent("Phase Offset", "Degrees offset from sun position (180° = opposite side of sky)"));
//             
//             if (orbitalPeriodProperty.floatValue <= 0)
//             {
//                 EditorGUILayout.HelpBox("Orbital period must be greater than 0", MessageType.Error);
//             }
//             else if (orbitalPeriodProperty.floatValue < 1f)
//             {
//                 EditorGUILayout.HelpBox("Very short orbital periods may cause rapid movement", MessageType.Warning);
//             }
//             
//             // Phase offset helpers
//             string phaseInfo = "Phase Offset Guide:\n";
//             if (Mathf.Approximately(phaseOffsetProperty.floatValue, 0f))
//             {
//                 phaseInfo += "• 0° = Same position as sun (new moon)\n";
//             }
//             else if (Mathf.Approximately(phaseOffsetProperty.floatValue, 90f))
//             {
//                 phaseInfo += "• 90° = Quarter ahead of sun (first quarter)\n";
//             }
//             else if (Mathf.Approximately(phaseOffsetProperty.floatValue, 180f))
//             {
//                 phaseInfo += "• 180° = Opposite to sun (full moon)\n";
//             }
//             else if (Mathf.Approximately(phaseOffsetProperty.floatValue, 270f))
//             {
//                 phaseInfo += "• 270° = Quarter behind sun (last quarter)\n";
//             }
//             
//             phaseInfo += "\nRecommended for night visibility: 180° (opposite sun)";
//             EditorGUILayout.HelpBox(phaseInfo, MessageType.Info);
//             
//             EditorGUI.indentLevel--;
//         }
//
//         private void SetDefaultCelestialBodyValues(SerializedProperty element, string defaultName, bool isMoon)
//         {
//             element.FindPropertyRelative("name").stringValue = defaultName;
//             element.FindPropertyRelative("active").boolValue = true;
//             
//             // Orbital settings
//             element.FindPropertyRelative("yAxisEnabled").boolValue = true;
//             element.FindPropertyRelative("yAxisSpeed").floatValue = 1f;
//             element.FindPropertyRelative("yAxisOverrideSpeed").boolValue = true; // Default to day sync
//             
//             // Orbital path settings
//             if (isMoon)
//             {
//                 // Moon defaults - moderate orbital path
//                 element.FindPropertyRelative("orbitalAngle").floatValue = 25f;
//                 element.FindPropertyRelative("baseElevation").floatValue = 180f; // Horizon level
//                 element.FindPropertyRelative("orbitalPeriod").floatValue = 29.5f;
//                 element.FindPropertyRelative("phaseOffset").floatValue = 180f; // Opposite to sun
//             }
//             else
//             {
//                 // Sun defaults - varies by season, but we'll use moderate values
//                 element.FindPropertyRelative("orbitalAngle").floatValue = 30f;
//                 element.FindPropertyRelative("baseElevation").floatValue = 180f; // Horizon level
//                 element.FindPropertyRelative("orbitalPeriod").floatValue = 365f; // Not used for suns
//                 element.FindPropertyRelative("phaseOffset").floatValue = 0f; // No offset
//             }
//         }
//
//         private bool IsNearGimbalLock(float elevation)
//         {
//             return Mathf.Abs(elevation - 90f) < 10f; // Within 10° of 90°
//         }
//     }
// }