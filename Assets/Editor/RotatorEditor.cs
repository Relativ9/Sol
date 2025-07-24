using UnityEngine;
using UnityEditor;

namespace Sol
{
        [CustomEditor(typeof(Rotator))]
        public class RotatorEditor : Editor
        {
            private SerializedProperty _rotationSpeed;
            private SerializedProperty _faceCamera;
            private SerializedProperty _xAxisRot;
            private SerializedProperty _yAxisRot;
            private SerializedProperty _zAxisRot;

            private SerializedProperty wheelRadius;
            private SerializedProperty rotationActive;
            private SerializedProperty getSpeedExternally;

            private SerializedProperty _celestialBodyMode;
            private SerializedProperty seasonalData;

            // Performance settings
            private SerializedProperty updateFrequency;

            // Celestial properties
            private SerializedProperty _xAxisCelestial;
            private SerializedProperty _xRotationMode;
            private SerializedProperty _xSyncWithAxis;
            private SerializedProperty _xSyncAxisIndex;
            private SerializedProperty _xOscillationSpeed;
            private SerializedProperty _xMinRange;
            private SerializedProperty _xMaxRange;
            private SerializedProperty _xRotationSpeed;

            private SerializedProperty _yAxisCelestial;
            private SerializedProperty _yRotationMode;
            private SerializedProperty _ySyncWithAxis;
            private SerializedProperty _ySyncAxisIndex;
            private SerializedProperty _yOscillationSpeed;
            private SerializedProperty _yMinRange;
            private SerializedProperty _yMaxRange;
            private SerializedProperty _yRotationSpeed;

            private SerializedProperty _zAxisCelestial;
            private SerializedProperty _zRotationMode;
            private SerializedProperty _zSyncWithAxis;
            private SerializedProperty _zSyncAxisIndex;
            private SerializedProperty _zOscillationSpeed;
            private SerializedProperty _zMinRange;
            private SerializedProperty _zMaxRange;
            private SerializedProperty _zRotationSpeed;

            private readonly string[] axisNames = { "X-Axis", "Y-Axis", "Z-Axis" };

