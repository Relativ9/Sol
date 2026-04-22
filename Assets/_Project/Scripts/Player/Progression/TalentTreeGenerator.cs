using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    // EXTRACTED: Moved to namespace level so ITalentTreeGenerator can reference it
    public class NodeInstance
    {
        public VisualElement Element;
        public Vector2 Position;
        public TalentNodeDataSO Data;
    }
    
    public class TalentTreeGenerator : MonoBehaviour, ITalentTreeGenerator
    {
        [Header("Configuration")]
        [SerializeField] private TreeLayoutCollectionSO _treeCollectionSO;
        [SerializeField] private TalentTreeSettingsSO _settingsSO;
        [SerializeField] private GameEvent _talentStateChangedEvent; // For StateService injection
        
        // Runtime state
        private Dictionary<string, NodeInstance> _nodeRegistry = new();
        private bool _dataLoaded = false;
        
        // Constants
        private const float BASE_NODE_SIZE = 50f;
        private const int MAX_TIER = 12;
        
        private void Awake()
        {
            // Self-registration
            ServiceLocator.RegisterService<ITalentTreeGenerator>(this);
            
            // Factory: Create and register the plain C# state service
            // StateService needs us for validation, and the GameEvent for state change notifications
            var stateService = new TalentStateService(this, _talentStateChangedEvent);
            ServiceLocator.RegisterService<ITalentStateService>(stateService);
        }

        // Called by GameInitializer in Start() - calculates positions, no UI
        public void LoadData()
        {
            if (_dataLoaded) return;
            
            if (_treeCollectionSO?.trees == null)
            {
                Debug.LogError("[TalentTreeGenerator] Tree Collection is null!");
                return;
            }
            
            ValidateSettings();
            CalculateAllNodePositions();
            
            _dataLoaded = true;
            Debug.Log($"[TalentTreeGenerator] Loaded {_nodeRegistry.Count} nodes.");
        }

        // Called by UI Manager when panel opens - creates VisualElements
        public void Generate(VisualElement root, VisualTreeAsset nodeTemplate)
        {
            if (!_dataLoaded)
            {
                Debug.LogError("[TalentTreeGenerator] LoadData() must be called before Generate()!");
                return;
            }
            
            var wheelContainer = root.Q<VisualElement>("wheel-container");
            if (wheelContainer == null)
            {
                Debug.LogError("[TalentTreeGenerator] wheel-container not found!");
                return;
            }
            
            // Clear previous UI if any
            wheelContainer.Clear();
            
            // Create VisualElements for cached nodes
            foreach (var kvp in _nodeRegistry)
            {
                var instance = kvp.Value;
                instance.Element = CreateNodeVisual(instance.Data, instance.Position, wheelContainer, nodeTemplate);
            }
            
            // Draw connections now that elements exist
            DrawAllConnections(wheelContainer);
            
            // Position container
            if (_nodeRegistry.Count > 0)
            {
                wheelContainer.style.left = Length.Percent(0f);
                wheelContainer.style.top = Length.Percent(100f);
            }
        }
        
        // Clears UI but keeps data (called when panel closes)
        public void ClearUI()
        {
            foreach (var kvp in _nodeRegistry)
            {
                kvp.Value.Element = null; // Release UI reference
            }
        }

        #region Data Loading (No UI)
        
        void CalculateAllNodePositions()
        {
            if (_treeCollectionSO?.trees == null || _treeCollectionSO.trees.Count == 0)
            {
                Debug.LogError("[TalentTreeGenerator] No trees in collection!");
                return;
            }
            
            int treeCount = _treeCollectionSO.trees.Count;
            float sectionAngle = 360f / treeCount;
            float gapAngle = sectionAngle - _settingsSO.treeAngularWidth;
            float rotationOffsetDegrees = _settingsSO.rotationSteps * sectionAngle;
            
            for (int i = 0; i < treeCount; i++)
            {
                if (_treeCollectionSO.trees[i] == null) continue;
                CalculateTreePositions(_treeCollectionSO.trees[i], i, sectionAngle, gapAngle, rotationOffsetDegrees);
            }
        }
        
        void CalculateTreePositions(TreeLayoutSO tree, int treeIndex, float sectionAngle, 
                                    float gapAngle, float rotationOffsetDegrees)
        {
            if (tree?.nodes == null || tree.nodes.Count == 0) return;
            
            // Calculate center (will use actual container size during Generate)
            Vector2 wheelCenter = new Vector2(500, 500); // Placeholder, updated during Generate
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
                
                if (!string.IsNullOrEmpty(nodeData.nodeId))
                {
                    _nodeRegistry[nodeData.nodeId] = new NodeInstance
                    {
                        Position = pos,
                        Data = nodeData,
                        Element = null // Created during Generate()
                    };
                }
            }
        }
        
        #endregion

        #region UI Generation
        
        VisualElement CreateNodeVisual(TalentNodeDataSO data, Vector2 pos, VisualElement container, VisualTreeAsset template)
        {
            if (template == null) return null;
            
            var instance = template.Instantiate();
            var nodeRoot = instance.Q<VisualElement>("node-root");
            
            if (nodeRoot == null)
            {
                Debug.LogError("[TalentTreeGenerator] node-root not found in template!");
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
            container.Add(nodeRoot);
            
            return nodeRoot;
        }
        
        void DrawAllConnections(VisualElement container)
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
                        DrawConnection(fromNode.Position, toNode.Position, container);
                    }
                }
            }
        }
        
        void DrawConnection(Vector2 nodeA, Vector2 nodeB, VisualElement container)
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
            line.style.backgroundColor = Color.gray;
            line.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50), 0);
            line.style.rotate = new Rotate(angle);
            
            container.Insert(0, line);
        }
        
        #endregion

        #region Validation
        
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
        
        #endregion

        #region ITalentTreeGenerator Interface
        
        public IReadOnlyDictionary<string, NodeInstance> GetNodeRegistry() => _nodeRegistry;
        
        public TalentNodeDataSO GetNodeData(string nodeId)
        {
            if (_nodeRegistry.TryGetValue(nodeId, out var instance))
                return instance.Data;
            return null;
        }
        
        #endregion
    }
}
