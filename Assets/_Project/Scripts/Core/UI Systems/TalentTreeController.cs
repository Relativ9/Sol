using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    public class TalentTreeController : MonoBehaviour, ITalentTreeController
    {
        private IStatsService _statsService;
        private IProgressionService _progressionService;
        private ITalentStateService _talentState;
        private ITalentTreeGenerator _treeGenerator;
        private ITooltipSystem _tooltipSystem;
        
        private IReadOnlyDictionary<string, NodeInstance> _nodes;
        private Label _talentPointsLabel;
        private VisualElement _root;
        private IVirtualCursor _cursor;

        public void Initialize(
            IReadOnlyDictionary<string, NodeInstance> nodes, 
            VisualElement root, 
            VisualTreeAsset tooltipTemplate)
        {   
            _statsService = ServiceLocator.Get<IStatsService>();
            _progressionService = ServiceLocator.Get<IProgressionService>();
            _talentState = ServiceLocator.Get<ITalentStateService>();
            _treeGenerator = ServiceLocator.Get<ITalentTreeGenerator>();
            _cursor = ServiceLocator.TryGet<IVirtualCursor>();
            _nodes = nodes;
            _root = root;
            
            _talentPointsLabel = root.Q<Label>("talent-points-label");
            
            // Setup tooltip locally. Do NOT register transient systems to ServiceLocator.
            var tooltipRoot = root.Q<VisualElement>("tooltip-root");
            if (tooltipRoot != null && tooltipTemplate != null)
            {
                tooltipTemplate.CloneTree(tooltipRoot);
                _tooltipSystem = new TooltipSystem(tooltipRoot);
            }

            BindGlobalInput(root);
            BindNodeInteractions();
            
            UpdatePointsDisplay();
            RefreshAllVisuals();
        }

        // The controller is destroyed when the Talents tab closes. Unsubscribe so the generator
        // doesn't call into a dead controller on the next focus change.
        void OnDestroy()
        {
            if (_treeGenerator != null)
                _treeGenerator.FocusChanged -= UpdateTreeTitle;
        }

        // Mouse wheel scrolls the wheel one tier per notch while the talent tab is open.
        // Scroll is inverted relative to the raw input (scroll up = outer tiers, matching the intended feel).
        void Update()
        {
            if (_treeGenerator == null || _cursor == null) return;

            int notches = _cursor.GetScrollNotches();
            if (notches != 0)
                _treeGenerator.ScrollTiers(-notches);
        }

        #region Input Binding
        
        void BindGlobalInput(VisualElement root)
        {
            root.RegisterCallback<MouseMoveEvent>(evt => 
            {
                _tooltipSystem?.UpdatePosition(evt.mousePosition);
            });

            // Arrows change the focused tree. The wheel is rotated, not the buttons.
            root.Q<Button>("talent-prev-tree")?.RegisterCallback<ClickEvent>(_ => _treeGenerator.FocusTree(_treeGenerator.FocusedTreeIndex - 1));
            root.Q<Button>("talent-next-tree")?.RegisterCallback<ClickEvent>(_ => _treeGenerator.FocusTree(_treeGenerator.FocusedTreeIndex + 1));

            // Scroll is polled in Update, not a WheelEvent: the virtual cursor never generates wheel events,
            // so UI Toolkit never sees the scroll. The cursor reads the mouse and exposes it instead.
            _treeGenerator.FocusChanged += UpdateTreeTitle;
            UpdateTreeTitle();
            ApplyVignette(root);
        }

        // Fade-to-black vignette: transparent in the middle, black at the edges. Visual only, no picking.
        // The gradient is generated at runtime, so no texture asset is needed.
        static Texture2D _vignetteTexture;

        void ApplyVignette(VisualElement root)
        {
            var vignette = root.Q<VisualElement>("talent-vignette");
            if (vignette == null) return;

            if (_vignetteTexture == null)
                _vignetteTexture = BuildVignetteTexture(256);

            vignette.style.backgroundImage = new StyleBackground(_vignetteTexture);
            vignette.style.unityBackgroundScaleMode = ScaleMode.StretchToFill;
        }

        static Texture2D BuildVignetteTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color[size * size];
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance from the centre, normalised so the edge midpoint is 1.
                    float dx = (x - half) / half;
                    float dy = (y - half) / half;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / Mathf.Sqrt(2f);

                    // Clear in the centre, then fade to fully black by 75% of the way to the edge.
                    // Lower start and end values pull the vignette in and make it stronger toward the centre.
                    float alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.75f, d));
                    pixels[y * size + x] = new Color(0f, 0f, 0f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        // Outline takes the node's tree colour when it is available or active, so each tree reads as its own.
        // Locked nodes keep the grey from the stylesheet, so their state still reads at a glance.
        void ApplyTreeOutline(NodeInstance nodeInstance, VisualElement element, bool locked)
        {
            if (locked)
            {
                element.style.borderTopColor = StyleKeyword.Null;
                element.style.borderRightColor = StyleKeyword.Null;
                element.style.borderBottomColor = StyleKeyword.Null;
                element.style.borderLeftColor = StyleKeyword.Null;
                return;
            }

            Color c = TreeOutlineColor(nodeInstance.TreeIndex);
            element.style.borderTopColor = c;
            element.style.borderRightColor = c;
            element.style.borderBottomColor = c;
            element.style.borderLeftColor = c;
        }

        // Same colour that names the tree in the header, forced opaque.
        Color TreeOutlineColor(int treeIndex)
        {
            Color c = _treeGenerator.TreeColor(treeIndex);
            return new Color(c.r, c.g, c.b, 1f);
        }

        // Title shows the focused tree's name and colour.
        void UpdateTreeTitle()
        {
            var title = _root?.Q<Label>("talent-tree-title");
            if (title == null) return;

            int index = _treeGenerator.FocusedTreeIndex;
            title.text = _treeGenerator.TreeName(index);

            // Tree colours are authored with alpha 0, which made the banner invisible. Keep the RGB
            // so each tree still has its colour, and force full opacity for the text.
            Color c = _treeGenerator.TreeColor(index);
            title.style.color = new Color(c.r, c.g, c.b, 1f);
        }
        
        void BindNodeInteractions()
        {
            foreach (var kvp in _nodes)
            {
                var nodeInstance = kvp.Value;
                var element = nodeInstance.Element;
                var data = nodeInstance.Data;
                
                string capturedNodeId = data.nodeId;
                
                if (data.maxPoints > 1) 
                    element.AddToClassList("multi-point");
                
                element.AddToClassList("talent-node");

                element.RegisterCallback<MouseDownEvent>(evt => OnNodeClicked(evt, capturedNodeId));
                element.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    // Dimmed nodes (other trees) get no tooltip. Hybrid nodes are always shown.
                    if (IsNodeInteractive(nodeInstance)) ShowTooltip(data, evt.mousePosition);
                });
                element.RegisterCallback<MouseLeaveEvent>(evt => HideTooltip());
            }
        }
        
        #endregion

        public string[] GetAllocatedNodeIds()
        {
            return _talentState.GetAllocatedNodeIds();
        }

        // A node is interactive if it belongs to the focused tree, or it is a hybrid node (which spans trees).
        // Dimmed nodes from other trees get no tooltip and cannot be allocated.
        bool IsNodeInteractive(NodeInstance nodeInstance)
        {
            if (nodeInstance.Data != null && nodeInstance.Data.isHybrid) return true;
            return nodeInstance.TreeIndex == _treeGenerator.FocusedTreeIndex;
        }

        void OnNodeClicked(MouseDownEvent evt, string nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var nodeInstance)) return;
            if (!IsNodeInteractive(nodeInstance)) return;
    
            if (evt.button == 0)
            {
                evt.StopPropagation();
                TryAllocate(nodeId, nodeInstance);
            }
            else if (evt.button == 1)
            {
                evt.StopPropagation();
                TryRemove(nodeId, nodeInstance);
            }
        }
        
        void TryAllocate(string nodeId, NodeInstance nodeInstance)
        {
            var data = nodeInstance.Data;
            int currentPoints = _talentState.GetAllocatedPoints(nodeId);
            
            if (currentPoints >= data.maxPoints) return;
            if (!PrerequisitesMet(data)) return;
            
            if (!_progressionService.SpendTalentPoint()) return;
            
            if (_talentState.TryAllocatePoint(nodeId))
            {
                // StateService raised GameEvent for persistent listeners (SaveManager, etc.)
                // We refresh visuals immediately for responsiveness.
                UpdateNodeVisual(nodeId, nodeInstance);
                RefreshDependents(nodeId);
                UpdatePointsDisplay();
            }
            else
            {
                _progressionService.RefundTalentPoint();
                UpdatePointsDisplay();
            }
        }
        
        void TryRemove(string nodeId, NodeInstance nodeInstance)
        {
            if (_talentState.GetAllocatedPoints(nodeId) <= 0) return;
            if (HasAllocatedDependent(nodeId)) return;
            
            if (_talentState.TryRemovePoint(nodeId))
            {
                _progressionService.RefundTalentPoint();
                UpdateNodeVisual(nodeId, nodeInstance);
                RefreshDependents(nodeId);
                UpdatePointsDisplay();
            }
        }
        
        bool PrerequisitesMet(TalentNodeDataSO data)
        {
            if (data.prerequisites == null || data.prerequisites.Count == 0)
                return true;
            
            foreach (var prereq in data.prerequisites)
            {
                if (_talentState.HasAllocatedPoints(prereq.nodeId))
                    return true;
            }
            return false;
        }
        
        bool HasAllocatedDependent(string parentNodeId)
        {
            foreach (var nodeId in _talentState.GetAllocatedNodeIds())
            {
                if (nodeId == parentNodeId) continue;
                if (_talentState.GetAllocatedPoints(nodeId) <= 0) continue;
                
                var nodeData = _treeGenerator.GetNodeData(nodeId);
                if (nodeData?.prerequisites == null) continue;
                
                foreach (var prereq in nodeData.prerequisites)
                    if (prereq.nodeId == parentNodeId) return true;
            }
            return false;
        }
        
        void RefreshDependents(string changedNodeId)
        {
            foreach (var kvp in _nodes)
            {
                if (kvp.Key == changedNodeId) continue;
                
                var dependentData = kvp.Value.Data;
                if (HasPrerequisite(dependentData, changedNodeId))
                    UpdateNodeVisual(kvp.Key, kvp.Value);
            }
        }
        
        bool HasPrerequisite(TalentNodeDataSO data, string prerequisiteId)
        {
            if (data.prerequisites == null) return false;
            foreach (var prereq in data.prerequisites)
                if (prereq.nodeId == prerequisiteId) return true;
            return false;
        }
        
        void UpdateNodeVisual(string nodeId, NodeInstance nodeInstance)
        {
            var data = nodeInstance.Data;
            var element = nodeInstance.Element;
            int allocated = _talentState.GetAllocatedPoints(nodeId);
            
            bool atMax = allocated >= data.maxPoints;
            bool canAllocate = !atMax && PrerequisitesMet(data);
            
            element.RemoveFromClassList("locked");
            element.RemoveFromClassList("available");
            element.RemoveFromClassList("active");

            if (allocated > 0)
            {
                element.AddToClassList("active");
            }
            else if (!canAllocate)
            {
                element.AddToClassList("locked");
            }
            else
            {
                element.AddToClassList("available");
            }

            ApplyTreeOutline(nodeInstance, element, locked: !canAllocate && allocated == 0);
            
            if (data.maxPoints > 1)
            {
                var counter = element.Q<Label>("point-counter");
                if (counter != null)
                    counter.text = $"{allocated}/{data.maxPoints}";
            }
        }
        
        void RefreshAllVisuals()
        {
            foreach (var kvp in _nodes)
            {
                UpdateNodeVisual(kvp.Key, kvp.Value);
            }
        }
        
        void ShowTooltip(TalentNodeDataSO data, Vector2 position)
        {
            _tooltipSystem.Show(new TalentNodeTooltipAdapter(data), position);
        }
        
        void HideTooltip() => _tooltipSystem?.Hide();
        
        void UpdatePointsDisplay()
        {
            if (_talentPointsLabel == null) return;
            int available = _progressionService.GetAvailableTalentPoints();
            int total = _progressionService.GetTotalTalentPoints();
            _talentPointsLabel.text = $"Points: {available}/{total}";
            _talentPointsLabel.style.color = available > 0
                ? new StyleColor(new Color(1f, 0.73f, 0.41f))
                : new StyleColor(new Color(0.8f, 0.2f, 0.2f));
        }
        
        [ContextMenu("Reset All Talents")]
        public void ResetAllTalents()
        {
            if (_nodes == null || _talentState == null)
            {
                Debug.LogWarning("[TalentTree] Cannot reset - not initialized yet");
                return;
            }

            int refundCount = 0;
            foreach (var nodeId in _talentState.GetAllocatedNodeIds())
            {
                refundCount += _talentState.GetAllocatedPoints(nodeId);
            }

            _talentState.ResetAll();

            for (int i = 0; i < refundCount; i++)
                _progressionService.RefundTalentPoint();

            RefreshAllVisuals();
            UpdatePointsDisplay();
            
            Debug.Log($"[TalentTree] Reset complete. Refunded {refundCount} points.");
        }
    }
}
