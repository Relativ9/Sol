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

        public void Initialize(
            IReadOnlyDictionary<string, NodeInstance> nodes, 
            VisualElement root, 
            VisualTreeAsset tooltipTemplate)
        {   
            _statsService = ServiceLocator.Get<IStatsService>();
            _progressionService = ServiceLocator.Get<IProgressionService>();
            _talentState = ServiceLocator.Get<ITalentStateService>();
            _treeGenerator = ServiceLocator.Get<ITalentTreeGenerator>();
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

        #region Input Binding
        
        void BindGlobalInput(VisualElement root)
        {
            root.RegisterCallback<MouseMoveEvent>(evt => 
            {
                _tooltipSystem?.UpdatePosition(evt.mousePosition);
            });
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
                element.RegisterCallback<MouseEnterEvent>(evt => ShowTooltip(data, evt.mousePosition));
                element.RegisterCallback<MouseLeaveEvent>(evt => HideTooltip());
            }
        }
        
        #endregion

        public string[] GetAllocatedNodeIds()
        {
            return _talentState.GetAllocatedNodeIds();
        }

        void OnNodeClicked(MouseDownEvent evt, string nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var nodeInstance)) return;
    
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
