using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sol
{
    public interface ITalentTreeGenerator
    {
        void Generate(VisualElement root, VisualTreeAsset nodeTemplate);
        IReadOnlyDictionary<string, NodeInstance> GetNodeRegistry();
        TalentNodeDataSO GetNodeData(string nodeId);
        void ClearUI();
        void LoadData();

        // Focus navigation (one tree in front at a time, mouse wheel scrolls tiers)
        int TreeCount { get; }
        int FocusedTreeIndex { get; }
        string TreeName(int index);
        UnityEngine.Color TreeColor(int index);
        void FocusTree(int index, bool animate = true);
        void ScrollTiers(int delta);
        event System.Action FocusChanged;
    }
}

