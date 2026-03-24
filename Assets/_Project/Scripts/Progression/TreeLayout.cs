using System.Collections.Generic;
using UnityEngine;

namespace Sol {
    
    [CreateAssetMenu(fileName = "NewTreeLayout", menuName = "Talent System/Tree Layout")]
    public class TreeLayout : ScriptableObject
    {
        public string treeName;
        public Color treeColor;
    
        [Header("Nodes in this tree")]
        public List<TalentNodeData> nodes;
    }
}