            void OnEnable()
            {
                // Find all serialized properties
                _rotationSpeed = serializedObject.FindProperty("_rotationSpeed");
                _faceCamera = serializedObject.FindProperty("_faceCamera");
                _xAxisRot = serializedObject.FindProperty("_xAxisRot");
                _yAxisRot = serializedObject.FindProperty("_yAxisRot");
                _zAxisRot = serializedObject.FindProperty("_zAxisRot");

                wheelRadius = serializedObject.FindProperty("wheelRadius");
                rotationActive = serializedObject.FindProperty("rotationActive");
                getSpeedExternally = serializedObject.FindProperty("getSpeedExternally");

                _celestialBodyMode = serializedObject.FindProperty("_celestialBodyMode");
                seasonalData = serializedObject.FindProperty("seasonalData");

                // Performance settings
                updateFrequency = serializedObject.FindProperty("updateFrequency");

                _xAxisCelestial = serializedObject.FindProperty("_xAxisCelestial");
                _xRotationMode = serializedObject.FindProperty("_xRotationMode");
                _xSyncWithAxis = serializedObject.FindProperty("_xSyncWithAxis");
                _xSyncAxisIndex = serializedObject.FindProperty("_xSyncAxisIndex");
                _xOscillationSpeed = serializedObject.FindProperty("_xOscillationSpeed");
                _xMinRange = serializedObject.FindProperty("_xMinRange");
                _xMaxRange = serializedObject.FindProperty("_xMaxRange");
                _xRotationSpeed = serializedObject.FindProperty("_xRotationSpeed");

                _yAxisCelestial = serializedObject.FindProperty("_yAxisCelestial");
                _yRotationMode = serializedObject.FindProperty("_yRotationMode");
                _ySyncWithAxis = serializedObject.FindProperty("_ySyncWithAxis");
                _ySyncAxisIndex = serializedObject.FindProperty("_ySyncAxisIndex");
                _yOscillationSpeed = serializedObject.FindProperty("_yOscillationSpeed");
                _yMinRange = serializedObject.FindProperty("_yMinRange");
                _yMaxRange = serializedObject.FindProperty("_yMaxRange");
                _yRotationSpeed = serializedObject.FindProperty("_yRotationSpeed");

                _zAxisCelestial = serializedObject.FindProperty("_zAxisCelestial");
                _zRotationMode = serializedObject.FindProperty("_zRotationMode");
                _zSyncWithAxis = serializedObject.FindProperty("_zSyncWithAxis");
                _zSyncAxisIndex = serializedObject.FindProperty("_zSyncAxisIndex");
                _zOscillationSpeed = serializedObject.FindProperty("_zOscillationSpeed");
                _zMinRange = serializedObject.FindProperty("_zMinRange");
                _zMaxRange = serializedObject.FindProperty("_zMaxRange");
                _zRotationSpeed = serializedObject.FindProperty("_zRotationSpeed");
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                // Always show celestial body mode toggle
                EditorGUILayout.PropertyField(_celestialBodyMode,
                    new GUIContent("Celestial Body Mode",
                        "Enable celestial body movement with oscillation and world-space rotation"));

                EditorGUILayout.Space();

                if (_celestialBodyMode.boolValue)
                {
                    // Performance Settings Section
                    EditorGUILayout.LabelField("Performance Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(updateFrequency,
                        new GUIContent("Update Frequency (Hz)",
                            "How many times per second to update light position. Lower values improve performance."));

                    // Show performance info
                    if (updateFrequency.floatValue > 0)
                    {
                        float updateInterval = 1f / updateFrequency.floatValue;
                        EditorGUILayout.HelpBox($"Updates every {updateInterval:F3} seconds\n" +
                                                "Recommended values:\n" +
                                                "• 60 Hz: High precision (no performance gain)\n" +
                                                "• 30 Hz: Good balance (recommended)\n" +
                                                "• 15 Hz: Mobile/VR optimization", MessageType.Info);
                    }

                    EditorGUILayout.Space();

                    // Show ScriptableObject field
                    EditorGUILayout.PropertyField(seasonalData,
                        new GUIContent("Seasonal Data",
                            "Drag a SeasonalData ScriptableObject here to override manual settings"));

                    bool usingScriptableObject = seasonalData.objectReferenceValue != null;

                    if (usingScriptableObject)
                    {
                        // Show read-only info when using ScriptableObject
                        EditorGUILayout.Space();
                        EditorGUILayout.HelpBox("Using ScriptableObject settings. Manual settings below are read-only.",
                            MessageType.Info);

                        // Apply settings from ScriptableObject
                        if (GUILayout.Button("Apply ScriptableObject Settings"))
                        {
                            Rotator rotator = (Rotator)target;
                            rotator.SetSeasonalData((SeasonalData)seasonalData.objectReferenceValue);
                        }

                        EditorGUILayout.Space();

                        // Show current settings as read-only
                        EditorGUI.BeginDisabledGroup(true);
                        DrawCelestialBodyUI();
                        EditorGUI.EndDisabledGroup();
                    }
                    else
                    {
                        // Show editable celestial settings when not using ScriptableObject
                        DrawCelestialBodyUI();
                    }

                    // Only show rotation active for celestial bodies
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(rotationActive,
                        new GUIContent("Rotation Active", "Enable/disable all celestial rotation"));
                }
                else
                {
                    // Regular Rotation Mode UI - show all regular settings
                    DrawRegularRotationUI();

                    // Show wheel settings and general controls for regular mode
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Wheel Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(wheelRadius);
                    EditorGUILayout.PropertyField(rotationActive);
                    EditorGUILayout.PropertyField(getSpeedExternally);
                }

                serializedObject.ApplyModifiedProperties();
            }

