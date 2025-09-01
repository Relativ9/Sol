using UnityEngine;
using UnityEditor;

namespace Sol.Editor
{
    [CustomEditor(typeof(CelestialRotator))]
    public class CelestialRotatorEditor : UnityEditor.Editor
    {
        private SerializedProperty celestialBodyNameProp;
        private SerializedProperty isMoonProp;
        private SerializedProperty baseRotationOffsetProp;
        private SerializedProperty useQuaternionRotationProp;
        private SerializedProperty smoothRotationProp;
        private SerializedProperty rotationSmoothSpeedProp;
        private SerializedProperty enablePerformanceOptimizationProp;
        private SerializedProperty updateFrequencyProp;
        private SerializedProperty enableDebugLoggingProp;

        private void OnEnable()
        {
            celestialBodyNameProp = serializedObject.FindProperty("celestialBodyName");
            isMoonProp = serializedObject.FindProperty("isMoon");
            baseRotationOffsetProp = serializedObject.FindProperty("baseRotationOffset");
            useQuaternionRotationProp = serializedObject.FindProperty("useQuaternionRotation");
            smoothRotationProp = serializedObject.FindProperty("smoothRotation");
            rotationSmoothSpeedProp = serializedObject.FindProperty("rotationSmoothSpeed");
            enablePerformanceOptimizationProp = serializedObject.FindProperty("enablePerformanceOptimization");
            updateFrequencyProp = serializedObject.FindProperty("updateFrequency");
            enableDebugLoggingProp = serializedObject.FindProperty("enableDebugLogging");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawCelestialBodyConfiguration();
            EditorGUILayout.Space(10);

            DrawRotationMethod();
            EditorGUILayout.Space(10);

            DrawPerformanceSettings();
            EditorGUILayout.Space(10);

            DrawDebugSettings();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Celestial Rotator", EditorStyles.largeLabel);
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("Controls the rotation of celestial bodies (suns, moons) based on time progression and seasonal data. Uses quaternion rotation to avoid gimbal lock.", MessageType.Info);
            EditorGUILayout.EndVertical();
        }

