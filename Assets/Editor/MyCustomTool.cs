using UnityEngine;
using UnityEditor;

public class MyCustomTool : EditorWindow
{
    [MenuItem("Tools/My Custom Tool")]
    public static void ShowWindow()
    {
        GetWindow<MyCustomTool>("My Custom Tool");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Welcome to My Custom Tool!");
        if (GUILayout.Button("Run Tool"))
        {
            // Place your tool logic here
            Debug.Log("Tool ran!");
        }
    }
}
