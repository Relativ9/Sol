using System.Collections.Generic;
using UnityEngine;

namespace Sol
{
    /// <summary>
    /// The player's status effects. Movement and combat read the locks, the hit pipeline rolls procs through
    /// RollHitProcs. It only knows IStatusEffect, so effect types can be added without changing receivers.
    /// </summary>
    public interface IStatusController
    {
        IReadOnlyList<StatusInstance> Active { get; }

        /// <summary>True while an active status blocks movement input.</summary>
        bool IsMovementLocked { get; }

        /// <summary>True while an active status blocks new attack input.</summary>
        bool IsCombatLocked { get; }

        /// <summary>Applies a status from a source. rawDamage is only used by effects that deal damage over time.</summary>
        void Apply(IStatusEffect effect, string sourceId, Vector3 sourceOrigin, Vector3 hitPoint, float rawDamage = 0f);

        /// <summary>Rolls the hit's status procs through the shared roller and applies any that succeed.</summary>
        void RollHitProcs(DamageResult result, DamageInfo info, IChanceRoller roller);

        /// <summary>Returns a pending knockback once, then clears it, so one application pushes exactly once.</summary>
        bool TryConsumeImpulse(out Vector3 impulse);
    }
}
