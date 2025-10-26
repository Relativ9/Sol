using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;

/// <summary>
/// Editor utility to inspect HDAdditionalLightData component properties via reflection.
/// Helps discover actual property names for celestial body configuration.
/// </summary>
public class HDLightDataInspector : EditorWindow
{
    private Light selectedLight;
    private Vector2 scrollPosition;
    private string searchFilter = "";

    [MenuItem("Tools/Sol/Inspect HD Light Data")]
    public static void ShowWindow()
    {
        GetWindow<HDLightDataInspector>("HD Light Inspector");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("HD Additional Light Data Inspector", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Light selection
        selectedLight = (Light)EditorGUILayout.ObjectField("Light Component", selectedLight, typeof(Light), true);

        if (selectedLight == null)
        {
            EditorGUILayout.HelpBox("Select a Light GameObject to inspect its HDAdditionalLightData", MessageType.Info);
            return;
        }

        // Search filter
        searchFilter = EditorGUILayout.TextField("Search Filter", searchFilter);

        EditorGUILayout.Space();

        if (GUILayout.Button("Dump All Properties to Console"))
        {
            DumpAllProperties();
        }

        // ⭐ ADD THIS NEW BUTTON HERE ⭐
        if (GUILayout.Button("Dump Celestial Enum Values"))
        {
            DumpEnumValues();
        }

        EditorGUILayout.Space();

        // Display properties
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        DisplayProperties();
        EditorGUILayout.EndScrollView();
    }

    private void DisplayProperties()
    {
        var hdLightData = selectedLight.GetComponent<MonoBehaviour>();
        if (hdLightData == null)
        {
            EditorGUILayout.HelpBox("No HDAdditionalLightData component found on this Light", MessageType.Warning);
            return;
        }

        var type = hdLightData.GetType();
        EditorGUILayout.LabelField($"Type: {type.FullName}", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        // Get all properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .OrderBy(p => p.Name)
            .ToArray();

        EditorGUILayout.LabelField($"Public Properties ({properties.Length} total)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        foreach (var prop in properties)
        {
            if (!string.IsNullOrEmpty(searchFilter) && 
                !prop.Name.ToLower().Contains(searchFilter.ToLower()))
                continue;

            EditorGUILayout.BeginHorizontal();
            
            // Property name
            EditorGUILayout.LabelField(prop.Name, GUILayout.Width(200));
            
            // Property type
            EditorGUILayout.LabelField($"({prop.PropertyType.Name})", EditorStyles.miniLabel, GUILayout.Width(150));

            // Try to get current value
            if (prop.CanRead)
            {
                try
                {
                    var value = prop.GetValue(hdLightData);
                    string valueStr = value != null ? value.ToString() : "null";
                    
                    // Special handling for enums
                    if (prop.PropertyType.IsEnum)
                    {
                        valueStr = $"{value} ({(int)value})";
                    }
                    
                    EditorGUILayout.LabelField($"= {valueStr}", EditorStyles.miniLabel);
                }
                catch (System.Exception e)
                {
                    EditorGUILayout.LabelField($"Error: {e.Message}", EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.LabelField("(write-only)", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    private void DumpAllProperties()
    {
        var hdLightData = selectedLight.GetComponent<MonoBehaviour>();
        if (hdLightData == null)
        {
            Debug.LogError("No HDAdditionalLightData found!");
            return;
        }

        var type = hdLightData.GetType();
        Debug.Log($"=== HD LIGHT DATA DUMP: {selectedLight.name} ===");
        Debug.Log($"Type: {type.FullName}");
        Debug.Log("");

        // Properties
        Debug.Log("--- PUBLIC PROPERTIES ---");
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties.OrderBy(p => p.Name))
        {
            if (prop.CanRead)
            {
                try
                {
                    var value = prop.GetValue(hdLightData);
                    Debug.Log($"{prop.Name} ({prop.PropertyType.Name}) = {value}");
                }
                catch (System.Exception e)
                {
                    Debug.Log($"{prop.Name} ({prop.PropertyType.Name}) = ERROR: {e.Message}");
                }
            }
            else
            {
                Debug.Log($"{prop.Name} ({prop.PropertyType.Name}) = (write-only)");
            }
        }

        Debug.Log("");
        Debug.Log("--- PUBLIC FIELDS ---");
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in fields.OrderBy(f => f.Name))
        {
            try
            {
                var value = field.GetValue(hdLightData);
                Debug.Log($"{field.Name} ({field.FieldType.Name}) = {value}");
            }
            catch (System.Exception e)
            {
                Debug.Log($"{field.Name} ({field.FieldType.Name}) = ERROR: {e.Message}");
            }
        }

        Debug.Log("=== END DUMP ===");
    }
    
    /// <summary>
    /// Comprehensive diagnostic dump of celestial-related properties.
    /// </summary>
    private void DumpEnumValues()
    {
        var hdLightData = selectedLight.GetComponent<MonoBehaviour>();
        if (hdLightData == null)
        {
            Debug.LogError("No HDAdditionalLightData component found!");
            return;
        }

        var type = hdLightData.GetType();
        
        Debug.Log("=== COMPREHENSIVE CELESTIAL DIAGNOSTICS ===");
        Debug.Log($"Light: {selectedLight.name}");
        Debug.Log($"Type: {type.FullName}");
        Debug.Log("");

        // Check for ALL properties containing celestial/sky/body keywords
        Debug.Log("--- SEARCHING FOR CELESTIAL-RELATED PROPERTIES ---");
        var allProperties = type.GetProperties(System.Reflection.BindingFlags.Public | 
                                               System.Reflection.BindingFlags.NonPublic | 
                                               System.Reflection.BindingFlags.Instance);
        
        string[] keywords = { "celestial", "sky", "body", "shading", "moon", "star", "sun", "physically" };
        
        bool foundAny = false;
        foreach (var prop in allProperties)
        {
            string propNameLower = prop.Name.ToLower();
            if (keywords.Any(keyword => propNameLower.Contains(keyword)))
            {
                foundAny = true;
                Debug.Log($"  ✓ {prop.Name} ({prop.PropertyType.Name})");
                
                if (prop.CanRead)
                {
                    try
                    {
                        var value = prop.GetValue(hdLightData);
                        
                        if (prop.PropertyType.IsEnum)
                        {
                            Debug.Log($"      Current: {value}");
                            Debug.Log($"      Possible values:");
                            foreach (var enumValue in System.Enum.GetValues(prop.PropertyType))
                            {
                                Debug.Log($"        - {enumValue} = {(int)enumValue}");
                            }
                        }
                        else
                        {
                            Debug.Log($"      Current Value: {value}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.Log($"      Error reading: {e.Message}");
                    }
                }
                else
                {
                    Debug.Log($"      (write-only)");
                }
                Debug.Log("");
            }
        }
        
        if (!foundAny)
        {
            Debug.LogWarning("No celestial-related properties found!");
        }

        // Also check fields (sometimes properties are backed by fields)
        Debug.Log("--- SEARCHING FOR CELESTIAL-RELATED FIELDS ---");
        var allFields = type.GetFields(System.Reflection.BindingFlags.Public | 
                                       System.Reflection.BindingFlags.NonPublic | 
                                       System.Reflection.BindingFlags.Instance);
        
        foundAny = false;
        foreach (var field in allFields)
        {
            string fieldNameLower = field.Name.ToLower();
            if (keywords.Any(keyword => fieldNameLower.Contains(keyword)))
            {
                foundAny = true;
                Debug.Log($"  ✓ {field.Name} ({field.FieldType.Name})");
                
                try
                {
                    var value = field.GetValue(hdLightData);
                    Debug.Log($"      Current Value: {value}");
                    
                    if (field.FieldType.IsEnum)
                    {
                        Debug.Log($"      Possible values:");
                        foreach (var enumValue in System.Enum.GetValues(field.FieldType))
                        {
                            Debug.Log($"        - {enumValue} = {(int)enumValue}");
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"      Error reading: {e.Message}");
                }
                Debug.Log("");
            }
        }
        
        if (!foundAny)
        {
            Debug.LogWarning("No celestial-related fields found!");
        }

        Debug.Log("=== END DIAGNOSTICS ===");
    }

}
