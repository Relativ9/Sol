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

        void Start()
        {
            ServiceLocator.RegisterService<ITooltipSystem>(_tooltipSystem);
        }
        
        public void Initialize(IReadOnlyDictionary<string, TalentTreeGenerator.NodeInstance> nodes, VisualElement root, VisualTreeAsset tooltipTemplate)
        {   
            _statsService = ServiceLocator.Get<IStatsService>();
            _talentPointsLabel = root.Q<Label>("talent-points-label");
            _nodes = nodes;
            
            var tooltipRoot = root.Q<VisualElement>("tooltip-root");
            tooltipTemplate.CloneTree(tooltipRoot);
            _tooltipSystem = new TooltipSystem(tooltipRoot);

            // No CheckSpentPoints() - it was spending points on initialization
            UpdatePointsDisplay();
            
            root.RegisterCallback<MouseMoveEvent>(evt => 
            {
                if (_tooltipSystem != null)
                    _tooltipSystem.UpdatePosition(evt.mousePosition);
            });
            
            foreach (var kvp in nodes)
            {
                var nodeInstance = kvp.Value;
                var element = nodeInstance.Element;
                var data = nodeInstance.Data;
                
                string capturedNodeId = data.nodeId;
                
                if (data.maxPoints > 1) element.AddToClassList("multi-point");
                
                element.AddToClassList("talent-node");

                // MouseDownEvent matches what VirtualCursor synthesizes
                element.RegisterCallback<MouseDownEvent>(evt => OnNodeClicked(evt, capturedNodeId));
                
                // PointerEnter/Leave crash when synthesized - use MouseEnter/Leave
                element.RegisterCallback<MouseEnterEvent>(evt => ShowTooltip(data, evt.mousePosition));
                element.RegisterCallback<MouseLeaveEvent>(evt => HideTooltip());
                
                UpdateNodeVisual(element, data);
            }
        }

        void OnNodeClicked(MouseDownEvent evt, string nodeId)
        {
            if (!_nodes.TryGetValue(nodeId, out var nodeInstance)) return;
            var data = nodeInstance.Data;
            var element = nodeInstance.Element;
            
            if (evt.button == 0)
            {
                evt.StopPropagation();
                TryAllocate(nodeId, data, element);
            }
            else if (evt.button == 1) // button 1 = right click in MouseEvent
            {
                evt.StopPropagation();
                TryRemove(nodeId, data, element);
            }
        }
        
        void TryAllocate(string nodeId, TalentNodeDataSO data, VisualElement element)
        {
            if (data.allocatedPoints >= data.maxPoints) return;
            if (!_statsService.SpendTalentPoint()) return;
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
                    if (prereqInstance.Data.allocatedPoints > 0)
                        return true;
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
                    if (prereq.nodeId == parentNodeId) return true;
            }
            return false;
        }
        
        void RefreshDependents(string changedNodeId)
        {
            foreach (var kvp in _nodes)
            {
                if (kvp.Key == changedNodeId) continue;
                var dependentInstance = kvp.Value;
                var dependentData = dependentInstance.Data;
                if (HasPrerequisite(dependentData, changedNodeId))
                    UpdateNodeVisual(dependentInstance.Element, dependentData);
            }
        }
        
        bool HasPrerequisite(TalentNodeDataSO data, string prerequisiteId)
        {
            if (data.prerequisites == null) return false;
            foreach (var prereq in data.prerequisites)
                if (prereq.nodeId == prerequisiteId) return true;
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
        
        void ShowTooltip(TalentNodeDataSO data, Vector2 position)
        {
            _tooltipSystem.Show(new TalentNodeTooltipAdapter(data), position);
        }
        
        void HideTooltip() => _tooltipSystem?.Hide();
        
        void UpdatePointsDisplay()
        {
            if (_talentPointsLabel == null) return;
            int available = _statsService.GetAvailableTalentPoints();
            int total = _statsService.GetTotalTalentPoints();
            _talentPointsLabel.text = $"Points: {available}/{total}";
            _talentPointsLabel.style.color = available > 0
                ? new StyleColor(new Color(1f, 0.73f, 0.41f))
                : new StyleColor(new Color(0.8f, 0.2f, 0.2f));
        }
        
        [ContextMenu("Reset All Talents")]
        public void ResetAllTalents()
        {
            if (_nodes == null)
            {
                Debug.LogWarning("[TalentTree] Cannot reset - not initialized yet");
                return;
            }

            int refundCount = 0;
            foreach (var kvp in _nodes)
            {
                var data = kvp.Value.Data;
                refundCount += data.allocatedPoints;
                data.allocatedPoints = 0;
            }

            for (int i = 0; i < refundCount; i++)
                _statsService.RefundTalentPoint();

            foreach (var kvp in _nodes)
                UpdateNodeVisual(kvp.Value.Element, kvp.Value.Data);

            UpdatePointsDisplay();
            Debug.Log($"[TalentTree] Reset complete. Refunded {refundCount} points.");
        }
    }
}
