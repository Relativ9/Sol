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
        public const float StaggerBase = 0.20f;
        public const float BleedBase = 0.25f;
        public const float WeaponBlunt = 0.15f;      // Blunt/polearm bonus (doc 4.1)
        public const float WeaponBladedPiercing = 0.20f; // Blade/piercing bonus (doc 4.2)
        public const float RigidPerKg = 0.002f;      // -0.2% per kg of plate
        public const float FlexPerKg = 0.00333f;     // -0.333% per kg of flex
        public const float Floor = 0.05f;

        public static float Stagger(float rigidWeightKg, bool blunt)
        {
            float bonus = blunt ? WeaponBlunt : 0f;
            float raw = StaggerBase + bonus - rigidWeightKg * RigidPerKg;
            return Mathf.Max(Floor, raw);
        }

        public static float Bleed(float flexWeightKg, bool bladedOrPiercing)
        {
            float bonus = bladedOrPiercing ? WeaponBladedPiercing : 0f;
            float raw = BleedBase + bonus - flexWeightKg * FlexPerKg;
            return Mathf.Max(Floor, raw);
        }
    }
}
