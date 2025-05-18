using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sol
{
    [ExecuteAlways]
public class TalentConnectionLines : MonoBehaviour
{
    [Tooltip("The origin point from which all paths will start")]
    public Transform originPoint;
    
    [Tooltip("The destination points to which paths will be drawn")]
    public List<Transform> destinationPoints = new List<Transform>();
    
    [Tooltip("The width of the lines")]
    public float lineWidth = 0.1f;
    
    [Tooltip("The color of the lines")]
    public Color lineColor = Color.white;
    
    [Tooltip("The material to use for the lines")]
    public Material lineMaterial;
    
    // List to store all the line renderers
    private List<LineRenderer> lineRenderers = new List<LineRenderer>();
    
    // Flag to track if lines have been generated
    private bool linesGenerated = false;

    private void Update()
    {
        // Only update positions if lines have been generated
        if (linesGenerated)
        {
            UpdateLinePositions();
        }
    }

    // Method to create the line renderers
    public void GenerateLines()
    {
        // Clear existing line renderers
        ClearLines();
        
        // Create a new line renderer for each destination
        for (int i = 0; i < destinationPoints.Count; i++)
        {
            if (destinationPoints[i] == null) continue;
            
            // Create a new GameObject for this line
            GameObject lineObj = new GameObject("Line_" + i);
            lineObj.transform.SetParent(transform);
            
            // Add a LineRenderer component
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            
            // Configure the LineRenderer
            lr.positionCount = 2; // Just two points: origin and destination
            lr.startWidth = lineWidth;
            lr.endWidth = lineWidth;
            lr.material = lineMaterial;
            lr.startColor = lineColor;
            lr.endColor = lineColor;
            lr.useWorldSpace = true; // Use world space coordinates
            
            // Add to our list
            lineRenderers.Add(lr);
        }
        
        // Update positions
        UpdateLinePositions();
        linesGenerated = true;
    }
    
    // Method to clear all line renderers
    public void ClearLines()
    {
        foreach (LineRenderer lr in lineRenderers)
        {
            if (lr != null)
            {
                DestroyImmediate(lr.gameObject);
            }
        }
        lineRenderers.Clear();
        linesGenerated = false;
    }

    private void UpdateLinePositions()
    {
        if (originPoint == null) return;
        
        // Update each line's positions
        for (int i = 0; i < lineRenderers.Count; i++)
        {
            if (i >= destinationPoints.Count || destinationPoints[i] == null) continue;
            
            LineRenderer lr = lineRenderers[i];
            if (lr != null)
            {
                // Set the positions
                lr.SetPosition(0, originPoint.position);
                lr.SetPosition(1, destinationPoints[i].position);
            }
        }
    }
}

#if UNITY_EDITOR
// Custom editor to add buttons to the inspector
[CustomEditor(typeof(TalentConnectionLines))]
public class ForkingPathRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();
        
        // Get the target script
        TalentConnectionLines pathRenderer = (TalentConnectionLines)target;
        
        // Add space for better UI
        EditorGUILayout.Space();
        
        // Add a button to generate lines
        if (GUILayout.Button("Generate Lines"))
        {
            pathRenderer.GenerateLines();
        }
        
        // Add a button to clear lines
        if (GUILayout.Button("Clear Lines"))
        {
            pathRenderer.ClearLines();
        }
    }
}
#endif
}

