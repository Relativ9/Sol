using UnityEngine;

namespace Sol
{
    /// <summary>
    /// What a status can do to the entity it's on. Any target can implement it (the player, enemies, dummies). Every hook is
    /// optional in practice: a target with no animator or combat state simply ignores those calls.
    /// Statuses never reach into the player's services directly, so the same effect works on any target.
    /// </summary>
    public interface IStatusTarget
    {
        /// <summary>Shown in the combat log.</summary>
        string StatusName { get; }

        /// <summary>Damage or healing from a status tick or effect cost. Negative removes health.</summary>
        void ApplyHealthDelta(float delta, string source);

        /// <summary>Energy cost of applying an effect. Negative spends energy. Targets without energy ignore it.</summary>
        void ApplyEnergyDelta(float delta, string source);

        /// <summary>Plays an animator trigger on the target's animation, if it has one.</summary>
        void SetAnimatorTrigger(string trigger);

        /// <summary>Cancels an attack in progress, for effects that lock combat.</summary>
        void CancelCombat();

        /// <summary>
        /// True if the target blocks this effect, from its type (an ActorStatSO flag) or from a temporary modifier
        /// (for the player, such as a potion or a talent). A blocked effect is not applied and is not logged as applied.
        /// </summary>
        bool IsImmuneTo(IStatusEffect effect);
    }
}
