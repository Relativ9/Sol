using Sol;
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(SeasonalData.StarSettings))]
public class StarSettingsPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
            EditorGUI.BeginProperty(position, label, property);

        // Get all properties
        SerializedProperty enabled = property.FindPropertyRelative("enabled");
        SerializedProperty rotationMode = property.FindPropertyRelative("rotationMode");
        SerializedProperty oscillationSpeed = property.FindPropertyRelative("oscillationSpeed");
        SerializedProperty syncWithAxis = property.FindPropertyRelative("syncWithAxis");
        SerializedProperty syncAxisIndex = property.FindPropertyRelative("syncAxisIndex");
        SerializedProperty rotationSpeed = property.FindPropertyRelative("rotationSpeed");
        SerializedProperty minRange = property.FindPropertyRelative("minRange");
        SerializedProperty maxRange = property.FindPropertyRelative("maxRange");

        // Calculate rects for each line
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float currentY = position.y;
        
        Rect headerRect = new Rect(position.x, currentY, position.width, lineHeight);
        currentY += lineHeight;
        
        Rect enabledRect = new Rect(position.x, currentY, position.width, lineHeight);
        currentY += lineHeight;

        // Draw axis header with bold label
        GUIStyle headerStyle = new GUIStyle(EditorStyles.label);
        headerStyle.fontStyle = FontStyle.Bold;
        EditorGUI.LabelField(headerRect, label, headerStyle);

        // Draw enabled toggle with axis context
        string axisLabel = label.text.Split(' ')[0];
        string enabledLabel = $"{axisLabel}-Axis Rotation";
        EditorGUI.PropertyField(enabledRect, enabled, new GUIContent(enabledLabel));

        if (enabled.boolValue)
        {
            EditorGUI.indentLevel++;
            
            // Draw rotation mode
            Rect modeRect = new Rect(position.x, currentY, position.width, lineHeight);
            currentY += lineHeight;
            EditorGUI.PropertyField(modeRect, rotationMode);

            // Draw appropriate controls based on mode
            CelestialRotationMode mode = (CelestialRotationMode)rotationMode.enumValueIndex;
            
            if (mode == CelestialRotationMode.Oscillate)
            {
                // Sync option
                Rect syncRect = new Rect(position.x, currentY, position.width, lineHeight);
                currentY += lineHeight;
                EditorGUI.PropertyField(syncRect, syncWithAxis, new GUIContent("Sync with Axis"));
                
                if (syncWithAxis.boolValue)
                {
                    EditorGUI.indentLevel++;
                    Rect syncAxisRect = new Rect(position.x, currentY, position.width, lineHeight);
                    currentY += lineHeight;
                    
                    string[] axisOptions = { "X-Axis (Elevation)", "Y-Axis (Azimuth)" };
                    syncAxisIndex.intValue = EditorGUILayout.Popup("Sync Axis", syncAxisIndex.intValue, axisOptions);
                    EditorGUI.indentLevel--;
                }
                else
                {
                    // Manual oscillation speed
                    Rect speedRect = new Rect(position.x, currentY, position.width, lineHeight);
                    currentY += lineHeight;
                    string oscillationLabel = $"{axisLabel}-Axis Oscillation Speed";
                    EditorGUI.PropertyField(speedRect, oscillationSpeed, new GUIContent(oscillationLabel));
                }
                
                // Range settings
                Rect minRect = new Rect(position.x, currentY, position.width, lineHeight);
                currentY += lineHeight;
                Rect maxRect = new Rect(position.x, currentY, position.width, lineHeight);
                currentY += lineHeight;
                
                string minLabel = $"Minimum {axisLabel}-Axis Angle";
                string maxLabel = $"Maximum {axisLabel}-Axis Angle";
                EditorGUI.PropertyField(minRect, minRange, new GUIContent(minLabel));
                EditorGUI.PropertyField(maxRect, maxRange, new GUIContent(maxLabel));
            }
            else
            {
                // Continuous rotation settings
                Rect speedRect = new Rect(position.x, currentY, position.width, lineHeight);
                currentY += lineHeight;
                
                string rotationLabel = $"{axisLabel}-Axis Rotation Speed (rotations/hour)";
                EditorGUI.PropertyField(speedRect, rotationSpeed, new GUIContent(rotationLabel));
                
                // Show helpful info
                Rect infoRect = new Rect(position.x, currentY, position.width, lineHeight * 2);
                currentY += lineHeight * 2;
                EditorGUI.HelpBox(infoRect, "1.0 = 1 rotation per hour\n24.0 = 24-hour day cycle", MessageType.Info);
            }
            
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.FindPropertyRelative("enabled").boolValue)
            return EditorGUIUtility.singleLineHeight * 2; // Header + enabled toggle
            
        CelestialRotationMode mode = (CelestialRotationMode)property.FindPropertyRelative("rotationMode").enumValueIndex;
        bool syncWithAxis = property.FindPropertyRelative("syncWithAxis").boolValue;
        
        if (mode == CelestialRotationMode.Oscillate)
        {
            if (syncWithAxis)
                return EditorGUIUtility.singleLineHeight * 7; // Header + enabled + mode + sync + syncAxis + min + max
            else
                return EditorGUIUtility.singleLineHeight * 7; // Header + enabled + mode + sync + speed + min + max
        }
        else
        {
            return EditorGUIUtility.singleLineHeight * 6; // Header + enabled + mode + speed + info(2 lines)
        }
    }
}
