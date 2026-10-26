using System;

namespace Sol
{
    /// <summary>
    /// Health for anything that can be hurt: enemies, dummies, and later NPCs. Separate from the player's vitals
    /// because the player's health is tied to saves, regen and statuses.
    /// </summary>
    public interface ITargetHealth
    {
        float Current { get; }
        float Max { get; }
        bool IsDead { get; }

        /// <summary>Raised after any change in health, with the amount removed (0 for heals).</summary>
        event Action<float> Changed;
    }
}
