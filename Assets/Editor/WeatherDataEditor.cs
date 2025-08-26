using UnityEngine;
using UnityEditor;

namespace Sol
{
    [CustomEditor(typeof(WeatherData))]
    public class WeatherDataEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Draw default script field
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromScriptableObject((WeatherData)target), typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // Basic info
            EditorGUILayout.PropertyField(serializedObject.FindProperty("weatherName"));
            EditorGUILayout.Space();
            
            // Weather pattern
            EditorGUILayout.LabelField("Weather Pattern", EditorStyles.boldLabel);
            var snowChanceProp = serializedObject.FindProperty("snowChance");
            EditorGUILayout.PropertyField(snowChanceProp);
            EditorGUILayout.LabelField($"Snow Probability: {snowChanceProp.floatValue * 100f:F0}%", EditorStyles.helpBox);
            EditorGUILayout.Space();
            
            // Duration ranges
            EditorGUILayout.LabelField("Duration Ranges (Hours)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minSnowDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSnowDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minClearDuration"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxClearDuration"));
            
            // Show duration summary
            var minSnow = serializedObject.FindProperty("minSnowDuration").floatValue;
            var maxSnow = serializedObject.FindProperty("maxSnowDuration").floatValue;
            var minClear = serializedObject.FindProperty("minClearDuration").floatValue;
            var maxClear = serializedObject.FindProperty("maxClearDuration").floatValue;
            
            EditorGUILayout.LabelField($"Snow: {minSnow:F1}-{maxSnow:F1}h | Clear: {minClear:F1}-{maxClear:F1}h", EditorStyles.helpBox);
            EditorGUILayout.Space();
            
            // Visual effects
            EditorGUILayout.LabelField("Visual Effects", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("minSnowEmissionRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSnowEmissionRate"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("snowTint"));
            
            // Show emission summary
            var minEmission = serializedObject.FindProperty("minSnowEmissionRate").floatValue;
            var maxEmission = serializedObject.FindProperty("maxSnowEmissionRate").floatValue;
            EditorGUILayout.LabelField($"Emission Range: {minEmission:F0}-{maxEmission:F0} particles/sec", EditorStyles.helpBox);
            EditorGUILayout.Space();
            
            // Audio (conditional)
            EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
            var windSoundProp = serializedObject.FindProperty("windSound");
            EditorGUILayout.PropertyField(windSoundProp);
            
            if (windSoundProp.objectReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                var windVolumeProp = serializedObject.FindProperty("windVolume");
                EditorGUILayout.PropertyField(windVolumeProp);
                EditorGUILayout.LabelField($"Volume: {windVolumeProp.floatValue * 100f:F0}%", EditorStyles.helpBox);
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.LabelField("No wind sound assigned", EditorStyles.helpBox);
            }
            
            EditorGUILayout.Space();
            
            // Validation section
            DrawValidationSection();
            
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawValidationSection()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            
            WeatherData weatherData = (WeatherData)target;
            bool hasIssues = false;
            
            // Check duration ranges
            if (weatherData.MinSnowDuration >= weatherData.MaxSnowDuration)
            {
                EditorGUILayout.HelpBox("Snow duration: Min should be less than Max", MessageType.Warning);
                hasIssues = true;
            }
            
            if (weatherData.MinClearDuration >= weatherData.MaxClearDuration)
            {
                EditorGUILayout.HelpBox("Clear duration: Min should be less than Max", MessageType.Warning);
                hasIssues = true;
            }
            
            // Check emission ranges
            if (weatherData.MinSnowEmissionRate >= weatherData.MaxSnowEmissionRate)
            {
                EditorGUILayout.HelpBox("Emission rate: Min should be less than Max", MessageType.Warning);
                hasIssues = true;
            }
            
            // Check for reasonable values
            if (weatherData.SnowChance == 0f)
            {
                EditorGUILayout.HelpBox("Snow chance is 0% - no snow will occur", MessageType.Info);
            }
            else if (weatherData.SnowChance == 1f)
            {
                EditorGUILayout.HelpBox("Snow chance is 100% - snow will always occur", MessageType.Info);
            }
            
            if (weatherData.WindSound != null && weatherData.WindVolume == 0f)
            {
                EditorGUILayout.HelpBox("Wind sound assigned but volume is 0%", MessageType.Warning);
                hasIssues = true;
            }
            
            if (!hasIssues)
            {
                EditorGUILayout.HelpBox("Weather data configuration looks good!", MessageType.Info);
            }
        }
    }
}