// using UnityEngine;
// using UnityEditor;
// using System.Collections.Generic;
//
// namespace Sol.Editor
// {
//     [CustomEditor(typeof(WorldTimeData))]
//     public class WorldTimeDataEditor : UnityEditor.Editor
//     {
//         private Dictionary<int, bool> seasonFoldouts = new Dictionary<int, bool>();
//         private bool showAdvancedSettings = false;
//         private bool showDebugSettings = false;
//
//         public override void OnInspectorGUI()
//         {
//             serializedObject.Update();
//
//             DrawHeader();
//             EditorGUILayout.Space(10);
//
//             DrawTimeConfiguration();
//             EditorGUILayout.Space(10);
//
//             DrawSeasonConfiguration();
//             EditorGUILayout.Space(10);
//
//             DrawStartingTimeConfiguration();
//             EditorGUILayout.Space(10);
//
//             DrawDayNightCycleConfiguration();
//             EditorGUILayout.Space(10);
//
//             DrawLightingConfiguration();
//             EditorGUILayout.Space(10);
//
//             DrawAdvancedSettings();
//             EditorGUILayout.Space(10);
//
//             DrawDebugSettings();
//             EditorGUILayout.Space(10);
//
//             DrawRuntimeInformation();
//
//             serializedObject.ApplyModifiedProperties();
//         }
//
//         private void DrawHeader()
//         {
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             EditorGUILayout.LabelField("World Time Configuration", EditorStyles.largeLabel);
//             EditorGUILayout.Space(5);
//             EditorGUILayout.HelpBox("Configure the world's time system, seasons, and celestial cycles. TimeManager will cycle through seasons based on day count and season lengths.", MessageType.Info);
//             EditorGUILayout.EndVertical();
//         }
//
//         private void DrawTimeConfiguration()
//         {
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             EditorGUILayout.LabelField("Time Configuration", EditorStyles.boldLabel);
//             EditorGUILayout.Space(5);
//
//             var dayLengthProperty = serializedObject.FindProperty("dayLengthInSeconds");
//             EditorGUILayout.PropertyField(dayLengthProperty, new GUIContent("Day Length (Seconds)", "Length of one day in real-world seconds"));
//
//             // Show day length in minutes for clarity
//             float minutes = dayLengthProperty.floatValue / 60f;
//             EditorGUILayout.LabelField($"Day Length: {minutes:F1} minutes ({dayLengthProperty.floatValue:F0} seconds)");
//
//             EditorGUILayout.Space(5);
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("enableTimeProgression"), 
//                 new GUIContent("Enable Time Progression", "Whether time automatically progresses"));
//             
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("timeProgressionSpeed"), 
//                 new GUIContent("Time Speed Multiplier", "Speed multiplier for time progression"));
//             
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("pauseTime"), 
//                 new GUIContent("Pause Time", "Temporarily pause time progression"));
//
//             EditorGUILayout.EndVertical();
//         }
//
//         private void DrawSeasonConfiguration()
//         {
//             var seasonsProperty = serializedObject.FindProperty("seasons");
//
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//
//             // Section header with add button
//             EditorGUILayout.BeginHorizontal();
//             EditorGUILayout.LabelField("Season Configuration", EditorStyles.boldLabel);
//             GUILayout.FlexibleSpace();
//             if (GUILayout.Button("Add Season", GUILayout.Width(100)))
//             {
//                 seasonsProperty.arraySize++;
//                 var newElement = seasonsProperty.GetArrayElementAtIndex(seasonsProperty.arraySize - 1);
//                 SetDefaultSeasonValues(newElement, seasonsProperty.arraySize);
//                 seasonFoldouts[seasonsProperty.arraySize - 1] = true;
//             }
//             EditorGUILayout.EndHorizontal();
//
//             if (seasonsProperty.arraySize == 0)
//             {
//                 EditorGUILayout.HelpBox("No seasons configured. Add at least one season for the time system to function.", MessageType.Error);
//             }
//             else
//             {
//                 EditorGUILayout.Space(5);
//
//                 // Show total year length
//                 int totalDays = CalculateTotalYearLength(seasonsProperty);
//                 EditorGUILayout.HelpBox($"Total Year Length: {totalDays} days", MessageType.Info);
//
//                 EditorGUILayout.Space(5);
//
//                 // Add padding to prevent foldout arrow cutoff
//                 EditorGUILayout.BeginVertical();
//                 GUILayout.Space(2);
//
//                 for (int i = 0; i < seasonsProperty.arraySize; i++)
//                 {
//                     // Add left margin to ensure foldout arrow is visible
//                     EditorGUILayout.BeginHorizontal();
//                     GUILayout.Space(4);
//
//                     EditorGUILayout.BeginVertical();
//                     DrawSeasonElement(seasonsProperty, i);
//                     EditorGUILayout.EndVertical();
//
//                     EditorGUILayout.EndHorizontal();
//                 }
//
//                 GUILayout.Space(2);
//                 EditorGUILayout.EndVertical();
//             }
//
//             EditorGUILayout.EndVertical();
//         }
//
//         private void DrawSeasonElement(SerializedProperty seasonsProperty, int index)
//         {
//             var element = seasonsProperty.GetArrayElementAtIndex(index);
//             var seasonNameProperty = element.FindPropertyRelative("seasonName");
//             var lengthInDaysProperty = element.FindPropertyRelative("lengthInDays");
//             var seasonalDataProperty = element.FindPropertyRelative("seasonalData");
//
//             // Initialize foldout state if needed
//             if (!seasonFoldouts.ContainsKey(index))
//                 seasonFoldouts[index] = false;
//
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//
//             // Header with manual arrow and spaced frame
//             Rect headerRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
//             
//             string displayName = string.IsNullOrEmpty(seasonNameProperty.stringValue) ? $"Season {index + 1}" : seasonNameProperty.stringValue;
//             string headerText = $"{displayName} ({lengthInDaysProperty.intValue} days)";
//             
//             // Calculate layout
//             float arrowWidth = 12f;
//             float spacingAfterArrow = 8f;
//             float frameStartX = headerRect.x + arrowWidth + spacingAfterArrow;
//             float frameWidth = headerRect.width - arrowWidth - spacingAfterArrow;
//             
//             // Create rects
//             Rect arrowRect = new Rect(headerRect.x, headerRect.y, arrowWidth, headerRect.height);
//             Rect frameRect = new Rect(frameStartX, headerRect.y, frameWidth, headerRect.height);
//             
//             // Draw the foldout arrow using Unity's style
//             EditorGUI.BeginChangeCheck();
//             bool newFoldoutState = EditorGUI.Foldout(arrowRect, seasonFoldouts[index], "");
//             if (EditorGUI.EndChangeCheck())
//             {
//                 seasonFoldouts[index] = newFoldoutState;
//             }
//             
//             // Draw the frame background
//             GUI.Box(frameRect, "", EditorStyles.toolbar);
//             
//             // Calculate content areas within the frame
//             float deleteButtonWidth = 25f;
//             float padding = 4f;
//             
//             float labelWidth = frameWidth - deleteButtonWidth - (padding * 2);
//             
//             Rect labelRect = new Rect(frameRect.x + padding, frameRect.y, labelWidth, frameRect.height);
//             Rect deleteButtonRect = new Rect(frameRect.xMax - deleteButtonWidth - padding, frameRect.y, deleteButtonWidth, frameRect.height);
//             
//             // Draw content within the frame
//             EditorGUI.LabelField(labelRect, headerText);
//             
//             // Delete button
//             GUI.backgroundColor = Color.red;
//             if (GUI.Button(deleteButtonRect, "×"))
//             {
//                 seasonsProperty.DeleteArrayElementAtIndex(index);
//                 GUI.backgroundColor = Color.white;
//                 EditorGUILayout.EndVertical();
//                 return;
//             }
//             GUI.backgroundColor = Color.white;
//
//             // Content area (only show if expanded)
//             if (seasonFoldouts[index])
//             {
//                 EditorGUILayout.Space(5);
//                 EditorGUI.indentLevel++;
//                 
//                 // Basic configuration
//                 EditorGUILayout.PropertyField(seasonNameProperty, new GUIContent("Season Name", "Display name for this season"));
//                 EditorGUILayout.PropertyField(lengthInDaysProperty, new GUIContent("Length (Days)", "Duration of this season in days"));
//                 EditorGUILayout.PropertyField(seasonalDataProperty, new GUIContent("Seasonal Data", "SeasonalData asset containing celestial body configurations"));
//                 
//                 // Season-specific settings
//                 EditorGUILayout.Space(5);
//                 var overrideAmbientProperty = element.FindPropertyRelative("overrideAmbientColors");
//                 EditorGUILayout.PropertyField(overrideAmbientProperty, new GUIContent("Override Ambient Colors", "Use custom ambient colors for this season"));
//                 
//                 if (overrideAmbientProperty.boolValue)
//                 {
//                     EditorGUI.indentLevel++;
//                     EditorGUILayout.PropertyField(element.FindPropertyRelative("seasonDayAmbient"), new GUIContent("Day Ambient"));
//                     EditorGUILayout.PropertyField(element.FindPropertyRelative("seasonNightAmbient"), new GUIContent("Night Ambient"));
//                                         EditorGUI.indentLevel--;
//                 }
//                 
//                 EditorGUILayout.Space(5);
//                 EditorGUILayout.PropertyField(element.FindPropertyRelative("defaultWeatherIntensity"), 
//                     new GUIContent("Default Weather Intensity", "Base weather intensity for this season"));
//                 EditorGUILayout.PropertyField(element.FindPropertyRelative("weatherVariation"), 
//                     new GUIContent("Weather Variation", "How much weather can vary from the default"));
//                 
//                 EditorGUI.indentLevel--;
//             }
//
//             EditorGUILayout.EndVertical();
//             EditorGUILayout.Space(3);
//         }
//
//         private void DrawStartingTimeConfiguration()
//         {
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             EditorGUILayout.LabelField("Starting Time Configuration", EditorStyles.boldLabel);
//             EditorGUILayout.Space(5);
//
//             var startingSeasonProperty = serializedObject.FindProperty("startingSeasonIndex");
//             var startingDayProperty = serializedObject.FindProperty("startingDayInSeason");
//             var startingTimeProperty = serializedObject.FindProperty("startingTimeOfDay");
//
//             EditorGUILayout.PropertyField(startingSeasonProperty, new GUIContent("Starting Season Index", "Which season to start in (0-based)"));
//             EditorGUILayout.PropertyField(startingDayProperty, new GUIContent("Starting Day in Season", "Which day of the season to start on (0-based)"));
//             EditorGUILayout.PropertyField(startingTimeProperty, new GUIContent("Starting Time of Day", "Time of day to start at (0.0 = dawn, 0.5 = noon, 1.0 = dusk)"));
//
//             // Show formatted time
//             float timeValue = startingTimeProperty.floatValue;
//             string formattedTime = GetFormattedTime(timeValue);
//             EditorGUILayout.LabelField($"Starting Time: {formattedTime}");
//
//             EditorGUILayout.EndVertical();
//         }
//
//         private void DrawDayNightCycleConfiguration()
//         {
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             EditorGUILayout.LabelField("Day/Night Cycle Configuration", EditorStyles.boldLabel);
//             EditorGUILayout.Space(5);
//
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("dawnHour"), new GUIContent("Dawn Hour", "Hour when dawn begins (0-23)"));
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("dayHour"), new GUIContent("Day Hour", "Hour when day begins (0-23)"));
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("duskHour"), new GUIContent("Dusk Hour", "Hour when dusk begins (0-23)"));
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("nightHour"), new GUIContent("Night Hour", "Hour when night begins (0-23)"));
//
//             EditorGUILayout.EndVertical();
//         }
//
//         private void DrawLightingConfiguration()
//         {
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             EditorGUILayout.LabelField("Lighting Configuration", EditorStyles.boldLabel);
//             EditorGUILayout.Space(5);
//
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("dayAmbientColor"), new GUIContent("Day Ambient Color"));
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("nightAmbientColor"), new GUIContent("Night Ambient Color"));
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("dayFogColor"), new GUIContent("Day Fog Color"));
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("nightFogColor"), new GUIContent("Night Fog Color"));
//
//             EditorGUILayout.Space(5);
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("dayLightIntensity"), new GUIContent("Day Light Intensity"));
//             EditorGUILayout.PropertyField(serializedObject.FindProperty("nightLightIntensity"), new GUIContent("Night Light Intensity"));
//
//             EditorGUILayout.EndVertical();
//         }
//
//         private void DrawAdvancedSettings()
//         {
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             
//             EditorGUILayout.BeginHorizontal();
//             showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings", true);
//             EditorGUILayout.EndHorizontal();
//
//             if (showAdvancedSettings)
//             {
//                 EditorGUILayout.Space(5);
//                 
//                 // Weather Integration
//                 EditorGUILayout.LabelField("Weather Integration", EditorStyles.miniBoldLabel);
//                 EditorGUILayout.PropertyField(serializedObject.FindProperty("enableWeatherEffects"), 
//                     new GUIContent("Enable Weather Effects", "Enable weather effects on lighting"));
//                 EditorGUILayout.PropertyField(serializedObject.FindProperty("stormLightReduction"), 
//                     new GUIContent("Storm Light Reduction", "Light intensity reduction during storms"));
//                 EditorGUILayout.PropertyField(serializedObject.FindProperty("stormFogIncrease"), 
//                     new GUIContent("Storm Fog Increase", "Fog density increase during storms"));
//
//                 EditorGUILayout.Space(5);
//                 
//                 // Performance Settings
//                 EditorGUILayout.LabelField("Performance Settings", EditorStyles.miniBoldLabel);
//                 EditorGUILayout.PropertyField(serializedObject.FindProperty("lightingUpdateFrequency"), 
//                     new GUIContent("Lighting Update Frequency", "Update frequency for lighting changes (Hz)"));
//                 EditorGUILayout.PropertyField(serializedObject.FindProperty("enableSmoothLighting"), 
//                     new GUIContent("Enable Smooth Lighting", "Enable smooth lighting transitions"));
//                 EditorGUILayout.PropertyField(serializedObject.FindProperty("lightingTransitionSpeed"), 
//                     new GUIContent("Lighting Transition Speed", "Speed of lighting transitions"));
//             }
//
//             EditorGUILayout.EndVertical();
//         }
//
//         private void DrawDebugSettings()
//         {
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             
//             EditorGUILayout.BeginHorizontal();
//             showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug Settings", true);
//             EditorGUILayout.EndHorizontal();
//
//             if (showDebugSettings)
//             {
//                 EditorGUILayout.Space(5);
//                 
//                 EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDebugLogging"), 
//                     new GUIContent("Enable Debug Logging", "Enable debug logging"));
//                 EditorGUILayout.PropertyField(serializedObject.FindProperty("showTimeInSceneView"), 
//                     new GUIContent("Show Time in Scene View", "Show time information in scene view"));
//                 EditorGUILayout.PropertyField(serializedObject.FindProperty("debugTimeSpeed"), 
//                     new GUIContent("Debug Time Speed", "Debug time progression speed (for testing)"));
//             }
//
//             EditorGUILayout.EndVertical();
//         }
//
//         private void DrawRuntimeInformation()
//         {
//             if (!Application.isPlaying)
//                 return;
//
//             EditorGUILayout.BeginVertical(EditorStyles.helpBox);
//             EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
//             EditorGUILayout.Space(5);
//
//             var worldTimeData = target as WorldTimeData;
//             if (worldTimeData != null)
//             {
//                 // Find TimeManager in scene
//                 var timeManager = FindObjectOfType<TimeManager>();
//                 if (timeManager != null)
//                 {
//                     EditorGUILayout.LabelField($"Current Season: {timeManager.CurrentSeasonName}");
//                     EditorGUILayout.LabelField($"Season Index: {timeManager.CurrentSeasonIndex}");
//                     EditorGUILayout.LabelField($"Day in Season: {timeManager.CurrentDayInSeason}");
//                     EditorGUILayout.LabelField($"Season Length: {timeManager.CurrentSeasonLength} days");
//                     EditorGUILayout.LabelField($"Current Time: {GetFormattedTime(timeManager.CelestialTime)}");
//                     
//                     float seasonProgress = worldTimeData.GetSeasonProgress(timeManager.CurrentDayInSeason, timeManager.CurrentSeasonIndex);
//                     EditorGUILayout.LabelField($"Season Progress: {seasonProgress:P1}");
//                 }
//                 else
//                 {
//                     EditorGUILayout.HelpBox("No TimeManager found in scene", MessageType.Info);
//                 }
//             }
//
//             EditorGUILayout.EndVertical();
//         }
//
//         private void SetDefaultSeasonValues(SerializedProperty seasonElement, int seasonNumber)
//         {
//             var seasonNameProperty = seasonElement.FindPropertyRelative("seasonName");
//             var lengthInDaysProperty = seasonElement.FindPropertyRelative("lengthInDays");
//             var defaultWeatherProperty = seasonElement.FindPropertyRelative("defaultWeatherIntensity");
//             var weatherVariationProperty = seasonElement.FindPropertyRelative("weatherVariation");
//
//             seasonNameProperty.stringValue = $"Season {seasonNumber}";
//             lengthInDaysProperty.intValue = 30;
//             defaultWeatherProperty.floatValue = 0f;
//             weatherVariationProperty.floatValue = 0.2f;
//         }
//
//         private int CalculateTotalYearLength(SerializedProperty seasonsProperty)
//         {
//             int total = 0;
//             for (int i = 0; i < seasonsProperty.arraySize; i++)
//             {
//                 var element = seasonsProperty.GetArrayElementAtIndex(i);
//                 var lengthProperty = element.FindPropertyRelative("lengthInDays");
//                 total += lengthProperty.intValue;
//             }
//             return total;
//         }
//
//         private string GetFormattedTime(float normalizedTime)
//         {
//             float totalHours = normalizedTime * 24f;
//             int hours = Mathf.FloorToInt(totalHours);
//             int minutes = Mathf.FloorToInt((totalHours - hours) * 60f);
//             return $"{hours:D2}:{minutes:D2}";
//         }
//     }
// }