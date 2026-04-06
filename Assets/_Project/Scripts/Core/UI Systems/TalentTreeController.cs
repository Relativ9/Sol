using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    public class TalentTreeController : MonoBehaviour, ITalentTreeController
    {
        private IStatsService _statsService;
        private ITooltipSystem _tooltipSystem;
        private IReadOnlyDictionary<string, TalentTreeGenerator.NodeInstance> _nodes;
        
        private Label _talentPointsLabel;
        
        private void Awake()
        {
            // var tooltipRoot = root.Q<VisualElement>("tooltip-root");
            // _tooltipSystem = new TooltipSystem(tooltipRoot);

        }

        void Start()
        {
            ServiceLocator.RegisterService<ITooltipSystem>(_tooltipSystem);
        }
        
        public void Initialize(IReadOnlyDictionary<string, TalentTreeGenerator.NodeInstance> nodes, VisualElement root)
        {   
            _statsService = ServiceLocator.Get<IStatsService>();
            _talentPointsLabel = root.Q<Label>("talent-points-label");
            _nodes = nodes; // CRITICAL: Store the reference

            CheckSpentPoints();
            UpdatePointsDisplay();
            
            foreach (var kvp in nodes)
            {
                var nodeInstance = kvp.Value;
                var element = nodeInstance.Element;
                var data = nodeInstance.Data;
                
                string capturedNodeId = data.nodeId; // Capture for lambda
                
                if (data.maxPoints > 1) element.AddToClassList("multi-point");
                
                element.AddToClassList("talent-node");
                element.RegisterCallback<ClickEvent>(evt => OnNodeClicked(evt, capturedNodeId));
                
                // Optional: Tooltip handlers
                element.RegisterCallback<MouseEnterEvent>(evt => ShowTooltip(data));
                element.RegisterCallback<MouseLeaveEvent>(evt => HideTooltip());
                
                UpdateNodeVisual(element, data);
            }
        }

        void CheckSpentPoints()
        {
            int alreadySpent = 0;
            foreach (var kvp in _nodes)
                alreadySpent += kvp.Value.Data.allocatedPoints;
            for (int i = 0; i < alreadySpent; i++)
                _statsService.SpendTalentPoint();
        }
        
        void OnNodeClicked(ClickEvent evt, string nodeId)
        {
            evt.StopPropagation();
            
            if (!_nodes.TryGetValue(nodeId, out var nodeInstance)) return;
            var data = nodeInstance.Data;
            var element = nodeInstance.Element;
            
            if (evt.button == 0) // Left click
            {
                TryAllocate(nodeId, data, element);
            }
            else if (evt.button == 1) // Right click
            {
                TryRemove(nodeId, data, element);
            }
        }
        
        void TryAllocate(string nodeId, TalentNodeDataSO data, VisualElement element)
        {
            if (data.allocatedPoints >= data.maxPoints) return;
            if (!_statsService.SpendTalentPoint()) return;
            // Check prerequisites
            bool canAllocate = CalculateCanAllocate(data);
            if (!canAllocate) return;
            
            data.allocatedPoints++;
            UpdateNodeVisual(element, data);
            RefreshDependents(nodeId);
            UpdatePointsDisplay();
            
            Debug.Log($"Allocated point to {data.displayName}: {data.allocatedPoints}/{data.maxPoints}");
        }
        
        void TryRemove(string nodeId, TalentNodeDataSO data, VisualElement element)
        {
            if (data.allocatedPoints <= 0) return;
            
            if (HasAllocatedDependent(nodeId))
            {
                Debug.Log($"Cannot remove {data.displayName}: dependent nodes are allocated");
                return;
            }
            
            data.allocatedPoints--;
            _statsService.RefundTalentPoint();
            UpdateNodeVisual(element, data);
            RefreshDependents(nodeId);
            UpdatePointsDisplay();
            
            Debug.Log($"Removed point from {data.displayName}: {data.allocatedPoints}/{data.maxPoints}");
        }
        
        bool CalculateCanAllocate(TalentNodeDataSO data)
        {
            if (data.prerequisites == null || data.prerequisites.Count == 0)
                return true;
            
            foreach (var prereq in data.prerequisites)
            {
                if (_nodes.TryGetValue(prereq.nodeId, out var prereqInstance))
                {
                    if (prereqInstance.Data.allocatedPoints > 0)
                        return true; // ANY prerequisite met (change to All for hybrids)
                }
            }
            return false;
        }
        
        bool HasAllocatedDependent(string parentNodeId)
        {
            foreach (var kvp in _nodes)
            {
                var dependentData = kvp.Value.Data;
                
                if (dependentData.allocatedPoints <= 0) continue;
                if (dependentData.prerequisites == null) continue;
                
                foreach (var prereq in dependentData.prerequisites)
                {
                    if (prereq.nodeId == parentNodeId)
                        return true;
                }
            }
            return false;
        }
        
        void RefreshDependents(string changedNodeId)
        {
            foreach (var kvp in _nodes)
            {
                if (kvp.Key == changedNodeId) continue; // Skip self
                
                var dependentInstance = kvp.Value;
                var dependentData = dependentInstance.Data;
                
                // Check if this node has the changed node as prerequisite
                if (HasPrerequisite(dependentData, changedNodeId))
                {
                    UpdateNodeVisual(dependentInstance.Element, dependentData);
                }
            }
        }
        
        bool HasPrerequisite(TalentNodeDataSO data, string prerequisiteId)
        {
            if (data.prerequisites == null) return false;
            
            foreach (var prereq in data.prerequisites)
            {
                if (prereq.nodeId == prerequisiteId) return true;
            }
            return false;
        }
        
        void UpdateNodeVisual(VisualElement element, TalentNodeDataSO data)
        {
            bool canAllocate = CalculateCanAllocate(data);
            
            element.RemoveFromClassList("locked");
            element.RemoveFromClassList("available");
            element.RemoveFromClassList("active");

            if (data.allocatedPoints > 0)
            {
                element.AddToClassList("active");
                Debug.Log($"[Visual] {data.nodeId} -> active, classes: {string.Join(", ", element.GetClasses())}");
            }
            else if (!canAllocate)
            {
                element.AddToClassList("locked");
                Debug.Log($"[Visual] {data.nodeId} -> locked, classes: {string.Join(", ", element.GetClasses())}");
            }
            else
            {
                element.AddToClassList("available");
                Debug.Log($"[Visual] {data.nodeId} -> available, classes: {string.Join(", ", element.GetClasses())}");
            }
            
            if (data.maxPoints > 1)
            {
                var counter = element.Q<Label>("point-counter");
                if (counter != null)
                    counter.text = $"{data.allocatedPoints}/{data.maxPoints}";
            }
        }
        
        void ShowTooltip(TalentNodeDataSO data)
        {
            // Stub - implement with your adapter
            //_tooltipSystem.Show(new TalentNodeTooltipAdapter(data));
        }
        
        void HideTooltip()
        {
            _tooltipSystem?.Hide();
        }
        
        void UpdatePointsDisplay()
        {
            if (_talentPointsLabel == null) return;
            int available = _statsService.GetAvailableTalentPoints();
            int total = _statsService.GetTotalTalentPoints();
            _talentPointsLabel.text = $"Points: {available}/{total}";
            _talentPointsLabel.style.color = available > 0
                ? new StyleColor(new Color(1f, 0.73f, 0.41f))   // Orange
                : new StyleColor(new Color(0.8f, 0.2f, 0.2f));  // Red
        }
        
        [ContextMenu("Reset All Talents")]
        public void ResetAllTalents()
        {
            if (_nodes == null)
            {
                Debug.LogWarning("[TalentTree] Cannot reset - not initialized yet");
                return;
            }

            // Count how many points we're refunding
            int refundCount = 0;

            foreach (var kvp in _nodes)
            {
                var data = kvp.Value.Data;
                refundCount += data.allocatedPoints;
                data.allocatedPoints = 0;
            }

            // Refund all points at once
            for (int i = 0; i < refundCount; i++)
                _statsService.RefundTalentPoint();

            // Refresh all visuals
            foreach (var kvp in _nodes)
                UpdateNodeVisual(kvp.Value.Element, kvp.Value.Data);

            UpdatePointsDisplay();

            Debug.Log($"[TalentTree] Reset complete. Refunded {refundCount} points.");
        }
    }
}

