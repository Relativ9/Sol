// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UIElements;
//
// namespace Sol
// {
//     public class RadialTreeGenerator : MonoBehaviour
//     {
//         [Header("References")]
//         [SerializeField] private UIDocument uiDocument;
//         [SerializeField] private VisualTreeAsset nodeTemplate;
//         [SerializeField] private TreeLayoutCollection treeCollection;
//
//         [Header("Wheel Dimensions")]
//         [Tooltip("Radius of the outermost ring (Tier 0) in pixels")]
//         public float outerRadius = 1200f;
//
//         [Tooltip("Radius of the innermost ring (Tier 12) in pixels")]
//         public float innerRadius = 400f;
//
//         [Header("Tree Layout")]
//         [Tooltip("Angular width of each tree in degrees. Remainder is split evenly as gaps between trees.")]
//         [Range(1f, 30f)]
//         public float treeAngularWidth = 20f;
//
//         [Tooltip("Maximum offset magnitude. Nodes can sit at -max to +max columns.")]
//         [Range(1, 4)]
//         public int maxOffset = 2;
//
//         [Header("Wheel Rotation")]
//         [Tooltip("Rotates the entire wheel by N tree-steps clockwise. Step 0 = first tree in collection at top.")]
//         [Range(0, 13)]
//         public int rotationSteps = 0;
//
//         [Header("Node Visuals")]
//         [Tooltip("Scale of node visuals only - does not affect position or spacing.")]
//         [Range(0.25f, 2f)]
//         public float nodeScale = 1f;
//
//         [Header("Live Tuning")]
//         [Tooltip("Regenerates the wheel when any value changes in Play Mode.")]
//         public bool liveTuningMode = false;
//
//         private const float BASE_NODE_SIZE = 50f;
//         private const int MAX_TIER = 12;
//
//         private VisualElement wheelContainer;
//         private Dictionary<string, NodeInstance> nodeRegistry = new();
//
//         private float _cachedOuterRadius;
//         private float _cachedInnerRadius;
//         private float _cachedTreeAngularWidth;
//         private int _cachedMaxOffset;
//         private float _cachedNodeScale;
//         private int _cachedRotationSteps;
//
//         // public GameEvent<T> _onGenerationComplete;
//
//         private class NodeInstance
//         {
//             public VisualElement element;
//             public Vector2 position;
//             public TalentNodeData data;
//         }
//
//         [SerializeField] private Color inactiveColor = Color.grey;
//         [SerializeField] private Color activeColor = Color.aquamarine;
//
//         void Start()
//         {
//             if (uiDocument == null)
//             {
//                 Debug.LogError("UI Document not assigned!");
//                 return;
//             }
//
//             wheelContainer = uiDocument.rootVisualElement?.Q<VisualElement>("wheel-container");
//
//             if (wheelContainer == null)
//             {
//                 Debug.LogError("wheel-container not found in UXML!");
//                 return;
//             }
//
//             if (treeCollection == null)
//             {
//                 Debug.LogError("Tree Collection not assigned!");
//                 return;
//             }
//
//             CacheCurrentValues();
//             //StartCoroutine(GenerateWhenReady());
//         }
//
//         void Update()
//         {
//             if (!liveTuningMode) return;
//             if (!Application.isPlaying) return;
//             if (wheelContainer == null) return;
//
//             if (ValuesChanged())
//             {
//                 CacheCurrentValues();
//                 StartCoroutine(GenerateWhenReady());
//             }
//         }
//
//         bool ValuesChanged()
//         {
//             return !Mathf.Approximately(outerRadius, _cachedOuterRadius)
//                 || !Mathf.Approximately(innerRadius, _cachedInnerRadius)
//                 || !Mathf.Approximately(treeAngularWidth, _cachedTreeAngularWidth)
//                 || maxOffset != _cachedMaxOffset
//                 || !Mathf.Approximately(nodeScale, _cachedNodeScale)
//                 || rotationSteps != _cachedRotationSteps;
//         }
//
//         void CacheCurrentValues()
//         {
//             _cachedOuterRadius = outerRadius;
//             _cachedInnerRadius = innerRadius;
//             _cachedTreeAngularWidth = treeAngularWidth;
//             _cachedMaxOffset = maxOffset;
//             _cachedNodeScale = nodeScale;
//             _cachedRotationSteps = rotationSteps;
//         }
//
//         IEnumerator GenerateWhenReady()
//         {
//             yield return null;
//
//             nodeRegistry.Clear();
//             wheelContainer.Clear();
//
//             ValidateSettings();
//             GenerateAllTrees();
//
//             if (nodeRegistry.Count > 0)
//             {
//                 DrawAllConnections();
//                 wheelContainer.style.left = Length.Percent(0f);
//                 wheelContainer.style.top = Length.Percent(80f);
//             }
//         }
//         
//         public void Generate()
//         {
//             nodeRegistry.Clear();
//             wheelContainer.Clear();
//             CacheCurrentValues();
//             StartCoroutine(GenerateWhenReady());
//         }
//
//         void ValidateSettings()
//         {
//             if (treeCollection?.trees == null) return;
//
//             int treeCount = treeCollection.trees.Count;
//             float sectionAngle = 360f / treeCount;
//
//             if (treeAngularWidth >= sectionAngle)
//             {
//                 Debug.LogWarning($"treeAngularWidth ({treeAngularWidth}°) >= section size ({sectionAngle:F1}°). " +
//                                  $"Trees will overlap. Max recommended: {sectionAngle - 1f:F1}°");
//             }
//
//             int nodeColumns = (maxOffset * 2) + 1;
//             float nodeSize = BASE_NODE_SIZE * nodeScale;
//             float innerArcLength = innerRadius * (treeAngularWidth * Mathf.Deg2Rad);
//             float requiredArc = nodeColumns * nodeSize;
//
//             if (innerArcLength < requiredArc)
//             {
//                 float minRadius = requiredArc / (treeAngularWidth * Mathf.Deg2Rad);
//                 Debug.LogWarning($"Inner radius ({innerRadius}) may be too small for {nodeColumns} nodes " +
//                                  $"at inner tier. Recommended minimum: {minRadius:F0}");
//             }
//
//             if (innerRadius >= outerRadius)
//             {
//                 Debug.LogError($"innerRadius ({innerRadius}) must be less than outerRadius ({outerRadius})!");
//             }
//         }
//
//         void GenerateAllTrees()
//         {
//             if (treeCollection?.trees == null || treeCollection.trees.Count == 0)
//             {
//                 Debug.LogError("No trees in collection!");
//                 return;
//             }
//
//             int treeCount = treeCollection.trees.Count;
//             float sectionAngle = 360f / treeCount;
//             float gapAngle = sectionAngle - treeAngularWidth;
//             float rotationOffsetDegrees = rotationSteps * sectionAngle;
//
//             for (int i = 0; i < treeCount; i++)
//             {
//                 if (treeCollection.trees[i] == null) continue;
//                 GenerateTree(treeCollection.trees[i], i, sectionAngle, gapAngle, rotationOffsetDegrees);
//             }
//
//             Debug.Log($"Generated {nodeRegistry.Count} nodes across {treeCount} trees. " +
//                       $"Section: {sectionAngle:F1}°, Gap: {gapAngle:F1}°, Rotation: {rotationOffsetDegrees:F1}°");
//         }
//
//         void GenerateTree(TreeLayout tree, int treeIndex, float sectionAngle, float gapAngle, float rotationOffsetDegrees)
//         {
//             if (tree?.nodes == null || tree.nodes.Count == 0) return;
//
//             Vector2 wheelCenter = GetWheelCenter();
//
//             float sectionStartAngle = -90f + rotationOffsetDegrees + (treeIndex * sectionAngle);
//             float treeCenterAngle = sectionStartAngle + (gapAngle / 2f) + (treeAngularWidth / 2f);
//
//             float offsetStepDegrees = (maxOffset > 0) ? treeAngularWidth / (maxOffset * 2f) : 0f;
//             float tierSpacing = (outerRadius - innerRadius) / MAX_TIER;
//
//             foreach (TalentNodeData nodeData in tree.nodes)
//             {
//                 if (nodeData == null) continue;
//
//                 float radius = outerRadius - (nodeData.tier * tierSpacing);
//                 radius = Mathf.Max(radius, 0f);
//
//                 float clampedOffset = Mathf.Clamp(nodeData.offset, -maxOffset, maxOffset);
//                 float angle = treeCenterAngle + (clampedOffset * offsetStepDegrees);
//                 float rad = angle * Mathf.Deg2Rad;
//
//                 float x = wheelCenter.x + Mathf.Cos(rad) * radius;
//                 float y = wheelCenter.y + Mathf.Sin(rad) * radius;
//
//                 Vector2 pos = new Vector2(x, y);
//
//                 VisualElement nodeElement = CreateNodeVisual(nodeData, pos);
//
//                 if (!string.IsNullOrEmpty(nodeData.nodeId))
//                 {
//                     nodeRegistry[nodeData.nodeId] = new NodeInstance
//                     {
//                         element = nodeElement,
//                         position = pos,
//                         data = nodeData
//                     };
//                 }
//             }
//         }
//
//         VisualElement CreateNodeVisual(TalentNodeData data, Vector2 pos)
//         {
//             if (nodeTemplate == null) return null;
//
//             var instance = nodeTemplate.Instantiate();
//             var nodeRoot = instance.Q<VisualElement>("node-root");
//
//             if (nodeRoot == null)
//             {
//                 Debug.LogError("node-root not found in template!");
//                 return null;
//             }
//
//             float nodeSize = BASE_NODE_SIZE * nodeScale;
//             float halfSize = nodeSize / 2f;
//
//             nodeRoot.style.position = Position.Absolute;
//             nodeRoot.style.left = pos.x - halfSize;
//             nodeRoot.style.top = pos.y - halfSize;
//             nodeRoot.style.width = nodeSize;
//             nodeRoot.style.height = nodeSize;
//
//             var iconSlot = nodeRoot.Q<VisualElement>("icon-slot");
//             if (iconSlot != null && data.icon != null)
//             {
//                 iconSlot.style.backgroundImage = new StyleBackground(data.icon);
//                 iconSlot.style.width = Length.Percent(100);
//                 iconSlot.style.height = Length.Percent(100);
//             }
//
//             nodeRoot.userData = data;
//             wheelContainer.Add(nodeRoot);
//             return nodeRoot;
//         }
//
//         void DrawAllConnections()
//         {
//             foreach (var kvp in nodeRegistry)
//             {
//                 NodeInstance fromNode = kvp.Value;
//                 if (fromNode.data?.prerequisites == null) continue;
//
//                 foreach (TalentNodeData prereq in fromNode.data.prerequisites)
//                 {
//                     if (prereq == null) continue;
//
//                     if (nodeRegistry.TryGetValue(prereq.nodeId, out var toNode))
//                     {
//                         DrawConnection(fromNode.position, toNode.position);
//                     }
//                 }
//             }
//         }
//
//         void DrawConnection(Vector2 nodeA, Vector2 nodeB)
//         {
//             var line = new VisualElement();
//
//             float dx = nodeB.x - nodeA.x;
//             float dy = nodeB.y - nodeA.y;
//             float distance = Mathf.Sqrt(dx * dx + dy * dy);
//             float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
//
//             line.style.position = Position.Absolute;
//             line.style.left = nodeA.x;
//             line.style.top = nodeA.y;
//             line.style.width = distance;
//             line.style.height = 2f;
//             line.style.transformOrigin = new TransformOrigin(Length.Percent(0), Length.Percent(50), 0);
//             line.style.rotate = new Rotate(angle);
//             line.style.backgroundColor = inactiveColor;
//
//             wheelContainer.Insert(0, line);
//         }
//
//         Vector2 GetWheelCenter()
//         {
//             return new Vector2(
//                 wheelContainer.resolvedStyle.width / 2f,
//                 wheelContainer.resolvedStyle.height / 2f
//             );
//         }
//
//         public TalentNodeData GetNodeData(string nodeId)
//         {
//             if (nodeRegistry.TryGetValue(nodeId, out var instance))
//                 return instance.data;
//             return null;
//         }
//     }
// }