            private void DrawCelestialBodyUI()
            {
                EditorGUILayout.LabelField("Celestial Body Settings", EditorStyles.boldLabel);

                EditorGUILayout.Space();

                // X-Axis (Elevation) Settings
                DrawAxisCelestialSettings("X-Axis Celestial Settings (Elevation)",
                    _xAxisCelestial, _xRotationMode, _xSyncWithAxis, _xSyncAxisIndex, _xOscillationSpeed, _xMinRange,
                    _xMaxRange, _xRotationSpeed,
                    "Enable celestial movement on X-axis (elevation)");

                EditorGUILayout.Space();

                // Y-Axis (Azimuth) Settings
                DrawAxisCelestialSettings("Y-Axis Celestial Settings (Azimuth)",
                    _yAxisCelestial, _yRotationMode, _ySyncWithAxis, _ySyncAxisIndex, _yOscillationSpeed, _yMinRange,
                    _yMaxRange, _yRotationSpeed,
                    "Enable celestial movement on Y-axis (azimuth)");

                EditorGUILayout.Space();

                // Z-Axis (Roll) Settings - Show but with warning
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox(
                    "Z-Axis rotation is not recommended for celestial lights (stars/suns) as it doesn't affect lighting.",
                    MessageType.Warning);
                DrawAxisCelestialSettings("Z-Axis Celestial Settings (Roll)",
                    _zAxisCelestial, _zRotationMode, _zSyncWithAxis, _zSyncAxisIndex, _zOscillationSpeed, _zMinRange,
                    _zMaxRange, _zRotationSpeed,
                    "Enable celestial movement on Z-axis (roll) - Not recommended for lights");
                EditorGUILayout.EndVertical();

                // Show helpful info box
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Celestial Body Mode uses world-space rotation:\n" +
                                        "• X-Axis = Elevation (up/down in world space)\n" +
                                        "• Y-Axis = Azimuth (left/right in world space)\n" +
                                        "• Z-Axis = Roll (rotation around forward axis) - Not useful for lights\n\n" +
                                        "Rotation Modes:\n" +
                                        "• Oscillate: Moves back and forth between min/max ranges\n" +
                                        "• Continuous: Rotates 360° perpetually at the specified speed\n\n" +
                                        "Sync Option: Synchronizes oscillation speed so one complete cycle happens per full rotation of the selected axis.",
                    MessageType.Info);
            }

