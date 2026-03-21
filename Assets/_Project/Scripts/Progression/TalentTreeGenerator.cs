using UnityEngine;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{

public class TalentTreeGenerator : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;  
    [SerializeField] private VisualTreeAsset nodeTemplate;  // Your TalentNode.uxml
    [SerializeField] private int nodeCount = 12;  // Start with 12 for visibility
    [SerializeField] private float radius = 300f; // Distance from center
    
    void Start()
    {
        GenerateRadialNodes();
    }
    
    void GenerateRadialNodes()
    {
        // Get the root and find our container
        var root = uiDocument.rootVisualElement;
        var wheel = root.Q<VisualElement>("wheel-container");
        
        if (wheel == null)
        {
            Debug.LogError("Could not find 'wheel-container'!");
            return;
        }
        
        Debug.Log($"Found wheel container! Child count: {wheel.childCount}");
        
        // Center of the wheel (container is 800x800)
        float centerX = 400f;
        float centerY = 400f;
        
        // Generate nodes in a circle
        for (int i = 0; i < nodeCount; i++)
        {
            // Calculate angle: 0°, 30°, 60°... for 12 nodes
            float angleDegrees = (360f / nodeCount) * i;
            
            // Convert to radians (Cos/Sin use radians)
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            
            // POLAR TO CARTESIAN
            // x = cos(angle) * radius
            // y = sin(angle) * radius
            float offsetX = Mathf.Cos(angleRadians) * radius;
            float offsetY = Mathf.Sin(angleRadians) * radius;
            
            // Instantiate the template
            var instance = nodeTemplate.Instantiate();
            var nodeElement = instance.Q<VisualElement>("node-root");
            
            // Position: center + offset - half node size (to center the node itself)
            // Your node is 50x50, so subtract 25 to center it on the point
            nodeElement.style.position = Position.Absolute;
            nodeElement.style.left = centerX + offsetX - 25f;
            nodeElement.style.top = centerY + offsetY - 25f;
            
            wheel.Add(nodeElement);
        }
    }
}

}
