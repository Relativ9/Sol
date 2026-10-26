using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Decides which combat events reach the combat log.
    /// Rules: damage from the player always shows. Damage from anything else shows only within
    /// <see cref="NearbyRadius"/> metres of the player. NPC-on-NPC events will use the same radius
    /// once NPCs can attack each other.
    /// </summary>
    public static class LogRules
    {
        public const float NearbyRadius = 20f;

        /// <param name="sourceIsPlayer">True if the player caused this event.</param>
        /// <param name="eventPosition">Where the event happened (usually the target).</param>
        public static bool ShouldShow(bool sourceIsPlayer, Vector3 eventPosition)
        {
            if (sourceIsPlayer) return true;

            IPlayerVitals vitals = ServiceLocator.TryGet<IPlayerVitals>();
            if (vitals == null) return false;

            Transform player = PlayerTransform();
            if (player == null) return false;

            return (eventPosition - player.position).sqrMagnitude <= NearbyRadius * NearbyRadius;
        }

        /// <summary>Adds the event to the combat log if the rules allow it.</summary>
        public static void Report(string sourceId, string targetName, string text, CombatLogKind kind, Vector3 position, bool sourceIsPlayer)
        {
            if (!ShouldShow(sourceIsPlayer, position)) return;
            ServiceLocator.TryGet<ICombatLog>()?.Add($"{sourceId} → {targetName}: {text}", kind);
        }

        // Found by tag so the rules work wherever the player is in the scene.
        static Transform PlayerTransform()
        {
            GameObject player = GameObject.FindWithTag("Player");
            return player != null ? player.transform : null;
        }
    }
}
