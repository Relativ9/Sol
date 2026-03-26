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
        
        [Header("Circular Grid Settings")]
        [Tooltip("Radius of the center (tier 12) in pixels")]
        public float baseRadius = 540f;
        
        [Tooltip("Distance between tier rings (vertical spacing)")]
        public float tierSpacing = 70f;
        
        [Tooltip("Extra angular gap between trees (degrees)")]
        public float betweenTreeGap = 3f;
        
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
            if (uiDocument == null) return;
            wheelContainer = uiDocument.rootVisualElement?.Q<VisualElement>("wheel-container");
            if (wheelContainer == null) return;
            if (treeCollection == null) return;
            
            StartCoroutine(GenerateWhenReady());
        }
        
        IEnumerator GenerateWhenReady()
        {
            yield return null;
            nodeRegistry.Clear();
            wheelContainer.Clear();
            
            GenerateAllTrees();
            
            if (nodeRegistry.Count > 0)
            {
                DrawAllConnections();
                wheelContainer.style.left = Length.Percent(0f);
                wheelContainer.style.top = Length.Percent(80f);
            }
        }

        void GenerateAllTrees()
        {
            if (treeCollection?.trees == null || treeCollection.trees.Count == 0) return;
            
            // Calculate angular space per tree accounting for gaps
            int treeCount = treeCollection.trees.Count;
            float totalGapDegrees = treeCount * betweenTreeGap;
            float availableForTrees = 360f - totalGapDegrees;
            float anglePerTree = availableForTrees / treeCount;
            
            for (int i = 0; i < treeCount; i++)
            {
                if (treeCollection.trees[i] == null) continue;
                GenerateTree(treeCollection.trees[i], i, anglePerTree, totalGapDegrees);
            }
        }
        
        void GenerateTree(TreeLayout tree, int treeIndex, float treeAngularWidth, float totalGapDegrees)
        {
            if (tree?.nodes == null || tree.nodes.Count == 0) return;
            
            // Tree center angle accounts for gaps accumulated before this tree
            float gapBeforeThisTree = treeIndex * betweenTreeGap;
            float treeCenterAngle = -90f + (treeIndex * treeAngularWidth) + gapBeforeThisTree + (treeAngularWidth / 2f);
            
            Vector2 wheelCenter = GetWheelCenter();
            
            foreach (TalentNodeData nodeData in tree.nodes)
            {
                if (nodeData == null) continue;
                
                // Radius calculation: tier 12 is at baseRadius (center), tier 0 is outermost
                float maxRadius = baseRadius + (12 * tierSpacing);
                float radius = maxRadius - (nodeData.tier * tierSpacing);
                
                if (radius < 0) radius = 0;
                
                // KEY: Calculate angular step to maintain equal arc spacing (= tierSpacing)
                // Arc = radius × angle, so angle = tierSpacing / radius
                float angleStepRadians = tierSpacing / radius;
                float angleStepDegrees = angleStepRadians * Mathf.Rad2Deg;
                
                // Apply offset (now variable per tier to maintain grid spacing)
                float angle = treeCenterAngle + (nodeData.offset * angleStepDegrees);
                float rad = angle * Mathf.Deg2Rad;
                
                float x = wheelCenter.x + Mathf.Cos(rad) * radius;
                float y = wheelCenter.y + Mathf.Sin(rad) * radius;
                
                Vector2 pos = new Vector2(x, y);
                
                VisualElement nodeElement = CreateNodeVisual(nodeData, pos);
                
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
        
        VisualElement CreateNodeVisual(TalentNodeData data, Vector2 pos)
        {
            if (nodeTemplate == null) return null;
            
            var instance = nodeTemplate.Instantiate();
            var nodeRoot = instance.Q<VisualElement>("node-root");
            
            if (nodeRoot == null) return null;
            
            nodeRoot.style.position = Position.Absolute;
            nodeRoot.style.left = pos.x - 25f;
            nodeRoot.style.top = pos.y - 25f;
            
            var iconSlot = nodeRoot.Q<VisualElement>("icon-slot");
            if (iconSlot != null && data.icon != null)
            {
                iconSlot.style.backgroundImage = new StyleBackground(data.icon);
            }
            
            nodeRoot.userData = data;
            wheelContainer.Add(nodeRoot);
            return nodeRoot;
        }
        
        void DrawAllConnections()
        {
            foreach (var kvp in nodeRegistry)
            {
                var fromNode = kvp.Value;
                if (fromNode.data?.prerequisites == null) continue;
                
                foreach (var prereq in fromNode.data.prerequisites)
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
        
        public TalentNodeData GetNodeData(string nodeId)
        {
            if (nodeRegistry.TryGetValue(nodeId, out var instance))
                return instance.data;
            return null;
        }
    }
}
