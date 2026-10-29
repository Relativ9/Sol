using UnityEngine;

namespace Sol
{
    /// <summary>
    /// An attack's damage definition. Designers make one asset per attack or hit type (trap, club, frost spike).
    /// Holds only content: amount, type, tags, and which status effects the hit can cause. Chances are never set here:
    /// they come from the doctrine (damage type and tags) plus the attacker's stat bonuses, in Build.
    /// </summary>
    [CreateAssetMenu(fileName = "New Damage", menuName = "Sol/Combat/Damage")]
    public class DamageSO : ScriptableObject
    {
        public DamageType damageType = DamageType.Physical;
        public float baseAmount = 10f;

        // Blunt, Blade, Piercing etc. here decide which status chances apply. The rates come from the doctrine.
        public DamageTag tags = DamageTag.None;

        [Header("Status effects (content choice)")]
        [Tooltip("Which stagger this hit can cause. Empty = no stagger. The chance is fixed by the doctrine (blunt tag).")]
        public StatusEffectSO staggerEffect;
        [Tooltip("Which bleed this hit can cause. Empty = no bleed. The chance is fixed by the doctrine (blade or piercing tag).")]
        public StatusEffectSO bleedEffect;

        /// <summary>
        /// Builds the hit. The attacker's base status chances come from the doctrine for this damage type and tags,
        /// plus the attacker's stat bonuses. Receivers then apply their armour and resistances.
        /// </summary>
        /// <param name="sourceOrigin">The attacker's root position. Used for knockback direction.</param>
        /// <param name="hitPoint">Where the hit landed. Optional; defaults to the origin.</param>
        /// <param name="staggerBonus">Attacker stagger bonus, in points (5 = +5%).</param>
        /// <param name="bleedBonus">Attacker bleed bonus, in points (5 = +5%).</param>
        /// <param name="penetration">Attacker penetration, in points. Cancels receiver resistance, capped by the curve.</param>
        public DamageInfo Build(string sourceId, float attackerBonus = 0f, bool sourceIsPlayer = false,
            Vector3 sourceOrigin = default, Vector3 hitPoint = default,
            float staggerBonus = 0f, float bleedBonus = 0f, float penetration = 0f)
        {
            // Bonuses are entered as points; converted to fractions once, here.
            float stagger = StatusChanceCalculator.AttackerStaggerChance(damageType, tags, staggerBonus / 100f);
            float bleed = StatusChanceCalculator.AttackerBleedChance(damageType, tags, bleedBonus / 100f);
            var statuses = new StatusAttack(staggerEffect, stagger, bleedEffect, bleed);
            return new DamageInfo(sourceId, damageType, tags, baseAmount + attackerBonus, sourceIsPlayer, sourceOrigin,
                statuses, hitPoint, penetration);
        }
    }
}
