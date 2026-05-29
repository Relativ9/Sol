using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;

namespace Sol
{
    public class TalentStateService : ITalentStateService
    {
        private readonly ITalentTreeGenerator _treeGenerator;
        private readonly GameEvent _talentStateChangedEvent; // Your SO GameEvent type
        private IStatsService _statsService; //injected after construction
        
        private Dictionary<string, int> _allocatedPoints = new();
        

        // Dependencies injected - testable, no hidden ServiceLocator calls
        public TalentStateService(ITalentTreeGenerator treeGenerator, GameEvent stateChangedEvent)
        {
            _treeGenerator = treeGenerator;
            _talentStateChangedEvent = stateChangedEvent;
        }
        
        
        // Called by GameInitializer with save data
        public void Initialize(PlayerSaveData saveData, IStatsService statsService)
        {
            _statsService = statsService;
            _allocatedPoints.Clear();
            
            if (saveData?.talentAllocations != null)
            {
                foreach (var entry in saveData.talentAllocations)
                {
                    if (entry.allocatedPoints > 0)
                        _allocatedPoints[entry.nodeId] = entry.allocatedPoints;
                }
            } 
            
            ValidateLoadedData();
        }
        
        public int GetTotalAllocatedPoints() => _allocatedPoints.Values.Sum();
        
        public int GetAllocatedPoints(string nodeId) => 
            _allocatedPoints.GetValueOrDefault(nodeId);
        
        public string[] GetAllocatedNodeIds() => 
            _allocatedPoints.Keys.ToArray();
        
        public bool HasAllocatedPoints(string nodeId) => 
            GetAllocatedPoints(nodeId) > 0;
        
        public bool TryAllocatePoint(string nodeId)
        {
            var data = _treeGenerator.GetNodeData(nodeId);
            if (data == null) return false;
            
            int current = GetAllocatedPoints(nodeId);
            if (current >= data.maxPoints) return false;
            if (!PrerequisitesMet(data)) return false;
            
            IncrementPointAllocation(nodeId);
            RaiseStateChanged();
            return true;
        }
        
        public bool TryRemovePoint(string nodeId)
        {
            var data = _treeGenerator.GetNodeData(nodeId);
            if (data == null) return false;
            
            int current = GetAllocatedPoints(nodeId);
            if (current <= 0) return false;
            if (HasAllocatedDependent(nodeId)) return false;
            
            DecrementPointAllocation(nodeId);
            RaiseStateChanged();
            return true;
        }
        
        public void ResetAll()
        {
            if (_statsService != null)
            {
                foreach (var nodeId in _allocatedPoints.Keys.ToArray())
                {
                    _statsService.RemoveModifiersFromSource(nodeId);
                }
            }
            _allocatedPoints.Clear();
            RaiseStateChanged();
        }
        
        // Private helpers
        void IncrementPointAllocation(string nodeId)
        {
            _allocatedPoints[nodeId] = _allocatedPoints.GetValueOrDefault(nodeId) + 1;
        }
        
        void DecrementPointAllocation(string nodeId)
        {
            int current = _allocatedPoints[nodeId];
            if (current <= 1)
                _allocatedPoints.Remove(nodeId);
            else
                _allocatedPoints[nodeId] = current - 1;
        }
        
        void RaiseStateChanged()
        {
            if (_talentStateChangedEvent == null)
            {
                Debug.Log("[TalentStateService] GameEvent is NULL! Assign it in TalentTreeGenerator inspector.");
                return;
            }
            Debug.Log("[TalentStateService] Raising GameEvent: " + _talentStateChangedEvent.name);
            _talentStateChangedEvent.Raise();
        }
        
        bool PrerequisitesMet(TalentNodeDataSO data)
        {
            if (data.prerequisites == null || data.prerequisites.Count == 0)
                return true;
            
            // ANY prerequisite met (adjust to ALL if your design requires)
            foreach (var prereq in data.prerequisites)
            {
                if (HasAllocatedPoints(prereq.nodeId))
                    return true;
            }
            return false;
        }
        
        bool HasAllocatedDependent(string parentNodeId)
        {
            foreach (var nodeId in _allocatedPoints.Keys)
            {
                if (_allocatedPoints[nodeId] <= 0) continue;
                
                var nodeData = _treeGenerator.GetNodeData(nodeId);
                if (nodeData?.prerequisites == null) continue;
                
                foreach (var prereq in nodeData.prerequisites)
                    if (prereq.nodeId == parentNodeId) return true;
            }
            return false;
        }
        
        void ValidateLoadedData()
        {
            // Remove any allocations that violate prerequisites 
            // (handles save file corruption or design changes)
            var invalidNodes = new List<string>();
            
            foreach (var nodeId in _allocatedPoints.Keys.ToArray())
            {
                var data = _treeGenerator.GetNodeData(nodeId);
                if (data == null || !PrerequisitesMet(data))
                {
                    invalidNodes.Add(nodeId);
                }
            }
            
            foreach (var invalid in invalidNodes)
            {
                Debug.LogWarning($"[TalentStateService] Removing invalid allocation: {invalid}");
                _allocatedPoints.Remove(invalid);
            }
        }
    }
}
