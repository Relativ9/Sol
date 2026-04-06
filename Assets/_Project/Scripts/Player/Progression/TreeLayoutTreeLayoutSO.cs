using System.Collections.Generic;
using UnityEngine;

namespace Sol {
    
    [CreateAssetMenu(fileName = "NewTreeLayout", menuName = "Sol/Talent System/Tree Layout")]
    public class TreeLayoutSO : ScriptableObject
    {
        public string treeName;
        public Color treeColor;
    
        [Header("Nodes in this tree")]
        public List<TalentNodeDataSO> nodes;
    }
}