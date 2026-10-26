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
        // Diminishing-returns constant from the armour design doc. Damage can never be fully negated.
        public const float MitigationConstant = 200f;

        public static float Mitigate(float amount, float resistance)
        {
            return amount * MitigationConstant / (MitigationConstant + Mathf.Max(0f, resistance));
        }

        /// <param name="info">The hit.</param>
        /// <param name="stat">Reads a stat from the receiver. Targets with no stats supply zero.</param>
        /// <param name="roller">The one source of randomness. Deflection is rolled here through it.</param>
        /// <remarks>The damage type table is resolved from ServiceLocator, so receivers never pass one in.</remarks>
        public static DamageResult Calculate(in DamageInfo info, Func<StatTypeEnum, float> stat, IChanceRoller roller)
        {
            bool isPhysical = info.Type == DamageType.Physical;

            // Deflection: physical only, rolled before any damage is applied.
            if (isPhysical && roller.Roll(stat(StatTypeEnum.DeflectionChance)))
                return new DamageResult(0f, deflected: true, unmappedType: false, 0f, 0f);

            float amount = info.Amount;
            bool unmapped = false;
            IDamageTypeTable table = ServiceLocator.TryGet<IDamageTypeTable>();
            if (table != null && table.TryGetResistanceStat(info.Type, out StatTypeEnum resistance))
                amount = Mitigate(amount, stat(resistance));
            else
                unmapped = true;

            if ((info.Tags & DamageTag.Environmental) != 0)
                amount = Mitigate(amount, stat(StatTypeEnum.EnvironmentalResistance));

            // Status chances use the doc's subtractive rules, driven by armour weight.
            // Bleed also requires damage > 0 after mitigation (doc 4.2).
            float stagger = 0f;
            if (isPhysical && (info.Tags & DamageTag.Blunt) != 0)
                stagger = StatusChanceCalculator.Stagger(stat(StatTypeEnum.RigidWeight), blunt: true);

            float bleed = 0f;
            if (isPhysical && amount > 0f && (info.Tags & (DamageTag.Blade | DamageTag.Piercing)) != 0)
                bleed = StatusChanceCalculator.Bleed(stat(StatTypeEnum.FlexWeight), bladedOrPiercing: true);

            return new DamageResult(amount, deflected: false, unmapped, stagger, bleed);
        }
    }
}
