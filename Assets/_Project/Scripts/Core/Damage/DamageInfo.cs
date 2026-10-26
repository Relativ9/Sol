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

        // Where the hit came from. Used by the combat log to decide whether to show it.
        public readonly bool SourceIsPlayer;
        public readonly Vector3 SourcePosition;

        public DamageInfo(string sourceId, DamageType type, DamageTag tags, float amount, bool sourceIsPlayer = false, Vector3 sourcePosition = default)
        {
            SourceId = sourceId;
            Type = type;
            Tags = tags;
            Amount = amount;
            SourceIsPlayer = sourceIsPlayer;
            SourcePosition = sourcePosition;
        }
    }
}
