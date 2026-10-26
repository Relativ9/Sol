using System.Collections.Generic;

namespace Sol
{
    /// <summary>
    /// Converts the six core attributes into secondary stat bonuses.
    /// Plain C#: no Unity lifecycle. Each attribute→stat link becomes one Permanent FlatAdditive modifier
    /// with a reserved sourceId, so it can be replaced on every recalculation without touching talent modifiers.
    /// </summary>
    public class DerivedStatResolver
    {
        private readonly IStatsService _stats;
        private readonly PlayerStatSO _defaults;

        public DerivedStatResolver(IStatsService stats, PlayerStatSO defaults)
        {
            _stats = stats;
            _defaults = defaults;
        }

        /// <summary>Recomputes every attribute-derived bonus. Safe to call repeatedly.</summary>
        public void RecalculateAll()
        {
            if (_stats == null || _defaults == null) return;

            Apply(StatTypeEnum.Brawn, StatTypeEnum.MeleeDamage, _defaults.brawnToMeleeDamage, "Derived_Brawn_MeleeDamage");
            Apply(StatTypeEnum.Brawn, StatTypeEnum.CarryCapacity, _defaults.brawnToCarryCapacity, "Derived_Brawn_CarryCapacity");
            Apply(StatTypeEnum.Finesse, StatTypeEnum.ProjectileDamage, _defaults.finesseToProjectileDamage, "Derived_Finesse_ProjectileDamage");
            Apply(StatTypeEnum.Insight, StatTypeEnum.SpellPower, _defaults.insightToSpellPower, "Derived_Insight_SpellPower");
            Apply(StatTypeEnum.Insight, StatTypeEnum.CritMultiplier, _defaults.insightToCritMultiplier, "Derived_Insight_CritMultiplier");
            Apply(StatTypeEnum.Focus, StatTypeEnum.CritChance, _defaults.focusToCritChance, "Derived_Focus_CritChance");
            Apply(StatTypeEnum.Vigor, StatTypeEnum.MaxHealth, _defaults.vigorToMaxHealth, "Derived_Vigor_MaxHealth");
            Apply(StatTypeEnum.Willpower, StatTypeEnum.MaxEnergy, _defaults.willpowerToMaxEnergy, "Derived_Willpower_MaxEnergy");
        }

        // Bonus is based on points above the default. GetBaseStat avoids feeding derived modifiers back into the attribute.
        private void Apply(StatTypeEnum attribute, StatTypeEnum target, float perPoint, string sourceId)
        {
            float defaultValue = GetDefault(attribute);
            float points = _stats.GetBaseStat(attribute) - defaultValue;

            // Zero or negative points (refunded below default) must still clear the old bonus.
            _stats.RemoveModifiersFromSource(sourceId);
            if (points == 0f) return;

            _stats.ApplyOrReplaceModifier(new StatModifier(
                type: ModifierType.FlatAdditive,
                category: ModifierCategory.Permanent,
                statType: target,
                value: points * perPoint,
                sourceId: sourceId,
                duration: -1f));
        }

        private float GetDefault(StatTypeEnum attribute)
        {
            return attribute switch
            {
                StatTypeEnum.Brawn => _defaults.brawn,
                StatTypeEnum.Finesse => _defaults.finesse,
                StatTypeEnum.Insight => _defaults.insight,
                StatTypeEnum.Focus => _defaults.focus,
                StatTypeEnum.Vigor => _defaults.vigor,
                StatTypeEnum.Willpower => _defaults.willpower,
                _ => 0f
            };
        }
    }
}
