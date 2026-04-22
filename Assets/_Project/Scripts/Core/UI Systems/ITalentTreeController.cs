using System.Collections.Generic;
using Sol;
using UnityEngine.UIElements;
using UnityEngine;

namespace Sol
{
    public interface ITalentTreeController
    {
        void Initialize(
            IReadOnlyDictionary<string, NodeInstance> nodes, 
            VisualElement root, 
            VisualTreeAsset tooltipTemplate);
    
        string[] GetAllocatedNodeIds();
    }
}

