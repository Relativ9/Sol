using UnityEngine;

namespace Sol
{
    /// <summary>
    /// An attack's damage definition. Designers make one asset per attack or hit type (trap, club, frost spike).
    /// Builds a DamageInfo at the moment of impact, adding any attacker bonus (e.g. MeleeDamage) to the base value.
    /// </summary>
    [CreateAssetMenu(fileName = "New Damage", menuName = "Sol/Combat/Damage")]
    public class DamageSO : ScriptableObject
    {
        public DamageType damageType = DamageType.Physical;
        public float baseAmount = 10f;

        // Blunt, Blade, Piercing etc. here drive the status chances. Their base rates come from the armour rules.
        public DamageTag tags = DamageTag.None;

        public DamageInfo Build(string sourceId, float attackerBonus = 0f, bool sourceIsPlayer = false, Vector3 sourcePosition = default)
        {
            return new DamageInfo(sourceId, damageType, tags, baseAmount + attackerBonus, sourceIsPlayer, sourcePosition);
        }
    }
}
