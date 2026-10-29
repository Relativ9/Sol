using UnityEngine;

namespace Sol
{
    /// <summary>
    /// A stagger: interrupts the target, locks movement and combat, drains energy, and pushes away from the source.
    /// One asset per strength (e.g. "Stagger Small", "Stagger Large", "Boss Stagger").
    /// </summary>
    [CreateAssetMenu(fileName = "New Stagger", menuName = "Sol/Status/Stagger")]
    public class StaggerEffectSO : StatusEffectSO
    {
        [Header("Stagger")]
        [SerializeField] private float _energyCost = 15f;
        [Tooltip("Horizontal push away from the source, in m/s, applied once per stagger.")]
        [SerializeField] private float _knockbackSpeed = 4f;
        [Tooltip("Animator trigger fired on stagger. Empty = none.")]
        [SerializeField] private string _animatorTrigger = "Stagger";

        public override bool LocksMovement => true;
        public override bool LocksCombat => true;
        public override float EnergyCostOnApply => _energyCost;
        public override float KnockbackSpeed => _knockbackSpeed;
        public override string AnimatorTrigger => _animatorTrigger;
    }
}
