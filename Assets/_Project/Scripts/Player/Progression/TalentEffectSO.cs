using UnityEngine;

namespace Sol
{
    public abstract class TalentEffectSO : ScriptableObject
    {
        public abstract void Apply(ITalentEffectContext context, string nodeId, int allocatedPoints);
        public abstract void Remove(ITalentEffectContext context, string nodeId);
    }
}
