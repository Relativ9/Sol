using System;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Pure damage maths: no Unity lifecycle, no services. Takes stat values through a delegate so it can be tested
    /// and reused by any receiver. Order follows the armour design doc: deflection, type mitigation, environmental, then status chances.
    /// </summary>
    public static class DamageCalculator
    {
        // Mitigation curve: final = amount * 100 / (100 + effective). Resistance and penetration are points
        // (5 = 5%). Effective resistance is capped at 400 points, which is 80% mitigation at most, so no defence
        // is ever pointless and no hit is ever fully negated. Penetration uses the same cap, so it can cancel
        // at most 80% of a defence.
        public const float CurveConstant = 100f;
        public const float ResistanceCap = StatCaps.ResistanceCap;

        /// <summary>The resistance that actually applies after penetration, clamped to 0 through the cap.</summary>
        public static float EffectiveResistance(float resistance, float penetration)
        {
            return Mathf.Clamp(resistance - penetration, 0f, ResistanceCap);
        }

        /// <summary>Applies the curve to an amount. One function for every damage type, physical included.</summary>
        public static float Mitigate(float amount, float resistance, float penetration)
        {
            float effective = EffectiveResistance(resistance, penetration);
            return amount * CurveConstant / (CurveConstant + effective);
        }

        /// <param name="info">The hit.</param>
        /// <param name="stat">Reads a stat from the receiver. Targets with no stats supply zero.</param>
        /// <param name="roller">The one source of randomness. Deflection is rolled here through it.</param>
        /// <remarks>The damage type table is resolved from ServiceLocator, so receivers never pass one in.</remarks>
        public static DamageResult Calculate(in DamageInfo info, Func<StatTypeEnum, float> stat, IChanceRoller roller)
        {
            bool isPhysical = info.Type == DamageType.Physical;

            // Deflection: physical only, rolled before any damage is applied. Chance stats are percentage points.
            if (isPhysical && roller.Roll(stat(StatTypeEnum.DeflectionChance) / 100f))
                return new DamageResult(0f, deflected: true, unmappedType: false, 0f, 0f);

            // Penetration is the attacker's, and it applies to every type, physical included.
            float penetration = info.Penetration;

            float amount = info.Amount;
            bool unmapped = false;
            IDamageTypeTable table = ServiceLocator.TryGet<IDamageTypeTable>();
            if (table != null && table.TryGetResistanceStat(info.Type, out StatTypeEnum resistance))
                amount = Mitigate(amount, stat(resistance), penetration);
            else
                unmapped = true;

            if ((info.Tags & DamageTag.Environmental) != 0)
                amount = Mitigate(amount, stat(StatTypeEnum.EnvironmentalResistance), penetration);

            // Status chances: the attacker brings a base chance (info.Statuses), and the receiver's armour reduces it.
            // Subtractive, with a floor, as in the doc. Bleed also requires damage > 0 after mitigation (doc 4.2).
            // A hit with no status effect (StaggerEffect == null) cannot stagger, so its chance stays zero.
            float stagger = 0f;
            if (isPhysical && info.Statuses.StaggerEffect != null)
                stagger = StatusChanceCalculator.ReduceByResistance(
                    info.Statuses.StaggerBaseChance, stat(StatTypeEnum.StaggerResistance));

            float bleed = 0f;
            if (isPhysical && amount > 0f && info.Statuses.BleedEffect != null)
                bleed = StatusChanceCalculator.ReduceByResistance(
                    info.Statuses.BleedBaseChance, stat(StatTypeEnum.BleedResistance));

            return new DamageResult(amount, deflected: false, unmapped, stagger, bleed);
        }
    }
}
