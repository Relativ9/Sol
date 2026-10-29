using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Owns one enemy's ActorStats. Assign an ActorStatSO per enemy type. Hit sources on the same enemy
    /// (its damage source, its projectiles) read the stats from here, so modifiers apply everywhere.
    /// </summary>
    public class EnemyStats : MonoBehaviour
    {
        [SerializeField] private ActorStatSO _baseStats;

        public ActorStats Stats { get; private set; }

        void Awake()
        {
            Stats = new ActorStats(_baseStats);
        }

        /// <summary>Returns the stats for the enemy on this object or a parent, or null when there is none.</summary>
        public static ActorStats Find(Transform from)
        {
            EnemyStats owner = from.GetComponentInParent<EnemyStats>();
            return owner != null ? owner.Stats : null;
        }

        /// <summary>
        /// The enemy's root position: the body, not a weapon or hitbox. Used as the knockback origin.
        /// Falls back to the topmost transform when no EnemyStats is present, with a warning so it can't go silent.
        /// </summary>
        public static Vector3 RootPosition(Transform from)
        {
            EnemyStats owner = from.GetComponentInParent<EnemyStats>();
            if (owner != null) return owner.transform.position;

            Debug.LogWarning($"[EnemyStats] {from.name} has no EnemyStats above it. Using its root transform as the knockback origin.", from);
            return from.root.position;
        }
    }
}
