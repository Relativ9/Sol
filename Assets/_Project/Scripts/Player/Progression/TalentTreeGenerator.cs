using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    public class TalentTreeGenerator : ITalentTreeGenerator
    {
        private readonly UIDocument _uiDocument;
        private readonly VisualTreeAsset _nodeTemplate;
        private readonly TreeLayoutCollectionSO _treeCollectionSO;
        private readonly TalentTreeSettingsSO _settingsSO;
        private const float BASE_NODE_SIZE = 50f;
        private const int MAX_TIER = 12;
        private VisualElement _wheelContainer;
        private Dictionary<string, NodeInstance> _nodeRegistry = new();
        public class NodeInstance
        {
            public VisualElement Element;
            public Vector2 Position;
            public TalentNodeDataSO Data;
        }
        public TalentTreeGenerator(UIDocument uiDocument, VisualTreeAsset nodeTemplate,
                                   TreeLayoutCollectionSO treeCollectionSo, TalentTreeSettingsSO settingsSo)
        {
            _uiDocument = uiDocument;
            _nodeTemplate = nodeTemplate;
            _treeCollectionSO = treeCollectionSo;
            _settingsSO = settingsSo;
        }
        public void Generate()
        {
            if (_uiDocument == null)
            {
                Debug.LogError("[RadialTreeGenerator] UI Document is null!");
                return;
            }
            _wheelContainer = _uiDocument.rootVisualElement?.Q<VisualElement>("wheel-container");
            
            if (_wheelContainer == null)
            {
                Debug.LogError("[RadialTreeGenerator] wheel-container not found!");
                return;
            }
            if (_treeCollectionSO == null)
            {
                Debug.LogError("[RadialTreeGenerator] Tree Collection is null!");
                return;
            }
            // Clear and regenerate
            _nodeRegistry.Clear();
            _wheelContainer.Clear();
            ValidateSettings();
            GenerateAllTrees();
            if (_nodeRegistry.Count > 0)
            {
                DrawAllConnections();
                _wheelContainer.style.left = Length.Percent(0f);
                _wheelContainer.style.top = Length.Percent(100f);
            }
        }
        void ValidateSettings()
        {
            if (_treeCollectionSO?.trees == null) return;
            int treeCount = _treeCollectionSO.trees.Count;
            float sectionAngle = 360f / treeCount;
            if (_settingsSO.treeAngularWidth >= sectionAngle)
            {
                Debug.LogWarning($"treeAngularWidth ({_settingsSO.treeAngularWidth}°) >= " +
                    $"section size ({sectionAngle:F1}°). Trees will overlap.");
            }
            int nodeColumns = (_settingsSO.maxOffset * 2) + 1;
            float nodeSize = BASE_NODE_SIZE * _settingsSO.nodeScale;
            float innerArcLength = _settingsSO.innerRadius * (_settingsSO.treeAngularWidth * Mathf.Deg2Rad);
            float requiredArc = nodeColumns * nodeSize;
            if (innerArcLength < requiredArc)
            {
                float minRadius = requiredArc / (_settingsSO.treeAngularWidth * Mathf.Deg2Rad);
                Debug.LogWarning($"Inner radius ({_settingsSO.innerRadius}) too small. " +
                    $"Recommended minimum: {minRadius:F0}");
            }
            if (_settingsSO.innerRadius >= _settingsSO.outerRadius)
            {
                Debug.LogError($"innerRadius ({_settingsSO.innerRadius}) must be less than " +
                    $"outerRadius ({_settingsSO.outerRadius})!");
            }
        }
        void GenerateAllTrees()
        {
            if (_treeCollectionSO?.trees == null || _treeCollectionSO.trees.Count == 0)
            {
                Debug.LogError("[RadialTreeGenerator] No trees in collection!");
                return;
            }
            int treeCount = _treeCollectionSO.trees.Count;
            float sectionAngle = 360f / treeCount;
            float gapAngle = sectionAngle - _settingsSO.treeAngularWidth;
            float rotationOffsetDegrees = _settingsSO.rotationSteps * sectionAngle;
            for (int i = 0; i < treeCount; i++)
            {
                if (_treeCollectionSO.trees[i] == null) continue;
                GenerateTree(_treeCollectionSO.trees[i], i, sectionAngle, gapAngle, rotationOffsetDegrees);
            }
            Debug.Log($"[RadialTreeGenerator] Generated {_nodeRegistry.Count} nodes across {treeCount} trees.");
        }
        void GenerateTree(TreeLayoutSO tree, int treeIndex, float sectionAngle, 
                         float gapAngle, float rotationOffsetDegrees)
        {
            if (tree?.nodes == null || tree.nodes.Count == 0) return;
            Vector2 wheelCenter = GetWheelCenter();
            float sectionStartAngle = -76f + rotationOffsetDegrees + (treeIndex * sectionAngle);
            float treeCenterAngle = sectionStartAngle + (gapAngle / 2f) + (_settingsSO.treeAngularWidth / 2f);
            float offsetStepDegrees = (_settingsSO.maxOffset > 0) 
                ? _settingsSO.treeAngularWidth / (_settingsSO.maxOffset * 2f) 
                : 0f;
            
            float tierSpacing = (_settingsSO.outerRadius - _settingsSO.innerRadius) / MAX_TIER;
            foreach (TalentNodeDataSO nodeData in tree.nodes)
            {
                if (nodeData == null) continue;
                float radius = _settingsSO.outerRadius - (nodeData.tier * tierSpacing);
                radius = Mathf.Max(radius, 0f);
                float clampedOffset = Mathf.Clamp(nodeData.offset, -_settingsSO.maxOffset, _settingsSO.maxOffset);
                float angle = treeCenterAngle + (clampedOffset * offsetStepDegrees);
                float rad = angle * Mathf.Deg2Rad;
                float x = wheelCenter.x + Mathf.Cos(rad) * radius;
                float y = wheelCenter.y + Mathf.Sin(rad) * radius;
                Vector2 pos = new Vector2(x, y);
                VisualElement nodeElement = CreateNodeVisual(nodeData, pos);
                if (!string.IsNullOrEmpty(nodeData.nodeId))
                {
                    _nodeRegistry[nodeData.nodeId] = new NodeInstance
                    {
                        Element = nodeElement,
                        Position = pos,
                        Data = nodeData
                    };
                }
            }
        }
        VisualElement CreateNodeVisual(TalentNodeDataSO data, Vector2 pos)
        {
            if (_nodeTemplate == null) return null;
            var instance = _nodeTemplate.Instantiate();
            var nodeRoot = instance.Q<VisualElement>("node-root");
            if (nodeRoot == null)
            {
                Debug.LogError("[RadialTreeGenerator] node-root not found in template!");
                return null;
            }
            float nodeSize = BASE_NODE_SIZE * _settingsSO.nodeScale;
            float halfSize = nodeSize / 2f;
            nodeRoot.style.position = Position.Absolute;
            nodeRoot.style.left = pos.x - halfSize;
            nodeRoot.style.top = pos.y - halfSize;
            nodeRoot.style.width = nodeSize;
            nodeRoot.style.height = nodeSize;
            var iconSlot = nodeRoot.Q<VisualElement>("icon-slot");
            if (iconSlot != null && data.icon != null)
            {
                iconSlot.style.backgroundImage = new StyleBackground(data.icon);
                iconSlot.style.width = Length.Percent(100);
                iconSlot.style.height = Length.Percent(100);
            }
            nodeRoot.userData = data;
            _wheelContainer.Add(nodeRoot);
            return nodeRoot;
        }
        
        void DrawAllConnections()
        {
            foreach (var kvp in _nodeRegistry)
            {
                NodeInstance fromNode = kvp.Value;
                if (fromNode.Data?.prerequisites == null) continue;
                foreach (TalentNodeDataSO prereq in fromNode.Data.prerequisites)
                {
                    if (prereq == null) continue;
                    if (_nodeRegistry.TryGetValue(prereq.nodeId, out var toNode))
                    {
                        DrawConnection(fromNode.Position, toNode.Position);
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
            line.style.backgroundColor = Color.gray; // <-- ADD THIS BACK
            line.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50), 0);
            line.style.rotate = new Rotate(angle);
            _wheelContainer.Insert(0, line);
        }
        
        Vector2 GetWheelCenter()
        {
            return new Vector2(
                _wheelContainer.resolvedStyle.width / 2f,
                _wheelContainer.resolvedStyle.height / 2f
            );
        }
        public TalentNodeDataSO GetNodeData(string nodeId)
        {
            if (_nodeRegistry.TryGetValue(nodeId, out var instance))
                return instance.Data;
            return null;
        }
        public IReadOnlyDictionary<string, NodeInstance> GetNodeRegistry() => _nodeRegistry;
    }
}


