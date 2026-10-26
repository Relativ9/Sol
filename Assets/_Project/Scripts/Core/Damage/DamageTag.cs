using System;

namespace Sol
{
    /// <summary>
    /// Descriptive tags on a hit. Tags change how a hit is mitigated or which statuses it can cause, but are not damage types.
    /// Saved as integers, so new flags are appended only.
    /// </summary>
    [Flags]
    public enum DamageTag
    {
        None = 0,
        Environmental = 1 << 0, // Traps, gas, heat, cold. Also reduced by EnvironmentalResistance.
        Melee = 1 << 1,
        Ranged = 1 << 2,
        Spell = 1 << 3,
        Area = 1 << 4,          // AoE. Crit chance is halved for these (design doc).
        Blade = 1 << 5,         // Can cause Bleed when physical.
        Blunt = 1 << 6,         // Can cause Stagger when physical.
        Piercing = 1 << 7,      // Can cause Bleed when physical.
    }
}
