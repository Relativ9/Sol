using UnityEngine;

namespace Sol
{
    [CreateAssetMenu(menuName = "Sol/Talent System/Effects/Stat Modifier")]
    public class StatModifierTalentEffectSO : TalentEffectSO
    {
        public StatTypeEnum targetStat;
        public ModifierType modifierType;
        public ModifierCategory category = ModifierCategory.Permanent;
        public float valuePerPoint;

        public override void Apply(ITalentEffectContext context, string nodeId, int points)
        {
            var mod = new StatModifier(
                type: modifierType,
                category: category,
                statType: targetStat,
                value: valuePerPoint * points,
                sourceId: nodeId,
                duration: -1f
            );
            context.StatsService.ApplyOrReplaceModifier(mod);
        }

        public override void Remove(ITalentEffectContext context, string nodeId)
        {
            context.StatsService.RemoveModifiersFromSource(nodeId);
        }
    }
}
