using System;

namespace Sol
{
    /// <summary>
    /// What a stat is for. Set on each StatTypeEnum member, so the role lives with the stat and no second list exists.
    /// Enforced by StatsService: only Attribute stats can be set by progression, Derived stats are written by the
    /// derivation and equipment, and Tuning stats are changed only through temporary modifiers (effects, zones, spells).
    /// Tuning stats are never shown on the stats screen.
    /// </summary>
    public enum StatRole
    {
        /// <summary>Player-spent core attribute. Set through progression and shown on the stats screen.</summary>
        Attribute,

        /// <summary>Computed from attributes, equipment and talents. Shown on the stats screen, never set directly.</summary>
        Derived,

        /// <summary>Game-tuned value (movement, gravity, effect chances). Changed by effects, zones and spells through
        /// modifiers. Hidden from the stats screen, and never set as a base value.</summary>
        Tuning,
    }

    /// <summary>Marks a StatTypeEnum member with its role. Read through StatRoles, which caches the lookup.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class StatAttribute : Attribute
    {
        public StatRole Role { get; }

        public StatAttribute(StatRole role)
        {
            Role = role;
        }
    }
}
