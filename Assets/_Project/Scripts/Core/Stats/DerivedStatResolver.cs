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

            // Multipliers never fall below their design value. This is a minimum, not a base: totals above it are unchanged.
            _stats.SetDerivedFloor(StatTypeEnum.CritMultiplier, _defaults.minCritMultiplier);
            _stats.SetDerivedFloor(StatTypeEnum.RunMultiplier, _defaults.minRunMultiplier);
            Apply(StatTypeEnum.Brawn, StatTypeEnum.JumpForce, _defaults.brawnToJumpForce, "Derived_Brawn_JumpForce");
            ApplyFallDamageReduction();

            Apply(StatTypeEnum.Brawn, StatTypeEnum.MeleeDamage, _defaults.brawnToMeleeDamage, "Derived_Brawn_MeleeDamage");
            Apply(StatTypeEnum.Brawn, StatTypeEnum.CarryCapacity, _defaults.brawnToCarryCapacity, "Derived_Brawn_CarryCapacity");
            Apply(StatTypeEnum.Brawn, StatTypeEnum.LiftStrength, _defaults.brawnToLiftStrength, "Derived_Brawn_LiftStrength");
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
            Apply(StatTypeEnum.Vigor, StatTypeEnum.EnergyRegen, _defaults.vigorToEnergyRegen, "Derived_Vigor_EnergyRegen");
            Apply(StatTypeEnum.Focus, StatTypeEnum.HealthRegen, _defaults.focusToHealthRegen, "Derived_Focus_HealthRegen");
            Apply(StatTypeEnum.Vigor, StatTypeEnum.Hunger, _defaults.vigorToHunger, "Derived_Vigor_Hunger");
            Apply(StatTypeEnum.Vigor, StatTypeEnum.Thirst, _defaults.vigorToThirst, "Derived_Vigor_Thirst");
            Apply(StatTypeEnum.Focus, StatTypeEnum.ProjectileRange, _defaults.focusToProjectileRange, "Derived_Focus_ProjectileRange");
            Apply(StatTypeEnum.Insight, StatTypeEnum.MagicResistance, _defaults.insightToMagicResistance, "Derived_Insight_MagicResistance");
            Apply(StatTypeEnum.Vigor, StatTypeEnum.EnvironmentalResistance, _defaults.vigorToEnvironmentalResistance, "Derived_Vigor_EnvironmentalResistance");
            Apply(StatTypeEnum.Focus, StatTypeEnum.ParryAndBlockTime, _defaults.focusToParryAndBlockTime, "Derived_Focus_ParryAndBlockTime");
            Apply(StatTypeEnum.Focus, StatTypeEnum.ClueRange, _defaults.focusToClueRange, "Derived_Focus_ClueRange");
            Apply(StatTypeEnum.Willpower, StatTypeEnum.CharmChance, _defaults.willpowerToCharmChance, "Derived_Willpower_CharmChance");
            Apply(StatTypeEnum.Insight, StatTypeEnum.KnowledgeChance, _defaults.insightToKnowledgeChance, "Derived_Insight_KnowledgeChance");
            Apply(StatTypeEnum.Finesse, StatTypeEnum.DeceptionChance, _defaults.finesseToDeceptionChance, "Derived_Finesse_DeceptionChance");
            Apply(StatTypeEnum.Brawn, StatTypeEnum.IntimidationChance, _defaults.brawnToIntimidationChance, "Derived_Brawn_IntimidationChance");
            Apply(StatTypeEnum.Finesse, StatTypeEnum.CovertMovement, _defaults.finesseToCovertMovement, "Derived_Finesse_CovertMovement");
            Apply(StatTypeEnum.Finesse, StatTypeEnum.Tinker, _defaults.finesseToTinker, "Derived_Finesse_Tinker");
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
            int pieces = Mathf.Clamp(Mathf.RoundToInt(_stats.GetStat(StatTypeEnum.PlateCount)), 0, MaxPlatePieces);
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
            float kg = _stats.GetStat(weight);
            if (kg <= 0f) return;

            _stats.ApplyOrReplaceModifier(new StatModifier(
                type: ModifierType.FlatAdditive,
                category: ModifierCategory.Equipment,
                statType: resistance,
                value: kg * pointsPerKg,
                sourceId: sourceId,
                duration: -1f));
        }

        // FallDamageMultiplier starts at 1 and goes DOWN as Finesse rises: 1 - Finesse × rate, never below 0.
        // Derived stats have no SO base, so the starting 1 is part of this rule, not a stored value.
        private const float FallDamageStart = 1f;
        private const string FallDamageSourceId = "Derived_Finesse_FallDamageMultiplier";

        private void ApplyFallDamageReduction()
        {
            float reduction = _stats.GetBaseStat(StatTypeEnum.Finesse) * _defaults.finesseToFallDamageReduction;
            float total = Mathf.Max(0f, FallDamageStart - reduction);

            _stats.RemoveModifiersFromSource(FallDamageSourceId);
            _stats.ApplyOrReplaceModifier(new StatModifier(
                type: ModifierType.FlatAdditive,
                category: ModifierCategory.Permanent,
                statType: StatTypeEnum.FallDamageMultiplier,
                value: total,
                sourceId: FallDamageSourceId,
                duration: -1f));
        }

        // The calculation rule for every attribute-derived stat. The bonus is the attribute's full current value times the rate.
        // The starting points in the stat asset are part of that value: they are the points a player begins with, not a
        // baseline that is subtracted. A stat deriving at 1x1 from Brawn is therefore Brawn itself, so 10 Brawn gives 10 and
        // 9000 gives 9000. Derived stats start from zero, so a modifier that is removed cannot leave a stale value behind.
        private void Apply(StatTypeEnum attribute, StatTypeEnum target, float perPoint, string sourceId)
        {
            float value = _stats.GetBaseStat(attribute) * perPoint;

            // Each derivation has its own source, so it is cleared and rewritten on every recalculation.
            _stats.RemoveModifiersFromSource(sourceId);
            if (value == 0f) return;

            _stats.ApplyOrReplaceModifier(new StatModifier(
                type: ModifierType.FlatAdditive,
                category: ModifierCategory.Permanent,
                statType: target,
                value: value,
                sourceId: sourceId,
                duration: -1f));
        }
    }
}