            private void DrawAxisCelestialSettings(string headerLabel, SerializedProperty axisCelestial, 
                SerializedProperty rotationMode, SerializedProperty syncWithAxis, SerializedProperty syncAxisIndex,
                SerializedProperty oscillationSpeed, SerializedProperty minRange, SerializedProperty maxRange, 
                SerializedProperty rotationSpeed, string axisTooltip)
            {
                EditorGUILayout.LabelField(headerLabel, EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(axisCelestial, new GUIContent("Enable " + headerLabel.Split(' ')[0] + " Celestial", axisTooltip));
                
                if (axisCelestial.boolValue)
                {
                    EditorGUI.indentLevel++;
                    
                    // Rotation Mode dropdown
                    EditorGUILayout.PropertyField(rotationMode, new GUIContent("Rotation Mode", "Choose between oscillating movement or continuous 360° rotation"));
                    
                    // Cast the enum value properly
                    CelestialRotationMode currentMode = (CelestialRotationMode)rotationMode.enumValueIndex;
                    
                    if (currentMode == CelestialRotationMode.Oscillate)
                    {
                        // Sync option for oscillation
                        EditorGUILayout.PropertyField(syncWithAxis, new GUIContent("Sync with Axis", "Automatically sync oscillation speed with another axis's continuous rotation"));
                        
                        if (syncWithAxis.boolValue)
                        {
                            EditorGUI.indentLevel++;
                            string[] axisOptions = { "X-Axis (Elevation)", "Y-Axis (Azimuth)" }; // Removed Z-axis from sync options
                            syncAxisIndex.intValue = EditorGUILayout.Popup("Sync Axis", syncAxisIndex.intValue, axisOptions);
                            EditorGUI.indentLevel--;
                            
                            // Show calculated speed (read-only)
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.FloatField(new GUIContent("Calculated Speed", "Automatically calculated based on sync axis rotation speed"), 
                                GetCalculatedSyncSpeed(syncAxisIndex.intValue));
                            EditorGUI.EndDisabledGroup();
                        }
                        else
                        {
                            // Manual oscillation settings
                            EditorGUILayout.PropertyField(oscillationSpeed, new GUIContent("Oscillation Speed", "Speed of oscillation between min/max range"));
                        }
                        
                        EditorGUILayout.PropertyField(minRange, new GUIContent("Min Range (degrees)", "Minimum angle for oscillation"));
                        EditorGUILayout.PropertyField(maxRange, new GUIContent("Max Range (degrees)", "Maximum angle for oscillation"));
                        EditorGUILayout.PropertyField(rotationSpeed, new GUIContent("Additional Rotation Speed (rotations/hour)", "Continuous rotation speed while oscillating (optional)"));
                    }
                    else // Continuous rotation
                    {
                        // Continuous rotation settings
                        EditorGUILayout.PropertyField(rotationSpeed, new GUIContent("Rotation Speed (rotations/hour)", "Speed of continuous 360° rotation"));
                        
                        // Show helpful note for continuous mode
                        EditorGUILayout.HelpBox("1.0 = 1 rotation per hour\n24.0 = 24-hour day cycle (2.5 min real-time)\nUse positive values for clockwise, negative for counter-clockwise.", MessageType.Info);
                    }
                    
                    EditorGUI.indentLevel--;
                }
            }

            private float GetCalculatedSyncSpeed(int syncAxisIndex)
            {
                float syncAxisRotationSpeed = 0f;
                CelestialRotationMode syncAxisMode = CelestialRotationMode.Oscillate;

                // Get the rotation speed and mode of the sync axis
                switch (syncAxisIndex)
                {
                    case 0: // X-axis
                        syncAxisRotationSpeed = _xRotationSpeed.floatValue;
                        syncAxisMode = (CelestialRotationMode)_xRotationMode.enumValueIndex;
                        break;
                    case 1: // Y-axis
                        syncAxisRotationSpeed = _yRotationSpeed.floatValue;
                        syncAxisMode = (CelestialRotationMode)_yRotationMode.enumValueIndex;
                        break;
                }

                // Only calculate for continuous rotation axes
                if (syncAxisMode != CelestialRotationMode.Continuous || Mathf.Abs(syncAxisRotationSpeed) < 0.001f)
                    return 0f;

                // Convert rotations per hour to oscillations per second
                // If Y-axis does 24 rotations per hour, we want 1 complete oscillation per Y rotation
                float rotationsPerSecond = syncAxisRotationSpeed / 3600f; // Convert rotations/hour to rotations/second
                float oscillationsPerSecond =
                    rotationsPerSecond * Mathf.PI * 2f; // One complete sine cycle per rotation

                return oscillationsPerSecond;
            }

            private void DrawRegularRotationUI()
            {
                EditorGUILayout.LabelField("Regular Rotation Settings", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(_rotationSpeed,
                    new GUIContent("Rotation Speed", "Speed of rotation in degrees per second"));
                EditorGUILayout.PropertyField(_faceCamera,
                    new GUIContent("Face Camera", "Make the object face the camera"));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Rotation Axes", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_xAxisRot,
                    new GUIContent("X-Axis Rotation", "Enable rotation around X-axis"));
                EditorGUILayout.PropertyField(_yAxisRot,
                    new GUIContent("Y-Axis Rotation", "Enable rotation around Y-axis"));
                EditorGUILayout.PropertyField(_zAxisRot,
                    new GUIContent("Z-Axis Rotation", "Enable rotation around Z-axis"));

                // Show helpful info box
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "Regular Rotation Mode uses local-space rotation with the specified speed and axes.",
                    MessageType.Info);
            }
        }
}
