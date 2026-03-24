using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    public class RadialTreeGenerator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private VisualTreeAsset nodeTemplate;
        [SerializeField] private TreeLayoutCollection treeCollection;
        
        [Header("Layout Settings")]
        public float centerRadius = 540f;    // Changed from 40f to 70f to match your working inspector values
        public float tierSpacing = 70f;      // Was 40f - should be 70f
        public float offsetAngle = 5f;       // 5f is correct
        
        [Header("Scaling")]
        [SerializeField][Range(0.5f, 5f)] private float globalScale = 1.5f;
        [SerializeField] private bool autoFitToScreen = false;
        
        private VisualElement wheelContainer;
        private Dictionary<string, NodeInstance> nodeRegistry = new();
        
        private class NodeInstance
        {
            public VisualElement element;
            public Vector2 position;
            public TalentNodeData data;
        }
        
        [SerializeField] private Color inactiveColor = Color.grey;
        [SerializeField] private Color activeColor = Color.aquamarine;

        void Start()
        {
            if (uiDocument == null)
            {
                Debug.LogError("UI Document not assigned!");
                return;
            }
            
            wheelContainer = uiDocument.rootVisualElement?.Q<VisualElement>("wheel-container");
            
            if (wheelContainer == null)
            {
                Debug.LogError("wheel-container not found in UXML!");
                return;
            }
            
            if (treeCollection == null)
            {
                Debug.LogError("Tree Collection not assigned!");
                return;
            }
            
            StartCoroutine(GenerateWhenReady());
        }
        
        IEnumerator GenerateWhenReady()
        {
            yield return null;
            
            if (autoFitToScreen)
            {
                CalculateOptimalScale();
            }
            
            nodeRegistry.Clear();
            wheelContainer.Clear();
            
            GenerateAllTrees();
            GenerateHybridNodes();
            
            if (nodeRegistry.Count > 0)
            {
                DrawAllConnections();
                wheelContainer.style.left = Length.Percent(0f);
                wheelContainer.style.top = Length.Percent(80f);
            }
        }

        void GenerateAllTrees()
        {
            if (treeCollection?.trees == null || treeCollection.trees.Count == 0)
            {
                Debug.LogError("No tree data!");
                return;
            }
            
            float anglePerTree = 360f / treeCollection.trees.Count;
            
            for (int i = 0; i < treeCollection.trees.Count; i++)
            {
                if (treeCollection.trees[i] == null) continue;
                GenerateTree(treeCollection.trees[i], i, anglePerTree);
            }
            
            Debug.Log($"Generated {nodeRegistry.Count} nodes");
        }
        
        void GenerateTree(TreeLayout tree, int treeIndex, float anglePerTree)
        {
            if (tree?.nodes == null || tree.nodes.Count == 0) return;
            
            float treeCenterAngle = -90f + (treeIndex * anglePerTree);
            Vector2 wheelCenter = GetWheelCenter();
            
            foreach (TalentNodeData nodeData in tree.nodes)
            {
                if (nodeData == null) continue;
                
                // Skip hybrid nodes - they get placed separately
                if (nodeData.isHybrid) continue;
                
                // Calculate position
                float maxRadius = centerRadius + (12 * tierSpacing);
                float baseRadius = maxRadius - (nodeData.tier * tierSpacing);
                float radius = baseRadius * globalScale;
                
                float angle = treeCenterAngle + (nodeData.offset * offsetAngle);
                float rad = angle * Mathf.Deg2Rad;
                
                float x = wheelCenter.x + Mathf.Cos(rad) * radius;
                float y = wheelCenter.y + Mathf.Sin(rad) * radius;
                
                Vector2 pos = new Vector2(x, y);
                
                // Create the visual node - passing treeColor for potential future use
                VisualElement nodeElement = CreateNodeVisual(nodeData, pos, tree.treeColor);
                
                // Register with the SO reference
                if (!string.IsNullOrEmpty(nodeData.nodeId))
                {
                    nodeRegistry[nodeData.nodeId] = new NodeInstance
                    {
                        element = nodeElement,
                        position = pos,
                        data = nodeData
                    };
                }
            }
        }
        
        void GenerateHybridNodes()
        {
            int treeCount = treeCollection.trees.Count;
            float anglePerTree = 360f / treeCount;
            Vector2 wheelCenter = GetWheelCenter();
            
            for (int i = 0; i < treeCount; i++)
            {
                int nextIndex = (i + 1) % treeCount;
                TreeLayout treeA = treeCollection.trees[i];
                TreeLayout treeB = treeCollection.trees[nextIndex];
                
                foreach (var node in treeA.nodes)
                {
                    if (node?.isHybrid == true)
                    {
                        // Check if this hybrid belongs between these two trees
                        // You can check node.parentTree or a specific hybrid pairing field
                        PlaceHybridNode(node, i, nextIndex, anglePerTree, wheelCenter);
                    }
                }
            }
        }
        
        void PlaceHybridNode(TalentNodeData hybridNode, int treeAIndex, int treeBIndex, 
                            float anglePerTree, Vector2 wheelCenter)
        {
            float angleA = -90f + (treeAIndex * anglePerTree);
            float angleB = -90f + (treeBIndex * anglePerTree);
            float boundaryAngle = angleA + (anglePerTree / 2f);
            
            float maxRadius = centerRadius + (12 * tierSpacing);
            float baseRadius = maxRadius - (hybridNode.tier * tierSpacing);
            float radius = baseRadius * globalScale;
            
            float rad = boundaryAngle * Mathf.Deg2Rad;
            float x = wheelCenter.x + Mathf.Cos(rad) * radius;
            float y = wheelCenter.y + Mathf.Sin(rad) * radius;
            
            Vector2 pos = new Vector2(x, y);
            
            // Create node (no special styling since no USS)
            VisualElement nodeElement = CreateNodeVisual(hybridNode, pos, Color.white);
            
            if (!string.IsNullOrEmpty(hybridNode.nodeId))
            {
                nodeRegistry[hybridNode.nodeId] = new NodeInstance
                {
                    element = nodeElement,
                    position = pos,
                    data = hybridNode
                };
            }
        }
        
        VisualElement CreateNodeVisual(TalentNodeData data, Vector2 pos, Color treeColor)
        {
            if (nodeTemplate == null) return null;
            
            var instance = nodeTemplate.Instantiate();
            var nodeRoot = instance.Q<VisualElement>("node-root");
            
            if (nodeRoot == null)
            {
                Debug.LogError("node-root not found in template!");
                return null;
            }
            
            // Position
            nodeRoot.style.position = Position.Absolute;
            nodeRoot.style.left = pos.x - 25f;
            nodeRoot.style.top = pos.y - 25f;
            
            // Set icon
            var iconSlot = nodeRoot.Q<VisualElement>("icon-slot");
            if (iconSlot != null && data.icon != null)
            {
                iconSlot.style.backgroundImage = new StyleBackground(data.icon);
            }
            
            // Store data reference for runtime access
            nodeRoot.userData = data;
            
            wheelContainer.Add(nodeRoot);
            return nodeRoot;
        }
        
        void DrawAllConnections()
        {
            foreach (var kvp in nodeRegistry)
            {
                NodeInstance fromNode = kvp.Value;
                if (fromNode.data?.prerequisites == null) continue;
                
                foreach (TalentNodeData prereq in fromNode.data.prerequisites)
                {
                    if (prereq == null) continue;
                    
                    if (nodeRegistry.TryGetValue(prereq.nodeId, out var toNode))
                    {
                        DrawConnection(fromNode.position, toNode.position);
                    }
                }
            }
        }
        
        void DrawConnection(Vector2 nodeA, Vector2 nodeB)
        {
            var line = new VisualElement();
            
            float dx = nodeB.x - nodeA.x;
            float dy = nodeB.y - nodeA.y;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
            
            line.style.position = Position.Absolute;
            line.style.left = nodeA.x;
            line.style.top = nodeA.y;
            line.style.width = distance;
            line.style.height = 2f;
            line.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50), 0);
            line.style.rotate = new Rotate(angle);
            line.style.backgroundColor = inactiveColor;
            
            wheelContainer.Insert(0, line);
        }
        
        Vector2 GetWheelCenter()
        {
            return new Vector2(
                wheelContainer.resolvedStyle.width / 2f,
                wheelContainer.resolvedStyle.height / 2f
            );
        }
        
        void CalculateOptimalScale()
        {
            float wheelSize = Mathf.Min(wheelContainer.resolvedStyle.width, 
                                        wheelContainer.resolvedStyle.height);
            float maxBaseRadius = centerRadius + (12 * tierSpacing);
            globalScale = (wheelSize * 0.4f) / maxBaseRadius;
        }
        
        public TalentNodeData GetNodeData(string nodeId)
        {
            if (nodeRegistry.TryGetValue(nodeId, out var instance))
            {
                return instance.data;
            }
            return null;
        }
        
        public void Rescale(float newScale)
        {
            globalScale = newScale;
            StartCoroutine(GenerateWhenReady());
        }
    }
}
