using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(fileName = "Talent Tree Settings", menuName = "Sol/Talent System/Talent Tree Settings")]
    public class TalentTreeSettingsSO : ScriptableObject
    {
        [Header("Wheel Dimensions")]
        public float outerRadius = 1200f;
        public float innerRadius = 400f;

        [Header("Tree Layout")]
        public float treeAngularWidth = 20f;
        public int maxOffset = 2;

        [Header("Wheel Rotation")]
        public int rotationSteps = 0;

        [Header("Node Visuals")]
        public float nodeScale = 1f;
    }
}

