using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Proc chances for stagger and bleed, from the armour design doc (sections 4.1 and 4.2).
    /// Subtractive, not multiplicative: armour weight is taken off the chance, with a 5% floor.
    /// Stagger uses Rigid (plate) weight only. Bleed uses Flexible weight only.
    /// Pure maths: returns a chance. The rolls service decides whether it happens.
    /// </summary>
    public static class StatusChanceCalculator
    {
        // Attacker-side doctrine (doc 4.1 and 4.2). Fixed by damage type and tags, never set per asset.
        public const float StaggerBase = 0.20f;
        public const float BleedBase = 0.25f;
        public const float WeaponBlunt = 0.15f;          // Blunt bonus (doc 4.1)
        public const float WeaponBladedPiercing = 0.20f; // Blade or piercing bonus (doc 4.2)

        /// <summary>
        /// Attacker's base stagger chance for a hit. Zero unless the hit is physical and blunt.
        /// <paramref name="bonus"/> is the attacker's stat bonus (enchantments, traits), added on top of the doctrine base.
        /// </summary>
        public static float AttackerStaggerChance(DamageType type, DamageTag tags, float bonus)
        {
            if (type != DamageType.Physical || (tags & DamageTag.Blunt) == 0) return 0f;
            return Clamp01Checked(StaggerBase + WeaponBlunt + bonus, "StaggerChance");
        }

        // Chances are fractions (0 to 1). A bonus above 1 is almost always a unit typo (50 for 50%), so it is clamped
        // and logged. Without this, a value like 50 makes the proc certain, which is what happened on the club dummy.
        private static float Clamp01Checked(float chance, string label)
        {
            if (chance < 0f || chance > 1f)
                UnityEngine.Debug.LogWarning($"[StatusChanceCalculator] {label} = {chance:0.##} is outside 0-1 after conversion. Check the stat value: chance stats are percentage points (5 = 5%). Clamped.");
            return Mathf.Clamp01(chance);
        }

        /// <summary>
        /// Attacker's base bleed chance for a hit. Zero unless the hit is physical and blade or piercing.
        /// <paramref name="bonus"/> is the attacker's stat bonus, added on top of the doctrine base.
        /// </summary>
        public static float AttackerBleedChance(DamageType type, DamageTag tags, float bonus)
        {
            if (type != DamageType.Physical || (tags & (DamageTag.Blade | DamageTag.Piercing)) == 0) return 0f;
            return Clamp01Checked(BleedBase + WeaponBladedPiercing + bonus, "BleedChance");
        }

        // Receiver rule: status resistance is subtractive and floored (conceptually: an open wound stays an open wound).
        public const float Floor = 0.05f;

        /// <summary>
        /// The receiver's side: subtracts its status resistance from the attacker's base chance, floored at 5%.
        /// Resistance is in points (5 = 5%), converted once here. A zero base chance stays zero, so no hit can be
        /// turned into a proc by resistance.
        /// </summary>
        public static float ReduceByResistance(float baseChance, float resistancePoints)
        {
            if (baseChance <= 0f) return 0f;
            return Mathf.Max(Floor, baseChance - resistancePoints / 100f);
        }
    }
}
