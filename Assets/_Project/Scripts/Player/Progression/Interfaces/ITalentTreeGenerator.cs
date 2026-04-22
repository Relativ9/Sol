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
    }
}

