using UnityEngine;
using UnityEditor;

namespace Sol
{
    [CustomEditor(typeof(SeasonalData))]
    public class SeasonalDataEditor : Editor
    {
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

            // Draw celestial sections
            DrawPrimaryStarSection();
            DrawRedDwarfSection();

            EditorGUILayout.Space();

            // Draw weather section
            DrawWeatherSection();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPrimaryStarSection()
        {
            EditorGUILayout.LabelField("Primary Star Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryStarActive"));
            
            if (serializedObject.FindProperty("primaryStarActive").boolValue)
            {
                EditorGUI.indentLevel++;
                DrawPrimaryXAxisControls();
                DrawPrimaryYAxisControls();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawPrimaryXAxisControls()
        {
            EditorGUILayout.LabelField("X-Axis (Elevation)", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryXAxisEnabled"), new GUIContent("Enabled"));
            
            if (serializedObject.FindProperty("primaryXAxisEnabled").boolValue)
            {
                EditorGUI.indentLevel++;
                
                var modeProp = serializedObject.FindProperty("primaryXAxisMode");
                var syncProp = serializedObject.FindProperty("primarySyncXWithY");
                
                EditorGUILayout.PropertyField(modeProp, new GUIContent("Rotation Mode"));
                EditorGUILayout.PropertyField(syncProp, new GUIContent("Sync with Y-Axis"));
                
                // Only show speed if not synced (for oscillate mode)
                if (modeProp.enumValueIndex == (int)CelestialRotationMode.Oscillate)
                {
                    if (!syncProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryXAxisSpeed"), new GUIContent("Speed"));
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Speed is automatically calculated from Y-axis rotation", MessageType.Info);
                    }
                    
                    // Always show range controls for oscillate mode
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryXAxisMinRange"), new GUIContent("Min Range"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryXAxisMaxRange"), new GUIContent("Max Range"));
                }
                else // Continuous mode
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryXAxisSpeed"), new GUIContent("Speed"));
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawPrimaryYAxisControls()
        {
            EditorGUILayout.LabelField("Y-Axis (Azimuth)", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryYAxisEnabled"), new GUIContent("Enabled"));
            
            if (serializedObject.FindProperty("primaryYAxisEnabled").boolValue)
            {
                EditorGUI.indentLevel++;
                
                var modeProp = serializedObject.FindProperty("primaryYAxisMode");
                EditorGUILayout.PropertyField(modeProp, new GUIContent("Rotation Mode"));
                
                if (modeProp.enumValueIndex == (int)CelestialRotationMode.Continuous)
                {
                    // For continuous mode, show day sync option
                    EditorGUILayout.HelpBox("Continuous mode syncs with TimeManager day length by default", MessageType.Info);
                    
                    // Add override option (you'll need to add this field to SeasonalData)
                    var overrideSpeedProp = serializedObject.FindProperty("primaryYAxisOverrideSpeed");
                    if (overrideSpeedProp != null)
                    {
                        EditorGUILayout.PropertyField(overrideSpeedProp, new GUIContent("Override Day Sync"));
                        
                        if (overrideSpeedProp.boolValue)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryYAxisSpeed"), new GUIContent("Custom Speed"));
                        }
                    }
                    else
                    {
                        // Fallback if override field doesn't exist yet
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryYAxisSpeed"), new GUIContent("Speed (overrides day sync)"));
                    }
                }
                else // Oscillate mode
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryYAxisSpeed"), new GUIContent("Speed"));
                    
                    // Show range controls for oscillate mode on Y-axis too
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryYAxisMinRange"), new GUIContent("Min Range"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("primaryYAxisMaxRange"), new GUIContent("Max Range"));
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawRedDwarfSection()
        {
            EditorGUILayout.LabelField("Red Dwarf Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("redDwarfActive"));
            
            if (serializedObject.FindProperty("redDwarfActive").boolValue)
            {
                EditorGUI.indentLevel++;
                DrawRedDwarfXAxisControls();
                DrawRedDwarfYAxisControls();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawRedDwarfXAxisControls()
        {
            EditorGUILayout.LabelField("X-Axis (Elevation)", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("redDwarfXAxisEnabled"), new GUIContent("Enabled"));
            
            if (serializedObject.FindProperty("redDwarfXAxisEnabled").boolValue)
            {
                EditorGUI.indentLevel++;
                
                var modeProp = serializedObject.FindProperty("redDwarfXAxisMode");
                var syncProp = serializedObject.FindProperty("redDwarfSyncXWithY");
                
                EditorGUILayout.PropertyField(modeProp, new GUIContent("Rotation Mode"));
                EditorGUILayout.PropertyField(syncProp, new GUIContent("Sync with Y-Axis"));
                
                // Only show speed if not synced (for oscillate mode)
                if (modeProp.enumValueIndex == (int)CelestialRotationMode.Oscillate)
                {
                    if (!syncProp.boolValue)
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("redDwarfXAxisSpeed"), new GUIContent("Speed"));
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Speed is automatically calculated from Y-axis rotation", MessageType.Info);
                    }
                    
                    // Always show range controls for oscillate mode
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("redDwarfXAxisMinRange"), new GUIContent("Min Range"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("redDwarfXAxisMaxRange"), new GUIContent("Max Range"));
                }
                else // Continuous mode
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("redDwarfXAxisSpeed"), new GUIContent("Speed"));
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        private void DrawRedDwarfYAxisControls()
        {
            EditorGUILayout.LabelField("Y-Axis (Azimuth)", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(serializedObject.FindProperty("redDwarfYAxisEnabled"), new GUIContent("Enabled"));
            
            if (serializedObject.FindProperty("redDwarfYAxisEnabled").boolValue)
            {
                EditorGUI.indentLevel++;
                
                var modeProp = serializedObject.FindProperty("redDwarfYAxisMode");
                EditorGUILayout.PropertyField(modeProp, new GUIContent("Rotation Mode"));
                
                if (modeProp.enumValueIndex == (int)CelestialRotationMode.Continuous)
                {
                    // For continuous mode, show day sync option
                    EditorGUILayout.HelpBox("Continuous mode syncs with TimeManager day length by default", MessageType.Info);
                    
                    // Add override option (you'll need to add this field to SeasonalData)
                    var overrideSpeedProp = serializedObject.FindProperty("redDwarfYAxisOverrideSpeed");
                    if (overrideSpeedProp != null)
                    {
                        EditorGUILayout.PropertyField(overrideSpeedProp, new GUIContent("Override Day Sync"));
                        
                        if (overrideSpeedProp.boolValue)
                        {
                            EditorGUILayout.PropertyField(serializedObject.FindProperty("redDwarfYAxisSpeed"), new GUIContent("Custom Speed"));
                        }
                    }
                    else
                    {
                        // Fallback if override field doesn't exist yet
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("redDwarfYAxisSpeed"), new GUIContent("Speed (overrides day sync)"));
                    }
                }
                else // Oscillate mode
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("redDwarfYAxisSpeed"), new GUIContent("Speed"));
                    
                    // Show range controls for oscillate mode on Y-axis too
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("redDwarfYAxisMinRange"), new GUIContent("Min Range"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("redDwarfYAxisMaxRange"), new GUIContent("Max Range"));
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
    }
}