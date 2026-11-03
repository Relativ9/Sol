using UnityEngine;

namespace Sol
{
    /// <summary>
    /// Rolls a hit's status procs (stagger and bleed) for any target. Plain C#: it takes the hit and the target's result,
    /// and reports which effects proced. The target applies them, so the roll is the same for the player and enemies.
    ///
    /// Every roll is logged to the combat log, hit or miss, with the chance, so a 45% bleed can be checked in play.
    /// </summary>
    public static class StatusRoller
    {
        /// <summary>
        /// Rolls each status the hit can cause, and applies the ones that succeed. A status the attack does not carry is never rolled.
        /// An immune target is reported as immune, and the roll is skipped.
        /// </summary>
        public static void Roll(DamageResult result, DamageInfo info, IChanceRoller roller,
            IStatusController controller, string targetName, bool sourceIsPlayer, Vector3 position)
        {
            if (controller == null || roller == null) return;

            if (info.Statuses.StaggerEffect != null)
                RollOne(info.Statuses.StaggerEffect, result.StaggerChance, "Stagger", info, roller, controller, targetName, sourceIsPlayer, position);

            if (info.Statuses.BleedEffect != null)
                RollOne(info.Statuses.BleedEffect, result.BleedChance, "Bleed", info, roller, controller, targetName, sourceIsPlayer, position,
                    info.Amount);
        }

        private static void RollOne(IStatusEffect effect, float chance, string label, DamageInfo info, IChanceRoller roller,
            IStatusController controller, string targetName, bool sourceIsPlayer, Vector3 position, float rawDamage = 0f)
        {
            if (controller.IsImmuneTo(effect))
            {
                LogRules.Report(info.SourceId, targetName, $"{label} blocked (immune)", CombatLogKind.Info, position, sourceIsPlayer);
                return;
            }

            bool proced = roller.Roll(chance);
            string outcome = proced ? "applied" : "resisted";
            LogRules.Report(info.SourceId, targetName, $"{label} {outcome} ({chance:P0} chance)",
                proced ? CombatLogKind.Warning : CombatLogKind.Info, position, sourceIsPlayer);

            if (proced) controller.Apply(effect, info.SourceId, info.SourceOrigin, info.HitPoint, rawDamage);
        }
    }
}
