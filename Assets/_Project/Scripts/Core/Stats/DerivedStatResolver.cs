using System.Collections.Generic;
using UnityEngine;

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
            Apply(StatTypeEnum.Finesse, StatTypeEnum.Penetration, _defaults.finesseToPenetration, "Derived_Finesse_Penetration");

            // Status resistance comes from armour weight (doctrine, doc 4.1 and 4.2). Points per kg.
            // Weight is the equipment input; resistance is the single value the receiver reads.
            SetFromWeight(StatTypeEnum.RigidWeight, StatTypeEnum.StaggerResistance, RigidPointsPerKg, "Derived_RigidWeight_StaggerResistance");
            SetFromWeight(StatTypeEnum.FlexWeight, StatTypeEnum.BleedResistance, FlexPointsPerKg, "Derived_FlexWeight_BleedResistance");

            // Deflection comes only from plate armour: each equipped plate piece adds points, up to the set maximum.
            SetDeflectionFromPlate();
            Apply(StatTypeEnum.Vigor, StatTypeEnum.MaxHealth, _defaults.vigorToMaxHealth, "Derived_Vigor_MaxHealth");
            Apply(StatTypeEnum.Willpower, StatTypeEnum.MaxEnergy, _defaults.willpowerToMaxEnergy, "Derived_Willpower_MaxEnergy");
        }

        // Doctrine: plate gives 0.2 points of stagger resistance per kg; flexible armour gives 0.333 points of bleed resistance per kg.
        public const float RigidPointsPerKg = 0.2f;
        public const float FlexPointsPerKg = 0.333f;

        // Deflection doctrine: each plate piece gives 3 points, to a maximum of 5 pieces (15 points).
        public const float DeflectionPerPlatePiece = 3f;
        public const int MaxPlatePieces = 5;
        private const string DeflectionSourceId = "Derived_Plate_Deflection";

        private void SetDeflectionFromPlate()
        {
            _stats.RemoveModifiersFromSource(DeflectionSourceId);
            int pieces = Mathf.Clamp(Mathf.RoundToInt(_stats.GetBaseStat(StatTypeEnum.PlateCount)), 0, MaxPlatePieces);
            if (pieces <= 0) return;

            _stats.ApplyOrReplaceModifier(new StatModifier(
                type: ModifierType.FlatAdditive,
                category: ModifierCategory.Equipment,
                statType: StatTypeEnum.DeflectionChance,
                value: pieces * DeflectionPerPlatePiece,
                sourceId: DeflectionSourceId,
                duration: -1f));
        }

        // Equipment weight becomes a derived resistance. Re-derived on every recalculation, so swapping armour replaces it.
        private void SetFromWeight(StatTypeEnum weight, StatTypeEnum resistance, float pointsPerKg, string sourceId)
        {
            _stats.RemoveModifiersFromSource(sourceId);
            float kg = _stats.GetBaseStat(weight);
            if (kg <= 0f) return;

            _stats.ApplyOrReplaceModifier(new StatModifier(
                type: ModifierType.FlatAdditive,
                category: ModifierCategory.Equipment,
                statType: resistance,
                value: kg * pointsPerKg,
                sourceId: sourceId,
                duration: -1f));
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
