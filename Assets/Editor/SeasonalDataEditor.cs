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
//             EditorGUILayout.Space(10);
//
//             DrawStarsSection();
//             EditorGUILayout.Space(10);
//
//             DrawMoonsSection();
//
//             serializedObject.ApplyModifiedProperties();
//         }
//
//         private void DrawHeader()
//         {
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             EditorGUILayout.LabelField("Seasonal Data Configuration", EditorStyles.largeLabel);
//             EditorGUILayout.Space(5);
//             EditorGUILayout.HelpBox("Configure celestial bodies for this season (Langsomr, Svik, Evinotr, or Gro). Each body orbits continuously with angled orbital paths.", MessageType.Info);
//             EditorGUILayout.EndVertical();
//         }
//         
//         private void DrawStarsSection()
//         {
//             var starsProperty = serializedObject.FindProperty("stars");
//             
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             
//             // Section header with add button
//             EditorGUILayout.BeginHorizontal();
//             EditorGUILayout.LabelField("Stars (Suns)", EditorStyles.boldLabel);
//             GUILayout.FlexibleSpace();
//             if (GUILayout.Button("Add Star", GUILayout.Width(80)))
//             {
//                 starsProperty.arraySize++;
//                 var newElement = starsProperty.GetArrayElementAtIndex(starsProperty.arraySize - 1);
//                 SetDefaultCelestialBodyValues(newElement, $"Star {starsProperty.arraySize}", false);
//                 starFoldouts[starsProperty.arraySize - 1] = true;
//             }
//             EditorGUILayout.EndHorizontal();
//             
//             if (starsProperty.arraySize == 0)
//             {
//                 EditorGUILayout.HelpBox("No stars configured. Add at least one star (sun) for proper lighting.", MessageType.Warning);
//             }
//             else
//             {
//                 EditorGUILayout.Space(5);
//                 
//                 // Add padding to prevent foldout arrow cutoff
//                 EditorGUILayout.BeginVertical();
//                 GUILayout.Space(2); // Small top padding
//                 
//                 for (int i = 0; i < starsProperty.arraySize; i++)
//                 {
//                     // Add left margin to ensure foldout arrow is visible
//                     EditorGUILayout.BeginHorizontal();
//                     GUILayout.Space(4); // Left padding for foldout arrow
//                     
//                     EditorGUILayout.BeginVertical();
//                     DrawCelestialBodyElement(starsProperty, i, "Star", starFoldouts, false);
//                     EditorGUILayout.EndVertical();
//                     
//                     EditorGUILayout.EndHorizontal();
//                 }
//                 
//                 GUILayout.Space(2); // Small bottom padding
//                 EditorGUILayout.EndVertical();
//             }
//             
//             EditorGUILayout.EndVertical();
//         }
//
//         private void DrawMoonsSection()
//         {
//             var moonsProperty = serializedObject.FindProperty("moons");
//             
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             
//             // Section header with add button
//             EditorGUILayout.BeginHorizontal();
//             EditorGUILayout.LabelField("Moons", EditorStyles.boldLabel);
//             GUILayout.FlexibleSpace();
//             if (GUILayout.Button("Add Moon", GUILayout.Width(80)))
//             {
//                 moonsProperty.arraySize++;
//                 var newElement = moonsProperty.GetArrayElementAtIndex(moonsProperty.arraySize - 1);
//                 SetDefaultCelestialBodyValues(newElement, $"Moon {moonsProperty.arraySize}", true);
//                 moonFoldouts[moonsProperty.arraySize - 1] = true;
//             }
//             EditorGUILayout.EndHorizontal();
//             
//             if (moonsProperty.arraySize == 0)
//             {
//                 EditorGUILayout.HelpBox("No moons configured. Moons provide nighttime lighting and atmospheric effects.", MessageType.Info);
//             }
//             else
//             {
//                 EditorGUILayout.Space(5);
//                 
//                 // Add padding to prevent foldout arrow cutoff
//                 EditorGUILayout.BeginVertical();
//                 GUILayout.Space(2); // Small top padding
//                 
//                 for (int i = 0; i < moonsProperty.arraySize; i++)
//                 {
//                     // Add left margin to ensure foldout arrow is visible
//                     EditorGUILayout.BeginHorizontal();
//                     GUILayout.Space(4); // Left padding for foldout arrow
//                     
//                     EditorGUILayout.BeginVertical();
//                     DrawCelestialBodyElement(moonsProperty, i, "Moon", moonFoldouts, true);
//                     EditorGUILayout.EndVertical();
//                     
//                     EditorGUILayout.EndHorizontal();
//                 }
//                 
//                 GUILayout.Space(2); // Small bottom padding
//                 EditorGUILayout.EndVertical();
//             }
//             
//             EditorGUILayout.EndVertical();
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
//             // Header with manual arrow and spaced frame
//             Rect headerRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
//             
//             string displayName = string.IsNullOrEmpty(nameProperty.stringValue) ? $"Unnamed {typeName}" : nameProperty.stringValue;
//             string headerText = $"{displayName} {(activeProperty.boolValue ? "" : "(Inactive)")}";
//             
//             // Calculate layout
//             float arrowWidth = 12f;
//             float spacingAfterArrow = 8f; // The space you want between arrow and frame
//             float frameStartX = headerRect.x + arrowWidth + spacingAfterArrow;
//             float frameWidth = headerRect.width - arrowWidth - spacingAfterArrow;
//             
//             // Create rects
//             Rect arrowRect = new Rect(headerRect.x, headerRect.y, arrowWidth, headerRect.height);
//             Rect frameRect = new Rect(frameStartX, headerRect.y, frameWidth, headerRect.height);
//             
//             // Draw the foldout arrow manually
//             if (GUI.Button(arrowRect, foldouts[index] ? "▼" : "▶", EditorStyles.label))
//             {
//                 foldouts[index] = !foldouts[index];
//             }
//             
//             // Draw the frame background
//             GUI.Box(frameRect, "", EditorStyles.toolbar);
//             
//             // Calculate content areas within the frame
//             float deleteButtonWidth = 25f;
//             float activeToggleWidth = 15f;
//             float activeLabelWidth = 40f;
//             float padding = 4f;
//             
//             float controlsWidth = deleteButtonWidth + activeToggleWidth + activeLabelWidth + padding;
//             float labelWidth = frameWidth - controlsWidth - (padding * 2);
//             
//             Rect labelRect = new Rect(frameRect.x + padding, frameRect.y, labelWidth, frameRect.height);
//             Rect activeLabelRect = new Rect(labelRect.xMax, frameRect.y, activeLabelWidth, frameRect.height);
//             Rect activeToggleRect = new Rect(activeLabelRect.xMax, frameRect.y, activeToggleWidth, frameRect.height);
//             Rect deleteButtonRect = new Rect(activeToggleRect.xMax + padding, frameRect.y, deleteButtonWidth, frameRect.height);
//             
//             // Draw content within the frame
//             EditorGUI.LabelField(labelRect, headerText);
//             EditorGUI.LabelField(activeLabelRect, "Active");
//             EditorGUI.PropertyField(activeToggleRect, activeProperty, GUIContent.none);
//             
//             // Delete button
//             GUI.backgroundColor = Color.red;
//             if (GUI.Button(deleteButtonRect, "×"))
//             {
//                 arrayProperty.DeleteArrayElementAtIndex(index);
//                 GUI.backgroundColor = Color.white;
//                 EditorGUILayout.EndVertical();
//                 return;
//             }
//             GUI.backgroundColor = Color.white;
//
//             // Content area (only show if expanded)
//             if (foldouts[index])
//             {
//                 EditorGUILayout.Space(5);
//                 EditorGUI.indentLevel++;
//                 
//                 // Basic configuration
//                 EditorGUILayout.PropertyField(nameProperty, new GUIContent("Name", "Display name for this celestial body"));
//                 
//                 if (activeProperty.boolValue)
//                 {
//                     EditorGUILayout.Space(5);
//                     DrawOrbitalControls(element);
//                     EditorGUILayout.Space(5);
//                     DrawOrbitalPathControls(element, isMoon);
//                     
//                     if (isMoon)
//                     {
//                         EditorGUILayout.Space(5);
//                         DrawMoonControls(element);
//                     }
//                 }
//                 else
//                 {
//                     EditorGUILayout.Space(5);
//                     EditorGUILayout.HelpBox($"This {typeName.ToLower()} is inactive and will not be visible during this season.", MessageType.Info);
//                 }
//                 
//                 EditorGUI.indentLevel--;
//             }
//
//             EditorGUILayout.EndVertical();
//             EditorGUILayout.Space(3);
//         }
//
//         private void DrawOrbitalControls(SerializedProperty element)
//         {
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             EditorGUILayout.LabelField("Orbital Motion (Azimuth)", EditorStyles.miniBoldLabel);
//             EditorGUILayout.Space(3);
//             
//             var yAxisEnabledProperty = element.FindPropertyRelative("yAxisEnabled");
//             EditorGUILayout.PropertyField(yAxisEnabledProperty, new GUIContent("Enable Orbital Motion", "Whether this celestial body moves across the sky"));
//             
//             if (yAxisEnabledProperty.boolValue)
//             {
//                 EditorGUILayout.Space(3);
//                 var yAxisOverrideProperty = element.FindPropertyRelative("yAxisOverrideSpeed");
//                 EditorGUILayout.PropertyField(yAxisOverrideProperty, new GUIContent("Sync with Day Length", "Automatically sync orbital speed with TimeManager day length (1 orbit per day)"));
//                 
//                 if (!yAxisOverrideProperty.boolValue)
//                 {
//                     var yAxisSpeedProperty = element.FindPropertyRelative("yAxisSpeed");
//                     EditorGUILayout.PropertyField(yAxisSpeedProperty, new GUIContent("Orbital Speed", "Speed multiplier (1.0 = one orbit per day, 2.0 = two orbits per day)"));
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
//             EditorGUILayout.EndVertical();
//         }
//
//         private void DrawOrbitalPathControls(SerializedProperty element, bool isMoon)
//         {
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             EditorGUILayout.LabelField("Orbital Path Configuration", EditorStyles.miniBoldLabel);
//             EditorGUILayout.Space(3);
//             
//             var orbitalAngleProperty = element.FindPropertyRelative("orbitalAngle");
//             var baseElevationProperty = element.FindPropertyRelative("baseElevation");
//             
//             EditorGUILayout.PropertyField(orbitalAngleProperty, new GUIContent("Orbital Angle", "Angle of orbital path variation (0° = flat circle, 45° = angled orbit). Higher values create more dramatic seasonal arcs."));
//             EditorGUILayout.PropertyField(baseElevationProperty, new GUIContent("Base Elevation", "Center elevation of orbital path (90° = overhead, 180° = horizon, 270° = below horizon)"));
//             
//             // Calculate and show orbital path preview
//             float minElevation = baseElevationProperty.floatValue + orbitalAngleProperty.floatValue;
//             float maxElevation = baseElevationProperty.floatValue - orbitalAngleProperty.floatValue;
//             
//             EditorGUILayout.Space(3);
//             EditorGUILayout.HelpBox($"Orbital Path Preview:\n• Highest point (noon): {maxElevation:F1}°\n• Lowest point (midnight): {minElevation:F1}°\n• Total variation: {orbitalAngleProperty.floatValue * 2:F1}°", MessageType.None);
//             
//             // Seasonal recommendations
//             if (!isMoon)
//             {
//                 EditorGUILayout.Space(3);
//                 string seasonGuide = "Season Recommendations:\n";
//                 seasonGuide += "• Langsomr (Summer): Angle 45°, Base 135° (high arc)\n";
//                 seasonGuide += "• Svik (Autumn): Angle 30°, Base 180° (moderate arc)\n";
//                 seasonGuide += "• Evinotr (Winter): Angle 15°, Base 225° (low arc)\n";
//                 seasonGuide += "• Gro (Spring): Angle 30°, Base 180° (moderate arc)";
//                 
//                 EditorGUILayout.HelpBox(seasonGuide, MessageType.Info);
//             }
//             
//             // Warn about extreme values
//             if (IsNearGimbalLock(baseElevationProperty.floatValue))
//             {
//                 EditorGUILayout.HelpBox("Warning: Base elevation near 90° may cause rotation issues. Consider using 85° or 95° instead.", MessageType.Warning);
//             }
//             
//             EditorGUILayout.EndVertical();
//         }
//
//         private void DrawMoonControls(SerializedProperty element)
//         {
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             EditorGUILayout.LabelField("Moon-Specific Settings", EditorStyles.miniBoldLabel);
//             EditorGUILayout.Space(3);
//             
//             var orbitalPeriodProperty = element.FindPropertyRelative("orbitalPeriod");
//             var phaseOffsetProperty = element.FindPropertyRelative("phaseOffset");
//             
//             EditorGUILayout.PropertyField(orbitalPeriodProperty, new GUIContent("Orbital Period (Days)", "How many days for one complete orbit around the planet. Earth's moon: ~29.5 days"));
//             EditorGUILayout.PropertyField(phaseOffsetProperty, new GUIContent("Phase Offset (Degrees)", "Angular offset from sun position (0° = same as sun, 180° = opposite sun for night visibility)"));
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
//             EditorGUILayout.Space(3);
//             string phaseInfo = GetPhaseOffsetDescription(phaseOffsetProperty.floatValue);
//             EditorGUILayout.HelpBox(phaseInfo, MessageType.Info);
//             
//             EditorGUILayout.EndVertical();
//         }
//
//         private string GetPhaseOffsetDescription(float phaseOffset)
//         {
//             float normalizedOffset = Mathf.Repeat(phaseOffset, 360f);
//             
//             if (Mathf.Approximately(normalizedOffset, 0f))
//                 return "Phase: New Moon (same position as sun, not visible at night)";
//             else if (Mathf.Approximately(normalizedOffset, 90f))
//                 return "Phase: First Quarter (90° ahead of sun, visible evening/night)";
//             else if (Mathf.Approximately(normalizedOffset, 180f))
//                 return "Phase: Full Moon (opposite to sun, visible all night) ⭐ Recommended";
//             else if (Mathf.Approximately(normalizedOffset, 270f))
//                 return "Phase: Last Quarter (90° behind sun, visible night/morning)";
//             else
//                 return $"Phase: Custom offset ({normalizedOffset:F0}°)\nRecommended for night visibility: 180° (opposite sun)";
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
//                 // Moon defaults - moderate orbital path for night visibility
//                 element.FindPropertyRelative("orbitalAngle").floatValue = 25f;
//                 element.FindPropertyRelative("baseElevation").floatValue = 160f; // Slightly above horizon
//                 element.FindPropertyRelative("orbitalPeriod").floatValue = 29.5f;
//                 element.FindPropertyRelative("phaseOffset").floatValue = 180f; // Opposite to sun
//             }
//             else
//             {
//                 // Sun defaults - moderate seasonal variation
//                 element.FindPropertyRelative("orbitalAngle").floatValue = 30f;
//                 element.FindPropertyRelative("baseElevation").floatValue = 180f; // Horizon level
//                 element.FindPropertyRelative("orbitalPeriod").floatValue = 0f; // Not used for suns
//                 element.FindPropertyRelative("phaseOffset").floatValue = 0f; // No offset for primary sun
//             }
//         }
//
//         private bool IsNearGimbalLock(float elevation)
//         {
//             return elevation >= 85f && elevation <= 95f;
//         }
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