        private void DrawCelestialBodyConfiguration()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Celestial Body Configuration", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(celestialBodyNameProp, new GUIContent("Celestial Body Name", 
                "Name of the celestial body in SeasonalData. Must match exactly (case-sensitive)."));

            EditorGUILayout.PropertyField(isMoonProp, new GUIContent("Is Moon", 
                "Whether this is a moon (affects orbital calculations and phase offsets) or a star/sun."));

            EditorGUILayout.PropertyField(baseRotationOffsetProp, new GUIContent("Base Rotation Offset", 
                "Additional rotation offset applied to the calculated celestial position. Use for fine-tuning alignment."));

            // Show current celestial body info if available
            CelestialRotator rotator = (CelestialRotator)target;
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Runtime Information", EditorStyles.miniBoldLabel);
                
                Vector3 currentRotation = rotator.CurrentRotation;
                EditorGUILayout.LabelField($"Current Rotation: ({currentRotation.x:F1}°, {currentRotation.y:F1}°, {currentRotation.z:F1}°)");
                EditorGUILayout.LabelField($"Rotation Method: {(rotator.UseQuaternionRotation ? "Quaternions" : "Euler Angles")}");
                
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawRotationMethod()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Rotation Method", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(useQuaternionRotationProp, new GUIContent("Use Quaternion Rotation", 
                "Use quaternions instead of Euler angles. Recommended to avoid gimbal lock when celestial bodies pass overhead."));

            if (!useQuaternionRotationProp.boolValue)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("Warning: Euler angles may cause gimbal lock when celestial bodies pass near overhead (90° elevation). Consider using quaternion rotation.", MessageType.Warning);
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(smoothRotationProp, new GUIContent("Smooth Rotation", 
                "Enable smooth interpolation between rotation updates. Provides smoother visual movement but uses more CPU."));

            if (smoothRotationProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(rotationSmoothSpeedProp, new GUIContent("Smooth Speed", 
                    "Speed of rotation interpolation. Higher values = faster convergence to target rotation."));
                
                if (rotationSmoothSpeedProp.floatValue <= 0f)
                {
                    EditorGUILayout.HelpBox("Smooth speed must be greater than 0", MessageType.Error);
                }
                
                EditorGUI.indentLevel--;
            }

            // Show interaction info
            EditorGUILayout.Space(5);
            string interactionInfo = GetRotationMethodInfo();
            EditorGUILayout.HelpBox(interactionInfo, MessageType.Info);

            EditorGUILayout.EndVertical();
        }

        private void DrawPerformanceSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Performance Optimization", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(enablePerformanceOptimizationProp, new GUIContent("Enable Performance Optimization", 
                "Reduce update frequency for better performance. Rotation calculations are still done every frame for smooth time progression."));

            if (enablePerformanceOptimizationProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(updateFrequencyProp, new GUIContent("Update Frequency (Hz)", 
                    "How often to apply rotation updates per second. Higher values = smoother movement but more CPU usage."));

                if (updateFrequencyProp.floatValue <= 0f)
                {
                    EditorGUILayout.HelpBox("Update frequency must be greater than 0", MessageType.Error);
                }
                else if (updateFrequencyProp.floatValue > 60f)
                {
                    EditorGUILayout.HelpBox("Update frequencies above 60Hz may not provide noticeable benefits", MessageType.Warning);
                }

                EditorGUI.indentLevel--;

                // Show performance impact info
                EditorGUILayout.Space(5);
                string performanceInfo = GetPerformanceInfo();
                EditorGUILayout.HelpBox(performanceInfo, MessageType.Info);
            }
            else
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("Rotation will be updated every frame (60+ FPS). This provides the smoothest movement but uses more CPU.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDebugSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Debug Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.PropertyField(enableDebugLoggingProp, new GUIContent("Enable Debug Logging", 
                "Log rotation calculations and celestial body information to the console. Useful for debugging but may impact performance."));

            if (enableDebugLoggingProp.boolValue)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.HelpBox("Debug logging is enabled. Check the console for celestial rotation information. Disable for production builds.", MessageType.Warning);
            }

            // Debug buttons for runtime testing
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Runtime Controls", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Toggle Rotation Method"))
                {
                    CelestialRotator rotator = (CelestialRotator)target;
                    rotator.SetUseQuaternionRotation(!rotator.UseQuaternionRotation);
                }
                if (GUILayout.Button("Log Current State"))
                {
                    CelestialRotator rotator = (CelestialRotator)target;
                    Debug.Log($"[CelestialRotator] {rotator.name}: Rotation={rotator.CurrentRotation}, Method={rotator.UseQuaternionRotation}");
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private string GetRotationMethodInfo()
        {
            bool useQuaternions = useQuaternionRotationProp.boolValue;
            bool smoothRotation = smoothRotationProp.boolValue;
            bool optimization = enablePerformanceOptimizationProp.boolValue;

            string info = "";

            if (useQuaternions && smoothRotation && optimization)
            {
                info = "Configuration: Quaternion rotation with smooth interpolation and performance optimization.\n";
                info += "• Rotation calculated every frame for smooth time progression\n";
                info += "• Smooth interpolation applied every frame\n";
                info += "• No gimbal lock issues\n";
                info += "• Moderate CPU usage";
            }
            else if (useQuaternions && smoothRotation && !optimization)
            {
                info = "Configuration: Quaternion rotation with smooth interpolation, no optimization.\n";
                info += "• Highest quality movement\n";
                info += "• No gimbal lock issues\n";
                info += "• Higher CPU usage";
            }
            else if (useQuaternions && !smoothRotation && optimization)
            {
                info = "Configuration: Quaternion rotation with optimization, no smoothing.\n";
                info += "• Good performance\n";
                info += "• No gimbal lock issues\n";
                info += "• May appear slightly choppy during fast time progression";
            }
            else if (!useQuaternions)
            {
                info = "Configuration: Euler angle rotation (legacy mode).\n";
                info += "• May experience gimbal lock near overhead positions\n";
                info += "• Slightly better performance\n";
                info += "• Recommended only for debugging or compatibility";
            }
            else
            {
                info = "Standard quaternion rotation configuration.";
            }

            return info;
        }

        private string GetPerformanceInfo()
        {
            float frequency = updateFrequencyProp.floatValue;
            bool smoothRotation = smoothRotationProp.boolValue;

            string info = $"Update frequency: {frequency:F1} Hz ({1000f/frequency:F1}ms intervals)\n";

            if (smoothRotation)
            {
                info += "Note: Smooth rotation overrides optimization and updates every frame for interpolation.";
            }
            else
            {
                info += $"Rotation will be applied {frequency:F0} times per second instead of every frame.";
            }

            return info;
        }

        // Validation
        private void OnValidate()
        {
            if (rotationSmoothSpeedProp != null && rotationSmoothSpeedProp.floatValue <= 0f)
            {
                rotationSmoothSpeedProp.floatValue = 2f;
            }

            if (updateFrequencyProp != null && updateFrequencyProp.floatValue <= 0f)
            {
                updateFrequencyProp.floatValue = 10f;
            }
        }
    }
}