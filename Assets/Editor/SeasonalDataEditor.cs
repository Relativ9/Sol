using Sol;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SeasonalData))]
public class SeasonalDataEditor : Editor
{
    private SerializedProperty xAxis;
    private SerializedProperty yAxis;

    private void OnEnable()
    {
        xAxis = serializedObject.FindProperty("xAxis");
        yAxis = serializedObject.FindProperty("yAxis");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Celestial Body Seasonal Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(xAxis, new GUIContent("X-Axis Settings (Elevation)"));
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(yAxis, new GUIContent("Y-Axis Settings (Azimuth)"));

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("X-Axis controls elevation (up/down movement of celestial bodies)\n" +
                                "Y-Axis controls azimuth (rotation around the planet)\n\n" +
                                "Rotation Speed: 1.0 = 1 full rotation per hour", 
            MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}
