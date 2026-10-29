using UnityEngine;

namespace Sol
{
    /// <summary>
    /// A hit as data. Agnostic of what it hits: it carries the amount, type, tags, and the base chances
    /// of the statuses it can cause. Receivers decide how it applies.
    /// </summary>
    public readonly struct DamageInfo
    {
        public readonly string SourceId;
        public readonly DamageType Type;
        public readonly DamageTag Tags;
        public readonly float Amount;

        // Used by the combat log to decide whether to show it.
        public readonly bool SourceIsPlayer;

        // The attacker's root position (enemy body, shooter, player root). Knockback direction is computed from this,
        // so it is never the position of a weapon or projectile.
        public readonly Vector3 SourceOrigin;

        // Where the hit landed (weapon tip, projectile position). Used for effects and as a fallback direction.
        public readonly Vector3 HitPoint;

        // The attacker's status contribution (effects and base chances). Default = the hit causes no statuses.
        public readonly StatusAttack Statuses;

        // The attacker's penetration, in points. Cancels up to the resistance cap of the receiver's defences.
        public readonly float Penetration;

        public DamageInfo(string sourceId, DamageType type, DamageTag tags, float amount, bool sourceIsPlayer = false,
            Vector3 sourceOrigin = default, StatusAttack statuses = default, Vector3 hitPoint = default, float penetration = 0f)
        {
            SourceId = sourceId;
            Type = type;
            Tags = tags;
            Amount = amount;
            SourceIsPlayer = sourceIsPlayer;
            SourceOrigin = sourceOrigin;
            Statuses = statuses;
            Penetration = penetration;
            // An unset hit point falls back to the origin, so it is always a usable position.
            HitPoint = hitPoint == default ? sourceOrigin : hitPoint;
        }
    }
}
