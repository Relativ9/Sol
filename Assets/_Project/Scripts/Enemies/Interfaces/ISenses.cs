using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Read-only perception interface for a target. Allows behavior and tactics to query awareness without
    /// depending on the concrete implementation (vision system, hearing, etc.).
    /// </summary>
    public interface ISenses
    {
        /// <summary>True if the target can currently see the player via raycast line-of-sight.</summary>
        bool CanSee { get; }

        /// <summary>True if the target can currently hear the player based on movement speed and distance.</summary>
        bool CanHear { get; }

        /// <summary>True if the target is aware of the player by either sight or sound.</summary>
        bool IsAware { get; }

        /// <summary>The last known position of the player (from vision or hearing). Only valid if IsAware.</summary>
        Vector3 LastSeenPlayerPosition { get; }

        /// <summary>The distance to the player, in meters.</summary>
        float DistanceToPlayer { get; }
    }
}
