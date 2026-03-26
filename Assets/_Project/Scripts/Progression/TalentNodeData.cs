using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "NewTalentNode", menuName = "Talent System/Node Data")]
    public class TalentNodeData : ScriptableObject
    {
        public string nodeId;           // Unique ID like "HA_Sacred_Plate"
        public string displayName;      // "Sacred Plate"
        public Sprite icon;             // The icon image
        public int maxPoints;           // Usually 1-3 for most nodes.
        [TextArea(3, 10)]
        public string description;      // Tooltip text

        public bool isActiveSkill;
    
        // Gameplay effects
        private float armorBonus;
        private float damageBonus;
        // etc...
    
        [Header("Visual Placement")]
        public bool isHybrid;           // True if this is a synergy node
        public float tier;                // 0-12
        public float offset;              // -2 to 2
        public TreeLayout parentTree;   // Which tree owns this node
    
        [Header("Connections")]
        public List<TalentNodeData> prerequisites;  // Which nodes unlock this one
        //public bool requiresHybridParent; // Special: needs BOTH trees to unlock (for hybrids only)
    }

}
