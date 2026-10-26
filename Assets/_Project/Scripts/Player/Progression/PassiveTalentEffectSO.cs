using UnityEngine;

namespace Sol
{
    // Stub: conditional/reactive effects (e.g. "2 health per 10 armor") are owned by DerivedStatResolver (Phase 6).
    [CreateAssetMenu(menuName = "Sol/Talent System/Effects/Passive (Stub)")]
    public class PassiveTalentEffectSO : TalentEffectSO
    {
        public override void Apply(ITalentEffectContext ctx, string nodeId, int pts)
            => Debug.Log($"[PassiveTalentEffectSO] Node {nodeId}: TODO register reactive rule.");

        public override void Remove(ITalentEffectContext ctx, string nodeId)
            => Debug.Log($"[PassiveTalentEffectSO] Node {nodeId}: TODO unregister reactive rule.");
    }
}
